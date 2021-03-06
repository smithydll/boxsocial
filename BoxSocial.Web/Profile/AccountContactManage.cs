﻿/*
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
    [AccountSubModule("profile", "contact")]
    public class AccountContactManage : AccountSubModule
    {
        public override string Title
        {
            get
            {
                return "Contact Details";
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
        /// Initializes a new instance of the AccountContactManage class. 
        /// </summary>
        /// <param name="core">The Core token.</param>
        public AccountContactManage(Core core, Primitive owner)
            : base(core, owner)
        {
            this.Load += new EventHandler(AccountContactManage_Load);
            this.Show += new EventHandler(AccountContactManage_Show);
        }

        void AccountContactManage_Load(object sender, EventArgs e)
        {
            AddModeHandler("edit-address", new ModuleModeHandler(AccountContactManage_EditAddress));
            AddModeHandler("add-email", new ModuleModeHandler(AccountContactManage_AddEmail));
            AddModeHandler("edit-email", new ModuleModeHandler(AccountContactManage_AddEmail));
            AddModeHandler("verify-email", new ModuleModeHandler(AccountContactManage_VerifyEmail));
            AddModeHandler("delete-email", new ModuleModeHandler(AccountContactManage_DeleteEmail));
            AddModeHandler("add-phone", new ModuleModeHandler(AccountContactManage_AddPhone));
            AddModeHandler("edit-phone", new ModuleModeHandler(AccountContactManage_AddPhone));
            AddModeHandler("delete-phone", new ModuleModeHandler(AccountContactManage_DeletePhone));
            AddModeHandler("verify-phone", new ModuleModeHandler(AccountContactManage_VerifyPhone));
            AddModeHandler("add-link", new ModuleModeHandler(AccountContactManage_AddLink));
            AddModeHandler("edit-link", new ModuleModeHandler(AccountContactManage_AddLink));
            AddModeHandler("delete-link", new ModuleModeHandler(AccountContactManage_DeleteLink));

            AddSaveHandler("edit-address", new EventHandler(AccountContactManage_EditAddress_save));
            AddSaveHandler("add-email", new EventHandler(AccountContactManage_AddEmail_save));
            AddSaveHandler("edit-email", new EventHandler(AccountContactManage_AddEmail_save));
            AddSaveHandler("add-phone", new EventHandler(AccountContactManage_AddPhone_save));
            AddSaveHandler("edit-phone", new EventHandler(AccountContactManage_AddPhone_save));
            AddSaveHandler("add-link", new EventHandler(AccountContactManage_AddLink_save));
            AddSaveHandler("edit-link", new EventHandler(AccountContactManage_AddLink_save));
            AddSaveHandler("verify-phone", new EventHandler(AccountContactManage_VerifyPhone_Save));
        }

        void AccountContactManage_Show(object sender, EventArgs e)
        {
            SetTemplate("account_contact_manage");

            List<UserEmail> emails = core.Session.LoggedInMember.GetEmailAddresses();
            List<UserPhoneNumber> phoneNumbers = new List<UserPhoneNumber>();
            List<UserLink> links = LoggedInMember.GetLinks();

            SelectQuery query = Item.GetSelectQueryStub(core, typeof(UserPhoneNumber));
            query.AddCondition("phone_user_id", core.LoggedInMemberId);

            DataTable phoneNumbersDataTable = db.Query(query);

            foreach (DataRow dr in phoneNumbersDataTable.Rows)
            {
                phoneNumbers.Add(new UserPhoneNumber(core, dr));
            }

            foreach (UserEmail email in emails)
            {
                VariableCollection emailsVariableCollection = template.CreateChild("email_list");

                emailsVariableCollection.Parse("EMAIL_ID", email.Id.ToString());
                emailsVariableCollection.Parse("EMAIL_ADDRESS", email.Email);
                emailsVariableCollection.Parse("U_VERIFY", BuildUri("contact", "verify-email", email.Id));
                emailsVariableCollection.Parse("U_EDIT", BuildUri("contact", "edit-email", email.Id));
                emailsVariableCollection.Parse("U_EDIT_PERMISSIONS", Access.BuildAclUri(core, email));
                emailsVariableCollection.Parse("U_DELETE", BuildUri("contact", "delete-email", email.Id));

                if (email.IsActivated)
                {
                    emailsVariableCollection.Parse("IS_VERIFIED", "TRUE");
                }
            }

            foreach (UserPhoneNumber phoneNumber in phoneNumbers)
            {
                VariableCollection phoneNumbersVariableCollection = template.CreateChild("phone_list");

                phoneNumbersVariableCollection.Parse("PHONE_ID", phoneNumber.Id.ToString());
                phoneNumbersVariableCollection.Parse("PHONE_NUMBER", phoneNumber.PhoneNumber);
                phoneNumbersVariableCollection.Parse("U_VERIFY", BuildUri("contact", "verify-phone", phoneNumber.Id));
                phoneNumbersVariableCollection.Parse("U_EDIT", BuildUri("contact", "edit-phone", phoneNumber.Id));
                phoneNumbersVariableCollection.Parse("U_EDIT_PERMISSIONS", Access.BuildAclUri(core, phoneNumber));
                phoneNumbersVariableCollection.Parse("U_DELETE", BuildUri("contact", "delete-phone", phoneNumber.Id));

                if (phoneNumber.Validated)
                {
                    phoneNumbersVariableCollection.Parse("IS_VERIFIED", "TRUE");
                }
            }

            if (core.Sms != null)
            {
                template.Parse("VERIFY_PHONE_NUMBERS", "TRUE");
            }

            foreach (UserLink link in links)
            {
                VariableCollection linksVariableCollection = template.CreateChild("link_list");

                linksVariableCollection.Parse("LINK_ID", link.Id.ToString());
                linksVariableCollection.Parse("LINK", link.LinkAddress);
                linksVariableCollection.Parse("U_EDIT", BuildUri("contact", "edit-link", link.Id));
                linksVariableCollection.Parse("U_DELETE", BuildUri("contact", "delete-link", link.Id));

                if (!string.IsNullOrEmpty(link.Favicon))
                {
                    Image faviconImage = new Image("favicon-" + link.Id, core.Hyperlink.AppendAbsoluteSid("/images/favicons/" + link.Favicon));
                    linksVariableCollection.Parse("S_FAVICON", faviconImage);
                }
            }

            template.Parse("U_ADD_LINK", BuildUri("contact", "add-link"));
            template.Parse("U_ADD_EMAIL", BuildUri("contact", "add-email"));
            template.Parse("U_ADD_PHONE", BuildUri("contact", "add-phone"));
        }

        void AccountContactManage_EditAddress(object sender, ModuleModeEventArgs e)
        {
            SetTemplate("account_address_edit");

            User user = LoggedInMember;

            /* */
            TextBox addressLine1TextBox = new TextBox("address-1");
            addressLine1TextBox.Value = user.Profile.AddressLine1;

            /* */
            TextBox addressLine2TextBox = new TextBox("address-2");
            addressLine2TextBox.Value = user.Profile.AddressLine2;

            /* */
            TextBox townTextBox = new TextBox("town");
            townTextBox.Value = user.Profile.AddressTown;

            /* */
            TextBox stateTextBox = new TextBox("state");
            stateTextBox.Value = user.Profile.AddressState;

            /* */
            TextBox postCodeTextBox = new TextBox("post-code");
            postCodeTextBox.MaxLength = 5;
            postCodeTextBox.Value = user.Profile.AddressPostCode;

            /* */
            SelectBox countrySelectBox = new SelectBox("country");

            List<Country> countries = new List<Country>();

            SelectQuery query = Item.GetSelectQueryStub(core, typeof(Country));
            query.AddSort(SortOrder.Ascending, "country_name");

            DataTable countryDataTable = db.Query(query);

            foreach (DataRow dr in countryDataTable.Rows)
            {
                countries.Add(new Country(core, dr));
            }

            foreach (Country country in countries)
            {
                countrySelectBox.Add(new SelectBoxItem(country.Iso, country.Name));
            }

            if (user.Profile.CountryIso != null)
            {
                countrySelectBox.SelectedKey = user.Profile.CountryIso;
            }

            template.Parse("S_ADDRESS_LINE_1", addressLine1TextBox);
            template.Parse("S_ADDRESS_LINE_2", addressLine2TextBox);
            template.Parse("S_TOWN", townTextBox);
            template.Parse("S_STATE", stateTextBox);
            template.Parse("S_POST_CODE", postCodeTextBox);
            template.Parse("S_COUNTRY", countrySelectBox);
        }

        void AccountContactManage_AddEmail(object sender, ModuleModeEventArgs e)
        {
            SetTemplate("account_email_edit");

            /**/
            TextBox emailTextBox = new TextBox("email-address");

            /**/
            SelectBox emailTypeSelectBox = new SelectBox("phone-type");
            emailTypeSelectBox.Add(new SelectBoxItem(((byte)EmailAddressTypes.Personal).ToString(), "Personal"));
            emailTypeSelectBox.Add(new SelectBoxItem(((byte)EmailAddressTypes.Business).ToString(), "Business"));
            emailTypeSelectBox.Add(new SelectBoxItem(((byte)EmailAddressTypes.Student).ToString(), "Student"));
            emailTypeSelectBox.Add(new SelectBoxItem(((byte)EmailAddressTypes.Other).ToString(), "Other"));

            switch (e.Mode)
            {
                case "add-email":
                    break;
                case "edit-email":
                    long emailId = core.Functions.FormLong("id", core.Functions.RequestLong("id", 0));
                    UserEmail email = null;

                    if (emailId > 0)
                    {
                        try
                        {
                            email = new UserEmail(core, emailId);

                            emailTextBox.IsDisabled = true;
                            emailTextBox.Value = email.Email;

                            if (emailTypeSelectBox.ContainsKey(((byte)email.EmailType).ToString()))
                            {
                                emailTypeSelectBox.SelectedKey = ((byte)email.EmailType).ToString();
                            }

                            template.Parse("S_ID", email.Id.ToString());
                        }
                        catch (InvalidUserEmailException)
                        {
                            return;
                        }
                    }
                    else
                    {
                        return;
                    }

                    template.Parse("EDIT", "TRUE");
                    break;
            }

            template.Parse("S_EMAIL", emailTextBox);
            template.Parse("S_EMAIL_TYPE", emailTypeSelectBox);
        }

        void AccountContactManage_VerifyEmail(object sender, ModuleModeEventArgs e)
        {
            AuthoriseRequestSid();

            UserEmail email = new UserEmail(core, core.Functions.RequestLong("id", 0));

            if (email.UserId == LoggedInMember.Id)
            {
                if (!email.IsActivated)
                {
                    string activateKey = User.GenerateActivationSecurityToken();

                    string activateUri = string.Format("http://" + Hyperlink.Domain + "/register/?mode=activate-email&id={0}&key={1}",
                        email.Id, activateKey);

                    UpdateQuery query = new UpdateQuery(typeof(UserEmail));
                    query.AddField("email_activate_code", activateKey);
                    query.AddCondition("email_id", email.Id);

                    core.Db.Query(query);

                    Template emailTemplate = new Template(core.Http.TemplateEmailPath, "email_activation.html");

                    emailTemplate.Parse("TO_NAME", Owner.DisplayName);
                    emailTemplate.Parse("U_ACTIVATE", activateUri);
                    emailTemplate.Parse("USERNAME", ((User)Owner).UserName);

                    core.Email.SendEmail(email.Email, core.Settings.SiteTitle + " email activation", emailTemplate);

                    SetRedirectUri(BuildUri());
                    core.Display.ShowMessage("Verification e-mail send", "A verification code has been sent to the e-mail address along with verification instructions.");
                }
                else
                {
                    SetRedirectUri(BuildUri());
                    core.Display.ShowMessage("Already verified", "You have already verified your email address.");
                }
            }
            else
            {
                SetRedirectUri(BuildUri());
                core.Display.ShowMessage("Error", "An error has occured.");
            }
        }

        void AccountContactManage_AddPhone(object sender, ModuleModeEventArgs e)
        {
            SetTemplate("account_phone_edit");

            /**/
            TextBox phoneNumberTextBox = new TextBox("phone-number");

            /* */
            SelectBox phoneTypeSelectBox = new SelectBox("phone-type");
            phoneTypeSelectBox.Add(new SelectBoxItem(((byte)PhoneNumberTypes.Home).ToString(), "Home"));
            phoneTypeSelectBox.Add(new SelectBoxItem(((byte)PhoneNumberTypes.Mobile).ToString(), "Mobile"));
            phoneTypeSelectBox.Add(new SelectBoxItem(((byte)PhoneNumberTypes.Business).ToString(), "Business"));
            phoneTypeSelectBox.Add(new SelectBoxItem(((byte)PhoneNumberTypes.BusinessMobile).ToString(), "BusinessMobile"));
            phoneTypeSelectBox.Add(new SelectBoxItem(((byte)PhoneNumberTypes.VoIP).ToString(), "VoIP"));
            phoneTypeSelectBox.Add(new SelectBoxItem(((byte)PhoneNumberTypes.Fax).ToString(), "Fax"));
            phoneTypeSelectBox.Add(new SelectBoxItem(((byte)PhoneNumberTypes.Other).ToString(), "Other"));

            switch (e.Mode)
            {
                case "add-phone":
                    break;
                case "edit-phone":
                    long phoneNumberId = core.Functions.FormLong("id", core.Functions.RequestLong("id", 0));
                    UserPhoneNumber phoneNumber = null;

                    if (phoneNumberId > 0)
                    {
                        try
                        {
                            phoneNumber = new UserPhoneNumber(core, phoneNumberId);

                            //phoneNumberTextBox.IsDisabled = true;
                            phoneNumberTextBox.Value = phoneNumber.PhoneNumber;

                            if (phoneTypeSelectBox.ContainsKey(((byte)phoneNumber.PhoneType).ToString()))
                            {
                                phoneTypeSelectBox.SelectedKey = ((byte)phoneNumber.PhoneType).ToString();
                            }

                            template.Parse("S_ID", phoneNumber.Id.ToString());
                        }
                        catch (InvalidUserPhoneNumberException)
                        {
                        }
                    }

                    template.Parse("EDIT", "TRUE");
                    break;
            }

            template.Parse("S_PHONE_NUMBER", phoneNumberTextBox);
            template.Parse("S_PHONE_TYPE", phoneTypeSelectBox);
        }

        void AccountContactManage_VerifyPhone(object sender, ModuleModeEventArgs e)
        {
            AuthoriseRequestSid();
            SetTemplate("account_phone_verify");

            UserPhoneNumber phoneNumber = new UserPhoneNumber(core, core.Functions.RequestLong("id", 0));

            if (phoneNumber.UserId == LoggedInMember.Id)
            {
                if (!phoneNumber.Validated)
                {
                    string activateKey = User.GeneratePhoneActivationToken();

                    UpdateQuery query = new UpdateQuery(typeof(UserPhoneNumber));
                    query.AddField("phone_activate_code", activateKey);
                    query.AddCondition("phone_id", phoneNumber.Id);

                    core.Db.Query(query);

                    core.Sms.SendSms(phoneNumber.PhoneNumber, string.Format("Your {0} security code is {1}.", core.Settings.SiteTitle, activateKey));

                    TextBox verifyTextBox = new TextBox("verify-code");
                    verifyTextBox.Type = InputType.Telephone;

                    template.Parse("S_ID", phoneNumber.Id.ToString());
                    template.Parse("PHONE_NUMBER", phoneNumber.PhoneNumber);
                    template.Parse("S_VERIFY_CODE", verifyTextBox);
                }
                else
                {
                    SetRedirectUri(BuildUri());
                    core.Display.ShowMessage("Already verified", "You have already verified your phone number.");
                }
            }
            else
            {
                SetRedirectUri(BuildUri());
                core.Display.ShowMessage("Error", "An error has occured.");
            }
        }

        void AccountContactManage_VerifyPhone_Save(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            UserPhoneNumber phoneNumber = new UserPhoneNumber(core, core.Functions.FormLong("id", 0));

            if (phoneNumber.UserId == LoggedInMember.Id)
            {
                if (!phoneNumber.Validated)
                {
                    if (phoneNumber.ActivateKey == core.Http.Form["verify-code"])
                    {
                        UpdateQuery query = new UpdateQuery(typeof(UserPhoneNumber));
                        query.AddField("phone_validated", true);
                        query.AddField("phone_validated_time_ut", UnixTime.UnixTimeStamp());
                        query.AddCondition("phone_id", phoneNumber.Id);

                        db.Query(query);

                        SetRedirectUri(BuildUri());
                        core.Display.ShowMessage("Phone number verified", "Your phone number has been verified");
                    }
                }
            }
        }

        void AccountContactManage_AddLink(object sender, ModuleModeEventArgs e)
        {
            SetTemplate("account_link_edit");

            /**/
            TextBox linkAddressTextBox = new TextBox("link-address");

            /* */
            TextBox linkTitleTextBox = new TextBox("link-title");

            switch (e.Mode)
            {
                case "add-link":
                    break;
                case "edit-link":
                    long linkId = core.Functions.FormLong("id", core.Functions.RequestLong("id", 0));
                    UserLink link = null;

                    if (linkId > 0)
                    {
                        try
                        {
                            link = new UserLink(core, linkId);

                            //phoneNumberTextBox.IsDisabled = true;
                            linkAddressTextBox.Value = link.LinkAddress;
                            linkAddressTextBox.IsDisabled = true;
                            linkTitleTextBox.Value = link.Title;

                            template.Parse("S_ID", link.Id.ToString());
                        }
                        catch (InvalidUserLinkException)
                        {
                        }
                    }

                    template.Parse("EDIT", "TRUE");
                    break;
            }

            template.Parse("S_LINK", linkAddressTextBox);
            template.Parse("S_TITLE", linkTitleTextBox);
        }

        void AccountContactManage_EditAddress_save(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            LoggedInMember.Profile.AddressLine1 = core.Http.Form["address-1"];
            LoggedInMember.Profile.AddressLine2 = core.Http.Form["address-2"];
            LoggedInMember.Profile.AddressTown = core.Http.Form["town"];
            LoggedInMember.Profile.AddressState = core.Http.Form["state"];
            LoggedInMember.Profile.AddressPostCode = core.Http.Form["post-code"];
            LoggedInMember.Profile.CountryIso = core.Http.Form["country"];

            try
            {
                LoggedInMember.Profile.Update();
            }
            catch (UnauthorisedToUpdateItemException)
            {
            }

            SetRedirectUri(BuildUri());
            core.Display.ShowMessage("Address Saved", "Your address has been saved in the database.");
        }

        void AccountContactManage_AddEmail_save(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            switch (core.Http.Form["mode"])
            {
                case "add-email":
                    string emailAddress = core.Http.Form["email-address"];
                    EmailAddressTypes emailType = (EmailAddressTypes)core.Functions.FormByte("email-type", (byte)EmailAddressTypes.Personal);

                    try
                    {
                        UserEmail.Create(core, emailAddress, emailType);

                        SetRedirectUri(BuildUri());
                        core.Display.ShowMessage("E-mail address Saved", "Your e-mail address has been saved in the database. Before your e-mail can be used it will need to be verification. A verification code has been sent to the e-mail address along with verification instructions.");
                        return;
                    }
                    catch (InvalidUserEmailException)
                    {
                    }
                    catch (EmailInvalidException)
                    {
                        this.SetError("E-mail address is not valid");
                        return;
                    }
                    catch (EmailAlreadyRegisteredException)
                    {
                        this.SetError("E-mail address has been registered with " + core.Settings.SiteTitle + " before, please add another address");
                        return;
                    }
                    return;
                case "edit-email":
                    long emailId = core.Functions.FormLong("id", 0);

                    UserEmail email = null;

                    try
                    {
                        email = new UserEmail(core, emailId);
                    }
                    catch (InvalidUserEmailException)
                    {
                        return;
                    }

                    email.EmailType = (EmailAddressTypes)core.Functions.FormByte("email-type", (byte)EmailAddressTypes.Other);
                    email.Update();

                    SetRedirectUri(BuildUri());
                    core.Display.ShowMessage("E-mail address Saved", "Your e-mail address settings has been saved in the database.");
                    return;
                default:
                    DisplayError("Error - no mode selected");
                    return;
            }
        }

        void AccountContactManage_AddPhone_save(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            switch (core.Http.Form["mode"])
            {
                case "add-phone":
                    string phoneNumber = core.Http.Form["phone-number"];
                    PhoneNumberTypes phoneType = (PhoneNumberTypes)core.Functions.FormByte("phone-type", 0);

                    try
                    {
                        UserPhoneNumber.Create(core, phoneNumber, phoneType);

                        SetRedirectUri(BuildUri());
                        core.Display.ShowMessage("Phone Number Saved", "Your phone number has been saved in the database.");
                    }
                    catch (InvalidUserEmailException)
                    {
                    }
                    return;
                case "edit-phone":
                    long phoneId = core.Functions.FormLong("id", 0);
                    UserPhoneNumber number = null;

                    try
                    {
                        number = new UserPhoneNumber(core, phoneId);
                    }
                    catch (InvalidUserPhoneNumberException)
                    {
                        return;
                    }

                    number.PhoneNumber = core.Http.Form["phone-number"];
                    number.PhoneType = (PhoneNumberTypes)core.Functions.FormByte("phone-type", (byte)PhoneNumberTypes.Home);
                    number.Update();

                    SetRedirectUri(BuildUri());
                    core.Display.ShowMessage("Phone Number Saved", "Your phone number has been saved in the database.");
                    return;
                default:
                    DisplayError("Error - no mode selected");
                    return;
            }
        }

        void AccountContactManage_AddLink_save(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            switch (core.Http.Form["mode"])
            {
                case "add-link":
                    string linkAddress = core.Http.Form["link-address"];
                    string linkTitle = core.Http.Form["link-title"];

                    try
                    {
                        UserLink.Create(core, linkAddress, linkTitle);

                        SetRedirectUri(BuildUri());
                        core.Display.ShowMessage("Link Saved", "Your link has been saved in the database.");
                    }
                    catch (InvalidUserEmailException)
                    {
                    }
                    return;
                case "edit-link":
                    long linkId = core.Functions.FormLong("id", 0);
                    UserLink link = null;

                    try
                    {
                        link = new UserLink(core, linkId);
                    }
                    catch (InvalidUserPhoneNumberException)
                    {
                        return;
                    }

                    //link.LinkAddress = core.Http.Form["link-address"];
                    link.Title = core.Http.Form["link-title"];
                    link.Update();

                    SetRedirectUri(BuildUri());
                    core.Display.ShowMessage("Link Saved", "Your link has been saved in the database.");
                    return;
                default:
                    DisplayError("Error - no mode selected");
                    return;
            }
        }

        void AccountContactManage_DeleteEmail(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            long emailId = core.Functions.RequestLong("id", 0);

            try
            {
            }
            catch(InvalidUserEmailException)
            {
                SetRedirectUri(BuildUri());
                core.Display.ShowMessage("Error", "Could not delete the email.");
                return;
            }
        }

        void AccountContactManage_DeletePhone(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            long phoneId = core.Functions.RequestLong("id", 0);

            try
            {
                
                UserPhoneNumber number = new UserPhoneNumber(core, phoneId);

                if (number.IsTwoFactor)
                {
                    SetRedirectUri(BuildUri());
                    core.Display.ShowMessage("Cannot delete phone number", "The phone number cannot be deleted because it is currently being used for two-factor authentication.");
                    return;
                }

                if (number.Delete() > 0)
                {
                    SetRedirectUri(BuildUri());
                    core.Display.ShowMessage("Phone Number Deleted", "The phone number has been deleted from the database.");
                    return;
                }
                else
                {
                    SetRedirectUri(BuildUri());
                    core.Display.ShowMessage("Error", "Could not delete the phone number.");
                    return;
                }
            }
            catch (InvalidUserPhoneNumberException)
            {
                SetRedirectUri(BuildUri());
                core.Display.ShowMessage("Error", "Could not delete the phone number.");
                return;
            }
        }

        void AccountContactManage_DeleteLink(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            long linkId = core.Functions.RequestLong("id", 0);

            try
            {
                UserLink link = new UserLink(core, linkId);

                if (link.Delete() > 0)
                {
                    SetRedirectUri(BuildUri());
                    core.Display.ShowMessage("Link Deleted", "The link has been deleted from the database.");
                    return;
                }
                else
                {
                    core.Display.ShowMessage("Error", "Could not delete the link.");
                    return;
                }
            }
            catch (PageNotFoundException)
            {
                core.Display.ShowMessage("Error", "Could not delete the link.");
                return;
            }
        }
    }
}
