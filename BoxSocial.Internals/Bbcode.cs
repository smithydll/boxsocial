/*
 * Box Social™
 * http://boxsocial.net/
 * Copyright © 2007, David Lachlan Smith
 * 
 * $Id: AccountBlog.cs,v 1.1 2007/11/18 00:22:42 Bakura\lachlan Exp $
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
using System.Web;
using System.Web.Security;
using System.Text.RegularExpressions;
using System.Text;

namespace BoxSocial.Internals
{
    /// <summary>
    /// Summary description for Bbcode
    /// </summary>

    public enum BbcodeOptions
    {
        DisableImages,
        DisableFlash,
        DisableVideo,
        DisableAudio,
    }

    public class Bbcode
    {
        public Bbcode()
        {
        }

        private class BbcodeTag
        {
            public string Tag;
            public string Attributes;
            public int indexStart;
            public int outputIndexStart;

            public BbcodeTag(string tag, string attr, int index, int outputOffset)
            {
                Tag = tag;
                Attributes = attr;
                indexStart = index;
                outputIndexStart = index + outputOffset;
            }

            public BbcodeAttributes GetAttributes()
            {
                return new BbcodeAttributes(Attributes);
            }
        }

        private class BbcodeTaglet : IComparable
        {
            public int StartIndex;
            public int Length;
            public string RenderText;
            public bool Li;

            public BbcodeTaglet(int startIndex, int length, string renderText)
            {
                StartIndex = startIndex;
                Length = length;
                RenderText = renderText;
                Li = false;
            }

            public BbcodeTaglet(int startIndex, int length, string renderText, bool li)
            {
                StartIndex = startIndex;
                Length = length;
                RenderText = renderText;
                Li = li;
            }

            #region IComparable Members

            int IComparable.CompareTo(object obj)
            {
                if (obj.GetType() != typeof(BbcodeTaglet))
                {
                    return -1;
                }

                BbcodeTaglet bt = (BbcodeTaglet)obj;

                if (StartIndex == bt.StartIndex)
                {
                    if (Li != bt.Li)
                    {
                        if (Li)
                        {
                            return -1;
                        }
                        else
                        {
                            return 1;
                        }
                    }
                    else
                    {
                        return 0;
                    }
                }
                else
                {
                    return StartIndex - bt.StartIndex;
                }
            }

            #endregion
        }

        public static string UlTypeToCssName(string input)
        {
            switch (input)
            {
                default: //case "disc":
                    return "disc";
                case "circle":
                    return "circle";
                case "square":
                    return "square";
            }
        }

        public static string OlTypeToCssName(string input)
        {
            switch (input)
            {
                default: //case "1":
                    return "decimal";
                case "I":
                    return "upper-roman";
                case "i":
                    return "lower-roman";
                case "A":
                    return "upper-alpha";
                case "a":
                    return "lower-alpha";
            }
        }

        public static string Parse(string input)
        {
            return Parse(input, null);
        }

        public static string Parse(string input, Member viewer)
        {
            return Parse(input, viewer, null);
        }

        public static string Parse(string input, Member viewer, Member postOwner)
        {
            if (string.IsNullOrEmpty(input))
            {
                return "";
            }

            StringBuilder debugLog = new StringBuilder();

            List<BbcodeOptions> options = new List<BbcodeOptions>();

            if (viewer != null)
            {
                options = viewer.GetUserBbcodeOptions;
            }

            long start = DateTime.Now.Ticks;
            Stack tags = new Stack();
            bool inTag = false;
            bool startAttr = false;
            bool endTag = false;
            bool inQuote = false;
            int inList = 0;
            bool parseList = false;
            bool inCode = false;
            string Tag = "";
            string attr = "";
            int startIndex = 0;
            int strLength = input.Length;
            int end = strLength;
            /*StringBuilder output = new StringBuilder(input);*/

            StringBuilder newOutput = new StringBuilder();
            int lastEndIndex = 0;

            List<BbcodeTaglet> taglets = new List<BbcodeTaglet>();

            int indexOffset = 0;
            //for (int i = 0; i < input.Length; i++)
            int i = 0;
            while (i < strLength)
            {
                //if (i >= input.Length) break;
                char current = input[i];
                /*if (current.Equals('[') && tags.Count > 0 && (i + 7) <= input.Length)
                {
                    if (input.Substring(i, 3) == "[*]" || input.Substring(i, 7) == "[/list]")
                    {
                        debugLog.AppendLine("++ * " + inList + " <br />");
                    }
                }*/
                if (!inQuote && inList > 0)
                {
                    if (current.Equals('[') && tags.Count > 0 && (i + 7) <= input.Length)
                    {
                        if (input.Substring(i, 3) == "[*]" || input.Substring(i, 7) == "[/list]")
                        {
                            if (((BbcodeTag)tags.Peek()).Tag.Equals("*"))
                            {
                                //debugLog.AppendLine("*" + " <br />");
                                endTag = true;
                                Tag = "*";
                                parseList = true;
                            }
                        }
                    }
                }
                if (current.Equals(']') && !inQuote || (inList > 0 && parseList))
                {
                    parseList = false;
                    startAttr = false;
                    if (!endTag && !inCode)
                    {
                        if (Tag.Length > 0)
                        {
                            tags.Push(new BbcodeTag(Tag, attr, startIndex, indexOffset));
                            if (Tag.Equals("list"))
                            {
                                if (ValidList((BbcodeTag)tags.Peek()))
                                {
                                    if (EndTag(input, i, "list")) inList++;
                                }
                                else
                                {
                                    tags.Pop();
                                }
                            }
                            if (Tag.Equals("code")) inCode = true;
                        }
                    }
                    if (endTag)
                    {
                        if (Tag.Length > 0)
                        {
                            if (tags.Count > 0)
                            {
                                bool startTagExists = false;
                                foreach (BbcodeTag bt in tags)
                                {
                                    if (bt.Tag.Equals(Tag)) startTagExists = true;
                                }
                                if (startTagExists)
                                {
                                    while (!((BbcodeTag)tags.Peek()).Tag.Equals(Tag))
                                    {
                                        tags.Pop();
                                    }
                                }
                                if (((BbcodeTag)tags.Peek()).Tag.Equals(Tag))
                                {
                                    BbcodeTag tempTag = (BbcodeTag)tags.Pop();
                                    int tagLength = Tag.Length;
                                    int startTagLength = 2 + tagLength + tempTag.Attributes.Length;
                                    int endTagLength = 3 + tagLength;
                                    int startReplaceLength = 2 + tagLength;
                                    int endReplaceLength = 3 + tagLength;
                                    int endOffset = 0;
                                    bool abortParse = false;
                                    string insertStart = "";
                                    string insertEnd = "";
                                    bool listItem = false;

                                    BbcodeAttributes attrs; //attrs = new BbcodeAttributes(tempTag.Attributes);

                                    switch (Tag.ToLower())
                                    {
                                        case "quote":
                                            attrs = tempTag.GetAttributes(); //new BbcodeAttributes(tempTag.Attributes);
                                            if (attrs.HasAttributes())
                                            {
                                                if (inList == 0)
                                                {
                                                    insertStart = "</p><p><strong>" + Parse(attrs.GetAttribute("default")) + " wrote:</strong></p><blockquote><p>";
                                                }
                                                else
                                                {
                                                    insertStart = "<p><strong>" + Parse(attrs.GetAttribute("default")) + " wrote:</strong></p><blockquote>";
                                                }
                                            }
                                            else
                                            {
                                                if (inList == 0)
                                                {
                                                    insertStart = "</p><p><strong>quote:</strong></p><blockquote><p>";
                                                }
                                                else
                                                {
                                                    insertStart = "<p><strong>quote:</strong></p><blockquote>";
                                                }
                                            }
                                            insertEnd = "</p></blockquote><p>";
                                            break;
                                        case "b":
                                            insertStart = "<strong>";
                                            insertEnd = "</strong>";
                                            break;
                                        case "i":
                                            insertStart = "<em>";
                                            insertEnd = "</em>";
                                            break;
                                        case "u":
                                            insertStart = "<span style=\"text-decoration: underline;\">";
                                            insertEnd = "</span>";
                                            break;
                                        case "s":
                                            insertStart = "<span style=\"text-decoration: line-through;\">";
                                            insertEnd = "</span>";
                                            break;
                                        case "code":
                                            if (inList == 0)
                                            {
                                                insertStart = "<strong>Code</strong></p><p><code>";
                                                insertEnd = "</code></p><p>";
                                            }
                                            else
                                            {
                                                insertStart = "<strong>Code</strong><br /><code>";
                                                insertEnd = "</code>";
                                            }
                                            inCode = false;
                                            break;
                                        case "list":
                                            inList--;
                                            attrs = tempTag.GetAttributes(); //new BbcodeAttributes(tempTag.Attributes);
                                            if (attrs.GetAttribute("default") != null)
                                            {
                                                if (Regex.IsMatch(attrs.GetAttribute("default"), "^[aA1iI]{1}$", RegexOptions.Compiled))
                                                {
                                                    if (inList == 0)
                                                    {
                                                        insertStart = "</p><ol style=\"list-style-type:" + Bbcode.OlTypeToCssName(attrs.GetAttribute("default")) + "\">";
                                                        insertEnd = "</ol><p>";
                                                    }
                                                    else
                                                    {
                                                        insertStart = "<ol style=\"list-style-type:" + Bbcode.OlTypeToCssName(attrs.GetAttribute("default")) + "\">";
                                                        insertEnd = "</ol>";
                                                    }
                                                }
                                                else if (Regex.IsMatch(attrs.GetAttribute("default"), "^(circle|square)$", RegexOptions.Compiled))
                                                {
                                                    if (inList == 0)
                                                    {
                                                        insertStart = "</p><ul style=\"list-style-type:" + Bbcode.OlTypeToCssName(attrs.GetAttribute("default")) + "\">";
                                                        insertEnd = "</ul><p>";
                                                    }
                                                    else
                                                    {
                                                        insertStart = "<ul style=\"list-style-type:" + Bbcode.OlTypeToCssName(attrs.GetAttribute("default")) + "\">";
                                                        insertEnd = "</ul>";
                                                    }
                                                }
                                                else
                                                {
                                                    abortParse = true;
                                                }
                                            }
                                            else
                                            {
                                                if (inList == 0)
                                                {
                                                    insertStart = "</p><ul>";
                                                    insertEnd = "</ul><p>";
                                                }
                                                else
                                                {
                                                    insertStart = "<ul>";
                                                    insertEnd = "</ul>";
                                                }
                                            }
                                            break;
                                        case "*":
                                            listItem = true;
                                            insertStart = "<li>";
                                            insertEnd = "</li>";
                                            endOffset = 0;
                                            //startIndex -= 1;
                                            endTagLength = 0;
                                            break;
                                        case "color":
                                            attrs = tempTag.GetAttributes(); //new BbcodeAttributes(tempTag.Attributes);
                                            try { System.Drawing.ColorTranslator.FromHtml(attrs.GetAttribute("default")); } //!Regex.IsMatch(attrs.GetAttribute("default"),"^(\\#([a-fA-F0-9]+))|([a-zA-Z]+)$", RegexOptions.Compiled)) abortParse = true;
                                            catch { abortParse = true; }
                                            insertStart = "<span style=\"color: " + attrs.GetAttribute("default") + "\">";
                                            insertEnd = "</span>";
                                            break;
                                        case "size":
                                            attrs = tempTag.GetAttributes(); //new BbcodeAttributes(tempTag.Attributes);
                                            try
                                            {
                                                int fontSize = int.Parse(attrs.GetAttribute("default"));
                                                if (fontSize > 22 || fontSize < 8) abortParse = true;
                                            }
                                            catch
                                            {
                                                abortParse = true;
                                            }
                                            insertStart = "<span style=\"font-size: " + attrs.GetAttribute("default") + "pt\">";
                                            insertEnd = "</span>";
                                            break;
                                        case "h1":
                                            insertStart = "</p><h2>";
                                            insertEnd = "</h2><p>";
                                            break;
                                        case "h2":
                                            insertStart = "</p><h3>";
                                            insertEnd = "</h3><p>";
                                            break;
                                        case "h3":
                                            insertStart = "</p><h4>";
                                            insertEnd = "</h4><p>";
                                            break;
                                        case "align":
                                            attrs = tempTag.GetAttributes();
                                            if (!Regex.IsMatch(attrs.GetAttribute("default"), "^(left|right|center|justify)$", RegexOptions.Compiled)) abortParse = true;
                                            insertStart = "</p><p style=\"text-align: " + attrs.GetAttribute("default") + "\">";
                                            insertEnd = "</p><p>";
                                            break;
                                        case "url":
                                            attrs = tempTag.GetAttributes(); //new BbcodeAttributes(tempTag.Attributes);
                                            if (attrs.HasAttributes())
                                            {
                                                if (Regex.IsMatch(attrs.GetAttribute("default"), "^([\\w]+?://[\\w\\#$%&~/.\\-;:=,?@\\[\\]+]*?)$", RegexOptions.Compiled))
                                                {
                                                    insertStart = "<a href=\"" + attrs.GetAttribute("default") + "\">";
                                                }
                                                else if (Regex.IsMatch(attrs.GetAttribute("default"), "^((www|ftp)\\.[\\w\\#$%&~/.\\-;:=,?@\\[\\]+]*?)$", RegexOptions.Compiled))
                                                {
                                                    insertStart = "<a href=\"http://" + attrs.GetAttribute("default") + "\">";
                                                }
                                                else
                                                {
                                                    abortParse = true;
                                                }
                                                insertEnd = "</a>";
                                            }
                                            else
                                            {
                                                int urlStartIndex = tempTag.indexStart + startTagLength;
                                                string urlBbcode = input.Substring(urlStartIndex, i - urlStartIndex - endTagLength + 1);
                                                if (Regex.IsMatch(urlBbcode, "^([\\w]+?://[\\w\\#$%&~/.\\-;:=,?@\\[\\]+]*?)$", RegexOptions.Compiled))
                                                {
                                                    insertStart = "<a href=\"" + urlBbcode + "\">";
                                                }
                                                else if (Regex.IsMatch(urlBbcode, "^((www|ftp)\\.[\\w\\#$%&~/.\\-;:=,?@\\[\\]+]*?)$", RegexOptions.Compiled))
                                                {
                                                    insertStart = "<a href=\"http://" + urlBbcode + "\">";
                                                }
                                                else
                                                {
                                                    abortParse = true;
                                                }
                                                insertEnd = "</a>";
                                            }
                                            break;
                                        case "inline":
                                            if (postOwner == null)
                                            {
                                                abortParse = true;
                                            }
                                            else
                                            {
                                                insertStart = "<img alt=\"Bbcode image\" style=\"max-width: 100%;\" src=\"/" + HttpUtility.HtmlEncode(postOwner.UserName) + "/images/_display/";
                                                insertEnd = "\" />";
                                            }
                                            break;
                                        case "thumb":
                                            if (postOwner == null)
                                            {
                                                abortParse = true;
                                            }
                                            else
                                            {
                                                insertStart = "<img alt=\"Bbcode image\" src=\"/" + HttpUtility.HtmlEncode(postOwner.UserName) + "/images/_thumb/";
                                                insertEnd = "\" />";
                                            }
                                            break;
                                        case "img":

                                            int imgStartIndex = tempTag.indexStart + startTagLength;
                                            string imgBbcode = input.Substring(imgStartIndex, i - imgStartIndex - endTagLength + 1);
                                            if (!Regex.IsMatch(imgBbcode, "^((http|ftp|https|ftps)://)([^ \\?&=\\#\\\"\\n\\r\\t<]*?(\\.(jpg|jpeg|gif|png)))$", RegexOptions.IgnoreCase)) abortParse = true;
                                            if (TagAllowed(Tag, options))
                                            {
                                                insertStart = "<img alt=\"Bbcode image\" src=\"";
                                                insertEnd = "\" />";
                                            }
                                            else
                                            {
                                                insertStart = "<a href=\"" + imgBbcode + "\"><strong>IMG</strong>: ";
                                                insertEnd = "</a>";
                                            }
                                            break;
                                        case "youtube":
                                            // TODO: optional youtube BBcode rendering
                                            int ytStartIndex = tempTag.indexStart + startTagLength;
                                            string youTubeUrl = input.Substring(ytStartIndex, i - ytStartIndex - endTagLength + 1);

                                            startTagLength += youTubeUrl.Length;

                                            if (youTubeUrl.ToLower().StartsWith("http://"))
                                            {
                                                char[] splitChars = { '=', '?', '&' };
                                                string[] argh = youTubeUrl.Split(splitChars);
                                                if (argh.Length <= 2)
                                                {
                                                    abortParse = true;
                                                }
                                                for (int y = 0; y < argh.Length - 1; y++)
                                                {
                                                    if (argh[y] == "v")
                                                    {
                                                        youTubeUrl = argh[y + 1];
                                                    }
                                                    else if (y == argh.Length - 2)
                                                    {
                                                        abortParse = true;
                                                    }
                                                }
                                            }

                                            if (TagAllowed(Tag, options))
                                            {
                                                youTubeUrl = "http://www.youtube.com/v/" + youTubeUrl;

                                                insertStart = "<object width=\"425\" height=\"350\"><param name=\"movie\" value=\"" + youTubeUrl + "\"></param><embed src=\"" + youTubeUrl + "\" type=\"application/x-shockwave-flash\" width=\"425\" height=\"350\"></embed></object>";
                                                insertEnd = "";
                                            }
                                            else
                                            {
                                                youTubeUrl = "http://www.youtube.com/watch?v=" + youTubeUrl;
                                                insertStart = "<a href=\"" + youTubeUrl + "\"><strong>YT:</strong> " + youTubeUrl;
                                                insertEnd = "</a>";
                                            }
                                            break;
                                        case "latex":
                                            // TODO: LaTeX BBcode
                                            int latexStartIndex = tempTag.indexStart + startTagLength;
                                            string latexExpression = input.Substring(latexStartIndex, i - latexStartIndex - endTagLength + 1);

                                            startTagLength += latexExpression.Length;

                                            latexExpression = HttpUtility.UrlEncode(latexExpression).Replace("+", "%20");

                                            insertStart = "<img src=\"http://agreendaysite.com/mimetex.cgi?" + latexExpression + "\" alt=\"LaTeX Equation\"/>";
                                            insertEnd = "";
                                            //abortParse = true;
                                            break;
                                        case "flash":
                                            // TODO: FLASH BBcode
                                            if (TagAllowed(Tag, options))
                                            {
                                            }
                                            else
                                            {
                                            }
                                            abortParse = true;
                                            break;
                                        case "silverlight":
                                            // TODO: Silverlight BBcode
                                            if (TagAllowed(Tag, options))
                                            {
                                            }
                                            else
                                            {
                                            }
                                            abortParse = true;
                                            break;
                                        default:
                                            abortParse = true;
                                            break;
                                    }

                                    startReplaceLength = insertStart.Length;
                                    endReplaceLength = insertEnd.Length;

                                    if (!abortParse)
                                    {
                                        /* original method */
                                        /*output.Remove(tempTag.outputIndexStart, startTagLength);
                                        output.Remove(indexOffset + i - startTagLength - endTagLength + 1 - endOffset, endTagLength);
                                        output.Insert(tempTag.outputIndexStart, insertStart);
                                        output.Insert(indexOffset + i - (startTagLength - startReplaceLength) - endTagLength + 1 - endOffset, insertEnd);*/

                                        /* single level modified method */
                                        /*if (tempTag.indexStart - lastEndIndex > 0)
                                        {
                                            newOutput.Append(input.Substring(lastEndIndex, tempTag.indexStart - lastEndIndex));
                                        }
                                        newOutput.Append(insertStart);
                                        if (startIndex - (tempTag.indexStart + startTagLength) > 0)
                                        {
                                            newOutput.Append(input.Substring(tempTag.indexStart + startTagLength, startIndex - (tempTag.indexStart + startTagLength)));
                                        }
                                        newOutput.Append(insertEnd);
                                        lastEndIndex = i + 1;*/

                                        /* two pass method */
                                        taglets.Add(new BbcodeTaglet(tempTag.indexStart, startTagLength, insertStart));
                                        taglets.Add(new BbcodeTaglet(startIndex - endOffset, endTagLength, insertEnd, listItem)); // startIndex - endOffset

                                        //i -= ((startTagLength - startReplaceLength) + (endTagLength - endReplaceLength));

                                        /* original method */
                                        /*indexOffset -= ((startTagLength - startReplaceLength) + (endTagLength - endReplaceLength));*/
                                    }
                                }
                            }
                        }
                    }
                    inTag = false;
                    endTag = false;
                    inQuote = false;
                    Tag = "";
                    attr = "";
                }
                else
                {
                    if (inTag)
                    {
                        /*if (current.Equals('"'))
                        {
                            inQuote = !inQuote;
                        }*/
                        if (current.Equals('&'))
                        {
                            if (input.Substring(i, 6).Equals("&quot;"))
                            {
                                inQuote = !inQuote;
                            }
                        }
                        if (i == startIndex + 1 && current.Equals('/'))
                        {
                            endTag = true;
                        }
                        else
                        {
                            if (current.Equals(' ') || current.Equals('='))
                            {
                                if (Tag.Length == 0)
                                {
                                    inTag = false;
                                    endTag = false;
                                    inQuote = false;
                                    Tag = "";
                                    attr = "";
                                }
                                else
                                {
                                    startAttr = true;
                                }
                            }
                            if (startAttr)
                            {
                                attr += current.ToString();
                            }
                            else
                            {
                                if ((current >= 'a' && current <= 'z') || (current >= 'A' && current <= 'Z') || (current >= '0' && current <= '9') || current == '*')
                                {
                                    Tag += current.ToString();
                                }
                                else
                                {
                                    inTag = false;
                                    endTag = false;
                                    inQuote = false;
                                    Tag = "";
                                    attr = "";
                                }
                            }
                        }
                    }
                }
                if (current.Equals('[') && !inQuote)
                {
                    startIndex = i;
                    inTag = true;
                    endTag = false;
                    inQuote = false;
                    startAttr = false; // fixed parsing error
                    Tag = "";
                    attr = "";
                }
                i++;
                if (!inTag)
                {
                    int nextIndex = input.IndexOf('[', i);
                    if (nextIndex > 0)
                    {
                        i = startIndex = nextIndex;
                        /*inTag = true;
                        endTag = false;
                        inQuote = false;
                        startAttr = false; // fixed parsing error
                        Tag = "";
                        attr = "";*/
                    }
                }
            }
            /*input = output.ToString();*/

            /* second pass */
            /* unpack the list into the input stream */
            taglets.Sort();

            //newOutput.Append(taglets.Count.ToString() + "<br />");

            for (int t = 0; t < taglets.Count; t++)
            {
                BbcodeTaglet taglet = taglets[t];
                if (taglet.StartIndex - lastEndIndex > 0)
                {
                    newOutput.Append(input.Substring(lastEndIndex, taglet.StartIndex - lastEndIndex));
                }
                newOutput.Append(taglet.RenderText);
                lastEndIndex = taglet.StartIndex + taglet.Length;
            }

            if (input.Length > lastEndIndex)
            {
                newOutput.Append(input.Substring(lastEndIndex, input.Length - lastEndIndex));
            }

            input = newOutput.ToString();
            double time = ((double)(DateTime.Now.Ticks - start)) / 10000000;
            /*input += "<br />t:" + time;*/
            input = input.Replace("\r\n", "\n");
            input = input.Replace("\n", "<br />");
            //input = input.Replace("<br /><br />", "</p>\n<p>");
            input = input.Replace("<p></p>", "");
            input = input.Replace("<p><br /><br />", "<p>");
            input = input.Replace("<p><br />", "<p>");
            input = input.Replace("<br /></p>", "</p>");
            input = input.Replace("<br /><li>", "<li>");
            input = input.Replace("<br /></ul>", "</ul>");
            input = input.Replace("<br /></ol>", "</ol>");
            input = input.Replace("<blockquote></blockquote>", "<blockquote>&nbsp;</blockquote>");
            input = Regex.Replace(input, @"\<p\>(\s)\<\/p\>", "", RegexOptions.Compiled);
            return input;
        }

        private static bool EndTag(string input, int startIndex, string tag)
        {
            int notMine = 0;
            string startTag = "[" + tag + "]";
            string endTag = "[/" + tag + "]";
            int startTagLength = tag.Length + 2;
            int endTagLength = tag.Length + 3;
            //for (int i = startIndex; i < (input.Length - endTagLength); i++)
            int i = startIndex;
            byte nextIndexOf = 0;
            while (i <= (input.Length - endTagLength) && i >= startIndex)
            {
                if (nextIndexOf == 1) //input.Substring(i, startTagLength) == startTag)
                {
                    notMine++;
                }
                if (nextIndexOf == 2) //input.Substring(i, endTagLength) == endTag)
                {
                    if (notMine == 0) return true;
                    notMine--;
                }
                nextIndexOf = 0;
                i++;
                int endT = input.IndexOf(startTag, i);
                int startT = input.IndexOf(endTag, i);
                if (endT <= 0) endT = startT + 1;
                else nextIndexOf = 1;
                if (startT <= 0) endT = endT + 1;
                else if (startT < endT) nextIndexOf = 2;
                i = Math.Min(endT, startT);
            }
            return false;
        }

        private static bool TagAllowed(string tag, List<BbcodeOptions> options)
        {
            if (options != null)
            {
                switch (tag)
                {
                    case "img":
                        if (options.Contains(BbcodeOptions.DisableImages))
                        {
                            return false;
                        }
                        break;
                    case "youtube":
                        if (options.Contains(BbcodeOptions.DisableVideo))
                        {
                            return false;
                        }
                        break;
                    case "flash":
                        if (options.Contains(BbcodeOptions.DisableFlash))
                        {
                            return false;
                        }
                        break;
                    case "silverlight":
                        // we'll treat silverlight as flash for now
                        if (options.Contains(BbcodeOptions.DisableFlash))
                        {
                            return false;
                        }
                        break;
                }
            }
            return true;
        }

        private static bool ValidList(BbcodeTag tag)
        {
            BbcodeAttributes attr = new BbcodeAttributes(tag.Attributes);
            if (!attr.HasAttributes()) return true;
            if (Regex.IsMatch(attr.GetAttribute("default"), "^(circle|square)|([aAiI1]{1})$", RegexOptions.Compiled))
                return true;
            return false;
        }

        public static string ParseSmilies(string input)
        {
            /*if (Global.SmiliesDataTable == null)
            {
                Global.SmiliesDataTable = Global.db.SelectQuery("SELECT * FROM smilies;");
            }
            foreach (DataRow smile in Global.SmiliesDataTable.Rows)
            {
                input = input.Replace(smile["SmilieCode"].ToString(), "<img alt=\"" + smile["SmilieDescription"] + "\" title=\"" + smile["SmilieDescription"] + "\" src=\"" + ConfigurationSettings.AppSettings["smilie-path"] + smile["SmilieUri"] + "\" />");
            }*/
            return input;
        }
    }

    public class BbcodeAttributes
    {
        Hashtable Attributes;
        public BbcodeAttributes(string input)
        {
            Attributes = new Hashtable();
            bool inQuote = false;
            bool inValue = false;
            int length = input.Length;
            string param = "";
            string val = "";
            for (int i = 0; i < length; i++)
            {
                /*if (input[i].Equals('"'))
                {
                    inQuote = !inQuote;
                }*/
                if (input[i].Equals('&'))
                {
                    if (input.Substring(i, 6).Equals("&quot;"))
                    {
                        inQuote = !inQuote;
                        i += 5;
                    }
                }
                else
                {
                    if (!inValue)
                    {
                        if (input[i].Equals('='))
                        {
                            inValue = true;
                        }
                        else
                        {
                            param += input[i].ToString();
                        }
                    }
                    else
                    {
                        val += input[i].ToString();
                    }
                }
                if ((!inQuote && input[i].Equals(' ')) || i + 1 == length)
                {
                    if (param.Length == 0) param = "default";
                    Attributes.Add(param, val);
                    param = "";
                    val = "";
                    inValue = false;
                }
            }
        }

        public string GetAttribute(string key)
        {
            return Attributes[key] as string;
        }

        public bool HasAttributes()
        {
            return (Attributes.Count > 0);
        }
    }
}