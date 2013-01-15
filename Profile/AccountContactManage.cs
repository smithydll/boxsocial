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
        public AccountContactManage(Core core)
            : base(core)
        {
            this.Load += new EventHandler(AccountContactManage_Load);
            this.Show += new EventHandler(AccountContactManage_Show);
        }

        void AccountContactManage_Load(object sender, EventArgs e)
        {
            AddModeHandler("edit-address", new ModuleModeHandler(AccountContactManage_EditAddress));
            AddModeHandler("add-email", new ModuleModeHandler(AccountContactManage_AddEmail));
            AddModeHandler("edit-email", new ModuleModeHandler(AccountContactManage_AddEmail));
            AddModeHandler("add-phone", new ModuleModeHandler(AccountContactManage_AddPhone));
            AddModeHandler("edit-phone", new ModuleModeHandler(AccountContactManage_AddPhone));

            AddSaveHandler("edit-address", new EventHandler(AccountContactManage_EditAddress_save));
            AddSaveHandler("add-email", new EventHandler(AccountContactManage_AddEmail_save));
            AddSaveHandler("edit-email", new EventHandler(AccountContactManage_AddEmail_save));
            AddSaveHandler("add-phone", new EventHandler(AccountContactManage_AddPhone_save));
            AddSaveHandler("edit-phone", new EventHandler(AccountContactManage_AddPhone_save));
        }

        void AccountContactManage_Show(object sender, EventArgs e)
        {
            SetTemplate("account_contact_manage");

            List<UserEmail> emails = core.Session.LoggedInMember.GetEmailAddresses();
            List<UserPhoneNumber> phoneNumbers = new List<UserPhoneNumber>();

            SelectQuery query = Item.GetSelectQueryStub(typeof(UserPhoneNumber));
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
                emailsVariableCollection.Parse("U_EDIT", BuildUri("contact", "edit-email", email.Id));
                emailsVariableCollection.Parse("U_EDIT_PERMISSIONS", Access.BuildAclUri(core, email));
            }

            foreach (UserPhoneNumber phoneNumber in phoneNumbers)
            {
                VariableCollection emailsVariableCollection = template.CreateChild("phone_list");

                emailsVariableCollection.Parse("PHONE_ID", phoneNumber.Id.ToString());
                emailsVariableCollection.Parse("PHONE_NUMBER", phoneNumber.PhoneNumber);
                emailsVariableCollection.Parse("U_EDIT", BuildUri("contact", "edit-phone", phoneNumber.Id));
                emailsVariableCollection.Parse("U_EDIT_PERMISSIONS", Access.BuildAclUri(core, phoneNumber));
            }

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

            SelectQuery query = Item.GetSelectQueryStub(typeof(Country));
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
                        core.Display.ShowMessage("E-mail address Saved", "Your e-mail address has been saved in the database. Before your e-mail can be used it will need to be validated. A validation code has been sent to the e-mail address along with validation instructions.");
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
                        this.SetError("E-mail address has been registered with zinzam before, please add another address");
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
    }
}
