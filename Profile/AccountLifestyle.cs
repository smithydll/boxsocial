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
using BoxSocial.Forms;
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
			Save(new EventHandler(AccountLifestyle_Save));
			
            SetTemplate("account_lifestyle");

            SelectBox maritialStatusesSelectBox = new SelectBox("maritial-status");
            maritialStatusesSelectBox.Add(new SelectBoxItem("UNDEF", "No Answer"));
            maritialStatusesSelectBox.Add(new SelectBoxItem("SINGLE", "Single"));
            maritialStatusesSelectBox.Add(new SelectBoxItem("RELATIONSHIP", "In a Relationship"));
            maritialStatusesSelectBox.Add(new SelectBoxItem("MARRIED", "Married"));
            maritialStatusesSelectBox.Add(new SelectBoxItem("SWINGER", "Swinger"));
            maritialStatusesSelectBox.Add(new SelectBoxItem("DIVORCED", "Divorced"));
            maritialStatusesSelectBox.Add(new SelectBoxItem("WIDOWED", "Widowed"));

			if (LoggedInMember.MaritialStatusRaw != null)
			{
				maritialStatusesSelectBox.SelectedKey = LoggedInMember.MaritialStatusRaw;
			}

            SelectBox religionsSelectBox = new SelectBox("religion");
            religionsSelectBox.Add(new SelectBoxItem("0", "No Answer"));

            DataTable religionsTable = db.Query("SELECT * FROM religions ORDER BY religion_title ASC");

            foreach (DataRow religionRow in religionsTable.Rows)
            {
                religionsSelectBox.Add(new SelectBoxItem(((short)religionRow["religion_id"]).ToString(), (string)religionRow["religion_title"]));
            }

			if (LoggedInMember.ReligionRaw != null)
			{
				religionsSelectBox.SelectedKey = LoggedInMember.ReligionRaw.ToString();
			}

            SelectBox sexualitiesSelectBox = new SelectBox("sexuality");
            sexualitiesSelectBox.Add(new SelectBoxItem("UNDEF", "No Answer"));
            sexualitiesSelectBox.Add(new SelectBoxItem("UNSURE", "Unsure"));
            sexualitiesSelectBox.Add(new SelectBoxItem("STRAIGHT", "Straight"));
            sexualitiesSelectBox.Add(new SelectBoxItem("HOMOSEXUAL", "Homosexual"));
            sexualitiesSelectBox.Add(new SelectBoxItem("BISEXUAL", "Bisexual"));
            sexualitiesSelectBox.Add(new SelectBoxItem("TRANSEXUAL", "Transexual"));

			if (LoggedInMember.SexualityRaw != null)
			{
				sexualitiesSelectBox.SelectedKey = LoggedInMember.SexualityRaw;
			}

            template.Parse("S_MARITIAL_STATUS", maritialStatusesSelectBox);
            template.Parse("S_RELIGION", religionsSelectBox);
            template.Parse("S_SEXUALITY", sexualitiesSelectBox);

            if (LoggedInMember.Profile.MaritialWithConfirmed && LoggedInMember.Profile.MaritialWithId > 0)
            {
                core.LoadUserProfile(LoggedInMember.Profile.MaritialWithId);

                template.Parse("S_RELATIONSHIP_WITH", core.UserProfiles[LoggedInMember.Profile.MaritialWithId].UserName);
            }
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

            string existingMaritialStatus = LoggedInMember.Profile.MaritialStatusRaw;
            long existingMaritialWith = LoggedInMember.Profile.MaritialWithId;

            LoggedInMember.Profile.ReligionId = short.Parse(Request.Form["religion"]);
            LoggedInMember.Profile.SexualityRaw = Request.Form["sexuality"];
            LoggedInMember.Profile.MaritialStatusRaw = Request.Form["maritial-status"];

            if (relation != null)
            {
                if (LoggedInMember.Id != relation.Id)
                {
                    LoggedInMember.Profile.MaritialWithId = relation.Id;
                }
                else
                {
                    LoggedInMember.Profile.MaritialWithId = 0;
                }
            }
            else
            {
                LoggedInMember.Profile.MaritialWithId = 0;
            }

            switch (Request.Form["maritial-status"])
            {
                case "RELATIONSHIP":
                case "MARRIED":
                    if (relation != null && relation.Id != existingMaritialWith)
                    {
                        ApplicationEntry ae = new ApplicationEntry(core, LoggedInMember, "Profile");

                        RawTemplate atpl = new RawTemplate("emails/user_relationship_notification.eml");

                        atpl.Parse("USER_ID", core.LoggedInMemberId.ToString());
                        atpl.Parse("U_CONFIRM", Linker.BuildAccountSubModuleUri("profile", "lifestyle", "confirm-relationship", core.LoggedInMemberId));

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

                                LoggedInMember.Profile.MaritialWithId = 0;
                                LoggedInMember.Profile.MaritialWithConfirmed = false;

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

            LoggedInMember.Profile.Update();

			SetInformation("Your lifestyle has been saved in the database.");
            //SetRedirectUri(BuildUri());
            //Display.ShowMessage("Lifestyle Saved", "Your lifestyle has been saved in the database.");
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

            Display.ShowConfirmBox(Linker.AppendSid(Owner.AccountUriStub, true),
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

                LoggedInMember.Profile.MaritialStatusRaw = relation.Profile.MaritialStatusRaw;
                LoggedInMember.Profile.MaritialWithId = relation.Id;
                LoggedInMember.Profile.MaritialWithConfirmed = true;

                LoggedInMember.Profile.Update();

                SetRedirectUri(Linker.BuildAccountModuleUri("dashboard"));
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
