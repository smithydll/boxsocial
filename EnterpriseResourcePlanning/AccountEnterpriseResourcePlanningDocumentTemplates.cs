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
using BoxSocial.Internals;
using BoxSocial.Forms;
using BoxSocial.IO;

namespace BoxSocial.Applications.EnterpriseResourcePlanning
{
    [AccountSubModule("erp", "templates", true)]
    public class AccountEnterpriseResourcePlanningDocumentTemplates : AccountSubModule
    {
        public override string Title
        {
            get
            {
                return "ERP Settings";
            }
        }

        public override int Order
        {
            get
            {
                return 1;
            }
        }

        public AccountEnterpriseResourcePlanningDocumentTemplates(Core core)
            : base(core)
        {
            this.Load += new EventHandler(AccountEnterpriseResourcePlanningDocumentTemplates_Load);
            this.Show += new EventHandler(AccountEnterpriseResourcePlanningDocumentTemplates_Show);
        }

        void AccountEnterpriseResourcePlanningDocumentTemplates_Load(object sender, EventArgs e)
        {
            AddModeHandler("edit", new ModuleModeHandler(AccountEnterpriseResourcePlanningDocumentTemplates_Edit));
            AddModeHandler("add", new ModuleModeHandler(AccountEnterpriseResourcePlanningDocumentTemplates_Edit));
        }

        void AccountEnterpriseResourcePlanningDocumentTemplates_Show(object sender, EventArgs e)
        {
            SetTemplate("account_erp_document_templates_manage");


        }

        void AccountEnterpriseResourcePlanningDocumentTemplates_Edit(object sender, ModuleModeEventArgs e)
        {
            SetTemplate("account_erp_document_templates_edit");

            SaveMode(AccountEnterpriseResourcePlanningDocumentTemplates_Edit_Save);

            TextBox titleTextBox = new TextBox("title");
            TextBox descriptionTextBox = new TextBox("description");
            descriptionTextBox.Lines = 5;

            switch (e.Mode)
            {
                case "add":
                    break;
                case "edit":
                    long templateId = core.Functions.FormLong("id", core.Functions.RequestLong("id", 0));
                    DocumentTemplate documentTemplate = null;

                    try
                    {
                        documentTemplate = new DocumentTemplate(core, templateId);
                    }
                    catch
                    {
                        core.Functions.Generate404();
                        return;
                    }

                    if (documentTemplate != null)
                    {
                        if (!documentTemplate.Owner.Equals(Owner))
                        {
                            core.Functions.Generate403();
                        }

                        titleTextBox.Value = documentTemplate.Title;
                        descriptionTextBox.Value = documentTemplate.Description;

                        template.Parse("S_ID", documentTemplate.Id);
                    }
                    break;
            }

            template.Parse("S_TITLE", titleTextBox);
            template.Parse("S_DESCRIPTION", descriptionTextBox);
        }

        void AccountEnterpriseResourcePlanningDocumentTemplates_Edit_Save(object sender, ModuleModeEventArgs e)
        {
            AuthoriseRequestSid();
        }


    }
}
