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
using System.Data;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
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
    [DataTable("network_keys")]
    public class Network : Primitive, ICommentableItem
    {
        public const string NETWORK_INFO_FIELDS = "`network_info`.network_id, `network_info`.network_name_display, `network_info`.network_abstract, `network_info`.network_members, `network_info`.network_comments, `network_info`.network_require_confirmation, `network_info`.network_type, `network_info`.network_gallery_items, `network_info`.network_bytes";

        [DataField("network_id", DataFieldKeys.Primary)]
        private long networkId;
        [DataField("network_network", DataFieldKeys.Unique, 24)]
        private string networkNetwork;

        private NetworkInfo networkInfo;

        private Dictionary<User, bool> networkMemberCache = new Dictionary<User, bool>();

        public long NetworkId
        {
            get
            {
                return networkId;
            }
        }

        public NetworkInfo Info
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
                return networkInfo.Comments;
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
        }

        public List<NetworkMember> GetMembers(int page, int perPage)
        {
            List<NetworkMember> members = new List<NetworkMember>();

            SelectQuery query = new SelectQuery(NetworkMember.GetTable(typeof(NetworkMember)));
            query.AddFields(NetworkMember.GetFieldsPrefixed(typeof(NetworkMember)));
            query.AddCondition("network_id", networkId);
            query.AddSort(SortOrder.Ascending, "member_join_date_ut");
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

            DataTable membershipsTable = core.db.Query(query);

            foreach (DataRow dr in membershipsTable.Rows)
            {
                memberships.Add(new NetworkMember(core, dr, UserLoadOptions.Key));
            }

            return memberships;
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

            SelectQuery query = Network.GetSelectQueryStub(typeof(Network));
            query.AddFields(NetworkInfo.GetFieldsPrefixed(typeof(NetworkInfo)));
            query.AddJoin(JoinTypes.Inner, NetworkInfo.GetTable(typeof(NetworkInfo)), "network_id", "network_id");
            query.AddCondition("network_type", typeString);

            DataTable networksTable = core.db.Query(query);

            foreach (DataRow dr in networksTable.Rows)
            {
                networks.Add(new Network(core, dr, NetworkLoadOptions.Common));
            }

            return networks;
        }

        public bool IsNetworkMember(User member)
        {
            if (member != null)
            {
                if (networkMemberCache.ContainsKey(member))
                {
                    return networkMemberCache[member];
                }
                else
                {
                    DataTable memberTable = db.Query(string.Format("SELECT user_id FROM network_members WHERE network_id = {0} AND user_id = {1} AND member_active = 1",
                        networkId, member.UserId));

                    if (memberTable.Rows.Count > 0)
                    {
                        networkMemberCache.Add(member, true);
                        return true;
                    }
                    else
                    {
                        networkMemberCache.Add(member, false);
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
                networkMemberCache.Add(member, true);
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
                RawTemplate emailTemplate = new RawTemplate(HttpContext.Current.Server.MapPath("./templates/emails/"), "join_network.eml");

                emailTemplate.Parse("TO_NAME", member.DisplayName);
                emailTemplate.Parse("U_ACTIVATE", activateUri);
                emailTemplate.Parse("S_EMAIL", member.MemberEmail);

                Email.SendEmail(member.MemberEmail, "ZinZam Network Registration Confirmation", emailTemplate.ToString());
            }
        }

        public NetworkMember Join(Core core, User member, string networkEmail)
        {
            string activateKey = User.GenerateActivationSecurityToken();

            if (!IsValidNetworkEmail(networkEmail) && networkInfo.RequireConfirmation)
            {
                return null;
            }

            if (IsNetworkMember(member))
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
            iQuery.AddField("member_join_ip", core.session.IPAddress.ToString());
            iQuery.AddField("member_email", networkEmail);
            iQuery.AddField("member_active", isActive);
            iQuery.AddField("member_activate_code", activateKey);

            db.Query(iQuery);

            NetworkMember newMember = new NetworkMember(core, this, member);
            string activateUri = string.Format("http://zinzam.com/network/{0}?mode=activate&id={1}&key={2}",
                networkNetwork, member.UserId, activateKey);

            if (networkInfo.RequireConfirmation)
            {
                UserEmail registrationEmail = UserEmail.Create(core, newMember, networkEmail, 0x0000, true);

                RawTemplate emailTemplate = new RawTemplate(HttpContext.Current.Server.MapPath("./templates/emails/"), "join_network.eml");

                emailTemplate.Parse("TO_NAME", member.DisplayName);
                emailTemplate.Parse("U_ACTIVATE", activateUri);
                emailTemplate.Parse("S_EMAIL", networkEmail);

                Email.SendEmail(networkEmail, "ZinZam Network Registration Confirmation", emailTemplate.ToString());
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

        public override bool IsCommentOwner(User member)
        {
            return false;
        }

        public override ushort GetAccessLevel(User member)
        {
            switch (NetworkType)
            {
                case NetworkTypes.Country:
                case NetworkTypes.Global:
                    // can view the network and all it's photos
                    return 0x0001;
                case NetworkTypes.University:
                case NetworkTypes.School:
                case NetworkTypes.Workplace:
                    if (IsNetworkMember(member))
                    {
                        return 0x0001;
                    }
                    else
                    {
                        // TODO: !!!!
                        return 0x0001;
                    }
            }

            return 0x0000;
        }

        public override void GetCan(ushort accessBits, User viewer, out bool canRead, out bool canComment, out bool canCreate, out bool canChange)
        {
            bool isNetworkMember = IsNetworkMember(viewer);
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
                    DisplayName, path);

            for (int i = 0; i < parts.Count; i++)
            {
                if (parts[i][0] != "")
                {
                    path += "/" + parts[i][0];
                    output += string.Format(" <strong>&#8249;</strong> <a href=\"{1}\">{0}</a>",
                        parts[i][1], path);
                }
            }

            return output;
        }

        public override string Uri
        {
            get
            {
                return Linker.AppendSid(string.Format("/network/{0}",
                    NetworkNetwork));
            }
        }

        public string MemberlistUri
        {
            get
            {
                return Linker.AppendSid(string.Format("/network/{0}/members",
                    NetworkNetwork));
            }
        }

        public string BuildJoinUri()
        {
            return AccountModule.BuildModuleUri("networks", "memberships", "join", NetworkId);
        }

        public string BuildLeaveUri()
        {
            return AccountModule.BuildModuleUri("networks", "memberships", "leave", NetworkId);
        }

        public string BuildMemberListUri()
        {
            return Linker.AppendSid(string.Format("/network/{0}/members",
                NetworkNetwork));
        }

        public static void Show(Core core, NPage page)
        {
            page.template.SetTemplate("Networks", "viewnetwork");
            page.Signature = PageSignature.viewnetwork;

            if (core.session.IsLoggedIn)
            {
                if (page.TheNetwork.IsNetworkMember(core.session.LoggedInMember))
                {
                    page.template.Parse("U_LEAVE", page.TheNetwork.BuildLeaveUri());
                }
                else
                {
                    page.template.Parse("U_JOIN", page.TheNetwork.BuildJoinUri());
                }
            }

            page.template.Parse("NETWORK_DISPLAY_NAME", page.TheNetwork.DisplayName);
            Display.ParseBbcode("DESCRIPTION", page.TheNetwork.Description);

            string langMembers = (page.TheNetwork.Members != 1) ? "members" : "member";
            string langIsAre = (page.TheNetwork.Members != 1) ? "are" : "is";

            page.template.Parse("MEMBERS", page.TheNetwork.Members.ToString());
            page.template.Parse("L_MEMBERS", langMembers);
            page.template.Parse("L_IS_ARE", langIsAre);
            page.template.Parse("U_MEMBERLIST", page.TheNetwork.BuildMemberListUri());

            List<NetworkMember> members = page.TheNetwork.GetMembers(1, 8);

            foreach (NetworkMember member in members)
            {
                Dictionary<string, string> membersLoopVars = new Dictionary<string, string>();
                VariableCollection membersVariableCollection = page.template.CreateChild("member_list");

                membersVariableCollection.Parse("USER_DISPLAY_NAME", member.DisplayName);
                membersVariableCollection.Parse("U_PROFILE", Linker.BuildProfileUri(member));
                membersVariableCollection.Parse("ICON", member.UserIcon);

            }

            core.InvokeHooks(new HookEventArgs(core, AppPrimitives.Network, page.TheNetwork));
        }

        public static void ShowMemberlist(Core core, NPage page)
        {
            page.template.SetTemplate("Networks", "viewnetworkmemberlist");

            int p = Functions.RequestInt("p", 1);

            page.template.Parse("MEMBERS_TITLE", "Member list for " + page.TheNetwork.DisplayName);
            page.template.Parse("MEMBERS", ((ulong)page.TheNetwork.Members).ToString());

            foreach (NetworkMember member in page.TheNetwork.GetMembers(p, 18))
            {
                VariableCollection memberVariableCollection = page.template.CreateChild("member_list");


                string age;
                int ageInt = member.Age;
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
                memberVariableCollection.Parse("USER_COUNTRY", member.Country);
                memberVariableCollection.Parse("USER_CAPTION", "");

                memberVariableCollection.Parse("U_PROFILE", Linker.BuildProfileUri(member));
                memberVariableCollection.Parse("ICON", member.UserIcon);
            }

            string pageUri = page.TheNetwork.MemberlistUri;
            Display.ParsePagination(pageUri, p, (int)Math.Ceiling(page.TheNetwork.Members / 18.0));
            page.TheNetwork.GenerateBreadCrumbs("members");
        }

        public override string Namespace
        {
            get
            {
                return Type;
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
    }

    public class InvalidNetworkException : Exception
    {
    }

    public class InvalidNetworkTypeException : Exception
    {
    }
}
