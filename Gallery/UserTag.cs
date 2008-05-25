/*
 * Box Social™
 * http://boxsocial.net/
 * Copyright © 2007, David Lachlan Smith
 * 
 * $Id:$
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License version 3 as
 * published by the Free Software Foundation.
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
    public sealed class UserTag
    {
        private static string TAG_INFO_FIELDS = "ut.tag_id, ut.tag_user_id, ut.gallery_item_id, ut.tag_x, ut.tag_y, ut.user_id, ut.tag_approved";

        private Core core;

        private Member taggedMember;
        private GalleryItem taggedGalleryItem;
        private long galleryItemId;
        private long userId;
        private long ownerId; // person who submitted the tag
        private long tagId;
        private Point tagLocation;
        private bool tagApproved;

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
        public Member TaggedMember
        {
            get
            {
                return taggedMember;
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
        {
            this.core = core;

            taggedGalleryItem = galleryItem;

            SelectQuery query = new SelectQuery("user_tags ut");
            query.AddFields(TAG_INFO_FIELDS);
            query.AddCondition("tag_id", tagId);
            query.AddCondition("gallery_item_id", galleryItem.ItemId);

            DataTable tagDataTable = core.db.Query(query);

            if (tagDataTable.Rows.Count > 0)
            {
                loadTagInfo(tagDataTable.Rows[0]);
            }
            else
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
        {
            this.core = core;

            taggedGalleryItem = galleryItem;
            loadTagInfo(tagRow);
        }

        /// <summary>
        /// Loads the database information into the UserTag class object.
        /// </summary>
        /// <param name="tagRow">Raw data row of user tag</param>
        private void loadTagInfo(DataRow tagRow)
        {
            tagId = (long)tagRow["tag_id"];
            userId = (long)tagRow["tag_user_id"];
            taggedMember = core.UserProfiles[userId];
            tagLocation = new Point((int)tagRow["tag_x"], (int)tagRow["tag_y"]);
            galleryItemId = (long)tagRow["gallery_item_id"];
            tagApproved = ((byte)tagRow["tag_approved"] > 0) ? true : false;
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

            SelectQuery query = new SelectQuery("user_tags ut");
            query.AddFields(TAG_INFO_FIELDS);
            query.AddCondition("gallery_item_id", galleryItem.ItemId);

            DataTable tagDataTable = core.db.Query(query);

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

            if (core.db.Query(query) == 1)
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

            if (core.db.Query(query) == 1)
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
        public static UserTag Create(Core core, GalleryItem galleryItem, Member owner, Member member, Point location)
        {
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

            long tagId = core.db.Query(query);

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
                if (tag.TaggedMember.EmailNotifications)
                {
                    Template emailTemplate = new Template(HttpContext.Current.Server.MapPath("./templates/emails/"), "photo_tag_notification.eml");

                    emailTemplate.ParseVariables("TO_NAME", tag.TaggedMember.DisplayName);
                    emailTemplate.ParseVariables("FROM_NAME", core.session.LoggedInMember.DisplayName);
                    emailTemplate.ParseVariables("FROM_USERNAME", core.session.LoggedInMember.UserName);
                    emailTemplate.ParseVariables("U_PHOTO", "http://zinzam.com" + tag.TaggedGalleryItem.BuildUri());

                    Email.SendEmail(tag.TaggedMember.AlternateEmail, string.Format("{0} tagged you in a photo",
                        core.session.LoggedInMember.DisplayName),
                        emailTemplate.ToString());
                }
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
