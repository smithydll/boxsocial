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
using System.Data;
using System.Configuration;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Threading;
using System.Web;
using BoxSocial;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    /// <summary>
    /// Handles string manipulation and translations.
    /// 
    /// An application shall have separate language files from the assembly
    /// which can be translated, uploaded, and updated without updating the
    /// application assembly itself. This facilitates community translation
    /// of the application in an efficient manner.
    /// 
    /// The structure shall be
    /// /language/applicationKey/applicationKey.ISO_languageCode.resources
    /// </summary>
    public class Prose
    {

        private static string language;
        private static Dictionary<string, ResourceManager> languageResources;

        public static string Language
        {
            set
            {
                language = value;
                Thread.CurrentThread.CurrentCulture = new CultureInfo(language);
                Thread.CurrentThread.CurrentUICulture = new CultureInfo(language);
            }
        }

        internal static void Initialise(string language)
        {
            Language = language;

            languageResources = new Dictionary<string, ResourceManager>();

            AddApplication("Internals");
        }

        internal static void AddApplication(string key)
        {
            try
            {
                ResourceManager rm = ResourceManager.CreateFileBasedResourceManager(key, "./language/" + key + "/", null);

                languageResources.Add(key, rm);
            }
            catch
            {
                // TODO: throw error loading language
            }
        }

        public static string GetString(string key)
        {
            try
            {
                return languageResources["Internals"].GetString(key);
            }
            catch
            {
                return "<MISSING LANGUAGE KEY>";
            }
        }

        public static string GetString(string applicationKey, string languageKey)
        {
            try
            {
                return languageResources[applicationKey].GetString(languageKey);
            }
            catch
            {
                return "<MISSING LANGUAGE KEY>";
            }
        }

        public static void Close()
        {
            if (languageResources != null)
            {
                foreach (string key in languageResources.Keys)
                {
                    ResourceManager rm = languageResources[key];
                    rm.ReleaseAllResources();
                }
            }
        }
    }
}
