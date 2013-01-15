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

/*
 * This file is based on skmRSS by Scott Mitchell
 * http://www.4GuysFromRolla.com/ScottMitchell
 */

using System;
using System.Xml;
using System.Xml.Serialization;
using System.Text.RegularExpressions;
using System.Collections;

namespace BoxSocial.Internals
{
	/// <summary>
	/// Summary description for RssDocument.
	/// </summary>
	[XmlRootAttribute("rss")]
	public class RssDocument
	{
		[XmlAttribute]
		public string version;

		[XmlElement("channel")]
		public RssChannel[] channels;

		public RssDocument()
		{
			channels = new RssChannel[1];
			channels[0] = new RssChannel();
		}
	}

	public class RssChannel
	{
		[XmlElement]
		public string title;
		public string link;
		[XmlElementAttribute(IsNullable = false)]
		public string description;
		private DateTime pubDate;
		public string generator;
		public string category;
		public string copyright;

		[XmlElement("pubDate")]
		public string pubdate
		{
			get 
			{
				string temp = pubDate.ToString(@"ddd, dd MMM yyyy hh:mm:ss zzz");
				if (temp.Length > 5)
				temp = temp.Substring(0, temp.Length - 3) + temp.Substring(temp.Length - 2, 2);
				return temp;
			}
			set
			{
				pubDate = DateTimeExt.Parse(value);
			}
		}

		//[XmlArrayAttribute("item")]
		[XmlElement("item")]
		public RssDocumentItem[] items;

		public RssChannel()
		{
			generator = "Buddy";
			description = "";
		}
	}

	public class RssChannelImage
	{
	}

	public class RssChannelTtl
	{
	}

	public class RssDocumentItem
	{
		public string title;
		public string link;
		public string comments;
		private DateTime pubDate;
		public string category;
		public string guid;
		public string content;
		//[XmlAnyElement(NodeType = XmlNodeType.CDATA)]
		public string description;
		[XmlElement("dc:creator")]
		public string dcCreator;
		public string author;

		[XmlElement("pubDate")]
		public string pubdate
		{
			get 
			{
				string temp = pubDate.ToString(@"ddd, dd MMM yyyy hh:mm:ss zzz");
				if (temp.Length > 5)
				temp = temp.Substring(0, temp.Length - 3) + temp.Substring(temp.Length - 2, 2);
				return temp;
			}
			set
			{
				pubDate = DateTimeExt.Parse(value);
			}
		}

		public RssDocumentItem()
		{
		}
	}

	/// <summary>
	/// DateTimeExt is able to parse RFC2822/RFC822 formatted dates.
	/// </summary>
	public class DateTimeExt
	{

		private static Regex rfc2822 = new Regex(@"\s*(?:(?:Mon|Tue|Wed|Thu|Fri|Sat|Sun)\s*,\s*)?(\d{1,2})\s+(Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)\s+(\d{2,})\s+(\d{2})\s*:\s*(\d{2})\s*(?::\s*(\d{2}))?\s+([+\-]\d{4}|UT|GMT|EST|EDT|CST|CDT|MST|MDT|PST|PDT|[A-IK-Z])", RegexOptions.Compiled);

		private static ArrayList months = new ArrayList(new string[]{"ZeroIndex","Jan","Feb","Mar","Apr","May","Jun","Jul","Aug","Sep","Oct","Nov","Dec" });

		/// Parse is able to parse RFC2822/RFC822 formatted dates.
		/// It has a fallback mechanism: if the string does not match,
		/// the normal DateTime.Parse() is called. If that throws a FormatException
		/// the current date (DateTime.Now) will be returned.
		public static DateTime Parse(string dateTimeString)
		{
			if (dateTimeString == null)
				return DateTime.Now;

			if (dateTimeString.Trim().Length == 0)
				return DateTime.Now;

			Match m = rfc2822.Match(dateTimeString);
			if (m.Success) 
			{
				try
				{
					int dd = Int32.Parse(m.Groups[1].Value); 
					int mth = months.IndexOf(m.Groups[2].Value);
					int yy = Int32.Parse(m.Groups[3].Value); 
					// following year completion is compliant with RFC 2822.
					yy = (yy < 50 ? 2000 + yy: (yy < 1000 ? 1900 + yy: yy));
					int hh = Int32.Parse(m.Groups[4].Value);
					int mm = Int32.Parse(m.Groups[5].Value);
					int ss = Int32.Parse(m.Groups[6].Value);
					string zone =  m.Groups[7].Value;

					DateTime xd = new DateTime(yy, mth, dd, hh, mm, ss);
					return xd.AddHours(-RFCTimeZoneToGMTBias(zone));
				}
				catch
				{
					for (int i = 0; i < m.Groups.Count; i++) 
					{ 
						Console.WriteLine("Found:'" + m.Groups[i] + "' at " 
							+ m.Groups[i].Index.ToString());
					}
				}
			}
			else
			{
				try 
				{
					return DateTime.Parse(dateTimeString);
				}
				catch
				{
					
				}
			}
			return DateTime.Now;
		}


		private DateTimeExt(){}


		private struct TZB
		{					
			public TZB(string z, int b) { Zone = z; Bias = b; }
			public string Zone;
			public int Bias;
		}

		private const int timeZones = 35;
		private static TZB[] ZoneBias = new TZB[timeZones]
				{
					new TZB("GMT", 0),     new TZB("UT", 0),
					new TZB("EST", -5*60), new TZB("EDT", -4*60),
					new TZB("CST", -6*60), new TZB("CDT", -5*60),
					new TZB("MST", -7*60), new TZB("MDT", -6*60),
					new TZB("PST", -8*60), new TZB("PDT", -7*60),
					new TZB("Z", 0),       new TZB("A", -1*60),
					new TZB("B", -2*60),   new TZB("C", -3*60),
					new TZB("D", -4*60),   new TZB("E", -5*60),
					new TZB("F", -6*60),   new TZB("G", -7*60),
					new TZB("H", -8*60),   new TZB("I", -9*60),
					new TZB("K", -10*60),  new TZB("L", -11*60),
					new TZB("M", -12*60),  new TZB("N", 1*60),
					new TZB("O", 2*60),    new TZB("P", 3*60),
					new TZB("Q", 4*60),    new TZB("R", 3*60),
					new TZB("S", 6*60),    new TZB("T", 3*60),
					new TZB("U", 8*60),    new TZB("V", 3*60),
					new TZB("W", 10*60),   new TZB("X", 3*60),
					new TZB("Y", 12*60)
				};

		private static double RFCTimeZoneToGMTBias(string zone)
		{

			string s;
			
			if ( zone.IndexOfAny(new char[]{'+', '-'}) == 0 )  // +hhmm format
			{
				int fact = (zone.Substring(0,1) == "-"? -1: 1);
				s = zone.Substring(1).TrimEnd();
				double hh = Math.Min(23, Int32.Parse(s.Substring(0,2)));
				double mm = Math.Min(59, Int32.Parse(s.Substring(2,2)))/60;
				return fact * (hh+mm);
			} 
			else
			{ // named format
				s = zone.ToUpper().Trim();
				for (int i = 0; i < timeZones; i++)
					if (ZoneBias[i].Zone.Equals(s)) 
					{
						return ZoneBias[i].Bias / 60;
					}
			}
			return 0.0;
		}
	}
}
