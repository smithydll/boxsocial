/*
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
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Data;
using System.Net;
using System.Xml;
using System.Xml.Schema;
using System.Xml.XPath;
using BoxSocial.IO;

namespace BoxSocial.Services.Installer
{
    public class Installer
    {
        private static AppDomain domain = AppDomain.CreateDomain("BoxSocialInstallerDomain");

        private static string svnPath;
        private static string cscPath;
        private static string islPath;
        private static string bsPath;
        private static string dbUsername;
        private static string dbPassword;
        private static string dbDatabase;
        private static bool usesvnlib;

        public static void Main(string[] args)
        {
            //
            // Performs the following
            //
            // 1. Queries for a list of applications that need to be installed
            // 2. Downloads application source code from
            //

            usesvnlib = false;

            islPath = domain.BaseDirectory;
            svnPath = Path.Combine(Path.Combine(domain.BaseDirectory, "svn"), "svn.exe");
            cscPath = @"c:\Windows\Microsoft.NET\Framework\v2.0.50727\csc.exe";
            bsPath = @"\\slifer\wroot\";

            if (args.Length > 0)
            {
                bsPath = args[0];
            }

            if (args.Length > 3)
            {
                dbUsername = args[1];
                dbPassword = args[2];
                dbDatabase = args[3];
            }

            if (args.Length > 4)
            {
                usesvnlib = bool.Parse(args[4]);
            }

            DateTime lastUpdated = DateTime.Now.AddHours(-3);
            DateTime lastUpdatedBase = DateTime.Now.AddDays(-2);

            for (; ; )
            {
                if (File.Exists(Path.Combine(islPath, "stop.txt")))
                {
                    File.Delete(Path.Combine(islPath, "stop.txt"));
                    Console.WriteLine("Exiting...");
                    Console.WriteLine("Delete stop.txt before restarting");
                    return;
                }
                else
                {
                    if (DateTime.Now.Subtract(lastUpdated).Hours > 2)
                    {
                        lastUpdated = DateTime.Now;
                        
                        if (DateTime.Now.Subtract(lastUpdatedBase).Days > 1)
                        {
                            try
                            {
                                BackupDatabase();
                            }
                            catch
                            {
                                // If we fail we want to continue and retry the next day
                                // TODO: Report failure
                            }
                            UpdateBase();
                        }

                        UpdateAll();
                    }
                    else
                    {
                        // Block the thread for 1 minute
                        System.Threading.Thread.Sleep(new TimeSpan(0, 1, 0));
                    }
                }
            }
        }

        private static void UpdateBase()
        {
            DownloadAndCompileBase("BoxSocial.IO");
            DownloadAndCompileBase("BoxSocial.Internals");
            DownloadAndCompileBase("BoxSocial.FrontEnd");
        }

        private static void UpdateAll()
        {
            Mysql db = new Mysql(dbUsername, dbDatabase, dbDatabase, "localhost");

            // Select all
            SelectQuery query = new SelectQuery("applications ap");
            query.AddFields("ap.application_assembly_name", "ap.application_primitive");

            DataTable applicationInfoTable = db.Query(query);
            db.CloseConnection();

            foreach (DataRow applicationRow in applicationInfoTable.Rows)
            {
                string assemblyName = (string)applicationRow["application_assembly_name"];
                bool isPrimitive = ((byte)applicationRow["application_primitive"] > 0) ? true : false;

                DownloadAndCompile(assemblyName, isPrimitive);
            }
        }

        private static void DownloadAndCompileAll()
        {
        }

        private static void DownloadAndCompileBase(string repositoryName)
        {
            try
            {
                DownloadApplicationSource(repositoryName);
                CompileApplication(repositoryName);
                InstallBaseApplication(repositoryName);
            }
            catch (Exception ex)
            {
                StreamWriter sw = File.CreateText(Path.Combine(islPath, "error.txt"));
                sw.Write(ex.ToString());
                sw.Close();
                System.Threading.Thread.CurrentThread.Abort();
            }
        }

        private static void DownloadAndCompile(string repositoryName, bool primitive)
        {
            try
            {
                DownloadApplicationSource(repositoryName);
                CompileApplication(repositoryName);

                if (primitive)
                {
                    InstallBaseApplication(repositoryName);
                }
                else
                {
                    InstallApplication(repositoryName);
                }
            }
            catch (Exception ex)
            {
                StreamWriter sw = File.CreateText(Path.Combine(islPath, "error.txt"));
                sw.Write(ex.ToString());
                sw.Close();
                System.Threading.Thread.CurrentThread.Abort();
            }
        }

        /// <summary>
        /// Step 0. & 1.
        /// </summary>
        /// <param name="repositoryName"></param>
        private static void DownloadApplicationSource(string repositoryName)
        {
            string repoPath = Path.Combine(Path.Combine(islPath, "source"), repositoryName); 

            // Step 0. Prepage the download area
            if (Directory.Exists(repoPath))
            {
                Directory.Delete(repoPath, true);
            }

            // Step 1. Download source code

            //DownloadUsingSvnLib(repositoryName, repoPath);
            DownloadFromSvn(string.Format(@"http://svn.boxsocial.net/{0}/", repositoryName), repoPath);
        }

        private static void DownloadUsingSvnLib(string repositoryName, string repoPath)
        {
            System.Diagnostics.Process proc = new System.Diagnostics.Process();
            proc.StartInfo.FileName = svnPath;
            proc.StartInfo.Arguments = string.Format(@"export http://svn.boxsocial.net/{0}/ {1}",
                repositoryName, repoPath);
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.UseShellExecute = false;
            proc.Start();
            Console.WriteLine(proc.StandardOutput.ReadToEnd());
            proc.WaitForExit(300000);
            proc.Close();
        }

        /// <summary>
        /// Step 1.a,b,c) Traverse the sourcecode
        /// </summary>
        /// <param name="modulePath"></param>
        /// <param name="localPath"></param>
        private static void DownloadFromSvn(string modulePath, string localPath)
        {
            Console.WriteLine("Downloading ... {0}", modulePath);
            WebClient wc = new WebClient();

            if (!Directory.Exists(localPath))
            {
                Directory.CreateDirectory(localPath);
            }

            try
            {
                // a) download
                string content = wc.DownloadString(modulePath);

                // b) parse

                // pre-pend an xml declaration so we can parse it using the
                // built in XML parser so we don't have to write out own
                content = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Strict//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd\">\n" + content;
                content = content.Replace("<html>", "<html xmlns=\"http://www.w3.org/1999/xhtml\" lang=\"en-au\" dir=\"ltr\">");
                content = content.Replace("<hr noshade>", "");

                StringReader sr = new StringReader(content);

                DataSet ds = new DataSet();
                ds.ReadXml(sr);

                DataTable anchorsTable = ds.Tables["a"];

                // c) Traverse
                foreach (DataRow dr in anchorsTable.Rows)
                {
                    string link = (string)dr["href"];
                    if (link == "../")
                    {
                        // Link to parent, Ignore
                    }
                    else if (link.IndexOf("/") == link.Length - 1)
                    {
                        // Directory

                        DownloadFromSvn(modulePath + link, Path.Combine(localPath, link.TrimEnd(new char[] { '/' })));
                    }
                    else if (link.IndexOf("/") < 0)
                    {
                        // File

                        DownloadFileFromSvn(modulePath + link, Path.Combine(localPath, link));
                    }
                    else
                    {
                        // External link, Ignore
                    }
                }
            }
            catch (WebException)
            {
            }
        }

        /// <summary>
        /// Step 1.d) Download the source code
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="localPath"></param>
        private static void DownloadFileFromSvn(string filePath, string localPath)
        {
            Console.WriteLine("Downloading ... {0}", filePath);
            WebClient wc = new WebClient();

            try
            {
                wc.DownloadFile(filePath, localPath);
            }
            catch (WebException)
            {
            }
        }

        /// <summary>
        /// Step 2. & 3.
        /// </summary>
        /// <param name="repositoryName"></param>
        private static void CompileApplication(string repositoryName)
        {
            string repoPath = Path.Combine(Path.Combine(islPath, "source"), repositoryName);

            // Step 2. Generate a list of files to compile

            DataSet xsd = new DataSet();
            xsd.ReadXml(Path.Combine(repoPath, repositoryName + ".csproj"));

            DataTable compileTable = xsd.Tables["Compile"];
            DataTable projectReferenceTable = xsd.Tables["ProjectReference"];
            DataTable referenceTable = xsd.Tables["Reference"];

            string files = "";
            string link = "";

            foreach (DataRow dr in compileTable.Rows)
            {
                string file = (string)dr["Include"];
                if (file.ToLower().EndsWith(".cs"))
                {
                    files += " \"" + Path.Combine(repoPath, file) + "\"";
                }
            }

            if (projectReferenceTable != null)
            {
                if (projectReferenceTable.Rows != null)
                {
                    foreach (DataRow dr in projectReferenceTable.Rows)
                    {
                        link += " \"/reference:" + Path.Combine(Path.Combine(bsPath, "bin"), (string)dr["Name"] + ".dll") + "\"";
                    }
                }
            }

            if (referenceTable != null)
            {
                if (referenceTable.Rows != null)
                {
                    foreach (DataRow dr in referenceTable.Rows)
                    {
                        string include = (string)dr["Include"];
                        include = include.Split(',')[0];

                        if (!include.StartsWith("System"))
                        {
                            link += " \"/reference:" + Path.Combine(Path.Combine(bsPath, "bin"), include + ".dll") + "\"";
                        }
                    }
                }
            }

            // Step 3. Start compilation
            System.Diagnostics.Process proc = new System.Diagnostics.Process();
            proc.StartInfo.FileName = cscPath;
            proc.StartInfo.Arguments = link + string.Format(@" /out:{0} /target:library /unsafe+ /optimize+ /debug+ /pdb:{1} {2}",
                Path.Combine(Path.Combine(islPath, "bin"), repositoryName + ".dll"), Path.Combine(Path.Combine(islPath, "bin"), repositoryName + ".pdb"), files);
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.UseShellExecute = false;
            proc.Start();
            Console.WriteLine(proc.StandardOutput.ReadToEnd());
            proc.WaitForExit(120000);
            proc.Close();

            
        }

        private static void CompileLanguages(string repositoryName)
        {
            // Step 4. Compile Language Files
            /*foreach (DataRow dr in compileTable.Rows)
            {
                string file = (string)dr["Include"];
                if (file.ToLower().EndsWith(".resx") && file.ToLower().StartsWith("languages"))
                {
                    //files += " \"" + Path.Combine(repoPath, file) + "\"";
                    // "Resgen.exe" "filename.resx"
                    // copy filename.resources
                }
            }*/
        }

        /// <summary>
        /// Step 5.
        /// </summary>
        /// <param name="repositoryName"></param>
        private static void InstallBaseApplication(string repositoryName)
        {
            // Step 4. Copy files to bin

            File.Copy(Path.Combine(Path.Combine(islPath, "bin"), repositoryName + ".dll"), Path.Combine(Path.Combine(bsPath, "bin"), repositoryName + ".dll"), true);
            File.Copy(Path.Combine(Path.Combine(islPath, "bin"), repositoryName + ".pdb"), Path.Combine(Path.Combine(bsPath, "bin"), repositoryName + ".pdb"), true);
        }

        /// <summary>
        /// Step 4.
        /// </summary>
        /// <param name="repositoryName"></param>
        private static void InstallApplication(string repositoryName)
        {
            // Step 4. Copy files to bin

            File.Copy(Path.Combine(Path.Combine(Path.Combine(islPath, "bin"), "applications"), repositoryName + ".dll"), Path.Combine(Path.Combine(Path.Combine(bsPath, "bin"), "applications"), repositoryName + ".dll"), true);
            File.Copy(Path.Combine(Path.Combine(Path.Combine(islPath, "bin"), "applications"), repositoryName + ".pdb"), Path.Combine(Path.Combine(Path.Combine(bsPath, "bin"), "applications"), repositoryName + ".pdb"), true);
        }

        private static void BackupDatabase()
        {
            System.Diagnostics.Process proc = new System.Diagnostics.Process();
            proc.StartInfo.FileName = "mysqldump";
            if (string.IsNullOrEmpty(dbPassword))
            {
                proc.StartInfo.Arguments = string.Format("-u {0} {1}", dbUsername, dbDatabase);
            }
            else
            {
                proc.StartInfo.Arguments = string.Format("-u {0} --password=\"{2}\" {1}", dbUsername, dbDatabase, dbPassword);
            }
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.UseShellExecute = false;
            proc.Start();

            int chars = 1;
            int totalChars = 0;
            char[] buffer = new char[1048576];
            StreamWriter sw = File.CreateText(Path.Combine(islPath, "backup.sql"));

            while (!proc.HasExited || chars > 0)
            {
                chars = proc.StandardOutput.Read(buffer, 0, 1048576);
                StringBuilder sb = new StringBuilder(1048576);
                sb.Append(buffer, 0, chars);
                sw.Write(sb.ToString());
                totalChars++;
            }

            sw.Close();

            proc.WaitForExit(3600000); // allow the backup to run for an hour
            proc.Close();
        }
    }
}
