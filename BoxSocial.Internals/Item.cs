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
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Diagnostics;
using System.Text;
using System.Web;
using System.Web.Caching;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    public abstract class Item
    {
        public const long MYSQL_TEXT = 65535L;
        public const long MYSQL_MEDIUM_TEXT = 16777215L;
        public const long MYSQL_LONG_TEXT = 4294967295L;
        public const long NAMESPACE = 31L;
        public const long IP = 50L;

        /// <summary>
        /// Database object
        /// </summary>
        protected Mysql db;

        /// <summary>
        /// Core token
        /// </summary>
        protected Core core;

        private Assembly assembly;

        private List<string> updatedItems;

        public delegate void ItemLoadHandler();
        public delegate void ItemChangeAuthenticationProviderHandler(object sender, ItemChangeAuthenticationProviderEventArgs e);

        public event ItemLoadHandler ItemLoad;
        public event ItemChangeAuthenticationProviderHandler ItemChangeAuthenticationProvider;
        public event EventHandler OnUpdate;
        public event EventHandler OnDelete;
        public event EventHandler ItemUpdated;
        public event EventHandler ItemDeleted;

        protected Item(Core core)
        {
            this.core = core;
            this.db = core.db;
            assembly = Assembly.GetCallingAssembly();
            updatedItems = new List<string>();
        }

        protected void LoadItem(long primaryKey)
        {
            LoadItem(this.GetType(), primaryKey);
        }

        protected void LoadItem(Type type, long primaryKey)
        {
            // 1. Discover primary key
            // 2. Build query
            // 3. Execute query
            // 4. Fill results

            string tableName = GetTable(type);
            List<DataFieldInfo> fields = GetFields(type);
            string keyField = "";

            SelectQuery query = new SelectQuery(tableName);

            foreach (DataFieldInfo field in fields)
            {
                if ((field.Key & DataFieldKeys.Primary) == DataFieldKeys.Primary)
                {
                    keyField = field.Name;
                }

                query.AddFields(field.Name);
            }

            if (string.IsNullOrEmpty(keyField))
            {
                // Error
                throw new NoPrimaryKeyException();
            }

            query.AddCondition(keyField, primaryKey);

            /*DataTable itemTable = Query(query);

            if (itemTable.Rows.Count == 1)
            {
                loadItemInfo(type, itemTable.Rows[0]);
            }
            else
            {
                // Error
                throw new InvalidItemException(this.GetType().FullName);
            }*/

            loadItemInfo(type, core.db.ReaderQuery(query));
        }

        protected void LoadItem(string uniqueIndex, object value)
        {
            LoadItem(uniqueIndex, value, false);
        }

        protected void LoadItem(string uniqueIndex, object value, bool caseInsensitive)
        {
            // 1. check index is unique
            // 2. Build query
            // 3. Execute query
            // 4. Fill results

            string tableName = GetTable(this.GetType());
            List<DataFieldInfo> fields = GetFields(this.GetType());
            string keyField = "";

            SelectQuery query = new SelectQuery(tableName);

            foreach (DataFieldInfo field in fields)
            {
                if (field.Name == uniqueIndex)
                {
                    if ((field.Key & (DataFieldKeys.Unique | DataFieldKeys.Primary)) != DataFieldKeys.None)
                    {
                        keyField = field.Name;
                    }
                    else
                    {
                        throw new FieldNotUniqueIndexException(field.Name);
                    }
                }

                query.AddFields(field.Name);
            }

            if (value is string && caseInsensitive)
            {
                query.AddCondition(new QueryFunction(keyField, QueryFunctions.ToLowerCase), ((string)value).ToLower());
            }
            else
            {
                query.AddCondition(keyField, value);
            }

            /*DataTable itemTable = Query(query);

            if (itemTable.Rows.Count == 1)
            {
                loadItemInfo(itemTable.Rows[0]);

                if (ItemLoad != null)
                {
                    ItemLoad();
                }
            }
            else
            {
                // Error
                throw new InvalidItemException(this.GetType().FullName);
            }*/
			
			loadItemInfo(this.GetType(), core.db.ReaderQuery(query));
        }

        protected void LoadItem(string ownerIdIndex, string ownerTypeIndex, Primitive owner)
        {
            // 1. check indexes are unique
            // 2. Build query
            // 3. Execute query
            // 4. Fill results

            string tableName = GetTable(this.GetType());
            List<DataFieldInfo> fields = GetFields(this.GetType());

            SelectQuery query = new SelectQuery(tableName);

            foreach (DataFieldInfo field in fields)
            {
                if (field.Name == ownerIdIndex || field.Name == ownerTypeIndex)
                {
                    if ((field.Key & DataFieldKeys.Unique) != DataFieldKeys.Unique)
                    {
                        throw new FieldNotUniqueIndexException();
                    }
                }

                query.AddFields(field.Name);
            }

            query.AddCondition(ownerIdIndex, owner.Id);
            query.AddCondition(ownerTypeIndex, owner.TypeId);

            /*DataTable itemTable = Query(query);

            if (itemTable.Rows.Count == 1)
            {
                loadItemInfo(itemTable.Rows[0]);

                if (ItemLoad != null)
                {
                    ItemLoad();
                }
            }
            else
            {
                // Error
                throw new InvalidItemException(this.GetType().FullName);
            }*/
			
			loadItemInfo(this.GetType(), core.db.ReaderQuery(query));
        }

        protected List<Type> getSubTypes()
        {
            List<Type> types = new List<Type>();

            Type currentType = this.GetType();
            Type[] allTypes = currentType.Assembly.GetTypes();

            foreach (Type type in allTypes)
            {
                List<DataFieldInfo> fields = GetFields(type);

                foreach (DataFieldInfo field in fields)
                {
                    if (field.ParentType == currentType)
                    {
                        types.Add(type);
                        break;
                    }
                }
            }

            return types;
        }

        protected void UpdateThis()
        {
            StackTrace st = new StackTrace();
            StackFrame sf = st.GetFrame(1);

            updatedItems.Add(sf.GetMethod().Name);

            HttpContext.Current.Response.Write(sf.GetMethod().Name);
        }

        protected void SetProperty(string key, object value)
        {
            try
            {
                Type thisType = this.GetType();
                FieldInfo fi = thisType.GetField(key, BindingFlags.Default | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

                if (!fi.GetValue(this).Equals(value))
                {
                    fi.SetValue(this, value);

                    if (!updatedItems.Contains(key))
                    {
                        updatedItems.Add(key);
                    }
                }
            }
            catch
            {
            }
        }

        protected bool HasPropertyUpdated(string key)
        {
            if (updatedItems.Contains(key))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        protected DataTable Query(SelectQuery query)
        {
            return db.Query(query);
        }

        protected long Query(InsertQuery query)
        {
            return db.Query(query);
        }

        protected long Query(UpdateQuery query)
        {
            return db.Query(query);
        }

        protected long Query(DeleteQuery query)
        {
            return db.Query(query);
        }

        public string Namespace
        {
            get
            {
                return GetNamespace(this.GetType());
            }
        }

        public abstract string Uri
        {
            get;
        }

        protected List<ItemTag> getTags()
        {
            List<ItemTag> tags = new List<ItemTag>();

            return tags;
        }

        protected Primitive fillOwner(long ownerId, long ownerTypeId)
        {
            Primitive owner;

            Type type = core.GetPrimitiveType(ownerTypeId);
            if (type.IsSubclassOf(typeof(Primitive)))
            {
                owner = System.Activator.CreateInstance(type, new object[] { core, ownerId }) as Primitive;

                return owner;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// returns the field that is linked to the parent of a given type
        /// </summary>
        /// <param name="parentType"></param>
        /// <returns></returns>
        public static string GetParentField(Type parentType)
        {
            string returnValue = null;

            Type[] types = parentType.Assembly.GetTypes();
            foreach (Type type in types)
            {
                List<DataFieldInfo> fields = GetFields(type);

                foreach (DataFieldInfo field in fields)
                {
                    if (field.ParentType == parentType)
                    {
                        if (string.IsNullOrEmpty(returnValue))
                        {
                            returnValue = field.Name;
                        }
                        else
                        {
                            // TODO: create a new exception
                            throw new Exception("Multiple children types");
                        }
                    }
                }
            }

            if (!string.IsNullOrEmpty(returnValue))
            {
                return returnValue;
            }
            else
            {
                // TODO: Exception
                throw new Exception("No parent of type " + parentType.Name + " found.");
            }
        }

        public SelectQuery GetSelectQueryStub()
        {
            return GetSelectQueryStub(this.GetType());
        }

        public static SelectQuery GetSelectQueryStub(Type type)
        {
            return GetSelectQueryStub(type, true);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="check">Check for redefined _GetSelectQueryStub</param>
        /// <returns></returns>
        public static SelectQuery GetSelectQueryStub(Type type, bool check)
        {
            SelectQuery query;

            if (check && type.GetMethod(type.Name + "_GetSelectQueryStub", Type.EmptyTypes) != null)
            {
                query = (SelectQuery)type.InvokeMember(type.Name + "_GetSelectQueryStub", BindingFlags.Public | BindingFlags.Static | BindingFlags.InvokeMethod, null, null, new object[] { }); //GetSelectQueryStub(typeToGet);
            }
            else
            {
                query = new SelectQuery(GetTable(type));
                query.AddFields(GetFieldsPrefixed(type));
				
				/*Type[] interfaces = type.GetInterfaces();
				foreach (Type i in interfaces)
				{
					if (i == typeof(IPermissibleItem))
					{
						//query.AddFields(GetFieldsPrefixed(typeof(AccessControlGrant)));
						/*  * /
					}
				}*/
            }

            return query;
        }

        public SelectQuery getSelectQueryStub()
        {
            Type type = this.GetType();

            SelectQuery query = new SelectQuery(GetTable(type));
            query.AddFields(GetFieldsPrefixed(type));

            return query;
        }
		
		private static Dictionary<Type, List<DataFieldInfo>> itemFieldsCache = null;
		
		private static void populateItemFieldsCache()
		{
			object o = null;
            System.Web.Caching.Cache cache;
			
			if (HttpContext.Current != null && HttpContext.Current.Cache != null)
			{
				cache = HttpContext.Current.Cache;
			}
			else
			{
				cache = new Cache();
			}
			
			if (HttpContext.Current != null && HttpContext.Current.Cache != null)
			{
				cache = HttpContext.Current.Cache;
			}
			else
			{
				cache = new Cache();
			}
			
			o = cache.Get("itemFields");
			
			if (o != null && o is Dictionary<Type, List<DataFieldInfo>>)
			{
				itemFieldsCache = (Dictionary<Type, List<DataFieldInfo>>)o;
			}
			else
			{
				itemFieldsCache = new Dictionary<Type, List<DataFieldInfo>>();
			}
		}

        internal protected static List<DataFieldInfo> GetFields(Type type)
        {
            List<DataFieldInfo> returnValue = new List<DataFieldInfo>();
			
			if  (itemFieldsCache != null)
			{
				if (itemFieldsCache.ContainsKey(type))
				{
					return itemFieldsCache[type];
				}
			}
			else
			{
				populateItemFieldsCache();
			}

            foreach (FieldInfo fi in type.GetFields(BindingFlags.Default | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                foreach (Attribute attr in Attribute.GetCustomAttributes(fi))
                {
                    if (attr is DataFieldAttribute)
                    {
                        DataFieldAttribute dfattr = (DataFieldAttribute)attr;
                        if (dfattr.FieldName != null)
                        {
							if (fi.FieldType == typeof(ItemKey))
							{
								DataFieldInfo dfiId;
								DataFieldInfo dfiTypeId;

                                dfiId = new DataFieldInfo(dfattr.FieldName + "_id", typeof(long), dfattr.MaxLength, dfattr.Index);
                                dfiTypeId = new DataFieldInfo(dfattr.FieldName + "_type_id", typeof(long), dfattr.MaxLength, dfattr.Index);

                                dfiId.ParentType = dfattr.ParentType;
                                dfiId.ParentFieldName = dfattr.ParentFieldName;
                                dfiTypeId.ParentType = dfattr.ParentType;
                                dfiTypeId.ParentFieldName = dfattr.ParentFieldName;
								
								returnValue.Add(dfiId);
								returnValue.Add(dfiTypeId);
							}
							else
							{
	                            DataFieldInfo dfi;
                                dfi = new DataFieldInfo(dfattr.FieldName, fi.FieldType, dfattr.MaxLength, dfattr.Index);
                                dfi.ParentType = dfattr.ParentType;
                                dfi.ParentFieldName = dfattr.ParentFieldName;

	                            returnValue.Add(dfi);
							}
                        }
                        else
                        {
                            returnValue.Add(new DataFieldInfo(fi.Name, fi.FieldType, 255));
                        }
                    }
                }

            }

            if (!itemFieldsCache.ContainsKey(type))
            {
                itemFieldsCache.Add(type, returnValue);
            }

            System.Web.Caching.Cache cache;
			
			if (HttpContext.Current != null && HttpContext.Current.Cache != null)
			{
				cache = HttpContext.Current.Cache;
			}
			else
			{
				cache = new Cache();
			}
			
			cache.Add("itemFields", itemFieldsCache, null, Cache.NoAbsoluteExpiration, new TimeSpan(12, 0, 0), CacheItemPriority.High, null);

            return returnValue;
        }

        internal static object GetFieldValue(DataFieldInfo dfi, Item item)
        {
            foreach (FieldInfo fi in item.GetType().GetFields(BindingFlags.Default | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                foreach (Attribute attr in Attribute.GetCustomAttributes(fi))
                {
                    if (attr is DataFieldAttribute)
                    {
                        DataFieldAttribute dfattr = (DataFieldAttribute)attr;
                        if (dfattr.FieldName != null)
                        {
                            if (dfi.Name == dfattr.FieldName)
                            {
                                return fi.GetValue(item);
                            }
                        }
                    }
                }
            }

            return 0;
        }

        public static string[] GetFieldsPrefixed(Type type)
        {
            string tableName = GetTable(type);
            List<DataFieldInfo> fields = GetFields(type);
            string[] returnValue = new string[fields.Count];

            for (int i = 0; i < fields.Count; i++)
            {
                returnValue[i] = string.Format("`{0}`.`{1}`",
                    tableName, fields[i].Name);
            }

            return returnValue;
        }

        protected string Table
        {
            get
            {
                return GetTable(this.GetType());
            }
        }

        protected string[] FieldsPrefixed
        {
            get
            {
                return GetFieldsPrefixed(this.GetType());
            }
        }

        public static string GetTable(Type type)
        {
            bool attributeFound = false;
            foreach (Attribute attr in type.GetCustomAttributes(typeof(DataTableAttribute), false))
            {
                DataTableAttribute dtattr = (DataTableAttribute)attr;
                if (dtattr != null)
                {
                    if (dtattr.TableName != null)
                    {
                        return dtattr.TableName;
                    }
                    attributeFound = true;
                }
            }

            /* Maybe is a Table View if haven't found a DataTable */
            if (!attributeFound)
            {
                foreach (Attribute attr in type.GetCustomAttributes(typeof(TableViewAttribute), false))
                {
                    TableViewAttribute tvattr = (TableViewAttribute)attr;
                    if (tvattr != null)
                    {
                        if (tvattr.TableName != null)
                        {
                            return tvattr.TableName;
                        }
                        attributeFound = true;
                    }
                }
            }

            if (attributeFound)
            {
                return type.Name;
            }
            else
            {
                return null;
            }
        }

        internal protected static string GetNamespace(Type type)
        {
            foreach (Attribute attr in type.GetCustomAttributes(typeof(DataTableAttribute), false))
            {
                DataTableAttribute dtattr = (DataTableAttribute)attr;
                if (attr != null)
                {
                    if (dtattr.Namespace != null)
                    {
                        return dtattr.Namespace;
                    }
                }
            }

            return type.FullName;
        }

        public long Update()
        {
            if (this is IPermissibleItem)
            {
                IPermissibleItem iThis = (IPermissibleItem)this;
                iThis.Access.SetSessionViewer(core.session);
                if (!iThis.Access.CanEdit)
                {
                    throw new UnauthorisedToUpdateItemException();
                }
            }

            if (ItemChangeAuthenticationProvider != null)
            {
                ItemChangeAuthenticationProvider(this, new ItemChangeAuthenticationProviderEventArgs(ItemChangeAction.Edit));
            }

            if (OnUpdate != null)
            {
                OnUpdate(this, new EventArgs());
            }

            if (updatedItems.Count == 0)
            {
                return 0;
            }

            SelectQuery sQuery = new SelectQuery(Item.GetTable(this.GetType()));
            UpdateQuery uQuery = new UpdateQuery(Item.GetTable(this.GetType()));

            foreach (FieldInfo fi in this.GetType().GetFields(BindingFlags.Default | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                foreach (Attribute attr in Attribute.GetCustomAttributes(fi))
                {

                    if (updatedItems.Contains(fi.Name))
                    {
                        if (attr is DataFieldAttribute)
                        {
                            DataFieldAttribute dfattr = (DataFieldAttribute)attr;
                            if (dfattr.FieldName != null)
                            {
                                uQuery.AddField(dfattr.FieldName, fi.GetValue(this));
                            }
                            else
                            {
                                uQuery.AddField(fi.Name, fi.GetValue(this));
                            }
                        }
                    }
                }
            }

            List<DataFieldInfo> fields = GetFields(this.GetType());
            bool foundKey = false;
            bool containsUniqueFields = false;

            foreach (DataFieldInfo field in fields)
            {
                if ((field.Key & DataFieldKeys.Primary) == DataFieldKeys.Primary)
                {
                    sQuery.AddFields(field.Name);
                    sQuery.AddCondition(field.Name, ConditionEquality.NotEqual, GetFieldValue(field, this));
                    uQuery.AddCondition(field.Name, GetFieldValue(field, this));
                    foundKey = true;
                }
            }

            foreach (DataFieldInfo field in fields)
            {
                if ((field.Key & DataFieldKeys.Unique) == DataFieldKeys.Unique)
                {
                    containsUniqueFields = true;
                    sQuery.AddCondition(field.Name, GetFieldValue(field, this));
                }
            }

            if (!foundKey)
            {
                foreach (DataFieldInfo field in fields)
                {
                    if ((field.Key & DataFieldKeys.Unique) == DataFieldKeys.Unique)
                    {
                        uQuery.AddCondition(field.Name, GetFieldValue(field, this));
                        if (updatedItems.Contains(field.Name))
                        {
                            foundKey = false;
                            break;
                        }
                        else
                        {
                            foundKey = true;
                            // continue, all must match
                        }
                    }
                }
            }

            if (!foundKey)
            {
                // Error
                throw new NoPrimaryKeyException();
            }

            // check uniqueness
            if (containsUniqueFields)
            {
                long uniqueness = db.Query(sQuery).Rows.Count;

                if (uniqueness != 1)
                {
                    throw new RecordNotUniqueException();
                }
            }

            long result = db.Query(uQuery);

            if (result > 0)
            {
                if (ItemUpdated != null)
                {
                    ItemUpdated(this, new EventArgs());
                }
            }

            return result;
        }

        protected void AuthenticateAction(ItemChangeAction action)
        {
            if (ItemChangeAuthenticationProvider != null)
            {
                ItemChangeAuthenticationProvider(this, new ItemChangeAuthenticationProviderEventArgs(action));
            }
        }
		
		public static Item Create(Core core, Type type, params FieldValuePair[] fields)
		{
			return Create(core,  type, false, fields);
		}
		
		public static Item Create(Core core, Type type, bool suppress, params FieldValuePair[] fields)
		{
            core.db.BeginTransaction();
			
			InsertQuery iQuery = new InsertQuery(GetTable(type));
			
			foreach (FieldValuePair field in fields)
			{
				iQuery.AddField(field.Field, field.Value);
			}
			
			long id = core.db.Query(iQuery);
			
			if (id > 0)
			{
				if (!suppress)
				{
					Item newItem = System.Activator.CreateInstance(type, new object[] { core, id }) as Item;
					
					return newItem;
				}
				else
				{
					return null;
				}
			}
			else
			{
				throw new InvalidItemException(type.FullName);
			}
		}

        public long Delete()
        {
            if (this is IPermissibleItem)
            {
                IPermissibleItem iThis = (IPermissibleItem)this;
                iThis.Access.SetSessionViewer(core.session);
                if (!iThis.Access.CanDelete)
                {
                    throw new UnauthorisedToDeleteItemException();
                }
            }

            AuthenticateAction(ItemChangeAction.Delete);

            db.BeginTransaction();

            /*List<Type> subTypes = getSubTypes();
            foreach (Type subType in subTypes)
            {
                List<Item> subItems = getSubItems(subType);

                foreach (Item item in subItems)
                {
                    item.Delete();
                }
            }*/

            DeleteQuery dQuery = new DeleteQuery(Item.GetTable(this.GetType()));

            List<DataFieldInfo> fields = GetFields(this.GetType());
            bool foundKey = false;

            foreach (DataFieldInfo field in fields)
            {
                if ((field.Key & (DataFieldKeys.Primary | DataFieldKeys.Unique)) != DataFieldKeys.None)
                {
                    dQuery.AddCondition(field.Name, GetFieldValue(field, this));
                    foundKey = true;
                }
            }

            if (!foundKey)
            {
                // Error
                throw new NoPrimaryKeyException();
            }

            long result = db.Query(dQuery);

            if (result > 0)
            {
                if (ItemDeleted != null)
                {
                    ItemDeleted(this, new EventArgs());
                }
            }

            return result;
        }

        protected void loadItemInfo(System.Data.Common.DbDataReader reader)
        {
            loadItemInfo(this.GetType(), reader);
        }

        protected void loadItemInfo(Type type, System.Data.Common.DbDataReader reader)
        {
			//HttpContext.Current.Response.Write("I am being used " + type.FullName + "<br />");
            int fieldsLoaded = 0;
            int objectFields = 0;
            Dictionary<string, FieldInfo> fields = new Dictionary<string, FieldInfo>();

            foreach (FieldInfo fi in type.GetFields(BindingFlags.Default | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                foreach (Attribute attr in Attribute.GetCustomAttributes(fi))
                {
                    if (attr is DataFieldAttribute)
                    {
                        DataFieldAttribute dfattr = (DataFieldAttribute)attr;
                        objectFields++;

                        string columnPrefix;
                        if (dfattr.FieldName != null)
                        {
                            columnPrefix = dfattr.FieldName;
                        }
                        else
                        {
                            columnPrefix = fi.Name;
                        }

                        fields.Add(columnPrefix, fi);
                        if (fi.FieldType == typeof(ItemKey))
                        {
                            fields.Add(columnPrefix + "_id", fi);
                            fields.Add(columnPrefix + "_type_id", fi);
                        }
                    }
                }
            }

            /* buffer for item */
            long ikBufferId = 0;
            long ikBufferTypeId = 0;

            if (reader.HasRows)
            {
                bool rowRead = false; /* past participle */
                while (reader.Read())
                {
                    /* only read one row */
                    if (rowRead)
                    {
                        // Error
						reader.Close();
                        throw new InvalidItemException(this.GetType().FullName);
                    }
                    else
                    {
                        rowRead = true;
                    }

                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        string column = reader.GetName(i);
                        object value = null;

                        if (fields.ContainsKey(column))
                        {
                            FieldInfo fi = fields[column];

                            if (!reader.IsDBNull(i))
                            {
                                value = reader.GetValue(i);
                            }
                            else
                            {
                                if (fi.FieldType == typeof(string))
                                {
                                    fieldsLoaded++;
                                    continue;
                                }
                            }

                            /*try
                            {*/
                            if (fi.FieldType == typeof(ItemKey))
                            {
                                if (ikBufferId > 0 && column.EndsWith("_type_id"))
                                {
                                    fi.SetValue(this, new ItemKey(ikBufferId, (long)value));
                                    fieldsLoaded++;
                                }
                                else if (ikBufferTypeId > 0 && column.EndsWith("_id"))
                                {
                                    fi.SetValue(this, new ItemKey((long)value, ikBufferTypeId));
                                    fieldsLoaded++;
                                }
                                else
                                {
                                    if (column.EndsWith("_id"))
                                    {
                                        ikBufferId = (long)value;
                                    }
                                    else if (column.EndsWith("_type_id"))
                                    {
                                        ikBufferTypeId = (long)value;
                                    }
                                }
                            }
                            else if (fi.FieldType == typeof(bool) && !(value is bool))
                            {
                                if (value is byte)
                                {
                                    fi.SetValue(this, ((byte)value > 0) ? true : false);
                                    fieldsLoaded++;
                                }
                                else if (value is sbyte)
                                {
                                    fi.SetValue(this, ((sbyte)value > 0) ? true : false);
                                    fieldsLoaded++;
                                }
                            }
                            else
                            {
                                fi.SetValue(this, value);
                                fieldsLoaded++;
                            }
                            /*}
                            catch (Exception ex)
                            {
                                Display.ShowMessage("Type error on load", Bbcode.Strip(column + " expected type " + fi.FieldType + " type returned was " + value.GetType() + "\n\n" + ex));
                            }*/
                        }
                    }
                }
            }

            if (fieldsLoaded != objectFields)
            {
				reader.Close();
                throw new InvalidItemException(this.GetType().FullName);
            }
			
			reader.Close();

            if (ItemLoad != null)
            {
                ItemLoad();
            }
        }

        protected void loadItemInfo(DataRow itemRow)
        {
            loadItemInfo(this.GetType(), itemRow);
        }

        protected void loadItemInfo(Type type, DataRow itemRow)
        {
            List<string> columns = new List<string>();
            List<string> columnsAttributed = new List<string>();
            int columnCount = 0;

            foreach (DataColumn column in itemRow.Table.Columns)
            {
                columns.Add(column.ColumnName);
            }

            foreach (FieldInfo fi in type.GetFields(BindingFlags.Default | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                foreach (Attribute attr in Attribute.GetCustomAttributes(fi))
                {
                    if (attr is DataFieldAttribute)
                    {
                        DataFieldAttribute dfattr = (DataFieldAttribute)attr;
						if (fi.FieldType == typeof(ItemKey))
						{
							string columnPrefix;
                            if (dfattr.FieldName != null)
	                        {
                                columnPrefix = dfattr.FieldName;
	                        }
	                        else
	                        {
	                            columnPrefix = fi.Name;
	                        }
							
							columnsAttributed.Add(columnPrefix);
							columnsAttributed.Add(columnPrefix + "_id");
							columnsAttributed.Add(columnPrefix + "_type_id");
							
							fi.SetValue(this, new ItemKey((long)itemRow[columnPrefix + "_id"], (long)itemRow[columnPrefix + "_type_id"])); 
						}
						else
						{
	                        string columnName;
                            if (dfattr.FieldName != null)
	                        {
                                columnName = dfattr.FieldName;
	                        }
	                        else
	                        {
	                            columnName = fi.Name;
	                        }

	                        columnsAttributed.Add(columnName);

	                        if (columns.Contains(columnName))
	                        {
	                            columnCount++;
	                            if (!(itemRow[columnName] is DBNull))
	                            {
	                                try
	                                {
	                                    if (fi.FieldType == typeof(bool) && !(itemRow[columnName] is bool))
	                                    {
	                                        if (itemRow[columnName] is byte)
	                                        {
	                                            fi.SetValue(this, ((byte)itemRow[columnName] > 0) ? true : false);
	                                        }
	                                        else if (itemRow[columnName] is sbyte)
	                                        {
	                                            fi.SetValue(this, ((sbyte)itemRow[columnName] > 0) ? true : false);
	                                        }
	                                    }
	                                    else
	                                    {
	                                        fi.SetValue(this, itemRow[columnName]);
	                                    }
	                                }
	                                catch (Exception ex)
	                                {
	                                    Display.ShowMessage("Type error on load", Bbcode.Strip(columnName + " expected type " + fi.FieldType + " type returned was " + itemRow[columnName].GetType() + "\n\n" + ex));
	                                }
	                            }
	                            else
	                            {
	                                // This works only when the non repeated columns are not entirely TEXT
	                                if (fi.FieldType == typeof(string))
	                                {
	                                    columnCount++;
	                                }
	                                else
	                                {
	                                    throw new InvalidItemException(this.GetType().FullName);
	                                }
	                            }
	                        }
						}
                    }
                }
            }

            if (columnCount == 0)
            {
                throw new InvalidItemException(this.GetType().FullName);
            }

            if (ItemLoad != null)
            {
                ItemLoad();
            }
        }

        protected void MoveUp(string orderField)
        {
            // TODO: make sure isn't INestableItem
            IOrderableItem oItem = (IOrderableItem)this;

            UpdateQuery uQuery = new UpdateQuery(GetTable(this.GetType()));
            uQuery.AddField(orderField, new QueryOperation(orderField, QueryOperations.Addition, 1));
            uQuery.AddCondition(orderField, oItem.Order);
            oItem.AddSequenceConditon(uQuery);

            core.db.Query(uQuery);

            oItem.Order--;

            this.Update();
        }

        protected void MoveDown(string orderField)
        {
            // TODO: make sure isn't INestableItem
            IOrderableItem oItem = (IOrderableItem)this;

            UpdateQuery uQuery = new UpdateQuery(GetTable(this.GetType()));
            uQuery.AddField(orderField, new QueryOperation(orderField, QueryOperations.Subtraction, 1));
            uQuery.AddCondition(orderField, oItem.Order);
            oItem.AddSequenceConditon(uQuery);

            core.db.Query(uQuery);

            oItem.Order++;

            this.Update();
        }
    }

    public enum ItemChangeAction
    {
        Edit,
        Delete,
		Create,
    }

    public class ItemChangeAuthenticationProviderEventArgs : EventArgs
    {
        private ItemChangeAction action;

        public ItemChangeAction Action
        {
            get
            {
                return action;
            }
        }

        public ItemChangeAuthenticationProviderEventArgs(ItemChangeAction action)
        {
            this.action = action;
        }
    }

    public class InvalidItemException : Exception
    {
		public InvalidItemException()
		{
		}
		
		public InvalidItemException(string type)
			: base("Type: " + type)
		{
		}
    }

    public class NoPrimaryKeyException : Exception
    {
    }

    public class RecordNotUniqueException : Exception
    {
    }

    public class FieldNotUniqueIndexException : Exception
    {
		public FieldNotUniqueIndexException()
			: base ()
		{
		}
		
		public FieldNotUniqueIndexException(string field)
			: base("Field: " + field)
		{
		}
    }

    public class UnauthorisedToCreateItemException : Exception
    {
    }

    public class UnauthorisedToUpdateItemException : Exception
    {
    }

    public class UnauthorisedToDeleteItemException : Exception
    {
    }
}
