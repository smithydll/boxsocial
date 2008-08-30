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
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Applications.Profile
{
    [AccountSubModule("friends", "invite")]
    public class AccountFriendInvite : AccountSubModule
    {
        public override string Title
        {
            get
            {
                return "Invite Friends";
            }
        }

        public override int Order
        {
            get
            {
                return 2;
            }
        }

        public AccountFriendInvite()
        {
            this.Load += new EventHandler(AccountFriendInvite_Load);
            this.Show += new EventHandler(AccountFriendInvite_Show);
        }

        void AccountFriendInvite_Load(object sender, EventArgs e)
        {
            AddModeHandler("send", new ModuleModeHandler(AccountFriendInvite_Send));
        }

        void AccountFriendInvite_Show(object sender, EventArgs e)
        {
            SetTemplate("account_friend_invite");

            Save(new EventHandler(AccountFriendInvite_Send));
        }

        void AccountFriendInvite_Send(object sender, EventArgs e)
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

            if (User.CheckEmailValid(friendEmail))
            {
                if (User.CheckEmailUnique(core, friendEmail))
                {
                    DataTable inviteKeysTable = db.Query(string.Format("SELECT email_key FROM invite_keys WHERE email_hash = '{0}' AND invite_allow = 0",
                        Mysql.Escape(User.HashPassword(friendEmail))));

                    if (inviteKeysTable.Rows.Count > 0)
                    {
                        Display.ShowMessage("Cannot Invite Friend", "The person you have invited has opted-out of mailings from ZinZam.");
                        return;
                    }
                    else
                    {
                        Random rand = new Random();
                        string emailKey = User.HashPassword(friendEmail + rand.NextDouble().ToString());
                        emailKey = emailKey.Substring((int)(rand.NextDouble() * 10), 32);

                        RawTemplate emailTemplate = new RawTemplate(Server.MapPath("./templates/emails/"), "friend_invitation.eml");

                        if (!string.IsNullOrEmpty(friendName))
                        {
                            emailTemplate.Parse("TO_NAME", " " + friendName);
                        }

                        emailTemplate.Parse("FROM_NAME", LoggedInMember.DisplayName);
                        emailTemplate.Parse("FROM_EMAIL", LoggedInMember.AlternateEmail);
                        emailTemplate.Parse("FROM_NAMES", LoggedInMember.DisplayNameOwnership);
                        emailTemplate.Parse("U_REGISTER", "http://zinzam.com/register/");
                        emailTemplate.Parse("U_PROFILE", "http://zinzam.com/" + LoggedInMember.UserName);
                        emailTemplate.Parse("U_OPTOUT", "http://zinzam.com/register/?mode=optout&key=" + emailKey);

                        Email.SendEmail(friendEmail, string.Format("{0} has invited you to ZinZam.",
                            LoggedInMember.DisplayName),
                            emailTemplate.ToString());

                        db.UpdateQuery(string.Format("INSERT INTO invite_keys (email_key, invite_allow, email_hash, invite_time_ut) VALUES ('{0}', 1, '{1}', {2});",
                            Mysql.Escape(emailKey), Mysql.Escape(User.HashPassword(friendEmail)), Mysql.Escape(UnixTime.UnixTimeStamp().ToString())));
                    }
                }
                else
                {
                    Display.ShowMessage("Already Member", string.Format("This person is already a member of ZinZam. To add them to your friends list <a href=\"{0}\">click here</a>.",
                        Linker.BuildAddFriendUri(User.lastEmailId)));
                    return;
                }
            }
            else
            {
                Display.ShowMessage("Cannot Invite Friend", "You must enter a valid e-mail address to invite.");
                return;
            }

            SetRedirectUri(BuildUri());
            Display.ShowMessage("Invited Friend", "You have invited a friend to ZinZam.");
        }

        private void InviteFriendsSend(string[] friendEmails)
        {
            foreach (string friendEmail in friendEmails)
            {
                //Response.Write(friendEmail + "<br />"); // DEBUG
                if (User.CheckEmailValid(friendEmail))
                {
                    if (User.CheckEmailUnique(core, friendEmail))
                    {
                        DataTable inviteKeysTable = db.Query(string.Format("SELECT email_key FROM invite_keys WHERE email_hash = '{0}' AND invite_allow = 0",
                            Mysql.Escape(User.HashPassword(friendEmail))));

                        if (inviteKeysTable.Rows.Count > 0)
                        {
                            // ignore ignore invites, plough on
                        }
                        else
                        {
                            Random rand = new Random();
                            string emailKey = User.HashPassword(friendEmail + rand.NextDouble().ToString());
                            emailKey = emailKey.Substring((int)(rand.NextDouble() * 10), 32);

                            RawTemplate emailTemplate = new RawTemplate(Server.MapPath("./templates/emails/"), "friend_invitation.eml");

                            emailTemplate.Parse("FROM_NAME", LoggedInMember.DisplayName);
                            emailTemplate.Parse("FROM_EMAIL", LoggedInMember.AlternateEmail);
                            emailTemplate.Parse("FROM_NAMES", LoggedInMember.DisplayNameOwnership);
                            emailTemplate.Parse("U_REGISTER", "http://zinzam.com/register/");
                            emailTemplate.Parse("U_PROFILE", "http://zinzam.com/" + LoggedInMember.UserName);
                            emailTemplate.Parse("U_OPTOUT", "http://zinzam.com/register/?mode=optout&key=" + emailKey);

                            Email.SendEmail(friendEmail, string.Format("{0} has invited you to ZinZam.",
                                LoggedInMember.DisplayName),
                                emailTemplate.ToString());

                            db.UpdateQuery(string.Format("INSERT INTO invite_keys (email_key, invite_allow, email_hash) VALUES ('{0}', 1, '{1}');",
                                Mysql.Escape(emailKey), Mysql.Escape(User.HashPassword(friendEmail))));
                        }
                    }
                    else
                    {
                        // ignore already a member, plough on
                        if (friendEmail.ToLower() != LoggedInMember.AlternateEmail.ToLower())
                        {
                            SelectQuery query = User.GetSelectQueryStub(UserLoadOptions.Info);
                            query.AddCondition("LCASE(user_alternate_email)", Mysql.Escape(friendEmail.ToLower()));

                            DataTable friendTable = db.Query(query);

                            if (friendTable.Rows.Count == 1)
                            {
                                User friendProfile = new User(core, friendTable.Rows[0], UserLoadOptions.Info);
                                long friendId = friendProfile.UserId;

                                db.BeginTransaction();
                                long relationId = db.UpdateQuery(string.Format("INSERT INTO user_relations (relation_me, relation_you, relation_time_ut, relation_type) VALUES ({0}, {1}, UNIX_TIMESTAMP(), 'FRIEND');",
                                    LoggedInMember.UserId, friendId));

                                db.UpdateQuery(string.Format("INSERT INTO friend_notifications (relation_id, notification_time_ut, notification_read) VALUES ({0}, UNIX_TIMESTAMP(), 0)",
                                    relationId));

                                db.UpdateQuery(string.Format("UPDATE user_info ui SET ui.user_friends = ui.user_friends + 1 WHERE ui.user_id = {0};",
                                    LoggedInMember.UserId));

                                // send e-mail notification
                                // only send a notification if they have subscribed to them
                                if (friendProfile.EmailNotifications)
                                {
                                    RawTemplate emailTemplate = new RawTemplate(Server.MapPath("./templates/emails/"), "friend_notification.eml");

                                    emailTemplate.Parse("TO_NAME", friendProfile.DisplayName);
                                    emailTemplate.Parse("FROM_NAME", LoggedInMember.DisplayName);
                                    emailTemplate.Parse("FROM_USERNAME", LoggedInMember.UserName);

                                    Email.SendEmail(friendProfile.AlternateEmail, string.Format("{0} added you as a friend on ZinZam.",
                                        LoggedInMember.DisplayName),
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

            SetRedirectUri(BuildUri());
            Display.ShowMessage("Invited Friend", "You have invited all your friends to ZinZam.");
        }
    }
}
