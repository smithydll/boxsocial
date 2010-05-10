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
            e.Template.SetTemplate("Musician", "music_directory");

            e.Template.Parse("U_MUSIC_ARTISTS", e.Core.Uri.AppendSid("/music/directory/all"));
            e.Template.Parse("U_MUSIC_GENRES", e.Core.Uri.AppendSid("/music/directory/genres"));

            e.Template.Parse("U_FILTER_ALL", e.Core.Uri.AppendSid("/music/directory/all"));
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
            long musicianCount = e.Db.LastQueryRows;

            Dictionary<long, MusicGenre> musicianGenres = MusicGenre.GetGenres(e.Core, musicians);

            foreach (Musician musician in musicians)
            {
                VariableCollection musicianVariableCollection = e.Template.CreateChild("musicians_list");

                musicianVariableCollection.Parse("U_MUSICIAN", musician.Uri);
                musicianVariableCollection.Parse("DISPLAY_NAME", musician.DisplayName);
                musicianVariableCollection.Parse("I_TILE", musician.Tile);
                musicianVariableCollection.Parse("I_ICON", musician.Icon);

                if (musician.GenreRaw > 0 && musicianGenres.ContainsKey(musician.GenreRaw))
                {
                    musicianVariableCollection.Parse("GENRE", musicianGenres[musician.GenreRaw].Name);
                }

                if (musician.SubGenreRaw > 0 && musicianGenres.ContainsKey(musician.SubGenreRaw))
                {
                    musicianVariableCollection.Parse("SUB_GENRE", musicianGenres[musician.SubGenreRaw].Name);
                }
            }

            e.Core.Display.ParsePagination(GetDirectoryUri(e.Core, e.Core.Functions.GetFilter()), e.Page.page, (int)(Math.Ceiling(musicianCount / 10.0)));
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

            return core.Uri.AppendSid(string.Format("/music/directory{0}?filter={1}",
                    genrePath, filter));
        }

        public static void ShowGenres(object sender, ShowPageEventArgs e)
        {
            e.Template.SetTemplate("Musician", "music_directory_genres");

            List<MusicGenre> genres = MusicGenre.GetGenres(e.Core);

            for (int i = 0; i < genres.Count; i++)
            {
                MusicGenre genre = genres[i];
                if (genre.ParentId == 0)
                {
                    VariableCollection genreVariableCollection = e.Template.CreateChild("genre_list");

                    genreVariableCollection.Parse("U_GENRE", genre.Uri);
                    genreVariableCollection.Parse("DISPLAY_NAME", genre.Name);
                    genreVariableCollection.Parse("MUSICIANS", e.Core.Functions.LargeIntegerToString(genre.Musicians));

                    for (int j = i; j < genres.Count; j++)
                    {
                        MusicGenre subGenre = genres[j];
                        if (subGenre.ParentId == genre.Id)
                        {
                            VariableCollection subGenreVariableCollection = genreVariableCollection.CreateChild("subgenre_list");

                            subGenreVariableCollection.Parse("U_SUBGENRE", subGenre.Uri);
                            subGenreVariableCollection.Parse("DISPLAY_NAME", subGenre.Name);
                            subGenreVariableCollection.Parse("MUSICIANS", e.Core.Functions.LargeIntegerToString(subGenre.Musicians));
                        }
                        else if (subGenre.ParentId < genre.Id)
                        {
                            continue;
                        }
                        else
                        {
                            // gone past the end, start the next genre
                            break;
                        }
                    }
                }
                else
                {
                    break;
                }
            }

        }

        public static void ShowGenre(object sender, ShowPageEventArgs e)
        {
            e.Template.SetTemplate("Musician", "music_directory_genres");

            string genre = e.Core.PagePathParts[1].Value;
            MusicGenre genreObject = null;

            try
            {
                genreObject = new MusicGenre(e.Core, genre);
            }
            catch (InvalidMusicGenreException)
            {
                e.Core.Functions.Generate404();
                return;
            }

            VariableCollection genreVariableCollection = e.Template.CreateChild("genre_list");

            genreVariableCollection.Parse("U_GENRE", genreObject.Uri);
            genreVariableCollection.Parse("DISPLAY_NAME", genreObject.Name);
            genreVariableCollection.Parse("MUSICIANS", e.Core.Functions.LargeIntegerToString(genreObject.Musicians));

            if (genreObject.ParentId == 0)
            {
                List<MusicGenre> subGenres = genreObject.GetSubGenres();

                foreach (MusicGenre subGenre in subGenres)
                {
                    VariableCollection subGenreVariableCollection = genreVariableCollection.CreateChild("subgenre_list");

                    subGenreVariableCollection.Parse("U_SUBGENRE", subGenre.Uri);
                    subGenreVariableCollection.Parse("DISPLAY_NAME", subGenre.Name);
                    subGenreVariableCollection.Parse("MUSICIANS", e.Core.Functions.LargeIntegerToString(subGenre.Musicians));
                }
            }

            List<Musician> musicians = genreObject.GetMusicians(e.Core.Functions.GetFilter(), e.Page.page);

            long musicianCount = e.Db.LastQueryRows;

            Dictionary<long, MusicGenre> musicianGenres = MusicGenre.GetGenres(e.Core, musicians);

            foreach (Musician musician in musicians)
            {
                VariableCollection musicianVariableCollection = e.Template.CreateChild("musicians_list");

                musicianVariableCollection.Parse("U_MUSICIAN", musician.Uri);
                musicianVariableCollection.Parse("DISPLAY_NAME", musician.DisplayName);
                musicianVariableCollection.Parse("I_TILE", musician.Tile);
                musicianVariableCollection.Parse("I_ICON", musician.Icon);

                if (musician.GenreRaw > 0 && musicianGenres.ContainsKey(musician.GenreRaw))
                {
                    musicianVariableCollection.Parse("GENRE", musicianGenres[musician.GenreRaw].Name);
                }

                if (musician.SubGenreRaw > 0 && musicianGenres.ContainsKey(musician.SubGenreRaw))
                {
                    musicianVariableCollection.Parse("SUB_GENRE", musicianGenres[musician.SubGenreRaw].Name);
                }
            }

            e.Template.Parse("ARTISTS", musicianCount.ToString());

            e.Core.Display.ParsePagination(GetDirectoryUri(e.Core, e.Core.Functions.GetFilter(), genreObject.Slug), e.Page.page, (int)(Math.Ceiling(musicianCount / 10.0)));
        }
    }
}
