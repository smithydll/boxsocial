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
using System.Xml;
using System.Xml.Serialization;
using BoxSocial.IO;
using BoxSocial.Internals;

namespace BoxSocial.Internals
{
	[DataTable("contacts")]
	public class Contact : NumberedItem
	{
		[DataField("contact_id", DataFieldKeys.Primary)]
		private long contactId;
		[DataField("contact_name_display", 64)]
        private string contactDisplayName;
		[DataField("contact_name_title", 8)]
        private string nameTitle;
        [DataField("contact_name_first", 36)]
        private string nameFirst;
        [DataField("contact_name_middle", 36)]
        private string nameMiddle;
        [DataField("contact_name_last", 36)]
        private string nameLast;
        [DataField("contact_name_suffix", 8)]
        private string nameSuffix;
		
		public long ContactId
		{
			get
			{
				return contactId;
			}
		}
		
		/// <summary>
        /// Gets the contact's Display name
        /// </summary>
        public string DisplayName
        {
            get
            {
                if (contactDisplayName == string.Empty)
                {
                    return FirstName + " " + LastName;
                }
                else
                {
                    return contactDisplayName;
                }
            }
            set
            {
                SetProperty("contactDisplayName", value);
            }
        }
		
		public string Title
        {
            get
            {
                return nameTitle;
            }
            set
            {
                SetProperty("nameTitle", value);
            }
        }

        public string FirstName
        {
            get
            {
                return nameFirst;
            }
            set
            {
                SetProperty("nameFirst", value);
            }
        }

        public string MiddleName
        {
            get
            {
                return nameMiddle;
            }
            set
            {
                SetProperty("nameMiddle", value);
            }
        }

        public string LastName
        {
            get
            {
                return nameLast;
            }
            set
            {
                SetProperty("nameLast", value);
            }
        }

        public string Suffix
        {
            get
            {
                return nameSuffix;
            }
            set
            {
                SetProperty("nameSuffix", value);
            }
        }
		
		public Contact(Core core)
			: base(core)
		{
		}
		
		public override long Id
		{
			get 
			{
				return contactId;
			}
		}

        public static void ShowAll(object sender, ShowUPageEventArgs e)
        {
            e.Template.SetTemplate("view_contacts.html");
        }

        public static void Show(object sender, ShowUPageEventArgs e)
        {
            e.Template.SetTemplate("view_contact.html");
        }

		public override string Uri
		{
			get 
			{
				throw new NotImplementedException();
			}
		}
	}
}
