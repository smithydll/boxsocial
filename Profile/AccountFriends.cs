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
using System.Configuration;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Security;
using BoxSocial;
using BoxSocial.IO;
using BoxSocial.Internals;

namespace BoxSocial
{
    public class AccountFriends : AccountModule
    {
        public AccountFriends(Account account)
            : base(account)
        {
            RegisterSubModule += new RegisterSubModuleHandler(ManageFriends);
            RegisterSubModule += new RegisterSubModuleHandler(InviteFriend);
            RegisterSubModule += new RegisterSubModuleHandler(ManageFamily);
            RegisterSubModule += new RegisterSubModuleHandler(ManageBlockList);
        }

        protected override void RegisterModule(Core core, EventArgs e)
        {
        }

        public override string Name
        {
            get
            {
                return "Friends";
            }
        }

        public override string Key
        {
            get
            {
                return "friends";
            }
        }

        public override int Order
        {
            get
            {
                return 3;
            }
        }

        private void ManageFriends(string submodule)
        {
            subModules.Add("friends", "Manage Friends");
            if (submodule != "friends" && !string.IsNullOrEmpty(submodule)) return;

            if (Request["mode"] == "add")
            {
                AddFriend();
            }
            else if (Request["mode"] == "delete")
            {
                DeleteFriend();
            }
            else if (Request["mode"] == "promote")
            {
                PromoteFriend();
            }
            else if (Request["mode"] == "demote")
            {
                DemoteFriend();
            }

            template.SetTemplate("Profile", "account_friends_manage");

            DataTable friendsTable = db.Query(string.Format("SELECT ur.relation_order, uk.user_name, uk.user_id FROM user_relations ur INNER JOIN user_keys uk ON uk.user_id = ur.relation_you WHERE ur.relation_type = 'FRIEND' AND ur.relation_me = {0} ORDER BY (relation_order - 1) ASC",
                loggedInMember.UserId));

            for (int i = 0; i < friendsTable.Rows.Count; i++)
            {
                VariableCollection friendsVariableCollection = template.CreateChild("friend_list");

                //Member friend = new Member(db, friendsTable.Rows[i], false, false);

                byte order = (byte)friendsTable.Rows[i]["relation_order"];

                friendsVariableCollection.ParseVariables("NAME", (string)friendsTable.Rows[i]["user_name"]);

                if (order > 0)
                {
                    friendsVariableCollection.ParseVariables("ORDER", order.ToString());
                }

                friendsVariableCollection.ParseVariables("U_PROFILE", HttpUtility.HtmlEncode(Linker.AppendSid(string.Format("/{0}", (string)friendsTable.Rows[i]["user_name"]))));
                friendsVariableCollection.ParseVariables("U_BLOCK", HttpUtility.HtmlEncode(Linker.BuildBlockUserUri((long)(int)friendsTable.Rows[i]["user_id"])));
                friendsVariableCollection.ParseVariables("U_DELETE", HttpUtility.HtmlEncode(Linker.BuildDeleteFriendUri((long)(int)friendsTable.Rows[i]["user_id"])));
                friendsVariableCollection.ParseVariables("U_PROMOTE", HttpUtility.HtmlEncode(Linker.BuildPromoteFriendUri((long)(int)friendsTable.Rows[i]["user_id"])));
                friendsVariableCollection.ParseVariables("U_DEMOTE", HttpUtility.HtmlEncode(Linker.BuildDemoteFriendUri((long)(int)friendsTable.Rows[i]["user_id"])));
            }
        }

        private void ManageFamily(string submodule)
        {
            subModules.Add("family", "Manage Family");
            if (submodule != "family") return;

            if (Request["mode"] == "add")
            {
                AddFamily();
            }
            else if (Request["mode"] == "delete")
            {
                DeleteFamily();
            }

            template.SetTemplate("Profile", "account_family_manage");

            DataTable familyTable = db.Query(string.Format("SELECT ur.relation_order, uk.user_name, uk.user_id FROM user_relations ur INNER JOIN user_keys uk ON uk.user_id = ur.relation_you WHERE ur.relation_type = 'FAMILY' AND ur.relation_me = {0} ORDER BY uk.user_name ASC",
                loggedInMember.UserId));

            for (int i = 0; i < familyTable.Rows.Count; i++)
            {
                VariableCollection familyVariableCollection = template.CreateChild("family_list");

                byte order = (byte)familyTable.Rows[i]["relation_order"];

                familyVariableCollection.ParseVariables("NAME", (string)familyTable.Rows[i]["user_name"]);

                familyVariableCollection.ParseVariables("U_BLOCK", HttpUtility.HtmlEncode(Linker.BuildBlockUserUri((long)(int)familyTable.Rows[i]["user_id"])));
                familyVariableCollection.ParseVariables("U_DELETE", HttpUtility.HtmlEncode(Linker.BuildDeleteFamilyUri((long)(int)familyTable.Rows[i]["user_id"])));
            }
        }

        private void DemoteFriend()
        {
            AuthoriseRequestSid();

            // all ok
            long friendId = 0;

            try
            {
                friendId = long.Parse(Request.QueryString["id"]);
            }
            catch
            {
                Display.ShowMessage("Cannot demote friend", "No friend specified to demote. Please go back and try again.");
                return;
            }

            DataTable friendTable = db.Query(string.Format("SELECT relation_order FROM user_relations WHERE relation_me = {0} AND relation_you = {1} AND relation_type = 'FRIEND'",
                loggedInMember.UserId, friendId));

            if (friendTable.Rows.Count == 1)
            {
                int relationOrder = (byte)friendTable.Rows[0]["relation_order"];

                if (relationOrder == 0)
                {
                    // do nothing, already demoted as far as will go, just wave through
                }
                else if (relationOrder == 255)
                {
                    db.UpdateQuery(string.Format("UPDATE user_relations SET relation_order = 0 WHERE relation_me = {0} AND relation_you = {1}",
                        loggedInMember.UserId, friendId));
                }
                else if (relationOrder < 255)
                {
                    int maxOrder = (int)(byte)db.Query(string.Format("SELECT MAX(relation_order) as max_order FROM user_relations WHERE relation_me = {0} AND relation_type = 'FRIEND'",
                        loggedInMember.UserId)).Rows[0]["max_order"];

                    if (relationOrder == maxOrder)
                    {
                        db.UpdateQuery(string.Format("UPDATE user_relations SET relation_order = 0 WHERE relation_me = {0} AND relation_you = {1}",
                            loggedInMember.UserId, friendId));
                    }
                    else
                    {
                        // switch places
                        db.BeginTransaction();
                        db.UpdateQuery(string.Format("UPDATE user_relations SET relation_order = relation_order - 1 WHERE relation_me = {0} AND relation_order = {1} AND relation_type = 'FRIEND'",
                            loggedInMember.UserId, relationOrder + 1));

                        db.UpdateQuery(string.Format("UPDATE user_relations SET relation_order = relation_order + 1 WHERE relation_me = {0} AND relation_you = {1} AND relation_type = 'FRIEND'",
                            loggedInMember.UserId, friendId));
                    }
                }

                template.ParseVariables("REDIRECT_URI", HttpUtility.HtmlEncode("/account/?module=friends&sub=friends"));
                Display.ShowMessage("Friend Demoted", "You have successfully demoted your friend in your social hierarchy.");
                return;
            }
            else
            {
                Display.ShowMessage("Error", "Error");
                return;
            }
        }

        private void PromoteFriend()
        {
            AuthoriseRequestSid();

            // all ok
            long friendId = 0;

            try
            {
                friendId = long.Parse(Request.QueryString["id"]);
            }
            catch
            {
                Display.ShowMessage("Cannot promote friend", "No friend specified to promote. Please go back and try again.");
                return;
            }

            DataTable friendTable = db.Query(string.Format("SELECT relation_order FROM user_relations WHERE relation_me = {0} AND relation_you = {1} AND relation_type = 'FRIEND'",
                loggedInMember.UserId, friendId));

            if (friendTable.Rows.Count == 1)
            {
                int relationOrder = (byte)friendTable.Rows[0]["relation_order"];

                if (relationOrder == 1)
                {
                    Display.ShowMessage("Cannot promote friend", "Cannot promote higher than the number one position.");
                    return;
                }
                else if (relationOrder > 0)
                {
                    // ordered friend

                    // switch places
                    db.BeginTransaction();
                    db.UpdateQuery(string.Format("UPDATE user_relations SET relation_order = relation_order + 1 WHERE relation_me = {0} AND relation_order = {1} AND relation_type = 'FRIEND'",
                        loggedInMember.UserId, relationOrder - 1));

                    db.UpdateQuery(string.Format("UPDATE user_relations SET relation_order = relation_order - 1 WHERE relation_me = {0} AND relation_you = {1} AND relation_type = 'FRIEND'",
                        loggedInMember.UserId, friendId));
                }
                else
                {
                    // unordered friend

                    // select the maximum order
                    int maxOrder = (int)(byte)db.Query(string.Format("SELECT MAX(relation_order) as max_order FROM user_relations WHERE relation_me = {0} AND relation_type = 'FRIEND'",
                        loggedInMember.UserId)).Rows[0]["max_order"];

                    // switch places
                    if (maxOrder > 0)
                    {
                        if (maxOrder == 255)
                        {
                            db.BeginTransaction();
                            db.UpdateQuery(string.Format("UPDATE user_relations SET relation_order = 0 WHERE relation_me = {0} AND relation_order = 255",
                                loggedInMember.UserId));

                            db.UpdateQuery(string.Format("UPDATE user_relations SET relation_order = {2} WHERE relation_me = {0} AND relation_you = {1} AND relation_type = 'FRIEND'",
                            loggedInMember.UserId, friendId, maxOrder));
                        }
                        else
                        {
                            db.UpdateQuery(string.Format("UPDATE user_relations SET relation_order = {2} WHERE relation_me = {0} AND relation_you = {1} AND relation_type = 'FRIEND'",
                                loggedInMember.UserId, friendId, maxOrder + 1));
                        }

                    }
                    else
                    {
                        db.UpdateQuery(string.Format("UPDATE user_relations SET relation_order = {2} WHERE relation_me = {0} AND relation_you = {1} AND relation_type = 'FRIEND'",
                            loggedInMember.UserId, friendId, 1));
                    }
                }

                template.ParseVariables("REDIRECT_URI", HttpUtility.HtmlEncode("/account/?module=friends&sub=friends"));
                Display.ShowMessage("Friend Promoted", "You have successfully promoted your friend in your social hierarchy.");
                return;
            }
            else
            {
                Display.ShowMessage("Error", "Error");
                return;
            }
        }

        private void DeleteFamily()
        {
            AuthoriseRequestSid();

            // all ok, delete from list of friends
            long friendId = 0;

            try
            {
                friendId = long.Parse(Request.QueryString["id"]);
            }
            catch
            {
                Display.ShowMessage("Cannot delete family member", "No family member specified to delete. Please go back and try again.");
                return;
            }

            db.BeginTransaction();
            long deletedRows = db.UpdateQuery(string.Format("DELETE FROM user_relations WHERE relation_me = {0} and relation_you = {1} AND relation_type = 'FAMILY'",
                loggedInMember.UserId, friendId));

            db.UpdateQuery(string.Format("UPDATE user_info ui SET ui.user_family = ui.user_family - {1} WHERE ui.user_id = {0};",
                loggedInMember.UserId, deletedRows));

            Display.ShowMessage("Deleted family member", "You have deleted a family member.");
        }

        private void DeleteFriend()
        {
            AuthoriseRequestSid();

            // all ok, delete from list of friends
            long friendId = 0;

            try
            {
                friendId = long.Parse(Request.QueryString["id"]);
            }
            catch
            {
                Display.ShowMessage("Cannot delete friend", "No friend specified to delete. Please go back and try again.");
                return;
            }

            db.BeginTransaction();
            long deletedRows = db.UpdateQuery(string.Format("DELETE FROM user_relations WHERE relation_me = {0} and relation_you = {1} AND relation_type = 'FRIEND'",
                loggedInMember.UserId, friendId));

            db.UpdateQuery(string.Format("UPDATE user_info ui SET ui.user_friends = ui.user_friends - {1} WHERE ui.user_id = {0};",
                loggedInMember.UserId, deletedRows));

            template.ParseVariables("REDIRECT_URI", "/account/?module=friends&sub=friends");
            Display.ShowMessage("Deleted friend", "You have deleted a friend.");
        }

        private void AddFamily()
        {
            AuthoriseRequestSid();

            // all ok, add as a friend
            long friendId = 0;

            try
            {
                friendId = long.Parse(Request.QueryString["id"]);
            }
            catch
            {
                Display.ShowMessage("Cannot add to family", "No user specified to add as family. Please go back and try again.");
                return;
            }

            // cannot befriend yourself
            if (friendId == loggedInMember.UserId)
            {
                Display.ShowMessage("Cannot add yourself", "You cannot add yourself as a family member.");
                return;
            }

            // check existing friend-foe status
            DataTable relationsTable = db.Query(string.Format("SELECT relation_type FROM user_relations WHERE relation_me = {0} AND relation_you = {1}",
                loggedInMember.UserId, friendId));

            for (int i = 0; i < relationsTable.Rows.Count; i++)
            {
                if ((string)relationsTable.Rows[i]["relation_type"] == "FAMILY")
                {
                    Display.ShowMessage("Already in family", "You have already added this person to your family.");
                    return;
                }
                if ((string)relationsTable.Rows[i]["relation_type"] == "BLOCKED")
                {
                    Display.ShowMessage("Person Blocked", "You have blocked this person, to add them to your family you must first unblock them.");
                    return;
                }
            }

            bool isFriend = false;
            if (db.Query(string.Format("SELECT relation_time_ut FROM user_relations WHERE relation_me = {0} AND relation_you = {1} AND relation_type = 'FAMILY';",
                loggedInMember.UserId, friendId)).Rows.Count == 1)
            {
                isFriend = true;
            }

            db.BeginTransaction();
            long relationId = db.UpdateQuery(string.Format("INSERT INTO user_relations (relation_me, relation_you, relation_time_ut, relation_type) VALUES ({0}, {1}, UNIX_TIMESTAMP(), 'FAMILY');",
                loggedInMember.UserId, friendId));

            if (!isFriend)
            {
                db.UpdateQuery(string.Format("INSERT INTO friend_notifications (relation_id, notification_time_ut, notification_read) VALUES ({0}, UNIX_TIMESTAMP(), 0)",
                    relationId));
            }

            db.UpdateQuery(string.Format("UPDATE user_info ui SET ui.user_family = ui.user_family + 1 WHERE ui.user_id = {0};",
                loggedInMember.UserId));

            Display.ShowMessage("Added family member", "You have added person to your family.");
        }

        private void InviteFriend(string submodule)
        {
            subModules.Add("invite", "Invite Friends");
            if (submodule != "invite") return;

            if (Request["mode"] == "Send" || Request.Form["send"] != null)
            {
                InviteFriendSend();
            }

            template.SetTemplate("Profile", "account_friend_invite");

            template.ParseVariables("S_INVITE_FRIEND", HttpUtility.HtmlEncode(Linker.AppendSid("/account/", true)));
        }

        private void InviteFriendsSend(string[] friendEmails)
        {
            foreach (string friendEmail in friendEmails)
            {
                //Response.Write(friendEmail + "<br />"); // DEBUG
                if (Member.CheckEmailValid(friendEmail))
                {
                    if (Member.CheckEmailUnique(db, friendEmail))
                    {
                        DataTable inviteKeysTable = db.Query(string.Format("SELECT email_key FROM invite_keys WHERE email_hash = '{0}' AND invite_allow = 0",
                            Mysql.Escape(Member.HashPassword(friendEmail))));

                        if (inviteKeysTable.Rows.Count > 0)
                        {
                            // ignore ignore invites, plough on
                        }
                        else
                        {
                            Random rand = new Random();
                            string emailKey = Member.HashPassword(friendEmail + rand.NextDouble().ToString());
                            emailKey = emailKey.Substring((int)(rand.NextDouble() * 10), 32);

                            Template emailTemplate = new Template(Server.MapPath("./templates/emails/"), "friend_invitation.eml");

                            emailTemplate.ParseVariables("FROM_NAME", loggedInMember.DisplayName);
                            emailTemplate.ParseVariables("FROM_EMAIL", loggedInMember.AlternateEmail);
                            emailTemplate.ParseVariables("FROM_NAMES", loggedInMember.DisplayNameOwnership);
                            emailTemplate.ParseVariables("U_REGISTER", "http://zinzam.com/register/");
                            emailTemplate.ParseVariables("U_PROFILE", "http://zinzam.com" + Linker.BuildProfileUri(loggedInMember));
                            emailTemplate.ParseVariables("U_OPTOUT", "http://zinzam.com/register/?mode=optout&key=" + emailKey);

                            Email.SendEmail(friendEmail, string.Format("{0} has invited you to ZinZam.",
                                loggedInMember.DisplayName),
                                emailTemplate.ToString());

                            db.UpdateQuery(string.Format("INSERT INTO invite_keys (email_key, invite_allow, email_hash) VALUES ('{0}', 1, '{1}');",
                                Mysql.Escape(emailKey), Mysql.Escape(Member.HashPassword(friendEmail))));
                        }
                    }
                    else
                    {
                        // ignore already a member, plough on
                        if (friendEmail.ToLower() != loggedInMember.AlternateEmail.ToLower())
                        {
                            DataTable friendTable = db.Query(string.Format("SELECT {1} FROM user_info ui WHERE LCASE(user_alternate_email) = '{1}'",
                                Mysql.Escape(friendEmail.ToLower()), Member.USER_INFO_FIELDS));

                            if (friendTable.Rows.Count == 1)
                            {
                                Member friendProfile = new Member(core, friendTable.Rows[0], false);
                                long friendId = friendProfile.UserId;

                                db.BeginTransaction();
                                long relationId = db.UpdateQuery(string.Format("INSERT INTO user_relations (relation_me, relation_you, relation_time_ut, relation_type) VALUES ({0}, {1}, UNIX_TIMESTAMP(), 'FRIEND');",
                                    loggedInMember.UserId, friendId));

                                db.UpdateQuery(string.Format("INSERT INTO friend_notifications (relation_id, notification_time_ut, notification_read) VALUES ({0}, UNIX_TIMESTAMP(), 0)",
                                    relationId));

                                db.UpdateQuery(string.Format("UPDATE user_info ui SET ui.user_friends = ui.user_friends + 1 WHERE ui.user_id = {0};",
                                    loggedInMember.UserId));

                                // send e-mail notification
                                // only send a notification if they have subscribed to them
                                if (friendProfile.EmailNotifications)
                                {
                                    Template emailTemplate = new Template(Server.MapPath("./templates/emails/"), "friend_notification.eml");

                                    emailTemplate.ParseVariables("TO_NAME", friendProfile.DisplayName);
                                    emailTemplate.ParseVariables("FROM_NAME", loggedInMember.DisplayName);
                                    emailTemplate.ParseVariables("FROM_USERNAME", loggedInMember.UserName);

                                    Email.SendEmail(friendProfile.AlternateEmail, string.Format("{0} added you as a friend on ZinZam.",
                                        loggedInMember.DisplayName),
                                        emailTemplate.ToString());
                                }
                            }
                        }
                    }
                }
                else
                {
                    // ignore invalid addresses, plough on
                }
            }

            template.ParseVariables("REDIRECT_URI", "/account/?module=friends&sub=invite");
            Display.ShowMessage("Invited Friend", "You have invited all your friends to ZinZam.");
        }

        private void InviteFriendSend()
        {
            AuthoriseRequestSid();

            if (Request.Files["contacts"] != null)
            {
                StreamReader sr = new StreamReader(Request.Files["contacts"].InputStream);
                string contactsString = sr.ReadToEnd();

                MatchCollection mc = Regex.Matches(contactsString, @"[a-z0-9&\'\.\-_\+]+@[a-z0-9\-]+\.([a-z0-9\-]+\.)*?[a-z]+", RegexOptions.IgnoreCase);
                string[] friendEmails = new string[mc.Count];
                int i = 0;
                foreach (Match m in mc)
                {
                    friendEmails[i] = m.Value;
                    i++;
                }
                InviteFriendsSend(friendEmails);
                return;
            }

            string friendEmail = ((string)Request.Form["email"]).Trim(new char[] { ' ', '\t' });
            string friendName = Request.Form["name"];

            friendEmail = (string.IsNullOrEmpty(friendEmail)) ? Request.QueryString["email"] : friendEmail;
            friendName = (string.IsNullOrEmpty(friendName)) ? Request.QueryString["name"] : friendName;

            if (string.IsNullOrEmpty(friendEmail))
            {
                Display.ShowMessage("Cannot Invite Friend", "You must enter a valid e-mail address to invite.");
                return;
            }

            if (Member.CheckEmailValid(friendEmail))
            {
                if (Member.CheckEmailUnique(db, friendEmail))
                {
                    DataTable inviteKeysTable = db.Query(string.Format("SELECT email_key FROM invite_keys WHERE email_hash = '{0}' AND invite_allow = 0",
                        Mysql.Escape(Member.HashPassword(friendEmail))));

                    if (inviteKeysTable.Rows.Count > 0)
                    {
                        Display.ShowMessage("Cannot Invite Friend", "The person you have invited has opted-out of mailings from ZinZam.");
                        return;
                    }
                    else
                    {
                        Random rand = new Random();
                        string emailKey = Member.HashPassword(friendEmail + rand.NextDouble().ToString());
                        emailKey = emailKey.Substring((int)(rand.NextDouble() * 10), 32);

                        Template emailTemplate = new Template(Server.MapPath("./templates/emails/"), "friend_invitation.eml");

                        if (!string.IsNullOrEmpty(friendName))
                        {
                            emailTemplate.ParseVariables("TO_NAME", " " + friendName);
                        }

                        emailTemplate.ParseVariables("FROM_NAME", loggedInMember.DisplayName);
                        emailTemplate.ParseVariables("FROM_EMAIL", loggedInMember.AlternateEmail);
                        emailTemplate.ParseVariables("FROM_NAMES", loggedInMember.DisplayNameOwnership);
                        emailTemplate.ParseVariables("U_REGISTER", "http://zinzam.com/register/");
                        emailTemplate.ParseVariables("U_PROFILE", "http://zinzam.com" + Linker.BuildProfileUri(loggedInMember));
                        emailTemplate.ParseVariables("U_OPTOUT", "http://zinzam.com/register/?mode=optout&key=" + emailKey);

                        Email.SendEmail(friendEmail, string.Format("{0} has invited you to ZinZam.",
                            loggedInMember.DisplayName),
                            emailTemplate.ToString());

                        db.UpdateQuery(string.Format("INSERT INTO invite_keys (email_key, invite_allow, email_hash) VALUES ('{0}', 1, '{1}');",
                            Mysql.Escape(emailKey), Mysql.Escape(Member.HashPassword(friendEmail))));
                    }
                }
                else
                {
                    Display.ShowMessage("Already Member", string.Format("This person is already a member of ZinZam. To add them to your friends list <a href=\"{0}\">click here</a>.",
                        Linker.BuildAddFriendUri(Member.lastEmailId)));
                    return;
                }
            }
            else
            {
                Display.ShowMessage("Cannot Invite Friend", "You must enter a valid e-mail address to invite.");
                return;
            }

            template.ParseVariables("REDIRECT_URI", "/account/?module=friends&sub=invite");
            Display.ShowMessage("Invited Friend", "You have invited a friend to ZinZam.");
        }

        private void AddFriend()
        {
            AuthoriseRequestSid();

            // all ok, add as a friend
            long friendId = 0;

            try
            {
                friendId = long.Parse(Request.QueryString["id"]);
            }
            catch
            {
                Display.ShowMessage("Cannot add friend", "No friend specified to add. Please go back and try again.");
                return;
            }

            // cannot befriend yourself
            if (friendId == loggedInMember.UserId)
            {
                Display.ShowMessage("Cannot add friend", "You cannot add yourself as a friend.");
                return;
            }

            // check existing friend-foe status
            DataTable relationsTable = db.Query(string.Format("SELECT relation_type FROM user_relations WHERE relation_me = {0} AND relation_you = {1}",
                loggedInMember.UserId, friendId));

            for (int i = 0; i < relationsTable.Rows.Count; i++)
            {
                if ((string)relationsTable.Rows[i]["relation_type"] == "FRIEND")
                {
                    Display.ShowMessage("Already friend", "You have already added this person as a friend.");
                    return;
                }
                if ((string)relationsTable.Rows[i]["relation_type"] == "BLOCKED")
                {
                    Display.ShowMessage("Person Blocked", "You have blocked this person, to add them as a friend you must first unblock them.");
                    return;
                }
            }

            Member friendProfile = new Member(core, friendId);

            bool isFriend = friendProfile.IsFriend(session.LoggedInMember);

            db.BeginTransaction();
            long relationId = db.UpdateQuery(string.Format("INSERT INTO user_relations (relation_me, relation_you, relation_time_ut, relation_type) VALUES ({0}, {1}, UNIX_TIMESTAMP(), 'FRIEND');",
                loggedInMember.UserId, friendId));

            //
            // send e-mail notifications
            //

            ApplicationEntry ae = new ApplicationEntry(core, core.session.LoggedInMember, "Profile");

            Template emailTemplate = new Template(Server.MapPath("./templates/emails/"), "friend_notification.eml");

            emailTemplate.ParseVariables("TO_NAME", friendProfile.DisplayName);
            emailTemplate.ParseVariables("FROM_NAME", loggedInMember.DisplayName);
            emailTemplate.ParseVariables("FROM_USERNAME", loggedInMember.UserName);

            if (!isFriend)
            {
                ae.SendNotification(friendProfile, string.Format("[user]{0}[/user] wants to add you as a friend.", loggedInMember.Id), string.Format("[iurl=\"{0}\" sid=true]Click Here[/iurl] to add [user]{1}[/user] as a friend.", Linker.BuildAddFriendUri(loggedInMember.Id, false), loggedInMember.Id), emailTemplate);
            }
            else
            {
                ae.SendNotification(friendProfile, string.Format("[user]{0}[/user] accepted your friendship.", loggedInMember.Id), string.Format("[user]{0}[/user] has confirmed your friendship. You may now be able to interract with your friend in more ways.", loggedInMember.Id), emailTemplate);
            }

            db.UpdateQuery(string.Format("UPDATE user_info ui SET ui.user_friends = ui.user_friends + 1 WHERE ui.user_id = {0};",
                loggedInMember.UserId));

            template.ParseVariables("REDIRECT_URI", "/account/?module=friends&sub=friends");
            Display.ShowMessage("Added friend", "You have added a friend.");
        }

        private void ManageBlockList(string submodule)
        {
            subModules.Add("block", "Manage Block List");
            if (submodule != "block") return;

            if (Request["mode"] == "block")
            {
                BlockPerson();
            }
            else if (Request["mode"] == "unblock")
            {
                UnBlockPerson();
            }

            template.SetTemplate("Profile", "account_blocklist_manage");

            DataTable blockTable = db.Query(string.Format("SELECT ur.relation_order, uk.user_name, uk.user_id FROM user_relations ur INNER JOIN user_keys uk ON uk.user_id = ur.relation_you WHERE ur.relation_type = 'BLOCKED' AND ur.relation_me = {0} ORDER BY uk.user_name ASC",
                loggedInMember.UserId));

            for (int i = 0; i < blockTable.Rows.Count; i++)
            {
                VariableCollection friendsVariableCollection = template.CreateChild("block_list");

                byte order = (byte)blockTable.Rows[i]["relation_order"];

                friendsVariableCollection.ParseVariables("NAME", (string)blockTable.Rows[i]["user_name"]);

                friendsVariableCollection.ParseVariables("U_UNBLOCK", HttpUtility.HtmlEncode(Linker.BuildUnBlockUserUri((long)(int)blockTable.Rows[i]["user_id"])));
            }
        }

        public void UnBlockPerson()
        {
            AuthoriseRequestSid();

            // all ok
            long blockId = 0;

            try
            {
                blockId = long.Parse(Request.QueryString["id"]);
            }
            catch
            {
                Display.ShowMessage("Cannot unblock person", "No person specified to unblock. Please go back and try again.");
                return;
            }

            // check existing friend-foe status
            DataTable relationsTable = db.Query(string.Format("SELECT relation_type FROM user_relations WHERE relation_me = {0} AND relation_you = {1}",
                loggedInMember.UserId, blockId));

            for (int i = 0; i < relationsTable.Rows.Count; i++)
            {
                if ((string)relationsTable.Rows[i]["relation_type"] == "BLOCKED")
                {
                    break;
                }
                else if (i == relationsTable.Rows.Count - 1)
                {
                    Display.ShowMessage("Cannot unblock person", "This person is not blocked, cannot unlock.");
                    return;
                }
            }

            db.BeginTransaction();
            db.UpdateQuery(string.Format("DELETE FROM user_relations WHERE relation_me = {0} AND relation_you = {1} AND relation_type = 'BLOCKED';",
                    loggedInMember.UserId, blockId));

            // do not notify

            db.UpdateQuery(string.Format("UPDATE user_info ui SET ui.user_block = ui.user_block - 1 WHERE ui.user_id = {0};",
                loggedInMember.UserId));

            template.ParseVariables("REDIRECT_URI", "/account/?module=friends&sub=friends");
            Display.ShowMessage("Unblocked Person", "You have unblocked a person.");
        }

        public void BlockPerson()
        {
            AuthoriseRequestSid();

            // all ok
            long blockId = 0;

            try
            {
                blockId = long.Parse(Request["id"]);
            }
            catch
            {
                Display.ShowMessage("Cannot block person", "No person specified to block. Please go back and try again.");
                return;
            }

            // cannot befriend yourself
            if (blockId == loggedInMember.UserId)
            {
                Display.ShowMessage("Cannot block person", "You cannot block yourself.");
                return;
            }

            // check existing friend-foe status
            DataTable relationsTable = db.Query(string.Format("SELECT relation_type FROM user_relations WHERE relation_me = {0} AND relation_you = {1}",
                loggedInMember.UserId, blockId));

            for (int i = 0; i < relationsTable.Rows.Count; i++)
            {
                if ((string)relationsTable.Rows[i]["relation_type"] == "FRIEND")
                {
                    switch (Display.GetConfirmBoxResult())
                    {
                        case ConfirmBoxResult.None:
                            Dictionary<string, string> hiddenFieldList = new Dictionary<string, string>();
                            hiddenFieldList.Add("module", "friends");
                            hiddenFieldList.Add("sub", "block");
                            hiddenFieldList.Add("mode", "block");
                            hiddenFieldList.Add("id", blockId.ToString());

                            Display.ShowConfirmBox(HttpUtility.HtmlEncode(Linker.AppendSid("/account/", true)),
                                "Delete as friend?",
                                "Do you also want to delete this person from your friends list?",
                                hiddenFieldList);
                            return;
                        case ConfirmBoxResult.Yes:
                            // remove from friends
                            db.BeginTransaction();
                            long deletedRows = db.UpdateQuery(string.Format("DELETE FROM user_relations WHERE relation_me = {0} and relation_you = {1} AND relation_type = 'FRIEND';",
                                loggedInMember.UserId, blockId));

                            db.UpdateQuery(string.Format("UPDATE user_info ui SET ui.user_friends = ui.user_friends - 1 WHERE ui.user_id = {0};",
                                loggedInMember.UserId));
                            break;
                        case ConfirmBoxResult.No:
                            // don't do anything
                            break;
                    }
                }
                if ((string)relationsTable.Rows[i]["relation_type"] == "BLOCKED")
                {
                    Display.ShowMessage("Person Already Blocked", "You have already blocked this person.");
                    return;
                }
            }

            db.BeginTransaction();
            long relationId = db.UpdateQuery(string.Format("INSERT INTO user_relations (relation_me, relation_you, relation_time_ut, relation_type) VALUES ({0}, {1}, UNIX_TIMESTAMP(), 'BLOCKED');",
                loggedInMember.UserId, blockId));

            // do not notify

            db.UpdateQuery(string.Format("UPDATE user_info ui SET ui.user_block = ui.user_block + 1 WHERE ui.user_id = {0};",
                loggedInMember.UserId));

            template.ParseVariables("REDIRECT_URI", "/account/?module=friends&sub=block");
            Display.ShowMessage("Blocked Person", "You have blocked a person.");
        }
    }
}
