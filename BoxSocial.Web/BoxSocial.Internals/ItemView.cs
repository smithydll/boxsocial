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
using System.Text;
using System.Text.RegularExpressions;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    public enum ItemViewDiscountedReason : int
    {
        None = 0x0000,
        InvalidSession = 0x0001,
        ItemOwner = 0x0002,
        BotDetected = 0x0004,
        LowQuality = 0x0008,
        RateLimited = 0x000F,
        IpAddress = 0x0010,
        ShortViewTime = 0x0020,
        BadUserAgent = 0x0040,
        OldUserAgent = 0x0080,
    }

    public enum ItemViewState : int
    {
        New = 1,
        Exited = 2,
        Background = 3,
        Foreground = 4,
        Inactive = 5,
        Active = 6,
        Timeout = 8
    }

    [DataTable("item_views")]
    public class ItemView : NumberedItem
    {
        [DataField("view_id", DataFieldKeys.Primary)]
        private long viewId;
        [DataField("view_item")]
        private ItemKey itemKey;
        [DataField("view_item_owner")]
        private ItemKey ownerKey;
        [DataField("user_id")]
        private long userId;
        [DataField("view_ip", 50)]
        private string viewIp;
        [DataField("view_session_id", 32)]
        private string viewSessionId;
        [DataField("view_time_ut")]
        private long viewTimeRaw;
        [DataField("view_timespan")]
        private long viewTimespan;
        [DataField("view_update_time_ut")]
        private long viewUpdateTimeRaw;
        [DataField("view_referral_uri", 2000)]
        private string viewReferralUri;
        [DataField("view_http_referer", 2000)]
        private string viewHttpReferer;
        [DataField("view_http_user_agent", 255)]
        private string viewHttpUserAgent;
        [DataField("view_cookies")]
        private bool viewCookies;
        [DataField("view_javascript")]
        private bool viewJavascript;
        [DataField("view_counted")]
        private bool viewCounted;
        [DataField("view_discounted")]
        private bool viewDiscounted;
        [DataField("view_discounted_reason")]
        private int viewDiscountedReason;
        [DataField("view_processed")]
        private bool viewProcessed;
        [DataField("view_quality")]
        private int viewQuality;
        [DataField("view_state")]
        private int viewState;

        public override long Id
        {
            get
            {
                return viewId;
            }
        }

        public ItemKey ViewKey
        {
            get
            {
                return itemKey;
            }
        }

        public bool Counted
        {
            get
            {
                return viewCounted;
            }
        }

        public bool Discounted
        {
            get
            {
                return viewDiscounted;
            }
        }

        public bool Processed
        {
            get
            {
                return viewProcessed;
            }
        }

        public string HttpUserAgent
        {
            get
            {
                return viewHttpUserAgent;
            }
        }

        public override string Uri
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        private ItemView(Core core, DataRow viewRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(ItemView_ItemLoad);

            loadItemInfo(viewRow);
        }

        private ItemView(Core core, long viewId)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(ItemView_ItemLoad);

            SelectQuery query = ItemView.GetSelectQueryStub(core, typeof(ItemView));
            query.AddCondition("view_id", viewId);

            System.Data.Common.DbDataReader viewReader = db.ReaderQuery(query);

            if (viewReader.HasRows)
            {
                viewReader.Read();

                loadItemInfo(viewReader);

                viewReader.Close();
                viewReader.Dispose();
            }
            else
            {
                throw new InvalidItemViewException();
            }
        }

        protected override void loadItemInfo(DataRow viewRow)
        {
            loadValue(viewRow, "view_id", out viewId);
            loadValue(viewRow, "view_item", out itemKey);
            loadValue(viewRow, "view_item_owner", out ownerKey);
            loadValue(viewRow, "view_ip", out viewIp);
            loadValue(viewRow, "view_session_id", out viewSessionId);
            loadValue(viewRow, "view_time_ut", out viewTimeRaw);
            loadValue(viewRow, "view_timespan", out viewTimespan);
            loadValue(viewRow, "view_update_time_ut", out viewUpdateTimeRaw);
            loadValue(viewRow, "view_referral_uri", out viewReferralUri);
            loadValue(viewRow, "view_http_referer", out viewHttpReferer);
            loadValue(viewRow, "view_http_user_agent", out viewHttpUserAgent);
            loadValue(viewRow, "view_cookies", out viewCookies);
            loadValue(viewRow, "view_javascript", out viewJavascript);
            loadValue(viewRow, "view_counted", out viewCounted);
            loadValue(viewRow, "view_discounted", out viewDiscounted);
            loadValue(viewRow, "view_discounted_reason", out viewDiscountedReason);
            loadValue(viewRow, "view_processed", out viewProcessed);
            loadValue(viewRow, "view_quality", out viewQuality);
            loadValue(viewRow, "view_state", out viewState);

            itemLoaded(viewRow);
            core.ItemCache.RegisterItem((NumberedItem)this);
        }

        void ItemView_ItemLoad()
        {

        }

        public static void UpdateView(Core core)
        {
            UpdateView(core, true);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="core"></param>
        public static void UpdateView(Core core, bool response)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            long viewId = core.Functions.FormLong("vid", core.Functions.RequestLong("vid", 0));
            string mode = core.Http.Form["view-mode"]; // tick, background, foreground, unload
            if (string.IsNullOrEmpty(mode))
            {
                mode = core.Http["view-mode"];
            }
            long timestamp = UnixTime.UnixTimeStamp();

            if (viewId > 0)
            {
                ItemView view = new ItemView(core, viewId);

                if (view.viewSessionId == core.Session.SessionId)
                {
                    switch (mode.ToLower())
                    {
                        case "tick":
                            if (view.viewState != (int)ItemViewState.Exited && view.viewState != (int)ItemViewState.Inactive)
                            {
                                if (timestamp - view.viewUpdateTimeRaw < 120) // ticks happen every 60 seconds with a 60 second page timeout
                                {
                                    UpdateQuery uQuery = new UpdateQuery(typeof(ItemView));
                                    uQuery.AddField("view_timespan", new QueryOperation("view_timespan", QueryOperations.Addition, timestamp - view.viewUpdateTimeRaw));
                                    uQuery.AddField("view_update_time_ut", timestamp);
                                    uQuery.AddField("view_cookies", core.Session.SessionMethod == SessionMethods.Cookie);
                                    uQuery.AddField("view_javascript", true);
                                    uQuery.AddField("view_state", (int)ItemViewState.Foreground);
                                    uQuery.AddCondition("view_id", viewId);

                                    core.Db.Query(uQuery);
                                }
                                else
                                {
                                    UpdateQuery uQuery = new UpdateQuery(typeof(ItemView));
                                    uQuery.AddField("view_update_time_ut", timestamp);
                                    uQuery.AddField("view_state", (int)ItemViewState.Foreground);
                                    uQuery.AddCondition("view_id", viewId);

                                    core.Db.Query(uQuery);
                                }
                            }
                            break;
                        case "background":
                        case "unload":
                        case "inactive":
                            {
                                UpdateQuery uQuery = new UpdateQuery(typeof(ItemView));
                                uQuery.AddField("view_javascript", true);
                                if (timestamp - view.viewUpdateTimeRaw < 120)
                                {
                                    uQuery.AddField("view_timespan", new QueryOperation("view_timespan", QueryOperations.Addition, timestamp - view.viewUpdateTimeRaw));
                                }
                                uQuery.AddField("view_update_time_ut", timestamp);
                                if (mode.ToLower() == "unload")
                                {
                                    uQuery.AddField("view_state", (int)ItemViewState.Exited);
                                }
                                else if (mode.ToLower() == "inactive")
                                {
                                    uQuery.AddField("view_state", (int)ItemViewState.Inactive);
                                }
                                else
                                {
                                    uQuery.AddField("view_state", (int)ItemViewState.Background);
                                }
                                uQuery.AddCondition("view_id", viewId);

                                core.Db.Query(uQuery);
                            }
                            break;
                        case "foreground":
                            {
                                UpdateQuery uQuery = new UpdateQuery(typeof(ItemView));
                                uQuery.AddField("view_update_time_ut", timestamp);
                                uQuery.AddField("view_state", (int)ItemViewState.Foreground);
                                uQuery.AddCondition("view_id", viewId);

                                core.Db.Query(uQuery);
                            }
                            break;
                        case "active":
                            {
                                UpdateQuery uQuery = new UpdateQuery(typeof(ItemView));
                                uQuery.AddField("view_update_time_ut", timestamp);
                                uQuery.AddField("view_state", (int)ItemViewState.Active);
                                uQuery.AddCondition("view_id", viewId);

                                core.Db.Query(uQuery);
                            }
                            break;
                    }
                }
                else
                {
                    // probably a view bot
                    UpdateQuery uQuery = new UpdateQuery(typeof(ItemView));
                    uQuery.AddField("view_discounted", true);
                    uQuery.AddField("view_processed", true);
                    uQuery.SetBitField("view_discounted_reason", (int)ItemViewDiscountedReason.InvalidSession);
                    uQuery.AddCondition("view_id", viewId);

                    core.Db.Query(uQuery);
                }
            }

            if (response)
            {
                core.Response.SendStatus("viewLogged");
            }
        }

        public static void LogView(Core core, NumberedItem item)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            if (item.Id < 1)
            {
                throw new InvalidItemException();
            }

            if (core.Session.IsBot)
            {
                // Do not count page views from robots
                return;
            }

            ItemViewDiscountedReason reason = ItemViewDiscountedReason.None;
            ItemKey ownerKey = null;

            if (item is IPermissibleItem)
            {
                IPermissibleItem pitem = (IPermissibleItem)item;

                ownerKey = pitem.OwnerKey;

                if (core.Session.SignedIn && pitem.OwnerKey == core.LoggedInMemberItemKey)
                {
                    reason = ItemViewDiscountedReason.ItemOwner;
                }
            }

            if (item is IPermissibleSubItem)
            {
                IPermissibleSubItem pitem = (IPermissibleSubItem)item;

                ownerKey = pitem.OwnerKey;

                if (core.Session.SignedIn && pitem.OwnerKey == core.LoggedInMemberItemKey)
                {
                    reason = ItemViewDiscountedReason.ItemOwner;
                }
            }

            long timestamp = UnixTime.UnixTimeStamp();
            string urlreferer = core.Http.UrlReferer;

            if (string.IsNullOrEmpty(urlreferer))
            {
                SelectQuery sQuery = Session.GetSelectQueryStub(core, typeof(Session));
                sQuery.AddCondition("session_string", core.Session.SessionId);

                DataTable sessionTable = core.Db.Query(sQuery);
                if (sessionTable.Rows.Count == 1)
                {
                    if (sessionTable.Rows[0]["session_http_referer"] is string)
                    {
                        string session_referer = (string)sessionTable.Rows[0]["session_http_referer"];
                        if (!string.IsNullOrEmpty(session_referer))
                        {
                            urlreferer = session_referer;
                        }
                    }
                }
            }

            InsertQuery iQuery = new InsertQuery("item_views");
            iQuery.AddField("view_item_id", item.ItemKey.Id);
            iQuery.AddField("view_item_type_id", item.ItemKey.TypeId);
            if (ownerKey != null)
            {
                iQuery.AddField("view_item_owner_id", ownerKey.Id);
                iQuery.AddField("view_item_owner_type_id", ownerKey.TypeId);
            }
            if (core.Session.SignedIn)
            {
                iQuery.AddField("user_id", core.LoggedInMemberId);
            }
            iQuery.AddField("view_ip", core.Session.IPAddress.ToString());
            iQuery.AddField("view_session_id", core.Session.SessionId);
            iQuery.AddField("view_time_ut", timestamp);
            iQuery.AddField("view_timespan", 0);
            iQuery.AddField("view_update_time_ut", timestamp);
            iQuery.AddField("view_referral_uri", core.Http["ref"]);
            iQuery.AddField("view_http_referer", urlreferer);
            iQuery.AddField("view_http_user_agent", core.Http.UserAgent);
            iQuery.AddField("view_cookies", core.Session.SessionMethod == SessionMethods.Cookie);
            iQuery.AddField("view_javascript", core.Http.BrowserIdentifiesJavascript);
            iQuery.AddField("view_counted", false);
            iQuery.AddField("view_discounted", false);
            iQuery.AddField("view_processed", false);
            iQuery.AddField("view_discounted_reason", (int)reason);
            iQuery.AddField("view_state", (int)ItemViewState.New);

            // commit the query
            long viewId = core.Db.Query(iQuery);

            core.Template.Parse("ITEM_VIEW_ID", viewId.ToString());
        }

        public static void ProcessViews(Core core)
        {
            Dictionary<ItemKey, long> adjustment = new Dictionary<ItemKey, long>();

            SelectQuery query = ItemView.GetSelectQueryStub(core, typeof(ItemView));
            query.AddCondition("view_time_ut", ConditionEquality.LessThan, UnixTime.UnixTimeStamp() - 60 * 60 * 24);
            query.AddCondition("view_processed", false);
            query.LimitCount = 30;

            DataTable viewsDataTable = core.Db.Query(query);

            List<ItemView> views = new List<ItemView>();

            foreach (DataRow row in viewsDataTable.Rows)
            {
                views.Add(new ItemView(core, row));
            }

            core.Db.BeginTransaction();

            foreach (ItemView view in views)
            {
                ItemInfo info = null;
                try
                {
                    info = new ItemInfo(core, view.ViewKey);
                }
                catch (InvalidIteminfoException)
                {
                    info = ItemInfo.Create(core, view.ViewKey);
                }

                if (info == null)
                {
                    continue;
                }

                if (!adjustment.ContainsKey(view.ViewKey))
                {
                    adjustment.Add(view.ViewKey, 0);
                }

                // If the view unique enough to be counted?
                ItemViewDiscountedReason viewUniqueReason = VerifyView(core, view);
                long increment = 0;

                UpdateQuery uQuery = new UpdateQuery(typeof(ItemView));
                uQuery.AddCondition("view_id", view.viewId);

                if (!view.Processed)
                {
                    if (viewUniqueReason == ItemViewDiscountedReason.None)
                    {
                        uQuery.AddField("view_counted", true);
                        uQuery.AddField("view_discounted", false);

                        adjustment[view.ViewKey]++;
                        increment++;
                    }
                    else
                    {
                        uQuery.AddField("view_counted", false);
                        uQuery.AddField("view_discounted", true);
                    }
                }
                else
                {
                    if (viewUniqueReason == ItemViewDiscountedReason.None)
                    {
                        uQuery.AddField("view_counted", true);
                        uQuery.AddField("view_discounted", false);

                        if (view.viewDiscounted)
                        {
                            adjustment[view.ViewKey]++;
                            increment++;
                        }
                    }
                    else
                    {
                        uQuery.AddField("view_counted", false);
                        uQuery.AddField("view_discounted", true);

                        if (view.viewCounted)
                        {
                            adjustment[view.ViewKey]--;
                            increment--;
                        }
                    }
                }
                uQuery.AddField("view_processed", true);
                uQuery.AddField("view_discounted_reason", (int)viewUniqueReason);

                core.Db.Query(uQuery);

                if (increment != 0 || (viewUniqueReason == ItemViewDiscountedReason.RateLimited && view.viewTimespan > 0))
                {
                    uQuery = new UpdateQuery(typeof(ItemViewCountByHour));
                    uQuery.AddField("view_hourly_count", new QueryOperation("view_hourly_count", QueryOperations.Addition, increment));
                    uQuery.AddField("view_hourly_time", new QueryOperation("view_hourly_time", QueryOperations.Addition, Math.Sign(increment) * Math.Min(view.viewTimespan, 20 * 60))); // attention span is 20 minutes
                    uQuery.AddCondition("view_hourly_time_ut", (view.viewTimeRaw / 60 / 60) * 60 * 60);
                    uQuery.AddCondition("view_hourly_item_id", view.ViewKey.Id);
                    uQuery.AddCondition("view_hourly_item_type_id", view.ViewKey.TypeId);

                    if (core.Db.Query(uQuery) == 0)
                    {
                        InsertQuery iQuery = new InsertQuery(typeof(ItemViewCountByHour));
                        iQuery.AddField("view_hourly_count", increment);
                        iQuery.AddField("view_hourly_time", Math.Sign(increment) * Math.Min(view.viewTimespan, 20 * 60)); // attention span is 20 minutes
                        iQuery.AddField("view_hourly_time_ut", (view.viewTimeRaw / 60 / 60) * 60 * 60);
                        iQuery.AddField("view_hourly_item_id", view.ViewKey.Id);
                        iQuery.AddField("view_hourly_item_type_id", view.ViewKey.TypeId);
                        if (view.ownerKey.Id > 0 && view.ownerKey.TypeId > 0)
                        {
                            iQuery.AddField("view_hourly_item_owner_id", view.ownerKey.Id);
                            iQuery.AddField("view_hourly_item_owner_type_id", view.ownerKey.TypeId);
                        }
                        else
                        {
                            NumberedItem item = NumberedItem.Reflect(core, view.ViewKey);
                            ItemKey ownerKey = null;

                            if (item is IPermissibleItem)
                            {
                                IPermissibleItem pitem = (IPermissibleItem)item;

                                ownerKey = pitem.OwnerKey;
                            }

                            if (item is IPermissibleSubItem)
                            {
                                IPermissibleSubItem pitem = (IPermissibleSubItem)item;

                                ownerKey = pitem.OwnerKey;
                            }

                            if (ownerKey != null)
                            {
                                iQuery.AddField("view_hourly_item_owner_id", ownerKey.Id);
                                iQuery.AddField("view_hourly_item_owner_type_id", ownerKey.TypeId);
                            }
                        }

                        core.Db.Query(iQuery);
                    }
                }
            }

            foreach (ItemKey itemKey in adjustment.Keys)
            {
                if (adjustment[itemKey] != 0)
                {
                    UpdateQuery uQuery = new UpdateQuery(typeof(ItemInfo));
                    uQuery.AddField("info_viewed_times", new QueryOperation("info_viewed_times", QueryOperations.Addition, adjustment[itemKey]));
                    uQuery.AddCondition("info_item_id", itemKey.Id);
                    uQuery.AddCondition("info_item_type_id", itemKey.TypeId);
                    core.Db.Query(uQuery);
                }
            }

            core.Db.CommitTransaction();
        }

        private static ItemViewDiscountedReason VerifyView(Core core, ItemView view)
        {
            ItemViewDiscountedReason returnValue = ItemViewDiscountedReason.None;

            if (SessionState.IsBotUserAgent(view.HttpUserAgent) != null)
            {
                return ItemViewDiscountedReason.BotDetected;
            }

            // Select the number of views within 24 hours of the view
            SelectQuery query = new SelectQuery(typeof(ItemView));
            query.AddField(new QueryFunction("view_id", QueryFunctions.Count, "real_views"));
            query.AddCondition("view_item_id", view.ViewKey.Id);
            query.AddCondition("view_item_type_id", view.ViewKey.TypeId);
            //query.AddCondition("view_processed", true);
            QueryCondition qc1 = query.AddCondition("view_ip", view.viewIp);
            QueryCondition qc2 = qc1.AddCondition("view_time_ut", ConditionEquality.GreaterThan, view.viewTimeRaw - 60 * 60 * 24); // last 24 hours
            qc2.AddCondition("view_time_ut", ConditionEquality.LessThan, view.viewTimeRaw + 60 * 5); // any in the next 5 minutes discounts the whole set
            qc1.AddCondition(ConditionRelations.Or, "view_session_id", view.viewSessionId);
            if (view.userId > 0)
            {
                qc1.AddCondition(ConditionRelations.Or, "user_id", view.userId);
            }

            if ((long)core.Db.Query(query).Rows[0]["real_views"] > 1)
            {
                returnValue = returnValue | ItemViewDiscountedReason.RateLimited;
            }

            if ((!view.viewHttpUserAgent.ToLower().Contains("mozilla/") && !view.viewHttpUserAgent.ToLower().Contains("gecko")) || view.viewHttpUserAgent.Length < 32)
            {
                returnValue = returnValue | ItemViewDiscountedReason.BadUserAgent;
            }

            //
            query = new SelectQuery(typeof(ItemView));
            query.AddField(new QueryFunction("view_id", QueryFunctions.Count, "real_views"));
            query.AddCondition("view_ip", view.viewIp);
            query.AddCondition("view_time_ut", ConditionEquality.GreaterThan, view.viewTimeRaw - 10);
            query.AddCondition("view_time_ut", ConditionEquality.LessThan, view.viewTimeRaw + 10);
            if ((long)core.Db.Query(query).Rows[0]["real_views"] > 8)
            {
                returnValue = returnValue | ItemViewDiscountedReason.RateLimited;
            }

            // ensure that the session is only used by ONE browser, no session hijacking
            query = new SelectQuery(typeof(ItemView));
            query.AddField(new DataField(typeof(ItemView), "view_http_user_agent"));
            query.AddCondition("view_session_id", view.viewSessionId);
            query.AddCondition("view_time_ut", ConditionEquality.GreaterThan, view.viewTimeRaw - 3600);
            query.AddCondition("view_time_ut", ConditionEquality.LessThan, view.viewTimeRaw + 3600);
            query.Distinct = true;
            if ((long)core.Db.Query(query).Rows.Count > 1)
            {
                returnValue = returnValue | ItemViewDiscountedReason.BotDetected;
            }

            long viewQuality = 0;

            if (view.viewTimespan > 60)
            {
                viewQuality += 3;
            }
            else if (view.viewTimespan > 30)
            {
                viewQuality += 2;
            }
            else if (view.viewTimespan > 5)
            {
                viewQuality++;
            }

            if (view.viewJavascript)
            {
                viewQuality++;
            }

            if (view.viewCookies)
            {
                viewQuality++;
            }
            else
            {
                // cookies can't be detected on the landing page, but a legit IP with a cookie is unlikely to go bad even if shared
                query = new SelectQuery(typeof(ItemView));
                query.AddField(new QueryFunction("view_id", QueryFunctions.Count, "real_views"));
                query.AddCondition("view_ip", view.viewIp);
                query.AddCondition("view_cookies", true);
                if ((long)core.Db.Query(query).Rows[0]["real_views"] > 0)
                {
                    viewQuality++;
                }
            }

            if (IsRecentBrowser(view.viewHttpUserAgent))
            {
                viewQuality += 2;
            }
            else
            {
                returnValue = returnValue | ItemViewDiscountedReason.OldUserAgent;
            }

            if (!string.IsNullOrEmpty(view.viewHttpReferer))
            {
                viewQuality++;
            }

            if (viewQuality < 3)
            {
                returnValue = returnValue | ItemViewDiscountedReason.LowQuality;
            }

            return returnValue;
        }

        private static bool IsRecentBrowser(string userAgent)
        {
            Dictionary<string, List<string>> matches = ParseUserAgent(userAgent);

            if (userAgent.Contains("Edge/") && matches.ContainsKey("Edge"))
            {
                string versionString = matches["Edge"][0];
                List<int> version = GetVersionParts(versionString);

                if (version[0] < 13)
                {
                    return false;
                }
            }
            else if (userAgent.Contains("OPR/") && matches.ContainsKey("OPR"))
            {
                string versionString = matches["OPR"][0];
                List<int> version = GetVersionParts(versionString);

                if (version[0] < 20)
                {
                    return false;
                }
            }
            else if (userAgent.Contains("SamsungBrowser/") && matches.ContainsKey("SamsungBrowser"))
            {
                string versionString = matches["SamsungBrowser"][0];
                List<int> version = GetVersionParts(versionString);

                if (version[0] < 2)
                {
                    return false;
                }
            }
            else if (userAgent.Contains("Firefox/") && matches.ContainsKey("Firefox"))
            {
                string versionString = matches["Firefox"][0];
                List<int> version = GetVersionParts(versionString);

                if (version[0] < 38)
                {
                    return false;
                }
            }
            else if (userAgent.Contains("Chrome/") && matches.ContainsKey("Chrome"))
            {
                string versionString = matches["Chrome"][0];
                List<int> version = GetVersionParts(versionString);

                if (version[0] < 45)
                {
                    return false;
                }
            }
            else if (userAgent.Contains("Chromium/") && matches.ContainsKey("Chromium"))
            {
                string versionString = matches["Chromium"][0];
                List<int> version = GetVersionParts(versionString);

                if (version[0] < 45)
                {
                    return false;
                }
            }
            else if (userAgent.Contains("Android") && userAgent.Contains("Version/") && matches.ContainsKey("Version"))
            {
                string versionString = matches["Version"][0];
                List<int> version = GetVersionParts(versionString);

                if (version[0] < 4)
                {
                    return false;
                }
            }
            else if (userAgent.Contains("Safari/") && userAgent.Contains("AppleWebKit/") && userAgent.Contains("Version/") && matches.ContainsKey("Version"))
            {
                string versionString = matches["Version"][0];
                List<int> version = GetVersionParts(versionString);

                if (version[0] < 8)
                {
                    return false;
                }
            }
            else if (userAgent.Contains("MSIE 10.0;") || userAgent.Contains("MSIE 9.0;") || userAgent.Contains("MSIE 8.0;") || userAgent.Contains("MSIE 7.0;") || userAgent.Contains("MSIE 6.0;"))
            {
                return false;
            }

            return true; // default to just accept
        }

        private static List<int> GetVersionParts(string version)
        {
            List<int> parts = new List<int>();
            Match versionNumbers = Regex.Match(version, @"(\d+)((\.(\d+))*)");
            if (versionNumbers.Success)
            {
                string versionString = versionNumbers.Value;
                string[] versionParts = versionString.Split(new char[] { '.' });

                foreach (string part in versionParts)
                {
                    parts.Add(int.Parse(part));
                }
            }
            else
            {
                // default to version 0.0
                parts.Add(0);
                parts.Add(0);
            }

            return parts;
        }

        private static Dictionary<string, List<string>> ParseUserAgent(string userAgent)
        {
            userAgent = userAgent.Trim();
            Dictionary<string, List<string>> parts = new Dictionary<string, List<string>>();

            int inParenthases = 0;
            int part = 1;
            string part1 = string.Empty;
            string part2 = string.Empty;
            string part3 = string.Empty;
            for (int i = 0; i < userAgent.Length; i++)
            {
                char c = userAgent[i];

                if (c == '(')
                {
                    if (inParenthases == 0)
                    {
                        part = 3;
                    }
                    inParenthases++;
                }

                if (inParenthases == 0)
                {
                    if (c == ' ')
                    {
                        if (userAgent[i + 1] != '(')
                        {
                            if (!parts.ContainsKey(part1))
                            {
                                parts.Add(part1, new List<string> { part2, part3 });
                                part = 1;
                                part1 = string.Empty;
                                part2 = string.Empty;
                                part3 = string.Empty;
                                continue;
                            }
                        }
                    }

                    if (c == '/')
                    {
                        part = 2;
                        continue;
                    }
                }

                if (c == ')')
                {
                    inParenthases--;
                }

                if (part == 1)
                {
                    part1 += c;
                }
                else if (part == 2)
                {
                    part2 += c;
                }
                else if (part == 3)
                {
                    part3 += c;
                }
            }

            if (!parts.ContainsKey(part1))
            {
                parts.Add(part1, new List<string> { part2, part3 });
            }

            return parts;
        }
    }

    public class InvalidItemViewException : Exception
    {

    }
}
