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
    public class StatusMessage : NumberedItem, ICommentableItem, IPermissibleItem, ILikeableItem, ISearchableItem, IShareableItem, IActionableItem
    {
        [DataField("status_id", DataFieldKeys.Primary)]
        private long statusId;
        [DataField("user_id", DataFieldKeys.Index)]
        private long ownerId;
        [DataField("status_message", 1023)]
        private string statusMessage;
        [DataField("status_likes")]
        private byte likes;
        [DataField("status_dislikes")]
        private byte dislikes;
        [DataField("status_shares")]
        private long shares;
        [DataField("status_time_ut")]
        private long timeRaw;
        [DataField("status_ip", 50)]
        private string ip;
        [DataField("status_simple_permissions")]
        private bool simplePermissions;

        private User owner;
        private Access access;

        public event CommentHandler OnCommentPosted;

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

        public ItemKey OwnerKey
        {
            get
            {
                return new ItemKey(ownerId, ItemType.GetTypeId(typeof(User)));
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
                }
                return owner;
            }
        }

        public string Message
        {
            get
            {
                return statusMessage;
            }
        }

        public string ShareString
        {
            get
            {
                return core.Bbcode.FromStatusCode(Message);
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

        public StatusMessage(Core core, DataRow statusRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(StatusMessage_ItemLoad);

            loadItemInfo(statusRow);
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
            ItemUpdated += new EventHandler(StatusMessage_ItemUpdated);
            ItemDeleted += new ItemDeletedEventHandler(StatusMessage_ItemDeleted);
            OnCommentPosted += new CommentHandler(StatusMessage_CommentPosted);
        }

        bool StatusMessage_CommentPosted(CommentPostedEventArgs e)
        {
            if (Owner is User)
            {
                ApplicationEntry ae = core.GetApplication("Profile");
                ae.SendNotification(core, (User)Owner, e.Comment.ItemKey, string.Format("[user]{0}[/user] commented on your status.", e.Poster.Id), string.Format("[quote=\"[iurl={0}]{1}[/iurl]\"]{2}[/quote]",
                    e.Comment.BuildUri(this), e.Poster.DisplayName, e.Comment.Body));
            }

            return true;
        }

        public void CommentPosted(CommentPostedEventArgs e)
        {
            if (OnCommentPosted != null)
            {
                OnCommentPosted(e);
            }
        }

        void StatusMessage_ItemUpdated(object sender, EventArgs e)
        {
            core.Search.UpdateIndex(this);
        }

        void StatusMessage_ItemDeleted(object sender, ItemDeletedEventArgs e)
        {
            core.Search.DeleteFromIndex(this);
            ActionableItem.CleanUp(core, this);
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
            iQuery.AddField("status_ip", core.Session.IPAddress.ToString());
            iQuery.AddField("status_time_ut", UnixTime.UnixTimeStamp());

            long statusId = core.Db.Query(iQuery);

            UpdateQuery uQuery = new UpdateQuery("user_info");
            uQuery.AddField("user_status_messages", new QueryOperation("user_status_messages", QueryOperations.Addition, 1));
            uQuery.AddCondition("user_id", creator.Id);

            core.Db.Query(uQuery);

            StatusMessage newStatusMessage = new StatusMessage(core, creator, statusId, message);

            core.Search.Index(newStatusMessage);

            return newStatusMessage;
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

            e.Template.Parse("U_PROFILE", e.Page.User.ProfileUri);
            e.Template.Parse("USER_COVER_PHOTO", e.Page.User.CoverPhoto);
            e.Template.Parse("USER_MOBILE_COVER_PHOTO", ((User)e.Page.Owner).MobileCoverPhoto);

            try
            {
                StatusMessage item = new StatusMessage(e.Core, e.ItemId);

                VariableCollection statusMessageVariableCollection = e.Core.Template.CreateChild("status_messages");

                e.Core.Meta.Add("twitter:card", "summary");
                if (!string.IsNullOrEmpty(e.Core.Settings.TwitterName))
                {
                    e.Core.Meta.Add("twitter:site", e.Core.Settings.TwitterName);
                }
                if (item.Owner is User && !string.IsNullOrEmpty(((User)item.Owner).UserInfo.TwitterUserName))
                {
                    e.Core.Meta.Add("twitter:creator", ((User)item.Owner).UserInfo.TwitterUserName);
                }
                e.Core.Meta.Add("twitter:title", BoxSocial.Internals.Action.GetTitle(item.Owner.DisplayName, item.Action));
                e.Core.Meta.Add("og:title", BoxSocial.Internals.Action.GetTitle(item.Owner.DisplayName, item.Action));
                e.Core.Meta.Add("twitter:description", Functions.TrimStringToWord(HttpUtility.HtmlDecode(e.Core.Bbcode.StripTags(HttpUtility.HtmlEncode(item.Message))).Trim(), 256, true));
                e.Core.Meta.Add("og:type", "article");
                e.Core.Meta.Add("og:url", e.Core.Hyperlink.StripSid(e.Core.Hyperlink.AppendCurrentSid(item.Uri)));
                e.Core.Meta.Add("og:description", Functions.TrimStringToWord(HttpUtility.HtmlDecode(e.Core.Bbcode.StripTags(HttpUtility.HtmlEncode(item.Message))).Trim(), 256, true));

                e.Page.CanonicalUri = item.Uri;

                //statusMessageVariableCollection.Parse("STATUS_MESSAGE", item.Message);
                e.Core.Display.ParseBbcode(statusMessageVariableCollection, "STATUS_MESSAGE", e.Core.Bbcode.FromStatusCode(item.Message), e.Page.Owner, true, string.Empty, string.Empty);
                statusMessageVariableCollection.Parse("STATUS_UPDATED", e.Core.Tz.DateTimeToString(item.GetTime(e.Core.Tz)));

                statusMessageVariableCollection.Parse("ID", item.Id.ToString());
                statusMessageVariableCollection.Parse("TYPE_ID", item.ItemKey.TypeId.ToString());
                statusMessageVariableCollection.Parse("USERNAME", item.Poster.DisplayName);
                statusMessageVariableCollection.Parse("USER_ID", item.Poster.Id);
                statusMessageVariableCollection.Parse("U_PROFILE", item.Poster.ProfileUri);
                statusMessageVariableCollection.Parse("U_QUOTE", string.Empty /*e.Core.Hyperlink.BuildCommentQuoteUri(item.Id)*/);
                statusMessageVariableCollection.Parse("U_REPORT", string.Empty /*e.Core.Hyperlink.BuildCommentReportUri(item.Id)*/);
                statusMessageVariableCollection.Parse("U_DELETE", string.Empty /*e.Core.Hyperlink.BuildCommentDeleteUri(item.Id)*/);
                statusMessageVariableCollection.Parse("USER_TILE", item.Poster.Tile);
                statusMessageVariableCollection.Parse("USER_ICON", item.Poster.Icon);

                if (e.Core.Session.IsLoggedIn)
                {
                    if (item.Owner.Id == e.Core.Session.LoggedInMember.Id)
                    {
                        statusMessageVariableCollection.Parse("IS_OWNER", "TRUE");
                    }
                }

                if (item.Likes > 0)
                {
                    statusMessageVariableCollection.Parse("LIKES", string.Format(" {0:d}", item.Likes));
                    statusMessageVariableCollection.Parse("DISLIKES", string.Format(" {0:d}", item.Dislikes));
                }

                if (item.Info.Comments > 0)
                {
                    statusMessageVariableCollection.Parse("COMMENTS", string.Format(" ({0:d})", item.Info.Comments));
                }

                if (item.Access.IsPublic())
                {
                    statusMessageVariableCollection.Parse("IS_PUBLIC", "TRUE");
                    if (item.ItemKey.ImplementsShareable)
                    {
                        statusMessageVariableCollection.Parse("SHAREABLE", "TRUE");
                        statusMessageVariableCollection.Parse("U_SHARE", item.ShareUri);

                        if (item.Info.SharedTimes > 0)
                        {
                            statusMessageVariableCollection.Parse("SHARES", string.Format(" {0:d}", item.Info.SharedTimes));
                        }
                    }
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
                return core.Hyperlink.AppendSid(string.Format("{0}status-feed/{1}",
                        Owner.UriStub, Id));
            }
        }

        public string ShareUri
        {
            get
            {
                return core.Hyperlink.AppendAbsoluteSid(string.Format("/share?item={0}&type={1}", ItemKey.Id, ItemKey.TypeId), true);
            }
        }

        public long Comments
        {
            get
            {
                return Info.Comments;
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

        public bool IsItemGroupMember(ItemKey viewer, ItemKey key)
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

        public bool GetDefaultCan(string permission, ItemKey viewer)
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

        public long SharedTimes
        {
            get
            {
                return shares;
            }
        }

        public string IndexingString
        {
            get
            {
                return Message;
            }
        }

        public string IndexingTitle
        {
            get
            {
                return string.Empty;
            }
        }

        public string IndexingTags
        {
            get
            {
                return string.Empty;
            }
        }

        public Template RenderPreview()
        {
            Template template = new Template("search_result.statusmessage.html");
            template.Medium = core.Template.Medium;
            template.SetProse(core.Prose);

            VariableCollection statusMessageVariableCollection = template.CreateChild("status_messages");

            //statusMessageVariableCollection.Parse("STATUS_MESSAGE", item.Message);
            core.Display.ParseBbcode(statusMessageVariableCollection, "STATUS_MESSAGE", core.Bbcode.FromStatusCode(Message), owner, true, string.Empty, string.Empty);
            statusMessageVariableCollection.Parse("STATUS_UPDATED", core.Tz.DateTimeToString(GetTime(core.Tz)));

            statusMessageVariableCollection.Parse("ID", Id.ToString());
            statusMessageVariableCollection.Parse("TYPE_ID", ItemKey.TypeId.ToString());
            statusMessageVariableCollection.Parse("USERNAME", Poster.DisplayName);
            statusMessageVariableCollection.Parse("USER_ID", Poster.Id);
            statusMessageVariableCollection.Parse("U_PROFILE", Poster.ProfileUri);
            statusMessageVariableCollection.Parse("U_QUOTE", string.Empty /*core.Hyperlink.BuildCommentQuoteUri(Id)*/);
            statusMessageVariableCollection.Parse("U_REPORT", string.Empty /*core.Hyperlink.BuildCommentReportUri(Id)*/);
            statusMessageVariableCollection.Parse("U_DELETE", string.Empty /*core.Hyperlink.BuildCommentDeleteUri(Id)*/);
            statusMessageVariableCollection.Parse("U_PERMISSIONS", Access.AclUri);
            statusMessageVariableCollection.Parse("USER_TILE", Poster.Tile);
            statusMessageVariableCollection.Parse("USER_ICON", Poster.Icon);
            statusMessageVariableCollection.Parse("URI", Uri);

            if (core.Session.IsLoggedIn)
            {
                if (Owner.Id == core.Session.LoggedInMember.Id)
                {
                    statusMessageVariableCollection.Parse("IS_OWNER", "TRUE");
                }
            }

            if (Info.Likes > 0)
            {
                statusMessageVariableCollection.Parse("LIKES", string.Format(" {0:d}", Info.Likes));
                statusMessageVariableCollection.Parse("DISLIKES", string.Format(" {0:d}", Info.Dislikes));
            }

            if (Info.Comments > 0)
            {
                statusMessageVariableCollection.Parse("COMMENTS", string.Format(" ({0:d})", Info.Comments));
            }

            return template;
        }


        public string Action
        {
            get
            {
                return core.Prose.GetString("_POSTED");
            }
        }

        public string GetActionBody(List<ItemKey> subItems)
        {
            return ShareString;
        }

        public string Noun
        {
            get
            {
                return core.Prose.GetString("_STATUS");
            }
        }


        public ActionableItemType PostType
        {
            get
            {
                return ActionableItemType.Text;
            }
        }

        public byte[] Data
        {
            get
            {
                return null;
            }
        }

        public string DataContentType
        {
            get
            {
                return null;
            }
        }

        public string Caption
        {
            get
            {
                return null;
            }
        }
    }

    public class InvalidStatusMessageException : InvalidItemException
    {
    }
}
