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
using System.Text;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Musician
{
    public class Directory
    {
        public static void Show(object sender, ShowPageEventArgs e)
        {
            e.Template.SetTemplate("music_directory");

            //e.Template.Parse("U_FILTER_ALL", page.Group.MemberlistUri);
            e.Template.Parse("U_FILTER_BEGINS_A", GetDirectoryUri(e.Core, "a"));
            e.Template.Parse("U_FILTER_BEGINS_B", GetDirectoryUri(e.Core, "b"));
            e.Template.Parse("U_FILTER_BEGINS_C", GetDirectoryUri(e.Core, "c"));
            e.Template.Parse("U_FILTER_BEGINS_D", GetDirectoryUri(e.Core, "d"));
            e.Template.Parse("U_FILTER_BEGINS_E", GetDirectoryUri(e.Core, "e"));
            e.Template.Parse("U_FILTER_BEGINS_F", GetDirectoryUri(e.Core, "f"));
            e.Template.Parse("U_FILTER_BEGINS_G", GetDirectoryUri(e.Core, "g"));
            e.Template.Parse("U_FILTER_BEGINS_H", GetDirectoryUri(e.Core, "h"));
            e.Template.Parse("U_FILTER_BEGINS_I", GetDirectoryUri(e.Core, "i"));
            e.Template.Parse("U_FILTER_BEGINS_J", GetDirectoryUri(e.Core, "j"));
            e.Template.Parse("U_FILTER_BEGINS_K", GetDirectoryUri(e.Core, "k"));
            e.Template.Parse("U_FILTER_BEGINS_L", GetDirectoryUri(e.Core, "l"));
            e.Template.Parse("U_FILTER_BEGINS_M", GetDirectoryUri(e.Core, "m"));
            e.Template.Parse("U_FILTER_BEGINS_N", GetDirectoryUri(e.Core, "n"));
            e.Template.Parse("U_FILTER_BEGINS_O", GetDirectoryUri(e.Core, "o"));
            e.Template.Parse("U_FILTER_BEGINS_P", GetDirectoryUri(e.Core, "p"));
            e.Template.Parse("U_FILTER_BEGINS_Q", GetDirectoryUri(e.Core, "q"));
            e.Template.Parse("U_FILTER_BEGINS_R", GetDirectoryUri(e.Core, "r"));
            e.Template.Parse("U_FILTER_BEGINS_S", GetDirectoryUri(e.Core, "s"));
            e.Template.Parse("U_FILTER_BEGINS_T", GetDirectoryUri(e.Core, "t"));
            e.Template.Parse("U_FILTER_BEGINS_U", GetDirectoryUri(e.Core, "u"));
            e.Template.Parse("U_FILTER_BEGINS_V", GetDirectoryUri(e.Core, "v"));
            e.Template.Parse("U_FILTER_BEGINS_W", GetDirectoryUri(e.Core, "w"));
            e.Template.Parse("U_FILTER_BEGINS_X", GetDirectoryUri(e.Core, "x"));
            e.Template.Parse("U_FILTER_BEGINS_Y", GetDirectoryUri(e.Core, "y"));
            e.Template.Parse("U_FILTER_BEGINS_Z", GetDirectoryUri(e.Core, "z"));

            List<Musician> musicians = Musician.GetMusicians(e.Core, e.Core.Functions.GetFilter(), e.Page.page);

            foreach (Musician musician in musicians)
            {
                VariableCollection musicianVariableCollection = e.Template.CreateChild("musicians_list");
            }
        }

        private static string GetDirectoryUri(Core core, string filter)
        {
            return GetDirectoryUri(core, filter, null);
        }

        private static string GetDirectoryUri(Core core, string filter, string genrePath)
        {

            if (genrePath == null)
            {
                genrePath = string.Empty;
            }
            if (!genrePath.StartsWith("/"))
            {
                genrePath = "/" + genrePath.TrimEnd(new char[] { '/' });
            }

            return core.Uri.AppendSid(string.Format("music/directory{0}?filter={1}",
                    genrePath, filter));
        }

        public static void ShowGenres(object sender, ShowPageEventArgs e)
        {
            List<MusicGenre> genres = MusicGenre.GetGenres(e.Core);

        }

        public static void ShowGenre(object sender, ShowPageEventArgs e)
        {
            string genre = e.Core.PagePathParts[1].Value;
        }
    }
}
