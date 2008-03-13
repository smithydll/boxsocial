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

namespace BoxSocial.Services.Installer
{
    public class Installer
    {

        public static void Main(string[] args)
        {
            //
            // Performs the following
            //
            // 1. Queries for a list of applications that need to be installed
            // 2. Downloads application source code from
            //

            DateTime lastUpdated = DateTime.Now;

            for (; ; )
            {
                if (DateTime.Now.Subtract(lastUpdated).Hours > 2)
                {
                    lastUpdated = DateTime.Now;
                    // UpdateAll();
                }
                else
                {
                    // Block the thread for 30 minutes
                    System.Threading.Thread.Sleep(new TimeSpan(0, 30, 0));
                }
            }
        }

        private static void InstallAllApplications()
        {
        }

        private static void InstallApplication()
        {
        }

        private static void DownloadApplicationSource()
        {
        }

        private static void CompileApplication()
        {
        }
    }
}
