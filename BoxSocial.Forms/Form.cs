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
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Web;

namespace BoxSocial.Forms
{
    public class Form
    {
        private string name;
        private string uri;
        private bool isFormSubmission;
        private Dictionary<string, FormField> formFields;
        private System.Collections.Specialized.NameValueCollection values;

        public Form(string name, string uri)
        {
            this.name = name;
            this.uri = uri;
            this.formFields = new Dictionary<string, FormField>();
        }

        public string Name
        {
            get
            {
                return name;
            }
        }

        public string Uri
        {
            get
            {
                return uri;
            }
        }

        public bool IsFormSubmission
        {
            get
            {
                return isFormSubmission;
            }
            set
            {
                isFormSubmission = value;
            }
        }

        public void AddFormField(FormField formField)
        {
            formFields.Add(formField.Name, formField);
            if (values != null)
            {
                foreach (string key in values.Keys)
                {
                    if (key == formField.Name)
                    {
                        formField.SetValue(values[formField.Name]);
                    }
                }
            }
        }

        public FormField this[string key]
        {
            get
            {
                return formFields[key];
            }
        }

        public Dictionary<string, FormField> FormFields
        {
            get
            {
                return formFields;
            }
        }

        public void SetValues(System.Collections.Specialized.NameValueCollection values)
        {
            this.values = values;
            foreach (string key in values.Keys)
            {
                if (formFields.ContainsKey(key))
                {
                    formFields[key].SetValue(values[key]);
                }
            }
        }
    }
}
