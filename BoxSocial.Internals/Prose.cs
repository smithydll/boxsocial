﻿/*
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
using System.Data;
using System.Configuration;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
    /// 
    /// A resource file is generated by resgen.exe (distributed with Visual
    /// Studio, and mono).
    /// </summary>
    public class Prose : IProse
    {

        private Core core;
        private string language;
        private CultureInfo culture;
        private Dictionary<string, ResourceManager> languageResources;
        private static Object stringCacheLock = new object();
        private static Dictionary<string, string> stringCache = new Dictionary<string,string>(StringComparer.Ordinal);

        public string Language
        {
            set
            {
                language = value;
                culture = new CultureInfo(language);

                if (language == "en")
                {
                    // We cannot set the thread to a neutral culture,
                    // but never mind, en-au is a pretty good compromise
                    Thread.CurrentThread.CurrentCulture = new CultureInfo("en-au");
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-au");
                }
                else
                {
                    Thread.CurrentThread.CurrentCulture = culture;
                    Thread.CurrentThread.CurrentUICulture = culture;
                }
            }
            get
            {
                return language;
            }
        }

        internal void Initialise(Core core, string language)
        {
            this.core = core;
            Language = language;

            languageResources = new Dictionary<string, ResourceManager>(StringComparer.Ordinal);

            AddApplication("Internals");
        }

        internal void AddApplication(string key)
        {
            try
            {
                if (!languageResources.ContainsKey(key))
                {
                    ResourceManager rm = ResourceManager.CreateFileBasedResourceManager(key, Path.Combine(core.Http.LanguagePath, key), null);

                    languageResources.Add(key, rm);
                }
            }
            catch
            {
                // TODO: throw error loading language
            }
        }

        /// <summary>
        /// From Internals
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string GetString(string key)
        {
            try
            {
                string value = string.Empty;

                lock (stringCacheLock)
                {
                    if (!stringCache.TryGetValue("Internals" + "-" + culture + "." + key, out value))
                    {
                        foreach (string akey in languageResources.Keys)
                        {
                            if (stringCache.TryGetValue(akey + "-" + culture + "." + key, out value))
                            {
                                return value;
                            }
                        }

                        value = languageResources["Internals"].GetString(key, culture);
                        stringCache.Add("Internals" + "-" + culture + "." + key, value);
                    }
                }

                return value;
            }
            catch
            {
                foreach (string akey in languageResources.Keys)
                {
                    string value = string.Empty;

                    if (!stringCache.TryGetValue(akey + "-" + culture + "." + key, out value))
                    {
                        try
                        {
                            value = languageResources[akey].GetString(key, culture);
                            stringCache.Add(akey + "-" + culture + "." + key, value);
                            return value;
                        }
                        catch
                        {
                        }
                    }
                }
            }
            return "<MISSING LANGUAGE KEY>";
        }

        /// <summary>
        /// From Internals
        /// </summary>
        /// <param name="key"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public string GetString(string key, params object[] param)
        {
            return string.Format(GetString(key), param);
        }

        /// <summary>
        /// Gets a language string for a specific language
        /// </summary>
        /// <param name="applicationKey"></param>
        /// <param name="languageKey"></param>
        /// <returns></returns>
        public string GetString(string applicationKey, string languageKey)
        {
            try
            {
                string value = string.Empty;

                lock (stringCacheLock)
                {
                    if (!stringCache.TryGetValue(applicationKey + "-" + culture + "." + languageKey, out value))
                    {
                        value = languageResources[applicationKey].GetString(languageKey, culture);
                        stringCache.Add(applicationKey + "-" + culture + "." + languageKey, value);
                    }
                }
                return value;
            }
            catch
            {
                return "<MISSING LANGUAGE KEY>";
            }
        }

        /// <summary>
        /// Gets a language string for a specific language
        /// </summary>
        /// <param name="applicationKey"></param>
        /// <param name="languageKey"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public string GetString(string applicationKey, string languageKey, params object[] param)
        {
            return string.Format(GetString(applicationKey, languageKey), param);
        }

        /// <summary>
        /// Close the resource files
        /// </summary>
        public void Close()
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

        /// <summary>
        /// Queries Internals
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool ContainsKey(string key)
        {
            try
            {
                if (string.IsNullOrEmpty(languageResources["Internals"].GetString(key)))
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public bool TryGetString(string key, out string value)
        {
            try
            {
                value = languageResources["Internals"].GetString(key);
                if (string.IsNullOrEmpty(value))
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            catch
            {
                value = null;
                return false;
            }
        }

        /// <summary>
        /// Queries the specified application
        /// </summary>
        /// <param name="applicationKey"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool ContainsKey(string applicationKey, string key)
        {
            try
            {
                if (string.IsNullOrEmpty(languageResources[applicationKey].GetString(key)))
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
