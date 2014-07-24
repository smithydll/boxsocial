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
using System.ComponentModel;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using BoxSocial.IO;

namespace BoxSocial.Internals
{

    [DataTable("comments")]
    public sealed class Comment : NumberedItem, ILikeableItem, IShareableItem, IPermissibleSubItem, IActionableItem
    {
        // TODO: 1023 max length
        public const int COMMENT_MAX_LENGTH = 1023;

        [DataField("comment_id", DataFieldKeys.Primary)]
        private long commentId;
        [DataField("user_id")]
        private long userId;
		[DataField("comment_item", DataFieldKeys.Index)]
        private ItemKey itemKey;
        [DataField("comment_likes")]
        private byte likes;
        [DataField("comment_dislikes")]
        private byte dislikes;
        [DataField("comment_spam_score")]
        private byte spamScore;
        [DataField("comment_time_ut")]
        private long timeRaw;
        [DataField("comment_ip", IP)]
        private string commentIp;
        [DataField("comment_text", COMMENT_MAX_LENGTH)]
        private string body;
        [DataField("comment_text_cache", COMMENT_MAX_LENGTH * 2)]
        private string bodyCache;
        [DataField("comment_hash", 128)]
        private string commentHash;
        [DataField("comment_deleted")]
        private bool deleted;

        private ICommentableItem item;
        private User user;

        public long CommentId
        {
            get
            {
                return commentId;
            }
        }

        public long UserId
        {
            get
            {
                return userId;
            }
        }

        public User User
        {
            get
            {
                if (user == null || userId != user.Id)
                {
                    core.PrimitiveCache.LoadUserProfile(userId);
                    user = core.PrimitiveCache[userId];
                    //creator = (User)core.ItemCache[new ItemKey(creatorId, typeof(User))];
                    return user;
                }
                else
                {
                    return user;
                }
            }
        }
		
		public ItemKey CommentedItemKey
		{
			get
			{
				return itemKey;
			}
		}

        public long ItemId
        {
            get
            {
                return itemKey.Id;
            }
        }

        public string ItemType
        {
            get
            {
                return itemKey.TypeString;
            }
        }

        public long ItemTypeId
        {
            get
            {
                return itemKey.TypeId;
            }
        }

        public byte SpamScore
        {
            get
            {
                return spamScore;
            }
        }

        public string Body
        {
            get
            {
                return body;
            }
        }

        public string BodyCache
        {
            get
            {
                return bodyCache;
            }
        }

        public DateTime GetTime(UnixTime tz)
        {
            return tz.DateTimeFromMysql(timeRaw);
        }

        public Comment(Core core, long commentId)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(Comment_ItemLoad);

            try
            {
                LoadItem(commentId);
            }
            catch (InvalidItemException)
            {
                throw new InvalidCommentException();
            }
        }

        public Comment(Core core, DataRow commentRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(Comment_ItemLoad);

            try
            {
                loadItemInfo(commentRow);
            }
            catch (InvalidItemException)
            {
                throw new InvalidCommentException();
            }
        }

        public Comment(Core core, System.Data.Common.DbDataReader commentRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(Comment_ItemLoad);

            try
            {
                loadItemInfo(commentRow);
            }
            catch (InvalidItemException)
            {
                throw new InvalidCommentException();
            }
        }

        protected override void loadItemInfo(DataRow commentRow)
        {
            loadValue(commentRow, "comment_id", out commentId);
            loadValue(commentRow, "user_id", out userId);
            loadValue(commentRow, "comment_item", out itemKey);
            loadValue(commentRow, "comment_likes", out likes);
            loadValue(commentRow, "comment_dislikes", out dislikes);
            loadValue(commentRow, "comment_spam_score", out spamScore);
            loadValue(commentRow, "comment_time_ut", out timeRaw);
            loadValue(commentRow, "comment_ip", out commentIp);
            loadValue(commentRow, "comment_text", out body);
            loadValue(commentRow, "comment_text_cache", out bodyCache);
            loadValue(commentRow, "comment_hash", out commentHash);
            loadValue(commentRow, "comment_deleted", out deleted);

            itemLoaded(commentRow);
            core.ItemCache.RegisterItem((NumberedItem)this);
        }

        protected override void loadItemInfo(System.Data.Common.DbDataReader commentRow)
        {
            loadValue(commentRow, "comment_id", out commentId);
            loadValue(commentRow, "user_id", out userId);
            loadValue(commentRow, "comment_item", out itemKey);
            loadValue(commentRow, "comment_likes", out likes);
            loadValue(commentRow, "comment_dislikes", out dislikes);
            loadValue(commentRow, "comment_spam_score", out spamScore);
            loadValue(commentRow, "comment_time_ut", out timeRaw);
            loadValue(commentRow, "comment_ip", out commentIp);
            loadValue(commentRow, "comment_text", out body);
            loadValue(commentRow, "comment_text_cache", out bodyCache);
            loadValue(commentRow, "comment_hash", out commentHash);
            loadValue(commentRow, "comment_deleted", out deleted);

            itemLoaded(commentRow);
            core.ItemCache.RegisterItem((NumberedItem)this);
        }

        void Comment_ItemLoad()
        {
            ItemDeleted += new ItemDeletedEventHandler(Comment_ItemDeleted);
        }

        void Comment_ItemDeleted(object sender, ItemDeletedEventArgs e)
        {
            ActionableItem.CleanUp(core, this);
        }

        public static Comment Create(Core core, ItemKey itemKey, string comment)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            if (!core.Session.IsLoggedIn)
            {
                throw new NotLoggedInException();
            }

            if (core.Db.Query(string.Format("SELECT user_id FROM comments WHERE (user_id = {0} OR comment_ip = '{1}') AND (UNIX_TIMESTAMP() - comment_time_ut) < 20",
                    core.LoggedInMemberId, core.Session.IPAddress.ToString())).Rows.Count > 0)
            {
                throw new CommentFloodException();
            }

            if (comment.Length > COMMENT_MAX_LENGTH)
            {
                throw new CommentTooLongException();
            }

            if (comment.Length < 2)
            {
                throw new CommentTooShortException();
            }

            Relation relations = Relation.None;
            // A little bit of hard coding we can't avoid
            if (itemKey.TypeString == typeof(User).FullName)
            {
                core.LoadUserProfile(itemKey.Id);
                relations = core.PrimitiveCache[itemKey.Id].GetRelations(core.Session.LoggedInMember.ItemKey);
            }

            string commentCache = string.Empty;

            if (!comment.Contains("[user") && !comment.Contains("sid=true]"))
            {
                commentCache = core.Bbcode.Parse(HttpUtility.HtmlEncode(comment), null, core.Session.LoggedInMember, true, string.Empty, string.Empty);
            }

            core.Db.BeginTransaction();

            Comment newComment = (Comment)Item.Create(core, typeof(Comment), new FieldValuePair("comment_item_id", itemKey.Id),
                new FieldValuePair("comment_item_type_id", itemKey.TypeId),
                new FieldValuePair("user_id", core.LoggedInMemberId),
                new FieldValuePair("comment_time_ut", UnixTime.UnixTimeStamp()),
                new FieldValuePair("comment_text", comment),
                new FieldValuePair("comment_text_cache", commentCache),
                new FieldValuePair("comment_ip", core.Session.IPAddress.ToString()),
                new FieldValuePair("comment_spam_score", CalculateSpamScore(core, comment, relations)),
                new FieldValuePair("comment_hash", MessageMd5(comment)));

            return newComment;
        }

        public static List<Comment> GetComments(Core core, ItemKey itemKey, SortOrder commentSortOrder, int currentPage, int perPage, List<User> commenters)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            Mysql db = core.Db;
            List<Comment> comments = new List<Comment>();

            string sort = (commentSortOrder == SortOrder.Ascending) ? "ASC" : "DESC";

            SelectQuery query = Comment.GetSelectQueryStub(typeof(Comment));
            query.AddCondition("comment_deleted", false);
            query.AddSort(commentSortOrder, "comment_time_ut");
            query.LimitStart = (currentPage - 1) * perPage;
            query.LimitCount = perPage;

            if (commenters != null)
            {
                if (commenters.Count == 2)
                {
                    if (itemKey.TypeString == typeof(User).FullName)
                    {
                        QueryCondition qc1 = query.AddCondition("comment_item_id", commenters[0].Id);
                        qc1.AddCondition("user_id", commenters[1].Id);

                        QueryCondition qc2 = query.AddCondition(ConditionRelations.Or, "comment_item_id", commenters[1].Id);
                        qc2.AddCondition("user_id", commenters[0].Id);

                        query.AddCondition("comment_item_type_id", itemKey.TypeId);
                    }
                    else
                    {
                        query.AddCondition("comment_item_id", itemKey.Id);
                        query.AddCondition("comment_item_type_id", itemKey.TypeId);
                    }
                }
                else
                {
                    query.AddCondition("comment_item_id", itemKey.Id);
                    query.AddCondition("comment_item_type_id", itemKey.TypeId);
                }
            }
            else
            {
                query.AddCondition("comment_item_id", itemKey.Id);
                query.AddCondition("comment_item_type_id", itemKey.TypeId);
            }

            System.Data.Common.DbDataReader commentsReader = db.ReaderQuery(query);

            while (commentsReader.Read())
            {
                comments.Add(new Comment(core, commentsReader));
            }

            commentsReader.Close();
            commentsReader.Dispose();

            return comments;
        }

        public static void LoadUserInfoCache(Core core, List<Comment> comments)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            List<long> userIds = GetUserIds(comments);

            core.LoadUserProfiles(userIds);
        }

        private static List<long> GetUserIds(List<Comment> comments)
        {
            List<long> userIds = new List<long>();

            foreach (Comment comment in comments)
            {
                if (!userIds.Contains(comment.UserId))
                {
                    userIds.Add(comment.UserId);
                }
            }

            return userIds;
        }

        private static int CalculateSpamScore(Core core, string message, Relation relations)
        {
            double spamScore = 0;
            bool hasMatchingHash = false;
            string messageMd5Hash = MessageMd5(message);

            TimeSpan ts = DateTime.Now - core.Session.LoggedInMember.UserInfo.RegistrationDate;

            // registered last ...
            if (ts.TotalMinutes <= 10) // first 10 minutes
            {
                spamScore += 6;
            }
            else if (ts.TotalDays <= 1) // first day
            {
                spamScore += 4;
            }
            else if (ts.TotalDays <= 7) // first week
            {
                spamScore += 1;
            }

            // hyperlinks
            MatchCollection httpMatches = Regex.Matches(message, @"(([a-z]+?://){1}|)([a-z0-9\-\.,\?!%\*_\#:;~\\&$@\/=\+\(\)]+)", RegexOptions.IgnoreCase);
            spamScore += Math.Sqrt(httpMatches.Count * 1.0);

            // embedded images
            int lastImgIndex = 0;
            int imageScore = 0;
            do
            {
                lastImgIndex = Math.Max(message.IndexOf("[img]", lastImgIndex + 1), message.IndexOf("[IMG]", lastImgIndex + 1));
                if (lastImgIndex >= 0)
                {
                    imageScore += 1;
                }
            }
            while (lastImgIndex >= 0);
            spamScore += Math.Sqrt((double)imageScore);

            // common spam words that on their own aren't spam, but if used alot might indicate the presence of spam
            string[] spamWords = { "porn", "poker", "ringtone", "casino", "viagra", "blackjack", "gambling", "beastiality", "insurance", "phentermine", "incest", "anal", "lesbian", "gay", "porno", "fisting", "sex", "dildo", "fuck", "cum", "milf", "insurence", "ephedrine", "prescription", "cialis", "heroin" };
            Array.Sort(spamWords);

            string[] messageWords = message.Split(new char[] { ' ', '\t', '\'', '\n', '?', ',', '.', '(', ')', '[', ']' });

            int wordScore = 0;
            for (int i = 0; i < messageWords.Length; i++)
            {
                if (Array.IndexOf(spamWords, messageWords[i].ToLower()) >= 0)
                {
                    wordScore += 1;
                }
            }
            spamScore += Math.Sqrt((double)wordScore);

            // if less than 5 words
            if (messageWords.Length < 5)
            {
                spamScore += 2;
            }

            //
            // select all threads that are related spam
            //
            DataTable spamCommentsTable = core.Db.Query(string.Format("SELECT comment_ip, comment_hash FROM comments WHERE ((comment_ip = '{0}' AND comment_time_ut + 86400 > UNIX_TIMESTAMP()) OR comment_hash = '{1}') AND comment_spam_score >= 10 GROUP BY comment_ip, comment_hash;",
                core.Session.IPAddress.ToString(), messageMd5Hash));

            // known spam IPs
            for (int i = 0; i < spamCommentsTable.Rows.Count; i++)
            {
                if ((string)spamCommentsTable.Rows[i]["comment_ip"] == core.Session.IPAddress.ToString())
                {
                    spamScore += 5;
                    break;
                }
            }

            // known spam hash
            for (int i = 0; i < spamCommentsTable.Rows.Count; i++)
            {
                if ((string)spamCommentsTable.Rows[i]["comment_hash"] == messageMd5Hash)
                {
                    spamScore += 8;
                    hasMatchingHash = true;
                    break;
                }
            }

            // friend
            if ((relations & Relation.Owner) == Relation.Owner)
            {
                spamScore = 0;
            }
            else if ((relations & Relation.Family) == Relation.Family)
            {
                spamScore -= 5;
            }
            else if ((relations & Relation.Friend) == Relation.Friend)
            {
                spamScore -= 3;
            }
            // Group: spamScore -= 1

            int returnScore = (int)Math.Ceiling(spamScore);
            returnScore = Math.Min(returnScore, byte.MaxValue); // prevent overflow
            returnScore = Math.Max(returnScore, byte.MinValue); // prevent underflow

            // update existing comments with matching MD5 string
            if (returnScore >= 10)
            {
                if (hasMatchingHash)
                {
                    core.Db.UpdateQuery(string.Format("UPDATE comments SET comment_spam_score = {0} WHERE comment_hash = '{1}'",
                        returnScore, messageMd5Hash));
                }
            }

            return returnScore;
        }

        public static string MessageMd5(string input)
        {
            return System.Web.Security.FormsAuthentication.HashPasswordForStoringInConfigFile(input, "MD5").ToLower();
        }

        public override long Id
        {
            get
            {
                return commentId;
            }
        }

        public string BuildUri(ICommentableItem item)
        {
            return core.Hyperlink.AppendSid(string.Format("{0}?c={2}&#c{1}",
                    core.Hyperlink.StripSid(item.Uri), commentId, commentId));
        }

        public override string Uri
        {
            get
            {
                return CommentedItem.Uri;
            }
        }

        public long Likes
        {
            get
            {
                return likes;
            }
        }

        public long Dislikes
        {
            get
            {
                return dislikes;
            }
        }

        public Primitive Owner
        {
            get
            {
                return PermissiveParent.Owner;
            }
        }

        public ICommentableItem CommentedItem
        {
            get
            {
                if (item == null || item.ItemKey.Id != CommentedItemKey.Id || item.ItemKey.TypeId != CommentedItemKey.TypeId)
                {
                    item = (ICommentableItem)NumberedItem.Reflect(core, CommentedItemKey);
                }
                return item;
            }
        }

        public IPermissibleItem PermissiveParent
        {
            get
            {
                if (item == null || item.ItemKey.Id != CommentedItemKey.Id || item.ItemKey.TypeId != CommentedItemKey.TypeId)
                {
                    core.ItemCache.RequestItem(CommentedItemKey);
                    item = (ICommentableItem)core.ItemCache[CommentedItemKey];
                }
                if (item is IPermissibleItem)
                {
                    return (IPermissibleItem)item;
                }
                else
                {
                    return item.Owner;
                }
            }
        }

        public static void Commented(Core core, ItemKey itemKey)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            if (itemKey.Id < 1)
            {
                throw new InvalidItemException();
            }

            core.AdjustCommentCount(itemKey, 1);
        }

        public static void CommentDeleted(Core core, ItemKey itemKey)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            if (itemKey.Id < 1)
            {
                throw new InvalidItemException();
            }

            core.AdjustCommentCount(itemKey, -1);
        }

        public long SharedTimes
        {
            get
            {
                return 0;
            }
        }

        public ItemKey OwnerKey
        {
            get
            {
                return new ItemKey(UserId, ItemKey.GetTypeId(typeof(User)));
            }
        }

        public string ShareString
        {
            get
            {
                return core.Bbcode.FromStatusCode(Body);
            }
        }

        public string ShareUri
        {
            get
            {
                return core.Hyperlink.AppendAbsoluteSid(string.Format("/share?item={0}&type={1}", CommentedItemKey.Id, CommentedItemKey.TypeId), true);
            }
        }


        public string Action
        {
            get
            {
                return string.Format(core.Prose.GetString("_COMMENTED_ON"), CommentedItem.Owner.DisplayNameOwnership, CommentedItem.Noun);
            }
        }

        public string GetActionBody(List<ItemKey> subItems)
        {
            return ShareString;
        }


        public ActionableItemType PostType
        {
            get
            {
                return ActionableItemType.Text;
            }
        }

        public byte[] Data
        {
            get
            {
                return null;
            }
        }

        public string DataContentType
        {
            get
            {
                return null;
            }
        }

        public string Caption
        {
            get
            {
                return null;
            }
        }
    }

    public class CommentPostedEventArgs : EventArgs
    {
        private Comment comment;
        private ItemKey itemKey;
        private User poster;

        public Comment Comment
        {
            get
            {
                return comment;
            }
        }
		
		public ItemKey ItemKey
		{
			get
			{
				return itemKey;
			}
		}

        public string ItemType
        {
            get
            {
                return itemKey.TypeString;
            }
        }

        public long ItemId
        {
            get
            {
                return itemKey.Id;
            }
        }

        public User Poster
        {
            get
            {
                return poster;
            }
        }

        public CommentPostedEventArgs(Comment comment, User poster, ItemKey itemKey)
        {
            this.comment = comment;
            this.poster = poster;
            this.itemKey = itemKey;
        }
    }

    public class InvalidCommentException : Exception
    {
    }

    public class CommentTooLongException : Exception
    {
    }

    public class CommentTooShortException : Exception
    {
    }

    public class CommentFloodException : Exception
    {
    }

    public class NotLoggedInException : Exception
    {
    }
}
