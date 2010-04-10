﻿/*
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
        }

        public override string ToString()
        {
            // This will be a complicated mishmash of javascript

            TextBox dateExpressionTextBox = new TextBox(name + "[expression]");
            dateExpressionTextBox.IsVisible = false;

            SelectBox dateYearsSelectBox = new SelectBox(name + "[date-year]");
            SelectBox dateMonthsSelectBox = new SelectBox(name + "[date-month]");
            SelectBox dateDaysSelectBox = new SelectBox(name + "[date-day]");

            SelectBox dateHoursSelectBox = new SelectBox(name + "[date-hour]");
            SelectBox dateMinutesSelectBox = new SelectBox(name + "[date-minute]");
            SelectBox dateSecondsSelectBox = new SelectBox(name + "[date-second]");

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

            for (int i = 1; i < 13; i++)
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

            /* Build display */
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("<div class=\"date-field\">");

            sb.AppendLine("<p class=\"date-drop\">");
            sb.Append("Year: ");
            sb.AppendLine(dateYearsSelectBox.ToString());
            sb.AppendLine(" Month: ");
            sb.AppendLine(dateMonthsSelectBox.ToString());
            sb.AppendLine(" Day: ");
            sb.AppendLine(dateDaysSelectBox.ToString());

            if (showTime)
            {
                sb.AppendLine(" Hour: ");
                sb.AppendLine(dateHoursSelectBox.ToString());
                sb.AppendLine(" Minute: ");
                sb.AppendLine(dateMinutesSelectBox.ToString());
                if (showSeconds)
                {
                    sb.AppendLine(" Second: ");
                    sb.AppendLine(dateSecondsSelectBox.ToString());
                }
            }
            sb.Append("</p>");

            sb.AppendLine("<p class=\"date-exp\">");
            sb.Append(dateExpressionTextBox.ToString());
            sb.Append("</p>");

            sb.AppendLine("</div>");

            return sb.ToString();
        }

        public static long FormDate(Core core, string name, ushort timeZoneCode)
        {
            long datetime = 0;
            UnixTime tz = new UnixTime(core, timeZoneCode);
            DateTime dt = tz.Now;

            string expression = core.Http.Form[name + "[expression]"];

            if (!string.IsNullOrEmpty(expression))
            {
                expression = core.Functions.InterpretDateTime(expression);

                if (!DateTime.TryParse(expression, out dt))
                {
                    int year = core.Functions.FormInt(name + "[date-year]", dt.Year);
                    int month = core.Functions.FormInt(name + "[date-month]", dt.Month);
                    int day = core.Functions.FormInt(name + "[date-day]", dt.Day);
                    int hour = core.Functions.FormInt(name + "[date-hour]", dt.Hour);
                    int minute = core.Functions.FormInt(name + "[date-minute]", dt.Minute);
                    int second = core.Functions.FormInt(name + "[date-second]", 0);

                    dt = new DateTime(year, month, day, hour, minute, second);
                }
            }
            else
            {
                int year = core.Functions.FormInt(name + "[date-year]", dt.Year);
                int month = core.Functions.FormInt(name + "[date-month]", dt.Month);
                int day = core.Functions.FormInt(name + "[date-day]", dt.Day);
                int hour = core.Functions.FormInt(name + "[date-hour]", dt.Hour);
                int minute = core.Functions.FormInt(name + "[date-minute]", dt.Minute);
                int second = core.Functions.FormInt(name + "[date-second]", 0);

                dt = new DateTime(year, month, day, hour, minute, second);
            }

            datetime = tz.GetUnixTimeStamp(dt);

            return datetime;
        }
    }
}
