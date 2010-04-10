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
using System.Data;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using BoxSocial.Forms;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    /// <summary>
    /// Summary description for Functions
    /// </summary>
    public class Functions
    {
        private Core core;
        private NumberFormatInfo numberFormatInfo;
        private List<string> itemTypes = new List<string>();
        private const string selected = " selected=\"selected\"";
        private const string disabled = " disabled=\"disabled\"";
        private const string boxChecked = " checked=\"checked\"";

        public Functions(Core core)
        {
            this.core = core;
        }

        public static bool CheckPageNameValid(string userName)
        {
            int matches = 0;

            List<string> disallowedNames = new List<string>();
            //disallowedNames.Add("lists");
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

        public byte RequestByte(string var, byte defaultValue)
        {
            byte outValue = defaultValue;
            try
            {
                outValue = byte.Parse(core.Http[var]);
            }
            catch
            {
            }
            return outValue;
        }

        public short RequestShort(string var, short defaultValue)
        {
            short outValue = defaultValue;
            try
            {
                outValue = short.Parse(core.Http[var]);
            }
            catch
            {
            }
            return outValue;
        }

        public int RequestInt(string var, int defaultValue)
        {
            int outValue = defaultValue;
            try
            {
                outValue = int.Parse(core.Http[var]);
            }
            catch
            {
            }
            return outValue;
        }

        public long RequestLong(string var, long defaultValue)
        {
            long outValue = defaultValue;
            try
            {
                outValue = long.Parse(core.Http[var]);
            }
            catch
            {
            }
            return outValue;
        }

        public byte FormByte(string var, byte defaultValue)
        {
            byte outValue = defaultValue;
            try
            {
                outValue = byte.Parse(core.Http.Form[var]);
            }
            catch
            {
            }
            return outValue;
        }

        public short FormShort(string var, short defaultValue)
        {
            short outValue = defaultValue;
            try
            {
                outValue = short.Parse(core.Http.Form[var]);
            }
            catch
            {
            }
            return outValue;
        }

        public ushort FormUShort(string var, ushort defaultValue)
        {
            ushort outValue = defaultValue;
            try
            {
                outValue = ushort.Parse(core.Http.Form[var]);
            }
            catch
            {
            }
            return outValue;
        }

        public int FormInt(string var, int defaultValue)
        {
            int outValue = defaultValue;
            try
            {
                outValue = int.Parse(core.Http.Form[var]);
            }
            catch
            {
            }
            return outValue;
        }

        public long FormLong(string var, long defaultValue)
        {
            long outValue = defaultValue;
            try
            {
                outValue = long.Parse(core.Http.Form[var]);
            }
            catch
            {
            }
            return outValue;
        }

        public byte GetLicenseId()
        {
            return byte.Parse(core.Http.Form["license"]);
        }
        
        public Classifications GetClassification()
        {
            byte a = byte.Parse(core.Http.Form["classification"]);
            return (Classifications)a;
        }

        public SortOrder GetSortOrder()
        {
            return ((core.Http.Query["order"] == "DESC") ? SortOrder.Descending : SortOrder.Ascending);
        }

        public string GetSortCriteria()
        {
            return core.Http.Query["sort"];
        }

        public string GetFilter()
        {
            return core.Http.Query["filter"];
        }

        /*public static string BuildPermissionsBox(ushort permission, List<string> permissions)
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
        }*/

        public static string BuildRadioArray(string name, int columns, List<SelectBoxItem> items, string selectedItem)
        {
            return BuildRadioArray(name, columns, items, selectedItem, new List<string>());
        }

        public static string BuildRadioArray(string name, int columns, List<SelectBoxItem> items, string selectedItem, List<string> disabledItems)
        {
            StringBuilder selectBox = new StringBuilder();
            selectBox.AppendLine("<div style=\"height: 20px;\">");

            for (int i = 0; i < items.Count; i++)
            {
                SelectBoxItem item = items[i];

                if (i % columns == 0)
                {
                    selectBox.AppendLine("</div>");
                    selectBox.AppendLine("<div style=\"height: 20px;\">");
                }

                string icon = "";

                if (!string.IsNullOrEmpty(item.Icon))
                {
                    icon = string.Format("<img src=\"{0}\" alt=\"{1}\" />",
                        item.Icon, item.Text);
                }
                else
                {
                    icon = "<span style=\"display: block; width: 16px; height: 16px; float: left;\"></span>";
                }

                selectBox.AppendLine("<div style=\"float: left; width: 150px\">");

                if (item.Key == selectedItem && disabledItems.Contains(item.Key))
                {
                    selectBox.AppendLine(string.Format("<label>{5}<input type=\"radio\" name=\"{0}\" id=\"{1}\" value=\"{5}\"{2}{3} />{4}</label>",
                        name, name + "-" + item.Key, boxChecked, disabled, item.Text, icon, item.Key));
                }
                if (item.Key == selectedItem)
                {
                    selectBox.AppendLine(string.Format("<label>{4}<input type=\"radio\" name=\"{0}\" id=\"{1}\" value=\"{5}\"{2} />{3}</label>",
                        name, name + "-" + item.Key, boxChecked, item.Text, icon, item.Key));
                }
                else if (disabledItems.Contains(item.Key))
                {
                    selectBox.AppendLine(string.Format("<label>{4}<input type=\"radio\" name=\"{0}\" id=\"{1}\" value=\"{5}\"{2} />{3}</label>",
                        name, name + "-" + item.Key, disabled, item.Text, icon, item.Key));
                }
                else
                {
                    selectBox.AppendLine(string.Format("<label>{3}<input type=\"radio\" name=\"{0}\" id=\"{1}\" value=\"{4}\"/>{2}</label>",
                        name, name + "-" + item.Key, item.Text, icon, item.Key));
                }

                selectBox.AppendLine("</div>");
            }

            selectBox.AppendLine("</div>");

            return selectBox.ToString();
        }

        public static string BuildSelectBox(string name, List<SelectBoxItem> items, string selectedItem)
        {
            return BuildSelectBox(name, items, selectedItem, new List<string>());
        }

        public static string BuildSelectBox(string name, List<SelectBoxItem> items, string selectedItem, List<string> disabledItems)
        {
            StringBuilder selectBox = new StringBuilder();
            selectBox.AppendLine(string.Format("<select name=\"{0}\" id=\"{0}\">",
                name));

            bool hasIcon = false;
            string defaultIcon = "";
            string iconImageName = name + "-icon";

            foreach (SelectBoxItem item in items)
            {
                if (item.Key == selectedItem && disabledItems.Contains(item.Key))
                {
                    selectBox.AppendLine(string.Format("<option value=\"{0}\"{1}{2}>{3}</option>",
                        item.Key, selected, disabled, item.Text));
                }
                else if (item.Key == selectedItem)
                {
                    selectBox.AppendLine(string.Format("<option value=\"{0}\"{1}>{2}</option>",
                        item.Key, selected, item.Text));
                }
                else if (disabledItems.Contains(item.Key))
                {
                    selectBox.AppendLine(string.Format("<option value=\"{0}\"{1}>{2}</option>",
                        item.Key, disabled, item.Text));
                }
                else
                {
                    selectBox.AppendLine(string.Format("<option value=\"{0}\"><img src=\"{2}\" />{1}</option>",
                        item.Key, item.Text, item.Icon));
                }
            }

            selectBox.AppendLine("</select>");

            if (hasIcon)
            {
                selectBox.AppendLine(string.Format("<img src=\"{1}\" id=\"{0}\" />",
                    iconImageName, defaultIcon));
            }

            return selectBox.ToString();
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

        public string IntToMonth(int month)
        {
            switch (month)
            {
                case 1:
                    return core.Prose.GetString("JANUARY");
                case 2:
                    return core.Prose.GetString("FEBURARY");
                case 3:
                    return core.Prose.GetString("MARCH");
                case 4:
                    return core.Prose.GetString("APRIL");
                case 5:
                    return core.Prose.GetString("MAY");
                case 6:
                    return core.Prose.GetString("JUNE");
                case 7:
                    return core.Prose.GetString("JULY");
                case 8:
                    return core.Prose.GetString("AUGUST");
                case 9:
                    return core.Prose.GetString("SEPTEMBER");
                case 10:
                    return core.Prose.GetString("OCTOBER");
                case 11:
                    return core.Prose.GetString("NOVEMBER");
                case 12:
                    return core.Prose.GetString("DECEMBER");
                default:
                    return core.Prose.GetString("INVALID");
            }
        }

        public void Generate404()
        {
            core.Http.StatusCode = 404;
            core.Template.SetTemplate("404.html");
            core.EndResponse();
        }

        public void Generate404(bool customTemplate)
        {
            if (!customTemplate)
            {
                Generate404();
            }
            else
            {
                core.Http.StatusCode = 404;
                core.Template.Parse("IS_404", "TRUE");
            }
        }

        public void Generate403()
        {
            core.Http.StatusCode = 403;
            core.Template.SetTemplate("403.html");
            core.EndResponse();
        }

        public void Generate403(bool customTemplate)
        {
            if (!customTemplate)
            {
                Generate403();
            }
            else
            {
                core.Http.StatusCode = 403;
                core.Template.Parse("IS_403", "TRUE");
            }
        }

        public void ThrowError()
        {
            core.Template.SetTemplate("1202.html");
            core.EndResponse();
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

        public string LargeIntegerToString(long num)
        {
            if (numberFormatInfo == null)
            {
                numberFormatInfo = new CultureInfo("en-AU", false).NumberFormat;
                numberFormatInfo.NumberGroupSeparator = " ";
            }
            return num.ToString("#,0", numberFormatInfo);
        }

        public string LargeIntegerToString(int num)
        {
            return LargeIntegerToString((long)num);
        }

        public string LargeIntegerToString(short num)
        {
            return LargeIntegerToString((long)num);
        }

        public static string TrimStringToWord(string input)
        {
            return TrimStringToWord(input, 60);
        }

        public static string TrimStringToWord(string input, int max)
        {
            char[] spacers = { ' ', '.', '-', '!', '?', '(', ')', '[', ']', '{', '}', ',', '#' };

            if (input.Length < max)
            {
                return input;
            }

            int posn = input.LastIndexOfAny(spacers, max - 1, max);

            if (posn >= 0)
            {
                input = input.Substring(0, posn);
            }
            else
            {
                input = input.Substring(0, max - 1);
            }

            return input;
        }

        public static string TrimStringWithExtension(string input)
        {
            return TrimStringWithExtension(input, 60);
        }

        public static string TrimStringWithExtension(string input, int max)
        {
            if (input.Length < max)
            {
                return input;
            }

            int posn = input.LastIndexOf('.');

            if (posn > 0 && (input.Length - posn) <= 4)
            {
                return input = input.Substring(0, max - (input.Length - posn)) + input.Substring(posn);
            }
            else
            {
                return TrimString(input, max);
            }
        }

        public static string TrimString(string input)
        {
            return TrimString(input, 60);
        }

        public static string TrimString(string input, int max)
        {
            if (input.Length < max)
            {
                return input;
            }
            else
            {
                return input.Substring(0, max);
            }
        }


        public static int LimitPageToStart(int page, int perPage)
        {
            return (page - 1) * perPage;
        }

        public string InterpretDateTime(string date)
        {
            switch (date.ToLower())
            {
                case "today":
                    return core.Tz.Now.ToString("dd/MM/yyyy hh:mm:ss");
                case "tomorrow":
                    return core.Tz.Now.AddDays(1).ToString("dd/MM/yyyy hh:mm:ss");
                case "next week":
                    return core.Tz.Now.AddDays(7).ToString("dd/MM/yyyy hh:mm:ss");
                case "two weeks time":
                    return core.Tz.Now.AddDays(14).ToString("dd/MM/yyyy hh:mm:ss");
            }

            return core.Tz.Now.ToString("dd/MM/yyyy hh:mm:ss");
        }

        public static int ParseNumber(string numeral)
        {
            string[] parts = numeral.ToLower().Split(new char[] { ' ', ',' });
            int number = 0;

            bool first = false;
            bool firstDigit = false;
            bool isNegative = false;
            bool lastAnd = false;

            foreach (string part in parts)
            {
                if (first)
                {
                    first = false;
                    switch (part)
                    {
                        case "minus":
                        case "negative":
                            isNegative = true;
                            break;
                        default:
                            number = ParseNumberPart(part);
                            firstDigit = true;
                            break;
                    }
                    continue;
                }

                if (firstDigit)
                {
                    if (part == "and")
                    {
                        lastAnd = true;
                        continue;
                    }

                    int i = ParseNumberPart(part);

                    if (i >= 100)
                    {
                        number *= i;
                    }
                    else
                    {
                        number += i;
                    }
                    lastAnd = false;
                }
                else
                {
                    number = ParseNumberPart(part);
                    firstDigit = true;
                }
            }

            return number;
        }

        public static int ParseNumberPart(string number)
        {
            switch (number.ToLower())
            {
                case "zero":
                    return 0;
                case "one":
                    return 1;
                case "two":
                    return 2;
                case "three":
                    return 3;
                case "four":
                    return 4;
                case "five":
                    return 5;
                case "six":
                    return 6;
                case "seven":
                    return 7;
                case "eight":
                    return 8;
                case "nine":
                    return 9;
                case "ten":
                    return 10;
                case "eleven":
                    return 11;
                case "twelve":
                    return 12;
                case "thirteen":
                    return 13;
                case "fourteen":
                    return 14;
                case "fiften":
                    return 15;
                case "sixteen":
                    return 16;
                case "seventeen":
                    return 17;
                case "eighteen":
                    return 18;
                case "ninteen":
                    return 19;
                case "twenty":
                    return 20;
                case "thirty":
                    return 30;
                case "fourty":
                    return 40;
                case "fifty":
                    return 50;
                case "sixty":
                    return 60;
                case "seventy":
                    return 70;
                case "eighty":
                    return 80;
                case "ninety":
                    return 90;
                case "hundred":
                    return 100;
                case "thousand":
                    return 1000;
                case "million":
                    return 1000000;
                case "billion":
                    return 1000000000;
                /*case "trillion":
                    return 1000000000000;*/
                default:
                    return 0;
            }
        }
    }
}
