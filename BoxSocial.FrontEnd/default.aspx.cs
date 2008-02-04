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
using System.Reflection;
using System.Web;
using System.Web.Security;
using BoxSocial;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.FrontEnd
{
    public partial class _default : TPage
    {
        public _default() : base("default.html")
        {
            if (session.IsLoggedIn)
            {
                template.SetTemplate("today.html");
                this.Signature = PageSignature.today;

                BoxSocial.Internals.Application.LoadApplication(core, AppPrimitives.Member, new ApplicationEntry(db, session.LoggedInMember, "Calendar"));

                template.ParseVariables("DATE_STRING", tz.Now.ToLongDateString());
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (session.IsLoggedIn)
            {
                core.InvokeHooks(new HookEventArgs(core, AppPrimitives.None, null));

                Feed.Show(core, this, core.session.LoggedInMember);
            }
            else
            {
                ArrayList defaultImages = new ArrayList();
                defaultImages.Add("default_1.jpg");
                defaultImages.Add("default_2.jpg");
                defaultImages.Add("default_3.jpg");
                defaultImages.Add("default_4.jpg");
                defaultImages.Add("default_5.jpg");

                Random rand = new Random((int)DateTime.Now.Ticks);

                template.ParseVariables("I_DEFAULT", (string)defaultImages[rand.Next(defaultImages.Count)]);

                if (loggedInMember != null)
                {
                    template.ParseVariables("U_INVITE", HttpUtility.HtmlEncode(Linker.AppendSid("/account/?module=friends&sub=invite")));
                    template.ParseVariables("U_WRITE_BLOG", HttpUtility.HtmlEncode(Linker.AppendSid("/account/?module=blog&sub=write")));
                }

                // new_points

                DataTable newUsers = db.SelectQuery(string.Format("SELECT {0}, {1}, {2} FROM user_info ui INNER JOIN user_profile up ON ui.user_id = up.user_id LEFT JOIN (countries c, gallery_items gi) ON (c.country_iso = up.profile_country AND gi.gallery_item_id = ui.user_icon) WHERE up.profile_access & 4369 = 4369 AND gi.gallery_item_uri IS NOT NULL ORDER BY user_reg_date_ut DESC LIMIT 3",
                    Member.USER_PROFILE_FIELDS, Member.USER_INFO_FIELDS, Member.USER_ICON_FIELDS));

                for (int i = 0; i < newUsers.Rows.Count; i++)
                {
                    Member newMember = new Member(db, newUsers.Rows[i], true, true);

                    VariableCollection newPointsVariableCollection = template.CreateChild("new_points");

                    newPointsVariableCollection.ParseVariables("USER_DISPLAY_NAME", HttpUtility.HtmlEncode(newMember.DisplayName));
                    newPointsVariableCollection.ParseVariables("USER_AGE", HttpUtility.HtmlEncode(newMember.GetAgeString()));
                    newPointsVariableCollection.ParseVariables("USER_COUNTRY", HttpUtility.HtmlEncode(newMember.Country));
                    newPointsVariableCollection.ParseVariables("USER_CAPTION", "");
                    newPointsVariableCollection.ParseVariables("U_PROFILE", HttpUtility.HtmlEncode(Linker.BuildHomepageUri(newMember)));
                    newPointsVariableCollection.ParseVariables("ICON", HttpUtility.HtmlEncode(newMember.UserIcon));
                }
            }
            EndResponse();
        }
    }
}
