﻿/*
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
using System.Web;
using BoxSocial.Forms;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    public class DateTimePicker : FormField
    {
        private Core core;
        private DateTime value;
        private bool showTime;
        private bool showSeconds;
        private bool disabled;
        private StyleLength width;

        public DateTime Value
        {
            get
            {
                return value;
            }
            set
            {
                this.value = value;
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

        public bool ShowTime
        {
            get
            {
                return showTime;
            }
            set
            {
                showTime = value;
            }
        }

        public bool ShowSeconds
        {
            get
            {
                return showSeconds;
            }
            set
            {
                showSeconds = value;
            }
        }

        public DateTimePicker(Core core, string name)
        {
            this.core = core;
            this.name = name;

            disabled = false;
            showTime = false;
            showSeconds = false;
            width = new StyleLength(100F, LengthUnits.Percentage);
        }

        public override string ToString(Forms.DisplayMedium medium)
        {
            // This will be a complicated mishmash of javascript

            HiddenField modeHiddenField = new HiddenField(name + "--mode");
            modeHiddenField.Class = "date-mode";
            modeHiddenField.Value = "forms";

            TextBox dateExpressionTextBox = new TextBox(name + "--expression");
            //dateExpressionTextBox.IsVisible = false;
            dateExpressionTextBox.Script.OnChange = "ParseDatePicker('" + name + "--expression" + "', " + (int)medium + ")";
            dateExpressionTextBox.Width.Length = Width.Length * 0.4F;
            dateExpressionTextBox.Width.Unit = Width.Unit;
            if (medium == DisplayMedium.Mobile)
            {
                dateExpressionTextBox.Type = InputType.Date;
            }

            TextBox timeExpressionTextBox = new TextBox(name + "--time");
            //timeExpressionTextBox.IsVisible = false;
            timeExpressionTextBox.Script.OnChange = "ParseTimePicker('" + name + "--time" + "')";
            timeExpressionTextBox.Width.Length = Width.Length * 0.4F;
            timeExpressionTextBox.Width.Unit = Width.Unit;
            if (medium == DisplayMedium.Mobile)
            {
                timeExpressionTextBox.Type = InputType.Time;
            }

            SelectBox dateYearsSelectBox = new SelectBox(name + "--date-year");
            SelectBox dateMonthsSelectBox = new SelectBox(name + "--date-month");
            SelectBox dateDaysSelectBox = new SelectBox(name + "--date-day");

            SelectBox dateHoursSelectBox = new SelectBox(name + "--date-hour");
            SelectBox dateMinutesSelectBox = new SelectBox(name + "--date-minute");
            SelectBox dateSecondsSelectBox = new SelectBox(name + "--date-second");

            for (int i = DateTime.Now.AddYears(-30).Year; i < DateTime.Now.AddYears(5).Year; i++)
            {
                dateYearsSelectBox.Add(new SelectBoxItem(i.ToString(), i.ToString()));
            }

            for (int i = 1; i < 13; i++)
            {
                dateMonthsSelectBox.Add(new SelectBoxItem(i.ToString(), core.Functions.IntToMonth(i)));
                dateMonthsSelectBox.Add(new SelectBoxItem(i.ToString(), i.ToString()));
            }

            for (int i = 1; i < 32; i++)
            {
                dateDaysSelectBox.Add(new SelectBoxItem(i.ToString(), i.ToString()));
            }

            for (int i = 0; i < 24; i++)
            {
                dateHoursSelectBox.Add(new SelectBoxItem(i.ToString(), i.ToString()));
            }

            for (int i = 0; i < 60; i++)
            {
                dateMinutesSelectBox.Add(new SelectBoxItem(i.ToString(), i.ToString()));
            }

            for (int i = 0; i < 60; i++)
            {
                dateSecondsSelectBox.Add(new SelectBoxItem(i.ToString(), i.ToString()));
            }

            dateYearsSelectBox.SelectedKey = value.Year.ToString();
            dateMonthsSelectBox.SelectedKey = value.Month.ToString();
            dateDaysSelectBox.SelectedKey = value.Day.ToString();

            if (medium == DisplayMedium.Mobile)
            {
                dateExpressionTextBox.Value = value.ToString("yyyy-MM-dd");
            }
            else
            {
                dateExpressionTextBox.Value = value.ToString("dd/MM/yyyy");
            }
            timeExpressionTextBox.Value = value.ToString("HH:mm:ss");

            /* Build display */
            StringBuilder sb = new StringBuilder();

            if (medium == DisplayMedium.Mobile)
            {
                sb.AppendLine("<div class=\"date-field\">");
                sb.AppendLine(modeHiddenField.ToString());

                sb.AppendLine("<p id=\"" + name + "[date-field]\" class=\"date-exp\" style=\"display: none;\">");
                sb.Append(core.Prose.GetString("DATE") + ": ");
                sb.Append(dateExpressionTextBox.ToString());
                if (ShowTime)
                {
                    sb.Append(" " + core.Prose.GetString("TIME") + ": ");
                    sb.Append(timeExpressionTextBox.ToString());
                }
                sb.Append("</p>");

                sb.AppendLine("</div>");
            }
            else
            {
                sb.AppendLine("<div class=\"date-field\">");
                sb.AppendLine(modeHiddenField.ToString());

                sb.AppendLine("<p id=\"" + name + "[date-drop]\" class=\"date-drop\">");
                sb.Append(core.Prose.GetString("YEAR") + ": ");
                sb.AppendLine(dateYearsSelectBox.ToString());
                sb.AppendLine(" " + core.Prose.GetString("MONTH") + ": ");
                sb.AppendLine(dateMonthsSelectBox.ToString());
                sb.AppendLine(" " + core.Prose.GetString("DAY") + ": ");
                sb.AppendLine(dateDaysSelectBox.ToString());

                if (showTime)
                {
                    sb.AppendLine(" " + core.Prose.GetString("HOUR") + ": ");
                    sb.AppendLine(dateHoursSelectBox.ToString());
                    sb.AppendLine(" " + core.Prose.GetString("MINUTE") + ": ");
                    sb.AppendLine(dateMinutesSelectBox.ToString());
                    if (showSeconds)
                    {
                        sb.AppendLine(" " + core.Prose.GetString("SECOND") + ": ");
                        sb.AppendLine(dateSecondsSelectBox.ToString());
                    }
                }
                sb.Append("</p>");

                sb.AppendLine("<p id=\"" + name + "[date-field]\" class=\"date-exp\" style=\"display: none;\">");
                sb.Append(core.Prose.GetString("DATE") + ": ");
                sb.Append(dateExpressionTextBox.ToString());
                if (ShowTime)
                {
                    sb.Append(" " + core.Prose.GetString("TIME") + ": ");
                    sb.Append(timeExpressionTextBox.ToString());
                }
                sb.Append("</p>");

                sb.AppendLine("</div>");

                sb.AppendLine("<script type=\"text/javascript\">//<![CDATA[");
                sb.AppendLine("dtp.push(Array(\"" + name + "[date-drop]\",\"" + name + "[date-field]\"));");
                sb.AppendLine("EnableDateTimePickers();");
                sb.AppendLine("//]]></script>");
            }

            return sb.ToString();
        }

        public static long FormDate(Core core, string name)
        {
            return FormDate(core, name, core.Tz);
        }

        public static long FormDate(Core core, string name, ushort timeZoneCode)
        {
            return FormDate(core, name, new UnixTime(core, timeZoneCode));
        }

        public static long FormDate(Core core, string name, UnixTime tz)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            long datetime = 0;
            DateTime dt = tz.Now;

            string dateExpression = core.Http.Form[name + "--expression"];
            string timeExpression = core.Http.Form[name + "--time"];
            string dateMode = core.Http.Form[name + "--mode"];

            if (dateMode == "ajax" && (!string.IsNullOrEmpty(dateExpression)))
            {
                dateExpression = core.Functions.InterpretDate(dateExpression, DisplayMedium.Desktop);
                timeExpression = core.Functions.InterpretTime(timeExpression);

                string expression = dateExpression + " " + timeExpression;

                if (!DateTime.TryParse(expression, out dt))
                {
                    int year = core.Functions.FormInt(name + "--date-year", dt.Year);
                    int month = core.Functions.FormInt(name + "--date-month", dt.Month);
                    int day = core.Functions.FormInt(name + "--date-day", dt.Day);
                    int hour = core.Functions.FormInt(name + "--date-hour", dt.Hour);
                    int minute = core.Functions.FormInt(name + "--date-minute", dt.Minute);
                    int second = core.Functions.FormInt(name + "--date-second", 0);

                    dt = new DateTime(year, month, day, hour, minute, second);
                }
            }
            else
            {
                int year = core.Functions.FormInt(name + "--date-year", dt.Year);
                int month = core.Functions.FormInt(name + "--date-month", dt.Month);
                int day = core.Functions.FormInt(name + "--date-day", dt.Day);
                int hour = core.Functions.FormInt(name + "--date-hour", dt.Hour);
                int minute = core.Functions.FormInt(name + "--date-minute", dt.Minute);
                int second = core.Functions.FormInt(name + "--date-second", 0);

                dt = new DateTime(year, month, day, hour, minute, second);
            }

            datetime = tz.GetUnixTimeStamp(dt);

            return datetime;
        }

        public override void SetValue(string value)
        {
            // Do Nothing
        }
    }
}
