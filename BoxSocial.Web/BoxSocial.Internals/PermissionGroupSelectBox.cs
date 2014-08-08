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
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using BoxSocial.Forms;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    public class PermissionGroupSelectBox : FormField
    {
        private Core core;
        private ItemKey permissibleItem;
        private bool selectMultiple = true;
        private List<PrimitivePermissionGroup> itemKeys;
        private StyleLength width;
        protected bool visible;
        protected bool disabled;
        private ScriptProperty script;

        public ItemKey PermissibleItem
        {
            get
            {
                return permissibleItem;
            }
            set
            {
                permissibleItem = value;
            }
        }

        public bool IsDisabled
        {
            get
            {
                return disabled;
            }
            set
            {
                disabled = value;
            }
        }

        public bool IsVisible
        {
            get
            {
                return visible;
            }
            set
            {
                visible = value;
            }
        }

        public bool SelectMultiple
        {
            get
            {
                return selectMultiple;
            }
            set
            {
                selectMultiple = value;
            }
        }

        public StyleLength Width
        {
            get
            {
                return width;
            }
            set
            {
                width = value;
            }
        }

        public List<PrimitivePermissionGroup> ItemKeys
        {
            get
            {
                return itemKeys;
            }
            set
            {
                itemKeys = value;
            }
        }

        public PermissionGroupSelectBox(Core core, string name, ItemKey permissibleItem)
        {
            this.core = core;
            this.name = name;
            this.permissibleItem = permissibleItem;

            disabled = false;
            visible = true;

            itemKeys = new List<PrimitivePermissionGroup>();
            width = new StyleLength(100F, LengthUnits.Percentage);
            script = new ScriptProperty();
        }

        public PermissionGroupSelectBox(Core core, string name, ItemKey permissibleItem, List<PrimitivePermissionGroup> itemKeys)
        {
            this.core = core;
            this.name = name;
            this.permissibleItem = permissibleItem;
            this.itemKeys = itemKeys;

            disabled = false;
            visible = true;

            width = new StyleLength(100F, LengthUnits.Percentage);
            script = new ScriptProperty();
        }

        public override string ToString()
        {
            return ToString(Forms.DisplayMedium.Desktop);
        }

        public override string ToString(Forms.DisplayMedium medium)
        {
            try
            {
                List<ItemKey> primitiveItemKeys = new List<ItemKey>();

                foreach (PrimitivePermissionGroup ppg in itemKeys)
                {
                    if (ppg.ItemKey.GetType(core).IsPrimitive)
                    {
                        primitiveItemKeys.Add(ppg.ItemKey);
                    }
                }

                core.PrimitiveCache.LoadPrimitiveProfiles(primitiveItemKeys);

                StringBuilder users = new StringBuilder();
                StringBuilder idList = new StringBuilder();

                bool first = true;
                foreach (PrimitivePermissionGroup ppg in itemKeys)
                {
                    if (!first)
                    {
                        idList.Append(",");
                    }
                    else
                    {
                        first = false;
                    }
                    idList.AppendFormat("{0}-{1}", ppg.TypeId, ppg.ItemId);

                    if (ppg.ItemKey.GetType(core).IsPrimitive)
                    {
                        if (ppg.ItemKey.Id > 0 && ppg.ItemKey.TypeId == ItemType.GetTypeId(core, typeof(User)))
                        {
                            switch (medium)
                            {
                                case Forms.DisplayMedium.Desktop:
                                    users.Append(string.Format("<span class=\"username\">{2}<span class=\"delete\" onclick=\"rvl($(this).parent().siblings('.ids'),'{1}-{0}'); $(this).parent().siblings\'.ids').trigger(\'change\'); $(this).parent().remove();\">x</span><input type=\"hidden\" id=\"group-{1}-{0}\" name=\"group[{1},{0}]\" value=\"{1},{0}\" /></span>", ppg.ItemKey.Id, ppg.ItemKey.TypeId, HttpUtility.HtmlEncode(core.PrimitiveCache[ppg.ItemKey].DisplayName)));
                                    break;
                                case DisplayMedium.Mobile:
                                case DisplayMedium.Tablet:
                                    users.Append(string.Format("<span class=\"item-{1}-{0} username\">{2}<input type=\"hidden\" id=\"group-{1}-{0}\" name=\"group[{1},{0}]\" value=\"{1},{0}\" /></span>", ppg.ItemKey.Id, ppg.ItemKey.TypeId, HttpUtility.HtmlEncode(core.PrimitiveCache[ppg.ItemKey].DisplayName)));
                                    break;
                            }
                        }
                        else if (ppg.ItemKey.Id > 0)
                        {
                            switch (medium)
                            {
                                case Forms.DisplayMedium.Desktop:
                                    users.Append(string.Format("<span class=\"group\">{2}<span class=\"delete\" onclick=\"rvl($(this).parent().siblings('.ids'),'{1}-{0}'); $(this).parent().siblings('.ids').trigger(\'change\'); $(this).parent().remove();\">x</span><input type=\"hidden\" id=\"group-{1}-{0}\" name=\"group[{1},{0}]\" value=\"{1},{0}\" /></span>", ppg.ItemKey.Id, ppg.ItemKey.TypeId, HttpUtility.HtmlEncode(core.PrimitiveCache[ppg.ItemKey].DisplayName)));
                                    break;
                                case DisplayMedium.Mobile:
                                case DisplayMedium.Tablet:
                                    users.Append(string.Format("<span class=\"item-{1}-{0} group\">{2}<input type=\"hidden\" id=\"group-{1}-{0}\" name=\"group[{1},{0}]\" value=\"{1},{0}\" /></span>", ppg.ItemKey.Id, ppg.ItemKey.TypeId, HttpUtility.HtmlEncode(core.PrimitiveCache[ppg.ItemKey].DisplayName)));
                                    break;
                            }
                        }
                        else if (!string.IsNullOrEmpty(ppg.LanguageKey))
                        {
                            switch (medium)
                            {
                                case Forms.DisplayMedium.Desktop:
                                    users.Append(string.Format("<span class=\"group\">{2}<span class=\"delete\" onclick=\"rvl($(this).parent().siblings('.ids'),'{1}-{0}'); $(this).parent().siblings('.ids').trigger(\'change\'); $(this).parent().remove();\">x</span><input type=\"hidden\" id=\"group-{1}-{0}\" name=\"group[{1},{0}]\" value=\"{1},{0}\" /></span>", ppg.ItemKey.Id, ppg.ItemKey.TypeId, core.Prose.GetString(ppg.LanguageKey)));
                                    break;
                                case DisplayMedium.Mobile:
                                case DisplayMedium.Tablet:
                                    users.Append(string.Format("<span class=\"item-{1}-{0} group\">{2}<input type=\"hidden\" id=\"group-{1}-{0}\" name=\"group[{1},{0}]\" value=\"{1},{0}\" /></span>", ppg.ItemKey.Id, ppg.ItemKey.TypeId, core.Prose.GetString(ppg.LanguageKey)));
                                    break;
                            }
                        }
                        else
                        {
                            switch (medium)
                            {
                                case Forms.DisplayMedium.Desktop:
                                    users.Append(string.Format("<span class=\"group\">{2}<span class=\"delete\" onclick=\"rvl($(this).parent().siblings('.ids'),'{1}-{0}'); $(this).parent().siblings('.ids').trigger(\'change\'); $(this).parent().remove();\">x</span><input type=\"hidden\" id=\"group-{1}-{0}\" name=\"group[{1},{0}]\" value=\"{1},{0}\" /></span>", ppg.ItemKey.Id, ppg.ItemKey.TypeId, HttpUtility.HtmlEncode(ppg.DisplayName)));
                                    break;
                                case DisplayMedium.Mobile:
                                case DisplayMedium.Tablet:
                                    users.Append(string.Format("<span class=\"item-{1}-{0} group\">{2}<input type=\"hidden\" id=\"group-{1}-{0}\" name=\"group[{1},{0}]\" value=\"{1},{0}\" /></span>", ppg.ItemKey.Id, ppg.ItemKey.TypeId, HttpUtility.HtmlEncode(ppg.DisplayName)));
                                    break;
                            }
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(ppg.LanguageKey))
                        {
                            switch (medium)
                            {
                                case Forms.DisplayMedium.Desktop:
                                    users.Append(string.Format("<span class=\"group\">{2}<span class=\"delete\" onclick=\"rvl($(this).parent().siblings('.ids'),'{1}-{0}'); $(this).parent().siblings('.ids').trigger(\'change\'); $(this).parent().remove();\">x</span><input type=\"hidden\" id=\"group-{1}-{0}\" name=\"group[{1},{0}]\" value=\"{1},{0}\" /></span>", ppg.ItemKey.Id, ppg.ItemKey.TypeId, core.Prose.GetString(ppg.LanguageKey)));
                                    break;
                                case DisplayMedium.Mobile:
                                case DisplayMedium.Tablet:
                                    users.Append(string.Format("<span class=\"item-{1}-{0} group\">{2}<input type=\"hidden\" id=\"group-{1}-{0}\" name=\"group[{1},{0}]\" value=\"{1},{0}\" /></span>", ppg.ItemKey.Id, ppg.ItemKey.TypeId, core.Prose.GetString(ppg.LanguageKey)));
                                    break;
                            }
                        }
                        else
                        {
                            switch (medium)
                            {
                                case Forms.DisplayMedium.Desktop:
                                    users.Append(string.Format("<span class=\"group\">{2}<span class=\"delete\" onclick=\"rvl($(this).parent().siblings('.ids'),'{1}-{0}'); $(this).parent().siblings('.ids').trigger(\'change\'); $(this).parent().remove();\">x</span><input type=\"hidden\" id=\"group-{1}-{0}\" name=\"group[{1},{0}]\" value=\"{1},{0}\" /></span>", ppg.ItemKey.Id, ppg.ItemKey.TypeId, HttpUtility.HtmlEncode(ppg.DisplayName)));
                                    break;
                                case DisplayMedium.Mobile:
                                case DisplayMedium.Tablet:
                                    users.Append(string.Format("<span class=\"item-{1}-{0} group\">{2}<input type=\"hidden\" id=\"group-{1}-{0}\" name=\"group[{1},{0}]\" value=\"{1},{0}\" /></span>", ppg.ItemKey.Id, ppg.ItemKey.TypeId, HttpUtility.HtmlEncode(ppg.DisplayName)));
                                    break;
                            }
                        }
                    }
                }

                switch (medium)
                {
                    case Forms.DisplayMedium.Desktop:
                        return string.Format("<div id=\"{0}\" class=\"permission-group-droplist\" onclick=\"$(this).children('.textbox').focus();\" style=\"width: {4};{3}\"><span class=\"empty\" style=\"{10}\">Type names to set permissions, or leave blank to inherit.</span>{6}<input type=\"text\" name=\"{0}-text\" id=\"{0}-text\" value=\"{1}\" class=\"textbox\" style=\"\"{2}{5}/><input type=\"hidden\" name=\"{0}-ids\" id=\"{0}-ids\" class=\"ids\" value=\"{9}\"/><input type=\"hidden\" name=\"{0}-id\" id=\"{0}-id\" class=\"item-id\" value=\"{7}\" /><input type=\"hidden\" name=\"{0}-type-id\" id=\"{0}-type-id\" class=\"item-type-id\" value=\"{8}\" /></div>",
                                HttpUtility.HtmlEncode(name),
                                HttpUtility.HtmlEncode(string.Empty),
                                (IsDisabled) ? " disabled=\"disabled\"" : string.Empty,
                                (!IsVisible) ? " display: none;" : string.Empty,
                                width,
                                Script.ToString(),
                                users.ToString(),
                                (permissibleItem != null) ? permissibleItem.Id.ToString() : "0",
                                (permissibleItem != null) ? permissibleItem.TypeId.ToString() : "0",
                                idList.ToString(),
                                (idList.Length > 0) ? "display: none" : string.Empty);
                    case Forms.DisplayMedium.Mobile:
                    case Forms.DisplayMedium.Tablet:
                        return string.Format("<div id=\"{0}\" class=\"permission-group-droplist\" onclick=\"showUsersBar(event, '{0}', 'permissions');\" style=\"width: {4};{3}\">{6}<input type=\"text\" name=\"{0}-text\" id=\"{0}-text\" value=\"{1}\" class=\"textbox\" style=\"\"{2}{5}/><input type=\"hidden\" name=\"{0}-ids\" id=\"{0}-ids\" class=\"ids\" value=\"{9}\"/><input type=\"hidden\" name=\"{0}-id\" id=\"{0}-id\" class=\"item-id\" value=\"{7}\" /><input type=\"hidden\" name=\"{0}-type-id\" id=\"{0}-type-id\" class=\"item-type-id\" value=\"{8}\" /></div>",
                                HttpUtility.HtmlEncode(name),
                                HttpUtility.HtmlEncode(string.Empty),
                                (IsDisabled) ? " disabled=\"disabled\"" : string.Empty,
                                (!IsVisible) ? " display: none;" : string.Empty,
                                width,
                                Script.ToString(),
                                users.ToString(),
                                (permissibleItem != null) ? permissibleItem.Id.ToString() : "0",
                                (permissibleItem != null) ? permissibleItem.TypeId.ToString() : "0",
                                idList.ToString(),
                                (idList.Length > 0) ? "display: none" : string.Empty);
                    default:
                        return string.Empty;
                }
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }

        public static List<PrimitivePermissionGroup> FormPermissionGroups(Core core, string name)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            List<PrimitivePermissionGroup> groupIds = new List<PrimitivePermissionGroup>();

            string formValue = core.Http.Form[name + "-ids"];
            if (!string.IsNullOrEmpty(formValue))
            {
                string[] ids = formValue.Split(new char[] { ',' });

                foreach (string idString in ids)
                {
                    string[] idStringParts = Regex.Replace(idString, "(\\d)\\-", "$1,").Split(new char[] { ',' });
                    long typeId;
                    long id;
                    long.TryParse(idStringParts[0], out typeId);
                    long.TryParse(idStringParts[1], out id);

                    if (id != 0 && typeId > 0)
                    {
                        groupIds.Add(new PrimitivePermissionGroup(new ItemKey(id, typeId), string.Empty, string.Empty));
                    }
                }
            }

            int limit = 10;
            string userNames = core.Http.Form[name + "-text"];

            if (!string.IsNullOrEmpty(userNames))
            {
                string[] usernames = userNames.Split(new char[] { ',', ';', ' ' });

                foreach (string username in usernames)
                {
                    if (limit > 0)
                    {
                        long id = core.PrimitiveCache.LoadUserProfile(username);
                        groupIds.Add(new PrimitivePermissionGroup(new ItemKey(id, ItemType.GetTypeId(core, typeof(User))), string.Empty, string.Empty));
                        limit--;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return groupIds;
        }

        public ScriptProperty Script
        {
            get
            {
                return script;
            }
        }

        public override void SetValue(string value)
        {
            itemKeys = FormPermissionGroups(core, value);
        }
    }
}
