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

        /// <summary>
        /// Initializes a new instance of the AccountLifestyle class. 
        /// </summary>
        /// <param name="core">The Core token.</param>
        public AccountLifestyle(Core core)
            : base(core)
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
            if (core.IsAjax)
            {
                AccountLifestyle_SaveParameter(sender, e);
                return;
            }

			Save(new EventHandler(AccountLifestyle_Save));
			
            SetTemplate("account_lifestyle");

            SelectBox maritialStatusesSelectBox = new SelectBox("maritial-status");
            maritialStatusesSelectBox.Add(new SelectBoxItem(((byte)MaritialStatus.Undefined).ToString(), core.Prose.GetString("NO_ANSWER")));
            maritialStatusesSelectBox.Add(new SelectBoxItem(((byte)MaritialStatus.Single).ToString(), core.Prose.GetString("SINGLE")));
            maritialStatusesSelectBox.Add(new SelectBoxItem(((byte)MaritialStatus.MonogomousRelationship).ToString(), core.Prose.GetString("IN_A_RELATIONSHIP")));
            maritialStatusesSelectBox.Add(new SelectBoxItem(((byte)MaritialStatus.OpenRelationship).ToString(), core.Prose.GetString("IN_A_OPEN_RELATIONSHIP")));
            maritialStatusesSelectBox.Add(new SelectBoxItem(((byte)MaritialStatus.Engaged).ToString(), core.Prose.GetString("ENGAGED")));
            maritialStatusesSelectBox.Add(new SelectBoxItem(((byte)MaritialStatus.Married).ToString(), core.Prose.GetString("MARRIED")));
            maritialStatusesSelectBox.Add(new SelectBoxItem(((byte)MaritialStatus.Separated).ToString(), core.Prose.GetString("SEPARATED")));
            maritialStatusesSelectBox.Add(new SelectBoxItem(((byte)MaritialStatus.Divorced).ToString(), core.Prose.GetString("DIVORCED")));
            maritialStatusesSelectBox.Add(new SelectBoxItem(((byte)MaritialStatus.Widowed).ToString(), core.Prose.GetString("WIDOWED")));
            maritialStatusesSelectBox.SelectedKey = ((byte)LoggedInMember.Profile.MaritialStatusRaw).ToString();

            SelectBox religionsSelectBox = new SelectBox("religion");
            religionsSelectBox.Add(new SelectBoxItem("0", core.Prose.GetString("NO_ANSWER")));

            // TODO: Fix this
            DataTable religionsTable = db.Query("SELECT * FROM religions ORDER BY religion_title ASC");

            foreach (DataRow religionRow in religionsTable.Rows)
            {
                religionsSelectBox.Add(new SelectBoxItem(((short)religionRow["religion_id"]).ToString(), (string)religionRow["religion_title"]));
            }

            religionsSelectBox.SelectedKey = LoggedInMember.Profile.ReligionId.ToString();
            religionsSelectBox.Script.OnChange = "SaveParameter('profile', 'lifestyle', '" + religionsSelectBox.Name + "');";

            SelectBox sexualitiesSelectBox = new SelectBox("sexuality");
            sexualitiesSelectBox.Add(new SelectBoxItem(((byte)Sexuality.Undefined).ToString(), core.Prose.GetString("NO_ANSWER")));
            sexualitiesSelectBox.Add(new SelectBoxItem(((byte)Sexuality.Unsure).ToString(), core.Prose.GetString("NOT_SURE")));
            sexualitiesSelectBox.Add(new SelectBoxItem(((byte)Sexuality.Asexual).ToString(), core.Prose.GetString("ASEXUAL")));
            sexualitiesSelectBox.Add(new SelectBoxItem(((byte)Sexuality.Hetrosexual).ToString(), core.Prose.GetString("STRAIGHT")));
            sexualitiesSelectBox.Add(new SelectBoxItem(((byte)Sexuality.Homosexual).ToString(), LoggedInMember.Profile.GenderRaw == Gender.Female ? core.Prose.GetString("LESBIAN") : core.Prose.GetString("GAY")));
            sexualitiesSelectBox.Add(new SelectBoxItem(((byte)Sexuality.Bisexual).ToString(), core.Prose.GetString("BISEXUAL")));
            sexualitiesSelectBox.Add(new SelectBoxItem(((byte)Sexuality.Pansexual).ToString(), core.Prose.GetString("PANSEXUAL")));
            sexualitiesSelectBox.Add(new SelectBoxItem(((byte)Sexuality.Polysexual).ToString(), core.Prose.GetString("POLYSEXUAL")));
    		sexualitiesSelectBox.SelectedKey = ((byte)LoggedInMember.Profile.SexualityRaw).ToString();
            sexualitiesSelectBox.Script.OnChange = "SaveParameter('profile', 'lifestyle', '" + sexualitiesSelectBox.Name + "');";

            CheckBoxArray interestedInCheckBoxes = new CheckBoxArray("interested-in");
            interestedInCheckBoxes.Layout = Layout.Horizontal;
            
            CheckBox interestedInMenCheckBox = new CheckBox("interested-in-men");
            interestedInMenCheckBox.Caption = core.Prose.GetString("MEN");
            interestedInMenCheckBox.IsChecked = LoggedInMember.Profile.InterestedInMen;
            interestedInMenCheckBox.Width = new StyleLength();
            interestedInMenCheckBox.Script.OnChange = "SaveParameter('profile', 'lifestyle', '" + interestedInMenCheckBox.Name + "');";

            CheckBox interestedInWomenCheckBox = new CheckBox("interested-in-women");
            interestedInWomenCheckBox.Caption = core.Prose.GetString("WOMEN");
            interestedInWomenCheckBox.IsChecked = LoggedInMember.Profile.InterestedInWomen;
            interestedInWomenCheckBox.Width = new StyleLength();
            interestedInWomenCheckBox.Script.OnChange = "SaveParameter('profile', 'lifestyle', '" + interestedInWomenCheckBox.Name + "');";

            interestedInCheckBoxes.Add(interestedInMenCheckBox);
            interestedInCheckBoxes.Add(interestedInWomenCheckBox);

            UserSelectBox relationUserSelectBox = new UserSelectBox(core, "relation");
            relationUserSelectBox.Width = new StyleLength();
            relationUserSelectBox.SelectMultiple = false;
            relationUserSelectBox.IsVisible = false;
            relationUserSelectBox.Script.OnChange = "SaveParameter('profile', 'lifestyle', '" + relationUserSelectBox.Name + "');";

            maritialStatusesSelectBox.Script.OnChange = "SaveParameter('profile', 'lifestyle', '" + maritialStatusesSelectBox.Name + "'); CheckRelationship('" + maritialStatusesSelectBox.Name + "', '" + relationUserSelectBox.Name + "');";

            template.Parse("S_MARITIAL_STATUS", maritialStatusesSelectBox);
            template.Parse("S_RELIGION", religionsSelectBox);
            template.Parse("S_SEXUALITY", sexualitiesSelectBox);
            template.Parse("S_INTERESTED_IN", interestedInCheckBoxes);

            switch (LoggedInMember.Profile.MaritialStatusRaw)
            {
                case MaritialStatus.MonogomousRelationship:
                case MaritialStatus.OpenRelationship:
                case MaritialStatus.Engaged:
                case MaritialStatus.Married:
                    relationUserSelectBox.IsVisible = true;
                    break;
            }

            if (LoggedInMember.Profile.MaritialWithConfirmed && LoggedInMember.Profile.MaritialWithId > 0)
            {
                relationUserSelectBox.Invitees = new List<long>(new long[] { LoggedInMember.Profile.MaritialWithId });
                core.LoadUserProfile(LoggedInMember.Profile.MaritialWithId);

                template.Parse("S_RELATIONSHIP_WITH", core.PrimitiveCache[LoggedInMember.Profile.MaritialWithId].UserName);
            }

            template.Parse("S_RELATION", relationUserSelectBox);
        }

        void AccountLifestyle_SaveParameter(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            MaritialStatus existingMaritialStatus = LoggedInMember.Profile.MaritialStatusRaw;
            long existingMaritialWith = LoggedInMember.Profile.MaritialWithId;

            switch (core.Http.Form["parameter"])
            {
                case "religion":
                    LoggedInMember.Profile.ReligionId = short.Parse(core.Http.Form["value"]);
                    break;
                case "maritial-status":
                    LoggedInMember.Profile.MaritialStatusRaw = (MaritialStatus)byte.Parse(core.Http.Form["value"]);

                    ProcessUpdateMaritialStatus(existingMaritialStatus, (MaritialStatus)byte.Parse(core.Http.Form["value"]), existingMaritialWith, existingMaritialWith);
                    break;
                case "relation":
                    long relationId = UserSelectBox.FormUser(core, "value", 0);

                    ProcessUpdateMaritialStatus(existingMaritialStatus, existingMaritialStatus, existingMaritialWith, relationId);
                    break;
                case "interested-in-men":
                    LoggedInMember.Profile.InterestedInMen = (core.Http.Form["value"] == "true");
                    break;
                case "interested-in-women":
                    LoggedInMember.Profile.InterestedInWomen = (core.Http.Form["value"] == "true");
                    break;
                case "sexuality":
                    LoggedInMember.Profile.SexualityRaw = (Sexuality)byte.Parse(core.Http.Form["value"]);
                    break;
                default:
                    core.Ajax.SendStatus("FAIL");
                    return;
            }

            try
            {
                LoggedInMember.Profile.Update();

                core.Ajax.SendStatus("SUCCESS");
            }
            catch (UnauthorisedToUpdateItemException)
            {
                core.Ajax.SendStatus("FAIL");
            }
        }

        private void ProcessUpdateMaritialStatus(MaritialStatus existingMaritialStatus, MaritialStatus newMaritialStatus, long existingMaritialWith, long newMaritialWith)
        {
            long relationId = newMaritialWith;

            User relation = null;

            if (relationId > 0)
            {
                core.PrimitiveCache.LoadUserProfile(relationId);
                relation = core.PrimitiveCache[relationId];
            }

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

            switch (newMaritialStatus)
            {
                case MaritialStatus.MonogomousRelationship:
                case MaritialStatus.OpenRelationship:
                case MaritialStatus.Engaged:
                case MaritialStatus.Married:
                    if (relation != null && relation.Id != existingMaritialWith)
                    {
                        ApplicationEntry ae = core.GetApplication("Profile");

                        RawTemplate atpl = new RawTemplate(core.Http.TemplateEmailPath, "user_relationship_notification.html");

                        atpl.Parse("USER_ID", core.LoggedInMemberId.ToString());
                        atpl.Parse("U_CONFIRM", core.Hyperlink.BuildAccountSubModuleUri("profile", "lifestyle", "confirm-relationship", core.LoggedInMemberId));

                        ae.SendNotification(core, relation, string.Format("[user]{0}[/user] wants to be in a relationship with you", core.LoggedInMemberId), atpl.ToString());

                        if (existingMaritialWith > 0)
                        {
                            core.LoadUserProfile(existingMaritialWith);
                            User oldRelation = core.PrimitiveCache[existingMaritialWith];

                            oldRelation.Profile.MaritialWithId = 0;
                            oldRelation.Profile.MaritialWithConfirmed = false;
                            oldRelation.Profile.MaritialStatusRaw = MaritialStatus.Undefined;

                            oldRelation.Profile.Update();
                        }
                    }
                    else
                    {
                        LoggedInMember.Profile.MaritialWithId = 0;
                        LoggedInMember.Profile.MaritialWithConfirmed = false;

                        if (existingMaritialWith > 0)
                        {
                            core.LoadUserProfile(existingMaritialWith);
                            User oldRelation = core.PrimitiveCache[existingMaritialWith];

                            oldRelation.Profile.MaritialWithId = 0;
                            oldRelation.Profile.MaritialWithConfirmed = false;
                            oldRelation.Profile.MaritialStatusRaw = MaritialStatus.Undefined;

                            oldRelation.Profile.Update();
                        }
                    }
                    break;
                default:
                    switch (existingMaritialStatus)
                    {
                        case MaritialStatus.MonogomousRelationship:
                        case MaritialStatus.OpenRelationship:
                        case MaritialStatus.Engaged:
                        case MaritialStatus.Married:
                            if (existingMaritialWith > 0)
                            {
                                core.LoadUserProfile(existingMaritialWith);
                                relation = core.PrimitiveCache[existingMaritialWith];

                                LoggedInMember.Profile.MaritialWithId = 0;
                                LoggedInMember.Profile.MaritialWithConfirmed = false;

                                relation.Profile.MaritialWithId = 0;
                                relation.Profile.MaritialWithConfirmed = false;
                                relation.Profile.MaritialStatusRaw = MaritialStatus.Undefined;

                                relation.Profile.Update();
                            }
                            break;
                        default:
                            // Ignore if empty or null
                            break;
                    }
                    break;
            }
        }

        void AccountLifestyle_Save(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            long relationId = UserSelectBox.FormUser(core, "relation", 0);

            MaritialStatus existingMaritialStatus = LoggedInMember.Profile.MaritialStatusRaw;
            long existingMaritialWith = LoggedInMember.Profile.MaritialWithId;

            LoggedInMember.Profile.ReligionId = short.Parse(core.Http.Form["religion"]);
            LoggedInMember.Profile.SexualityRaw = (Sexuality)byte.Parse(core.Http.Form["sexuality"]);
            LoggedInMember.Profile.MaritialStatusRaw = (MaritialStatus)byte.Parse(core.Http.Form["maritial-status"]);
            LoggedInMember.Profile.InterestedInMen = core.Http.Form["interested-in-men"] != null;
            LoggedInMember.Profile.InterestedInWomen = core.Http.Form["interested-in-women"] != null;

            ProcessUpdateMaritialStatus(existingMaritialStatus, (MaritialStatus)byte.Parse(core.Http.Form["value"]), existingMaritialWith, relationId);

            LoggedInMember.Profile.Update();

			SetInformation("Your lifestyle has been saved in the database.");
            //SetRedirectUri(BuildUri());
            //Display.ShowMessage("Lifestyle Saved", "Your lifestyle has been saved in the database.");
        }

        void AccountLifestyle_ConfirmRelationship(object sender, EventArgs e)
        {
            long id = core.Functions.RequestLong("id", 0);

            if (id == 0)
            {
                DisplayGenericError();
            }

            core.LoadUserProfile(id);

            User relation = core.PrimitiveCache[id];

            Dictionary<string, string> hiddenFieldList = new Dictionary<string, string>();
            hiddenFieldList.Add("module", ModuleKey);
            hiddenFieldList.Add("sub", Key);
            hiddenFieldList.Add("mode", "confirm-relationship");
            hiddenFieldList.Add("id", relation.Id.ToString());

            core.Display.ShowConfirmBox(core.Hyperlink.AppendSid(Owner.AccountUriStub, true),
                "Confirm relationship",
                string.Format("Confirm your relationship status {1} with {0}",
                relation.DisplayName, relation.Profile.MaritialStatus), hiddenFieldList);
        }

        void AccountLifestyle_ConfirmRelationship_Save(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            long id = core.Functions.FormLong("id", 0);

            if (id == 0)
            {
                DisplayGenericError();
            }

            core.LoadUserProfile(id);

            User relation = core.PrimitiveCache[id];

            if (core.Display.GetConfirmBoxResult() == ConfirmBoxResult.Yes)
            {
                relation.Profile.MaritialWithConfirmed = true;

                relation.Profile.Update();

                LoggedInMember.Profile.MaritialStatusRaw = relation.Profile.MaritialStatusRaw;
                LoggedInMember.Profile.MaritialWithId = relation.Id;
                LoggedInMember.Profile.MaritialWithConfirmed = true;

                LoggedInMember.Profile.Update();

                SetRedirectUri(core.Hyperlink.BuildAccountModuleUri("dashboard"));
                core.Display.ShowMessage("Maritial Status updated", "You have successfully updated your maritial status.");
            }
            else
            {
                relation.Profile.MaritialWithConfirmed = false;
                relation.Profile.MaritialWithId = 0;

                relation.Profile.Update();

                core.Display.ShowMessage("Maritial Status unchanged", "You have not updated your maritial status.");
            }
        }
    }
}
