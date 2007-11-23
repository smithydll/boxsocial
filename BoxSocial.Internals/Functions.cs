/*
 * Box Social�
 * http://boxsocial.net/
 * Copyright � 2007, David Lachlan Smith
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
using System.Data;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Security;
using Lachlan.Web;

namespace BoxSocial.Internals
{
    /// <summary>
    /// Summary description for Functions
    /// </summary>
    public class Functions
    {
        private static NumberFormatInfo numberFormatInfo;
        private static List<string> itemTypes = new List<string>();
        private static string selected = " selected=\"selected\"";
        private static string disabled = " disabled=\"disabled\"";
        private static string boxChecked = " checked=\"checked\"";

        public static bool CheckPageNameValid(string userName)
        {
            int matches = 0;

            List<string> disallowedNames = new List<string>();
            disallowedNames.Add("blog");
            disallowedNames.Add("friends");
            disallowedNames.Add("gallery");
            disallowedNames.Add("lists");
            disallowedNames.Sort();

            if (disallowedNames.BinarySearch(userName) >= 0)
            {
                matches++;
            }

            if (!Regex.IsMatch(userName, @"^([A-Za-z0-9\-_\.\!~\*'&=\$].+)$"))
            {
                matches++;
            }

            userName = userName.Normalize().ToLower();

            if (userName.Length < 2)
            {
                matches++;
            }

            if (userName.Length > 64)
            {
                matches++;
            }

            if (userName.EndsWith(".aspx"))
            {
                matches++;
            }

            if (userName.EndsWith(".php"))
            {
                matches++;
            }

            if (userName.EndsWith(".html"))
            {
                matches++;
            }

            if (userName.EndsWith(".gif"))
            {
                matches++;
            }

            if (userName.EndsWith(".png"))
            {
                matches++;
            }

            if (userName.EndsWith(".js"))
            {
                matches++;
            }

            if (userName.EndsWith(".bmp"))
            {
                matches++;
            }

            if (userName.EndsWith(".jpg"))
            {
                matches++;
            }

            if (userName.EndsWith(".jpeg"))
            {
                matches++;
            }

            if (userName.EndsWith(".zip"))
            {
                matches++;
            }

            if (userName.EndsWith(".jsp"))
            {
                matches++;
            }

            if (userName.EndsWith(".cfm"))
            {
                matches++;
            }

            if (userName.EndsWith(".exe"))
            {
                matches++;
            }

            if (userName.StartsWith("."))
            {
                matches++;
            }

            if (userName.EndsWith("."))
            {
                matches++;
            }

            if (matches > 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public static int RequestInt(string var, int defaultValue)
        {
            int outValue = defaultValue;
            try
            {
                outValue = int.Parse(HttpContext.Current.Request.QueryString[var]);
            }
            catch
            {
            }
            return outValue;
        }

        public static ushort GetPermission(HttpRequest Request)
        {
            ushort permission = 0;

            for (int i = 0; i < 16; i++)
            {
                if (Request.Form[string.Format("perm-{0:X4}", 1 << i)] != null)
                {
                    permission = (ushort)(permission | (ushort)(1 << i));
                }
            }

            return permission;
        }

        public static string BuildPermissionsBox(ushort permission, List<string> permissions)
        {
            StringBuilder permissionsBox = new StringBuilder();

            permissionsBox.AppendLine("<table id=\"perms-table\">");
            permissionsBox.AppendLine("<tr>");
            permissionsBox.AppendLine("<th>Setting</th>");
            permissionsBox.AppendLine("<th>Friends</th>");
            permissionsBox.AppendLine("<th>Family</th>");
            permissionsBox.AppendLine("<th>Group</th>");
            permissionsBox.AppendLine("<th>Other (Everyone else)</th>");
            permissionsBox.AppendLine("</tr>");

            for (int i = 0; i < permissions.Count; i++)
            {
                permissionsBox.AppendLine("<tr>");
                permissionsBox.AppendLine(string.Format("<td>{0}</td>", permissions[i]));
                for (int j = 3; j >= 0; j--)
                {
                    ushort permissionsMask = (ushort)(1 << i << (j * 4));
                    if ((ushort)(permission & permissionsMask) == permissionsMask)
                    {
                        permissionsBox.AppendLine(string.Format("<td class=\"check\"><input type=\"checkbox\" name=\"perm-{0:X4}\"{1} /></td>", permissionsMask, boxChecked));
                    }
                    else
                    {
                        permissionsBox.AppendLine(string.Format("<td class=\"check\"><input type=\"checkbox\" name=\"perm-{0:X4}\" /></td>", permissionsMask));
                    }
                }
                permissionsBox.AppendLine("</tr>");
            }

            permissionsBox.AppendLine("</table>");

            return permissionsBox.ToString();
        }

        public static string BuildSelectBox(string name, Dictionary<string, string> items, string selectedItem)
        {
            return BuildSelectBox(name, items, selectedItem, new List<string>());
        }

        public static string BuildSelectBox(string name, Dictionary<string, string> items, string selectedItem, List<string> disabledItems)
        {
            StringBuilder selectBox = new StringBuilder();
            selectBox.AppendLine(string.Format("<select name=\"{0}\" id=\"{0}\">",
                name));

            foreach (string key in items.Keys)
            {
                if (key == selectedItem && disabledItems.Contains(key))
                {
                    selectBox.AppendLine(string.Format("<option value=\"{0}\"{1}{2}>{3}</option>",
                        key, selected, disabled, items[key]));
                }
                else if (key == selectedItem)
                {
                    selectBox.AppendLine(string.Format("<option value=\"{0}\"{1}>{2}</option>",
                        key, selected, items[key]));
                }
                else if (disabledItems.Contains(key))
                {
                    selectBox.AppendLine(string.Format("<option value=\"{0}\"{1}>{2}</option>",
                        key, disabled, items[key]));
                }
                else
                {
                    selectBox.AppendLine(string.Format("<option value=\"{0}\">{1}</option>",
                        key, items[key]));
                }
            }

            selectBox.AppendLine("</select>");

            return selectBox.ToString();
        }

        public static string IntToMonth(int month)
        {
            switch (month)
            {
                case 1:
                    return "Janurary";
                case 2:
                    return "Feburary";
                case 3:
                    return "March";
                case 4:
                    return "April";
                case 5:
                    return "May";
                case 6:
                    return "June";
                case 7:
                    return "July";
                case 8:
                    return "August";
                case 9:
                    return "September";
                case 10:
                    return "October";
                case 11:
                    return "November";
                case 12:
                    return "December";
                default:
                    return "Invalid";
            }
        }

        public static bool IsValidItemType(string itemType)
        {
            if (itemTypes.Count == 0)
            {
                itemTypes.Add("PHOTO");
                itemTypes.Add("BLOGPOST");
                itemTypes.Add("PODCAST");
                itemTypes.Add("PODCASTEPISODE");
                itemTypes.Add("USER");
                itemTypes.Add("PAGE");
                itemTypes.Add("LIST");
                itemTypes.Add("GROUP");
                itemTypes.Add("NETWORK");
            }
            return itemTypes.Contains(itemType);
        }

        public static void Generate404(Core core)
        {
            HttpContext.Current.Response.StatusCode = 404;
            core.template.SetTemplate("404.html");
            core.EndResponse();
        }

        public static void Generate404(Core core, bool customTemplate)
        {
            if (!customTemplate)
            {
                Generate404(core);
            }
            else
            {
                HttpContext.Current.Response.StatusCode = 404;
                core.template.ParseVariables("IS_404", "TRUE");
            }
        }

        public static void Generate403(Core core)
        {
            HttpContext.Current.Response.StatusCode = 403;
            core.template.SetTemplate("403.html");
            core.EndResponse();
        }

        public static void Generate403(Core core, bool customTemplate)
        {
            if (!customTemplate)
            {
                Generate403(core);
            }
            else
            {
                HttpContext.Current.Response.StatusCode = 403;
                core.template.ParseVariables("IS_403", "TRUE");
            }
        }

        public static string GenerateBreadCrumbs(string userName, string path)
        {
            string[] paths = path.Split('/');
            string output = "";

            path = string.Format("/{0}", userName);
            output = string.Format("<a href=\"{1}\">{0}</a>",
                    userName, path);

            for (int i = 0; i < paths.Length; i++)
            {
                if (paths[i] != "")
                {
                    path += "/" + paths[i];
                    output += string.Format(" <strong>&#8249;</strong> <a href=\"{1}\">{0}</a>",
                        paths[i], path);
                }
            }

            return output;
        }

        public static string LargeIntegerToString(long num)
        {
            if (numberFormatInfo == null)
            {
                numberFormatInfo = new CultureInfo("en-AU", false).NumberFormat;
                numberFormatInfo.NumberGroupSeparator = " ";
            }
            return num.ToString("#,0", numberFormatInfo);
        }

        public static string LargeIntegerToString(int num)
        {
            return LargeIntegerToString((long)num);
        }

        public static string LargeIntegerToString(short num)
        {
            return LargeIntegerToString((long)num);
        }

    }
}
