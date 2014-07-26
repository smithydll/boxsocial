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
using System.Reflection;
using System.Diagnostics;
using System.Runtime.CompilerServices;
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
        public delegate void ItemDeletedEventHandler(object sender, ItemDeletedEventArgs e);

        public event ItemLoadHandler ItemLoad;
        public event ItemChangeAuthenticationProviderHandler ItemChangeAuthenticationProvider;
        public event EventHandler OnUpdate;
        public event EventHandler OnDelete;
        public event EventHandler ItemUpdated;
        public event ItemDeletedEventHandler ItemDeleted;

        public Bbcode Bbcode
        {
            get
            {
                return core.Bbcode;
            }
        }

        private Functions Functions
        {
            get
            {
                return core.Functions;
            }
        }

        private Stopwatch timer;

        [MethodImpl(MethodImplOptions.NoInlining)]
        protected Item(Core core)
        {
            this.core = core;
            this.db = core.Db;
            assembly = Assembly.GetCallingAssembly();
            updatedItems = new List<string>();

            // Profile this method
            timer = new Stopwatch();
            timer.Start();
        }

        public static string GetPrimaryKey(Type type)
        {
            List<DataFieldInfo> fields = GetFields(type);
            string keyField = string.Empty;

            foreach (DataFieldInfo field in fields)
            {
                if ((field.Key & DataFieldKeys.Primary) == DataFieldKeys.Primary)
                {
                    keyField = field.Name;
                }
            }

            if (string.IsNullOrEmpty(keyField))
            {
                // Error
                throw new NoPrimaryKeyException();
            }

            return keyField;
        }

        protected void LoadItem(params FieldValuePair[] keyFields)
        {
            LoadItem(this.GetType(), keyFields);
        }

        protected void LoadItem(Type type, params FieldValuePair[] keyFields)
        {
            // 1. Build query
            // 2. Execute query
            // 3. Fill results

            string tableName = GetTable(type);
            List<DataFieldInfo> fields = GetFields(type);

            SelectQuery query = new SelectQuery(tableName);

            foreach (DataFieldInfo field in fields)
            {
                query.AddFields(field.Name);
            }

            if (keyFields != null)
            {
                foreach (FieldValuePair fvp in keyFields)
                {
                    query.AddCondition(fvp.Field, fvp.Value);
                }
            }
            else
            {
                throw new NoUniqueKeyException();
            }

            /*DataTable dataTable = core.Db.Query(query);
            if (dataTable.Rows.Count == 1)
            {
                loadItemInfo(dataTable.Rows[0]);
            }
            else
            {
                throw new InvalidItemException();
            }*/

            System.Data.Common.DbDataReader reader = core.Db.ReaderQuery(query);
            if (reader.HasRows)
            {
                reader.Read();
                loadItemInfo(reader);

                reader.Close();
                reader.Dispose();
            }
            else
            {
                reader.Close();
                reader.Dispose();

                throw new InvalidItemException();
            }

            if (type.IsSubclassOf(typeof(NumberedItem)))
            {
                core.ItemCache.RegisterItem((NumberedItem)this);
            }
        }

        protected void LoadItem(long primaryKey, params FieldValuePair[] keyFields)
        {
            LoadItem(this.GetType(), primaryKey, keyFields);
        }

        protected void LoadItem(long primaryKey)
        {
            LoadItem(this.GetType(), primaryKey, null);
        }

        protected void LoadItem(Type type, long primaryKey, params FieldValuePair[] keyFields)
        {
            // 1. Discover primary key
            // 2. Build query
            // 3. Execute query
            // 4. Fill results

            string tableName = GetTable(type);
            List<DataFieldInfo> fields = GetFields(type);
            string keyField = string.Empty;

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


            if (keyFields != null)
            {
                foreach (FieldValuePair fvp in keyFields)
                {
                    query.AddCondition(fvp.Field, fvp.Value);
                }
            }

            /*DataTable dataTable = core.Db.Query(query);
            if (dataTable.Rows.Count == 1)
            {
                loadItemInfo(dataTable.Rows[0]);
            }
            else
            {
                throw new InvalidItemException(this.GetType().FullName);
            }*/

            System.Data.Common.DbDataReader reader = core.Db.ReaderQuery(query);
            if (reader.HasRows)
            {
                reader.Read();
                loadItemInfo(reader);

                reader.Close();
                reader.Dispose();
            }
            else
            {
                reader.Close();
                reader.Dispose();

                throw new InvalidItemException();
            }

            if (this.GetType().IsSubclassOf(typeof(NumberedItem)))
            {
                core.ItemCache.RegisterItem((NumberedItem)this);
            }
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

            Type type = this.GetType();

            string tableName = GetTable(type);
            List<DataFieldInfo> fields = GetFields(type);
            string keyField = string.Empty;

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

            /*DataTable dataTable = core.Db.Query(query);
            if (dataTable.Rows.Count == 1)
            {
                loadItemInfo(dataTable.Rows[0]);
            }
            else
            {
                throw new InvalidItemException(type.FullName);
            }*/

            System.Data.Common.DbDataReader reader = core.Db.ReaderQuery(query);
            if (reader.HasRows)
            {
                reader.Read();
                loadItemInfo(reader);

                reader.Close();
                reader.Dispose();
            }
            else
            {
                reader.Close();
                reader.Dispose();

                throw new InvalidItemException();
            }
        }

        protected void LoadItem(string ownerIdIndex, string ownerTypeIndex, Primitive owner, params FieldValuePair[] keyFields)
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
                        throw new FieldNotUniqueIndexException(field.Name);
                    }
                }

                query.AddFields(field.Name);
            }

            query.AddCondition(ownerIdIndex, owner.Id);
            query.AddCondition(ownerTypeIndex, owner.TypeId);

            if (keyFields != null)
            {
                foreach (FieldValuePair fvp in keyFields)
                {
                    query.AddCondition(fvp.Field, fvp.Value);
                }
            }

            /*DataTable dataTable = core.Db.Query(query);
            if (dataTable.Rows.Count == 1)
            {
                loadItemInfo(dataTable.Rows[0]);
            }
            else
            {
                throw new InvalidItemException(this.GetType().FullName);
            }*/

            System.Data.Common.DbDataReader reader = core.Db.ReaderQuery(query);
            if (reader.HasRows)
            {
                reader.Read();
                loadItemInfo(reader);

                reader.Close();
                reader.Dispose();
            }
            else
            {
                reader.Close();
                reader.Dispose();

                throw new InvalidItemException();
            }

            if (this.GetType().IsSubclassOf(typeof(NumberedItem)))
            {
                core.ItemCache.RegisterItem((NumberedItem)this);
            }
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
        }

        protected void SetProperty(ref object var, object value)
        {
            if (var != value)
            {
                string key = string.Empty;
                try
                {
                    Type thisType = this.GetType();
                    FieldInfo[] fis = getFieldInfo(thisType);

                    foreach (FieldInfo fi in fis)
                    {
                        object val = fi.GetValue(this);
                        if (Object.ReferenceEquals(val, var))
                        {
                            key = fi.Name;
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                }

                if (!string.IsNullOrEmpty(key))
                {
                    var = value;

                    if (!updatedItems.Contains(key))
                    {
                        updatedItems.Add(key);
                    }
                }
            }
        }

        protected void SetProperty(string key, object value)
        {
            try
            {
                Type thisType = this.GetType();
                FieldInfo fi = thisType.GetField(key, BindingFlags.Default | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

                if ((fi.GetValue(this) == null && value != null )|| (!fi.GetValue(this).Equals(value)))
                {
                    fi.SetValue(this, value);

                    if (!updatedItems.Contains(key))
                    {
                        updatedItems.Add(key);
                    }
                }
            }
            catch (Exception ex)
            {
            }
        }

        protected static string GetName<T>(T item) where T : class
        {
            var properties = typeof(T).GetProperties();
            return properties[0].Name;
        }

        protected void SetPropertyByRef<T>(T item, object value) where T : class
        {
            var properties = typeof(T).GetProperties();
            string key = properties[0].Name;

            SetProperty(key, value);
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

        public static string GetParentField(Type parentType, Type childType)
        {
            string returnValue = null;

            List<DataFieldInfo> fields = GetFields(childType);

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
                        throw new Exception("Multiple children types.");
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
                throw new Exception("No parent of type " + childType.Name + " found.");
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
            long typeId = ItemType.GetTypeId(type);
            if (typeId > 0 && QueryCache.HasQuery(typeId))
            {
                return (SelectQuery)QueryCache.GetQuery(type, typeId);
            }
            else
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
                    if (type.IsSubclassOf(typeof(NumberedItem)) && type.Name != "ItemInfo" &&
                        (typeof(ILikeableItem).IsAssignableFrom(type) ||
                        typeof(IRateableItem).IsAssignableFrom(type) ||
                        typeof(ITagableItem).IsAssignableFrom(type) ||
                        typeof(IViewableItem).IsAssignableFrom(type) ||
                        typeof(ICommentableItem).IsAssignableFrom(type) ||
                        typeof(ISubscribeableItem).IsAssignableFrom(type) ||
                        typeof(IShareableItem).IsAssignableFrom(type) ||
                        typeof(IViewableItem).IsAssignableFrom(type)))
                    {
                        List<DataFieldInfo> fields = GetFields(type);
                        bool foundKey = false;
                        bool continueJoin = true;
                        string idField = string.Empty;

                        foreach (DataFieldInfo field in fields)
                        {
                            if ((field.Key & (DataFieldKeys.Primary)) != DataFieldKeys.None)
                            {
                                if (foundKey)
                                {
                                    continueJoin = false;
                                    break;
                                    //throw new ComplexKeyException(GetTable(type));
                                }
                                idField = field.Name;
                                foundKey = true;
                            }
                        }

                        if (!foundKey)
                        {
                            // Error
                            //throw new NoPrimaryKeyException();
                            continueJoin = false;
                        }

                        if (continueJoin)
                        {
                            query.AddFields(Item.GetFieldsPrefixed(typeof(ItemInfo)));
                            TableJoin join = query.AddJoin(JoinTypes.Left, new DataField(GetTable(type), idField), new DataField(typeof(ItemInfo), "info_item_id"));
                            join.AddCondition(new DataField(typeof(ItemInfo), "info_item_type_id"), ItemType.GetTypeId(type));
                        }
                    }

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

                if (check)
                {
                    QueryCache.AddQueryToCache(typeId, query);
                }
                return query;
            }
        }

        public SelectQuery getSelectQueryStub()
        {
            Type type = this.GetType();

            SelectQuery query = new SelectQuery(GetTable(type));
            query.AddFields(GetFieldsPrefixed(type));

            return query;
        }

        private static Object itemFieldInfoCacheLock = new Object();
        private static Dictionary<long, FieldInfo[]> itemFieldInfoCache = null;

        private static void populateItemFieldInfoCache()
        {
            object o = null;
            System.Web.Caching.Cache cache;

            if (HttpContext.Current != null && HttpContext.Current.Cache != null)
            {
                cache = HttpContext.Current.Cache;
            }
            else
            {
                cache = new System.Web.Caching.Cache();
            }

            try
            {
                if (cache != null)
                {
                    o = cache.Get("itemFieldInfo");
                }
            }
            catch (NullReferenceException)
            {
            }

            lock (itemFieldInfoCacheLock)
            {
                if (o != null && o is Dictionary<long, FieldInfo[]>)
                {
                    itemFieldInfoCache = (Dictionary<long, FieldInfo[]>)o;
                }
                else
                {
                    itemFieldInfoCache = new Dictionary<long, FieldInfo[]>(16);
                }
            }
        }

        public static FieldInfo[] saveToFieldInfoCache(Type type)
        {
            FieldInfo[] fields = type.GetFields(BindingFlags.Default | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

            long typeId = ItemType.GetTypeId(type);
            if (typeId > 0)
            {
                lock (itemFieldInfoCacheLock)
                {
                    if (!itemFieldInfoCache.ContainsKey(typeId))
                    {
                        itemFieldInfoCache.Add(typeId, fields);
                    }
                }

                System.Web.Caching.Cache cache;

                if (HttpContext.Current != null && HttpContext.Current.Cache != null)
                {
                    cache = HttpContext.Current.Cache;
                }
                else
                {
                    cache = new System.Web.Caching.Cache();
                }

                if (cache != null)
                {
                    try
                    {
                        cache.Add("itemFieldInfo", itemFieldInfoCache, null, System.Web.Caching.Cache.NoAbsoluteExpiration, new TimeSpan(12, 0, 0), CacheItemPriority.High, null);
                    }
                    catch (NullReferenceException)
                    {
                    }
                }
            }

            return fields;
        }

        public static FieldInfo[] getFieldInfo(Type type)
        {
            FieldInfo[] fields = null;
            long typeId = ItemType.GetTypeId(type);

            if (typeId > 0 && itemFieldInfoCache != null)
            {
                if (!itemFieldInfoCache.TryGetValue(typeId, out fields))
                {
                    populateItemFieldInfoCache();
                    fields = saveToFieldInfoCache(type);
                }
            }
            
            if (fields == null)
            {
                populateItemFieldInfoCache();
                fields = saveToFieldInfoCache(type);
            }

            return fields;
        }

        private static Object itemFieldsCacheLock = new Object();
		private static Dictionary<long, List<DataFieldInfo>> itemFieldsCache = null;

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
                cache = new System.Web.Caching.Cache();
            }

            try
            {
                if (cache != null)
                {
                    o = cache.Get("itemFields");
                }
            }
            catch (NullReferenceException)
            {
            }

            lock (itemFieldsCacheLock)
            {
                if (o != null && o is Dictionary<long, List<DataFieldInfo>>)
                {
                    itemFieldsCache = (Dictionary<long, List<DataFieldInfo>>)o;
                }
                else
                {
                    itemFieldsCache = new Dictionary<long, List<DataFieldInfo>>(16);
                }
            }
        }
		
		internal protected static List<DataFieldInfo> GetFields(Type type)
		{
			return GetFields(type, false);
		}
		
		internal protected static List<DataFieldInfo> GetRawFields(Type type)
		{
			return GetFields(type, true);
		}

        internal protected static List<DataFieldInfo> GetFields(Type type, bool getRawFields)
        {
            long typeId = ItemType.GetTypeId(type);

            List<DataFieldInfo> returnValue = new List<DataFieldInfo>(16);

            if (itemFieldsCache != null && typeId > 0)
			{
				if (!getRawFields)
				{
                    List<DataFieldInfo> dfis = null;

                    if (itemFieldsCache.TryGetValue(typeId, out dfis))
                    {
                        return dfis;
                    }
				}
			}
			else
			{
				populateItemFieldsCache();
			}

            FieldInfo[] fields = getFieldInfo(type);

            foreach (FieldInfo fi in fields /*type.GetFields(BindingFlags.Default | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)*/)
            {
                List<DataFieldKeyAttribute> additionalIndexes = new List<DataFieldKeyAttribute>();
                foreach (Attribute attr in Attribute.GetCustomAttributes(fi, typeof(DataFieldKeyAttribute)))
                {
                    DataFieldKeyAttribute dfkattr = (DataFieldKeyAttribute)attr;

                    additionalIndexes.Add(dfkattr);
                }

                foreach (Attribute attr in Attribute.GetCustomAttributes(fi, typeof(DataFieldAttribute)))
                {
                    DataFieldAttribute dfattr = (DataFieldAttribute)attr;
                    dfattr.AddIndexes(additionalIndexes.ToArray());
                    if (dfattr.FieldName != null)
                    {
                        if ((fi.FieldType == typeof(ItemKey)) && (!getRawFields))
                        {
                            DataFieldInfo dfiId;
                            DataFieldInfo dfiTypeId;

                            dfiId = new DataFieldInfo(dfattr.FieldName + "_id", typeof(long), dfattr.MaxLength, dfattr.Indicies);
                            dfiTypeId = new DataFieldInfo(dfattr.FieldName + "_type_id", typeof(long), dfattr.MaxLength, dfattr.Indicies);

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
                            dfi = new DataFieldInfo(dfattr.FieldName, fi.FieldType, dfattr.MaxLength, dfattr.Indicies);
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

            if ((!itemFieldsCache.ContainsKey(typeId)) && (!getRawFields))
            {
                lock (itemFieldsCacheLock)
                {
                    if (!itemFieldsCache.ContainsKey(typeId))
                    {
                        itemFieldsCache.Add(typeId, returnValue);
                    }
                }
            }

            System.Web.Caching.Cache cache;
			
			if (HttpContext.Current != null && HttpContext.Current.Cache != null)
			{
				cache = HttpContext.Current.Cache;
			}
			else
			{
                cache = new System.Web.Caching.Cache();
			}

            if (cache != null)
            {
                try
                {
                    cache.Add("itemFields", itemFieldsCache, null, System.Web.Caching.Cache.NoAbsoluteExpiration, new TimeSpan(12, 0, 0), CacheItemPriority.High, null);
                }
                catch (NullReferenceException)
                {
                }
            }

            return returnValue;
        }

        internal static object GetFieldValue(DataFieldInfo dfi, Item item)
        {
            FieldInfo[] fields = getFieldInfo(item.GetType());

            foreach (FieldInfo fi in fields /*item.GetType().GetFields(BindingFlags.Default | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)*/)
            {
                foreach (Attribute attr in Attribute.GetCustomAttributes(fi, typeof(DataFieldAttribute)))
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

            /*return new string[] {string.Format("`{0}`.*",
                    tableName)};*/
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
            return DataFieldAttribute.GetTable(type);
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
			return Update(this.GetType());
		}

        public long Update(Type type)
        {
            if (this is IPermissibleItem)
            {
                IPermissibleItem iThis = (IPermissibleItem)this;
                if (!iThis.Access.Can("EDIT"))
                {
                    throw new UnauthorisedToUpdateItemException();
                }
            }

            if (this is IPermissibleSubItem)
            {
                EditPermissionAttribute[] editAttributes = (EditPermissionAttribute[])this.GetType().GetCustomAttributes(typeof(EditPermissionAttribute), false);

                if (editAttributes.Length == 1)
                {
                    if (string.IsNullOrEmpty(editAttributes[0].Key))
                    {
                        throw new UnauthorisedToUpdateItemException();
                    }
                    else
                    {
                        IPermissibleSubItem isThis = (IPermissibleSubItem)this;
                        if (!isThis.PermissiveParent.Access.Can(editAttributes[0].Key))
                        {
                            throw new UnauthorisedToUpdateItemException();
                        }
                    }
                }
            }

            ItemChangeAuthenticationProviderHandler itemChangeAuthenticationProviderHander = ItemChangeAuthenticationProvider;
            if (itemChangeAuthenticationProviderHander != null)
            {
                itemChangeAuthenticationProviderHander(this, new ItemChangeAuthenticationProviderEventArgs(ItemChangeAction.Edit));
            }

            EventHandler onUpdateHandler = OnUpdate;
            if (onUpdateHandler != null)
            {
                onUpdateHandler(this, new EventArgs());
            }

            if (updatedItems.Count == 0)
            {
                return 0;
            }

            SelectQuery sQuery = new SelectQuery(Item.GetTable(type));
            UpdateQuery uQuery = new UpdateQuery(Item.GetTable(type));

            FieldInfo[] ffields = getFieldInfo(type);
            foreach (FieldInfo fi in ffields /*type.GetFields(BindingFlags.Default | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)*/)
            {
                List<DataFieldKeyAttribute> additionalIndexes = new List<DataFieldKeyAttribute>();
                foreach (Attribute attr in Attribute.GetCustomAttributes(fi, typeof(DataFieldKeyAttribute)))
                {
                    DataFieldKeyAttribute dfkattr = (DataFieldKeyAttribute)attr;

                    additionalIndexes.Add(dfkattr);
                }

                foreach (Attribute attr in Attribute.GetCustomAttributes(fi, typeof(DataFieldAttribute)))
                {
                    if (updatedItems.Contains(fi.Name))
                    {
                        DataFieldAttribute dfattr = (DataFieldAttribute)attr;
                        dfattr.AddIndexes(additionalIndexes.ToArray());
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

            List<DataFieldInfo> fields = GetRawFields(type);
            bool foundKey = false;
            bool containsUniqueFields = false;

            foreach (DataFieldInfo field in fields)
            {
                if ((field.Key & DataFieldKeys.Primary) == DataFieldKeys.Primary)
                {
                    sQuery.AddFields(field.Name);
                    //sQuery.AddCondition(field.Name, ConditionEquality.NotEqual, GetFieldValue(field, this));
                    sQuery.AddCondition(field.Name, GetFieldValue(field, this));
                    uQuery.AddCondition(field.Name, GetFieldValue(field, this));
                    foundKey = true;
                }
            }

            foreach (DataFieldInfo field in fields)
            {
                if ((field.Key & DataFieldKeys.Unique) == DataFieldKeys.Unique)
                {
                    containsUniqueFields = true;
					if (field.Type == typeof(ItemKey))
					{
						sQuery.AddCondition(field.Name + "_id", ((ItemKey)GetFieldValue(field, this)).Id);
						sQuery.AddCondition(field.Name + "_type_id", ((ItemKey)GetFieldValue(field, this)).TypeId);
						
						//uQuery.AddCondition(field.Name + "_id", ((ItemKey)GetFieldValue(field, this)).Id);
						//uQuery.AddCondition(field.Name + "_type_id", ((ItemKey)GetFieldValue(field, this)).TypeId);
					}
					else
					{
                    	sQuery.AddCondition(field.Name, GetFieldValue(field, this));
						//uQuery.AddCondition(field.Name, GetFieldValue(field, this));
					}
                }
            }

            if (!foundKey)
            {
                foreach (DataFieldInfo field in fields)
                {
                    if ((field.Key & DataFieldKeys.Unique) == DataFieldKeys.Unique)
                    {
						if (field.Type == typeof(ItemKey))
						{
							uQuery.AddCondition(field.Name + "_id", ((ItemKey)GetFieldValue(field, this)).Id);
							uQuery.AddCondition(field.Name + "_type_id", ((ItemKey)GetFieldValue(field, this)).TypeId);
						}
						else
						{
                        	uQuery.AddCondition(field.Name, GetFieldValue(field, this));
						}
                        if (updatedItems.Contains(field.Name))
                        {
							/* cannot change a unique key */
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
                EventHandler itemUpdatedHandler = ItemUpdated;
                if (itemUpdatedHandler != null)
                {
                    itemUpdatedHandler(this, new EventArgs());
                }
            }

            return result;
        }

        protected void AuthenticateAction(ItemChangeAction action)
        {
            ItemChangeAuthenticationProviderHandler itemChangeAuthenticationProviderHander = ItemChangeAuthenticationProvider;
            if (itemChangeAuthenticationProviderHander != null)
            {
                itemChangeAuthenticationProviderHander(this, new ItemChangeAuthenticationProviderEventArgs(action));
            }
        }
		
		public static Item Create(Core core, Type type, params FieldValuePair[] fields)
		{
			return Create(core, type, null, false, fields);
		}

        public static Item Create(Core core, Type type, IPermissibleItem parentItem, params FieldValuePair[] fields)
        {
            return Create(core, type, parentItem, false, fields);
        }

        public static Item Create(Core core, Type type, bool suppressReturnItem, params FieldValuePair[] fields)
        {
            return Create(core, type, null, suppressReturnItem, fields);
        }
		
		public static Item Create(Core core, Type type, IPermissibleItem parentItem, bool suppressReturnItem, params FieldValuePair[] fields)
		{
            if (core == null)
            {
                throw new NullCoreException();
            }

            if (parentItem != null)
            {
                if (type.IsSubclassOf(typeof(IPermissibleSubItem)))
                {
                    CreatePermissionAttribute[] createAttributes = (CreatePermissionAttribute[])type.GetCustomAttributes(typeof(CreatePermissionAttribute), false);

                    if (createAttributes.Length == 1)
                    {
                        if (string.IsNullOrEmpty(createAttributes[0].Key))
                        {
                            throw new UnauthorisedToUpdateItemException();
                        }
                        else
                        {
                            if (!parentItem.Access.Can(createAttributes[0].Key))
                            {
                                throw new UnauthorisedToUpdateItemException();
                            }
                        }
                    }
                }
            }

            core.Db.BeginTransaction();

            List<DataFieldInfo> itemFields = GetFields(type);
			
			InsertQuery iQuery = new InsertQuery(GetTable(type));
			
            // Validate Fields
            foreach (FieldValuePair field in fields)
			{
                bool flag = false;
                foreach (DataFieldInfo dfi in itemFields)
                {
                    if (dfi.Name == field.TableField)
                    {
                        if (dfi.Length > 0 && field.Value is string)
                        {
                            if (((string)field.Value).Length > dfi.Length)
                            {
                                throw new FieldTooLongException(dfi.Name);
                            }
                        }
                        flag = true;
                        break;
                    }
                }

                if (!flag)
                {
                    throw new FieldNotFoundException(field.Field);
                }
            }

            // Add Fields to Query statement
			foreach (FieldValuePair field in fields)
			{
				iQuery.AddField(field.Field, field.Value);
			}

			long id = core.Db.Query(iQuery);
			
			if (id > 0)
			{
				if (!suppressReturnItem)
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
                if (!suppressReturnItem)
                {
                    throw new InvalidItemException(type.FullName);
                }
                else
                {
                    return null;
                }
			}
		}

        public static long DeleteItem(Type type, long itemId)
        {
            /*if (this is IPermissibleItem)
            {
                IPermissibleItem iThis = (IPermissibleItem)this;
                //iThis.Access.SetSessionViewer(core.session);
                if (!iThis.Access.Can("DELETE"))
                {
                    throw new UnauthorisedToDeleteItemException();
                }
            }*/

            return 0;
        }

        public long Delete()
        {
            return Delete(false);
        }

        public long Delete(bool parentDeleted)
        {
            if (this is IPermissibleItem)
            {
                IPermissibleItem iThis = (IPermissibleItem)this;
                //iThis.Access.SetSessionViewer(core.session);
                if (!iThis.Access.Can("DELETE"))
                {
                    throw new UnauthorisedToDeleteItemException();
                }
            }

            Type type = this.GetType();

            if (this is IPermissibleSubItem)
            {
                DeletePermissionAttribute[] deleteAttributes = (DeletePermissionAttribute[])type.GetCustomAttributes(typeof(DeletePermissionAttribute), false);

                if (deleteAttributes.Length == 1)
                {
                    if (string.IsNullOrEmpty(deleteAttributes[0].Key))
                    {
                        throw new UnauthorisedToUpdateItemException();
                    }
                    else
                    {
                        IPermissibleSubItem isThis = (IPermissibleSubItem)this;
                        if (!isThis.PermissiveParent.Access.Can(deleteAttributes[0].Key))
                        {
                            throw new UnauthorisedToUpdateItemException();
                        }
                    }
                }
            }

            AuthenticateAction(ItemChangeAction.Delete);

            db.BeginTransaction();

            DeleteQuery dQuery = new DeleteQuery(Item.GetTable(type));

            List<DataFieldInfo> fields = GetFields(type);
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

            if (type.IsSubclassOf(typeof(NumberedItem)))
            {
                /* Delete any ItemInfo rows */
                DeleteQuery idQuery = new DeleteQuery(Item.GetTable(typeof(ItemInfo)));
                idQuery.AddCondition("info_item_id", ((NumberedItem)this).ItemKey.Id);
                idQuery.AddCondition("info_item_type_id", ((NumberedItem)this).ItemKey.TypeId);

                db.Query(idQuery);

                /* Delete any Action rows */
                SelectQuery sQuery = new SelectQuery(typeof(ActionItem));
                sQuery.AddField(new DataField(typeof(Action), "action_id"));
                sQuery.AddField(new DataField(typeof(Action), "action_item_id"));
                sQuery.AddField(new DataField(typeof(Action), "action_item_type_id"));
                sQuery.AddJoin(JoinTypes.Inner, new DataField(typeof(ActionItem), "action_id"), new DataField(typeof(Action), "action_id"));
                sQuery.AddCondition(new DataField(typeof(ActionItem), "item_id"), ((NumberedItem)this).ItemKey.Id);
                sQuery.AddCondition(new DataField(typeof(ActionItem),"item_type_id"), ((NumberedItem)this).ItemKey.TypeId);
                sQuery.AddCondition(new DataField(typeof(ActionItem), "item_id"), ConditionEquality.NotEqual, new DataField(typeof(Action), "action_item_id"));

                DataTable table = db.Query(sQuery);

                idQuery = new DeleteQuery(Item.GetTable(typeof(Action)));
                idQuery.AddCondition("action_item_id", ((NumberedItem)this).ItemKey.Id);
                idQuery.AddCondition("action_item_type_id", ((NumberedItem)this).ItemKey.TypeId);

                db.Query(idQuery);

                /* Delete any ActionItem rows */
                idQuery = new DeleteQuery(Item.GetTable(typeof(ActionItem)));
                idQuery.AddCondition("item_id", ((NumberedItem)this).ItemKey.Id);
                idQuery.AddCondition("item_type_id", ((NumberedItem)this).ItemKey.TypeId);

                db.Query(idQuery);

                /* delete notifications */
                idQuery = new DeleteQuery(Item.GetTable(typeof(Notification)));
                idQuery.AddCondition("action_item_id", ((NumberedItem)this).ItemKey.Id);
                idQuery.AddCondition("action_item_type_id", ((NumberedItem)this).ItemKey.TypeId);

                db.Query(idQuery);

                /* Select action sub items so we can inform the action to re-calculate it's view */
                foreach (DataRow row in table.Rows)
                {
                    long actionId = (long)row["action_id"];
                    long itemId = (long)row["action_item_id"];
                    long itemTypeId = (long)row["action_item_type_id"];
                    ItemKey ik = new ItemKey(itemId, itemTypeId);

                    NumberedItem item = NumberedItem.Reflect(core, ik);

                    if (item != null)
                    {
                        SelectQuery query = ActionItem.GetSelectQueryStub(typeof(ActionItem));
                        query.AddCondition("action_id", actionId);
                        query.LimitCount = 3;

                        DataTable actionItemDataTable = db.Query(query);

                        List<ItemKey> subItemsShortList = new List<ItemKey>();

                        for (int i = 0; i < actionItemDataTable.Rows.Count; i++)
                        {
                            subItemsShortList.Add(new ItemKey((long)actionItemDataTable.Rows[i]["item_id"], (long)actionItemDataTable.Rows[i]["item_type_id"]));
                        }

                        string newBody = ((IActionableItem)item).GetActionBody(subItemsShortList);
                        string newBodyCache = string.Empty;

                        User owner = null;

                        if (!newBody.Contains("[user") && !newBody.Contains("sid=true]"))
                        {
                            newBodyCache = Bbcode.Parse(HttpUtility.HtmlEncode(newBody), null, owner, true, string.Empty, string.Empty);
                        }

                        if (string.IsNullOrEmpty(newBody))
                        {
                            idQuery = new DeleteQuery(Item.GetTable(typeof(Action)));
                            idQuery.AddCondition("action_id", actionId);

                            db.Query(idQuery);
                        }
                        else
                        {
                            UpdateQuery uQuery = new UpdateQuery(typeof(Action));
                            uQuery.AddField("action_body", newBody);
                            uQuery.AddField("action_body_cache", newBodyCache);
                            uQuery.AddCondition("action_id", actionId);

                            if (subItemsShortList != null && subItemsShortList.Count == 1)
                            {
                                uQuery.AddField("interact_item_id", subItemsShortList[0].Id);
                                uQuery.AddField("interact_item_type_id", subItemsShortList[0].TypeId);
                            }
                            else
                            {
                                uQuery.AddField("interact_item_id", new DataField(typeof(Action), "action_item_id"));
                                uQuery.AddField("interact_item_type_id", new DataField(typeof(Action), "action_item_type_id"));
                            }

                            db.Query(uQuery);
                        }
                    }
                }
            }

            long result = db.Query(dQuery);

            if (result > 0)
            {
                ItemDeletedEventHandler itemDeletedHandler = ItemDeleted;
                if (itemDeletedHandler != null)
                {
                    itemDeletedHandler(this, new ItemDeletedEventArgs(parentDeleted));
                }
            }

            return result;
        }

        protected virtual void loadItemInfo(System.Data.Common.DbDataReader reader)
        {
            loadItemInfo(this.GetType(), reader);
        }

        protected void loadItemInfo(Type type, System.Data.Common.DbDataReader reader)
        {
            FieldInfo[] ffields = getFieldInfo(type);

            int fieldsLoaded = 0;
            int objectFields = 0;
            Dictionary<string, FieldInfo> fields = new Dictionary<string, FieldInfo>(ffields.Length, StringComparer.Ordinal);

            foreach (FieldInfo fi in ffields)
            {
                foreach (Attribute attr in Attribute.GetCustomAttributes(fi, typeof(DataFieldAttribute))) // Surely THIS is SLOW??!!
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

            /* buffer for item */
            Dictionary<string, long> ikBufferId = new Dictionary<string, long>(4, StringComparer.Ordinal);
            Dictionary<string, long> ikBufferTypeId = new Dictionary<string, long>(4, StringComparer.Ordinal);
            string ikBufferPrefix = string.Empty;

            if (reader.HasRows)
            {
                bool rowRead = false; /* past participle */
                //while (reader.Read())
                {
                    /* only read one row */
                    if (rowRead)
                    {
                        // Error
						//reader.Close();
                        //reader.Dispose();
                        //throw new InvalidItemException(this.GetType().FullName + " :: Row Count");
                        return;
                    }
                    else
                    {
                        rowRead = true;
                    }

                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        string column = reader.GetName(i);
                        object value = null;

                        FieldInfo fi = null;

                        if (fields.TryGetValue(column, out fi))
                        {
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
                                if (column.EndsWith("_type_id") && ikBufferId.ContainsKey(column.Substring(0, column.Length - 8)))
                                {
                                    if (setProperty(type, column, new ItemKey(ikBufferId[column.Substring(0, column.Length - 8)], (long)value)))
                                    {
                                        fieldsLoaded++;
                                    }
                                }
                                else if (column.EndsWith("_id") && ikBufferTypeId.ContainsKey(column.Substring(0, column.Length - 3)))
                                {
                                    if (setProperty(type, column, new ItemKey((long)value, ikBufferTypeId[column.Substring(0, column.Length - 3)])))
                                    {
                                        fieldsLoaded++;
                                    }
                                }
                                else
                                {
                                    if (column.EndsWith("_id"))
                                    {
                                        ikBufferPrefix = column.Substring(0, column.Length - 3);
                                        ikBufferId.Add(ikBufferPrefix, (long)value);
                                    }
                                    else if (column.EndsWith("_type_id"))
                                    {
                                        ikBufferPrefix = column.Substring(0, column.Length - 8);
                                        ikBufferTypeId.Add(ikBufferPrefix, (long)value);
                                    }
                                }
                            }
                            else if (value is string && fi.FieldType == typeof(char))
                            {
                                if (((string)value).Length > 0)
                                {
                                    if (setProperty(type, column, ((string)value)[0]))
                                    {
                                        fieldsLoaded++;
                                    }
                                }
                                else
                                {
                                    if (setProperty(type, column, null))
                                    {
                                        fieldsLoaded++;
                                    }
                                }
                            }
                            else if (fi.FieldType == typeof(bool) && !(value is bool))
                            {
                                if (value is byte)
                                {
                                    if (setProperty(type, column, ((byte)value > 0) ? true : false))
                                    {
                                        fieldsLoaded++;
                                    }
                                }
                                else if (value is sbyte)
                                {
                                    if (setProperty(type, column, ((sbyte)value > 0) ? true : false))
                                    {
                                        fieldsLoaded++;
                                    }
                                }
                            }
                            else
                            {
                                if (setProperty(type, column, value))
                                {
                                    fieldsLoaded++;
                                }
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

            if (type.IsSubclassOf(typeof(NumberedItem)) && type.Name != "ItemInfo")
            {
                if (typeof(ICommentableItem).IsAssignableFrom(type) ||
                    typeof(ILikeableItem).IsAssignableFrom(type) ||
                    typeof(IRateableItem).IsAssignableFrom(type) ||
                    typeof(ITagableItem).IsAssignableFrom(type) ||
                    typeof(IViewableItem).IsAssignableFrom(type) ||
                    typeof(ISubscribeableItem).IsAssignableFrom(type) ||
                    typeof(IShareableItem).IsAssignableFrom(type) ||
                    typeof(IViewableItem).IsAssignableFrom(type))
                {

                    bool loadIF = false;
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        string column = reader.GetName(i);
                        if (column == "info_item_time_ut")
                        {
                            loadIF = true;
                        }
                    }
                    // the column most likely to be unique
                    if (loadIF)
                    {
                        try
                        {
                            ((NumberedItem)this).info = new ItemInfo(core, reader);
                        }
                        catch (InvalidIteminfoException)
                        {
                            // not all rows will have one yet, but be ready
                        }
                        catch //(Exception ex)
                        {
                            //HttpContext.Current.Response.Write(ex.ToString());
                            //HttpContext.Current.Response.End();
                            // catch all remaining errors
                        }
                    }
                }
            }

            if (ItemLoad != null)
            {
                ItemLoad();
            }

#if DEBUG
            long timerElapsed = timer.ElapsedTicks;
            timer.Stop();
            if (HttpContext.Current != null)
            {
                //HttpContext.Current.Response.Write("<!-- Time loading " + type.Name + ": " + (timerElapsed / 10000000.0).ToString() + "--><!-- slow dbreader path -->\r\n");
            }
#endif
        }

        protected virtual void loadItemInfo(HibernateItem reader)
        {
            throw new NotImplementedException();
        }

        private Dictionary<string, FieldInfo> propertyFields = null;

        protected virtual bool setProperty(string field, object value)
        {
            return setProperty(this.GetType(), field, value);
        }

        protected virtual bool setProperty(Type type, string column, object value)
        {
            FieldInfo[] ffields = getFieldInfo(type);

            if (propertyFields == null)
            {
                Dictionary<string, FieldInfo> fields = new Dictionary<string, FieldInfo>(ffields.Length, StringComparer.Ordinal);

                foreach (FieldInfo fi in ffields)
                {
                    foreach (Attribute attr in Attribute.GetCustomAttributes(fi, typeof(DataFieldAttribute))) // Surely THIS is SLOW??!!
                    {
                        DataFieldAttribute dfattr = (DataFieldAttribute)attr;

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
                propertyFields = fields;
            }

            {
                FieldInfo fi = null;

                if (propertyFields.TryGetValue(column, out fi))
                {
                    fi.SetValue(this, value);
                    return true;
                }
            }

            return false;
        }

        protected virtual void loadItemInfo(DataRow itemRow)
        {
            loadItemInfo(this.GetType(), itemRow);
        }

        protected virtual void loadItemInfo(Type type, DataRow itemRow)
        {
#if DEBUG
            // Profile this method
            Stopwatch timer = new Stopwatch();
            timer.Start();
#endif

            FieldInfo[] fields = getFieldInfo(type);

            //List<string> columns = new List<string>();
            List<string> columnsAttributed = new List<string>(fields.Length);
            int columnCount = 0;

            /*foreach (DataColumn column in itemRow.Table.Columns)
            {
                columns.Add(column.ColumnName);
            }*/

            foreach (FieldInfo fi in fields /*type.GetFields(BindingFlags.Default | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)*/)
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

                            if (itemRow.Table.Columns.Contains(columnName))
	                        {
	                            columnCount++;
                                object colValue = itemRow[columnName];

                                if (!(colValue is DBNull))
	                            {
	                                try
	                                {
                                        if (fi.FieldType == typeof(bool) && !(colValue is bool))
	                                    {
                                            if (colValue is byte)
	                                        {
                                                fi.SetValue(this, ((byte)colValue > 0) ? true : false);
	                                        }
                                            else if (colValue is sbyte)
	                                        {
                                                fi.SetValue(this, ((sbyte)colValue > 0) ? true : false);
	                                        }
	                                    }
                                        else if (colValue is string && fi.FieldType == typeof(char))
                                        {
                                            if (((string)colValue).Length > 0)
                                            {
                                                fi.SetValue(this, ((string)colValue)[0]);
                                            }
                                            else
                                            {
                                                fi.SetValue(this, null);
                                            }
                                        }
	                                    else
	                                    {
                                            fi.SetValue(this, colValue);
	                                    }
	                                }
	                                catch (Exception ex)
	                                {
                                        core.Display.ShowMessage("Type error on load", core.Bbcode.Flatten(columnName + " expected type " + fi.FieldType + " type returned was " + colValue.GetType() + " of value " + itemRow[columnName].ToString() + "\n\n" + ex));
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

            // the column most likely to be unique
            if (itemRow.Table.Columns.Contains("info_item_time_ut"))
            {
                if (type.IsSubclassOf(typeof(NumberedItem)) && type.Name != "ItemInfo")
                {
                    if (typeof(ICommentableItem).IsAssignableFrom(type) ||
                        typeof(ILikeableItem).IsAssignableFrom(type) ||
                        typeof(IRateableItem).IsAssignableFrom(type) ||
                        typeof(ITagableItem).IsAssignableFrom(type) ||
                        typeof(IViewableItem).IsAssignableFrom(type) ||
                        typeof(ISubscribeableItem).IsAssignableFrom(type) ||
                        typeof(IShareableItem).IsAssignableFrom(type) ||
                        typeof(IViewableItem).IsAssignableFrom(type))
                    {

                        try
                        {
                            ((NumberedItem)this).info = new ItemInfo(core, itemRow);
                        }
                        catch (InvalidIteminfoException)
                        {
                            // not all rows will have one yet, but be ready
                        }
                        catch //(Exception ex)
                        {
                            //HttpContext.Current.Response.Write(ex.ToString());
                            //HttpContext.Current.Response.End();
                            // catch all remaining errors
                        }
                    }
                }
            }

            if (ItemLoad != null)
            {
                ItemLoad();
            }

            if (type.IsSubclassOf(typeof(NumberedItem)))
            {
                if (core != null && core.ItemCache != null && (!(type == typeof(ItemType))))
                {
                    core.ItemCache.RegisterItem((NumberedItem)this);
                }
            }

#if DEBUG
            long timerElapsed = timer.ElapsedTicks;
            timer.Stop();
            if (HttpContext.Current != null)
            {
                //HttpContext.Current.Response.Write("<!-- Time loading " + type.Name + ": " + (timerElapsed / 10000000.0).ToString() + "--><!-- default path \r\n" + Environment.StackTrace+ "\r\n-->\r\n");
            }
#endif
        }

        protected void MoveUp(string orderField)
        {
            // TODO: make sure isn't INestableItem
            IOrderableItem oItem = (IOrderableItem)this;

            UpdateQuery uQuery = new UpdateQuery(GetTable(this.GetType()));
            uQuery.AddField(orderField, new QueryOperation(orderField, QueryOperations.Addition, 1));
            uQuery.AddCondition(orderField, oItem.Order);
            oItem.AddSequenceConditon(uQuery);

            core.Db.Query(uQuery);

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

            core.Db.Query(uQuery);

            oItem.Order++;

            this.Update();
        }

        public static void IncrementItemColumn(Core core, Type type, long id, string column, int value)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            UpdateQuery uQuery = new UpdateQuery(GetTable(type));
            uQuery.AddField(column, new QueryOperation(column, QueryOperations.Addition, value));
            uQuery.AddCondition(GetPrimaryKey(type), id);

            core.Db.Query(uQuery);
        }

        protected static void loadValue(DataRow row, string field, out string value)
        {
            if (!(row[field] is DBNull))
            {
                value = (string)row[field];
            }
            else
            {
                value = null;
            }
        }

        protected static void loadValue(System.Data.Common.DbDataReader row, string field, out string value)
        {
            if (!(row[field] is DBNull))
            {
                value = (string)row[field];
            }
            else
            {
                value = null;
            }
        }

        protected static void loadValue(HibernateItem row, string field, out string value)
        {
            if (!(row[field] is DBNull))
            {
                value = (string)row[field];
            }
            else
            {
                value = null;
            }
        }

        protected static void loadValue(DataRow row, string field, out ItemKey value)
        {
            value = new ItemKey((long)row[field + "_id"], (long)row[field + "_type_id"]);
        }

        protected static void loadValue(System.Data.Common.DbDataReader row, string field, out ItemKey value)
        {
            value = new ItemKey((long)row[field + "_id"], (long)row[field + "_type_id"]);
        }

        protected static void loadValue(HibernateItem row, string field, out ItemKey value)
        {
            value = new ItemKey((long)row[field + "_id"], (long)row[field + "_type_id"]);
        }

        protected static void loadValue(DataRow row, string field, out bool value)
        {
            if (row[field] is bool)
            {
                value = (bool)row[field];
            }
            else if (row[field] is byte)
            {
                value = ((byte)row[field] > 0) ? true : false;
            }
            else if (row[field] is sbyte)
            {
                value = ((sbyte)row[field] > 0) ? true : false;
            }
            else
            {
                value = false;
            }
        }

        protected static void loadValue(System.Data.Common.DbDataReader row, string field, out bool value)
        {
            if (row[field] is bool)
            {
                value = (bool)row[field];
            }
            else if (row[field] is byte)
            {
                value = ((byte)row[field] > 0) ? true : false;
            }
            else if (row[field] is sbyte)
            {
                value = ((sbyte)row[field] > 0) ? true : false;
            }
            else
            {
                value = false;
            }
        }

        protected static void loadValue(HibernateItem row, string field, out bool value)
        {
            if (row[field] is bool)
            {
                value = (bool)row[field];
            }
            else if (row[field] is byte)
            {
                value = ((byte)row[field] > 0) ? true : false;
            }
            else if (row[field] is sbyte)
            {
                value = ((sbyte)row[field] > 0) ? true : false;
            }
            else
            {
                value = false;
            }
        }

        protected static void loadValue(DataRow row, string field, out ulong value)
        {
            value = (ulong)row[field];
        }

        protected static void loadValue(System.Data.Common.DbDataReader row, string field, out ulong value)
        {
            value = (ulong)row[field];
        }

        protected static void loadValue(HibernateItem row, string field, out ulong value)
        {
            value = (ulong)row[field];
        }

        protected static void loadValue(DataRow row, string field, out long value)
        {
            value = (long)row[field];
        }

        protected static void loadValue(System.Data.Common.DbDataReader row, string field, out long value)
        {
            value = (long)row[field];
        }

        protected static void loadValue(HibernateItem row, string field, out long value)
        {
            value = (long)row[field];
        }

        protected static void loadValue(DataRow row, string field, out uint value)
        {
            value = (uint)row[field];
        }

        protected static void loadValue(System.Data.Common.DbDataReader row, string field, out uint value)
        {
            value = (uint)row[field];
        }

        protected static void loadValue(HibernateItem row, string field, out uint value)
        {
            value = (uint)row[field];
        }

        protected static void loadValue(DataRow row, string field, out int value)
        {
            value = (int)row[field];
        }

        protected static void loadValue(System.Data.Common.DbDataReader row, string field, out int value)
        {
            value = (int)row[field];
        }

        protected static void loadValue(HibernateItem row, string field, out int value)
        {
            value = (int)row[field];
        }

        protected static void loadValue(DataRow row, string field, out ushort value)
        {
            value = (ushort)row[field];
        }

        protected static void loadValue(System.Data.Common.DbDataReader row, string field, out ushort value)
        {
            value = (ushort)row[field];
        }

        protected static void loadValue(HibernateItem row, string field, out ushort value)
        {
            value = (ushort)row[field];
        }

        protected static void loadValue(DataRow row, string field, out short value)
        {
            value = (short)row[field];
        }

        protected static void loadValue(System.Data.Common.DbDataReader row, string field, out short value)
        {
            value = (short)row[field];
        }

        protected static void loadValue(HibernateItem row, string field, out short value)
        {
            value = (short)row[field];
        }

        protected static void loadValue(DataRow row, string field, out sbyte value)
        {
            value = (sbyte)row[field];
        }

        protected static void loadValue(System.Data.Common.DbDataReader row, string field, out sbyte value)
        {
            value = (sbyte)row[field];
        }

        protected static void loadValue(HibernateItem row, string field, out sbyte value)
        {
            value = (sbyte)row[field];
        }

        protected static void loadValue(DataRow row, string field, out byte value)
        {
            value = (byte)row[field];
        }

        protected static void loadValue(System.Data.Common.DbDataReader row, string field, out byte value)
        {
            value = (byte)row[field];
        }

        protected static void loadValue(HibernateItem row, string field, out byte value)
        {
            value = (byte)row[field];
        }

        protected static void loadValue(DataRow row, string field, out double value)
        {
            value = (double)row[field];
        }

        protected static void loadValue(System.Data.Common.DbDataReader row, string field, out double value)
        {
            value = (double)row[field];
        }

        protected static void loadValue(HibernateItem row, string field, out double value)
        {
            value = (double)row[field];
        }

        protected static void loadValue(DataRow row, string field, out float value)
        {
            value = (float)row[field];
        }

        protected static void loadValue(System.Data.Common.DbDataReader row, string field, out float value)
        {
            value = (float)row[field];
        }

        protected static void loadValue(HibernateItem row, string field, out float value)
        {
            value = (float)row[field];
        }

        protected void itemLoaded(DataRow itemRow)
        {
            Type type = this.GetType();

#if DEBUG
            long timerElapsed = timer.ElapsedTicks;
            timer.Stop();
            if (HttpContext.Current != null)
            {
                //HttpContext.Current.Response.Write("<!-- Time loading " + type.Name + ": " + (timerElapsed / 10000000.0).ToString() + "-->\r\n");
            }
#endif

            // the column most likely to be unique
            if (itemRow.Table.Columns.Contains("info_item_time_ut"))
            {
                if (type.IsSubclassOf(typeof(NumberedItem)) && (!type.Equals(typeof(ItemInfo))))
                {
                    if (typeof(ICommentableItem).IsAssignableFrom(type) ||
                        typeof(ILikeableItem).IsAssignableFrom(type) ||
                        typeof(IRateableItem).IsAssignableFrom(type) ||
                        typeof(ITagableItem).IsAssignableFrom(type) ||
                        typeof(IViewableItem).IsAssignableFrom(type) ||
                        typeof(ISubscribeableItem).IsAssignableFrom(type) ||
                        typeof(IShareableItem).IsAssignableFrom(type) ||
                        typeof(IViewableItem).IsAssignableFrom(type))
                    {

                        try
                        {
                            if (((NumberedItem)this).info == null)
                            {
                                ((NumberedItem)this).info = new ItemInfo(core, itemRow);
                            }
                        }
                        catch (InvalidIteminfoException)
                        {
                            // not all rows will have one yet, but be ready
                        }
                        catch
                        {
                            // catch all remaining errors
                        }
                    }
                }
#if DEBUG
                if (type.Equals(typeof(ItemInfo)))
                {
                    //HttpContext.Current.Response.Write("<!-- " + itemRow["info_item_type_id"].ToString() + "," + itemRow["info_item_id"].ToString() + " -->\r\n");
                }
#endif
            }

            if (ItemLoad != null)
            {
                ItemLoad();
            }
        }

        protected void itemLoaded(System.Data.Common.DbDataReader itemRow)
        {
            Type type = this.GetType();

#if DEBUG
            long timerElapsed = timer.ElapsedTicks;
            timer.Stop();
            if (HttpContext.Current != null)
            {
                //HttpContext.Current.Response.Write("<!-- Time loading " + type.Name + ": " + (timerElapsed / 10000000.0).ToString() + "--><!-- dbreader path -->\r\n");
            }
#endif

            // the column most likely to be unique
            bool hasItemInfo = false;
            for (int i = 0; i < itemRow.FieldCount; i++)
            {
                if (itemRow.GetName(i) == "info_item_time_ut")
                {
                    hasItemInfo = true;
                    break;
                }
            }

            if (hasItemInfo)
            {
                if (type.IsSubclassOf(typeof(NumberedItem)) && (!type.Equals(typeof(ItemInfo))))
                {
                    if (typeof(ICommentableItem).IsAssignableFrom(type) ||
                        typeof(ILikeableItem).IsAssignableFrom(type) ||
                        typeof(IRateableItem).IsAssignableFrom(type) ||
                        typeof(ITagableItem).IsAssignableFrom(type) ||
                        typeof(IViewableItem).IsAssignableFrom(type) ||
                        typeof(ISubscribeableItem).IsAssignableFrom(type) ||
                        typeof(IShareableItem).IsAssignableFrom(type) ||
                        typeof(IViewableItem).IsAssignableFrom(type))
                    {

                        try
                        {
                            if (((NumberedItem)this).info == null)
                            {
                                ((NumberedItem)this).info = new ItemInfo(core, itemRow);
                            }
                        }
                        catch (InvalidIteminfoException)
                        {
                            // not all rows will have one yet, but be ready
                        }
                        catch
                        {
                            // catch all remaining errors
                        }
                    }
                }
#if DEBUG
                if (type.Equals(typeof(ItemInfo)))
                {
                    //HttpContext.Current.Response.Write("<!-- " + itemRow["info_item_type_id"].ToString() + "," + itemRow["info_item_id"].ToString() + " --><!-- dbreader path -->\r\n");
                }
#endif
            }

            if (ItemLoad != null)
            {
                ItemLoad();
            }
        }

        protected void itemLoaded(HibernateItem itemRow)
        {
            Type type = this.GetType();

#if DEBUG
            long timerElapsed = timer.ElapsedTicks;
            timer.Stop();
            if (HttpContext.Current != null)
            {
                //HttpContext.Current.Response.Write("<!-- Time loading " + type.Name + ": " + (timerElapsed / 10000000.0).ToString() + "--><!-- hibernate path -->\r\n");
            }
#endif

            // the column most likely to be unique
            bool hasItemInfo = itemRow.ContainsKey("info_item_time_ut");

            if (hasItemInfo)
            {
                if (type.IsSubclassOf(typeof(NumberedItem)) && (!type.Equals(typeof(ItemInfo))))
                {
                    if (typeof(ICommentableItem).IsAssignableFrom(type) ||
                        typeof(ILikeableItem).IsAssignableFrom(type) ||
                        typeof(IRateableItem).IsAssignableFrom(type) ||
                        typeof(ITagableItem).IsAssignableFrom(type) ||
                        typeof(IViewableItem).IsAssignableFrom(type) ||
                        typeof(ISubscribeableItem).IsAssignableFrom(type) ||
                        typeof(IShareableItem).IsAssignableFrom(type) ||
                        typeof(IViewableItem).IsAssignableFrom(type))
                    {

                        try
                        {
                            if (((NumberedItem)this).info == null)
                            {
                                ((NumberedItem)this).info = new ItemInfo(core, itemRow);
                            }
                        }
                        catch (InvalidIteminfoException)
                        {
                            // not all rows will have one yet, but be ready
                        }
                        catch
                        {
                            // catch all remaining errors
                        }
                    }
                }
#if DEBUG
                if (type.Equals(typeof(ItemInfo)))
                {
                    //HttpContext.Current.Response.Write("<!-- " + itemRow["info_item_type_id"].ToString() + "," + itemRow["info_item_id"].ToString() + " --><!-- hibernate path -->\r\n");
                }
#endif
            }

            if (ItemLoad != null)
            {
                ItemLoad();
            }
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

    public class ItemDeletedEventArgs : EventArgs
    {
        private bool parentDeleted;

        public bool ParentDeleted
        {
            get
            {
                return parentDeleted;
            }
        }

        public ItemDeletedEventArgs(bool parentDeleted)
        {
            this.parentDeleted = parentDeleted;
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

    public class NoUniqueKeyException : Exception
    {
    }

    public class ComplexKeyException : Exception
    {
        public ComplexKeyException()
			: base ()
		{
		}

        public ComplexKeyException(string table)
            : base("Table: " + table)
        {
        }
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

    public class FieldTooLongException : Exception
    {
        private string fieldName;

        public string FieldName
        {
            get
            {
                return fieldName;
            }
        }

        public FieldTooLongException(string field)
            : base("Field: " + field)
        {
            this.fieldName = field;
        }
    }

    public class FieldNotFoundException : Exception
    {
        public FieldNotFoundException()
			: base ()
		{
		}

        public FieldNotFoundException(string field)
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
