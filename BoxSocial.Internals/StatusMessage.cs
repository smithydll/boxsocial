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
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Web;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    [DataTable("user_status_messages")]
    [Permission("VIEW", "Can view this status message", PermissionTypes.View)]
    [Permission("COMMENT", "Can comment on this status message", PermissionTypes.Interact)]
    public class StatusMessage : NumberedItem, ICommentableItem, IPermissibleItem, ILikeableItem
    {
        [DataField("status_id", DataFieldKeys.Primary)]
        private long statusId;
        [DataField("user_id", DataFieldKeys.Index)]
        private long ownerId;
        [DataField("status_message", 255)]
        private string statusMessage;
        [DataField("comments")]
        private long comments;
        [DataField("status_likes")]
        private byte likes;
        [DataField("status_dislikes")]
        private byte dislikes;
        [DataField("status_time_ut")]
        private long timeRaw;
        [DataField("status_simple_permissions")]
        private bool simplePermissions;

        private User owner;
        private Access access;

        public long StatusId
        {
            get
            {
                return statusId;
            }
        }

        public User Poster
        {
            get
            {
                return (User)Owner;
            }
        }

        public Primitive Owner
        {
            get
            {
                if (owner == null || ownerId != owner.Id)
                {
                    core.LoadUserProfile(ownerId);
                    owner = core.PrimitiveCache[ownerId];
                    return owner;
                }
                else
                {
                    return owner;
                }
            }
        }

        public string Message
        {
            get
            {
                return statusMessage;
            }
        }

        public long TimeRaw
        {
            get
            {
                return timeRaw;
            }
        }

        public DateTime GetTime(UnixTime tz)
        {
            return tz.DateTimeFromMysql(timeRaw);
        }

        public StatusMessage(Core core, long statusId)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(StatusMessage_ItemLoad);

            try
            {
                LoadItem(statusId);
            }
            catch (InvalidItemException)
            {
                throw new InvalidStatusMessageException();
            }
        }

        public StatusMessage(Core core, User owner, DataRow statusRow)
            : base(core)
        {
            this.owner = owner;
            ItemLoad += new ItemLoadHandler(StatusMessage_ItemLoad);

            loadItemInfo(statusRow);
        }

        private StatusMessage(Core core, User owner, long statusId, string statusMessage)
            : base(core)
        {
            this.owner = owner;
            this.ownerId = owner.Id;
            this.statusId = statusId;
            this.statusMessage = statusMessage;
        }

        private void StatusMessage_ItemLoad()
        {
        }

        public static StatusMessage Create(Core core, User creator, string message)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            InsertQuery iQuery = new InsertQuery("user_status_messages");
            iQuery.AddField("user_id", creator.Id);
            iQuery.AddField("status_message", message);
            iQuery.AddField("status_time_ut", UnixTime.UnixTimeStamp());

            long statusId = core.Db.Query(iQuery);

            UpdateQuery uQuery = new UpdateQuery("user_info");
            uQuery.AddField("user_status_messages", new QueryOperation("user_status_messages", QueryOperations.Addition, 1));
            uQuery.AddCondition("user_id", creator.Id);

            core.Db.Query(uQuery);

            return new StatusMessage(core, creator, statusId, message);
        }

        public static void Show(object sender, ShowUPageEventArgs e)
        {
            if (!e.Page.Owner.Access.Can("VIEW"))
            {
                e.Core.Functions.Generate403();
                return;
            }

            e.Template.SetTemplate("Profile", "viewstatusfeed");

            if (e.Core.Session.IsLoggedIn && e.Page.Owner == e.Core.Session.LoggedInMember)
            {
                e.Core.Template.Parse("OWNER", "TRUE");
            }

            try
            {
                StatusMessage item = new StatusMessage(e.Core, e.ItemId);

                VariableCollection statusMessageVariableCollection = e.Core.Template.CreateChild("status_messages");

                //statusMessageVariableCollection.Parse("STATUS_MESSAGE", item.Message);
                e.Core.Display.ParseBbcode(statusMessageVariableCollection, "STATUS_MESSAGE", e.Core.Bbcode.FromStatusCode(item.Message), e.Page.Owner);
                statusMessageVariableCollection.Parse("STATUS_UPDATED", e.Core.Tz.DateTimeToString(item.GetTime(e.Core.Tz)));

                statusMessageVariableCollection.Parse("ID", item.Id.ToString());
                statusMessageVariableCollection.Parse("TYPE_ID", item.ItemKey.TypeId.ToString());
                statusMessageVariableCollection.Parse("USERNAME", item.Poster.DisplayName);
                statusMessageVariableCollection.Parse("U_PROFILE", item.Poster.ProfileUri);
                statusMessageVariableCollection.Parse("U_QUOTE", e.Core.Uri.BuildCommentQuoteUri(item.Id));
                statusMessageVariableCollection.Parse("U_REPORT", e.Core.Uri.BuildCommentReportUri(item.Id));
                statusMessageVariableCollection.Parse("U_DELETE", e.Core.Uri.BuildCommentDeleteUri(item.Id));
                statusMessageVariableCollection.Parse("USER_TILE", item.Poster.UserTile);

                if (item.Likes > 0)
                {
                    statusMessageVariableCollection.Parse("LIKES", string.Format(" {0:d}", item.Likes));
                    statusMessageVariableCollection.Parse("DISLIKES", string.Format(" {0:d}", item.Dislikes));
                }

                /* pages */
                e.Core.Display.ParsePageList(e.Page.Owner, true);

                List<string[]> breadCrumbParts = new List<string[]>();

                breadCrumbParts.Add(new string[] { "*profile", "Profile" });
                breadCrumbParts.Add(new string[] { "status-feed", "Status Feed" });
                breadCrumbParts.Add(new string[] { item.Id.ToString(), "Status" });

                e.Page.Owner.ParseBreadCrumbs(breadCrumbParts);
            }
            catch (InvalidStatusMessageException)
            {
                e.Core.Functions.Generate404();
                return;
            }
        }

        public override long Id
        {
            get
            {
                return statusId;
            }
        }

        public override string Uri
        {
            get
            {
                return core.Uri.AppendSid(string.Format("{0}status-feed/{1}",
                        Owner.UriStub, Id));
            }
        }

        public long Comments
        {
            get
            {
                return comments;
            }
        }

        public SortOrder CommentSortOrder
        {
            get
            {
                return SortOrder.Ascending;
            }
        }

        public byte CommentsPerPage
        {
            get
            {
                return 10;
            }
        }

        public Access Access
        {
            get
            {
                if (access == null)
                {
                    access = new Access(core, this);
                }
                return access;
            }
        }

        public bool IsSimplePermissions
        {
            get
            {
                return simplePermissions;
            }
            set
            {
                SetPropertyByRef(new { simplePermissions }, value);
            }
        }

        public List<AccessControlPermission> AclPermissions
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsItemGroupMember(User viewer, ItemKey key)
        {
            return false;
        }

        public IPermissibleItem PermissiveParent
        {
            get
            {
                return Owner;
            }
        }

        public ItemKey PermissiveParentKey
        {
            get
            {
                return new ItemKey(ownerId, typeof(User));
            }
        }

        public string DisplayTitle
        {
            get
            {
                return "Message: " + Message;
            }
        }

        public bool GetDefaultCan(string permission)
        {
            return false;
        }

        public string ParentPermissionKey(Type parentType, string permission)
        {
            switch (permission)
            {
                case "DELETE":
                    return "DELETE_STATUS";
                default:
                    return permission;
            }
        }

        public long Likes
        {
            get
            {
                return likes;
            }
        }

        public long Dislikes
        {
            get
            {
                return dislikes;
            }
        }
    }

    public class InvalidStatusMessageException : Exception
    {
    }
}
