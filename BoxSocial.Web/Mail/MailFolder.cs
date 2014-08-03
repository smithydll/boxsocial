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
    public enum FolderTypes : byte
    {
        Custom = 0x00,
        Inbox = 0x01,
        Draft = 0x02,
        Outbox = 0x03,
        SentItems = 0x04,
    }

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
        [DataField("folder_type")]
        private byte folderType;

        public string FolderName
        {
            get
            {
                return folderName;
            }
        }

        public FolderTypes FolderType
        {
            get
            {
                return (FolderTypes)folderType;
            }
        }

        public long MessageCount
        {
            get
            {
                return folderMessages;
            }
        }
		
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

        public MailFolder(Core core, long folderId)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(MailFolder_ItemLoad);

            try
            {
                LoadItem(folderId);
            }
            catch (InvalidItemException)
            {
                throw new InvalidMailFolderException();
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

        public MailFolder(Core core, System.Data.Common.DbDataReader folderRow)
            : base(core)
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

        public MailFolder(Core core, User user, string name)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(MailFolder_ItemLoad);

            try
            {
                LoadItem(new FieldValuePair("owner_id", user.Id), new FieldValuePair("folder_parent", 0), new FieldValuePair("folder_name", name));
            }
            catch (InvalidItemException)
            {
                throw new InvalidMailFolderException();
            }
        }

        protected override void loadItemInfo(DataRow folderRow)
        {
            loadValue(folderRow, "folder_id", out folderId);
            loadValue(folderRow, "owner_id", out ownerId);
            loadValue(folderRow, "folder_name", out folderName);
            loadValue(folderRow, "folder_parent", out parentId);
            loadValue(folderRow, "folder_order", out folderOrder);
            loadValue(folderRow, "folder_level", out folderLevel);
            loadValue(folderRow, "folder_parents", out parents);
            loadValue(folderRow, "folder_messages", out folderMessages);
            loadValue(folderRow, "folder_type", out folderType);

            itemLoaded(folderRow);
            core.ItemCache.RegisterItem((NumberedItem)this);
        }

        protected override void loadItemInfo(System.Data.Common.DbDataReader folderRow)
        {
            loadValue(folderRow, "folder_id", out folderId);
            loadValue(folderRow, "owner_id", out ownerId);
            loadValue(folderRow, "folder_name", out folderName);
            loadValue(folderRow, "folder_parent", out parentId);
            loadValue(folderRow, "folder_order", out folderOrder);
            loadValue(folderRow, "folder_level", out folderLevel);
            loadValue(folderRow, "folder_parents", out parents);
            loadValue(folderRow, "folder_messages", out folderMessages);
            loadValue(folderRow, "folder_type", out folderType);

            itemLoaded(folderRow);
            core.ItemCache.RegisterItem((NumberedItem)this);
        }

        void MailFolder_ItemLoad()
        {
        }

        public static MailFolder Create(Core core, FolderTypes type, string title)
        {
            return Create(core, core.Session.LoggedInMember, type, title);
        }

        public static MailFolder Create(Core core, User owner, FolderTypes type, string title)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            /* TODO: Fix */
            Item item = Item.Create(core, typeof(MailFolder), new FieldValuePair("owner_id", owner.Id),
                new FieldValuePair("folder_type", (byte)type),
                new FieldValuePair("folder_parent", 0),
                new FieldValuePair("folder_name", title));

            return (MailFolder)item;
        }

        public static MailFolder Create(Core core, MailFolder parent, string title)
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

        public List<Message> GetMessages(int page, int perPage)
        {
            List<Message> messages = new List<Message>();

            SelectQuery query = MessageRecipient.GetSelectQueryStub(core, typeof(MessageRecipient));
            query.AddFields(Item.GetFieldsPrefixed(core, typeof(Message)));
            query.AddJoin(JoinTypes.Inner, new DataField(typeof(MessageRecipient), "message_id"), new DataField(typeof(Message), "message_id"));
            query.AddCondition("message_folder_id", folderId);
            if (((FolderTypes)folderType) != FolderTypes.Draft)
            {
                query.AddCondition("message_draft", false);
            }
            query.AddSort(SortOrder.Descending, "message_time_ut");
            query.LimitStart = (page - 1) * perPage;
            query.LimitCount = perPage;

            System.Data.Common.DbDataReader messagesReader = db.ReaderQuery(query);

            while (messagesReader.Read())
            {
                messages.Add(new Message(core, messagesReader));
            }

            messagesReader.Close();
            messagesReader.Dispose();

            return messages;
        }

        public List<Message> GetThreads(int page, int perPage)
        {
            List<Message> messages = new List<Message>();

            SelectQuery query = MessageRecipient.GetSelectQueryStub(core, typeof(MessageRecipient));
            query.AddFields(Item.GetFieldsPrefixed(core, typeof(Message)));
            query.AddJoin(JoinTypes.Inner, new DataField(typeof(MessageRecipient), "message_id"), new DataField(typeof(Message), "message_id"));
            query.AddCondition("message_thread_start_id", 0);
            QueryCondition qc1 = query.AddCondition("message_folder_id", folderId);
            QueryCondition qc2 = qc1.AddCondition(ConditionRelations.Or, new DataField(typeof(MessageRecipient), "sender_id"), ConditionEquality.Equal, ownerId);
            qc2.AddCondition(ConditionRelations.And, new DataField(typeof(MessageRecipient), "user_id"), ConditionEquality.Equal, core.LoggedInMemberId);
            query.AddSort(SortOrder.Descending, "message_time_last_ut");
            query.LimitStart = (page - 1) * perPage;
            query.LimitCount = perPage;

            /*HttpContext.Current.Response.Write(query.ToString());
            HttpContext.Current.Response.End();*/

            System.Data.Common.DbDataReader messagesReader = db.ReaderQuery(query);

            while (messagesReader.Read())
            {
                messages.Add(new Message(core, messagesReader));
            }

            messagesReader.Close();
            messagesReader.Dispose();

            return messages;
        }

        public static MailFolder GetFolder(Core core, FolderTypes folder, User user)
        {
            SelectQuery query = MailFolder.GetSelectQueryStub(core, typeof(MailFolder));
            query.AddCondition("folder_type", (byte)folder);
            query.AddCondition("owner_id", user.Id);

            System.Data.Common.DbDataReader inboxReader = core.Db.ReaderQuery(query);

            if (inboxReader.HasRows)
            {
                inboxReader.Read();

                MailFolder newFolder = new MailFolder(core, inboxReader);

                inboxReader.Close();
                inboxReader.Dispose();

                return newFolder;
            }
            else
            {
                inboxReader.Close();
                inboxReader.Dispose();

                throw new InvalidMailFolderException();
            }
        }

        public static MailFolder GetFolder(Core core, FolderTypes folder, MessageRecipient user)
        {
            SelectQuery query = MailFolder.GetSelectQueryStub(core, typeof(MailFolder));
            query.AddCondition("folder_type", (byte)folder);
            query.AddCondition("owner_id", user.UserId);

            System.Data.Common.DbDataReader inboxReader = core.Db.ReaderQuery(query);

            if (inboxReader.HasRows)
            {
                inboxReader.Read();

                MailFolder newFolder = new MailFolder(core, inboxReader);

                inboxReader.Close();
                inboxReader.Dispose();

                return newFolder;
            }
            else
            {
                inboxReader.Close();
                inboxReader.Dispose();

                throw new InvalidMailFolderException();
            }
        }

        public static List<MailFolder> GetFolders(Core core, User user)
        {
            List<MailFolder> folders = new List<MailFolder>();
            SelectQuery query = MailFolder.GetSelectQueryStub(core, typeof(MailFolder));
            query.AddCondition("owner_id", user.Id);

            System.Data.Common.DbDataReader inboxReader = core.Db.ReaderQuery(query);

            while(inboxReader.Read())
            {
                folders.Add(new MailFolder(core, inboxReader));
            }

            inboxReader.Close();
            inboxReader.Dispose();

            return folders;
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
