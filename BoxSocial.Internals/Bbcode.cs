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
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Web;
using System.Text.RegularExpressions;
using System.Text;

namespace BoxSocial.Internals
{
    /// <summary>
    /// Summary description for Bbcode
    /// </summary>

    public enum BbcodeOptions : byte
    {
        None = 0x00,
        ShowImages = 0x01,
        ShowFlash = 0x02,
        ShowVideo = 0x04,
        ShowAudio = 0x08,
    }

    public class Bbcode
    {
        private Core core = null;

        private delegate void BbcodeHookHandler(BbcodeEventArgs e);

        private event BbcodeHookHandler BbcodeHooks;

        private List<string> styleList;

        public Bbcode(Core core)
        {
            this.core = core;

            Initialise();
        }

        public void Initialise()
        {
            BbcodeHooks += new BbcodeHookHandler(BbcodeQuote);
            BbcodeHooks += new BbcodeHookHandler(BbcodeBold);
            BbcodeHooks += new BbcodeHookHandler(BbcodeItalic);
            BbcodeHooks += new BbcodeHookHandler(BbcodeUnderline);
            BbcodeHooks += new BbcodeHookHandler(BbcodeStrikeout);
            BbcodeHooks += new BbcodeHookHandler(BbcodeCode);
            BbcodeHooks += new BbcodeHookHandler(BbcodeList);
            BbcodeHooks += new BbcodeHookHandler(BbcodeListItem);
            BbcodeHooks += new BbcodeHookHandler(BbcodeColour);
            BbcodeHooks += new BbcodeHookHandler(BbcodeSize);
            BbcodeHooks += new BbcodeHookHandler(BbcodeH1);
            BbcodeHooks += new BbcodeHookHandler(BbcodeH2);
            BbcodeHooks += new BbcodeHookHandler(BbcodeH3);
            BbcodeHooks += new BbcodeHookHandler(BbcodeAlign);
            BbcodeHooks += new BbcodeHookHandler(BbcodeFloat);
            BbcodeHooks += new BbcodeHookHandler(BbcodeUrl);
            BbcodeHooks += new BbcodeHookHandler(BbcodeInternalUrl);
            BbcodeHooks += new BbcodeHookHandler(BbcodeInline);
            BbcodeHooks += new BbcodeHookHandler(BbcodeThumb);
            BbcodeHooks += new BbcodeHookHandler(BbcodeImage);
            BbcodeHooks += new BbcodeHookHandler(BbcodeYouTube);
            BbcodeHooks += new BbcodeHookHandler(BbcodeLaTeX);
            // TODO: flash
            //BbcodeHooks += new BbcodeHookHandler(BbcodeFlash);
            // TODO: silverlight
            BbcodeHooks += new BbcodeHookHandler(BbcodeUser);

            styleList = new List<string>();
            styleList.Add("color");
            styleList.Add("size");
        }

        private sealed class BbcodeEventArgs
        {
            private BbcodeTag tag;
            private BbcodeAttributes attributes;
            private BbcodeOptions options;
            private string prefixText;
            private string suffixText;
            private bool inList;
            private bool handled;
            private bool abortParse;
            private bool noContents;
            private string contents;
            private bool stripTag;
            private User owner = null;

            public BbcodeTag Tag
            {
                get
                {
                    return tag;
                }
            }

            public BbcodeAttributes Attributes
            {
                get
                {
                    return attributes;
                }
            }

            public BbcodeOptions Options
            {
                get
                {
                    return options;
                }
            }

            public string PrefixText
            {
                get
                {
                    return prefixText;
                }
                set
                {
                    prefixText = value;
                }
            }

            public string SuffixText
            {
                get
                {
                    return suffixText;
                }
                set
                {
                    suffixText = value;
                }
            }

            public bool InList
            {
                get
                {
                    return inList;
                }
            }

            public bool StripTag
            {
                get
                {
                    return stripTag;
                }
            }

            public void SetHandled()
            {
                handled = true;
            }

            public void AbortParse()
            {
                abortParse = true;
            }

            public bool Handled
            {
                get
                {
                    return handled;
                }
            }

            public bool ParseAborted
            {
                get
                {
                    return abortParse;
                }
            }

            public void RemoveContents()
            {
                noContents = true;
            }

            public bool NoContents
            {
                get
                {
                    return noContents;
                }
            }

            public string Contents
            {
                get
                {
                    return contents;
                }
            }

            public User Owner
            {
                get
                {
                    return owner;
                }
            }

            public BbcodeEventArgs(string contents, BbcodeTag tag, BbcodeOptions options, User postOwner, bool inList, bool stripTag, ref string prefixText, ref string suffixText, ref bool handled, ref bool abortParse)
            {
                this.tag = tag;
                this.options = options;
                this.attributes = tag.GetAttributes();
                this.contents = contents;
                this.prefixText = prefixText;
                this.suffixText = suffixText;
                this.inList = inList;
                this.stripTag = stripTag;
                this.handled = handled;
                this.abortParse = abortParse;
                this.owner = postOwner;
            }
        }

        private sealed class BbcodeTag
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

            public int StartLength
            {
                get
                {
                    return 2 + Tag.Length + Attributes.Length;
                }
            }

            public int EndLength
            {
                get
                {
                    return 3 + Tag.Length;
                }
            }
        }

        private sealed class BbcodeTaglet : IComparable
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

        public string Parse(string input)
        {
            return Parse(input, null);
        }

        public string Parse(string input, User viewer)
        {
            return Parse(input, viewer, null);
        }

        public string Parse(string input, User viewer, User postOwner)
        {
            return Parse(input, viewer, postOwner, false);
        }

        private string Parse(string input, User viewer, User postOwner, bool stripTags)
        {
            if (string.IsNullOrEmpty(input))
            {
                return "";
            }

            StringBuilder debugLog = new StringBuilder();

            BbcodeOptions options = BbcodeOptions.ShowImages | BbcodeOptions.ShowFlash | BbcodeOptions.ShowVideo | BbcodeOptions.ShowAudio;

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

            StringBuilder newOutput = new StringBuilder();
            int lastEndIndex = 0;

            List<BbcodeTaglet> taglets = new List<BbcodeTaglet>();

            int indexOffset = 0;
            int i = 0;
            while (i < strLength)
            {
                char current = input[i];
                if (!inQuote && inList > 0)
                {
                    if (current.Equals('[') && tags.Count > 0 && (i + 7) <= input.Length)
                    {
                        if (input.Substring(i, 3) == "[*]" || input.Substring(i, 7) == "[/list]")
                        {
                            if (((BbcodeTag)tags.Peek()).Tag.Equals("*"))
                            {
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
                                        // TODO: style
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

                                    /*
                                     * A couple of special cases
                                     */
                                    if (Tag == "code")
                                    {
                                        inCode = false;
                                    }

                                    if (Tag == "list")
                                    {
                                        inList--;
                                    }

                                    if (Tag == "*")
                                    {
                                        listItem = true;
                                        endOffset = 0;
                                        endTagLength = 0;
                                    }

                                    bool handled = false;

                                    int tempIndex = tempTag.indexStart + tempTag.StartLength;
                                    string contents = input.Substring(tempIndex, i - tempIndex - tempTag.EndLength + 1);

                                    BbcodeEventArgs eventArgs = new BbcodeEventArgs(contents, tempTag, options, postOwner, (inList > 0), stripTags, ref insertStart, ref insertEnd, ref handled, ref abortParse);
                                    BbcodeHooks(eventArgs);

                                    insertStart = eventArgs.PrefixText;
                                    insertEnd = eventArgs.SuffixText;
                                    handled = eventArgs.Handled;
                                    abortParse = eventArgs.ParseAborted;

                                    if (eventArgs.NoContents)
                                    {
                                        startTagLength += eventArgs.Contents.Length;
                                    }

                                    if (!handled)
                                    {
                                        abortParse = true;
                                    }

                                    startReplaceLength = insertStart.Length;
                                    endReplaceLength = insertEnd.Length;

                                    if (!abortParse)
                                    {
                                        /* two pass method */
                                        taglets.Add(new BbcodeTaglet(tempTag.indexStart, startTagLength, insertStart));
                                        taglets.Add(new BbcodeTaglet(startIndex - endOffset, endTagLength, insertEnd, listItem));
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
                        if (current.Equals('&') && (i + 6) <= strLength && input.Substring(i, 6).Equals("&quot;"))
                        {
                            inQuote = !inQuote;
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
                                    Tag += current.ToString().ToLower();
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
                    }
                }
            }

            /* second pass */
            /* unpack the list into the input stream */
            taglets.Sort();

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
            input = input.Replace("\r\n", "\n");
            input = input.Replace("\n", "<br />");
            input = input.Replace("<p></p>", "");
            input = input.Replace("<p><br /><br />", "<p>");
            input = input.Replace("<p><br />", "<p>");
            input = input.Replace("<br /></p>", "</p>");
            input = input.Replace("<br /><li>", "<li>");
            input = input.Replace("<br /></ul>", "</ul>");
            input = input.Replace("<br /></ol>", "</ol>");
            input = input.Replace("<blockquote></blockquote>", "<blockquote>&nbsp;</blockquote>");
            //input = Regex.Replace(input, @"\<p\>(\s)\<\/p\>", "", RegexOptions.Compiled);
            input = input.Replace("<p> </p>", "");
            input = input.Replace("<p>\n</p>", "");
            input = input.Replace("<p>\r\n</p>", "");
            return input;
        }

        private static bool EndTag(string input, int startIndex, string tag)
        {
            int notMine = 0;
            string startTag = "[" + tag + "]";
            string endTag = "[/" + tag + "]";
            int startTagLength = tag.Length + 2;
            int endTagLength = tag.Length + 3;
            int i = startIndex;
            byte nextIndexOf = 0;
            while (i <= (input.Length - endTagLength) && i >= startIndex)
            {
                if (nextIndexOf == 1)
                {
                    notMine++;
                }
                if (nextIndexOf == 2)
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

        private static bool TagAllowed(string tag, BbcodeOptions options)
        {
            switch (tag)
            {
                case "img":
                    if ((options & BbcodeOptions.ShowImages) != BbcodeOptions.ShowImages)
                    {
                        return false;
                    }
                    break;
                case "youtube":
                    if ((options & BbcodeOptions.ShowVideo) != BbcodeOptions.ShowVideo)
                    {
                        return false;
                    }
                    break;
                case "flash":
                    if ((options & BbcodeOptions.ShowFlash) != BbcodeOptions.ShowFlash)
                    {
                        return false;
                    }
                    break;
                case "silverlight":
                    // we'll treat silverlight as flash for now
                    if ((options & BbcodeOptions.ShowFlash) != BbcodeOptions.ShowFlash)
                    {
                        return false;
                    }
                    break;
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
                Global.SmiliesDataTable = Global.db.Query("SELECT * FROM smilies;");
            }
            foreach (DataRow smile in Global.SmiliesDataTable.Rows)
            {
                input = input.Replace(smile["SmilieCode"].ToString(), "<img alt=\"" + smile["SmilieDescription"] + "\" title=\"" + smile["SmilieDescription"] + "\" src=\"" + ConfigurationSettings.AppSettings["smilie-path"] + smile["SmilieUri"] + "\" />");
            }*/
            return input;
        }

        private void BbcodeQuote(BbcodeEventArgs e)
        {
            if (e.Tag.Tag != "quote") return;

            e.SetHandled();

            if (e.StripTag)
            {
                if (e.Attributes.HasAttributes())
                {
                    e.PrefixText = "\n--- Quote: " + Parse(e.Attributes.GetAttribute("default"), null, e.Owner, e.StripTag) + " wrote: ---\n";
                }
                else
                {
                    e.PrefixText = "\n--- Quote: ---\n";
                }
                e.SuffixText = "\n---------------------\n";
            }
            else
            {
                if (e.Attributes.HasAttributes())
                {
                    if (!e.InList)
                    {
                        e.PrefixText = "</p><p><strong>" + Parse(e.Attributes.GetAttribute("default")) + " wrote:</strong></p><blockquote><p>";
                    }
                    else
                    {
                        e.PrefixText = "<p><strong>" + Parse(e.Attributes.GetAttribute("default")) + " wrote:</strong></p><blockquote>";
                    }
                }
                else
                {
                    if (!e.InList)
                    {
                        e.PrefixText = "</p><p><strong>quote:</strong></p><blockquote><p>";
                    }
                    else
                    {
                        e.PrefixText = "<p><strong>quote:</strong></p><blockquote>";
                    }
                }
                e.SuffixText = "</p></blockquote><p>";
            }
        }

        private static void BbcodeBold(BbcodeEventArgs e)
        {
            if (e.Tag.Tag != "b") return;

            e.SetHandled();

            if (e.StripTag)
            {
                e.PrefixText = "*";
                e.SuffixText = "*";
            }
            else
            {
                e.PrefixText = "<strong>";
                e.SuffixText = "</strong>";
            }
        }

        private static void BbcodeItalic(BbcodeEventArgs e)
        {
            if (e.Tag.Tag != "i") return;

            e.SetHandled();

            if (e.StripTag)
            {
                e.PrefixText = "_";
                e.SuffixText = "_";
            }
            else
            {
                e.PrefixText = "<em>";
                e.SuffixText = "</em>";
            }
        }

        private static void BbcodeUnderline(BbcodeEventArgs e)
        {
            if (e.Tag.Tag != "u") return;

            e.SetHandled();

            if (e.StripTag)
            {
                e.PrefixText = "";
                e.SuffixText = "";
            }
            else
            {
                e.PrefixText = "<span style=\"text-decoration: underline;\">";
                e.SuffixText = "</span>";
            }
        }

        private static void BbcodeStrikeout(BbcodeEventArgs e)
        {
            if (e.Tag.Tag != "s") return;

            e.SetHandled();

            if (e.StripTag)
            {
                e.PrefixText = "--";
                e.SuffixText = "--";
            }
            else
            {
                e.PrefixText = "<span style=\"text-decoration: line-through;\">";
                e.SuffixText = "</span>";
            }
        }

        private static void BbcodeCode(BbcodeEventArgs e)
        {
            if (e.Tag.Tag != "code") return;

            e.SetHandled();

            if (e.StripTag)
            {
                e.PrefixText = "--- Code ---";
                e.SuffixText = "------";
            }
            else
            {
                if (!e.InList)
                {
                    e.PrefixText = "<strong>Code</strong></p><p><code>";
                    e.SuffixText = "</code></p><p>";
                }
                else
                {
                    e.PrefixText = "<strong>Code</strong><br /><code>";
                    e.SuffixText = "</code>";
                }
            }
        }

        private static void BbcodeList(BbcodeEventArgs e)
        {
            if (e.Tag.Tag != "list") return;

            e.SetHandled();

            if (e.StripTag)
            {
            }
            else
            {
                if (e.Attributes.GetAttribute("default") != null)
                {
                    if (Regex.IsMatch(e.Attributes.GetAttribute("default"), "^[aA1iI]{1}$", RegexOptions.Compiled))
                    {
                        if (!e.InList)
                        {
                            e.PrefixText = "</p><ol style=\"list-style-type:" + Bbcode.OlTypeToCssName(e.Attributes.GetAttribute("default")) + "\">";
                            e.SuffixText = "</ol><p>";
                        }
                        else
                        {
                            e.PrefixText = "<ol style=\"list-style-type:" + Bbcode.OlTypeToCssName(e.Attributes.GetAttribute("default")) + "\">";
                            e.SuffixText = "</ol>";
                        }
                    }
                    else if (Regex.IsMatch(e.Attributes.GetAttribute("default"), "^(circle|square)$", RegexOptions.Compiled))
                    {
                        if (!e.InList)
                        {
                            e.PrefixText = "</p><ul style=\"list-style-type:" + Bbcode.OlTypeToCssName(e.Attributes.GetAttribute("default")) + "\">";
                            e.SuffixText = "</ul><p>";
                        }
                        else
                        {
                            e.PrefixText = "<ul style=\"list-style-type:" + Bbcode.OlTypeToCssName(e.Attributes.GetAttribute("default")) + "\">";
                            e.SuffixText = "</ul>";
                        }
                    }
                    else
                    {
                        e.AbortParse();
                    }
                }
                else
                {
                    if (!e.InList)
                    {
                        e.PrefixText = "</p><ul>";
                        e.SuffixText = "</ul><p>";
                    }
                    else
                    {
                        e.PrefixText = "<ul>";
                        e.SuffixText = "</ul>";
                    }
                }
            }
        }

        private static void BbcodeListItem(BbcodeEventArgs e)
        {
            if (e.Tag.Tag != "*") return;

            e.SetHandled();

            if (e.StripTag)
            {
                e.PrefixText = " *";
                e.SuffixText = "";
            }
            else
            {
                e.PrefixText = "<li>";
                e.SuffixText = "</li>";
            }
        }

        private static void BbcodeColour(BbcodeEventArgs e)
        {
            if (e.Tag.Tag != "color") return;

            e.SetHandled();

            if (e.StripTag)
            {
                e.PrefixText = "";
                e.SuffixText = "";
            }
            else
            {
                try { System.Drawing.ColorTranslator.FromHtml(e.Attributes.GetAttribute("default")); }
                catch { e.AbortParse(); }
                e.PrefixText = "<span style=\"color: " + e.Attributes.GetAttribute("default") + "\">";
                e.SuffixText = "</span>";
            }
        }

        private static void BbcodeSize(BbcodeEventArgs e)
        {
            if (e.Tag.Tag != "size") return;

            e.SetHandled();

            if (e.StripTag)
            {
                e.PrefixText = "";
                e.SuffixText = "";
            }
            else
            {
                try
                {
                    int fontSize = int.Parse(e.Attributes.GetAttribute("default"));
                    if (fontSize > 300 || fontSize < 25) e.AbortParse();
                }
                catch
                {
                    e.AbortParse();
                }
                e.PrefixText = "<span style=\"font-size: " + e.Attributes.GetAttribute("default") + "%\">";
                e.SuffixText = "</span>";
            }
        }

        private static void BbcodeH1(BbcodeEventArgs e)
        {
            if (e.Tag.Tag != "h1") return;

            e.SetHandled();

            if (e.StripTag)
            {
                e.PrefixText = "";
                e.SuffixText = "";
            }
            else
            {
                e.PrefixText = "</p><h2>";
                e.SuffixText = "</h2><p>";
            }
        }

        private static void BbcodeH2(BbcodeEventArgs e)
        {
            if (e.Tag.Tag != "h2") return;

            e.SetHandled();

            if (e.StripTag)
            {
                e.PrefixText = "";
                e.SuffixText = "";
            }
            else
            {
                e.PrefixText = "</p><h3>";
                e.SuffixText = "</h3><p>";
            }
        }

        private static void BbcodeH3(BbcodeEventArgs e)
        {
            if (e.Tag.Tag != "h3") return;

            e.SetHandled();

            if (e.StripTag)
            {
                e.PrefixText = "";
                e.SuffixText = "";
            }
            else
            {
                e.PrefixText = "</p><h4>";
                e.SuffixText = "</h4><p>";
            }
        }

        private static void BbcodeAlign(BbcodeEventArgs e)
        {
            if (e.Tag.Tag != "align") return;

            e.SetHandled();

            if (e.StripTag)
            {
                e.PrefixText = "";
                e.SuffixText = "";
            }
            else
            {
                if (!Regex.IsMatch(e.Attributes.GetAttribute("default"), "^(left|right|center|justify)$", RegexOptions.Compiled))
                    e.AbortParse();
                e.PrefixText = "</p><p style=\"text-align: " + e.Attributes.GetAttribute("default") + "\">";
                e.SuffixText = "</p><p>";
            }
        }

        private static void BbcodeFloat(BbcodeEventArgs e)
        {
            if (e.Tag.Tag != "float")
                return;

            e.SetHandled();

            if (e.StripTag)
            {
                e.PrefixText = "";
                e.SuffixText = "";
            }
            else
            {
                if (!e.InList)
                {
                    if (!Regex.IsMatch(e.Attributes.GetAttribute("default"), "^(left|right)$", RegexOptions.Compiled))
                        e.AbortParse();
                    e.PrefixText = "</p><div style=\"float: " + e.Attributes.GetAttribute("default") + "\">";
                    e.SuffixText = "</div><p>";
                }
                else
                {
                    e.AbortParse();
                }
            }
        }

        private static void BbcodeUrl(BbcodeEventArgs e)
        {
            if (e.Tag.Tag != "url") return;

            e.SetHandled();

            if (e.StripTag)
            {
                if (e.Attributes.HasAttributes())
                {
                    e.PrefixText = "";
                    e.SuffixText = " : (" + e.Attributes.GetAttribute("default") + ")";
                }
                else
                {
                    e.PrefixText = "";
                    e.SuffixText = "";
                }
            }
            else
            {
                if (e.Attributes.HasAttributes())
                {
                    if (Regex.IsMatch(e.Attributes.GetAttribute("default"), "^([\\w]+?://[\\w\\#$%&~/.\\-;:=,?@\\[\\]+]*?)$", RegexOptions.Compiled))
                    {
                        e.PrefixText = "<a href=\"" + e.Attributes.GetAttribute("default") + "\">";
                    }
                    else if (Regex.IsMatch(e.Attributes.GetAttribute("default"), "^((www|ftp)\\.[\\w\\#$%&~/.\\-;:=,?@\\[\\]+]*?)$", RegexOptions.Compiled))
                    {
                        e.PrefixText = "<a href=\"http://" + e.Attributes.GetAttribute("default") + "\">";
                    }
                    else
                    {
                        e.AbortParse();
                    }
                    e.SuffixText = "</a>";
                }
                else
                {
                    if (Regex.IsMatch(e.Contents, "^([\\w]+?://[\\w\\#$%&~/.\\-;:=,?@\\[\\]+]*?)$", RegexOptions.Compiled))
                    {
                        e.PrefixText = "<a href=\"" + e.Contents + "\">";
                    }
                    else if (Regex.IsMatch(e.Contents, "^((www|ftp)\\.[\\w\\#$%&~/.\\-;:=,?@\\[\\]+]*?)$", RegexOptions.Compiled))
                    {
                        e.PrefixText = "<a href=\"http://" + e.Contents + "\">";
                    }
                    else
                    {
                        e.AbortParse();
                    }
                    e.SuffixText = "</a>";
                }
            }
        }

        private void BbcodeInternalUrl(BbcodeEventArgs e)
        {
            if (e.Tag.Tag != "iurl") return;

            e.SetHandled();

            if (e.StripTag)
            {
                if (e.Attributes.HasAttributes())
                {
                    e.PrefixText = "";
                    e.SuffixText = string.Format("(http://zinzam.com{0})", core.Uri.StripSid(e.Attributes.GetAttribute("default")));
                }
                else
                {
                    e.PrefixText = "(http://zinzam.com";
                    e.SuffixText = ")";
                }
            }
            else
            {
                if (e.Attributes.HasAttributes())
                {
                    if (e.Attributes.HasAttribute("sid") && e.Attributes.GetAttribute("sid").ToLower() == "true")
                    {
                        e.PrefixText = "<a href=\"" + core.Uri.AppendSid(e.Attributes.GetAttribute("default"), true) + "\">";
                    }
                    else
                    {
                        e.PrefixText = "<a href=\"" + core.Uri.AppendSid(e.Attributes.GetAttribute("default")) + "\">";
                    }
                    e.SuffixText = "</a>";
                }
                else
                {
                    e.PrefixText = "<a href=\"" + core.Uri.AppendSid(e.Contents) + "\">";
                    e.SuffixText = "</a>";
                }
            }
        }

        private static void BbcodeInline(BbcodeEventArgs e)
        {
            if (e.Tag.Tag != "inline") return;

            e.SetHandled();

            if (e.Owner == null)
            {
                e.AbortParse();
            }
            else
            {
                e.PrefixText = "<img alt=\"Bbcode image\" style=\"max-width: 100%;\" src=\"/" + HttpUtility.HtmlEncode(e.Owner.UserName) + "/images/_display/";
                e.SuffixText = "\" />";
            }
        }

        private static void BbcodeThumb(BbcodeEventArgs e)
        {
            if (e.Tag.Tag != "thumb") return;

            e.SetHandled();

            if (e.Owner == null)
            {
                e.AbortParse();
            }
            else
            {
                e.PrefixText = "<img alt=\"Bbcode image\" src=\"/" + HttpUtility.HtmlEncode(e.Owner.UserName) + "/images/_icon/";
                e.SuffixText = "\" />";
            }
        }

        private static void BbcodeImage(BbcodeEventArgs e)
        {
            if (e.Tag.Tag != "img") return;

            e.SetHandled();

            if (e.StripTag)
            {
                e.PrefixText = "";
                e.SuffixText = "";
            }
            else
            {
                if (!Regex.IsMatch(e.Contents, "^((http|ftp|https|ftps)://)([^ \\?&=\\#\\\"\\n\\r\\t<]*?(\\.(jpg|jpeg|gif|png)))$", RegexOptions.IgnoreCase)) e.AbortParse();
                if (TagAllowed(e.Tag.Tag, e.Options))
                {
                    e.PrefixText = "<img alt=\"Bbcode image\" src=\"";
                    e.SuffixText = "\" />";
                }
                else
                {
                    e.PrefixText = "<a href=\"" + e.Contents + "\"><strong>IMG</strong>: ";
                    e.SuffixText = "</a>";
                }
            }
        }

        private static void BbcodeYouTube(BbcodeEventArgs e)
        {
            if (e.Tag.Tag != "youtube") return;

            e.SetHandled();

            if (e.StripTag)
            {
                e.PrefixText = "";
                e.SuffixText = "";
            }
            else
            {
                string youTubeUrl = e.Contents;
                e.RemoveContents();

                if (youTubeUrl.ToLower().StartsWith("http://"))
                {
                    char[] splitChars = { '=', '?', '&' };
                    string[] argh = youTubeUrl.Split(splitChars);
                    if (argh.Length <= 2)
                    {
                        e.AbortParse();
                    }
                    for (int y = 0; y < argh.Length - 1; y++)
                    {
                        if (argh[y] == "v")
                        {
                            youTubeUrl = argh[y + 1];
                        }
                        else if (y == argh.Length - 2)
                        {
                            e.AbortParse();
                        }
                    }
                }

                if (TagAllowed(e.Tag.Tag, e.Options))
                {
                    youTubeUrl = "http://www.youtube.com/v/" + youTubeUrl;

                    e.PrefixText = "<object width=\"425\" height=\"350\"><param name=\"movie\" value=\"" + youTubeUrl + "\"></param><embed src=\"" + youTubeUrl + "\" type=\"application/x-shockwave-flash\" width=\"425\" height=\"350\"></embed></object>";
                    e.SuffixText = "";
                }
                else
                {
                    youTubeUrl = "http://www.youtube.com/watch?v=" + youTubeUrl;
                    e.PrefixText = "<a href=\"" + youTubeUrl + "\"><strong>YT:</strong> " + youTubeUrl;
                    e.SuffixText = "</a>";
                }
            }
        }

        private static void BbcodeLaTeX(BbcodeEventArgs e)
        {
            if (e.Tag.Tag != "latex") return;

            e.SetHandled();

            if (e.StripTag)
            {
                e.PrefixText = "";
                e.SuffixText = "";
            }
            else
            {
                string latexExpression = HttpUtility.UrlEncode(e.Contents).Replace("+", "%20");
                e.RemoveContents();

                e.PrefixText = "<img src=\"http://zinzam.com/mimetex.cgi?" + latexExpression + "\" alt=\"LaTeX Equation\"/>";
                e.SuffixText = "";
            }
        }

        private static void BbcodeFlash(BbcodeEventArgs e)
        {
            // TODO: flash bbcode
            if (e.Tag.Tag != "flash") return;

            e.SetHandled();

            if (e.StripTag)
            {
                e.PrefixText = "";
                e.SuffixText = "";
            }
            else
            {
                if (TagAllowed(e.Tag.Tag, e.Options))
                {
                }
                else
                {
                }
            }
        }

        private static void BbcodeSilverlight(BbcodeEventArgs e)
        {
            // TODO: silverlight bbcode
            if (e.Tag.Tag != "silverlight") return;

            e.SetHandled();

            if (e.StripTag)
            {
                e.PrefixText = "";
                e.SuffixText = "";
            }
            else
            {
                if (TagAllowed(e.Tag.Tag, e.Options))
                {
                }
                else
                {
                }
            }
        }

        private void BbcodeUser(BbcodeEventArgs e)
        {
            if (e.Tag.Tag != "user") return;

            e.SetHandled();

            if (e.StripTag)
            {
                string key = e.Contents;
                e.RemoveContents();

                long id = 0;


                if (key != "you")
                {
                    try
                    {
                        id = long.Parse(key);
                    }
                    catch
                    {
                        e.AbortParse();
                        return;
                    }
                }
                else
                {
                    id = core.LoggedInMemberId;
                }

                if (id > 0)
                {
                    core.LoadUserProfile(id);
                    User userUser = core.PrimitiveCache[id];

                    if (e.Attributes.HasAttribute("ownership") &&
                        e.Attributes.GetAttribute("ownership") == "true")
                    {
                        e.PrefixText = userUser.DisplayNameOwnership;
                        e.SuffixText = "";
                    }
                    else
                    {
                        e.PrefixText = userUser.DisplayName;
                        e.SuffixText = "";
                    }
                }
                else
                {
                    e.PrefixText = "Anonymous";
                    e.SuffixText = "";
                }
            }
            else
            {
                if (core != null)
                {
                    string key = e.Contents;
                    e.RemoveContents();

                    long id = 0;

                    if (key != "you")
                    {
                        try
                        {
                            id = long.Parse(key);
                        }
                        catch
                        {
                            e.AbortParse();
                            return;
                        }
                    }
                    else
                    {
                        id = core.LoggedInMemberId;
                    }

                    if (id > 0)
                    {
                        core.LoadUserProfile(id);
                        User userUser = core.PrimitiveCache[id];

                        if (e.Attributes.HasAttribute("ownership") &&
                            e.Attributes.GetAttribute("ownership") == "true")
                        {
                            e.PrefixText = string.Format("<a href=\"{1}\">{0}</a>",
                                userUser.DisplayNameOwnership, userUser.Uri);
                            e.SuffixText = "";
                        }
                        else
                        {
                            e.PrefixText = string.Format("<a href=\"{1}\">{0}</a>",
                                userUser.DisplayName, userUser.Uri);
                            e.SuffixText = "";
                        }
                    }
                    else
                    {
                        e.PrefixText = "Anonymous";
                        e.SuffixText = "";
                    }
                }
            }
        }

        public string Strip(string input)
        {
            return Parse(input, null, null, true);
        }

        public string Strip(string input, User viewer)
        {
            return Parse(input, viewer, null, true);
        }

        public string Strip(string input, User viewer, User postOwner)
        {
            return Parse(input, viewer, postOwner, true);
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
                if (input[i].Equals('&') && i + 6 < input.Length && input.Substring(i, 6).Equals("&quot;"))
                {
                    inQuote = !inQuote;
                    i += 5;
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
                    else if (!(!inQuote && input[i].Equals(' ')))
                    {
                        val += input[i].ToString();
                    }
                }
                if ((!inQuote && input[i].Equals(' ')) || i + 1 == length)
                {
                    if (param.Length == 0) param = "default";
                    Attributes.Add(param.ToLower(), val);
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

        internal bool HasAttribute(string key)
        {
            return Attributes.ContainsKey(key);
        }
    }
}
