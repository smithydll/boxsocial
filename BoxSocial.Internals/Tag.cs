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
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    [DataTable("tags")]
    public class Tag : NumberedItem
    {
        [DataField("tag_id", DataFieldKeys.Primary)]
        private long tagId;
        [DataField("tag_text", 31)]
        private string text;
        [DataField("tag_text_normalised", DataFieldKeys.Unique, 31)]
        private string textNormalised;
        [DataField("tag_items")]
        private long tagItems;

        public string TagText
        {
            get
            {
                return text;
            }
        }

        public string TagTextNormalised
        {
            get
            {
                return textNormalised;
            }
        }

        public Tag(Core core, long tagId)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(Tag_ItemLoad);

            try
            {
                LoadItem(tagId);
            }
            catch (InvalidItemException)
            {
                throw new InvalidTagException();
            }
        }

        public Tag(Core core, string textNormalised)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(Tag_ItemLoad);

            try
            {
                LoadItem("tag_text_normalised", textNormalised);
            }
            catch (InvalidItemException)
            {
                throw new InvalidTagException();
            }
        }

        public Tag(Core core, DataRow tagRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(Tag_ItemLoad);

            loadItemInfo(tagRow);
        }

        private void Tag_ItemLoad()
        {
        }

        public static Tag Create(Core core, string tag)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            string tagNormalised = string.Empty;
            NormaliseTag(tag, ref tagNormalised);

            Item newItem = Item.Create(core, typeof(Tag), new FieldValuePair("tag_text", tag),
                new FieldValuePair("tag_text_normalised", tagNormalised),
                new FieldValuePair("tag_items", 0));

            return (Tag)newItem;
        }

        public static void NormaliseTag(string text, ref string normalisedText)
        {
            if (string.IsNullOrEmpty(normalisedText))
            {
                normalisedText = text;
            }

            // normalise slug if it has been fiddeled with
            normalisedText = normalisedText.ToLower().Normalize(NormalizationForm.FormD);
            string normalisedSlug = "";

            for (int i = 0; i < normalisedText.Length; i++)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(normalisedText[i]) != UnicodeCategory.NonSpacingMark)
                {
                    normalisedSlug += normalisedText[i].ToString();
                }
            }
            // we want to be a little less stringent with list items to allow for some punctuation of being of importance
            normalisedText = Regex.Replace(normalisedSlug, @"([^\w\+\&\*\(\)\=\:\?\-\#\@\!\$]+)", "-");
        }

        public static List<Tag> GetTags(Core core, NumberedItem item)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            List<Tag> tags = new List<Tag>();

            SelectQuery query = Item.GetSelectQueryStub(typeof(ItemTag));
            query.AddCondition("item_id", item.Id);
            query.AddCondition("item_type_id", item.ItemKey.TypeId);
            query.AddSort(SortOrder.Ascending, "tag_text_normalised");

            DataTable tagsTable = core.Db.Query(query);

            foreach (DataRow dr in tagsTable.Rows)
            {
                tags.Add(new Tag(core, dr));
            }

            return tags;
        }

        public static List<Tag> GetTags(Core core, string[] tags)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            List<Tag> tagList = new List<Tag>();

            if (tags.Length == 0)
            {
                return tagList;
            }

            SelectQuery query = Item.GetSelectQueryStub(typeof(Tag));
            query.AddCondition("tag_text_normalised", ConditionEquality.In, tags);

            DataTable tagsTable = core.Db.Query(query);

            foreach (DataRow dr in tagsTable.Rows)
            {
                tagList.Add(new Tag(core, dr));
            }

            return tagList;
        }

        public static List<Tag> SearchTags(Core core, string tagText)
        {
            List<Tag> tags = new List<Tag>();

            if (!string.IsNullOrEmpty(tagText))
            {
                string normalisedText = string.Empty;
                Tag.NormaliseTag(tagText, ref normalisedText);

                SelectQuery query = Tag.GetSelectQueryStub(typeof(Tag));
                query.AddCondition("tag_text_normalised", ConditionEquality.Like, QueryCondition.EscapeLikeness(normalisedText) + "%");
                query.AddSort(SortOrder.Descending, "tag_items");

                query.LimitCount = 20;

                DataTable tagsTable = core.Db.Query(query);

                foreach (DataRow dr in tagsTable.Rows)
                {
                    tags.Add(new Tag(core, dr));
                }
            }

            return tags;
        }

        public static void LoadTagsIntoItem(Core core, NumberedItem item, List<long> tagIds)
        {
            LoadTagsIntoItem(core, item, tagIds, false);
        }

        public static void LoadTagsIntoItem(Core core, NumberedItem item, List<long> tagIds, bool isNewItem)
        {
            if (isNewItem)
            {
                if (tagIds.Count == 0)
                {
                    return;
                }
            }

            List<Tag> itemTags = GetTags(core, item);
            List<long> itemTagIds = new List<long>();

            List<long> tagsToAdd = new List<long>();
            List<long> tagsToRemove = new List<long>();

            foreach (Tag tag in itemTags)
            {
                itemTagIds.Add(tag.Id);
                if (!tagIds.Contains(tag.Id))
                {
                    tagsToRemove.Add(tag.Id);
                }
            }

            foreach (long tagId in tagIds)
            {
                if (!itemTagIds.Contains(tagId))
                {
                    tagsToAdd.Add(tagId);
                }
            }

            if (tagsToAdd.Count > 0)
            {
                for (int i = 0; i < tagsToAdd.Count; i++)
                {
                    ItemTag.Create(core, item, tagsToAdd[i]);
                }

                UpdateQuery uQuery = new UpdateQuery(typeof(Tag));
                uQuery.AddField("tag_items", new QueryOperation("tag_items", QueryOperations.Addition, 1));
                uQuery.AddCondition("tag_id", ConditionEquality.In, tagsToAdd.ToArray());

                core.Db.Query(uQuery);
            }

            if (tagsToRemove.Count > 0)
            {
                DeleteQuery dQuery = new DeleteQuery(typeof(ItemTag));
                dQuery.AddCondition("tag_id", ConditionEquality.In, tagsToRemove.ToArray());
                dQuery.AddCondition("item_id", item.Id);
                dQuery.AddCondition("item_type_id", item.ItemKey.TypeId);

                core.Db.Query(dQuery);

                UpdateQuery uQuery = new UpdateQuery(typeof(Tag));
                uQuery.AddField("tag_items", new QueryOperation("tag_items", QueryOperations.Subtraction, 1));
                uQuery.AddCondition("tag_id", ConditionEquality.In, tagsToRemove.ToArray());

                core.Db.Query(uQuery);
            }
        }

        public static void LoadTagsIntoItem(Core core, NumberedItem item, string tagList)
        {
            LoadTagsIntoItem(core, item, tagList, false);
        }

        public static void LoadTagsIntoItem(Core core, NumberedItem item, string tagList, bool isNewItem)
        {
            if (isNewItem)
            {
                if (string.IsNullOrEmpty(tagList))
                {
                    return;
                }

                if (tagList.Trim(new char[] { ' ', '\t', ';', ',', ':', '.', '-', '(', ')', '<', '>', '[', ']', '{', '}', '|', '\\', '/' }).Length < 2)
                {
                    return;
                }
            }

            List<Tag> itemTags = GetTags(core, item);
            List<string> tagsListNormalised = new List<string>();
            List<string> tagsNormalised = new List<string>();
            List<string> tagsToAdd = new List<string>();
            List<string> tagsToAddNormalised = new List<string>();
            List<string> tagsToRemoveNormalised = new List<string>();
            List<string> tagsToLoad = new List<string>();

            int totalTags = 0;

            foreach (Tag tag in itemTags)
            {
                tagsListNormalised.Add(tag.TagTextNormalised);
            }

            string[] tags = tagList.Split(new char[] {' '});

            for (int i = 0; i < tags.Length; i++)
            {
                string tag = tags[i].Trim(new char[] { ',', ';', ' ' });
                string tagNormalised = string.Empty;
                NormaliseTag(tag, ref tagNormalised);

                if (!tagsListNormalised.Contains(tagNormalised))
                {
                    tagsListNormalised.Add(tagNormalised);
                    tagsToAddNormalised.Add(tagNormalised);
                    tagsToAdd.Add(tag);
                }
                
                tagsNormalised.Add(tagNormalised);

                totalTags++;
                /* Limit to 10 tags per item */
                if (totalTags == 10)
                {
                    break;
                }
            }

            foreach (Tag tag in itemTags)
            {
                if (!tagsNormalised.Contains(tag.TagTextNormalised))
                {
                    tagsToRemoveNormalised.Add(tag.TagTextNormalised);
                }
            }

            foreach (string tag in tagsToAddNormalised)
            {
                tagsToLoad.Add(tag);
            }

            foreach (string tag in tagsToRemoveNormalised)
            {
                tagsToLoad.Add(tag);
            }

            List<Tag> tagIds = GetTags(core, tagsToLoad.ToArray());
            Dictionary<string, Tag> tagIdsNormalised = new Dictionary<string, Tag>();

            foreach (Tag tag in tagIds)
            {
                tagIdsNormalised.Add(tag.TagTextNormalised, tag);
            }

            if (tagsToAddNormalised.Count > 0)
            {
                for (int i = 0; i < tagsToAddNormalised.Count; i++)
                {
                    if (!tagIdsNormalised.ContainsKey(tagsToAddNormalised[i]))
                    {
                        Tag newTag = Tag.Create(core, tagsToAdd[i]);
                        ItemTag.Create(core, item, newTag);
                    }
                    else
                    {
                        ItemTag.Create(core, item, tagIdsNormalised[tagsToAddNormalised[i]]);
                    }
                }

                UpdateQuery uQuery = new UpdateQuery(typeof(Tag));
                uQuery.AddField("tag_items", new QueryOperation("tag_items", QueryOperations.Addition, 1));
                uQuery.AddCondition("tag_text_normalised", ConditionEquality.In, tagsToAddNormalised.ToArray());

                core.Db.Query(uQuery);
            }

            if (tagsToRemoveNormalised.Count > 0)
            {
                List<long> tagToRemoveIds = new List<long>();
                foreach (string tag in tagsToRemoveNormalised)
                {
                    tagToRemoveIds.Add(tagIdsNormalised[tag].Id);
                }

                DeleteQuery dQuery = new DeleteQuery(typeof(ItemTag));
                dQuery.AddCondition("tag_id", ConditionEquality.In, tagToRemoveIds.ToArray());
                dQuery.AddCondition("item_id", item.Id);
                dQuery.AddCondition("item_type_id", item.ItemKey.TypeId);

                core.Db.Query(dQuery);

                UpdateQuery uQuery = new UpdateQuery(typeof(Tag));
                uQuery.AddField("tag_items", new QueryOperation("tag_items", QueryOperations.Subtraction, 1));
                uQuery.AddCondition("tag_id", ConditionEquality.In, tagToRemoveIds.ToArray());

                core.Db.Query(uQuery);
            }
        }

        public override long Id
        {
            get
            {
                return tagId;
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

    public class InvalidTagException : Exception
    {
    }
}
