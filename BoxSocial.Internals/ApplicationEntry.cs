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
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Text;
using System.Web;
using Lachlan.Web;

namespace BoxSocial.Internals
{
    public class ApplicationEntry
    {
        public const string APPLICATION_FIELDS = "ap.application_id, ap.application_title, ap.application_description, ap.application_icon, ap.application_assembly_name, ap.user_id, ap.application_primitives, ap.application_date_ut";
        public const string USER_APPLICATION_FIELDS = "pa.app_id, pa.app_access, pa.item_id, pa.item_type";

        private Mysql db;
        private int applicationId;
        private int creatorId;
        private long itemId;
        private string title;
        private string description;
        private string icon;
        private string assemblyName;
        private AppPrimitives primitives;
        private long dateRaw;
        private ushort permissions;
        private Access applicationAccess;

        public string AssemblyName
        {
            get
            {
                return assemblyName;
            }
        }

        public ApplicationEntry(Mysql db, DataRow applicationRow)
        {
            this.db = db;

            applicationId = (int)applicationRow["application_id"];
            creatorId = (int)applicationRow["ap.user_id"];
            itemId = (long)applicationRow["pa.item_id"];
            title = (string)applicationRow["application_title"];
            description = (string)applicationRow["application_description"];
            icon = (string)applicationRow["application_icon"];
            assemblyName = (string)applicationRow["application_assembly_name"];
            primitives = (AppPrimitives)applicationRow["application_primitives"];
            dateRaw = (long)applicationRow["application_date_ut"];
            permissions = (ushort)applicationRow["app_profile_access"];

            //applicationAccess = new Access(db, permissions, AccessItem.Application, (long)userId);
        }
    }
}
