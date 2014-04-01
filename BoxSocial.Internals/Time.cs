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
using System.Configuration;
using System.Data;
using System.Globalization;
using System.Web;
using BoxSocial.Forms;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    /// <summary>
    /// 01 -12:00 International Date Line West
    /// 02 -11:00 Midway Island, Samoa
    /// 03 -10:00 Hawaii
    /// 04 -09:00 Alaska
    /// 05 -08:00 Pacific Time (US & Canada)
    /// 06 -08:00 Tijuana, Baja California
    /// 07 -07:00 Arizona
    /// 08 -07:00 Chihuahua, La Paz, Mazatlan
    /// 09 -07:00 Mountain Time (US & Canada)
    /// 10 -06:00 Central America
    /// 11 -06:00 Central Time (US & Canada)
    /// 12 -06:00 Guadalajara, Mexico City, Monterrey
    /// 13 -06:00 Saskatchewan
    /// 14 -05:00 Bogota, Lima, Quito, Rio Branco
    /// 15 -05:00 Eastern Time (US & Canada)
    /// 16 -05:00 Indiana (East)
    /// 17 -04:00 Atlantic Time (Canada)
    /// 18 -04:00 Caracas, La Paz
    /// 19 -04:00 Manaus
    /// 20 -04:00 Santiago
    /// 21 -03:30 Newfoundland
    /// 22 -03:00 Brasilia
    /// 23 -03:00 Bueno Aires, Georgetown
    /// 24 -03:00 Greenland
    /// 25 -03:00 Montevideo
    /// 26 -02:00 Mid-Atlantic
    /// 27 -01:00 Azores
    /// 28 -01:00 Cape Verde Is.
    /// 29 00:00 Casablanca, Monrovia, Reykjavik
    /// 30 00:00 Greenwich Mean Time : Dublin, Edinburgh, Lisbon, London
    /// 31 +01:00 Amsterdam, Berlin, Bern, Rome, Stockholm, Vienna
    /// 32 +01:00 Belgrade, Bratislava, Budapest, Ljublijana, Prague
    /// 33 +01:00 Brussels, Copenhagen, Madrid, Paris
    /// 34 +01:00 Sarajevo, Skopje, Warsaw, Zagreb
    /// 35 +01:00 West Central Africa
    /// 36 +02:00 Amman
    /// 37 +02:00 Athens, Bucharest, Istanbul
    /// 38 +02:00 Beirut
    /// 39 +02:00 Cairo
    /// 40 +02:00 Harare, Pretoria
    /// 41 +02:00 Helsinki, Kyiv, Riga, Sofia, Tallinn, Vilnius
    /// 42 +02:00 Jerusalem
    /// 43 +02:00 Minsk
    /// 44 +02:00 Windhoek
    /// 45 +03:00 Baghdad
    /// 46 +03:00 Kuwait, Riyadh
    /// 47 +03:00 Moscow, St. Petersburg, Volgograd
    /// 48 +03:00 Nairobi
    /// 49 +03:00 Tbilisi
    /// 50 +03:30 Tehran
    /// 51 +04:00 Abu Dhabi, Muscat
    /// 52 +04:00 Baku
    /// 53 +04:00 Verevan
    /// 54 +04:30 Kabul
    /// 55 +05:00 Ekaterinburg
    /// 56 +05:00 Islamabed, Karachi, Tashkent
    /// 57 +05:30 Chennai, Kolata, Mumbai, New Delhi
    /// 58 +05:30 Sri Jayawardenepura
    /// 59 +05:45 Kathmandu
    /// 60 +06:00 Almaty, Novosibrisk
    /// 61 +06:00 Astana, Dhaka
    /// 62 +06:30 Yangon (Rangoon)
    /// 63 +07:00 Bangkok, Hanoi, Jakarta
    /// 64 +07:00 Krasnoyarsk
    /// 65 +08:00 Bejing, Chonqing, Hong Kong, Urumqi
    /// 66 +08:00 Irkutsk, Ulaan Bataar
    /// 67 +08:00 Kuala Lumpur, Singapore
    /// 68 +08:00 Perth
    /// 69 +08:00 Taipei
    /// 70 +09:00 Osaka, Sapporo, Tokyo
    /// 71 +09:00 Seoul
    /// 72 +09:00 Yakutsk
    /// 73 +09:30 Adelaide
    /// 74 +09:30 Darwin
    /// 75 +10:00 Brisbane
    /// 76 +10:00 Canberra, Melbourne, Sydney
    /// 77 +10:00 Guam, Port Moresby
    /// 78 +10:00 Hobart
    /// 79 +10:00 Vladivostok
    /// 80 +11:00 Magadan, Solomon Is., New Caledonia
    /// 81 +12:00 Auckland, Wellington
    /// 82 +12:00 Fiji, Kamchatka, Marshall Is.
    /// 83 +13:00 Nuku'alofa
    /// </summary>
    [DataTable("timezones")]
    public class UnixTime : NumberedItem
    {
        /// <summary>
        /// The Time Zone Code for UTC.
        /// </summary>
        public static readonly ushort UTC_CODE = 30;

        [DataField("timezone_id", DataFieldKeys.Primary)]
        private long timezoneId;
        [DataField("timezone_utc")]
        private ushort timeZoneCode;
        [DataField("timezone_title", 31)]
        private string title;
        [DataField("timezone_autumn_day")]
        private byte autumnDay;
        [DataField("timezone_autumn_month")]
        private byte autumnMonth;
        [DataField("timezone_spring_day")]
        private byte springDay;
        [DataField("timezone_spring_month")]
        private byte springMonth;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="timeZoneCode"></param>
        public UnixTime(Core core, ushort timeZoneCode)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(UnixTime_ItemLoad);

            this.timeZoneCode = timeZoneCode;
        }

        void UnixTime_ItemLoad()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        public ushort TimeZoneCode
        {
            get
            {
                return timeZoneCode;
            }
            set
            {
                timeZoneCode = value;
            }
        }

        /// <summary>
        /// Returns the offset in seconds
        /// </summary>
        /// <param name="timeZoneCode"></param>
        /// <returns></returns>
        public static int GetUtcOffset(ushort timeZoneCode)
        {
            switch (timeZoneCode)
            {
                case 0: // UTC (unlisted)
                    return 0;
                case 1: // International Date Line West
                    return -12 * 60 * 60;
                case 2: // Midway Island, Samoa
                    return -11 * 60 * 60;
                case 3: // Hawaii
                    return -10 * 60 * 60;
                case 4: // Alaska
                    return -9 * 60 * 60;
                case 5: // Pacific Time (US & Canada)
                case 6: // Tijuana, Baja California
                    return -8 * 60 * 60;
                case 7: // Arizona
                case 8: // Chihuahua, La Paz, Mazatlan
                case 9: // Mountain Time (US & Canada)
                    return -7 * 60 * 60;
                case 10: // Central America
                case 11: // Central Time (US & Canada)
                case 12: // Guadalajara, Mexico City, Monterrey
                case 13: // Saskatchewan
                    return -6 * 60 * 60;
                case 14: // Bogota, Lima, Quito, Rio Branco
                case 15: // Eastern Time (US & Canada)
                case 16: // Indiana (East)
                    return -5 * 60 * 60;
                case 17: // Atlantic Time (Canada)
                case 18: // Caracas, La Paz
                case 19: // Manaus
                case 20: // Santiago
                    return -4 * 60 * 60;
                case 21: // Newfoundland
                    return -3 * 60 * 60 - 30 * 60;
                case 22: // Brasilia
                case 23: // Bueno Aires, Georgetown
                case 24: // Greenland
                case 25: // Montevideo
                    return -3 * 60 * 60;
                case 26: // Mid-Atlantic
                    return -2 * 60 * 60;
                case 27: // Azores
                case 28: // Cape Verde Is.
                    return -1 * 60 * 60;
                case 29: // Casablanca, Monrovia, Reykjavik
                case 30: // Greenwich Mean Time : Dublin, Edinburgh, Lisbon, London
                    return 0;
                case 31: // Amsterdam, Berlin, Bern, Rome, Stockholm, Vienna
                case 32: // Belgrade, Bratislava, Budapest, Ljublijana, Prague
                case 33: // Brussels, Copenhagen, Madrid, Paris
                case 34: // Sarajevo, Skopje, Warsaw, Zagreb
                case 35: // West Central Africa
                    return 1 * 60 * 60;
                case 36: // Amman
                case 37: // Athens, Bucharest, Istanbul
                case 38: // Beirut
                case 39: // Cairo
                case 40: // Harare, Pretoria
                case 41: // Helsinki, Kyiv, Riga, Sofia, Tallinn, Vilnius
                case 42: // Jerusalem
                case 43: // Minsk
                case 44: // Windhoek
                    return 2 * 60 * 60;
                case 45: // Baghdad
                case 46: // Kuwait, Riyadh
                case 47: // Moscow, St. Petersburg, Volgograd
                case 48: // Nairobi
                case 49: // Tbilisi
                    return 3 * 60 * 60;
                case 50: // Tehran
                    return 3 * 60 * 60 + 30 * 60;
                case 51: // Abu Dhabi, Muscat
                case 52: // Baku
                case 53: // Verevan
                    return 4 * 60 * 60;
                case 54: // Kabul
                    return 4 * 60 * 60 + 30 * 60;
                case 55: // Ekaterinburg
                case 56: // Islamabed, Karachi, Tashkent
                    return 5 * 60 * 60;
                case 57: // Chennai, Kolata, Mumbai, New Delhi
                case 58: // Sri Jayawardenepura
                    return 5 * 60 * 60 + 30 * 60;
                case 59: // Kathmandu
                    return 5 * 60 * 60 + 45 * 60;
                case 60: // Almaty, Novosibrisk
                case 61: // Astana, Dhaka
                    return 6 * 60 * 60;
                case 62: // Yangon (Rangoon)
                    return 6 * 60 * 60 + 30 * 60;
                case 63: // Bangkok, Hanoi, Jakarta
                case 64: // Krasnoyarsk
                    return 7 * 60 * 60;
                case 65: // Bejing, Chonqing, Hong Kong, Urumqi
                case 66: // Irkutsk, Ulaan Bataar
                case 67: // Kuala Lumpur, Singapore
                case 68: // Perth
                case 69: // Taipei
                    return 08 * 60 * 60;
                case 70: // Osaka, Sapporo, Tokyo
                case 71: // Seoul
                case 72: // Yakutsk
                    return 09 * 60 * 60;
                case 73: // Adelaide
                case 74: // Darwin
                    return 09 * 60 * 60 + 30 * 60;
                case 75: // Brisbane
                case 76: // Canberra, Melbourne, Sydney
                case 77: // Guam, Port Moresby
                case 78: // Hobart
                case 79: // Vladivostok
                    return 10 * 60 * 60;
                case 80: // Magadan, Solomon Is., New Caledonia
                    return 11 * 60 * 60;
                case 81: // Auckland, Wellington
                case 82: // Fiji, Kamchatka, Marshall Is.
                    return 12 * 60 * 60;
                case 83: // Nuku'alofa
                    return 13 * 60 * 60;
            }
            return 0;
        }

        public static string GetOffsetString(ushort timeZoneCode)
        {
            int offset = UnixTime.GetUtcOffset(timeZoneCode);
            int hour = offset / 60 / 60;
            int minute = (offset - hour * 60 * 60) / 60;

            if (offset == 0)
            {
                return " 00:00 ";
            }
            else if (offset > 0)
            {
                return string.Format("+{0:00}:{1:00} ",
                    hour, minute);
            }
            else
            {
                return string.Format("-{0:00}:{1:00} ",
                    -hour, minute);
            }
        }

        public static SelectBox BuildTimeZoneSelectBox(string name)
        {
            SelectBox dateTimeSelectBox = new SelectBox(name);

            dateTimeSelectBox.Add(new SelectBoxItem("1", UnixTime.GetOffsetString(1) + "International Date Line West"));
            dateTimeSelectBox.Add(new SelectBoxItem("2", UnixTime.GetOffsetString(2) + "Midway Island, Samoa"));
            dateTimeSelectBox.Add(new SelectBoxItem("3", UnixTime.GetOffsetString(3) + "Hawaii"));
            dateTimeSelectBox.Add(new SelectBoxItem("4", UnixTime.GetOffsetString(4) + "Alaska"));
            dateTimeSelectBox.Add(new SelectBoxItem("5", UnixTime.GetOffsetString(5) + "Pacific Time (US & Canada)"));
            dateTimeSelectBox.Add(new SelectBoxItem("6", UnixTime.GetOffsetString(6) + "Tijuana, Baja California"));
            dateTimeSelectBox.Add(new SelectBoxItem("7", UnixTime.GetOffsetString(7) + "Arizona"));
            dateTimeSelectBox.Add(new SelectBoxItem("8", UnixTime.GetOffsetString(8) + "Chihuahua, La Paz, Mazatlan"));
            dateTimeSelectBox.Add(new SelectBoxItem("9", UnixTime.GetOffsetString(9) + "Mountain Time (US & Canada)"));
            dateTimeSelectBox.Add(new SelectBoxItem("10", UnixTime.GetOffsetString(10) + "Central America"));
            dateTimeSelectBox.Add(new SelectBoxItem("11", UnixTime.GetOffsetString(11) + "Central Time (US & Canada)"));
            dateTimeSelectBox.Add(new SelectBoxItem("12", UnixTime.GetOffsetString(12) + "Guadalajara, Mexico City, Monterrey"));
            dateTimeSelectBox.Add(new SelectBoxItem("13", UnixTime.GetOffsetString(13) + "Saskatchewan"));
            dateTimeSelectBox.Add(new SelectBoxItem("14", UnixTime.GetOffsetString(14) + "Bogota, Lima, Quito, Rio Branco"));
            dateTimeSelectBox.Add(new SelectBoxItem("15", UnixTime.GetOffsetString(15) + "Eastern Time (US & Canada)"));
            dateTimeSelectBox.Add(new SelectBoxItem("16", UnixTime.GetOffsetString(16) + "Indiana (East)"));
            dateTimeSelectBox.Add(new SelectBoxItem("17", UnixTime.GetOffsetString(17) + "Atlantic Time (Canada)"));
            dateTimeSelectBox.Add(new SelectBoxItem("18", UnixTime.GetOffsetString(18) + "Caracas, La Paz"));
            dateTimeSelectBox.Add(new SelectBoxItem("19", UnixTime.GetOffsetString(19) + "Manaus"));
            dateTimeSelectBox.Add(new SelectBoxItem("20", UnixTime.GetOffsetString(20) + "Santiago"));
            dateTimeSelectBox.Add(new SelectBoxItem("21", UnixTime.GetOffsetString(21) + "Newfoundland"));
            dateTimeSelectBox.Add(new SelectBoxItem("22", UnixTime.GetOffsetString(22) + "Brasilia"));
            dateTimeSelectBox.Add(new SelectBoxItem("23", UnixTime.GetOffsetString(23) + "Bueno Aires, Georgetown"));
            dateTimeSelectBox.Add(new SelectBoxItem("24", UnixTime.GetOffsetString(24) + "Greenland"));
            dateTimeSelectBox.Add(new SelectBoxItem("25", UnixTime.GetOffsetString(25) + "Montevideo"));
            dateTimeSelectBox.Add(new SelectBoxItem("26", UnixTime.GetOffsetString(26) + "Mid-Atlantic"));
            dateTimeSelectBox.Add(new SelectBoxItem("27", UnixTime.GetOffsetString(27) + "Azores"));
            dateTimeSelectBox.Add(new SelectBoxItem("28", UnixTime.GetOffsetString(28) + "Cape Verde Is."));
            dateTimeSelectBox.Add(new SelectBoxItem("29", UnixTime.GetOffsetString(29) + "Casablanca, Monrovia, Reykjavik"));
            dateTimeSelectBox.Add(new SelectBoxItem("30", UnixTime.GetOffsetString(30) + "Greenwich Mean Time : Dublin, Edinburgh, Lisbon, London"));
            dateTimeSelectBox.Add(new SelectBoxItem("31", UnixTime.GetOffsetString(31) + "Amsterdam, Berlin, Bern, Rome, Stockholm, Vienna"));
            dateTimeSelectBox.Add(new SelectBoxItem("32", UnixTime.GetOffsetString(32) + "Belgrade, Bratislava, Budapest, Ljublijana, Prague"));
            dateTimeSelectBox.Add(new SelectBoxItem("33", UnixTime.GetOffsetString(33) + "Brussels, Copenhagen, Madrid, Paris"));
            dateTimeSelectBox.Add(new SelectBoxItem("34", UnixTime.GetOffsetString(34) + "Sarajevo, Skopje, Warsaw, Zagreb"));
            dateTimeSelectBox.Add(new SelectBoxItem("35", UnixTime.GetOffsetString(35) + "West Central Africa"));
            dateTimeSelectBox.Add(new SelectBoxItem("36", UnixTime.GetOffsetString(36) + "Amman"));
            dateTimeSelectBox.Add(new SelectBoxItem("37", UnixTime.GetOffsetString(37) + "Athens, Bucharest, Istanbul"));
            dateTimeSelectBox.Add(new SelectBoxItem("38", UnixTime.GetOffsetString(38) + "Beirut"));
            dateTimeSelectBox.Add(new SelectBoxItem("39", UnixTime.GetOffsetString(39) + "Cairo"));
            dateTimeSelectBox.Add(new SelectBoxItem("40", UnixTime.GetOffsetString(40) + "Harare, Pretoria"));
            dateTimeSelectBox.Add(new SelectBoxItem("41", UnixTime.GetOffsetString(41) + "Helsinki, Kyiv, Riga, Sofia, Tallinn, Vilnius"));
            dateTimeSelectBox.Add(new SelectBoxItem("42", UnixTime.GetOffsetString(42) + "Jerusalem"));
            dateTimeSelectBox.Add(new SelectBoxItem("43", UnixTime.GetOffsetString(43) + "Minsk"));
            dateTimeSelectBox.Add(new SelectBoxItem("44", UnixTime.GetOffsetString(44) + "Windhoek"));
            dateTimeSelectBox.Add(new SelectBoxItem("45", UnixTime.GetOffsetString(45) + "Baghdad"));
            dateTimeSelectBox.Add(new SelectBoxItem("46", UnixTime.GetOffsetString(46) + "Kuwait, Riyadh"));
            dateTimeSelectBox.Add(new SelectBoxItem("47", UnixTime.GetOffsetString(47) + "Moscow, St. Petersburg, Volgograd"));
            dateTimeSelectBox.Add(new SelectBoxItem("48", UnixTime.GetOffsetString(48) + "Nairobi"));
            dateTimeSelectBox.Add(new SelectBoxItem("49", UnixTime.GetOffsetString(49) + "Tbilisi"));
            dateTimeSelectBox.Add(new SelectBoxItem("50", UnixTime.GetOffsetString(50) + "Tehran"));
            dateTimeSelectBox.Add(new SelectBoxItem("51", UnixTime.GetOffsetString(51) + "Abu Dhabi, Muscat"));
            dateTimeSelectBox.Add(new SelectBoxItem("52", UnixTime.GetOffsetString(52) + "Baku"));
            dateTimeSelectBox.Add(new SelectBoxItem("53", UnixTime.GetOffsetString(53) + "Verevan"));
            dateTimeSelectBox.Add(new SelectBoxItem("54", UnixTime.GetOffsetString(54) + "Kabul"));
            dateTimeSelectBox.Add(new SelectBoxItem("55", UnixTime.GetOffsetString(55) + "Ekaterinburg"));
            dateTimeSelectBox.Add(new SelectBoxItem("56", UnixTime.GetOffsetString(56) + "Islamabed, Karachi, Tashkent"));
            dateTimeSelectBox.Add(new SelectBoxItem("57", UnixTime.GetOffsetString(57) + "Chennai, Kolata, Mumbai, New Delhi"));
            dateTimeSelectBox.Add(new SelectBoxItem("58", UnixTime.GetOffsetString(58) + "Sri Jayawardenepura"));
            dateTimeSelectBox.Add(new SelectBoxItem("59", UnixTime.GetOffsetString(59) + "Kathmandu"));
            dateTimeSelectBox.Add(new SelectBoxItem("60", UnixTime.GetOffsetString(60) + "Almaty, Novosibrisk"));
            dateTimeSelectBox.Add(new SelectBoxItem("61", UnixTime.GetOffsetString(61) + "Astana, Dhaka"));
            dateTimeSelectBox.Add(new SelectBoxItem("62", UnixTime.GetOffsetString(62) + "Yangon (Rangoon)"));
            dateTimeSelectBox.Add(new SelectBoxItem("63", UnixTime.GetOffsetString(63) + "Bangkok, Hanoi, Jakarta"));
            dateTimeSelectBox.Add(new SelectBoxItem("64", UnixTime.GetOffsetString(64) + "Krasnoyarsk"));
            dateTimeSelectBox.Add(new SelectBoxItem("65", UnixTime.GetOffsetString(65) + "Bejing, Chonqing, Hong Kong, Urumqi"));
            dateTimeSelectBox.Add(new SelectBoxItem("66", UnixTime.GetOffsetString(66) + "Irkutsk, Ulaan Bataar"));
            dateTimeSelectBox.Add(new SelectBoxItem("67", UnixTime.GetOffsetString(67) + "Kuala Lumpur, Singapore"));
            dateTimeSelectBox.Add(new SelectBoxItem("68", UnixTime.GetOffsetString(68) + "Perth"));
            dateTimeSelectBox.Add(new SelectBoxItem("69", UnixTime.GetOffsetString(69) + "Taipei"));
            dateTimeSelectBox.Add(new SelectBoxItem("70", UnixTime.GetOffsetString(70) + "Osaka, Sapporo, Tokyo"));
            dateTimeSelectBox.Add(new SelectBoxItem("71", UnixTime.GetOffsetString(71) + "Seoul"));
            dateTimeSelectBox.Add(new SelectBoxItem("72", UnixTime.GetOffsetString(72) + "Yakutsk"));
            dateTimeSelectBox.Add(new SelectBoxItem("73", UnixTime.GetOffsetString(73) + "Adelaide"));
            dateTimeSelectBox.Add(new SelectBoxItem("74", UnixTime.GetOffsetString(74) + "Darwin"));
            dateTimeSelectBox.Add(new SelectBoxItem("75", UnixTime.GetOffsetString(75) + "Brisbane"));
            dateTimeSelectBox.Add(new SelectBoxItem("76", UnixTime.GetOffsetString(76) + "Canberra, Melbourne, Sydney"));
            dateTimeSelectBox.Add(new SelectBoxItem("77", UnixTime.GetOffsetString(77) + "Guam, Port Moresby"));
            dateTimeSelectBox.Add(new SelectBoxItem("78", UnixTime.GetOffsetString(78) + "Hobart"));
            dateTimeSelectBox.Add(new SelectBoxItem("79", UnixTime.GetOffsetString(79) + "Vladivostok"));
            dateTimeSelectBox.Add(new SelectBoxItem("80", UnixTime.GetOffsetString(80) + "Magadan, Solomon Is., New Caledonia"));
            dateTimeSelectBox.Add(new SelectBoxItem("81", UnixTime.GetOffsetString(81) + "Auckland, Wellington"));
            dateTimeSelectBox.Add(new SelectBoxItem("82", UnixTime.GetOffsetString(82) + "Fiji, Kamchatka, Marshall Is."));
            dateTimeSelectBox.Add(new SelectBoxItem("83", UnixTime.GetOffsetString(83) + "Nuku'alofa"));

            return dateTimeSelectBox;
        }

        /*public static string BuildTimeZoneSelectBox(string selectedItem)
        {
            

            return Functions.BuildSelectBox("timezone", timeZones, selectedItem);
        }*/

        /*public bool IsDst(ushort timeZone)
        {
            // timezones are not fun
            switch (timeZone)
            {
                case 76:
                    // +10:00 AEST
                    if (DateTime.Now.Month == 10)
                    {
                    }
                    break;
            }
        }

        private int GetLastSunday()
        {
            //DateTime firstDayOfMonth = new DateTime(DateTime.Now, 10, 1);
        }

        private int GetOffsetToSunday(DayOfWeek dow)
        {
            switch (dow)
            {
                case DayOfWeek.Sunday:
                    return 0;
                case DayOfWeek.Monday:
                    return 6;
                case DayOfWeek.Tuesday:
                    return 5;
                case DayOfWeek.Wednesday:
                    return 4;
                case DayOfWeek.Thursday:
                    return 3;
                case DayOfWeek.Friday:
                    return 2;
                case DayOfWeek.Saturday:
                    return 1;
            }
        }*/

        public DateTime Now
        {
            get
            {
                return DateTime.UtcNow.Add(new TimeSpan(0, 0, UnixTime.GetUtcOffset(timeZoneCode)));
            }
        }

        public string ToStringPast(DateTime time)
        {
            DateTime now = new DateTime(Now.Year, Now.Month, Now.Day);
            DateTime then = new DateTime(time.Year, time.Month, time.Day);
            TimeSpan ts = then.Subtract(now);
            if (Math.Sign(ts.Days) >= 0)
            {
                // Present
                if (ts.TotalDays < 1)
                {
                    return core.Prose.GetString("TODAY");
                }
                // Future
                else if (ts.TotalDays < 2)
                {
                    return core.Prose.GetString("TOMORROW");
                }
                else if (ts.TotalDays < 7)
                {
                    switch (time.DayOfWeek)
                    {
                        case DayOfWeek.Monday:
                            return core.Prose.GetString("MONDAY");
                        case DayOfWeek.Tuesday:
                            return core.Prose.GetString("TUESDAY");
                        case DayOfWeek.Wednesday:
                            return core.Prose.GetString("WEDNESDAY");
                        case DayOfWeek.Thursday:
                            return core.Prose.GetString("THURSDAY");
                        case DayOfWeek.Friday:
                            return core.Prose.GetString("FRIDAY");
                        case DayOfWeek.Saturday:
                            return core.Prose.GetString("SATURDAY");
                        case DayOfWeek.Sunday:
                            return core.Prose.GetString("SUNDAY");
                        default:
                            return time.DayOfWeek.ToString();
                    }
                }
                else if (ts.TotalDays < 14)
                {
                    return core.Prose.GetString("NEXT_WEEK");
                }
                else
                {
                    return core.Prose.GetString("NEWER");
                }
            }
            else
            {
                // Past
                if (ts.TotalDays > -1)
                {
                    return core.Prose.GetString("YESTERDAY");
                }
                else if (ts.TotalDays > -14)
                {
                    return core.Prose.GetString("LAST_WEEK");
                }
                else if (now.Month - 1 == then.Month && now.Year == then.Year && now.Month != 1)
                {
                    return core.Prose.GetString("LAST_MONTH");
                }
                else if (now.Month == 1 && then.Month == 12 && now.Year == then.Year - 1)
                {
                    return core.Prose.GetString("LAST_MONTH");
                }
                else if (ts.TotalDays > -21)
                {
                    return core.Prose.GetString("TWO_WEEKS_AGO");
                }
                else if (now.Month == then.Month && now.Year == then.Year)
                {
                    return core.Prose.GetString("EARLIER_IN_THE_MONTH");
                }
                else
                {
                    return core.Prose.GetString("OLDER");
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public DateTime DateTimeFromMysql(object p)
        {
            long timeStamp = (long)p;
            long localTimeStamp = timeStamp + (long)UnixTime.GetUtcOffset(timeZoneCode);
            int days = (int)(localTimeStamp / 60L / 60L / 24L);
            int hours = (int)((localTimeStamp - days * 60L * 60L * 24L) / 60 / 60);
            int minutes = (int)((localTimeStamp - days * 60L * 60L * 24L - hours * 60L * 60L) / 60);
            int seconds = (int)(localTimeStamp - days * 60L * 60L * 24L - hours * 60L * 60L - minutes * 60L);

            DateTime returnTime = new DateTime(1970, 1, 1, 0, 0, 0);
            returnTime = returnTime.Add(new TimeSpan(days, hours, minutes, seconds));

            return returnTime;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public long GetUnixTimeStamp(DateTime input)
        {
            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0);
            TimeSpan ts = input.Subtract(epoch);

            return (long)ts.Seconds + ts.Minutes * 60L + ts.Hours * 60L * 60L + ts.Days * 60L * 60L * 24L - UnixTime.GetUtcOffset(timeZoneCode);
        }

        /// <summary>
        /// Input is UTC
        /// </summary>
        /// <param name="input">UTC</param>
        /// <returns></returns>
        public static long UnixTimeStamp(DateTime input)
        {
            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0);
            TimeSpan ts = input.Subtract(epoch);

            return (long)ts.Seconds + ts.Minutes * 60L + ts.Hours * 60L * 60L + ts.Days * 60L * 60L * 24L;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static long UnixTimeStamp()
        {
            return UnixTime.UnixTimeStamp(DateTime.UtcNow);
        }

        public string MysqlToString(object p)
        {
            return DateTimeToString(DateTimeFromMysql(p));
        }

        public string MysqlToString(object p, bool today)
        {
            return DateTimeToString(DateTimeFromMysql(p), today);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public string DateTimeToString(DateTime dt)
        {
            return DateTimeToString(dt, false);
        }

        public string DateTimeToDateString(DateTime dt)
        {
            return DateTimeToDateString(dt, false);
        }

        public string MonthToString(int month)
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
            }
            return string.Empty;
        }

        public string DateTimeToDateString(DateTime dt, bool today)
        {
            if (today)
            {
                TimeSpan ts = DateTime.UtcNow.Subtract(dt.Subtract(new TimeSpan(0, 0, UnixTime.GetUtcOffset(timeZoneCode))));

                if (ts.TotalHours <= 24)
                {
                    return "Today";
                }
                else
                {
                    return MonthToString(dt.Month) + dt.ToString(" dd, yyyy");
                }
            }
            else
            {
                return MonthToString(dt.Month) + dt.ToString(" dd, yyyy");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="today"></param>
        /// <returns></returns>
        public string DateTimeToString(DateTime dt, bool today)
        {
            TimeSpan ts = DateTime.UtcNow.Subtract(dt.Subtract(new TimeSpan(0, 0, UnixTime.GetUtcOffset(timeZoneCode))));

            if (today)
            {
                if (ts.TotalMinutes <= 5)
                {
                    return core.Prose.GetString("NOW");
                }
                else if (ts.TotalHours <= 24)
                {
                    return core.Prose.GetString("TODAY");
                }
                else if (dt.Year == 1000)
                {
                    return core.Prose.GetString("NEVER");
                }
                else
                {
                    return MonthToString(dt.Month) + dt.ToString(" dd, yyyy");
                }
            }
            else
            {
                if (ts.TotalSeconds < 1)
                {
                    return core.Prose.GetString("NOW");
                }
                if (ts.TotalSeconds < 60 && (int)ts.TotalSeconds != 1)
                {
                    return string.Format("{0} seconds ago", (int)ts.TotalSeconds);
                }
                else if ((int)ts.TotalSeconds == 1)
                {
                    return "1 second ago";
                }
                else if (ts.TotalMinutes < 60 && (int)ts.TotalMinutes != 1)
                {
                    return string.Format("{0} minutes ago", (int)ts.TotalMinutes);
                }
                else if ((int)ts.TotalMinutes == 1)
                {
                    return "1 minute ago";
                }
                else if (ts.TotalDays > 1 && ts.TotalDays <= 2)
                {
                    return core.Prose.GetString("YESTERDAY");
                }
                else if (ts.TotalHours < 12 && (ts.TotalHours >= 2 || ts.TotalHours < 1))
                {
                    return string.Format("{0} hours ago", (int)ts.TotalHours);
                }
                else if (ts.TotalHours < 2 && ts.TotalHours >= 1)
                {
                    return string.Format("{0} hour ago", (int)ts.TotalHours);
                }
                else if (ts.TotalHours <= 24)
                {
                    return core.Prose.GetString("TODAY");
                }
                else if (dt.Year == 1000)
                {
                    return core.Prose.GetString("NEVER");
                }
                else
                {
                    return MonthToString(dt.Month) + dt.ToString(" dd, yyyy");
                }
            }
        }

        public override long Id
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override string Uri
        {
            get
            {
                throw new NotImplementedException();
            }
        }
    }

    public class Time
    {
        // http://en.wikipedia.org/wiki/List_of_time_zones
        /*public string BuildTimeZoneBox()
        {
        }*/
    }
}
