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
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
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

    /// <summary>
    /// DONE: run query on zinzam.com db
    /// ALTER TABLE `zinzam0_zinzam`.`comments` MODIFY COLUMN `comment_item_type` ENUM('UNASSOCIATED','PHOTO','BLOGPOST','PODCAST','PODCASTEPISODE','USER','PAGE','LIST','GROUP','NETWORK') CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL DEFAULT 'UNASSOCIATED';
    /// </summary>
    public class Network : Primitive
    {
        public const string NETWORK_INFO_FIELDS = "ni.network_id, ni.network_name_display, ni.network_abstract, ni.network_members, ni.network_comments, ni.network_require_confirmation, ni.network_type, ni.network_gallery_items, ni.network_bytes";

        private Mysql db;
        private int networkId;
        private string networkNetwork;
        private string displayName;
        private string description;
        private long members;
        private ulong comments;
        private bool requireConfirmation;
        private NetworkTypes networkType;
        private uint galleryItems;
        private long bytes;
        private string displayNameOwnership;

        private Dictionary<Member, bool> networkMemberCache = new Dictionary<Member, bool>();

        public int NetworkId
        {
            get
            {
                return networkId;
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
                return displayName;
            }
        }

        public override string DisplayNameOwnership
        {
            get
            {
                if (displayNameOwnership == null)
                {
                    displayNameOwnership = (displayName != "") ? displayName : networkNetwork;

                    if (displayNameOwnership.EndsWith("s"))
                    {
                        displayNameOwnership = displayNameOwnership + "'";
                    }
                    else
                    {
                        displayNameOwnership = displayNameOwnership + "'s";
                    }
                }
                return displayNameOwnership;
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
                return description;
            }
        }

        public long Members
        {
            get
            {
                return members;
            }
        }

        public ulong Comments
        {
            get
            {
                return comments;
            }
        }

        public bool RequireConfirmation
        {
            get
            {
                return requireConfirmation;
            }
        }

        public NetworkTypes NetworkType
        {
            get
            {
                return networkType;
            }
        }

        public uint GalleryItems
        {
            get
            {
                return galleryItems;
            }
        }

        public Network(Mysql db, long networkId)
        {
            this.db = db;

            DataTable networkTable = db.SelectQuery(string.Format("SELECT {1}, nk.network_network FROM network_keys nk INNER JOIN network_info ni ON nk.network_id = ni.network_id WHERE nk.network_id = {0}",
                networkId, NETWORK_INFO_FIELDS));

            if (networkTable.Rows.Count == 1)
            {
                loadNetworkInfo(networkTable.Rows[0]);
            }
            else
            {
                throw new InvalidNetworkException();
            }
        }

        public Network(Mysql db, string network)
        {
            this.db = db;

            DataTable networkTable = db.SelectQuery(string.Format("SELECT {1}, nk.network_network FROM network_keys nk INNER JOIN network_info ni ON nk.network_id = ni.network_id WHERE nk.network_network = '{0}'",
                Mysql.Escape(network), NETWORK_INFO_FIELDS));

            if (networkTable.Rows.Count == 1)
            {
                loadNetworkInfo(networkTable.Rows[0]);
            }
            else
            {
                throw new InvalidNetworkException();
            }
        }

        public Network(Mysql db, DataRow networkRow)
        {
            this.db = db;

            loadNetworkInfo(networkRow);
        }

        private void loadNetworkInfo(DataRow networkRow)
        {
            networkId = (int)networkRow["network_id"];
            networkNetwork = (string)networkRow["network_network"];
            displayName = (string)networkRow["network_name_display"];
            if (!(networkRow["network_abstract"] is DBNull))
            {
                description = (string)networkRow["network_abstract"];
            }
            members = (long)networkRow["network_members"];
            comments = (ulong)networkRow["network_comments"];
            requireConfirmation = ((byte)networkRow["network_require_confirmation"] > 0) ? true : false;
            switch ((string)networkRow["network_type"])
            {
                case "UNIVERSITY":
                    networkType = NetworkTypes.University;
                    break;
                case "SCHOOL":
                    networkType = NetworkTypes.School;
                    break;
                case "WORKKPLACE":
                    networkType = NetworkTypes.Workplace;
                    break;
                case "COUNTRY":
                    networkType = NetworkTypes.Country;
                    break;
                case "GLOBAL":
                    networkType = NetworkTypes.Global;
                    break;
            }
            galleryItems = (uint)networkRow["network_gallery_items"];
            bytes = (long)networkRow["network_bytes"];
        }

        public List<NetworkMember> GetMembers(int page, int perPage)
        {
            List<NetworkMember> members = new List<NetworkMember>();

            DataTable membersTable = db.SelectQuery(string.Format("SELECT {1}, {2}, {3}, {4} FROM network_members nm INNER JOIN user_info ui ON nm.user_id = ui.user_id INNER JOIN user_profile up ON nm.user_id = up.user_id LEFT JOIN countries c ON c.country_iso = up.profile_country LEFT JOIN gallery_items gi ON ui.user_icon = gi.gallery_item_id WHERE nm.network_id = {0} ORDER BY nm.member_join_date_ut ASC LIMIT {5}, {6}",
                networkId, Member.USER_INFO_FIELDS, Member.USER_PROFILE_FIELDS, Member.USER_ICON_FIELDS, NetworkMember.USER_NETWORK_FIELDS, (page - 1) * perPage, perPage));

            foreach (DataRow dr in membersTable.Rows)
            {
                members.Add(new NetworkMember(db, dr, true, true, true));
            }

            return members;
        }

        public static List<Network> GetNetworks(Mysql db, NetworkTypes type)
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

            DataTable networksTable = db.SelectQuery(string.Format("SELECT {1}, nk.network_network FROM network_keys nk INNER JOIN network_info ni ON nk.network_id = ni.network_id WHERE ni.network_type = '{0}'",
                Mysql.Escape(typeString), NETWORK_INFO_FIELDS));

            foreach (DataRow dr in networksTable.Rows)
            {
                networks.Add(new Network(db, dr));
            }

            return networks;
        }

        public bool IsNetworkMember(Member member)
        {
            if (member != null)
            {
                if (networkMemberCache.ContainsKey(member))
                {
                    return networkMemberCache[member];
                }
                else
                {
                    DataTable memberTable = db.SelectQuery(string.Format("SELECT user_id FROM network_members WHERE network_id = {0} AND user_id = {1} AND member_active = 1",
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
        public bool Activate(TPage page, Member member, string activateKey)
        {
            long rowsChanged = db.UpdateQuery(string.Format("UPDATE network_members SET member_active = 1 WHERE network_id = {0} AND user_id = {1} AND member_activate_code = '{2}' AND member_active = 0;",
                networkId, member.UserId, activateKey), false);

            db.UpdateQuery(string.Format("UPDATE network_info SET network_members = network_members + {1} WHERE network_id = {0}",
                networkId, rowsChanged), false);

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

        public NetworkMember Join(Core core, Member member, string networkEmail)
        {
            string activateKey = Member.GenerateActivationSecurityToken();

            if (!IsValidNetworkEmail(networkEmail) && requireConfirmation)
            {
                return null;
            }

            if (IsNetworkMember(member))
            {
                return null;
            }

            int isActive = (requireConfirmation) ? 0 : 1;

            // delete any existing unactivated e-mails for this user in this network, re-send the invitation
            db.UpdateQuery(string.Format("DELETE FROM network_members WHERE network_id = {0} AND user_id = {1} AND member_active = 0",
                networkId, member.UserId), true);

            if (!requireConfirmation)
            {
                db.UpdateQuery(string.Format("UPDATE network_info SET network_members = network_members + 1 WHERE network_id = {0}",
                    networkId), true);
            }

            db.UpdateQuery(string.Format("INSERT INTO network_members (network_id, user_id, member_join_date_ut, member_join_ip, member_email, member_active, member_activate_code) VALUES ({0}, {1}, UNIX_TIMESTAMP(), '{2}', '{3}', {4}, '{5}');",
                networkId, member.UserId, Mysql.Escape(core.session.IPAddress.ToString()), Mysql.Escape(networkEmail), isActive, Mysql.Escape(activateKey)), false);

            NetworkMember newMember = new NetworkMember(db, this, member);
            string activateUri = string.Format("http://zinzam.com/network/{0}?mode=activate&id={1}&key={2}",
                networkNetwork, member.UserId, activateKey);


            if (requireConfirmation)
            {
                Template emailTemplate = new Template(HttpContext.Current.Server.MapPath("./templates/emails/"), "join_network.eml");

                emailTemplate.ParseVariables("TO_NAME", member.DisplayName);
                emailTemplate.ParseVariables("U_ACTIVATE", activateUri);
                emailTemplate.ParseVariables("S_EMAIL", networkEmail);

                Email.SendEmail(core, networkEmail, "ZinZam Network Registration Confirmation", emailTemplate.ToString());
            }

            return newMember;
        }

        public bool IsValidNetworkEmail(string networkEmail)
        {
            if (Member.CheckEmailValid(networkEmail))
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

        public override bool CanModerateComments(Member member)
        {
            return false;
        }

        public override bool IsCommentOwner(Member member)
        {
            return false;
        }

        public override ushort GetAccessLevel(Member member)
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
                    break;
            }

            return 0x0000;
        }

        public override void GetCan(ushort accessBits, Member viewer, out bool canRead, out bool canComment, out bool canCreate, out bool canChange)
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
            return Linker.AppendSid(string.Format("/account/?module=networks&sub=join&id={0}",
                NetworkId), true);
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
                    // TODO: leave network URI
                }
                else
                {
                    page.template.ParseVariables("U_JOIN", HttpUtility.HtmlEncode(page.TheNetwork.BuildJoinUri()));
                }
            }

            page.template.ParseVariables("NETWORK_DISPLAY_NAME", HttpUtility.HtmlEncode(page.TheNetwork.DisplayName));
            page.template.ParseVariables("DESCRIPTION", Bbcode.Parse(HttpUtility.HtmlEncode(page.TheNetwork.Description), core.session.LoggedInMember));

            string langMembers = (page.TheNetwork.Members != 1) ? "members" : "member";
            string langIsAre = (page.TheNetwork.Members != 1) ? "are" : "is";

            page.template.ParseVariables("MEMBERS", HttpUtility.HtmlEncode(page.TheNetwork.Members.ToString()));
            page.template.ParseVariables("L_MEMBERS", HttpUtility.HtmlEncode(langMembers));
            page.template.ParseVariables("L_IS_ARE", HttpUtility.HtmlEncode(langIsAre));
            page.template.ParseVariables("U_MEMBERLIST", HttpUtility.HtmlEncode(page.TheNetwork.BuildMemberListUri()));

            List<NetworkMember> members = page.TheNetwork.GetMembers(1, 8);

            foreach (NetworkMember member in members)
            {
                Dictionary<string, string> membersLoopVars = new Dictionary<string, string>();
                VariableCollection membersVariableCollection = page.template.CreateChild("member_list");

                membersVariableCollection.ParseVariables("USER_DISPLAY_NAME", HttpUtility.HtmlEncode(member.DisplayName));
                membersVariableCollection.ParseVariables("U_PROFILE", HttpUtility.HtmlEncode(Linker.BuildProfileUri(member)));
                membersVariableCollection.ParseVariables("ICON", HttpUtility.HtmlEncode(member.UserIcon));

            }

            core.InvokeHooks(new HookEventArgs(core, AppPrimitives.Network, page.TheNetwork));
        }

        public static void ShowMemberlist(Core core, NPage page)
        {
            page.template.SetTemplate("Networks", "viewnetworkmemberlist");

            int p = Functions.RequestInt("p", 1);

            page.template.ParseVariables("MEMBERS_TITLE", HttpUtility.HtmlEncode("Member list for " + page.TheNetwork.DisplayName));
            page.template.ParseVariables("MEMBERS", HttpUtility.HtmlEncode(((ulong)page.TheNetwork.Members).ToString()));

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

                memberVariableCollection.ParseVariables("USER_DISPLAY_NAME", HttpUtility.HtmlEncode(member.DisplayName));
                memberVariableCollection.ParseVariables("JOIN_DATE", HttpUtility.HtmlEncode(page.tz.DateTimeToString(member.GetNetworkMemberJoinDate(page.tz))));
                memberVariableCollection.ParseVariables("USER_AGE", HttpUtility.HtmlEncode(age));
                memberVariableCollection.ParseVariables("USER_COUNTRY", HttpUtility.HtmlEncode(member.Country));
                memberVariableCollection.ParseVariables("USER_CAPTION", "");

                memberVariableCollection.ParseVariables("U_PROFILE", HttpUtility.HtmlEncode(Linker.BuildProfileUri(member)));
                memberVariableCollection.ParseVariables("ICON", HttpUtility.HtmlEncode(member.UserIcon));
            }

            string pageUri = page.TheNetwork.MemberlistUri;
            page.template.ParseVariables("PAGINATION", Display.GeneratePagination(pageUri, p, (int)Math.Ceiling(page.TheNetwork.Members / 18.0)));
            page.template.ParseVariables("BREADCRUMBS", page.TheNetwork.GenerateBreadCrumbs("members"));
        }
    }

    public class InvalidNetworkException : Exception
    {
    }
}
