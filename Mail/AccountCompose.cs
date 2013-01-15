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

namespace BoxSocial.Applications.Mail
{
    [AccountSubModule(AppPrimitives.Member, "mail", "compose")]
    public class AccountCompose : AccountSubModule
    {
        public override string Title
        {
            get
            {
                return "Compose";
            }
        }

        public override int Order
        {
            get
            {
                return 1;
            }
        }

        /// <summary>
        /// Initializes a new instance of the AccountCompose class. 
        /// </summary>
        /// <param name="core">The Core token.</param>
        public AccountCompose(Core core)
            : base(core)
        {
            this.Load += new EventHandler(AccountCompose_Load);
            this.Show += new EventHandler(AccountCompose_Show);
        }

        void AccountCompose_Load(object sender, EventArgs e)
        {
        }

        void AccountCompose_Show(object sender, EventArgs e)
        {
            SetTemplate("account_compose");

            UserSelectBox toUserSelectBox = new UserSelectBox(core, "to");
            TextBox subjectTextBox = new TextBox("subject");
            TextBox messageTextBox = new TextBox("message");
            messageTextBox.IsFormatted = true;

            template.Parse("S_TO", toUserSelectBox);
            //template.Parse("S_SUBJECT", subjectTextBox);
            //template.Parse("S_MESSAGE", messageTextBox);
        }

        void AccountCompose_Save(object sender, EventArgs e)
        {
        }

        void AccountCompose_Send(object sender, EventArgs e)
        {
        }
    }
}
