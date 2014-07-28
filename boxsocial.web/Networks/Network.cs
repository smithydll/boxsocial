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
using System.Data;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Networks
{
    public enum NetworkTypes
    {
        Global,
        Country,
        University,
        School,
        Workplace
    }

    public enum NetworkLoadOptions : byte
    {
        Key = 0x01,
        Info = Key | 0x02,
        Icon = Key | 0x08,
        Common = Key | Info,
        All = Key | Info | Icon,
    }

    /// <summary>
    /// 
    /// </summary>
    [DataTable("network_keys", "NETWORK")]
    [Primitive("NETWORK", NetworkLoadOptions.All, "network_id", "network_network")]
    [Permission("COMMENT", "Can write on the guest book", PermissionTypes.Interact)]
    public class Network : Primitive, ICommentableItem, IPermissibleItem
    {
        [DataField("network_id", DataFieldKeys.Primary)]
        private long networkId;
        [DataField("network_network", DataFieldKeys.Unique, 24)]
        private string networkNetwork;
        [DataField("network_simple_permissions")]
        private bool simplePermissions;

        private NetworkInfo networkInfo;
        private Access access;

        private Dictionary<ItemKey, bool> networkMemberCache = new Dictionary<ItemKey, bool>();

        public event CommentHandler OnCommentPosted;

        public long NetworkId
        {
            get
            {
                return networkId;
            }
        }

        public NetworkInfo NetworkInfo
        {
            get
            {
                return networkInfo;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override long Id
        {
            get
            {
                return NetworkId;
            }
        }

        public override string Type
        {
            get
            {
                return "NETWORK";
            }
        }

        public override AppPrimitives AppPrimitive
        {
            get
            {
                return AppPrimitives.Network;
            }
        }

        public string NetworkNetwork
        {
            get
            {
                return networkNetwork;
            }
        }

        public override string Key
        {
            get
            {
                return networkNetwork;
            }
        }

        public override string DisplayName
        {
            get
            {
                return networkInfo.DisplayName;
            }
        }

        public override string DisplayNameOwnership
        {
            get
            {
                return networkInfo.DisplayNameOwnership;
            }
        }

        public override string TitleNameOwnership
        {
            get
            {
                return "the network " + DisplayNameOwnership;
            }
        }

        public override string TitleName
        {
            get
            {
                return "the network " + DisplayName;
            }
        }

        public string Description
        {
            get
            {
                return networkInfo.Description;
            }
        }

        public long Members
        {
            get
            {
                return networkInfo.Members;
            }
        }

        public long Comments
        {
            get
            {
                return Info.Comments;
            }
        }

        public bool RequireConfirmation
        {
            get
            {
                return networkInfo.RequireConfirmation;
            }
        }

        public NetworkTypes NetworkType
        {
            get
            {
                return networkInfo.NetworkType;
            }
        }

        public long GalleryItems
        {
            get
            {
                return networkInfo.GalleryItems;
            }
        }

        public Network(Core core, long networkId)
            : this(core, networkId, NetworkLoadOptions.Info | NetworkLoadOptions.Icon)
        {
        }

        public Network(Core core, long networkId, NetworkLoadOptions loadOptions)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(Network_ItemLoad);

            bool containsInfoData = false;
            bool containsIconData = false;

            if (loadOptions == NetworkLoadOptions.Key)
            {
                try
                {
                    LoadItem(networkId);
                }
                catch (InvalidItemException)
                {
                    throw new InvalidNetworkException();
                }
            }
            else
            {
                SelectQuery query = new SelectQuery(Network.GetTable(typeof(Network)));
                query.AddFields(Network.GetFieldsPrefixed(typeof(Network)));
                query.AddCondition("`network_keys`.`network_id`", networkId);

                if ((loadOptions & NetworkLoadOptions.Info) == NetworkLoadOptions.Info)
                {
                    containsInfoData = true;

                    query.AddJoin(JoinTypes.Inner, NetworkInfo.GetTable(typeof(NetworkInfo)), "network_id", "network_id");
                    query.AddFields(NetworkInfo.GetFieldsPrefixed(typeof(NetworkInfo)));

                    if ((loadOptions & NetworkLoadOptions.Icon) == NetworkLoadOptions.Icon)
                    {
                        // TODO: Network Icon
                        /*containsIconData = true;

                        query.AddJoin(JoinTypes.Left, new DataField("network_info", "network_icon"), new DataField("gallery_items", "gallery_item_id"));
                        query.AddField(new DataField("gallery_items", "gallery_item_uri"));
                        query.AddField(new DataField("gallery_items", "gallery_item_parent_path"));*/
                    }
                }

                DataTable networkTable = db.Query(query);

                if (networkTable.Rows.Count > 0)
                {
                    loadItemInfo(typeof(Network), networkTable.Rows[0]);

                    if (containsInfoData)
                    {
                        networkInfo = new NetworkInfo(core, networkTable.Rows[0]);
                    }

                    if (containsIconData)
                    {
                        // TODO: Network Icon
                        //loadNetworkIcon(networkTable.Rows[0]);
                    }
                }
                else
                {
                    throw new InvalidNetworkException();
                }
            }
        }

        public Network(Core core, string networkNetwork)
            : this(core, networkNetwork, NetworkLoadOptions.Info | NetworkLoadOptions.Icon)
        {
        }

        public Network(Core core, string networkNetwork, NetworkLoadOptions loadOptions)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(Network_ItemLoad);

            bool containsInfoData = false;
            bool containsIconData = false;

            if (loadOptions == NetworkLoadOptions.Key)
            {
                try
                {
                    LoadItem("network_network", networkNetwork);
                }
                catch (InvalidItemException)
                {
                    throw new InvalidNetworkException();
                }
            }
            else
            {
                SelectQuery query = new SelectQuery(Network.GetTable(typeof(Network)));
                query.AddFields(Network.GetFieldsPrefixed(typeof(Network)));
                query.AddCondition("`network_keys`.`network_network`", networkNetwork);

                if ((loadOptions & NetworkLoadOptions.Info) == NetworkLoadOptions.Info)
                {
                    containsInfoData = true;

                    query.AddJoin(JoinTypes.Inner, NetworkInfo.GetTable(typeof(NetworkInfo)), "network_id", "network_id");
                    query.AddFields(NetworkInfo.GetFieldsPrefixed(typeof(NetworkInfo)));

                    if ((loadOptions & NetworkLoadOptions.Icon) == NetworkLoadOptions.Icon)
                    {
                        // TODO: Network Icon
                        /*containsIconData = true;

                        query.AddJoin(JoinTypes.Left, new DataField("network_info", "network_icon"), new DataField("gallery_items", "gallery_item_id"));
                        query.AddField(new DataField("gallery_items", "gallery_item_uri"));
                        query.AddField(new DataField("gallery_items", "gallery_item_parent_path"));*/
                    }
                }

                DataTable networkTable = db.Query(query);

                if (networkTable.Rows.Count > 0)
                {
                    loadItemInfo(typeof(Network), networkTable.Rows[0]);

                    if (containsInfoData)
                    {
                        networkInfo = new NetworkInfo(core, networkTable.Rows[0]);
                    }

                    if (containsIconData)
                    {
                        // TODO: Network Icon
                        //loadUserGroupIcon(networkTable.Rows[0]);
                    }
                }
                else
                {
                    throw new InvalidNetworkException();
                }
            }
        }

        public Network(Core core, DataRow networkRow, NetworkLoadOptions loadOptions)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(Network_ItemLoad);

            if (networkRow != null)
            {
                loadItemInfo(typeof(Network), networkRow);

                if ((loadOptions & NetworkLoadOptions.Info) == NetworkLoadOptions.Info)
                {
                    networkInfo = new NetworkInfo(core, networkRow);
                }

                if ((loadOptions & NetworkLoadOptions.Icon) == NetworkLoadOptions.Icon)
                {
                    // TODO: Network Icon
                    //loadUserGroupIcon(groupRow);
                }
            }
            else
            {
                throw new InvalidNetworkException();
            }
        }

        void Network_ItemLoad()
        {
            OnCommentPosted += new CommentHandler(Network_CommentPosted);
        }

        bool Network_CommentPosted(CommentPostedEventArgs e)
        {
            return true;
        }

        public void CommentPosted(CommentPostedEventArgs e)
        {
            if (OnCommentPosted != null)
            {
                OnCommentPosted(e);
            }
        }

        public List<NetworkMember> GetMembers(int page, int perPage)
        {
            return GetMembers(page, perPage, null);
        }

        public List<NetworkMember> GetMembers(int page, int perPage, string filter)
        {
            List<NetworkMember> members = new List<NetworkMember>();

            SelectQuery query = new SelectQuery(NetworkMember.GetTable(typeof(NetworkMember)));
            query.AddFields(NetworkMember.GetFieldsPrefixed(typeof(NetworkMember)));
            query.AddCondition("network_id", networkId);
            query.AddSort(SortOrder.Ascending, "member_join_date_ut");
            if (!string.IsNullOrEmpty(filter))
            {
                query.AddCondition(new DataField("user_keys", "user_name_first"), filter[0]);
            }
            query.LimitStart = (page - 1) * perPage;
            query.LimitCount = perPage;

            DataTable membersTable = db.Query(query);

            List<long> memberIds = new List<long>();

            foreach (DataRow dr in membersTable.Rows)
            {
                memberIds.Add((long)dr["user_id"]);
            }

            core.LoadUserProfiles(memberIds);

            foreach (DataRow dr in membersTable.Rows)
            {
                members.Add(new NetworkMember(core, dr));
            }

            return members;
        }

        public static List<NetworkMember> GetNetworkMemberships(Core core, User member)
        {
            List<NetworkMember> memberships = new List<NetworkMember>();

            SelectQuery query = NetworkMember.GetSelectQueryStub(UserLoadOptions.Key);
            query.AddCondition("user_id", member.Id);

            DataTable membershipsTable = core.Db.Query(query);

            foreach (DataRow dr in membershipsTable.Rows)
            {
                memberships.Add(new NetworkMember(core, dr, UserLoadOptions.Key));
            }

            return memberships;
        }

        public static List<Network> GetUserNetworks(Core core, User member)
        {
            List<Network> networks = new List<Network>();

            SelectQuery query = Network.GetSelectQueryStub(NetworkLoadOptions.All);
            query.AddJoin(JoinTypes.Inner, new DataField(typeof(Network), "network_id"), new DataField(typeof(NetworkMember), "network_id"));
            query.AddCondition("user_id", member.Id);

            DataTable networksTable = core.Db.Query(query);

            foreach (DataRow dr in networksTable.Rows)
            {
                networks.Add(new Network(core, dr, NetworkLoadOptions.Common));
            }

            return networks;
        }

        public static List<Network> GetNetworks(Core core, NetworkTypes type)
        {
            List<Network> networks = new List<Network>();

            string typeString = "";
            switch (type)
            {
                case NetworkTypes.Country:
                    typeString = "COUNTRY";
                    break;
                case NetworkTypes.Global:
                    typeString = "GLOBAL";
                    break;
                case NetworkTypes.School:
                    typeString = "SCHOOL";
                    break;
                case NetworkTypes.University:
                    typeString = "UNIVERSITY";
                    break;
                case NetworkTypes.Workplace:
                    typeString = "WORKPLACE";
                    break;
            }

            SelectQuery query = Network.GetSelectQueryStub(NetworkLoadOptions.All);
            query.AddCondition("network_type", typeString);

            DataTable networksTable = core.Db.Query(query);

            foreach (DataRow dr in networksTable.Rows)
            {
                networks.Add(new Network(core, dr, NetworkLoadOptions.Common));
            }

            return networks;
        }

        public bool IsNetworkMember(ItemKey key)
        {
            if (key != null)
            {
                if (networkMemberCache.ContainsKey(key))
                {
                    return networkMemberCache[key];
                }
                else
                {
                    DataTable memberTable = db.Query(string.Format("SELECT user_id FROM network_members WHERE network_id = {0} AND user_id = {1} AND member_active = 1",
                        networkId, key.Id));

                    if (memberTable.Rows.Count > 0)
                    {
                        networkMemberCache.Add(key, true);
                        return true;
                    }
                    else
                    {
                        networkMemberCache.Add(key, false);
                        return false;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// returns true on success
        /// </summary>
        /// <param name="page"></param>
        /// <param name="member"></param>
        /// <returns></returns>
        public bool Activate(TPage page, User member, string activateKey)
        {
            long rowsChanged = db.UpdateQuery(string.Format("UPDATE network_members SET member_active = 1 WHERE network_id = {0} AND user_id = {1} AND member_activate_code = '{2}' AND member_active = 0;",
                networkId, member.UserId, activateKey));

            db.UpdateQuery(string.Format("UPDATE network_info SET network_members = network_members + {1} WHERE network_id = {0}",
                networkId, rowsChanged));

            if (rowsChanged == 1)
            {
                networkMemberCache.Add(member.ItemKey, true);
                return true;
            }
            else
            {
                return false;
            }
        }

        public void ResendConfirmationKey(Core core, NetworkMember member)
        {
            string activateKey = member.MemberActivationCode;

            int isActive = (networkInfo.RequireConfirmation) ? 0 : 1;

            string activateUri = string.Format("http://zinzam.com/network/{0}?mode=activate&id={1}&key={2}",
                networkNetwork, member.UserId, activateKey);

            if (networkInfo.RequireConfirmation)
            {
                Template emailTemplate = new Template(core.Http.TemplateEmailPath, "join_network.html");

                emailTemplate.Parse("SITE_TITLE", core.Settings.SiteTitle);
                emailTemplate.Parse("U_SITE", core.Hyperlink.StripSid(core.Hyperlink.AppendAbsoluteSid(core.Hyperlink.BuildHomeUri())));
                emailTemplate.Parse("TO_NAME", member.DisplayName);
                emailTemplate.Parse("U_ACTIVATE", activateUri);
                emailTemplate.Parse("S_EMAIL", member.MemberEmail);

                core.Email.SendEmail(member.MemberEmail, core.Settings.SiteTitle + " Network Registration Confirmation", emailTemplate);
            }
        }

        public static SelectQuery GetSelectQueryStub(NetworkLoadOptions loadOptions)
        {
            SelectQuery query = new SelectQuery(Network.GetTable(typeof(Network)));
            query.AddFields(Network.GetFieldsPrefixed(typeof(Network)));

            if ((loadOptions & NetworkLoadOptions.Info) == NetworkLoadOptions.Info)
            {
                query.AddJoin(JoinTypes.Inner, NetworkInfo.GetTable(typeof(NetworkInfo)), "network_id", "network_id");
                query.AddFields(NetworkInfo.GetFieldsPrefixed(typeof(NetworkInfo)));

                if ((loadOptions & NetworkLoadOptions.Icon) == NetworkLoadOptions.Icon)
                {
                    // TODO: Network Icon
                    /*containsIconData = true;

                    query.AddJoin(JoinTypes.Left, new DataField("network_info", "network_icon"), new DataField("gallery_items", "gallery_item_id"));
                    query.AddField(new DataField("gallery_items", "gallery_item_uri"));
                    query.AddField(new DataField("gallery_items", "gallery_item_parent_path"));*/
                }
            }

            return query;
        }

        public static SelectQuery Network_GetSelectQueryStub()
        {
            return GetSelectQueryStub(NetworkLoadOptions.All);
        }

        public NetworkMember Join(Core core, User member, string networkEmail)
        {
            string activateKey = User.GenerateActivationSecurityToken();

            if (!IsValidNetworkEmail(networkEmail) && networkInfo.RequireConfirmation)
            {
                return null;
            }

            if (IsNetworkMember(member.ItemKey))
            {
                return null;
            }

            int isActive = (networkInfo.RequireConfirmation) ? 0 : 1;

            // delete any existing unactivated e-mails for this user in this network, re-send the invitation
            db.BeginTransaction();

            try
            {
                NetworkMember nm = new NetworkMember(core, this, member);

                if (!nm.IsMemberActive)
                {
                    try
                    {
                        UserEmail uMail = new UserEmail(core, nm.MemberEmail);
                        uMail.Delete();
                    }
                    catch (InvalidUserEmailException)
                    {
                        // Do Nothing
                    }
                    nm.Delete();
                }
            }
            catch (InvalidUserException)
            {
                // Do Nothing
            }

            if (!networkInfo.RequireConfirmation)
            {
                UpdateQuery uQuery = new UpdateQuery(GetTable(typeof(Network)));
                uQuery.AddField("network_members", new QueryOperation("network_members", QueryOperations.Addition, 1));
                uQuery.AddCondition("network_id", networkId);

                db.Query(uQuery);
            }

            InsertQuery iQuery = new InsertQuery(GetTable(typeof(NetworkMember)));
            iQuery.AddField("network_id", this.Id);
            iQuery.AddField("user_id", member.UserId);
            iQuery.AddField("member_join_date_ut", UnixTime.UnixTimeStamp());
            iQuery.AddField("member_join_ip", core.Session.IPAddress.ToString());
            iQuery.AddField("member_email", networkEmail);
            iQuery.AddField("member_active", isActive);
            iQuery.AddField("member_activate_code", activateKey);

            db.Query(iQuery);

            NetworkMember newMember = new NetworkMember(core, this, member);
            string activateUri = string.Format("http://zinzam.com/network/{0}?mode=activate&id={1}&key={2}",
                networkNetwork, member.UserId, activateKey);

            if (networkInfo.RequireConfirmation)
            {
                EmailAddressTypes emailType = EmailAddressTypes.Other;

                switch (networkInfo.NetworkType)
                {
                    case NetworkTypes.School:
                    case NetworkTypes.University:
                        emailType = EmailAddressTypes.Student;
                        break;
                    case NetworkTypes.Workplace:
                        emailType = EmailAddressTypes.Business;
                        break;
                }

                UserEmail registrationEmail = UserEmail.Create(core, newMember, networkEmail, emailType, true);

                Template emailTemplate = new Template(core.Http.TemplateEmailPath, "join_network.html");

                emailTemplate.Parse("SITE_TITLE", core.Settings.SiteTitle);
                emailTemplate.Parse("U_SITE", core.Hyperlink.StripSid(core.Hyperlink.AppendAbsoluteSid(core.Hyperlink.BuildHomeUri())));
                emailTemplate.Parse("TO_NAME", member.DisplayName);
                emailTemplate.Parse("U_ACTIVATE", activateUri);
                emailTemplate.Parse("S_EMAIL", networkEmail);

                core.Email.SendEmail(networkEmail, core.Settings.SiteTitle + " Network Registration Confirmation", emailTemplate);
            }

            return newMember;
        }

        public bool IsValidNetworkEmail(string networkEmail)
        {
            if (User.CheckEmailValid(networkEmail))
            {
                if (networkEmail.ToLower().EndsWith(networkNetwork.ToString()))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public override bool CanModerateComments(User member)
        {
            return false;
        }

        public override bool IsItemOwner(User member)
        {
            return false;
        }

        public void GetCan(ushort accessBits, User viewer, out bool canRead, out bool canComment, out bool canCreate, out bool canChange)
        {
            bool isNetworkMember = IsNetworkMember(viewer.ItemKey);
            switch (NetworkType)
            {
                case NetworkTypes.Country:
                case NetworkTypes.Global:
                    if (isNetworkMember)
                    {
                        canRead = true;
                        canComment = true;
                        canCreate = true;
                        canChange = false;
                    }
                    else
                    {
                        canRead = true;
                        canComment = false;
                        canCreate = false;
                        canChange = false;
                    }
                    break;
                case NetworkTypes.University:
                case NetworkTypes.School:
                case NetworkTypes.Workplace:
                    if (isNetworkMember)
                    {
                        canRead = true;
                        canComment = true;
                        canCreate = true;
                        canChange = false;
                    }
                    else
                    {
                        canRead = false;
                        canComment = false;
                        canCreate = false;
                        canChange = false;
                    }
                    break;
                default:
                    canRead = false;
                    canComment = false;
                    canCreate = false;
                    canChange = false;
                    break;
            }
        }

        public override string GenerateBreadCrumbs(List<string[]> parts)
        {
            string output = "";
            string path = string.Format("/network/{0}", NetworkNetwork);
            output = string.Format("<a href=\"{1}\">{0}</a>",
                    DisplayName, core.Hyperlink.AppendSid(path));

            for (int i = 0; i < parts.Count; i++)
            {
                if (parts[i][0] != "")
                {
                    output += string.Format(" <strong>&#8249;</strong> <a href=\"{1}\">{0}</a>",
                        parts[i][1], core.Hyperlink.AppendSid(path + "/" + parts[i][0].TrimStart(new char[] { '*', '!' })));
                    if (!parts[i][0].StartsWith("*"))
                    {
                        path += "/" + parts[i][0];
                    }
                }
            }

            return output;
        }

        public override string UriStub
        {
            get
            {
                if (core.Http.Domain != Hyperlink.Domain)
                {
                    return core.Hyperlink.Uri + "network/" + NetworkNetwork + "/";
                }
                else
                {
                    return string.Format("/network/{0}/",
                        NetworkNetwork);
                }
            }
        }

        public override string UriStubAbsolute
        {
            get
            {
                return core.Hyperlink.AppendAbsoluteSid(UriStub);
            }
        }

        public override string Uri
        {
            get
            {
                return core.Hyperlink.AppendSid(UriStub);
            }
        }

        public string MemberlistUri
        {
            get
            {
                return core.Hyperlink.AppendSid(string.Format("{0}members",
                    UriStub));
            }
        }

        public string BuildJoinUri()
        {
            return core.Hyperlink.BuildAccountSubModuleUri("networks", "memberships", "join", NetworkId);
        }

        public string BuildLeaveUri()
        {
            return core.Hyperlink.BuildAccountSubModuleUri("networks", "memberships", "leave", NetworkId);
        }

        public string BuildMemberListUri()
        {
            return core.Hyperlink.AppendSid(string.Format("/network/{0}/members",
                NetworkNetwork));
        }

        public static void Show(Core core, NPage page)
        {
            core.Template.SetTemplate("Networks", "viewnetwork");
            page.Signature = PageSignature.viewnetwork;

            if (core.Session.IsLoggedIn)
            {
                if (page.Network.IsNetworkMember(core.Session.LoggedInMember.ItemKey))
                {
                    core.Template.Parse("U_LEAVE", page.Network.BuildLeaveUri());
                }
                else
                {
                    core.Template.Parse("U_JOIN", page.Network.BuildJoinUri());
                }
            }

            core.Template.Parse("NETWORK_DISPLAY_NAME", page.Network.DisplayName);
            core.Display.ParseBbcode("DESCRIPTION", page.Network.Description);

            string langMembers = (page.Network.Members != 1) ? "members" : "member";
            string langIsAre = (page.Network.Members != 1) ? "are" : "is";

            core.Template.Parse("MEMBERS", page.Network.Members.ToString());
            core.Template.Parse("L_MEMBERS", langMembers);
            core.Template.Parse("L_IS_ARE", langIsAre);
            core.Template.Parse("U_MEMBERLIST", page.Network.BuildMemberListUri());

            List<NetworkMember> members = page.Network.GetMembers(1, 8);

            foreach (NetworkMember member in members)
            {
                Dictionary<string, string> membersLoopVars = new Dictionary<string, string>();
                VariableCollection membersVariableCollection = core.Template.CreateChild("member_list");

                membersVariableCollection.Parse("USER_DISPLAY_NAME", member.DisplayName);
                membersVariableCollection.Parse("U_PROFILE", member.Uri);
                membersVariableCollection.Parse("ICON", member.Icon);

            }

            core.InvokeHooks(new HookEventArgs(core, AppPrimitives.Network, page.Network));
        }

        public static void ShowMemberlist(Core core, NPage page)
        {
            core.Template.SetTemplate("Networks", "viewnetworkmemberlist");

            core.Template.Parse("MEMBERS_TITLE", "Member list for " + page.Network.DisplayName);
            core.Template.Parse("MEMBERS", ((ulong)page.Network.Members).ToString());

            foreach (NetworkMember member in page.Network.GetMembers(page.TopLevelPageNumber, 18))
            {
                VariableCollection memberVariableCollection = core.Template.CreateChild("member_list");


                string age;
                int ageInt = member.Profile.Age;
                if (ageInt == 0)
                {
                    age = "FALSE";
                }
                else
                {
                    age = ageInt.ToString() + " years old";
                }

                memberVariableCollection.Parse("USER_DISPLAY_NAME", member.DisplayName);
                memberVariableCollection.Parse("JOIN_DATE", page.tz.DateTimeToString(member.GetNetworkMemberJoinDate(page.tz)));
                memberVariableCollection.Parse("USER_AGE", age);
                memberVariableCollection.Parse("USER_COUNTRY", member.Profile.Country);
                memberVariableCollection.Parse("USER_CAPTION", "");

                memberVariableCollection.Parse("U_PROFILE", member.Uri);
                memberVariableCollection.Parse("ICON", member.Icon);
            }

            string pageUri = page.Network.MemberlistUri;
            core.Display.ParsePagination(pageUri, 18, page.Network.Members);

            List<string[]> breadCrumbParts = new List<string[]>();

            breadCrumbParts.Add(new string[] { "members", core.Prose.GetString("MEMBERS") });

            page.Network.GenerateBreadCrumbs(breadCrumbParts);
        }

        public override string AccountUriStub
        {
            get
            {
                return string.Format("/network/{0}/account/",
                    Key);
            }
        }

        #region ICommentableItem Members


        public SortOrder CommentSortOrder
        {
            get
            {
                return SortOrder.Descending;
            }
        }

        public byte CommentsPerPage
        {
            get
            {
                return 10;
            }
        }

        #endregion

        public override Access Access
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

        public override bool IsSimplePermissions
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

        public override List<AccessControlPermission> AclPermissions
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool IsItemGroupMember(ItemKey viewer, ItemKey key)
        {
            return false;
        }

        public override List<PrimitivePermissionGroup> GetPrimitivePermissionGroups()
        {
            List<PrimitivePermissionGroup> ppgs = new List<PrimitivePermissionGroup>();

            ppgs.Add(new PrimitivePermissionGroup(ItemType.GetTypeId(typeof(NetworkMember)), -1, "L_MEMBER", null, string.Empty));
            ppgs.Add(new PrimitivePermissionGroup(ItemType.GetTypeId(typeof(User)), -2, "L_EVERYONE", null, string.Empty));

            return ppgs;
        }

        public override List<User> GetPermissionUsers()
        {
            List<NetworkMember> members = GetMembers(1, 10);

            List<User> users = new List<User>();

            foreach (NetworkMember member in members)
            {
                users.Add(member);
            }

            return users;
        }

        public override List<User> GetPermissionUsers(string namePart)
        {
            List<NetworkMember> members = GetMembers(1, 10, namePart);

            List<User> users = new List<User>();

            foreach (NetworkMember member in members)
            {
                users.Add(member);
            }

            return users;
        }

        public static List<PrimitivePermissionGroup> Network_GetPrimitiveGroups(Core core, Primitive owner)
        {
            List<PrimitivePermissionGroup> ppgs = new List<PrimitivePermissionGroup>();

            if (owner is User)
            {
                List<Network> networks = Network.GetUserNetworks(core, (User)owner);

                foreach (Network network in networks)
                {
                    ppgs.Add(new PrimitivePermissionGroup(network.TypeId, network.Id, network.DisplayName, string.Empty));
                }
            }

            return ppgs;
        }

        public override bool GetIsMemberOfPrimitive(ItemKey viewer, ItemKey primitiveKey)
        {
            if (core.LoggedInMemberId > 0)
            {
                return IsNetworkMember(viewer);
            }

            return false;
        }

        public override bool CanEditPermissions()
        {
            return false;
        }

        public override bool CanEditItem()
        {
            return false;
        }

        public override bool CanDeleteItem()
        {
            return false;
        }

        public override bool GetDefaultCan(string permission, ItemKey viewer)
        {
            switch (permission)
            {
                case "COMMENT":
                    return IsNetworkMember(viewer);
                case "DELETE_COMMENTS":
                    return false;
            }
            return false;
        }

        public override string DisplayTitle
        {
            get
            {
                return "Network: " + DisplayName;
            }
        }

        public override string ParentPermissionKey(Type parentType, string permission)
        {
            return permission;
        }

        public static ItemKey NetworkMembersGroupKey
        {
            get
            {
                return new ItemKey(-1, ItemType.GetTypeId(typeof(NetworkMember)));
            }
        }

        public string Noun
        {
            get
            {
                return "guest book";
            }
        }

        public override string Thumbnail
        {
            get
            {
                return string.Empty;
            }
        }

        public override string Icon
        {
            get
            {
                return string.Empty;
            }
        }

        public override string Tile
        {
            get
            {
                return string.Empty;
            }
        }

        public override string Square
        {
            get
            {
                return string.Empty;
            }
        }

        public override string CoverPhoto
        {
            get
            {
                return string.Empty;
            }
        }

        public override string MobileCoverPhoto
        {
            get
            {
                return string.Empty;
            }
        }
    }

    public class InvalidNetworkException : Exception
    {
    }

    public class InvalidNetworkTypeException : Exception
    {
    }
}
