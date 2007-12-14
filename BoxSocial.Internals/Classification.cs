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
using System.Collections.Generic;
using System.Web;
using System.Text;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    /*
     * DONE:
     * ALTER TABLE `zinzam0_zinzam`.`user_pages` ADD COLUMN `page_classification` TINYINT UNSIGNED NOT NULL AFTER `page_modified_ut`;
     * ALTER TABLE `zinzam0_zinzam`.`user_pages` MODIFY COLUMN `page_classification` TINYINT(3) UNSIGNED NOT NULL DEFAULT 0;
     */
    public enum Classifications : byte
    {
        None = 0,
        Everyone = 1,
        Mature = 3,
        Restricted = 5,
    }

    public sealed class Classification
    {
        private static string boxChecked = " checked=\"checked\"";

        public static string BuildClassificationBox(Classifications classification)
        {
            Template template = new Template("std.classifications_box.html");

            switch (classification)
            {
                case Classifications.None:
                    template.ParseVariables("IS_NONE", boxChecked);
                    break;
                case Classifications.Everyone:
                    template.ParseVariables("IS_EVERYONE", boxChecked);
                    break;
                case Classifications.Mature:
                    template.ParseVariables("IS_MATURE", boxChecked);
                    break;
                case Classifications.Restricted:
                    template.ParseVariables("IS_RESTRICTED", boxChecked);
                    break;
            }

            return template.ToString();
        }

        public static Classifications RequestClassification()
        {
            return (Classifications)byte.Parse(HttpContext.Current.Request.Form["classification"]);
        }

        public static void ApplyRestrictions(Core core, Classifications classification)
        {
            switch (classification)
            {
                case Classifications.Restricted:
                    if (core.session.LoggedInMember.Age < 18)
                    {
                        // TODO: Restricted content notice
                        Functions.Generate403(core);
                        return;
                    }
                    break;
                case Classifications.Mature:
                    if (core.session.LoggedInMember.Age < 13)
                    {
                        // TODO: Restricted content notice
                        Functions.Generate403(core);
                        return;
                    }
                    else if (core.session.LoggedInMember.Age < 15)
                    {
                        // TODO: click-through message for 13/14 year olds
                        // TODO: Restricted content notice
                        Functions.Generate403(core);
                        return;
                    }
                    break;
            }
        }
    }
}
