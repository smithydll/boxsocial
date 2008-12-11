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
using System.IO;
using System.Text;

namespace BoxSocial.Install
{
    public static class Installer
    {

        private static bool binary;
        private static string root;
        private static string domain;

        static void Main(string[] args)
        {
            List<string> argsList = new List<string>(args);
            if (argsList.Contains("-b"))
            {
                binary = true;
            }
            else
            {
                binary = false;
            }

            Console.WriteLine("Box Social will only install into the root directory of a domain. Everything in the root directory will be deleted. Do you want to continue? (y/n)");
            if (Console.ReadLine().ToLower().StartsWith("y"))
            {
                Console.WriteLine("If you do not provide the root directory of a domain, Box Social will not install properly.");
                Console.WriteLine("Please enter the root directory of the domain you want to use:");
                root = Console.ReadLine();

                Console.WriteLine("Please enter the domain name of the directory you just entered (e.g. zinzam.com, localhost, 127.0.0.1):");
                domain = Console.ReadLine();

                // install
                PerformInstall();

                Console.WriteLine("Box Social installed successfully.");
                return;
            }
            else
            {
                Console.WriteLine("Installation of Box Social aborted.");
                return;
            }
        }

        static void PerformInstall()
        {
            if (!binary)
            {
                DownloadRepository(@"BoxSocial.Forms");
                CompileRepository(@"BoxSocial.Forms");
            }
            InstallRepository(@"BoxSocial.Forms");
        }

        private static void DownloadRepository(string repo)
        {
        }

        private static void CompileRepository(string repo)
        {
        }

        private static void InstallRepository(string repo)
        {
            File.Copy(Path.Combine(repo, repo + ".dll"), Path.Combine(root, repo + ".dll"));
        }
    }
}
