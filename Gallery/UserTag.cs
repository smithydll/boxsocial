/*
 * Box Social™
 * http://boxsocial.net/
  * Copyright © 2007, David Smith
 * 
 * $Id:$
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License version 2 of
 * the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Text;
using System.Web;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Applications.Gallery
{
    /// <summary>
    /// Represents a tag on a photo of a user in the photo.
    /// </summary>
    [DataTable("user_tags")]
    public sealed class UserTag : NumberedItem
    {
        //private static string TAG_INFO_FIELDS = "ut.tag_id, ut.tag_user_id, ut.gallery_item_id, ut.tag_x, ut.tag_y, ut.user_id, ut.tag_approved";

        [DataField("tag_id", DataFieldKeys.Primary)]
        private long tagId;
        [DataField("tag_user_id", typeof(User))]
        private long userId;
        [DataField("gallery_item_id", typeof(GalleryItem))]
        private long galleryItemId;
        [DataField("user_id")]
        private long ownerId; // person who submitted the tag
        [DataField("tag_approved")]
        private bool tagApproved;
        [DataField("tag_x")]
        private int tagX;
        [DataField("tag_y")]
        private int tagY;

        private Point tagLocation;
        private User taggedMember;
        private GalleryItem taggedGalleryItem;

        /// <summary>
        /// Gets the user tag entry id
        /// </summary>
        public long TagId
        {
            get
            {
                return tagId;
            }
        }

        /// <summary>
        /// Gets the user tagged
        /// </summary>
        public User TaggedMember
        {
            get
            {
                if (taggedMember == null || userId != taggedMember.Id)
                {
                    core.LoadUserProfile(userId);
                    taggedMember = core.PrimitiveCache[userId];
                    return taggedMember;
                }
                else
                {
                    return taggedMember;
                }
            }
        }

        /// <summary>
        /// Gets the photo tagged
        /// </summary>
        public GalleryItem TaggedGalleryItem
        {
            get
            {
                return taggedGalleryItem;
            }
        }

        /// <summary>
        /// Location point is scaled to square 640 000 x 640 000
        /// </summary>
        public Point TagLocation
        {
            get
            {
                if (tagLocation == null)
                {
                    tagLocation = new Point(tagX, tagY);
                }
                return tagLocation;
            }
        }

        /// <summary>
        /// Initialises a new instance of the UserTag class.
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="galleryItem">The gallery item tagged</param>
        /// <param name="tagId">Tag Id to retrieve</param>
        public UserTag(Core core, GalleryItem galleryItem, long tagId)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(UserTag_ItemLoad);

            taggedGalleryItem = galleryItem;

            try
            {
                LoadItem(tagId);
            }
            catch (InvalidItemException)
            {
                throw new InvalidUserTagException();
            }
        }

        /// <summary>
        /// Initialises a new instance of the UserTag class.
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="galleryItem">Gallery item</param>
        /// <param name="tagRow">Raw data row of user tag</param>
        private UserTag(Core core, GalleryItem galleryItem, DataRow tagRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(UserTag_ItemLoad);

            taggedGalleryItem = galleryItem;

            loadItemInfo(tagRow);
        }

        void UserTag_ItemLoad()
        {
            tagLocation = new Point(tagX, tagY);
            core.PrimitiveCache.LoadUserProfile(userId);
        }

        /// <summary>
        /// Retrieves a list of user tags for a given photo.
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="galleryItem">Gallery item to retrieve user tags of</param>
        /// <returns>A list of user tags</returns>
        public static List<UserTag> GetTags(Core core, GalleryItem galleryItem)
        {
            List<UserTag> tags = new List<UserTag>();

            SelectQuery query = UserTag.GetSelectQueryStub(typeof(UserTag));
            query.AddCondition("gallery_item_id", galleryItem.ItemId);

            DataTable tagDataTable = core.Db.Query(query);

            List<long> userIds = new List<long>();
            foreach (DataRow dr in tagDataTable.Rows)
            {
                userIds.Add((long)dr["tag_user_id"]);
            }

            core.LoadUserProfiles(userIds);

            foreach (DataRow dr in tagDataTable.Rows)
            {
                tags.Add(new UserTag(core, galleryItem, dr));
            }

            return tags;
        }

        /// <summary>
        /// Approves a user tag for public display.
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="tag">User tag to approve</param>
        /// <returns>True on success</returns>
        public static bool ApproveTag(Core core, UserTag tag)
        {
            UpdateQuery query = new UpdateQuery("user_tags");
            query.AddField("tag_approved", true);
            query.AddCondition("tag_id", tag.TagId);
            query.AddCondition("user_id", core.LoggedInMemberId);

            if (core.Db.Query(query) == 1)
            {
                tag.tagApproved = true; // we can update private members
                NotifyTag(core, tag);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Deletes a user tag.
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="tagId">Tag Id to delete</param>
        /// <returns>True on success</returns>
        public static bool DeleteTag(Core core, long tagId)
        {
            DeleteQuery query = new DeleteQuery("user_tags");
            query.AddCondition("tag_id", tagId);
            query.AddCondition("user_id", core.LoggedInMemberId);

            if (core.Db.Query(query) == 1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="galleryItem"></param>
        /// <param name="owner"></param>
        /// <param name="member"></param>
        /// <param name="location"></param>
        /// <returns></returns>
        public static UserTag Create(Core core, GalleryItem galleryItem, User owner, User member, Point location)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            InsertQuery query = new InsertQuery("user_tags");
            query.AddField("user_id", owner.UserId);
            query.AddField("tag_user_id", member.UserId);
            query.AddField("gallery_item_id", galleryItem.ItemId);
            query.AddField("tag_x", location.X);
            query.AddField("tag_y", location.Y);
            if (owner.UserId != member.UserId)
            {
                query.AddField("tag_approved", false);
            }
            else
            {
                query.AddField("tag_approved", true);
            }

            long tagId = core.Db.Query(query);

            UserTag tag = new UserTag(core, galleryItem, tagId);
            NotifyTag(core, tag);

            return tag;
        }

        /// <summary>
        /// Notify users affected by a tag of the tag
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="tag">Tag to notify of</param>
        private static void NotifyTag(Core core, UserTag tag)
        {
            if (tag.tagApproved)
            {
                if (tag.TaggedMember.UserInfo.EmailNotifications)
                {
                    RawTemplate emailTemplate = new RawTemplate(core.Http.TemplateEmailPath, "photo_tag_notification.eml");

                    emailTemplate.Parse("TO_NAME", tag.TaggedMember.DisplayName);
                    emailTemplate.Parse("FROM_NAME", core.Session.LoggedInMember.DisplayName);
                    emailTemplate.Parse("FROM_USERNAME", core.Session.LoggedInMember.UserName);
                    emailTemplate.Parse("U_PHOTO", core.Hyperlink.StripSid(core.Hyperlink.AppendAbsoluteSid(tag.TaggedGalleryItem.BuildUri())));

                    core.Email.SendEmail(tag.TaggedMember.UserInfo.PrimaryEmail, string.Format("{0} tagged you in a photo",
                        core.Session.LoggedInMember.DisplayName),
                        emailTemplate.ToString());
                }
            }
        }

        public override long Id
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override string Uri
        {
            get
            {
                throw new NotImplementedException();
            }
        }
    }

    /// <summary>
    /// The exception that is thrown when a requested user tag does not exist.
    /// </summary>
    public class InvalidUserTagException : Exception
    {
    }
}
