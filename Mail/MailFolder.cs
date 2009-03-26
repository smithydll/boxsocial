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
using System.Text;
using System.Web;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Applications.Mail
{
	[DataTable("mail_folders")]
	public class MailFolder : NumberedItem, INestableItem
	{
		[DataField("folder_id", DataFieldKeys.Primary)] 
		private long folderId;
		[DataField("owner_id")]
		private long ownerId;
		[DataField("folder_name", 31)]
		private string folderName;
		[DataField("folder_order")]
        private int folderOrder;
		[DataField("folder_level")]
        private int folderLevel;
        [DataField("folder_parents", MYSQL_TEXT)]
        private string parents;
		
		public int Order
		{
			get
			{
				return folderOrder;
			}
		}
		
		public int Level
		{
			get
			{
				return folderLevel;
			}
		}

		
		public MailFolder()
		{
		}
		
		public override long Id
        {
            get
            {
                return folderId;
            }
        }

        public override string Uri
        {
            get
            {
				return Linker.BuildAccountSubModuleUri("mail", "read", messageId);

            }
        }
	}
}
