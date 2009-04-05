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
using System.Collections;
using System.Configuration;
using System.Data;
using System.Web;
using BoxSocial;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.FrontEnd
{
    public partial class login : TPage
    {
        public login()
            : base("login.html")
        {
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            string redirect = (Request.Form["redirect"] != null) ? Request.Form["redirect"] : Request.QueryString["redirect"];
            string domain = (Request.Form["domain"] != null) ? Request.Form["domain"] : Request.QueryString["domain"];
            DnsRecord record = null;

            if (!string.IsNullOrEmpty(domain))
            {
                try
                {
					if (domain != Linker.Domain)
					{
						record = new DnsRecord(core, domain);
					}
                    if (Request.QueryString["mode"] == "sign-out")
                    {
						if (record != null)
						{
							session.SessionEnd(Request.QueryString["sid"], loggedInMember.UserId, record);
						}
						else
						{
							session.SessionEnd(Request.QueryString["sid"], loggedInMember.UserId);
						}

                        if (!string.IsNullOrEmpty(redirect))
                        {
                            Response.Redirect(Linker.AppendSid("http://" + record.Domain + "/" + redirect.TrimStart(new char[] { '/' }), true));
                        }
                        else
                        {
                            Response.Redirect(Linker.AppendSid("http://" + record.Domain + "/", true));
                        }
                    }
                    else if (core.LoggedInMemberId > 0)
                    {
                        string sessionId = Request.QueryString["sid"];

                        if (!string.IsNullOrEmpty(sessionId))
                        {
                            core.session.SessionEnd(sessionId, 0, record);
                        }

                        sessionId = core.session.SessionBegin(core.LoggedInMemberId, false, false, false, record);

                        Response.Redirect(Linker.AppendSid("http://" + record.Domain + "/" + redirect.TrimStart(new char[] { '/' }), true));
                    }
                }
                catch (InvalidDnsRecordException)
                {
                    Display.ShowMessage("Error", "Error starting remote session");
                    return;
                }
            }

            if (Request.QueryString["mode"] == "sign-out")
            {
                string sessionId = Request.QueryString["sid"];

                if (!string.IsNullOrEmpty(sessionId))
                {
                    core.session.SessionEnd(sessionId, loggedInMember.UserId);
                }

                if (!string.IsNullOrEmpty(redirect))
                {
                    Response.Redirect(redirect, true);
                }
                else
                {
                    Response.Redirect("/", true);
                }
                return;
            }
            if (Request.Form["submit"] != null)
            {
                if (Request.QueryString["mode"] == "reset-password")
                {
                    string email = Request.Form["email"];

                    if (string.IsNullOrEmpty(email))
                    {
                        Display.ShowMessage("Error", "An error occured");
                        return;
                    }
                    else
                    {
                        try
                        {
                            UserEmail userEmail = new UserEmail(core, email);

                            if (userEmail.IsActivated)
                            {
                                string newPassword = BoxSocial.Internals.User.GenerateRandomPassword();
                                string activateCode = BoxSocial.Internals.User.GenerateActivationSecurityToken();

                                db.UpdateQuery(string.Format("UPDATE user_info SET user_new_password = '{0}', user_activate_code = '{1}' WHERE user_id = {2}",
                                    Mysql.Escape(newPassword), Mysql.Escape(activateCode), userEmail.Owner.Id));

                                string activateUri = string.Format(Linker.Uri + "register/?mode=activate-password&id={0}&key={1}",
                                    userEmail.Owner.Id, activateCode);

                                // send the e-mail

                                RawTemplate emailTemplate = new RawTemplate(HttpContext.Current.Server.MapPath("./templates/emails/"), "new_password.eml");
                                emailTemplate.Parse("TO_NAME", userEmail.Owner.DisplayName);
                                emailTemplate.Parse("U_ACTIVATE", activateUri);
                                emailTemplate.Parse("USERNAME", userEmail.Owner.UserName);
                                emailTemplate.Parse("PASSWORD", newPassword);

                                Email.SendEmail(userEmail.Email, "Password Reset", emailTemplate.ToString());

                                Display.ShowMessage("Password reset", "You have been sent an e-mail to the address you entered with your new password. You will need to click the confirmation link before you can sign in");
                                return;
                            }
                            else
                            {
                                Display.ShowMessage("E-mail not verified", "The e-mail you have entered has not been verified, you need to enter an e-mail address you have verified to reset your password.");
                                return;
                            }
                        }
                        catch (InvalidUserEmailException)
                        {
                            Display.ShowMessage("No e-mail registered", "The e-mail you have entered is not associated with a user account.");
                            return;
                        }
                    }
                }
                else
                {
                    string userName = Request.Form["username"];
                    string password = BoxSocial.Internals.User.HashPassword(Request.Form["password"]);

                    DataTable userTable = db.Query(string.Format("SELECT uk.user_name, uk.user_id, ui.user_password FROM user_keys uk INNER JOIN user_info ui ON uk.user_id = ui.user_id WHERE uk.user_name = '{0}';",
                       userName));

                    if (userTable.Rows.Count == 1)
                    {
						DataRow userRow = userTable.Rows[0];
						bool authenticated = false;
						string dbPassword = (string)userRow["user_password"];
						
						// old phpBB passwords
						if (dbPassword.Length == 32)
						{
							// phpBB2 passwords
							if (SessionState.SessionMd5(Request.Form["password"]) == dbPassword.ToLower())
							{
								authenticated = true;
							}
						}
						else if (dbPassword.Length == 34)
						{
									// phpBB3 passwords
							string itoa64 = "./0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
							
							if (SessionState.phpBB3Hash(Request.Form["password"], dbPassword, ref itoa64) == dbPassword)
							{
								authenticated = true;
							}
						}
						else
						{
							if (dbPassword == password)
							{
								authenticated = true;
							}
						}
						
						if (authenticated)
						{
                        if (Request.Form["remember"] == "true")
                        {
                            session.SessionBegin((long)userRow["user_id"], false, true);
                        }
                        else
                        {
                            session.SessionBegin((long)userRow["user_id"], false, false);
                        }
                        if ((!string.IsNullOrEmpty(domain)) && (record != null))
                        {
                            string sessionId = core.session.SessionBegin((long)userRow["user_id"], false, false, false, record);

                            Linker.Sid = sessionId;
                            if (!string.IsNullOrEmpty(redirect))
                            {
                                Response.Redirect(Linker.AppendSid("http://" + record.Domain + "/" + redirect.TrimStart(new char[] { '/' }), true));
                            }
                            else
                            {
                                Response.Redirect(Linker.AppendSid("http://" + record.Domain + "/", true));
                            }
                            return;
                        }
                        if (!string.IsNullOrEmpty(redirect))
                        {
                            if (redirect.StartsWith("/account"))
                            {
                                redirect = Linker.AppendSid(Linker.StripSid(redirect), true);
                            }
                            Response.Redirect(redirect, true);
                        }
                        else
                        {
                            Response.Redirect("/", true);
                        }
                        return; /* stop processing the display of this page */
						}
						else
						{
							template.Parse("ERROR", "Bad log in credentials were given, you could not be logged in. Try again.");
						}
                    }
                    else
                    {
                        template.Parse("ERROR", "Bad log in credentials were given, you could not be logged in. Try again.");
                    }
                }
            }

            if (Request.QueryString["mode"] == "reset-password")
            {
                template.SetTemplate("password_reset.html");

                EndResponse();
                return;
            }
            else
            {
                template.Parse("U_FORGOT_PASSWORD", Linker.AppendSid("/sign-in/?mode=reset-password"));
            }

            template.Parse("DOMAIN", domain);
            template.Parse("REDIRECT", redirect);

            EndResponse();
        }
    }
}
