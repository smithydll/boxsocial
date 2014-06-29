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
using System.Web;
using BoxSocial.Forms;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    public class TagSelectBox : FormField
    {
        private Core core;
        private List<long> tagIds;
        private StyleLength width;
        protected bool visible;
        protected bool disabled;
        private ScriptProperty script;

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

        public List<long> TagIds
        {
            get
            {
                return tagIds;
            }
            set
            {
                tagIds = value;
            }
        }

        public TagSelectBox(Core core, string name)
        {
            this.core = core;
            this.name = name;

            disabled = false;
            visible = true;

            tagIds = new List<long>();
            width = new StyleLength(100F, LengthUnits.Percentage);
            script = new ScriptProperty();
        }

        public TagSelectBox(Core core, string name, List<long> tagIds)
        {
            this.core = core;
            this.name = name;
            this.tagIds = tagIds;

            disabled = false;
            visible = true;

            width = new StyleLength(100F, LengthUnits.Percentage);
            script = new ScriptProperty();
        }

        public void AddTagId(long tagId)
        {
            this.tagIds.Add(tagId);
        }

        public void AddTag(Tag tag)
        {
            this.tagIds.Add(tag.Id);
        }

        public override string ToString()
        {
            return this.ToString(Forms.DisplayMedium.Desktop);
        }

        public override string ToString(Forms.DisplayMedium medium)
        {
            long tagTypeId = ItemType.GetTypeId(typeof(Tag));

            foreach (long tagId in tagIds)
            {
                core.ItemCache.RequestItem(new ItemKey(tagId, tagTypeId));
            }

            StringBuilder tags = new StringBuilder();
            StringBuilder idList = new StringBuilder();

            bool first = true;
            foreach (long tagId in tagIds)
            {
                if (!first)
                {
                    idList.Append(",");
                }
                else
                {
                    first = false;
                }
                idList.Append(tagId.ToString());

                switch (medium)
                {
                    case Forms.DisplayMedium.Desktop:
                        tags.Append(string.Format("<span class=\"tag\">{1}<span class=\"delete\" onclick=\"rvl($(this).parent().siblings('.ids'),'{0}'); $(this).parent().siblings('.ids').trigger(\'change\'); $(this).parent().remove();\">x</span><input type=\"hidden\" id=\"tag-{0}\" name=\"tag[{0}]\" value=\"{0}\" /></span>", tagId, HttpUtility.HtmlEncode(((Tag)core.ItemCache[new ItemKey(tagId, tagTypeId)]).TagText)));
                        break;
                    case Forms.DisplayMedium.Mobile:
                    case Forms.DisplayMedium.Tablet:
                        tags.Append(string.Format("<span class=\"item-{0} tag\">{1}<input type=\"hidden\" id=\"tag-{0}\" name=\"tag[{0}]\" value=\"{0}\" /></span>", tagId, HttpUtility.HtmlEncode(((Tag)core.ItemCache[new ItemKey(tagId, tagTypeId)]).TagText)));
                        break;
                }
            }

            switch (medium)
            {
                case Forms.DisplayMedium.Desktop:
                    return string.Format("<div id=\"{0}\" class=\"tag-droplist\" onclick=\"$(this).children('.textbox').focus();\" style=\"width: {4};{3}\">{6}<input type=\"text\" name=\"{0}-text\" id=\"{0}-text\" value=\"{1}\" class=\"textbox\" style=\"\"{2}{5}/><input type=\"hidden\" name=\"{0}-ids\" id=\"{0}-ids\" class=\"ids\" value=\"{7}\"{5}/></div>",
                            HttpUtility.HtmlEncode(name),
                            HttpUtility.HtmlEncode(string.Empty),
                            (IsDisabled) ? " disabled=\"disabled\"" : string.Empty,
                            (!IsVisible) ? " display: none;" : string.Empty,
                            width,
                            Script.ToString(),
                            tags.ToString(),
                            idList.ToString());
                case Forms.DisplayMedium.Mobile:
                case Forms.DisplayMedium.Tablet:
                    return string.Format("<div id=\"{0}\" class=\"tag-droplist\" onclick=\"showUsersBar(event, '{0}', 'users');\" style=\"width: {4};{3}\">{6}<input type=\"text\" name=\"{0}-text\" id=\"{0}-text\" value=\"{1}\" class=\"textbox\" style=\"\"{2}{5}/><input type=\"hidden\" name=\"{0}-ids\" id=\"{0}-ids\" class=\"ids\" value=\"{7}\"{5}/></div>",
                            HttpUtility.HtmlEncode(name),
                            HttpUtility.HtmlEncode(string.Empty),
                            (IsDisabled) ? " disabled=\"disabled\"" : string.Empty,
                            (!IsVisible) ? " display: none;" : string.Empty,
                            width,
                            Script.ToString(),
                            tags.ToString(),
                            idList.ToString());
                default:
                    return string.Empty;
            }
        }

        public static List<long> FormTags(Core core, string name)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            List<long> tagIds = new List<long>();

            string formValue = core.Http.Form[name + "-ids"];
            if (!string.IsNullOrEmpty(formValue))
            {
                string[] ids = formValue.Split(new char[] { ',' });

                foreach (string idString in ids)
                {
                    long id;
                    long.TryParse(idString, out id);

                    if (id > 0)
                    {
                        tagIds.Add(id);
                    }
                }
            }

            int limit = 10;
            string tagsText = core.Http.Form[name + "-text"];

            if (!string.IsNullOrEmpty(tagsText))
            {
                string[] tagText = tagsText.Split(new char[] { ',', ';', ' ' });

                foreach (string tag in tagText)
                {
                    if (limit > 0)
                    {
                        string normalisedTag = string.Empty;
                        Tag.NormaliseTag(tag, ref normalisedTag);

                        if (!string.IsNullOrEmpty(normalisedTag) && normalisedTag.Length >= 2)
                        {
                            try
                            {
                                Tag existingTag = new Tag(core, normalisedTag);

                                tagIds.Add(existingTag.Id);
                                limit--;
                            }
                            catch (InvalidTagException)
                            {
                                Tag newTag = Tag.Create(core, tag);

                                tagIds.Add(newTag.Id);
                                limit--;
                            }
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return tagIds;
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
            tagIds = FormTags(core, value);
        }
    }
}
