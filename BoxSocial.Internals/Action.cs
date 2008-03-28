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
using System.Data;
using System.Text;
using System.Web;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    /*
     * ALTER TABLE `zinzam0_zinzam`.`actions` ADD COLUMN `user_id` INTEGER NOT NULL AFTER `action_id`,
 ADD COLUMN `action_access` SMALLINT UNSIGNED NOT NULL AFTER `user_id`,
 ADD COLUMN `action_verb` VARCHAR(15) NOT NULL AFTER `action_access`,
 ADD COLUMN `action_application` INTEGER NOT NULL AFTER `action_verb`,
 ADD COLUMN `action_using` VARCHAR(15) NOT NULL AFTER `action_application`,
 ADD COLUMN `action_primtive_id` BIGINT NOT NULL AFTER `action_using`,
 ADD COLUMN `action_primitive_type` VARCHAR(15) NOT NULL AFTER `action_primtive_id`;
      
     ALTER TABLE `zinzam0_zinzam`.`actions` CHANGE COLUMN `action_primtive_id` `action_primitive_id` BIGINT(20) NOT NULL;
      
     ALTER TABLE `zinzam0_zinzam`.`actions` ADD COLUMN `action_title` VARCHAR(63) NOT NULL AFTER `action_primitive_type`,
 ADD COLUMN `action_body` VARCHAR(255) NOT NULL AFTER `action_title`;

     ALTER TABLE `zinzam0_zinzam`.`actions` DROP COLUMN `user_id`;
     
     ALTER TABLE `zinzam0_zinzam`.`actions` DROP COLUMN `action_access`;

     ALTER TABLE `zinzam0_zinzam`.`actions` DROP COLUMN `action_verb`;

     ALTER TABLE `zinzam0_zinzam`.`actions` DROP COLUMN `action_using`;

     ALTER TABLE `zinzam0_zinzam`.`actions` ADD COLUMN `action_time_ut` BIGINT NOT NULL AFTER `action_body`;

     * 
     * TODO:
     ALTER TABLE `zinzam0_zinzam`.`actions` MODIFY COLUMN `action_body` VARCHAR(511) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL;

     */
    public class Action
    {
        public const string FEED_FIELDS = "at.action_id, at.action_application, at.action_primitive_id, at.action_primitive_type, at.action_title, at.action_body, at.action_time_ut";

        private Mysql db;

        private long actionId;
        private string title;
        private string body;
        private int applicationId;
        private Primitive owner;
        private long primitiveId;
        private long timeRaw;

        public long ActionId
        {
            get
            {
                return actionId;
            }
        }

        public string Title
        {
            get
            {
                if (title.Contains("[/user]"))
                {
                    return title;
                }
                else
                {
                    return string.Format("[user]{0}[/user] {1}",
                        primitiveId, title);
                }
            }
        }

        public string Body
        {
            get
            {
                return body;
            }
        }

        public long OwnerId
        {
            get
            {
                return primitiveId;
            }
        }

        public Action (Mysql db, Primitive owner, DataRow actionRow)
        {
            this.db = db;
            this.owner = owner;

            loadActionRow(actionRow);
        }

        public DateTime GetTime(UnixTime tz)
        {
            return tz.DateTimeFromMysql(timeRaw);
        }

        private void loadActionRow(DataRow actionRow)
        {
            actionId = (long)actionRow["action_id"];
            applicationId = (int)actionRow["action_application"];
            title = (string)actionRow["action_title"];
            body = (string)actionRow["action_body"];
            primitiveId = (long)actionRow["action_primitive_id"];
            timeRaw = (long)actionRow["action_time_ut"];
        }
    }
}
