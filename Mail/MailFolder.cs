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
        [DataField("folder_parent")]
        private long parentId;
		[DataField("folder_order")]
        private int folderOrder;
		[DataField("folder_level")]
        private int folderLevel;
        [DataField("folder_parents", MYSQL_TEXT)]
        private string parents;
		[DataField("folder_messages")]
        private long folderMessages;
		
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

        public long ParentTypeId
        {
            get
            {
                return ItemType.GetTypeId(typeof(MailFolder));
            }
        }
		
		public MailFolder(Core core, DataRow folderRow)
            : base (core)
        {
            ItemLoad += new ItemLoadHandler(MailFolder_ItemLoad);

            try
            {
                loadItemInfo(folderRow);
            }
            catch (InvalidItemException)
            {
                throw new InvalidMailFolderException();
            }
        }

        void MailFolder_ItemLoad()
        {
        }

        public MailFolder Create(Core core, MailFolder parent, string title)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            /* TODO: Fix */
            Item item = Item.Create(core, typeof(MailFolder), new FieldValuePair("owner_id", parent.ownerId),
                new FieldValuePair("folder_parent", parent.Id),
                new FieldValuePair("folder_name", title));

            return (MailFolder)item;
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
                throw new NotImplementedException();
            }
        }

        #region INestableItem Members


        public ParentTree GetParents()
        {
            throw new NotImplementedException();
        }

        public List<Item> GetChildren()
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    public class InvalidMailFolderException : Exception
    {
    }
}
