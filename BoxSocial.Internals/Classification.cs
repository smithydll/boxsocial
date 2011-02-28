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

        public static string BuildClassificationBox(Core core, Classifications classification)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            Template template = new Template(core.Http.TemplatePath, "std.classifications_box.html");

            switch (classification)
            {
                case Classifications.None:
                    template.Parse("IS_NONE", boxChecked);
                    break;
                case Classifications.Everyone:
                    template.Parse("IS_EVERYONE", boxChecked);
                    break;
                case Classifications.Mature:
                    template.Parse("IS_MATURE", boxChecked);
                    break;
                case Classifications.Restricted:
                    template.Parse("IS_RESTRICTED", boxChecked);
                    break;
            }

            return template.ToString();
        }

        public static void ApplyRestrictions(Core core, Classifications classification)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            switch (classification)
            {
                case Classifications.Restricted:
                    if (core.Session.LoggedInMember.Profile.Age < 18)
                    {
                        // TODO: Restricted content notice
                        core.Functions.Generate403();
                        return;
                    }
                    break;
                case Classifications.Mature:
                    if (core.Session.LoggedInMember.Profile.Age < 13)
                    {
                        // TODO: Restricted content notice
                        core.Functions.Generate403();
                        return;
                    }
                    else if (core.Session.LoggedInMember.Profile.Age < 15)
                    {
                        // TODO: click-through message for 13/14 year olds
                        // TODO: Restricted content notice
                        core.Functions.Generate403();
                        return;
                    }
                    break;
            }
        }
    }
}
