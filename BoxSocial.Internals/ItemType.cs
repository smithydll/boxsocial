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
using System.Data;
using System.Configuration;
using System.Web;
using BoxSocial.IO;

namespace BoxSocial.Internals
{	
	[DataTable("item_types")]
	public class ItemType : NumberedItem
	{
		
		[DataField("type_id", DataFieldKeys.Primary)]
		long typeId;
		[DataField("type_namespace", DataFieldKeys.Unique, 184)]
		string typeNamespace;
		[DataField("type_application_id")]
		long applicationId;
        [DataField("type_primitive")]
        private bool typeInheritsPrimitive;
        [DataField("type_likeable")]
        private bool typeImplementsILikeable;
        [DataField("type_commentable")]
        private bool typeImplementsICommentable;
        [DataField("type_rateable")]
        private bool typeImplementsIRateable;
        [DataField("type_subscribeable")]
        private bool typeImplementsISubscribeable;
        [DataField("type_shareable")]
        private bool typeImplementsIShareable;
        [DataField("type_viewable")]
        private bool typeImplementsIViewable;
        [DataField("type_notifiable")]
        private bool typeImplementsINotifiable;
        [DataField("type_embeddable")]
        private bool typeImplementsIEmbeddable;
			
		public long TypeId
		{
			get
			{
				return typeId;
			}
		}
		
		public string TypeNamespace
		{
			get
			{
				return typeNamespace;
			}
		}
		
		public long ApplicationId
		{
			get
			{
				return applicationId;
			}
		}

        public bool IsPrimitive
        {
            get
            {
                return typeInheritsPrimitive;
            }
        }

        public bool Likeable
        {
            get
            {
                return typeImplementsILikeable;
            }
        }

        public bool Commentable
        {
            get
            {
                return typeImplementsICommentable;
            }
        }

        public bool Rateable
        {
            get
            {
                return typeImplementsIRateable;
            }
        }

        public bool Subscribeable
        {
            get
            {
                return typeImplementsISubscribeable;
            }
        }

        public bool Shareable
        {
            get
            {
                return typeImplementsIShareable;
            }
        }

        public bool Viewable
        {
            get
            {
                return typeImplementsIViewable;
            }
        }

        public bool Notifiable
        {
            get
            {
                return typeImplementsINotifiable;
            }
        }

        public bool Embeddable
        {
            get
            {
                return typeImplementsIEmbeddable;
            }
        }

        internal ItemType(Core core, DataRow typeRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(ItemType_ItemLoad);

            try
            {
                loadItemInfo(typeRow);
            }
            catch (InvalidItemException)
            {
                throw new InvalidItemTypeException();
            }
        }
		
		public ItemType(Core core, long typeId)
			: base(core)
		{
			ItemLoad += new ItemLoadHandler(ItemType_ItemLoad);

            try
            {
                LoadItem(typeId);
            }
            catch (InvalidItemException)
            {
                throw new InvalidItemTypeException();
            }
		}
		
		public ItemType(Core core, string typeNamespace)
			: base(core)
		{
			ItemLoad += new ItemLoadHandler(ItemType_ItemLoad);

            try
            {
                LoadItem("type_namespace", typeNamespace);
            }
            catch (InvalidItemException)
            {
                throw new InvalidItemTypeException();
            }
		}
		
		private void ItemType_ItemLoad()
        {
        }
		
		public static ItemType Create(Core core, Type type, ApplicationEntry ae)
		{
            if (core == null)
            {
                throw new NullCoreException();
            }

			string ns = Item.GetNamespace(type);
			
			ItemType it = (ItemType)Item.Create(core, typeof(ItemType),
			                          new FieldValuePair("type_namespace", ns),
			                          new FieldValuePair("type_application_id", ae.Id.ToString()),
                                      new FieldValuePair("type_commentable", (typeof(ICommentableItem).IsAssignableFrom(type))),
                                      new FieldValuePair("type_likeable", (typeof(ILikeableItem).IsAssignableFrom(type))),
                                      new FieldValuePair("type_rateable", (typeof(IRateableItem).IsAssignableFrom(type))),
                                      new FieldValuePair("type_subscribeable", (typeof(ISubscribeableItem).IsAssignableFrom(type))),
                                      new FieldValuePair("type_viewable", (typeof(IViewableItem).IsAssignableFrom(type))),
                                      new FieldValuePair("type_shareable", (typeof(IShareableItem).IsAssignableFrom(type))),
                                      new FieldValuePair("type_primitive", type.IsSubclassOf(typeof(Primitive))));
			
			return it;
		}

        public static long GetTypeId(Type type)
        {
            return ItemKey.GetTypeId(type);
        }
		
		public override string Uri
		{
			get
			{
				throw new NotImplementedException();
			}
		}
		
		public override long Id
		{
			get
			{
				return typeId;
			}
		}
	}
	
	public class InvalidItemTypeException : Exception
	{
	}
}
