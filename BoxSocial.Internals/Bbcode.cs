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
using System.Data;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using System.Web;
using System.Text.RegularExpressions;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

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

    public enum BbcodeParseMode : byte
    {
        Normal = 0x00,
        Flatten = 0x01,
        Tldr = 0x02,
        StripTags = 0x04,
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
            BbcodeHooks += new BbcodeHookHandler(BbcodeShare);
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
            BbcodeHooks += new BbcodeHookHandler(BbcodeTwitter);
            BbcodeHooks += new BbcodeHookHandler(BbcodeSoundcloud);
            BbcodeHooks += new BbcodeHookHandler(BbcodeInstagram);
            // TODO: flash
            //BbcodeHooks += new BbcodeHookHandler(BbcodeFlash);
            // TODO: silverlight
            BbcodeHooks += new BbcodeHookHandler(BbcodeUser);
            //BbcodeHooks += new BbcodeHookHandler(BbcodeBreak);

            styleList = new List<string>();
            styleList.Add("color");
            styleList.Add("size");
        }

        private sealed class BbcodeEventArgs
        {
            private Core core;
            private BbcodeTag tag;
            private BbcodeAttributes attributes;
            private BbcodeOptions options;
            private string prefixText;
            private string suffixText;
            private bool inList;
            private int quoteDepth;
            private int shareDepth;
            private bool handled;
            private bool abortParse;
            private bool noContents;
            private string contents;
            private BbcodeParseMode mode;
            private Primitive owner = null;

            public Core Core
            {
                get
                {
                    return core;
                }
            }

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

            public int QuoteDepth
            {
                get
                {
                    return quoteDepth;
                }
            }

            public int ShareDepth
            {
                get
                {
                    return shareDepth;
                }
            }

            public BbcodeParseMode Mode
            {
                get
                {
                    return mode;
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

            public Primitive Owner
            {
                get
                {
                    return owner;
                }
            }

            public BbcodeEventArgs(Core core, string contents, BbcodeTag tag, BbcodeOptions options, Primitive postOwner, bool inList, int quoteDepth, int shareDepth, BbcodeParseMode mode, ref string prefixText, ref string suffixText, ref bool handled, ref bool abortParse)
            {
                this.core = core;
                this.tag = tag;
                this.options = options;
                this.attributes = tag.GetAttributes();
                this.contents = contents;
                this.prefixText = prefixText;
                this.suffixText = suffixText;
                this.inList = inList;
                this.quoteDepth = quoteDepth;
                this.shareDepth = shareDepth;
                this.mode = mode;
                this.handled = handled;
                this.abortParse = abortParse;
                this.owner = postOwner;
            }

            public BbcodeEventArgs(Core core, string contents, BbcodeTag tag, BbcodeOptions options, User postOwner, bool inList, int quoteDepth, int shareDepth, BbcodeParseMode mode, ref string prefixText, ref string suffixText, ref bool handled, ref bool abortParse)
            {
                this.core = core;
                this.tag = tag;
                this.options = options;
                this.attributes = tag.GetAttributes();
                this.contents = contents;
                this.prefixText = prefixText;
                this.suffixText = suffixText;
                this.inList = inList;
                this.quoteDepth = quoteDepth;
                this.shareDepth = shareDepth;
                this.mode = mode;
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
            public bool TrimStart;

            public BbcodeTaglet(bool trimStart, int startIndex, int length, string renderText)
            {
                TrimStart = trimStart;
                StartIndex = startIndex;
                Length = length;
                RenderText = renderText;
            }

            public BbcodeTaglet(int startIndex, int length, string renderText)
            {
                TrimStart = false;
                StartIndex = startIndex;
                Length = length;
                RenderText = renderText;
            }

            public BbcodeTaglet(int startIndex, int length, string renderText, bool li)
            {
                TrimStart = false;
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

        public List<string> ExtractImages(string input, Primitive owner, bool forceThumbnail, bool firstOnly)
        {
            List<string> imageUris = new List<string>();
            MatchCollection matches = Regex.Matches(input, "\\[img\\](((http|ftp|https|ftps)://)([^ \\?&=\\#\\\"\\n\\r\\t<]*?(\\.(jpg|jpeg|gif|png))))\\[/img\\]", RegexOptions.IgnoreCase);

            foreach (Match match in matches)
            {
                string imageUri = match.Groups[0].Value;
                imageUris.Add(imageUri);

                if (firstOnly)
                {
                    return imageUris;
                }
            }

            if (owner != null)
            {
                matches = Regex.Matches(input, "\\[inline\\](([^ \\?&=\\#\\\"\\n\\r\\t<]*?(\\.(jpg|jpeg|gif|png))))\\[/inline\\]", RegexOptions.IgnoreCase);

                foreach (Match match in matches)
                {
                    string imageUri = null;
                    if (forceThumbnail)
                    {
                        imageUri = owner.UriStubAbsolute + "images/_tile/" + match.Groups[1].Value;
                    }
                    else
                    {
                        imageUri = owner.UriStubAbsolute + "images/_display/" + match.Groups[1].Value;
                    }
                    imageUris.Add(imageUri);

                    if (firstOnly)
                    {
                        return imageUris;
                    }
                }

                matches = Regex.Matches(input, "\\[thumb\\](([^ \\?&=\\#\\\"\\n\\r\\t<]*?(\\.(jpg|jpeg|gif|png))))\\[/thumb\\]", RegexOptions.IgnoreCase);

                foreach (Match match in matches)
                {
                    string imageUri = owner.UriStubAbsolute + "images/_tile/" + match.Groups[1].Value;
                    imageUris.Add(imageUri);

                    if (firstOnly)
                    {
                        return imageUris;
                    }
                }
            }

            return imageUris;
        }

        public string FromStatusCode(string input)
        {
            string output = input;

            // http://weblogs.asp.net/farazshahkhan/archive/2008/08/09/regex-to-find-url-within-text-and-make-them-as-link.aspx
            MatchCollection matches = Regex.Matches(input, "(?:^|\\s)((http(s)?://|ftp://|www\\.)([\\w+?\\.\\w+]+)([a-zA-Z0-9\\~\\!\\@\\#\\$\\%\\^\\&\\*\\(\\)_\\-\\=\\+\\\\\\/\\?\\.\\:\\;\\'\\,]*)?)", RegexOptions.IgnoreCase);

            int offset = 0;
            foreach (Match match in matches)
            {
                string domain = match.Groups[4].Value;
                string path = match.Groups[5].Value;
                if (domain.ToLower().EndsWith("youtube.com") && path.ToLower().StartsWith("/watch?v="))
                {
                    //output = output.Replace(match.Value, "\n[youtube]" + match.Groups[1].Value + "[/youtube]");
                    output = output.Insert(match.Groups[1].Index + offset, "\n[youtube]").Insert(match.Groups[1].Index + match.Groups[1].Length + offset + 10, "[/youtube]");
                    offset += 20;
                }

                if (domain.ToLower().EndsWith("twitter.com") && path.ToLower().Contains("/status/"))
                {
                    output = output.Insert(match.Groups[1].Index + offset, "\n[tweet]").Insert(match.Groups[1].Index + match.Groups[1].Length + offset + 8, "[/tweet]");
                    offset += 16;
                }

                if (domain.ToLower().EndsWith("soundcloud.com") && path.ToLower().Contains("/"))
                {
                    output = output.Insert(match.Groups[1].Index + offset, "\n[soundcloud]").Insert(match.Groups[1].Index + match.Groups[1].Length + offset + 13, "[/soundcloud]");
                    offset += 16;
                }

                if (domain.ToLower().EndsWith("instagram.com") && path.ToLower().Contains("/p/"))
                {
                    output = output.Insert(match.Groups[1].Index + offset, "\n[instagram]").Insert(match.Groups[1].Index + match.Groups[1].Length + offset + 12, "[/instagram]");
                    offset += 16;
                }
            }

            /* Match hash tags to BBcode for parsing */
            matches = Regex.Matches(input, "(?:^|\\s)((\\#)([a-z0-9]+)?)", RegexOptions.IgnoreCase);
            offset = 0;
            foreach (Match match in matches)
            {
                string hashtag = match.Groups[1].Value;
                if (!string.IsNullOrEmpty(hashtag))
                {
                    string searchUri = core.Hyperlink.AppendAbsoluteSid("/search?q=" + HttpUtility.UrlEncode(hashtag));
                    string startTag = "[url=\"" + searchUri + "\"]";
                    string endTag = "[/url]";

                    output = output.Insert(match.Groups[1].Index + offset, startTag).Insert(match.Groups[1].Index + match.Groups[1].Length + offset + startTag.Length, endTag);

                    offset += (startTag + endTag).Length;
                }
            }

            /* Match user links to BBcode for parsing */
            matches = Regex.Matches(input, "(?:^|\\s)((\\@)([a-z0-9]+)?)", RegexOptions.IgnoreCase);
            offset = 0;
            List<string> usernames = new List<string>();
            foreach (Match match in matches)
            {
                string username = match.Groups[3].Value;
                usernames.Add(username);
            }

            Dictionary<string, long> userIds = core.LoadUserProfiles(usernames);

            foreach (Match match in matches)
            {
                string username = match.Groups[3].Value;

                if (userIds.ContainsKey(username.ToLower()))
                {
                    string startTag = "[user]" + userIds[username.ToLower()].ToString();
                    string endTag = "[/user]";

                    output = output.Remove(match.Groups[1].Index + offset, match.Groups[1].Value.Length).Insert(match.Groups[1].Index + offset, startTag + endTag);

                    offset += ((startTag + endTag).Length - match.Groups[1].Value.Length);
                }
            }

            return output.Trim(new char[] { '\n' });
        }

        public string ParseUrls(string input)
        {
            //MatchCollection matches = 

            input = Regex.Replace(input, "(^|\\s)((http(s)?://|ftp://|www\\.)([\\w+?\\.\\w+]+)([a-zA-Z0-9\\~\\!\\@\\#\\$\\%\\^\\&\\*\\(\\)_\\-\\=\\+\\\\\\/\\?\\.\\:\\;\\'\\,]*)?)", "$1[url]$2[/url]", RegexOptions.IgnoreCase);

            /*int offset = 0;
            foreach (Match match in matches)
            {
                output = output.Insert(match.Groups[1].Index + offset, "[url]").Insert(match.Groups[1].Index + match.Groups[1].Length + offset + 5,"[/url]");
                offset += 11;
            }*/

            return input;
        }

        public string Parse(string input)
        {
            return Parse(input, null);
        }

        public string Parse(string input, bool appendP, string id, string styleClass)
        {
            return Parse(input, null, appendP, id, styleClass);
        }

        public string Parse(string input, User viewer)
        {
            return Parse(input, viewer, null);
        }

        public string Parse(string input, User viewer, bool appendP, string id, string styleClass)
        {
            return Parse(input, viewer, null, appendP, id, styleClass);
        }

        public string Parse(string input, User viewer, Primitive postOwner)
        {
            return Parse(input, viewer, postOwner, false, string.Empty, string.Empty, BbcodeParseMode.Normal);
        }

        public string Parse(string input, User viewer, Primitive postOwner, bool appendP, string id, string styleClass)
        {
            return Parse(input, viewer, postOwner, appendP, id, styleClass, BbcodeParseMode.Normal);
        }

        private string Parse(string input, User viewer, Primitive postOwner, bool appendP, string id, string styleClass, BbcodeParseMode mode)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            if (mode == BbcodeParseMode.Normal)
            {
                // Convert all URLs that aren't BB Coded into BB Code
                input = ParseUrls(input);
            }

            StringBuilder debugLog = new StringBuilder();

            BbcodeOptions options = BbcodeOptions.ShowImages | BbcodeOptions.ShowFlash | BbcodeOptions.ShowVideo | BbcodeOptions.ShowAudio;

            if (viewer != null)
            {
                options = viewer.UserInfo.GetUserBbcodeOptions;
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
            string Tag = string.Empty;
            string attr = string.Empty;
            int startIndex = 0;
            int strLength = input.Length;
            int end = strLength;
            int quoteDepth = 0;
            int shareStart = 0;
            int shareDepth = 0;

            StringBuilder newOutput = new StringBuilder();
            int lastEndIndex = 0;

            if (mode == BbcodeParseMode.Normal && appendP)
            {
                newOutput.Append("<p");
                if (!string.IsNullOrEmpty(id))
                {
                    newOutput.Append(" id=\"");
                    newOutput.Append(id);
                    newOutput.Append("\"");
                }

                if (!string.IsNullOrEmpty(styleClass))
                {
                    newOutput.Append(" class=\"");
                    newOutput.Append(styleClass);
                    newOutput.Append("\"");
                }
                newOutput.Append(">");
            }

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

                            if (Tag.Equals("code"))
                            {
                                inCode = true;
                            }

                            if (Tag.Equals("quote"))
                            {
                                quoteDepth++;
                            }

                            if (Tag.Equals("share"))
                            {
                                quoteDepth++;
                                shareDepth++;
                            }
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
                                    string insertStart = string.Empty;
                                    string insertEnd = string.Empty;
                                    bool listItem = false;
                                    bool trimStart = false;

                                    /*
                                     * A couple of special cases
                                     */
                                    if (Tag.Equals("code"))
                                    {
                                        inCode = false;
                                    }

                                    if (Tag.Equals("list"))
                                    {
                                        inList--;
                                    }

                                    if (Tag.Equals("*"))
                                    {
                                        listItem = true;
                                        endOffset = 0;
                                        endTagLength = 0;
                                    }

                                    if (Tag.Equals("quote"))
                                    {
                                        quoteDepth--;
                                    }

                                    if (Tag.Equals("share"))
                                    {
                                        quoteDepth--;
                                    }

                                    bool handled = false;

                                    int tempIndex = tempTag.indexStart + tempTag.StartLength;
                                    string contents = input.Substring(tempIndex, i - tempIndex - tempTag.EndLength + 1);

                                    BbcodeEventArgs eventArgs = new BbcodeEventArgs(core, contents, tempTag, options, postOwner, (inList > 0), quoteDepth, shareDepth, mode, ref insertStart, ref insertEnd, ref handled, ref abortParse);
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

                                    /* We force trimming the share to the first shared to behave properly */
                                    if (mode == BbcodeParseMode.Tldr && Tag.Equals("share") && quoteDepth == 1)
                                    {
                                        trimStart = true;
                                        /* Do not delete the tag as we are in Tldr mode, but do remove everything before it */
                                        taglets.Add(new BbcodeTaglet(trimStart, tempTag.indexStart, 0, insertStart));
                                    }

                                    if (!abortParse)
                                    {
                                        /* two pass method */
                                        taglets.Add(new BbcodeTaglet(trimStart, tempTag.indexStart, startTagLength, insertStart));
                                        taglets.Add(new BbcodeTaglet(startIndex - endOffset, endTagLength, insertEnd, listItem));
                                    }
                                }
                            }
                        }
                    }
                    inTag = false;
                    endTag = false;
                    inQuote = false;
                    Tag = string.Empty;
                    attr = string.Empty;
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
                                    Tag = string.Empty;
                                    attr = string.Empty;
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
                                    Tag = string.Empty;
                                    attr = string.Empty;
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
                    Tag = string.Empty;
                    attr = string.Empty;
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
                if ((!taglet.TrimStart) && taglet.StartIndex - lastEndIndex > 0)
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

            if (mode == BbcodeParseMode.Tldr)
            {
                if (shareStart > 0)
                {
                    newOutput.Remove(0, shareStart);
                }
            }

            if (mode == BbcodeParseMode.Normal && appendP)
            {
                newOutput.Append("</p>");
            }

            input = newOutput.ToString();
            double time = ((double)(DateTime.Now.Ticks - start)) / 10000000;

            if (mode == BbcodeParseMode.Normal)
            {
                input = input.Replace("\r\n", "\n");
                input = input.Replace("\n", "<br />");
                input = input.Replace("<p></p>", string.Empty);
                input = input.Replace("<p><br /><br />", "<p>");
                input = input.Replace("<p><br />", "<p>");
                input = input.Replace("<br /></p>", "</p>");
                input = input.Replace("<br /><li>", "<li>");
                input = input.Replace("<br /></ul>", "</ul>");
                input = input.Replace("<br /></ol>", "</ol>");
                input = input.Replace("<blockquote></blockquote>", "<blockquote>&nbsp;</blockquote>");
                //input = Regex.Replace(input, @"\<p\>(\s+)\<\/p\>", string.Empty, RegexOptions.Compiled);
                input = input.Replace("<p> </p>", string.Empty);
                input = input.Replace("<p>\n</p>", string.Empty);
                input = input.Replace("<p>\r\n</p>", string.Empty);
            }
            
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

            switch (e.Mode)
            {
                case BbcodeParseMode.Tldr:
                    // Preserve
                    if (e.QuoteDepth > 1)
                    {
                        e.PrefixText = string.Empty;
                        e.SuffixText = string.Empty;
                        e.RemoveContents();
                    }
                    else
                    {
                        e.AbortParse();
                    }
                    break;
                case BbcodeParseMode.StripTags:
                    e.PrefixText = string.Empty;
                    e.SuffixText = string.Empty;
                    break;
                case BbcodeParseMode.Flatten:
                    if (e.Attributes.HasAttributes())
                    {
                        e.PrefixText = "\n--- Quote: " + Parse(e.Attributes.GetAttribute("default"), null, e.Owner, false, null, null, e.Mode) + " wrote: ---\n";
                    }
                    else
                    {
                        e.PrefixText = "\n--- Quote: ---\n";
                    }
                    e.SuffixText = "\n---------------------\n";
                    break;
                case BbcodeParseMode.Normal:
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
                    break;
            }
        }

        private void BbcodeShare(BbcodeEventArgs e)
        {
            if (e.Tag.Tag != "share") return;

            e.SetHandled();

            switch (e.Mode)
            {
                case BbcodeParseMode.Tldr:
                    // Preserve
                    if (e.ShareDepth == 1)
                    {
                        e.AbortParse();
                    }
                    else
                    {
                        if (e.QuoteDepth == 1)
                        {
                            e.AbortParse();
                        }
                        else if (e.QuoteDepth > 1)
                        {
                            e.PrefixText = string.Empty;
                            e.SuffixText = string.Empty;
                            e.RemoveContents();
                        }
                        else
                        {
                            e.PrefixText = string.Empty;
                            e.SuffixText = string.Empty;
                        }
                    }
                    break;
                case BbcodeParseMode.StripTags:
                    e.PrefixText = string.Empty;
                    e.SuffixText = string.Empty;
                    break;
                case BbcodeParseMode.Flatten:
                    if (e.Attributes.HasAttributes())
                    {
                        e.PrefixText = "\n--- " + Parse(e.Attributes.GetAttribute("default"), null, e.Owner, false, null, null, e.Mode) + " originally shared: ---\n";
                    }
                    else
                    {
                        e.PrefixText = "\n--- Shared: ---\n";
                    }
                    e.SuffixText = "\n---------------------\n";
                    break;
                case BbcodeParseMode.Normal:
                    if (e.Attributes.HasAttributes())
                    {
                        if (!e.InList)
                        {
                            e.PrefixText = "</p><p><strong>" + Parse(e.Attributes.GetAttribute("default")) + " originally shared:</strong></p><blockquote><p>";
                        }
                        else
                        {
                            e.PrefixText = "<p><strong>" + Parse(e.Attributes.GetAttribute("default")) + " originally shared:</strong></p><blockquote>";
                        }
                    }
                    else
                    {
                        if (!e.InList)
                        {
                            e.PrefixText = "</p><blockquote><p>";
                        }
                        else
                        {
                            e.PrefixText = "<blockquote>";
                        }
                    }
                    e.SuffixText = "</p></blockquote><p>";
                    break;
            }
        }

        private static void BbcodeBold(BbcodeEventArgs e)
        {
            if (e.Tag.Tag != "b") return;

            e.SetHandled();

            switch (e.Mode)
            {
                case BbcodeParseMode.Tldr:
                    // Preserve
                    e.AbortParse();
                    break;
                case BbcodeParseMode.StripTags:
                    e.PrefixText = string.Empty;
                    e.SuffixText = string.Empty;
                    break;
                case BbcodeParseMode.Flatten:
                    e.PrefixText = "*";
                    e.SuffixText = "*";
                    break;
                case BbcodeParseMode.Normal:
                    e.PrefixText = "<strong>";
                    e.SuffixText = "</strong>";
                    break;
            }
        }

        private static void BbcodeItalic(BbcodeEventArgs e)
        {
            if (e.Tag.Tag != "i") return;

            e.SetHandled();

            switch (e.Mode)
            {
                case BbcodeParseMode.Tldr:
                    // Preserve
                    e.AbortParse();
                    break;
                case BbcodeParseMode.StripTags:
                    e.PrefixText = string.Empty;
                    e.SuffixText = string.Empty;
                    break;
                case BbcodeParseMode.Flatten:
                    e.PrefixText = "_";
                    e.SuffixText = "_";
                    break;
                case BbcodeParseMode.Normal:
                    e.PrefixText = "<em>";
                    e.SuffixText = "</em>";
                    break;
            }
        }

        private static void BbcodeUnderline(BbcodeEventArgs e)
        {
            if (e.Tag.Tag != "u") return;

            e.SetHandled();

            switch (e.Mode)
            {
                case BbcodeParseMode.Tldr:
                    // Preserve
                    e.AbortParse();
                    break;
                case BbcodeParseMode.StripTags:
                case BbcodeParseMode.Flatten:
                    e.PrefixText = string.Empty;
                    e.SuffixText = string.Empty;
                    break;
                case BbcodeParseMode.Normal:
                    e.PrefixText = "<span style=\"text-decoration: underline;\">";
                    e.SuffixText = "</span>";
                    break;
            }
        }

        private static void BbcodeStrikeout(BbcodeEventArgs e)
        {
            if (e.Tag.Tag != "s") return;

            e.SetHandled();

            switch (e.Mode)
            {
                case BbcodeParseMode.Tldr:
                    // Preserve
                    e.AbortParse();
                    break;
                case BbcodeParseMode.StripTags:
                    e.PrefixText = string.Empty;
                    e.SuffixText = string.Empty;
                    break;
                case BbcodeParseMode.Flatten:
                    e.PrefixText = "--";
                    e.SuffixText = "--";
                    break;
                case BbcodeParseMode.Normal:
                    e.PrefixText = "<span style=\"text-decoration: line-through;\">";
                    e.SuffixText = "</span>";
                    break;
            }
        }

        private static void BbcodeCode(BbcodeEventArgs e)
        {
            if (e.Tag.Tag != "code") return;

            e.SetHandled();

            switch (e.Mode)
            {
                case BbcodeParseMode.Tldr:
                    // Remove code blocks
                    e.PrefixText = string.Empty;
                    e.SuffixText = string.Empty;
                    e.RemoveContents();
                    break;
                case BbcodeParseMode.StripTags:
                    e.PrefixText = string.Empty;
                    e.SuffixText = string.Empty;
                    break;
                case BbcodeParseMode.Flatten:
                    e.PrefixText = "--- Code ---";
                    e.SuffixText = "------";
                    break;
                case BbcodeParseMode.Normal:
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
                    break;
            }
        }

        private static void BbcodeList(BbcodeEventArgs e)
        {
            if (e.Tag.Tag != "list") return;

            e.SetHandled();

            switch (e.Mode)
            {
                case BbcodeParseMode.Tldr:
                    // Preserve
                    e.AbortParse();
                    break;
                case BbcodeParseMode.StripTags:
                    e.PrefixText = string.Empty;
                    e.SuffixText = string.Empty;
                    break;
                case BbcodeParseMode.Flatten:
                    e.PrefixText = string.Empty;
                    e.SuffixText = string.Empty;
                    break;
                case BbcodeParseMode.Normal:
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
                    break;
            }
        }

        private static void BbcodeListItem(BbcodeEventArgs e)
        {
            if (e.Tag.Tag != "*") return;

            e.SetHandled();

            switch (e.Mode)
            {
                case BbcodeParseMode.Tldr:
                    // Preserve
                    e.AbortParse();
                    break;
                case BbcodeParseMode.StripTags:
                    e.PrefixText = string.Empty;
                    e.SuffixText = string.Empty;
                    break;
                case BbcodeParseMode.Flatten:
                    e.PrefixText = " *";
                    e.SuffixText = string.Empty;
                    break;
                case BbcodeParseMode.Normal:
                    e.PrefixText = "<li>";
                    e.SuffixText = "</li>";
                    break;
            }
        }

        private static void BbcodeColour(BbcodeEventArgs e)
        {
            if (e.Tag.Tag != "color") return;

            e.SetHandled();

            switch (e.Mode)
            {
                case BbcodeParseMode.Tldr:
                    // Preserve
                    e.AbortParse();
                    break;
                case BbcodeParseMode.StripTags:
                case BbcodeParseMode.Flatten:
                    e.PrefixText = string.Empty;
                    e.SuffixText = string.Empty;
                    break;
                case BbcodeParseMode.Normal:
                    try { System.Drawing.ColorTranslator.FromHtml(e.Attributes.GetAttribute("default")); }
                    catch { e.AbortParse(); }
                    e.PrefixText = "<span style=\"color: " + e.Attributes.GetAttribute("default") + "\">";
                    e.SuffixText = "</span>";
                    break;
            }
        }

        private static void BbcodeSize(BbcodeEventArgs e)
        {
            if (e.Tag.Tag != "size") return;

            e.SetHandled();

            switch (e.Mode)
            {
                case BbcodeParseMode.Tldr:
                    // Preserve
                    e.AbortParse();
                    break;
                case BbcodeParseMode.StripTags:
                case BbcodeParseMode.Flatten:
                    e.PrefixText = string.Empty;
                    e.SuffixText = string.Empty;
                    break;
                case BbcodeParseMode.Normal:
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
                    break;
            }
        }

        private static void BbcodeH1(BbcodeEventArgs e)
        {
            if (e.Tag.Tag != "h1") return;

            e.SetHandled();

            switch (e.Mode)
            {
                case BbcodeParseMode.Tldr:
                    // Preserve
                    e.AbortParse();
                    break;
                case BbcodeParseMode.StripTags:
                case BbcodeParseMode.Flatten:
                    e.PrefixText = string.Empty;
                    e.SuffixText = string.Empty;
                    break;
                case BbcodeParseMode.Normal:
                    e.PrefixText = "</p><h2>";
                    e.SuffixText = "</h2><p>";
                    break;
            }
        }

        private static void BbcodeH2(BbcodeEventArgs e)
        {
            if (e.Tag.Tag != "h2") return;

            e.SetHandled();

            switch (e.Mode)
            {
                case BbcodeParseMode.Tldr:
                    // Preserve
                    e.AbortParse();
                    break;
                case BbcodeParseMode.StripTags:
                case BbcodeParseMode.Flatten:
                    e.PrefixText = string.Empty;
                    e.SuffixText = string.Empty;
                    break;
                case BbcodeParseMode.Normal:
                    e.PrefixText = "</p><h3>";
                    e.SuffixText = "</h3><p>";
                    break;
            }
        }

        private static void BbcodeH3(BbcodeEventArgs e)
        {
            if (e.Tag.Tag != "h3") return;

            e.SetHandled();

            switch (e.Mode)
            {
                case BbcodeParseMode.Tldr:
                    // Preserve
                    e.AbortParse();
                    break;
                case BbcodeParseMode.StripTags:
                case BbcodeParseMode.Flatten:
                    e.PrefixText = string.Empty;
                    e.SuffixText = string.Empty;
                    break;
                case BbcodeParseMode.Normal:
                    e.PrefixText = "</p><h4>";
                    e.SuffixText = "</h4><p>";
                    break;
            }
        }

        private static void BbcodeAlign(BbcodeEventArgs e)
        {
            if (e.Tag.Tag != "align") return;

            e.SetHandled();

            switch (e.Mode)
            {
                case BbcodeParseMode.Tldr:
                    // Preserve
                    e.AbortParse();
                    break;
                case BbcodeParseMode.StripTags:
                case BbcodeParseMode.Flatten:
                    e.PrefixText = string.Empty;
                    e.SuffixText = string.Empty;
                    break;
                case BbcodeParseMode.Normal:
                    if (!Regex.IsMatch(e.Attributes.GetAttribute("default"), "^(left|right|center|justify)$", RegexOptions.Compiled))
                        e.AbortParse();
                    e.PrefixText = "</p><p style=\"text-align: " + e.Attributes.GetAttribute("default") + "\">";
                    e.SuffixText = "</p><p>";
                    break;
            }
        }

        private static void BbcodeFloat(BbcodeEventArgs e)
        {
            if (e.Tag.Tag != "float")
                return;

            e.SetHandled();

            switch (e.Mode)
            {
                case BbcodeParseMode.Tldr:
                    // Preserve
                    e.AbortParse();
                    break;
                case BbcodeParseMode.StripTags:
                case BbcodeParseMode.Flatten:
                    e.PrefixText = string.Empty;
                    e.SuffixText = string.Empty;
                    break;
                case BbcodeParseMode.Normal:
                    if (!e.InList)
                    {
                        if (!Regex.IsMatch(e.Attributes.GetAttribute("default"), "^(left|right)$", RegexOptions.Compiled))
                            e.AbortParse();
                        string styles = string.Empty;
                        styles = "float: " + e.Attributes.GetAttribute("default") + "; margin-right: 10px";

                        if (e.Attributes.HasAttribute("width"))
                        {
                            styles += "; width: " + e.Attributes.GetAttribute("width") + "px";
                        }

                        if (e.Attributes.HasAttribute("height"))
                        {
                            styles += "; height: " + e.Attributes.GetAttribute("height") + "px";
                        }

                        e.PrefixText = "</p><div style=\"" + styles + "\">";
                        e.SuffixText = "</div><p>";
                    }
                    else
                    {
                        e.AbortParse();
                    }
                    break;
            }
        }

        private static void BbcodeUrl(BbcodeEventArgs e)
        {
            if (e.Tag.Tag != "url") return;

            e.SetHandled();

            switch (e.Mode)
            {
                case BbcodeParseMode.Tldr:
                    // Preserve
                    e.AbortParse();
                    break;
                case BbcodeParseMode.StripTags:
                    e.PrefixText = string.Empty;
                    e.SuffixText = string.Empty;
                    break;
                case BbcodeParseMode.Flatten:
                    if (e.Attributes.HasAttributes())
                    {
                        e.PrefixText = string.Empty;
                        e.SuffixText = " : (" + e.Attributes.GetAttribute("default") + ")";
                    }
                    else
                    {
                        e.PrefixText = string.Empty;
                        e.SuffixText = string.Empty;
                    }
                    break;
                case BbcodeParseMode.Normal:
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
                    break;
            }
        }

        private void BbcodeInternalUrl(BbcodeEventArgs e)
        {
            if (e.Tag.Tag != "iurl") return;

            e.SetHandled();

            switch (e.Mode)
            {
                case BbcodeParseMode.Tldr:
                    // Preserve
                    e.AbortParse();
                    break;
                case BbcodeParseMode.StripTags:
                    e.PrefixText = string.Empty;
                    e.SuffixText = string.Empty;
                    break;
                case BbcodeParseMode.Flatten:
                    if (e.Attributes.HasAttributes())
                    {
                        e.PrefixText = string.Empty;
                        e.SuffixText = string.Format("(http://" + Hyperlink.Domain + "{0})", core.Hyperlink.StripSid(e.Attributes.GetAttribute("default")));
                    }
                    else
                    {
                        e.PrefixText = "(http://" + Hyperlink.Domain;
                        e.SuffixText = ")";
                    }
                    break;
                case BbcodeParseMode.Normal:
                    if (e.Attributes.HasAttributes())
                    {
                        if (e.Attributes.HasAttribute("sid") && e.Attributes.GetAttribute("sid").ToLower() == "true")
                        {
                            e.PrefixText = "<a href=\"" + core.Hyperlink.AppendSid(e.Attributes.GetAttribute("default"), true) + "\">";
                        }
                        else
                        {
                            e.PrefixText = "<a href=\"" + core.Hyperlink.AppendSid(e.Attributes.GetAttribute("default")) + "\">";
                        }
                        e.SuffixText = "</a>";
                    }
                    else
                    {
                        e.PrefixText = "<a href=\"" + core.Hyperlink.AppendSid(e.Contents) + "\">";
                        e.SuffixText = "</a>";
                    }
                    break;
            }
        }

        private static void BbcodeInline(BbcodeEventArgs e)
        {
            if (e.Tag.Tag != "inline") return;

            e.SetHandled();

            switch (e.Mode)
            {
                case BbcodeParseMode.Tldr:
                    // Preserve
                    e.AbortParse();
                    break;
                case BbcodeParseMode.StripTags:
                    e.PrefixText = string.Empty;
                    e.PrefixText = string.Empty;
                    e.RemoveContents();
                    break;
                case BbcodeParseMode.Flatten:
                    e.PrefixText = string.Empty;
                    e.PrefixText = string.Empty;
                    break;
                case BbcodeParseMode.Normal:
                    if (e.Owner == null)
                    {
                        e.AbortParse();
                    }
                    else
                    {
                        if (Regex.IsMatch(e.Contents, "^((http|ftp|https|ftps)://)([^ \\?&=\\#\\\"\\n\\r\\t<]*?(\\.(jpg|jpeg|gif|png)))$", RegexOptions.IgnoreCase))
                        {
                            e.PrefixText = "<img alt=\"Bbcode image\" src=\"";
                            e.SuffixText = "\" />";
                        }
                        else
                        {
                            if (e.Owner == null)
                            {
                                e.AbortParse();
                            }
                            else
                            {
                                int width = 0;
                                int height = 0;

                                if (e.Attributes.HasAttribute("width"))
                                {
                                    int.TryParse(e.Attributes.GetAttribute("width"), out width);
                                }

                                if (e.Attributes.HasAttribute("height"))
                                {
                                    int.TryParse(e.Attributes.GetAttribute("height"), out height);
                                }

                                if (e.Core.Settings.UseCdn && e.Attributes.HasAttribute("cdn-object") && (width > 640 || height > 640))
                                {
                                    e.PrefixText = "<img alt=\"Bbcode image\" style=\"max-width: 100%;\" src=\"http://" + HttpUtility.HtmlEncode(e.Core.Settings.CdnDisplayBucketDomain) + "/" + e.Attributes.GetAttribute("cdn-object") + "\" data-at2x=\"" + HttpUtility.HtmlEncode(e.Core.Settings.CdnFullBucketDomain) + "/" + e.Attributes.GetAttribute("cdn-object");
                                    e.SuffixText = "\" />";
                                    e.RemoveContents();
                                }
                                else
                                {
                                    e.PrefixText = "<img alt=\"Bbcode image\" style=\"max-width: 100%;\" src=\"" + HttpUtility.HtmlEncode(e.Owner.UriStubAbsolute) + "/images/_display/" + e.Contents + "\" data-at2x=\"" + HttpUtility.HtmlEncode(e.Owner.UriStubAbsolute) + "/images/_full/";
                                    e.SuffixText = "\" />";
                                }
                            }
                        }
                    }
                    break;
            }
        }

        private static void BbcodeThumb(BbcodeEventArgs e)
        {
            if (e.Tag.Tag != "thumb") return;

            e.SetHandled();

            switch (e.Mode)
            {
                case BbcodeParseMode.Tldr:
                    // Preserve
                    e.AbortParse();
                    break;
                case BbcodeParseMode.StripTags:
                    e.PrefixText = string.Empty;
                    e.PrefixText = string.Empty;
                    e.RemoveContents();
                    break;
                case BbcodeParseMode.Flatten:
                    e.PrefixText = string.Empty;
                    e.PrefixText = string.Empty;
                    break;
                case BbcodeParseMode.Normal:
                    if (Regex.IsMatch(e.Contents, "^((http|ftp|https|ftps)://)([^ \\?&=\\#\\\"\\n\\r\\t<]*?(\\.(jpg|jpeg|gif|png)))$", RegexOptions.IgnoreCase))
                    {
                        e.PrefixText = "<img alt=\"Bbcode image\" class=\"bbcode-thumb\" style=\"max-width: 100px; max-height: 100px;\" src=\"";
                        e.SuffixText = "\" />";
                    }
                    else
                    {
                        if (e.Owner == null)
                        {
                            e.AbortParse();
                        }
                        else
                        {
                            if (e.Core.Settings.UseCdn && e.Attributes.HasAttribute("cdn-object"))
                            {
                                e.PrefixText = "<img alt=\"Bbcode image\" class=\"bbcode-thumb\" src=\"http://" + HttpUtility.HtmlEncode(e.Core.Settings.CdnTileBucketDomain) + "/" + e.Attributes.GetAttribute("cdn-object") + "\" data-at2x=\"" + HttpUtility.HtmlEncode(e.Core.Settings.CdnSquareBucketDomain) + "/" + e.Attributes.GetAttribute("cdn-object");
                                e.SuffixText = "\" />";
                                e.RemoveContents();
                            }
                            else
                            {
                                e.PrefixText = "<img alt=\"Bbcode image\" class=\"bbcode-thumb\" src=\"" + HttpUtility.HtmlEncode(e.Owner.UriStubAbsolute) + "images/_tile/" + e.Contents + "\" data-at2x=\"" + HttpUtility.HtmlEncode(e.Owner.UriStubAbsolute) + "images/_square/";
                                e.SuffixText = "\" />";
                            }
                        }
                    }
                    break;
            }
        }

        private static void BbcodeImage(BbcodeEventArgs e)
        {
            if (e.Tag.Tag != "img") return;

            e.SetHandled();

            switch (e.Mode)
            {
                case BbcodeParseMode.Tldr:
                    // Preserve
                    e.AbortParse();
                    break;
                case BbcodeParseMode.StripTags:
                    e.PrefixText = string.Empty;
                    e.PrefixText = string.Empty;
                    e.RemoveContents();
                    break;
                case BbcodeParseMode.Flatten:
                    e.PrefixText = string.Empty;
                    e.PrefixText = string.Empty;
                    break;
                case BbcodeParseMode.Normal:
                    if (!Regex.IsMatch(e.Contents, "^((http|ftp|https|ftps)://)([^ \\?&=\\#\\\"\\n\\r\\t<]*?(\\.(jpg|jpeg|gif|png)))$", RegexOptions.IgnoreCase)) e.AbortParse();
                    if (TagAllowed(e.Tag.Tag, e.Options))
                    {
                        e.PrefixText = "<img alt=\"Bbcode image\" style=\"max-width: 100%;\" src=\"";
                        e.SuffixText = "\" />";
                    }
                    else
                    {
                        e.PrefixText = "<a href=\"" + e.Contents + "\"><strong>IMG</strong>: ";
                        e.SuffixText = "</a>";
                    }
                    break;
            }
        }

        private static void BbcodeYouTube(BbcodeEventArgs e)
        {
            if (e.Tag.Tag != "youtube") return;

            e.SetHandled();

            switch (e.Mode)
            {
                case BbcodeParseMode.Tldr:
                    // Preserve
                    e.AbortParse();
                    break;
                case BbcodeParseMode.StripTags:
                case BbcodeParseMode.Flatten:
                    e.PrefixText = string.Empty;
                    e.PrefixText = string.Empty;
                    break;
                case BbcodeParseMode.Normal:
                    string youTubeUrl = e.Contents;
                    e.RemoveContents();

                    if (youTubeUrl.ToLower().StartsWith("http://") || youTubeUrl.ToLower().StartsWith("https://"))
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
                        youTubeUrl = "http://www.youtube.com/embed/" + youTubeUrl;

                        // Old YouTube Flash Embed Code
                        //e.PrefixText = "<object width=\"425\" height=\"350\"><param name=\"movie\" value=\"" + youTubeUrl + "\"></param><embed src=\"" + youTubeUrl + "\" type=\"application/x-shockwave-flash\" width=\"425\" height=\"350\"></embed></object>";
                        // New YouTube Embed Code

                        if (e.Core.IsMobile)
                        {
                            e.PrefixText = "<iframe class=\"youtube-player\" type=\"text/html\" width=\"300\" height=\"194\" src=\"" + youTubeUrl + "\" frameborder=\"0\"></iframe>";
                        }
                        else
                        {
                            e.PrefixText = "<iframe class=\"youtube-player\" type=\"text/html\" width=\"560\" height=\"340\" src=\"" + youTubeUrl + "\" frameborder=\"0\"></iframe>";
                        }
                        e.SuffixText = string.Empty;
                    }
                    else
                    {
                        youTubeUrl = "http://www.youtube.com/watch?v=" + youTubeUrl;
                        e.PrefixText = "<a href=\"" + youTubeUrl + "\"><strong>YT:</strong> " + youTubeUrl;
                        e.SuffixText = "</a>";
                    }
                    break;
            }
        }

        private static void BbcodeInstagram(BbcodeEventArgs e)
        {
            if (e.Tag.Tag != "instagram") return;

            e.SetHandled();

            switch (e.Mode)
            {
                case BbcodeParseMode.Tldr:
                    // Preserve
                    e.AbortParse();
                    break;
                case BbcodeParseMode.StripTags:
                case BbcodeParseMode.Flatten:
                    e.PrefixText = string.Empty;
                    e.PrefixText = string.Empty;
                    break;
                case BbcodeParseMode.Normal:
                    string instagramUrl = e.Contents;
                    string instagramId = instagramUrl;
                    //if (!Regex.IsMatch(e.Contents, "^((http|ftp|https|ftps)://)([^ \\?&=\\#\\\"\\n\\r\\t<]*?(\\.(jpg|jpeg|gif|png)))$", RegexOptions.IgnoreCase)) e.AbortParse();

                    e.RemoveContents();

                    if (TagAllowed(e.Tag.Tag, e.Options))
                    {
                        ContentPreviewCache preview = ContentPreviewCache.GetPreview(e.Core, "instagram.com", instagramId, e.Core.Prose.Language);
                        string instagram = string.Empty;

                        if (preview != null)
                        {
                            instagram = preview.Body;
                        }
                        else
                        {
                            string apiUri = "http://api.instagram.com/oembed?url=" + HttpUtility.UrlEncode(instagramId) + "&maxwidth=550";
                            WebClient wc = new WebClient();
                            string response = wc.DownloadString(apiUri);

                            Dictionary<string, string> strings = (Dictionary<string, string>)JsonConvert.DeserializeObject(response, typeof(Dictionary<string, string>));

                            if (strings.ContainsKey("url") && strings.ContainsKey("type") && strings.ContainsKey("title"))
                            {
                                if (strings["type"] == "photo")
                                {
                                    instagram = "<a href=\"" + instagramUrl + "\"><img src=\"" + strings["url"] + "\" alt=\"" + HttpUtility.HtmlEncode(strings["title"]) + "\" style=\"max-width: 100%;\" /></a>";
                                }
                            }
                            ContentPreviewCache.Create(e.Core, "instagram.com", instagramId, string.Empty, instagram, e.Core.Prose.Language);
                        }


                        if (!string.IsNullOrEmpty(instagram))
                        {
                            e.PrefixText = instagram;
                            e.SuffixText = string.Empty;
                        }
                        else
                        {
                            e.PrefixText = "<a href=\"" + instagramUrl + "\"><strong>IMG</strong>: ";
                            e.SuffixText = "</a>";
                        }
                    }
                    else
                    {
                        e.PrefixText = "<a href=\"" + instagramUrl + "\"><strong>IMG</strong>: ";
                        e.SuffixText = "</a>";
                    }
                    break;
            }
        }

        private static void BbcodeSoundcloud(BbcodeEventArgs e)
        {
            if (e.Tag.Tag != "soundcloud") return;

            e.SetHandled();

            switch (e.Mode)
            {
                case BbcodeParseMode.Tldr:
                    // Preserve
                    e.AbortParse();
                    break;
                case BbcodeParseMode.StripTags:
                case BbcodeParseMode.Flatten:
                    e.PrefixText = string.Empty;
                    e.PrefixText = string.Empty;
                    break;
                case BbcodeParseMode.Normal:
                    string soundcloudUrl = e.Contents;
                    string soundcloudId = soundcloudUrl;
                    //if (!Regex.IsMatch(e.Contents, "^((http|ftp|https|ftps)://)([^ \\?&=\\#\\\"\\n\\r\\t<]*?(\\.(jpg|jpeg|gif|png)))$", RegexOptions.IgnoreCase)) e.AbortParse();

                    e.RemoveContents();

                    if (TagAllowed(e.Tag.Tag, e.Options))
                    {
                        ContentPreviewCache preview = ContentPreviewCache.GetPreview(e.Core, "soundcloud.com", soundcloudId, e.Core.Prose.Language);
                        string soundcloud = string.Empty;

                        if (preview != null)
                        {
                            soundcloud = preview.Body;
                        }
                        else
                        {
                            string apiUri = "http://soundcloud.com/oembed?format=json&url=" + HttpUtility.UrlEncode(soundcloudId) + "&show_comments=false";
                            WebClient wc = new WebClient();
                            string response = wc.DownloadString(apiUri);

                            Dictionary<string, string> strings = (Dictionary<string, string>)JsonConvert.DeserializeObject(response, typeof(Dictionary<string, string>));

                            if (strings.ContainsKey("html"))
                            {
                                soundcloud = strings["html"];
                            }
                            ContentPreviewCache.Create(e.Core, "soundcloud.com", soundcloudId, string.Empty, soundcloud, e.Core.Prose.Language);
                        }


                        if (!string.IsNullOrEmpty(soundcloud))
                        {
                            e.PrefixText = "</p>" + soundcloud + "<p>";
                            e.SuffixText = string.Empty;
                        }
                        else
                        {
                            e.PrefixText = "<a href=\"" + soundcloudUrl + "\"><strong>IMG</strong>: ";
                            e.SuffixText = "</a>";
                        }
                    }
                    else
                    {
                        e.PrefixText = "<a href=\"" + soundcloudUrl + "\"><strong>IMG</strong>: ";
                        e.SuffixText = "</a>";
                    }
                    break;
            }
        }

        private static void BbcodeTwitter(BbcodeEventArgs e)
        {
            if (e.Tag.Tag != "tweet") return;

            e.SetHandled();

            switch (e.Mode)
            {
                case BbcodeParseMode.Tldr:
                    // Preserve
                    e.AbortParse();
                    break;
                case BbcodeParseMode.StripTags:
                case BbcodeParseMode.Flatten:
                    e.PrefixText = string.Empty;
                    e.PrefixText = string.Empty;
                    break;
                case BbcodeParseMode.Normal:
                    string tweetUrl = e.Contents;
                    string tweetId = tweetUrl;
                    //if (!Regex.IsMatch(e.Contents, "^((http|ftp|https|ftps)://)([^ \\?&=\\#\\\"\\n\\r\\t<]*?(\\.(jpg|jpeg|gif|png)))$", RegexOptions.IgnoreCase)) e.AbortParse();

                    e.RemoveContents();

                    if (tweetUrl.ToLower().StartsWith("http://") || tweetUrl.ToLower().StartsWith("https://"))
                    {
                        char[] splitChars = { '/' };
                        string[] argh = tweetUrl.Split(splitChars);
                        if (argh.Length > 0)
                        {
                            tweetId = argh[argh.Length - 1];
                        }
                    }


                    if (TagAllowed(e.Tag.Tag, e.Options))
                    {
                        ContentPreviewCache preview = ContentPreviewCache.GetPreview(e.Core, "twitter.com", tweetId, e.Core.Prose.Language);
                        string tweet = string.Empty;

                        if (preview != null)
                        {
                            tweet = preview.Body;
                        }
                        else
                        {
                            string apiUri = "https://api.twitter.com/1/statuses/oembed.json?id=" + tweetId + "&maxwidth=550";
                            WebClient wc = new WebClient();
                            string response = wc.DownloadString(apiUri);

                            Dictionary<string, string> strings = (Dictionary<string, string>)JsonConvert.DeserializeObject(response, typeof(Dictionary<string, string>));

                            if (strings.ContainsKey("html"))
                            {
                                tweet = strings["html"];
                            }
                            ContentPreviewCache.Create(e.Core, "twitter.com", tweetId, string.Empty, tweet, e.Core.Prose.Language);
                        }


                        if (!string.IsNullOrEmpty(tweet))
                        {
                            e.PrefixText = "</p>" + tweet + "<p>";
                            e.SuffixText = string.Empty;
                        }
                        else
                        {
                            e.PrefixText = "<a href=\"" + tweetUrl + "\"><strong>IMG</strong>: ";
                            e.SuffixText = "</a>";
                        }
                    }
                    else
                    {
                        e.PrefixText = "<a href=\"" + tweetUrl + "\"><strong>IMG</strong>: ";
                        e.SuffixText = "</a>";
                    }
                    break;
            }
        }

        private static void BbcodeLaTeX(BbcodeEventArgs e)
        {
            if (e.Tag.Tag != "latex") return;

            e.SetHandled();

            switch (e.Mode)
            {
                case BbcodeParseMode.Tldr:
                    // Preserve
                    e.AbortParse();
                    break;
                case BbcodeParseMode.StripTags:
                case BbcodeParseMode.Flatten:
                    e.PrefixText = string.Empty;
                    e.PrefixText = string.Empty;
                    break;
                case BbcodeParseMode.Normal:
                    string latexExpression = HttpUtility.UrlEncode(e.Contents).Replace("+", "%20");
                    e.RemoveContents();

                    e.PrefixText = "<img src=\"http://" + Hyperlink.Domain + "/mimetex.cgi?" + latexExpression + "\" alt=\"LaTeX Equation\"/>";
                    e.SuffixText = string.Empty;
                    break;
            }
        }

        private static void BbcodeFlash(BbcodeEventArgs e)
        {
            // TODO: flash bbcode
            if (e.Tag.Tag != "flash") return;

            e.SetHandled();

            switch (e.Mode)
            {
                case BbcodeParseMode.Tldr:
                    // Preserve
                    e.AbortParse();
                    break;
                case BbcodeParseMode.StripTags:
                case BbcodeParseMode.Flatten:
                    e.PrefixText = string.Empty;
                    e.PrefixText = string.Empty;
                    break;
                case BbcodeParseMode.Normal:
                    if (TagAllowed(e.Tag.Tag, e.Options))
                    {
                    }
                    else
                    {
                    }
                    break;
            }
        }

        private static void BbcodeSilverlight(BbcodeEventArgs e)
        {
            // TODO: silverlight bbcode
            if (e.Tag.Tag != "silverlight") return;

            e.SetHandled();

            switch (e.Mode)
            {
                case BbcodeParseMode.Tldr:
                    // Preserve
                    e.AbortParse();
                    break;
                case BbcodeParseMode.StripTags:
                case BbcodeParseMode.Flatten:
                    e.PrefixText = string.Empty;
                    e.PrefixText = string.Empty;
                    break;
                case BbcodeParseMode.Normal:
                    if (TagAllowed(e.Tag.Tag, e.Options))
                    {
                    }
                    else
                    {
                    }
                    break;
            }
        }

        private void BbcodeUser(BbcodeEventArgs e)
        {
            if (e.Tag.Tag != "user") return;

            e.SetHandled();
            string key = string.Empty;
            long id = 0;

            switch (e.Mode)
            {
                case BbcodeParseMode.Tldr:
                    // Preserve
                    e.AbortParse();
                    break;
                case BbcodeParseMode.StripTags:
                    e.AbortParse();
                    break;
                case BbcodeParseMode.Flatten:
                    key = e.Contents;
                    e.RemoveContents();

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
                            e.SuffixText = string.Empty;
                        }
                        else
                        {
                            e.PrefixText = userUser.DisplayName;
                            e.SuffixText = string.Empty;
                        }
                    }
                    else
                    {
                        e.PrefixText = "Anonymous";
                        e.SuffixText = string.Empty;
                    }
                    break;
                case BbcodeParseMode.Normal:
                    if (core != null)
                    {
                        key = e.Contents;
                        e.RemoveContents();

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
                                e.SuffixText = string.Empty;
                            }
                            else
                            {
                                e.PrefixText = string.Format("<a href=\"{1}\">{0}</a>",
                                    userUser.DisplayName, userUser.Uri);
                                e.SuffixText = string.Empty;
                            }
                        }
                        else
                        {
                            e.PrefixText = "Anonymous";
                            e.SuffixText = string.Empty;
                        }
                    }
                    break;
            }
        }

        public string Flatten(string input)
        {
            return Parse(input, null, null, false, null, null, BbcodeParseMode.Flatten);
        }

        public string Flatten(string input, User viewer)
        {
            return Parse(input, viewer, null, false, null, null, BbcodeParseMode.Flatten);
        }

        public string Flatten(string input, User viewer, Primitive postOwner)
        {
            return Parse(input, viewer, postOwner, false, null, null, BbcodeParseMode.Flatten);
        }

        public string StripTags(string input)
        {
            return Parse(input, null, null, false, null, null, BbcodeParseMode.StripTags);
        }

        public string StripTags(string input, User viewer)
        {
            return Parse(input, viewer, null, false, null, null, BbcodeParseMode.StripTags);
        }

        public string StripTags(string input, User viewer, Primitive postOwner)
        {
            return Parse(input, viewer, postOwner, false, null, null, BbcodeParseMode.StripTags);
        }

        public string Tldr(string input)
        {
            return Parse(input, null, null, false, null, null, BbcodeParseMode.Tldr);
        }

        public string Tldr(string input, User viewer)
        {
            return Parse(input, viewer, null, false, null, null, BbcodeParseMode.Tldr);
        }

        public string Tldr(string input, User viewer, Primitive postOwner)
        {
            return Parse(input, viewer, postOwner, false, null, null, BbcodeParseMode.Tldr);
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
            string param = string.Empty;
            string val = string.Empty;
            for (int i = 0; i < length; i++)
            {
                if (input[i].Equals('&') && i + 5 < length && input.Substring(i, 6).Equals("&quot;"))
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
                    param = string.Empty;
                    val = string.Empty;
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
