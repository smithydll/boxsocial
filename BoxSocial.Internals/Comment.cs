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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Security;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    public sealed class Comment
    {
        public const string COMMENT_INFO_FIELDS = "c.comment_id, c.user_id, c.comment_item_id, c.comment_item_type, c.comment_spam_score, c.comment_time_ut, c.comment_text";

        // TODO: 1023 max length
        public const int COMMENT_MAX_LENGTH = 511;

        private Mysql db;

        private long commentId;
        private long userId;
        private long itemId;
        private string itemType;
        private byte spamScore;
        private long timeRaw;
        private string body;

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

        public long ItemId
        {
            get
            {
                return itemId;
            }
        }

        public string ItemType
        {
            get
            {
                return itemType;
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

        public DateTime GetTime(UnixTime tz)
        {
            return tz.DateTimeFromMysql(timeRaw);
        }

        public Comment(Mysql db, long commentId)
        {
            this.db = db;

            DataTable commentsTable = db.Query(string.Format("SELECT {1} FROM comments c WHERE c.comment_id = {0};",
                commentId, Comment.COMMENT_INFO_FIELDS));

            if (commentsTable.Rows.Count == 1)
            {
                loadCommentInfo(commentsTable.Rows[0]);
            }
            else
            {
                throw new InvalidCommentException();
            }
        }

        public Comment(Mysql db, DataRow commentRow)
        {
            this.db = db;

            loadCommentInfo(commentRow);
        }

        private void loadCommentInfo(DataRow commentRow)
        {
            commentId = (long)commentRow["comment_id"];
            userId = (long)(int)commentRow["user_id"];
            itemId = (long)commentRow["comment_item_id"];
            itemType = (string)commentRow["comment_item_type"];
            spamScore = (byte)commentRow["comment_spam_score"];
            timeRaw = (long)commentRow["comment_time_ut"];
            if (!(commentRow["comment_text"] is DBNull))
            {
                body = (string)commentRow["comment_text"];
            }
        }

        public static Comment Create(Core core, string itemType, long itemId, string comment)
        {
            if (!core.session.IsLoggedIn)
            {
                throw new NotLoggedInException();
            }

            if (core.db.Query(string.Format("SELECT user_id FROM comments WHERE (user_id = {0} OR comment_ip = '{1}') AND (UNIX_TIMESTAMP() - comment_time_ut) < 20",
                    core.LoggedInMemberId, core.session.IPAddress.ToString())).Rows.Count > 0)
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
            if (itemType == "USER")
            {
                core.LoadUserProfile(itemId);
                relations = core.UserProfiles[itemId].GetRelations(core.session.LoggedInMember);
            }

            long commentId = core.db.UpdateQuery(string.Format("INSERT INTO comments (comment_item_id, comment_item_type, user_id, comment_time_ut, comment_text, comment_ip, comment_spam_score, comment_hash) VALUES ({0}, '{1}', {2}, UNIX_TIMESTAMP(), '{3}', '{4}', {5}, '{6}');",
                    itemId, Mysql.Escape(itemType), core.LoggedInMemberId, Mysql.Escape(comment), core.session.IPAddress.ToString(), CalculateSpamScore(core, comment, relations), MessageMd5(comment)), true);

            return new Comment(core.db, commentId);
        }

        public static List<Comment> GetComments(Mysql db, string itemType, long itemId, SortOrder commentSortOrder, int currentPage, int perPage)
        {
            List<Comment> comments = new List<Comment>();

            string sort = (commentSortOrder == SortOrder.Ascending) ? "ASC" : "DESC";

            DataTable commentsTable = db.Query(string.Format("SELECT {2} FROM comments c WHERE c.comment_item_type = '{1}' AND c.comment_item_id = {0} AND comment_deleted = FALSE ORDER BY c.comment_time_ut {5} LIMIT {3}, {4};",
                itemId, Mysql.Escape(itemType), Comment.COMMENT_INFO_FIELDS, (currentPage - 1) * perPage, perPage, sort));

            foreach (DataRow dr in commentsTable.Rows)
            {
                comments.Add(new Comment(db, dr));
            }

            return comments;
        }

        public static void LoadUserInfoCache(Core core, List<Comment> comments)
        {
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

            TimeSpan ts = DateTime.Now - core.session.LoggedInMember.RegistrationDate;

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
            DataTable spamCommentsTable = core.db.Query(string.Format("SELECT comment_ip, comment_hash FROM comments WHERE ((comment_ip = '{0}' AND comment_time_ut + 86400 > UNIX_TIMESTAMP()) OR comment_hash = '{1}') AND comment_spam_score >= 10 GROUP BY comment_ip, comment_hash;",
                core.session.IPAddress.ToString(), messageMd5Hash));

            // known spam IPs
            for (int i = 0; i < spamCommentsTable.Rows.Count; i++)
            {
                if ((string)spamCommentsTable.Rows[i]["comment_ip"] == core.session.IPAddress.ToString())
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
                    core.db.UpdateQuery(string.Format("UPDATE comments SET comment_spam_score = {0} WHERE comment_hash = '{1}'",
                        returnScore, messageMd5Hash));
                }
            }

            return returnScore;
        }

        public static string MessageMd5(string input)
        {
            return FormsAuthentication.HashPasswordForStoringInConfigFile(input, "MD5").ToLower();
        }

        public long Id
        {
            get
            {
                return commentId;
            }
        }

        public string Namespace
        {
            get
            {
                return this.GetType().FullName;
            }
        }

        public string BuildUri(ICommentableItem item)
        {
            return Linker.AppendSid(string.Format("{0}?c={2}&#c{1}",
                    Linker.StripSid(item.Uri), commentId, commentId));
        }
    }

    public class CommentPostedEventArgs : EventArgs
    {
        private Comment comment;
        private string itemType;
        private long itemId;
        private Member poster;

        public Comment Comment
        {
            get
            {
                return comment;
            }
        }

        public string ItemType
        {
            get
            {
                return itemType;
            }
        }

        public long ItemId
        {
            get
            {
                return itemId;
            }
        }

        public Member Poster
        {
            get
            {
                return poster;
            }
        }

        public CommentPostedEventArgs(Comment comment, Member poster, string itemType, long itemId)
        {
            this.comment = comment;
            this.poster = poster;
            this.itemType = itemType;
            this.itemId = itemId;
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
