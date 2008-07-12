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
using System.Web;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Applications.Profile
{
    [AccountSubModule("profile", "lifestyle")]
    public class AccountLifestyle : AccountSubModule
    {
        public override string Title
        {
            get
            {
                return "Lifestyle";
            }
        }

        public override int Order
        {
            get
            {
                return 6;
            }
        }

        public AccountLifestyle()
        {
            this.Load += new EventHandler(AccountLifestyle_Load);
            this.Show += new EventHandler(AccountLifestyle_Show);
        }

        void AccountLifestyle_Load(object sender, EventArgs e)
        {
            AddModeHandler("confirm-relationship", new ModuleModeHandler(AccountLifestyle_ConfirmRelationship));
            AddSaveHandler("confirm-relationship", new EventHandler(AccountLifestyle_ConfirmRelationship_Save));
        }

        void AccountLifestyle_Show(object sender, EventArgs e)
        {
            SetTemplate("account_lifestyle");

            Dictionary<string, string> maritialStatuses = new Dictionary<string, string>();
            maritialStatuses.Add("UNDEF", "No Answer");
            maritialStatuses.Add("SINGLE", "Single");
            maritialStatuses.Add("RELATIONSHIP", "In a Relationship");
            maritialStatuses.Add("MARRIED", "Married");
            maritialStatuses.Add("SWINGER", "Swinger");
            maritialStatuses.Add("DIVORCED", "Divorced");
            maritialStatuses.Add("WIDOWED", "Widowed");

            Dictionary<string, string> religions = new Dictionary<string, string>();
            religions.Add("0", "No Answer");

            DataTable religionsTable = db.Query("SELECT * FROM religions ORDER BY religion_title ASC");

            foreach (DataRow religionRow in religionsTable.Rows)
            {
                religions.Add(((short)religionRow["religion_id"]).ToString(), (string)religionRow["religion_title"]);
            }

            Dictionary<string, string> sexualities = new Dictionary<string, string>();
            sexualities.Add("UNDEF", "No Answer");
            sexualities.Add("UNSURE", "Unsure");
            sexualities.Add("STRAIGHT", "Straight");
            sexualities.Add("HOMOSEXUAL", "Homosexual");
            sexualities.Add("BISEXUAL", "Bisexual");
            sexualities.Add("TRANSEXUAL", "Transexual");

            Display.ParseSelectBox(template, "S_MARITIAL_STATUS", "maritial-status", maritialStatuses, loggedInMember.MaritialStatusRaw);
            Display.ParseSelectBox(template, "S_RELIGION", "religion", religions, loggedInMember.ReligionRaw.ToString());
            Display.ParseSelectBox(template, "S_SEXUALITY", "sexuality", sexualities, loggedInMember.SexualityRaw);

            if (loggedInMember.Profile.MaritialWithConfirmed && loggedInMember.Profile.MaritialWithId > 0)
            {
                core.LoadUserProfile(loggedInMember.Profile.MaritialWithId);

                template.Parse("S_RELATIONSHIP_WITH", core.UserProfiles[loggedInMember.Profile.MaritialWithId].UserName);
            }

            Save(new EventHandler(AccountLifestyle_Save));
        }

        void AccountLifestyle_Save(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            string relationshipWith = Request.Form["relationship-with"];
            User relation = null;

            if (!string.IsNullOrEmpty(relationshipWith))
            {
                long key = core.LoadUserProfile(relationshipWith);
                relation = core.UserProfiles[key];
            }

            string existingMaritialStatus = loggedInMember.Profile.MaritialStatusRaw;
            long existingMaritialWith = loggedInMember.Profile.MaritialWithId;

            loggedInMember.Profile.ReligionId = short.Parse(Request.Form["religion"]);
            loggedInMember.Profile.SexualityRaw = Request.Form["sexuality"];
            loggedInMember.Profile.MaritialStatusRaw = Request.Form["maritial-status"];

            if (relation != null)
            {
                if (loggedInMember.Id != relation.Id)
                {
                    loggedInMember.Profile.MaritialWithId = relation.Id;
                }
                else
                {
                    loggedInMember.Profile.MaritialWithId = 0;
                }
            }
            else
            {
                loggedInMember.Profile.MaritialWithId = 0;
            }

            switch (Request.Form["maritial-status"])
            {
                case "RELATIONSHIP":
                case "MARRIED":
                    if (relation != null && relation.Id != existingMaritialWith)
                    {
                        ApplicationEntry ae = new ApplicationEntry(core, core.session.LoggedInMember, "Profile");

                        RawTemplate atpl = new RawTemplate("emails/user_relationship_notification.eml");

                        atpl.Parse("USER_ID", core.LoggedInMemberId.ToString());
                        atpl.Parse("U_CONFIRM", AccountModule.BuildModuleUri("profile", "lifestyle", "mode=confirm-relationship", "id=" + core.LoggedInMemberId.ToString()));

                        ae.SendNotification(relation, string.Format("[user]{0}[/user] wants to be in a relationship with you", core.LoggedInMemberId), atpl.ToString());

                        if (existingMaritialWith > 0)
                        {
                            core.LoadUserProfile(existingMaritialWith);
                            User oldRelation = core.UserProfiles[existingMaritialWith];

                            oldRelation.Profile.MaritialWithId = 0;
                            oldRelation.Profile.MaritialWithConfirmed = false;
                            oldRelation.Profile.MaritialStatusRaw = "";

                            oldRelation.Profile.Update();
                        }
                    }
                    else
                    {
                        if (existingMaritialWith > 0)
                        {
                            core.LoadUserProfile(existingMaritialWith);
                            User oldRelation = core.UserProfiles[existingMaritialWith];

                            oldRelation.Profile.MaritialWithId = 0;
                            oldRelation.Profile.MaritialWithConfirmed = false;
                            oldRelation.Profile.MaritialStatusRaw = "";

                            oldRelation.Profile.Update();
                        }
                    }
                    break;
                default:
                    switch (existingMaritialStatus)
                    {
                        case "RELATIONSHIP":
                        case "MARRIED":
                            if (existingMaritialWith > 0)
                            {
                                core.LoadUserProfile(existingMaritialWith);
                                relation = core.UserProfiles[existingMaritialWith];

                                loggedInMember.Profile.MaritialWithId = 0;
                                loggedInMember.Profile.MaritialWithConfirmed = false;

                                relation.Profile.MaritialWithId = 0;
                                relation.Profile.MaritialWithConfirmed = false;
                                relation.Profile.MaritialStatusRaw = "";

                                relation.Profile.Update();
                            }
                            break;
                        default:
                            // Ignore if empty or null
                            break;
                    }
                    break;
            }

            loggedInMember.Profile.Update();

            SetRedirectUri(BuildUri());
            Display.ShowMessage("Lifestyle Saved", "Your lifestyle has been saved in the database.");
        }

        void AccountLifestyle_ConfirmRelationship(object sender, EventArgs e)
        {
            long id = Functions.RequestLong("id", 0);

            if (id == 0)
            {
                DisplayGenericError();
            }

            core.LoadUserProfile(id);

            User relation = core.UserProfiles[id];

            Dictionary<string, string> hiddenFieldList = new Dictionary<string, string>();
            hiddenFieldList.Add("module", ModuleKey);
            hiddenFieldList.Add("sub", Key);
            hiddenFieldList.Add("mode", "confirm-relationship");
            hiddenFieldList.Add("id", relation.Id.ToString());

            Display.ShowConfirmBox(Linker.AppendSid("/account", true),
                "Confirm relationship",
                string.Format("Confirm your relationship with {0}",
                relation.DisplayName), hiddenFieldList);
        }

        void AccountLifestyle_ConfirmRelationship_Save(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            long id = Functions.FormLong("id", 0);

            if (id == 0)
            {
                DisplayGenericError();
            }

            core.LoadUserProfile(id);

            User relation = core.UserProfiles[id];

            if (Display.GetConfirmBoxResult() == ConfirmBoxResult.Yes)
            {
                relation.Profile.MaritialWithConfirmed = true;

                relation.Profile.Update();

                loggedInMember.Profile.MaritialStatusRaw = relation.Profile.MaritialStatusRaw;
                loggedInMember.Profile.MaritialWithId = relation.Id;
                loggedInMember.Profile.MaritialWithConfirmed = true;

                loggedInMember.Profile.Update();

                SetRedirectUri(AccountModule.BuildModuleUri("dashboard"));
                Display.ShowMessage("Maritial Status updated", "You have successfully updated your maritial status.");
            }
            else
            {
                relation.Profile.MaritialWithConfirmed = false;
                relation.Profile.MaritialWithId = 0;

                relation.Profile.Update();

                Display.ShowMessage("Maritial Status unchanged", "You have not updated your maritial status.");
            }
        }
    }
}
