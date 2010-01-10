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
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using System.Threading;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Install
{
    public static class Installer
    {
        static object displayUpdateLock;

        private static bool binary;
        private static string root;
        private static string binRoot;
        private static string imagesRoot;
        private static string stylesRoot;
        private static string scriptsRoot;
        private static string languageRoot;
        private static string domain;
        private static string mysqlRootPassword;
        private static string mysqlWebUser;
        private static string mysqlWebPassword;
        private static string mysqlDatabase;
        private static string adminUsername;
        private static string adminPassword;
        private static string adminEmail;
        private static long adminUid;

        static void Main(string[] args)
        {
            displayUpdateLock = new object();

            ThreadStart threadStart = new ThreadStart(updateDisplayedTime);
            Thread thread = new Thread(threadStart);
            thread.Start();

            PollMenu();

            /* Exit Application */
            thread.Abort();
        }

        static void PollMenu()
        {
            while (true)
            {
                lock (displayUpdateLock)
                {
                    ShowMainMenu();
                    Console.CursorVisible = false;
                }

                ConsoleKeyInfo menuKey = Console.ReadKey(false);

                switch (menuKey.Key)
                {
                    case ConsoleKey.A: // Update Application
                        EnterUpdateApplication();
                        break;
                    case ConsoleKey.B: // Sync Application
                        break;
                    case ConsoleKey.C: // Install Application
                        break;
                    case ConsoleKey.D: // Install Box Social
                        EnterInstallBoxSocial();
                        break;
                    case ConsoleKey.Escape:
                    case ConsoleKey.Q:
                    case ConsoleKey.E: // Exit
                        Console.Clear();
                        return;
                }
            }
        }

        static void ShowMainMenu()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.White;
            Console.SetCursorPosition(5, 5);
            Console.Write("a) Update Application");
            Console.SetCursorPosition(5, 7);
            Console.Write("b) Sync Application");
            Console.SetCursorPosition(5, 9);
            Console.Write("c) Install Application");
            Console.SetCursorPosition(5, 11);
            Console.Write("d) Install Box Social");
            Console.SetCursorPosition(5, 13);
            Console.Write("e) Exit");
        }

        static void EnterUpdateApplication()
        {
            lock (displayUpdateLock)
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.White;

                Console.SetCursorPosition(5, 5);
                Console.Write("Application: ________________");

                Console.SetCursorPosition(5, 7);
                Console.Write("WWW Root: ________________");

                Console.SetCursorPosition(5, 9);
                Console.Write("Domain: ________________");

                Console.SetCursorPosition(5, 11);
                Console.Write("Database: ________________");

                Console.SetCursorPosition(5, 13);
                Console.Write("MySQL Root Password: ________________");

                Console.BackgroundColor = ConsoleColor.White;
                Console.ForegroundColor = ConsoleColor.Black;
                Console.SetCursorPosition(5, 21);
                Console.Write(" Enter ");
                Console.BackgroundColor = ConsoleColor.Black;

                Console.ForegroundColor = ConsoleColor.Green;
            }

            ConsoleKey begin;
            string repo = string.Empty;
            string root = string.Empty;
            string domain = string.Empty;
            string database = string.Empty;
            string mysqlRootPassword = string.Empty;

            do
            {
            lock (displayUpdateLock)
            {
                Console.SetCursorPosition(18, 5);
            }
            repo = getField(false, repo);

            lock (displayUpdateLock)
            {
                Console.SetCursorPosition(15, 7);
            }
            root = getField(false, root);

            lock (displayUpdateLock)
            {
                Console.SetCursorPosition(13, 9);
            }
            domain = getField(false, domain);

            lock (displayUpdateLock)
            {
                Console.SetCursorPosition(15, 11);
            }
            database = getField(false, database);

            lock (displayUpdateLock)
            {
                Console.SetCursorPosition(26, 13);
            }
            mysqlRootPassword = getField(true, mysqlRootPassword);

            Console.SetCursorPosition(5, 21);
            Console.Write(" Enter ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("\t- Begin update process");

            while (true)
            {
                begin = Console.ReadKey(false).Key;
                if (begin == ConsoleKey.Enter)
                {
                    Console.Clear();

                    Console.ForegroundColor = ConsoleColor.White;

                    Console.SetCursorPosition(5, 5);
                    Console.Write("Installing...");

                    Installer.root = root;
                    Installer.mysqlDatabase = database;
                    Installer.mysqlRootPassword = mysqlRootPassword;

                    doUpdate(repo);
                    Console.WriteLine("Application Updated");
                    Console.WriteLine("Press Enter to continue");
                    while (!(Console.ReadKey(true).Key == ConsoleKey.Enter))
                    {
                        Thread.Sleep(100);
                    }
                    return;
                }
                else if (begin == ConsoleKey.Escape)
                {
                    return;
                }
                else if (begin == ConsoleKey.Tab)
                {
                    break;
                }
            }
            }
            while (begin == ConsoleKey.Tab);

        }

        static void EnterInstallBoxSocial()
        {
            lock (displayUpdateLock)
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.White;

                Console.SetCursorPosition(5, 3);
                Console.Write("WWW Root: ________________");

                Console.SetCursorPosition(5, 5);
                Console.Write("Domain: ________________");

                Console.SetCursorPosition(5, 7);
                Console.Write("Database: ________________");

                Console.SetCursorPosition(5, 9);
                Console.Write("Mysql root password: ________________");

                Console.SetCursorPosition(5, 11);
                Console.Write("Mysql User: ________________");

                Console.SetCursorPosition(5, 13);
                Console.Write("Mysql User password: ________________");

                Console.SetCursorPosition(5, 15);
                Console.Write("Administrator username: ________________");

                Console.SetCursorPosition(5, 17);
                Console.Write("Administrator password: ________________");

                Console.SetCursorPosition(5, 19);
                Console.Write("Administrator email: ________________");

                Console.BackgroundColor = ConsoleColor.White;
                Console.ForegroundColor = ConsoleColor.Black;
                Console.SetCursorPosition(5, 21);
                Console.Write(" Enter ");
                Console.BackgroundColor = ConsoleColor.Black;

                Console.ForegroundColor = ConsoleColor.Green;
            }

            ConsoleKey begin;
            string root = string.Empty;
            string domain = string.Empty;
            string database = string.Empty;
            string mysqlRootPassword = string.Empty;
            string mysqlUser = string.Empty;
            string mysqlUserPassword = string.Empty;
            string administrationUserName = string.Empty;
            string administrationPassword = string.Empty;
            string administrationEmail = string.Empty;
            do
            {
                lock (displayUpdateLock)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                }

                lock (displayUpdateLock)
                {
                    Console.SetCursorPosition(15, 3);
                }
                root = getField(false, root);

                lock (displayUpdateLock)
                {
                    Console.SetCursorPosition(13, 5);
                }
                domain = getField(false, domain);

                lock (displayUpdateLock)
                {
                    Console.SetCursorPosition(15, 7);
                }
                database = getField(false, database);

                lock (displayUpdateLock)
                {
                    Console.SetCursorPosition(26, 9);
                }
                mysqlRootPassword = getField(true, mysqlRootPassword);

                lock (displayUpdateLock)
                {
                    Console.SetCursorPosition(17, 11);
                }
                mysqlUser = getField(false, mysqlUser);

                lock (displayUpdateLock)
                {
                    Console.SetCursorPosition(26, 13);
                }
                mysqlUserPassword = getField(true, mysqlUserPassword);

                lock (displayUpdateLock)
                {
                    Console.SetCursorPosition(29, 15);
                }
                administrationUserName = getField(false, administrationUserName);

                lock (displayUpdateLock)
                {
                    Console.SetCursorPosition(29, 17);
                }
                administrationPassword = getField(true, administrationPassword);

                lock (displayUpdateLock)
                {
                    Console.SetCursorPosition(26, 19);
                }
                administrationEmail = getField(false, administrationEmail);

                Console.SetCursorPosition(5, 21);
                Console.Write(" Enter ");
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("\t- Begin installation");

                while (true)
                {
                    begin = Console.ReadKey(false).Key;
                    if (begin == ConsoleKey.Enter)
                    {
                        Console.Clear();

                        Console.ForegroundColor = ConsoleColor.White;

                        Console.SetCursorPosition(5, 5);
                        Console.Write("Installing...");

                        Installer.root = root;
                        Installer.domain = domain;
                        Installer.mysqlDatabase = database;
                        Installer.mysqlRootPassword = mysqlRootPassword;
                        Installer.mysqlWebUser = mysqlUser;
                        Installer.mysqlWebPassword = mysqlUserPassword;
                        Installer.adminUsername = administrationUserName;
                        Installer.adminPassword = administrationPassword;
                        Installer.adminEmail = administrationEmail;

                        doInstall();
                        Console.WriteLine("Box Social Installed");
                        Console.WriteLine("Press Enter to continue");
                        while (!(Console.ReadKey(true).Key == ConsoleKey.Enter))
                        {
                            Thread.Sleep(100);
                        }
                        return;
                    }
                    else if (begin == ConsoleKey.Escape)
                    {
                        return;
                    }
                    else if (begin == ConsoleKey.Tab)
                    {
                        break;
                    }
                }
            }
            while (begin == ConsoleKey.Tab);
        }

        #region Time Thread
        static void updateDisplayedTime()
        {
            while (true)
            {
                lock (displayUpdateLock)
                {
                    int left = Console.CursorLeft;
                    int top = Console.CursorTop;
                    ConsoleColor colour = Console.ForegroundColor;
                    bool cursor = Console.CursorVisible;

                    Console.CursorVisible = false;
                    Console.ForegroundColor = ConsoleColor.White;

                    Console.SetCursorPosition(0, 0);
                    Console.Write("Box Social Installer");
                    Console.SetCursorPosition(0, 1);
                    for (int i = 0; i < Console.WindowWidth; i++)
                    {
                        Console.Write("-");
                    }

                    Console.SetCursorPosition(0, Console.WindowHeight - 2);

                    for (int i = 0; i < Console.WindowWidth; i++)
                    {
                        Console.Write("-");
                    }

                    Console.SetCursorPosition(Console.WindowWidth - 10, 0);
                    Console.Write(DateTime.Now.ToShortTimeString());

                    Console.SetCursorPosition(left, top);
                    Console.ForegroundColor = colour;
                    Console.CursorVisible = cursor;
                }
                Thread.Sleep(100);
            }
        }
        #endregion

        static string getField(bool isPasswordField)
        {
            return getField(isPasswordField, string.Empty);
        }

        static string getField(bool isPasswordField, string currentValue)
        {
            lock (displayUpdateLock)
            {
                Console.CursorVisible = true;
            }

            string field = currentValue;
            if (isPasswordField)
            {
                for (int i = 0; i < field.Length; i++)
                {
                    Console.Write("*");
                }
            }
            else
            {
                Console.Write(field);
            }

            ConsoleKeyInfo key;
            while ((key = Console.ReadKey(true)) != null)
            {
                switch (key.Key)
                {
                    case ConsoleKey.Enter:
                        lock (displayUpdateLock)
                        {
                            Console.CursorVisible = false;
                        }
                        return field;
                    case ConsoleKey.Backspace:
                        field = field.Substring(0, field.Length - 1);
                        lock (displayUpdateLock)
                        {
                            Console.CursorLeft = Console.CursorLeft - 1;
                            Console.Write(" ");
                            Console.CursorLeft = Console.CursorLeft - 1;
                        }
                        break;
                    default:
                        if (!Char.IsControl(key.KeyChar))
                        {
                            field += key.KeyChar;
                            if (isPasswordField)
                            {
                                lock (displayUpdateLock)
                                {
                                    Console.Write("*");
                                }
                            }
                            else
                            {
                                lock (displayUpdateLock)
                                {
                                    Console.Write(key.KeyChar);
                                }
                            }
                        }
                        break;
                }
            }

            lock (displayUpdateLock)
            {
                Console.CursorVisible = false;
            }

            return field;
        }

        static void doUpdate(string repo)
        {
            binRoot = Path.Combine(root, "bin");
            imagesRoot = Path.Combine(root, "images");
            stylesRoot = Path.Combine(Path.Combine(root, "styles"), "applications");
            scriptsRoot = Path.Combine(root, "scripts");

            InstallRepository(repo);
            InstallApplication(repo);
            InstallLanguage("en", repo);

            Console.WriteLine("Reloading apache");
            Process p1 = new Process();
            p1.StartInfo.FileName = "/etc/init.d/apache2";
            p1.StartInfo.Arguments = "force-reload";
            p1.Start();

            p1.WaitForExit();
        }

        static void doInstall()
        {
            binRoot = Path.Combine(root, "bin");
            imagesRoot = Path.Combine(root, "images");
            stylesRoot = Path.Combine(Path.Combine(root, "styles"), "applications");
            scriptsRoot = Path.Combine(root, "scripts");

            PerformInstall();

            Console.WriteLine("Reloading apache");
            Process p1 = new Process();
            p1.StartInfo.FileName = "/etc/init.d/apache2";
            p1.StartInfo.Arguments = "force-reload";
            p1.Start();

            p1.WaitForExit();
        }

        static void goAway(string[] args)
        {
            List<string> argsList = new List<string>(args);
            if (argsList.Contains("--source") || argsList.Contains("-S"))
            {
                binary = false;
            }
            else
            {
                binary = true;
            }

            if (argsList.Contains("--install-lang") || argsList.Contains("-il") && argsList.Count >= 3)
            {
                string lang = string.Empty;
                string repo = string.Empty;
                int langIndex = Math.Max(argsList.IndexOf("-l"), argsList.IndexOf("--lang"));
                int repoIndex = Math.Max(argsList.IndexOf("-r"), argsList.IndexOf("--repo"));
            }

            if (argsList.Contains("--sync") || argsList.Contains("s") && argsList.Count >= 2)
            {
                string mysqlRootPassword = string.Empty;
                string mysqlDatabase = string.Empty;
                int passwordIndex = argsList.IndexOf("-p");
                int databaseIndex = argsList.IndexOf("-d");

                if (passwordIndex > 0 && passwordIndex + 1 < args.Length)
                {
                    mysqlRootPassword = argsList[passwordIndex + 1];
                }

                if (databaseIndex > 0 && databaseIndex + 1 < args.Length)
                {
                    mysqlDatabase = argsList[databaseIndex + 1];
                }

                Mysql db = new Mysql("root", mysqlRootPassword, mysqlDatabase, "localhost");
                Template template = new Template(Path.Combine("/var/www/", "templates"), "default.html");
                Core core = new Core(db, template);
                UnixTime tz = new UnixTime(core, 0);

                SelectQuery query = new SelectQuery(typeof(User));
                DataTable dt = db.Query(query);

                AccessControlPermission acpView = new AccessControlPermission(core, ItemType.GetTypeId(typeof(User)), "VIEW");
                AccessControlPermission acpComment = new AccessControlPermission(core, ItemType.GetTypeId(typeof(User)), "COMMENT");

                /*foreach (DataRow dr in dt.Rows)
                {
                    User user = new User(core, dr, UserLoadOptions.Key);

                    // FRIENDS
                    AccessControlGrant.Create(core, new ItemKey(-1, ItemType.GetTypeId(typeof(Friend))), user.ItemKey, acpView.Id, AccessControlGrants.Allow);
                    AccessControlGrant.Create(core, new ItemKey(-1, ItemType.GetTypeId(typeof(Friend))), user.ItemKey, acpComment.Id, AccessControlGrants.Allow);
                    // EVERYONE
                    AccessControlGrant.Create(core, new ItemKey(-2, ItemType.GetTypeId(typeof(User))), user.ItemKey, acpView.Id, AccessControlGrants.Allow);
                }*/

                query = new SelectQuery(typeof(Page));
                dt = db.Query(query);

                acpView = new AccessControlPermission(core, ItemType.GetTypeId(typeof(Page)), "VIEW");

                foreach (DataRow dr in dt.Rows)
                {
                    Page page = new Page(core, null, dr);

                    // EVERYONE
                    AccessControlGrant.Create(core, User.EveryoneGroupKey, page.ItemKey, acpView.Id, AccessControlGrants.Allow);
                }

                return;
            }

            if (argsList.Contains("--update") || argsList.Contains("u") && argsList.Count >= 2)
            {
                Console.WriteLine("Please enter the root directory of the domain you want to use:");
                root = Console.ReadLine();
                binRoot = Path.Combine(root, "bin");
                imagesRoot = Path.Combine(root, "images");
                stylesRoot = Path.Combine(Path.Combine(root, "styles"), "applications");
                scriptsRoot = Path.Combine(root, "scripts");
                languageRoot = Path.Combine(root, "language");

                Console.WriteLine("Please enter the domain name of the directory you just entered (e.g. zinzam.com, localhost, 127.0.0.1):");
                domain = Console.ReadLine();

                Console.WriteLine("Please enter the mysql root password:");
                mysqlRootPassword = Console.ReadLine();

                Console.WriteLine("Please enter the mysql database you have created:");
                mysqlDatabase = Console.ReadLine();
				
				InstallRepository(argsList[argsList.Count - 1]);
                InstallApplication(argsList[argsList.Count - 1]);

                Console.WriteLine("Reloading apache");
                Process p1 = new Process();
                p1.StartInfo.FileName = "/etc/init.d/apache2";
                p1.StartInfo.Arguments = "force-reload";
                p1.Start();

                p1.WaitForExit();

                Console.WriteLine(argsList[argsList.Count - 1] + " updated successfully.");

                return;
            }

            Console.WriteLine("Box Social will only install into the root directory of a domain. Everything in the root directory will be deleted. Do you want to continue? (y/n)");
            if (Console.ReadLine().ToLower().StartsWith("y"))
            {
                Console.WriteLine("If you do not provide the root directory of a domain, Box Social will not install properly.");
                Console.WriteLine("Please enter the root directory of the domain you want to use:");
                root = Console.ReadLine();
                binRoot = Path.Combine(root, "bin");
                imagesRoot = Path.Combine(root, "images");
                stylesRoot = Path.Combine(Path.Combine(root, "styles"), "applications");
                scriptsRoot = Path.Combine(root, "scripts");

                Console.WriteLine("Please enter the domain name of the directory you just entered (e.g. zinzam.com, localhost, 127.0.0.1):");
                domain = Console.ReadLine();

                Console.WriteLine("Please enter the mysql root password:");
                mysqlRootPassword = Console.ReadLine();

                Console.WriteLine("Please enter the mysql database you have created:");
                mysqlDatabase = Console.ReadLine();

                Console.WriteLine("Please enter the mysql low privledge user name (e.g. web@localhost):");
                mysqlWebUser = Console.ReadLine();

                Console.WriteLine("Please enter the mysql low privledge user password:");
                mysqlWebPassword = Console.ReadLine();

                Console.WriteLine("Please enter administrator username:");
                adminUsername = Console.ReadLine();

                Console.WriteLine("Please enter administrator e-mail address:");
                adminEmail = Console.ReadLine();

                Console.WriteLine("Please enter administrator password:");
                adminPassword = Console.ReadLine();

                // install
                PerformInstall();

                Console.WriteLine("Reloading apache");
                Process p1 = new Process();
                p1.StartInfo.FileName = "/etc/init.d/apache2";
                p1.StartInfo.Arguments = "force-reload";
                p1.Start();

                p1.WaitForExit();

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
            if (Directory.Exists(binRoot))
            {
                Directory.Delete(binRoot, true);
            }

            Directory.CreateDirectory(binRoot);
            Directory.CreateDirectory(Path.Combine(binRoot, "applications"));

            File.Copy("MySql.Data.dll", Path.Combine(binRoot, "MySql.Data.dll"));

            if (Directory.Exists(imagesRoot))
            {
                Directory.Delete(imagesRoot, true);
            }

            Directory.CreateDirectory(imagesRoot);

            /* ==================== */
            if (!binary)
            {
                DownloadRepository(@"BoxSocial.Forms");
                CompileRepository(@"BoxSocial.Forms");
            }
            InstallRepository(@"BoxSocial.Forms");
            /* ==================== */
            InstallRepository(@"BoxSocial.FrontEnd");
            /* ==================== */
            InstallRepository(@"BoxSocial.IO");
            /* ==================== */
            InstallRepository(@"BoxSocial.Internals");
            InstallApplication(@"BoxSocial.Internals");
            /* ==================== */
            InstallRepository(@"Groups");
            InstallApplication(@"Groups");
            /* ==================== */
            InstallRepository(@"Networks");
            InstallApplication(@"Networks");
            /* ==================== */
            InstallRepository(@"Musician");
            InstallApplication(@"Musician");
            /* ==================== */
            InstallRepository(@"Profile");
            InstallApplication(@"Profile");
            /* ==================== */
            InstallRepository(@"Calendar");
            InstallApplication(@"Calendar");
            /* ==================== */
            InstallRepository(@"Gallery");
            InstallApplication(@"Gallery");
            /* ==================== */
            InstallRepository(@"GuestBook");
            InstallApplication(@"GuestBook");
            /* ==================== */
            InstallRepository(@"Pages");
            InstallApplication(@"Pages");
            /* ==================== */
            InstallRepository(@"Blog");
            InstallApplication(@"Blog");
            /* ==================== */
            InstallRepository(@"Forum");
            InstallApplication(@"Forum");
            /* ==================== */
            InstallRepository(@"Mail");
            InstallApplication(@"Mail");
            /* ==================== */
            InstallRepository(@"News");
            InstallApplication(@"News");
            /* ==================== */

            Mysql db = new Mysql("root", mysqlRootPassword, mysqlDatabase, "localhost");
            Template template = new Template(Path.Combine(root, "templates"), "default.html");
            Core core = new Core(db, template);
            UnixTime tz = new UnixTime(core, 0);

            //User anonymous = User.Register(core, "Anonymous", "anonymous@zinzam.com", "Anonymous", "Anonymous");
            // blank out the anon password to make it impossible to login as
            //db.UpdateQuery("UPDATE user_info SET user_password = '' WHERE user_id = " + anonymous.Id + ";");
			CreateUser(core, "Anonymous", "anonymous@zinzam.com", null);

            //User admin = User.Register(core, adminUsername, adminEmail, adminPassword, adminPassword);
            //adminUid = admin.Id;
			long adminId = CreateUser(core, adminUsername, adminEmail, adminPassword);

            db.UpdateQuery("UPDATE applications SET user_id = " + adminId + ";");

            FinaliseApplicationInstall(core, new User(core, adminId), @"Groups");
            FinaliseApplicationInstall(core, new User(core, adminId), @"Networks");
            FinaliseApplicationInstall(core, new User(core, adminId), @"Musician");
            FinaliseApplicationInstall(core, new User(core, adminId), @"Profile");
            FinaliseApplicationInstall(core, new User(core, adminId), @"Calendar");
            FinaliseApplicationInstall(core, new User(core, adminId), @"Gallery");
            FinaliseApplicationInstall(core, new User(core, adminId), @"GuestBook");
            FinaliseApplicationInstall(core, new User(core, adminId), @"Pages");
            FinaliseApplicationInstall(core, new User(core, adminId), @"Blog");
            FinaliseApplicationInstall(core, new User(core, adminId), @"Forum");
            FinaliseApplicationInstall(core, new User(core, adminId), @"Mail");
            FinaliseApplicationInstall(core, new User(core, adminId), @"News");

            // TODO:
            // Fill Countries
            // Fill Categories
            InstallData(core);

            InstallLanguage("en", @"Internals");
            InstallLanguage("en", @"Networks");
            InstallLanguage("en", @"Musician");
            InstallLanguage("en", @"Profile");
            InstallLanguage("en", @"Calendar");
            InstallLanguage("en", @"Gallery");
            InstallLanguage("en", @"GuestBook");
            InstallLanguage("en", @"Pages");
            InstallLanguage("en", @"Blog");
            InstallLanguage("en", @"Forum");
            InstallLanguage("en", @"Mail");
            InstallLanguage("en", @"News");

            InstallGDK();

            db.CloseConnection();
        }

        private static void InstallGDK()
        {
            List<string> images = new List<string>();
            images.Add("add-friend");
            images.Add("background-bottom-centre");
            images.Add("background-bottom-left");
            images.Add("background-bottom-right");
            images.Add("background-middle-centre");
            images.Add("background-middle-left");
            images.Add("background-middle-right");
            images.Add("background-no-repeat");
            images.Add("background-repeat");
            images.Add("background-repeat-x");
            images.Add("background-repeat-y");
            images.Add("background-top-centre");
            images.Add("background-top-left");
            images.Add("background-top-right");
            images.Add("block-user");
            images.Add("move-down");
            images.Add("move-up");
            images.Add("no_picture");
            images.Add("permissions");
            images.Add("posticon_read_med");
            images.Add("posticon_read_med_announce");
            images.Add("posticon_read_med_sticky");
            images.Add("posticon_read_sml");
            images.Add("posticon_unread_med");
            images.Add("posticon_unread_med_announce");
            images.Add("posticon_unread_med_sticky");
            images.Add("posticon_unread_sml");
            images.Add("rating_15");
            images.Add("rating_18");
            images.Add("rating_e");
            images.Add("rotate_left");
            images.Add("rotate_right");

            foreach (string image in images)
            {
                string input = Path.Combine("GDK", image + ".svg");
                string output = Path.Combine(imagesRoot, image + ".png");
                if (File.Exists(input))
                {
                    if (File.Exists(output))
                    {
                        File.Delete(output);
                    }

                    Process p1 = new Process();
                    p1.StartInfo.FileName = "rsvg-convert";
                    p1.StartInfo.Arguments = input + " -o " + output;
                    p1.Start();

                    p1.WaitForExit();
                }
            }
        }

        public static void InstallLanguage(string lang, string repo)
        {
            try
            {
                if (File.Exists(string.Format("{0}.{1}.resources", repo, lang)))
                {
                    string dir = Path.Combine(Installer.languageRoot, repo);

                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }
                    File.Copy(string.Format("{0}.{1}.resources", repo, lang),
                        Path.Combine(dir, string.Format("{0}.{1}.resources", repo, lang)));
                }
            }
            catch
            {
            }
        }

        struct LicenseData
        {
            public string Title;
            public string Uri;
            public string Icon;

            public LicenseData(string title, string uri, string icon)
            {
                this.Title = title;
                this.Uri = uri;
                this.Icon = icon;
            }
        }

        public static void InstallData(Core core)
        {
            // Categories
            Dictionary<string, string> categories = new Dictionary<string, string>();
            categories.Add("uncategorised", "Uncategorised");
            categories.Add("music", "Music");
            categories.Add("cars-and-vehicles", "Cars and Vehicles");
            categories.Add("travel-and-adventure", "Travel and Adventure");
            categories.Add("art-and-culture", "Art and Culture");
            categories.Add("friends-and-family", "Friends and Family");
            categories.Add("people-and-society", "People and Society");
            categories.Add("toys-and-gadgets", "Toys and Gadgets");
            categories.Add("entertainment-and-comedy", "Entertainment and Comedy");
            categories.Add("news-and-politicts", "News and Politicts");
            categories.Add("opinion", "Opinion");
            categories.Add("animals-and-pets", "Animals and Pets");
            categories.Add("sports-and-games", "Sports and Games");
            categories.Add("education", "Education");
            categories.Add("computers-and-the-internet", "Computers and The Internet");
            categories.Add("business-and-employment", "Business and Employment");
            categories.Add("not-for-profit", "Not-for-profit");

            // Countries
            Dictionary<string, string> countries = new Dictionary<string, string>();
            countries.Add("AD", "Andorra");
            countries.Add("AE", "United Arab Emirates");
            countries.Add("AF", "Afghanistan");
            countries.Add("AG", "Antigua and Barbuda");
            countries.Add("AI", "Anguilla");
            countries.Add("AL", "Albania");
            countries.Add("AM", "Armenia");
            countries.Add("AN", "Netherlands Antilles");
            countries.Add("AO", "Angola");
            countries.Add("AQ", "Antartica");
            countries.Add("AR", "Argentina");
            countries.Add("AS", "American Samoa");
            countries.Add("AT", "Austria");
            countries.Add("AU", "Australia");
            countries.Add("AW", "Aruba");
            countries.Add("AZ", "Azerbaijan");
            countries.Add("BA", "Bosnia and Herzegovina");
            countries.Add("BB", "Barbados");
            countries.Add("BD", "Bangladesh");
            countries.Add("BE", "Belgium");
            countries.Add("BF", "Burkina Faso");
            countries.Add("BG", "Bulgaria");
            countries.Add("BH", "Bahrain");
            countries.Add("BI", "Burundi");
            countries.Add("BJ", "Benin");
            countries.Add("BM", "Bermuda");
            countries.Add("BN", "Brunei");
            countries.Add("BO", "Bolivia");
            countries.Add("BR", "Brazil");
            countries.Add("BS", "The Bahamas");
            countries.Add("BT", "Bhutan");
            countries.Add("BV", "Bouvet Island");
            countries.Add("BW", "Botswana");
            countries.Add("BY", "Belarus");
            countries.Add("BZ", "Belize");
            countries.Add("CA", "Canada");
            countries.Add("CC", "Cocos (Keeling) Islands");
            countries.Add("CD", "Congo, Democratic Republic of the");
            countries.Add("CF", "Central African Republic");
            countries.Add("CG", "Congo, Republic of the");
            countries.Add("CH", "Switzerland");
            countries.Add("CI", "Cote d'Ivoire");
            countries.Add("CK", "Cook Islands");
            countries.Add("CL", "Chile");
            countries.Add("CM", "Cameroon");
            countries.Add("CN", "China");
            countries.Add("CO", "Colombia");
            countries.Add("CR", "Costa Rica");
            countries.Add("CU", "Cuba");
            countries.Add("CV", "Cape Verde");
            countries.Add("CX", "Christmas Island");
            countries.Add("CY", "Cyprus");
            countries.Add("CZ", "Czech Republic");
            countries.Add("DE", "Germany");
            countries.Add("DJ", "Djibouti");
            countries.Add("DK", "Denmark");
            countries.Add("DM", "Dominica");
            countries.Add("DO", "Dominican Republic");
            countries.Add("DZ", "Algeria");
            countries.Add("EC", "Ecuador");
            countries.Add("EE", "Estonia");
            countries.Add("EG", "Egypt");
            countries.Add("EH", "Western Sahara");
            countries.Add("ER", "Eritrea");
            countries.Add("ES", "Spain");
            countries.Add("ET", "Ethiopia");
            countries.Add("FI", "Finland");
            countries.Add("FJ", "Fiji");
            countries.Add("FK", "Falkland Islands (Islas Malvinas)");
            countries.Add("FM", "Federated States of Micronesia");
            countries.Add("FO", "Faroe Islands");
            countries.Add("FR", "France");
            countries.Add("FX", "France, Metropolitan");
            countries.Add("GA", "Gabon");
            countries.Add("GB", "United Kingdom");
            countries.Add("GD", "Grenada");
            countries.Add("GE", "Georgia");
            countries.Add("GF", "French Guiana");
            countries.Add("GG", "Guernsey");
            countries.Add("GH", "Ghana");
            countries.Add("GI", "Gibraltar");
            countries.Add("GL", "Greenland");
            countries.Add("GM", "The Gambia");
            countries.Add("GN", "Guinea");
            countries.Add("GP", "Guadeloupe");
            countries.Add("GQ", "Equatorial Guinea");
            countries.Add("GR", "Greece");
            countries.Add("GS", "South Georgia and the Islands");
            countries.Add("GT", "Guatemala");
            countries.Add("GU", "Guam");
            countries.Add("GW", "Guinea-Bissau");
            countries.Add("GY", "Guyana");
            countries.Add("HK", "Hong Kong");
            countries.Add("HM", "Heard Island and McDonald Islands");
            countries.Add("HN", "Honduras");
            countries.Add("HR", "Croatia");
            countries.Add("HT", "Haiti");
            countries.Add("HU", "Hungary");
            countries.Add("ID", "Indonesia");
            countries.Add("IE", "Isle of Man");
            countries.Add("IL", "Israel");
            countries.Add("IN", "India");
            countries.Add("IO", "British Indian Ocean Territory");
            countries.Add("IQ", "Iraq");
            countries.Add("IR", "Iran");
            countries.Add("IS", "Iceland");
            countries.Add("IT", "Italy");
            countries.Add("JE", "Jersey");
            countries.Add("JM", "Jamaica");
            countries.Add("JO", "Jordan");
            countries.Add("JP", "Japan");
            countries.Add("KE", "Kenya");
            countries.Add("KG", "Kyrgyzstan");
            countries.Add("KH", "Cambodia");
            countries.Add("KI", "Kiribati");
            countries.Add("KM", "Comoros");
            countries.Add("KN", "Saint Kitts and Nevis");
            countries.Add("KP", "North Korea");
            countries.Add("KR", "South Korea");
            countries.Add("KW", "Kuwait");
            countries.Add("KY", "Cayman Islands");
            countries.Add("KZ", "Kazakhstan");
            countries.Add("LA", "Laos");
            countries.Add("LB", "Lebanon");
            countries.Add("LC", "Saint Lucia");
            countries.Add("LI", "Liechtenstein");
            countries.Add("LK", "Sri Lanka");
            countries.Add("LR", "Liberia");
            countries.Add("LS", "Lesotho");
            countries.Add("LT", "Lithuania");
            countries.Add("LU", "Luxembourg");
            countries.Add("LV", "Latvia");
            countries.Add("LY", "Libya");
            countries.Add("MA", "Morocco");
            countries.Add("MC", "Monaco");
            countries.Add("MD", "Moldova");
            countries.Add("ME", "Montenegro");
            countries.Add("MG", "Madagascar");
            countries.Add("MH", "Marshall Islands");
            countries.Add("MK", "Macedonia");
            countries.Add("ML", "Mali");
            countries.Add("MM", "Burma");
            countries.Add("MN", "Mongolia");
            countries.Add("MO", "Macau");
            countries.Add("MP", "Northern Mariana Islands");
            countries.Add("MQ", "Martinique");
            countries.Add("MR", "Mauritania");
            countries.Add("MS", "Montserrat");
            countries.Add("MT", "Malta");
            countries.Add("MU", "Mauritius");
            countries.Add("MV", "Maldives");
            countries.Add("MW", "Malawi");
            countries.Add("MX", "Mexico");
            countries.Add("MY", "Malaysia");
            countries.Add("MZ", "Mozambique");
            countries.Add("NA", "Namibia");
            countries.Add("NC", "New Caledonia");
            countries.Add("NE", "Niger");
            countries.Add("NF", "Norfolk Island");
            countries.Add("NG", "Nigeria");
            countries.Add("NI", "Nicaragua");
            countries.Add("NL", "Netherlands");
            countries.Add("NO", "Norway");
            countries.Add("NP", "Nepal");
            countries.Add("NR", "Nauru");
            countries.Add("NU", "Niue");
            countries.Add("NZ", "New Zealand");
            countries.Add("OM", "Oman");
            countries.Add("PA", "Panama");
            countries.Add("PE", "Peru");
            countries.Add("PF", "French Polynesia");
            countries.Add("PG", "Papua New Guinea");
            countries.Add("PH", "Philippines");
            countries.Add("PK", "Pakistan");
            countries.Add("PL", "Poland");
            countries.Add("PM", "Saint Pierre and Miquelon");
            countries.Add("PN", "Pitcairn Islands");
            countries.Add("PR", "Puerto Rico");
            countries.Add("PS", "Gaza Strip");
            countries.Add("PT", "Portugal");
            countries.Add("PW", "Palau");
            countries.Add("PY", "Paraguay");
            countries.Add("QA", "Qatar");
            countries.Add("RE", "Reunion");
            countries.Add("RO", "Romania");
            countries.Add("RS", "Serbia");
            countries.Add("RU", "Russia");
            countries.Add("RW", "Rwanda");
            countries.Add("SA", "Saudi Arabia");
            countries.Add("SB", "Solomon Islands");
            countries.Add("SC", "Seychelles");
            countries.Add("SD", "Sudan");
            countries.Add("SE", "Sweden");
            countries.Add("SG", "Singapore");
            countries.Add("SH", "Saint Helena");
            countries.Add("SI", "Slovenia");
            countries.Add("SJ", "Svalbard");
            countries.Add("SK", "Slovakia");
            countries.Add("SL", "Sierra Leone");
            countries.Add("SM", "San Marino");
            countries.Add("SN", "Senegal");
            countries.Add("SO", "Somalia");
            countries.Add("SR", "Suriname");
            countries.Add("ST", "Sao Tome and Principe");
            countries.Add("SV", "El Salvador");
            countries.Add("SY", "Syria");
            countries.Add("SZ", "Swaziland");
            countries.Add("TC", "Turks and Caicos Islands");
            countries.Add("TD", "Chad");
            countries.Add("TF", "French Southern and Antarctic Lands");
            countries.Add("TG", "Togo");
            countries.Add("TH", "Thailand");
            countries.Add("TJ", "Tajikistan");
            countries.Add("TK", "Tokelau");
            countries.Add("TL", "East Timor");
            countries.Add("TM", "Turkmenistan");
            countries.Add("TN", "Tunisia");
            countries.Add("TO", "Tonga");
            countries.Add("TR", "Turkey");
            countries.Add("TT", "Trinidad and Tobago");
            countries.Add("TV", "Tuvalu");
            countries.Add("TW", "Taiwan");
            countries.Add("TZ", "Tanzania");
            countries.Add("UA", "Ukraine");
            countries.Add("UG", "Uganda");
            countries.Add("UM", "United States Minor Outlying Islands");
            countries.Add("US", "United States");
            countries.Add("UY", "Uruguay");
            countries.Add("UZ", "Uzbekistan");
            countries.Add("VA", "Holy See (Vatican City)");
            countries.Add("VC", "Saint Vincent and the Grenadines");
            countries.Add("VE", "Venezuela");
            countries.Add("VG", "British Virgin Islands");
            countries.Add("VI", "Virgin Islands");
            countries.Add("VN", "Vietnam");
            countries.Add("VU", "Vanuatu");
            countries.Add("WF", "Wallis and Futuna");
            countries.Add("WS", "Samoa");
            countries.Add("YE", "Yemen");
            countries.Add("YT", "Mayotte");
            countries.Add("ZA", "South Africa");
            countries.Add("ZM", "Zambia");
            countries.Add("ZW", "Zimbabwe");

            // Timezones

            // List Types
            List<string> listTypes = new List<string>();
            listTypes.Add("Custom");
            listTypes.Add("Music");
            listTypes.Add("Movies");
            listTypes.Add("TV");
            listTypes.Add("Heroes");
            listTypes.Add("Books");

            // Licenses
            List<LicenseData> licenses = new List<LicenseData>();
            licenses.Add(new LicenseData("GNU Free Document License", "http://www.gnu.org/licenses/fdl.html", ""));
            licenses.Add(new LicenseData("Creative Commons Attribution (3.0)", "http://creativecommons.org/licenses/by/3.0/", "cc-by.png"));
            licenses.Add(new LicenseData("Creative Commons Attribution - Share Alike (3.0)", "http://creativecommons.org/licenses/by-sa/3.0/", "cc-by-sa.png"));
            licenses.Add(new LicenseData("Creative Commons Attribution - Non-commercial (3.0)", "http://creativecommons.org/licenses/by-nc/3.0/", "cc-by-nc.png"));
            licenses.Add(new LicenseData("Creative Commons Attribution - Non-commercial Share Alike (3.0)", "http://creativecommons.org/licenses/by-nc-sa/3.0/", "cc-by-nc-sa.png"));
            licenses.Add(new LicenseData("Creative Commons Attribution - No Derivs (3.0)", "http://creativecommons.org/licenses/by-nd/3.0/", "cc-by-nd.png"));
            licenses.Add(new LicenseData("Creative Commons Attribution - Non-commercial No Derivs (3.0)", "http://creativecommons.org/licenses/by-nc-nd/3.0/", "cc-by-nc-nd.png"));

            /* Install */
            foreach (string key in categories.Keys)
            {
                InsertQuery iQuery = new InsertQuery("global_categories");
                iQuery.AddField("category_path", key);
                iQuery.AddField("category_title", categories[key]);

                core.db.Query(iQuery);
            }

            foreach (string key in countries.Keys)
            {
                InsertQuery iQuery = new InsertQuery("countries");
                iQuery.AddField("country_iso", key);
                iQuery.AddField("country_name", countries[key]);

                core.db.Query(iQuery);
            }

            foreach (string type in listTypes)
            {
                InsertQuery iQuery = new InsertQuery("list_types");
                iQuery.AddField("list_type_title", type);

                core.db.Query(iQuery);
            }

            foreach (LicenseData license in licenses)
            {
                InsertQuery iQuery = new InsertQuery("licenses");
                iQuery.AddField("license_title", license.Title);
                iQuery.AddField("license_link", license.Uri);
                iQuery.AddField("license_icon", license.Icon);

                core.db.Query(iQuery);
            }
        }

        public static void FinaliseApplicationInstall(Core core, User owner, string app)
        {
            ApplicationEntry ae = new ApplicationEntry(core, owner, app);

            try
            {
                ApplicationEntry profileAe = new ApplicationEntry(core, ae, "Profile");
                profileAe.Install(core, owner, ae);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Profile");
                Console.WriteLine(ex.ToString());
            }

            try
            {
                ApplicationEntry guestbookAe = new ApplicationEntry(core, ae, "GuestBook");
                guestbookAe.Install(core, owner, ae);
            }
            catch (Exception ex)
            {
                Console.WriteLine("GuestBook");
                Console.WriteLine(ex.ToString());
            }
        }
		
		public static long CreateUser(Core core, string userName, string email, string password)
		{
            InsertQuery iQuery = new InsertQuery("user_keys");
            iQuery.AddField("user_name", userName);
            iQuery.AddField("user_name_lower", userName.ToLower());
            iQuery.AddField("user_name_first", userName.ToLower()[0]);

            long userId = core.db.Query(iQuery);

			iQuery = new InsertQuery("user_info");
            iQuery.AddField("user_id", userId);
			iQuery.AddField("user_name", userName);
			iQuery.AddField("user_reg_ip", "");
			iQuery.AddField("user_reg_date_ut", UnixTime.UnixTimeStamp().ToString());
			iQuery.AddField("user_last_visit_ut", UnixTime.UnixTimeStamp().ToString());
			if (password != null)
			{
				iQuery.AddField("user_password", User.HashPassword(password));
			}
			else
			{
				iQuery.AddField("user_password", "");
			}
			iQuery.AddField("user_new_password", "");
			iQuery.AddField("user_active", 1);
			iQuery.AddField("user_alternate_email", email);
			iQuery.AddField("user_home_page", "/profile");
			iQuery.AddField("user_show_custom_styles", 1);
			iQuery.AddField("user_email_notifications", 1);
			iQuery.AddField("user_show_bbcode", 0x07);
			iQuery.AddField("user_bytes", 0);
			
			core.db.Query(iQuery);
			
			iQuery = new InsertQuery("user_profile");
			iQuery.AddField("user_id", userId);
            // TODO: ACLs
			
			core.db.Query(iQuery);
			
			iQuery = new InsertQuery("user_emails");
			iQuery.AddField("email_user_id", userId);
			iQuery.AddField("email_email", email);
			iQuery.AddField("email_verified", 1);
					
			iQuery.AddField("email_time_ut", UnixTime.UnixTimeStamp().ToString());
						
			core.db.Query(iQuery);
			
			User newUser = new User(core, userId);
			core.session = new SessionState(core, newUser);
			
			// Install a couple of applications
            try
            {
                ApplicationEntry profileAe = new ApplicationEntry(core, newUser, "Profile");
                profileAe.Install(core, newUser);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Profile");
				Console.WriteLine(ex.ToString());
            }

            try
            {
                ApplicationEntry galleryAe = new ApplicationEntry(core, newUser, "Gallery");
                galleryAe.Install(core, newUser);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Gallery");
                Console.WriteLine(ex.ToString());
            }

            try
            {
                ApplicationEntry guestbookAe = new ApplicationEntry(core, newUser, "GuestBook");
                guestbookAe.Install(core, newUser);
            }
            catch (Exception ex)
            {
                Console.WriteLine("GuestBook");
                Console.WriteLine(ex.ToString());
            }

            try
            {
                ApplicationEntry groupsAe = new ApplicationEntry(core, newUser, "Groups");
                groupsAe.Install(core, newUser);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Groups");
                Console.WriteLine(ex.ToString());
            }

            try
            {
                ApplicationEntry networksAe = new ApplicationEntry(core, newUser, "Networks");
                networksAe.Install(core, newUser);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Networks");
                Console.WriteLine(ex.ToString());
            }

            try
            {
                ApplicationEntry calendarAe = new ApplicationEntry(core, newUser, "Calendar");
                calendarAe.Install(core, newUser);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Calendar");
                Console.WriteLine(ex.ToString());
            }

            Access.CreateGrantForPrimitive(core, newUser, User.EveryoneGroupKey, "VIEW");

            return userId;
		}

        private static void DownloadRepository(string repo)
        {
        }

        private static void CompileRepository(string repo)
        {
        }

        private static void InstallRepository(string repo)
        {
            string filePath = string.Empty;
            switch (repo)
            {
                case "BoxSocial.Forms":
                case "BoxSocial.FrontEnd":
                case "BoxSocial.Internals":
                case "BoxSocial.IO":
                case "Groups":
                case "Networks":
                case "Musician":
                    filePath = Path.Combine(binRoot, repo + ".dll");
                    break;
                default:
                    filePath = Path.Combine(Path.Combine(binRoot, "applications"), repo + ".dll");
                    break;
            }

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            File.Copy(repo + ".dll", filePath);
        }

        private static void InstallApplication(string repo)
        {
            Mysql db = new Mysql("root", mysqlRootPassword, mysqlDatabase, "localhost");
            Template template = new Template(Path.Combine(root, "templates"), "default.html");
            Core core = new Core(db, template);
            UnixTime tz = new UnixTime(core, 0);

            string assemblyPath = null;
            bool isPrimitive = false;
            bool isInternals = false;
            switch (repo)
            {
                case "BoxSocial.Internals":
                    assemblyPath = Path.Combine(binRoot, repo + ".dll");
                    isInternals = true;
                    isPrimitive = false;
                    break;
                case "Groups":
                case "Networks":
                case "Musician":
                    assemblyPath = Path.Combine(binRoot, repo + ".dll");
                    isInternals = false;
                    isPrimitive = true;
                    break;
                default:
                    assemblyPath = Path.Combine(Path.Combine(binRoot, "applications"), repo + ".dll");
                    isInternals = false;
                    isPrimitive = false;
                    break;
            }

            Assembly loadApplication = Assembly.LoadFrom(assemblyPath);

            if (isInternals)
            {
                BoxSocial.Internals.Application.InstallTables(core, loadApplication);
                BoxSocial.Internals.Application.InstallTypes(core, loadApplication, 0);
                
                Type[] types = loadApplication.GetTypes();
                foreach (Type t in types)
                {
                    //if (t.GetInterfaces().
                    List<PermissionInfo> permissions = AccessControlLists.GetPermissionInfo(t);
                    
                    foreach (PermissionInfo pi in permissions)
                    {
                        try
                        {
                            ItemType it = new ItemType(core, t.FullName);
                            try
                            {
                                AccessControlPermission acp = new AccessControlPermission(core, it.Id, pi.Key);
                            }
                            catch (InvalidAccessControlPermissionException)
                            {
                                AccessControlPermission.Create(core, it.Id, pi.Key, pi.Description, pi.PermissionType);
                            }
                        }
                        catch (InvalidItemTypeException)
                        {
                        }
                    }
                }

                Console.WriteLine(repo + " has been installed.");
            }
            else
            {
                Type[] types = loadApplication.GetTypes();
                foreach (Type type in types)
                {
                    if (type.IsSubclassOf(typeof(Application)))
                    {
                        BoxSocial.Internals.Application newApplication = System.Activator.CreateInstance(type, new object[] { core }) as Application;

                        if (newApplication != null)
                        {
                            long updatedRaw = UnixTime.UnixTimeStamp();
                            long applicationId = 0;

                            /*DataTable applicationTable = db.Query(string.Format(@"SELECT {0}
                            FROM applications ap
                            WHERE application_assembly_name = '{1}'",
                                ApplicationEntry.APPLICATION_FIELDS, Mysql.Escape(repo)));*/

                            SelectQuery query1 = Item.GetSelectQueryStub(typeof(ApplicationEntry));
                            query1.AddCondition("application_assembly_name", repo);

                            DataTable applicationTable = db.Query(query1);

                            if (applicationTable.Rows.Count == 1)
                            {
                                ApplicationEntry updateApplication = new ApplicationEntry(core, applicationTable.Rows[0]);
                                applicationId = updateApplication.ApplicationId;

                                //
                                // Save Icon
                                //
                                if (newApplication.Icon != null)
                                {
                                    if (!Directory.Exists(Path.Combine(imagesRoot, updateApplication.Key)))
                                    {
                                        Directory.CreateDirectory(Path.Combine(imagesRoot, updateApplication.Key));
                                    }

                                    newApplication.Icon.Save(Path.Combine(Path.Combine(imagesRoot, updateApplication.Key), "icon.png"), System.Drawing.Imaging.ImageFormat.Png);
                                }

                                //
                                // Render SVG base Icon and thumbnail
                                //
                                if (newApplication.SvgIcon != null)
                                {
                                    if (!Directory.Exists(Path.Combine(imagesRoot, updateApplication.Key)))
                                    {
                                        Directory.CreateDirectory(Path.Combine(imagesRoot, updateApplication.Key));
                                    }

                                    string input = Path.Combine("GDK", updateApplication.Key + ".svg");
                                    string output = Path.Combine(Path.Combine(imagesRoot, updateApplication.Key), "icon.png");
                                    string thumbOutput = Path.Combine(Path.Combine(imagesRoot, updateApplication.Key), "thumb.png");
                                    string tileOutput = Path.Combine(Path.Combine(imagesRoot, updateApplication.Key), "tile.png");

                                    FileStream fs = new FileStream(input, FileMode.Create);
                                    fs.Write(newApplication.SvgIcon, 0, newApplication.SvgIcon.Length);
                                    fs.Close();

                                    if (File.Exists(input))
                                    {
                                        if (File.Exists(output))
                                        {
                                            File.Delete(output);
                                        }
                                        if (File.Exists(thumbOutput))
                                        {
                                            File.Delete(thumbOutput);
                                        }

                                        Process p1 = new Process();
                                        p1.StartInfo.FileName = "rsvg-convert";
                                        p1.StartInfo.Arguments = input + " -o " + output;
                                        p1.Start();

                                        p1.WaitForExit();

                                        p1 = new Process();
                                        p1.StartInfo.FileName = "rsvg-convert";
                                        p1.StartInfo.Arguments = input + " -w 160 -d 160 -o " + thumbOutput;
                                        p1.Start();

                                        p1.WaitForExit();

                                        p1 = new Process();
                                        p1.StartInfo.FileName = "rsvg-convert";
                                        p1.StartInfo.Arguments = input + " -w 50 -d 50 -o " + tileOutput;
                                        p1.Start();

                                        p1.WaitForExit();
                                    }
                                }

                                //
                                // Save StyleSheet
                                //
                                if (!string.IsNullOrEmpty(newApplication.StyleSheet))
                                {
                                    if (!Directory.Exists(stylesRoot))
                                    {
                                        Directory.CreateDirectory(stylesRoot);
                                    }

                                    SaveTextFile(newApplication.StyleSheet, Path.Combine(stylesRoot, updateApplication.Key + ".css"));
                                }

                                //
                                // Save JavaScript
                                //
                                if (!string.IsNullOrEmpty(newApplication.JavaScript))
                                {
                                    SaveTextFile(newApplication.JavaScript, Path.Combine(scriptsRoot, updateApplication.Key + ".js"));
                                }

                                UpdateQuery query = new UpdateQuery("applications");
                                query.AddField("application_title", newApplication.Title);
                                query.AddField("application_description", newApplication.Description);
                                query.AddField("application_primitive", isPrimitive);
                                query.AddField("application_primitives", (byte)newApplication.GetAppPrimitiveSupport());
                                query.AddField("application_comment", newApplication.UsesComments);
                                query.AddField("application_rating", newApplication.UsesRatings);
                                query.AddField("application_style", !string.IsNullOrEmpty(newApplication.StyleSheet));
                                query.AddField("application_script", !string.IsNullOrEmpty(newApplication.JavaScript));
                                query.AddField("application_icon", string.Format(@"/images/{0}/icon.png", updateApplication.Key));
                                query.AddField("application_thumb", string.Format(@"/images/{0}/thumb.png", updateApplication.Key));
                                query.AddField("application_tile", string.Format(@"/images/{0}/tile.png", updateApplication.Key));
                                query.AddCondition("application_assembly_name", repo);

                                db.BeginTransaction();
                                db.Query(query);

                            }
                            else
                            {
                                InsertQuery query = new InsertQuery("applications");
                                query.AddField("application_assembly_name", repo);
                                query.AddField("user_id", 0);
                                query.AddField("application_date_ut", UnixTime.UnixTimeStamp());
                                query.AddField("application_title", newApplication.Title);
                                query.AddField("application_description", newApplication.Description);
                                query.AddField("application_primitive", isPrimitive);
                                query.AddField("application_primitives", (byte)newApplication.GetAppPrimitiveSupport());
                                query.AddField("application_comment", newApplication.UsesComments);
                                query.AddField("application_rating", newApplication.UsesRatings);
                                query.AddField("application_style", !string.IsNullOrEmpty(newApplication.StyleSheet));
                                query.AddField("application_script", !string.IsNullOrEmpty(newApplication.JavaScript));
                                query.AddField("application_icon", string.Format(@"/images/{0}/icon.png", repo));
                                query.AddField("application_thumb", string.Format(@"/images/{0}/thumb.png", repo));
                                query.AddField("application_tile", string.Format(@"/images/{0}/tile.png", repo));

                                /*applicationId = db.UpdateQuery(string.Format(@"INSERT INTO applications (application_assembly_name, user_id, application_date_ut, application_title, application_description, application_primitive, application_primitives, application_comment, application_rating) VALUES ('{0}', {1}, {2}, '{3}', '{4}', {5}, {6}, {7}, {8});",
                                    Mysql.Escape(repo), 0, tz.GetUnixTimeStamp(tz.Now), Mysql.Escape(newApplication.Title), Mysql.Escape(newApplication.Description), isPrimitive, (byte)newApplication.GetAppPrimitiveSupport(), newApplication.UsesComments, newApplication.UsesRatings));*/

                                applicationId = db.Query(query);

                                ApplicationEntry updateApplication = new ApplicationEntry(core, applicationId);

                                /*try
                                {
                                    ApplicationEntry profileAe = new ApplicationEntry(core, null, "Profile");
                                    db.UpdateQuery(string.Format(@"INSERT INTO primitive_apps (application_id, item_id, item_type_id) VALUES ({0}, {1}, '{2}');",
                                        profileAe.ApplicationId, applicationId, ItemKey.GetTypeId(typeof(ApplicationEntry))));
                                }
                                catch
                                {
                                }

                                try
                                {
                                    ApplicationEntry guestbookAe = new ApplicationEntry(core, null, "GuestBook");
                                    db.UpdateQuery(string.Format(@"INSERT INTO primitive_apps (application_id, item_id, item_type_id) VALUES ({0}, {1}, '{2}');",
                                        guestbookAe.ApplicationId, applicationId, ItemKey.GetTypeId(typeof(ApplicationEntry))));
                                }
                                catch
                                {
                                }*/

                                //
                                // Save Icon
                                //
                                if (newApplication.Icon != null)
                                {
                                    if (!Directory.Exists(Path.Combine(imagesRoot, updateApplication.Key)))
                                    {
                                        Directory.CreateDirectory(Path.Combine(imagesRoot, updateApplication.Key));
                                    }

                                    newApplication.Icon.Save(Path.Combine(Path.Combine(imagesRoot, updateApplication.Key), "icon.png"), System.Drawing.Imaging.ImageFormat.Png);
                                }

                                //
                                // Render SVG base Icon and thumbnail
                                //
                                if (newApplication.SvgIcon != null)
                                {
                                    if (!Directory.Exists(Path.Combine(imagesRoot, updateApplication.Key)))
                                    {
                                        Directory.CreateDirectory(Path.Combine(imagesRoot, updateApplication.Key));
                                    }

                                    string input = Path.Combine("GDK", updateApplication.Key + ".svg");
                                    string output = Path.Combine(Path.Combine(imagesRoot, updateApplication.Key), "icon.png");
                                    string thumbOutput = Path.Combine(Path.Combine(imagesRoot, updateApplication.Key), "thumb.png");
                                    string tileOutput = Path.Combine(Path.Combine(imagesRoot, updateApplication.Key), "tile.png");

                                    FileStream fs = new FileStream(input, FileMode.Create);
                                    fs.Write(newApplication.SvgIcon, 0, newApplication.SvgIcon.Length);
                                    fs.Close();

                                    if (File.Exists(input))
                                    {
                                        if (File.Exists(output))
                                        {
                                            File.Delete(output);
                                        }
                                        if (File.Exists(thumbOutput))
                                        {
                                            File.Delete(thumbOutput);
                                        }

                                        Process p1 = new Process();
                                        p1.StartInfo.FileName = "rsvg-convert";
                                        p1.StartInfo.Arguments = input + " -o " + output;
                                        p1.Start();

                                        p1.WaitForExit();

                                        p1 = new Process();
                                        p1.StartInfo.FileName = "rsvg-convert";
                                        p1.StartInfo.Arguments = input + " -w 160 -d 160 -o " + thumbOutput;
                                        p1.Start();

                                        p1.WaitForExit();

                                        p1 = new Process();
                                        p1.StartInfo.FileName = "rsvg-convert";
                                        p1.StartInfo.Arguments = input + " -w 50 -d 50 -o " + tileOutput;
                                        p1.Start();

                                        p1.WaitForExit();
                                    }
                                }

                                //
                                // Save StyleSheet
                                //
                                if (!string.IsNullOrEmpty(newApplication.StyleSheet))
                                {
                                    if (!Directory.Exists(stylesRoot))
                                    {
                                        Directory.CreateDirectory(stylesRoot);
                                    }

                                    SaveTextFile(newApplication.StyleSheet, Path.Combine(stylesRoot, updateApplication.Key + ".css"));
                                }

                                //
                                // Save JavaScript
                                //
                                if (!string.IsNullOrEmpty(newApplication.JavaScript))
                                {
                                    SaveTextFile(newApplication.JavaScript, Path.Combine(scriptsRoot, updateApplication.Key + ".js"));
                                }
                            }

                            if (applicationId > 0)
                            {
                                ApplicationInstallationInfo aii = newApplication.Install();

                                if (aii.ApplicationSlugs != null)
                                {
                                    foreach (ApplicationSlugInfo slug in aii.ApplicationSlugs)
                                    {
                                        if (db.UpdateQuery(string.Format(@"UPDATE application_slugs SET slug_primitives = {0}, slug_updated_ut = {1} WHERE slug_stub = '{2}' AND slug_slug_ex = '{3}' AND application_id = {4}",
                                            (byte)slug.Primitives, updatedRaw, Mysql.Escape(slug.Stub), Mysql.Escape(slug.SlugEx), applicationId)) != 1)
                                        {
                                            /*db.UpdateQuery(string.Format(@"INSERT INTO application_slugs (slug_stub, slug_slug_ex, application_id, slug_primitives, slug_updated_ut) VALUES ('{0}', '{1}', {2}, {3}, {4});",
                                                Mysql.Escape(slug.Stub), Mysql.Escape(slug.SlugEx), applicationId, (byte)slug.Primitives, updatedRaw));*/
                                            ApplicationSlug.Create(core, applicationId, slug);
                                        }
                                    }
                                }

                                if (aii.ApplicationModules != null)
                                {
                                    foreach (ApplicationModule module in aii.ApplicationModules)
                                    {
                                        if (db.UpdateQuery(string.Format(@"UPDATE account_modules SET module_updated_ut = {0} WHERE module_module = '{1}' AND application_id = {2};",
                                            updatedRaw, Mysql.Escape(module.Slug), applicationId)) != 1)
                                        {
                                            db.UpdateQuery(string.Format(@"INSERT INTO account_modules (module_module, application_id, module_updated_ut) VALUES ('{0}', {1}, {2});",
                                                Mysql.Escape(module.Slug), applicationId, updatedRaw));
                                        }
                                    }
                                }

                                /*if (aii.ApplicationCommentTypes != null)
                                {
                                    foreach (ApplicationCommentType ct in aii.ApplicationCommentTypes)
                                    {
                                        if (db.UpdateQuery(string.Format(@"UPDATE comment_types SET type_updated_ut = {0} WHERE type_type = '{1}' AND application_id = {2};",
                                            updatedRaw, Mysql.Escape(ct.Type), applicationId)) != 1)
                                        {
                                            db.UpdateQuery(string.Format(@"INSERT INTO comment_types (type_type, application_id, type_updated_ut) VALUES ('{0}', {1}, {2});",
                                                Mysql.Escape(ct.Type), applicationId, updatedRaw));
                                        }
                                    }
                                }*/

                                /*if (aii.ApplicationItemAccessPermissions != null)
                                {
                                    foreach (ApplicationItemAccessPermissions iap in aii.ApplicationItemAccessPermissions)
                                    {
                                        try
                                        {
                                            AccessControlPermission acp = new AccessControlPermission(core, iap.TypeId, iap.PermissionName);
                                        }
                                        catch (InvalidAccessControlPermissionException)
                                        {
                                            AccessControlPermission.Create(core, iap.TypeId, iap.PermissionName);
                                        }
                                    }
                                }*/
                                
                                // TODO Permissions

                                db.UpdateQuery(string.Format(@"DELETE FROM application_slugs WHERE application_id = {0} AND slug_updated_ut <> {1};",
                                    applicationId, updatedRaw));

                                db.UpdateQuery(string.Format(@"DELETE FROM account_modules WHERE application_id = {0} AND module_updated_ut <> {1};",
                                    applicationId, updatedRaw));

                                /*db.UpdateQuery(string.Format(@"DELETE FROM comment_types WHERE application_id = {0} AND type_updated_ut <> {1};",
                                    applicationId, updatedRaw));*/

                                BoxSocial.Internals.Application.InstallTypes(core, loadApplication, applicationId);
                                BoxSocial.Internals.Application.InstallTables(core, loadApplication);
                                
                                //Type[] types = loadApplication.GetTypes();
                                foreach (Type t in types)
                                {
                                    //if (t.GetInterfaces().
                                    List<PermissionInfo> permissions = AccessControlLists.GetPermissionInfo(t);
                                    
                                    foreach (PermissionInfo pi in permissions)
                                    {
                                        try
                                        {
                                            ItemType it = new ItemType(core, t.FullName);
                                            try
                                            {
                                                AccessControlPermission acp = new AccessControlPermission(core, it.Id, pi.Key);
                                            }
                                            catch (InvalidAccessControlPermissionException)
                                            {
                                                AccessControlPermission.Create(core, it.Id, pi.Key, pi.Description, pi.PermissionType);
                                            }
                                        }
                                        catch (InvalidItemTypeException)
                                        {
                                        }
                                    }
                                }

                            }
                            else
                            {
                                Console.WriteLine("error installing" + repo);
                            }
                        }
                    }
                }

                Console.WriteLine(repo + " has been installed.");

            }

            db.CloseConnection();
        }

        /// <summary>
        /// Save a text file
        /// </summary>
        /// <param name="fileToSave"></param>
        /// <param name="fileName"></param>
        private static void SaveTextFile(string fileToSave, string fileName)
        {
            StreamWriter myStreamWriter = File.CreateText(fileName);
            myStreamWriter.Write(fileToSave);
            myStreamWriter.Close();
        }
    }
}
