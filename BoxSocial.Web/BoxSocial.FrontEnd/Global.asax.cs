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
using System.Data;
using System.Configuration;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Caching;
using System.Web.Configuration;
using System.Web.SessionState;
using BoxSocial.Internals;
using BoxSocial.Groups;
using BoxSocial.IO;

namespace BoxSocial.FrontEnd
{
    public partial class Global : System.Web.HttpApplication
    {
        protected static BackgroundWorker worker = new BackgroundWorker();

        private static Object queueLock = new object();
        protected static JobQueue queue = null;

        protected static JobQueue Queue
        {
            get
            {
                if (queue == null)
                {
                    switch (WebConfigurationManager.AppSettings["queue-provider"])
                    {
                        case "amazon_sqs":
                            queue = new AmazonSQS(WebConfigurationManager.AppSettings["amazon-key-id"], WebConfigurationManager.AppSettings["amazon-secret-key"]);
                            break;
                        case "rackspace":
                            queue = new RackspaceCloudQueues(WebConfigurationManager.AppSettings["rackspace-key"], WebConfigurationManager.AppSettings["rackspace-username"]);

                            string location = WebConfigurationManager.AppSettings["rackspace-location"];
                            if (!string.IsNullOrEmpty(location))
                            {
                                ((RackspaceCloudQueues)queue).SetLocation(location);
                            }
                            break;
                        case "database":
                        default:
                            //queue = new DatabaseQueue(db);
                            break;
                    }
                }
                return queue;
            }
        }

        protected void Application_Start(object sender, EventArgs e)
        {
            AppDomainSetup ads = AppDomain.CurrentDomain.SetupInformation;
            ads.ShadowCopyFiles = bool.TrueString;
            if (ads.ShadowCopyDirectories != null)
            {
                if (ads.ShadowCopyDirectories == "")
                {
                    ads.ShadowCopyDirectories = HttpContext.Current.Server.MapPath("/applications/");
                }
                else
                {
                    ads.ShadowCopyDirectories = string.Format("{0};{1}", ads.ShadowCopyDirectories, HttpContext.Current.Server.MapPath("/applications/"));
                }
            }

            ads.ShadowCopyDirectories = ads.ShadowCopyDirectories + ";" + Server.MapPath(@"/applications/");
            //AppDomain.CurrentDomain.SetShadowCopyPath(ads.ShadowCopyDirectories + ";" + Server.MapPath(@"/applications/"));

            //
            // Load all applications
            //

            Mysql db = new Mysql(WebConfigurationManager.AppSettings["mysql-user"],
                WebConfigurationManager.AppSettings["mysql-password"],
                WebConfigurationManager.AppSettings["mysql-database"],
                WebConfigurationManager.AppSettings["mysql-host"]);

            BoxSocial.Internals.Application.LoadAssemblies(db);

            db.CloseConnection();

            //
            // Implements a message queue processor
            //

            lock (queueLock)
            {
                // Check the queues and create is not exist
                if (Queue != null)
                {
                    if (!Queue.QueueExists(WebConfigurationManager.AppSettings["queue-default-priority"]))
                    {
                        Queue.CreateQueue(WebConfigurationManager.AppSettings["queue-default-priority"]);
                    }

                    // Starts the queue processor
                    worker.WorkerReportsProgress = false;
                    worker.WorkerSupportsCancellation = true;
                    worker.DoWork += new DoWorkEventHandler(worker_DoWork);
                    worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted);
                    worker.RunWorkerAsync(HttpContext.Current);
                }

                queue.CloseConnection();
                queue = null;
            }
        }

        protected void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            int wait = 15;
            for (int i = 0; i < wait * 100; i++)
            {
                // The sleep should happen on the worker thread rather than the application thread
                System.Threading.Thread.Sleep(10);
                if (e.Cancel)
                {
                    // Poll the thread every [wait] seconds to work out if the worker should be cancelled
                    return;
                }
            }

            HttpContext.Current = (HttpContext)e.Argument;

            Mysql db = new Mysql(WebConfigurationManager.AppSettings["mysql-user"],
                WebConfigurationManager.AppSettings["mysql-password"],
                WebConfigurationManager.AppSettings["mysql-database"],
                WebConfigurationManager.AppSettings["mysql-host"]);

            Core core = new Core(null, db, null);

            try
            {
                if (Queue != null)
                {
                    int failedJobs = 0;

                    // Retrieve Jobs
                    Dictionary<string, int> queues = new Dictionary<string, int>();
                    queues.Add(WebConfigurationManager.AppSettings["queue-default-priority"], 10);

                    foreach (string queueName in queues.Keys)
                    {
                        List<Job> jobs = null;
                        lock (queueLock)
                        {
                            jobs = Queue.ClaimJobs(queueName, queues[queueName]);
                        }

                        foreach (Job job in jobs)
                        {
                            core.LoadUserProfile(job.UserId);
                        }

                        foreach (Job job in jobs)
                        {
                            core.CreateNewSession(core.PrimitiveCache[job.UserId]);

                            // Load Application
                            if (job.ApplicationId > 0)
                            {
                                ApplicationEntry ae = core.GetApplication(job.ApplicationId);
                                BoxSocial.Internals.Application.LoadApplication(core, AppPrimitives.Any, ae);
                            }

                            // Execute Job
                            if (core.InvokeJob(job))
                            {
                                lock (queueLock)
                                {
                                    Queue.DeleteJob(job);
                                }
                            }
                            else
                            {
                                failedJobs++;
                                //queue.ReleaseJob(job);
                            }
                        }
                    }

                    if (failedJobs > 0)
                    {
                        core.Email.SendEmail(WebConfigurationManager.AppSettings["error-email"], "Jobs failed at " + Hyperlink.Domain, "FAILED JOB COUNT:\n" + failedJobs.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                try
                {
                    core.Email.SendEmail(WebConfigurationManager.AppSettings["error-email"], "An Error occured at " + Hyperlink.Domain + " in global.asax", "EXCEPTION THROWN:\n" + ex.ToString());
                }
                catch
                {
                    try
                    {
                        core.Email.SendEmail(WebConfigurationManager.AppSettings["error-email"], "An Error occured at " + Hyperlink.Domain + " in global.asax", "EXCEPTION THROWN:\n" + ex.ToString());
                    }
                    catch
                    {
                    }
                }
            }

            // Cleanup

            core.CloseProse();
            core.CloseSearch();
            db.CloseConnection();
            core = null;
            queue.CloseConnection();
            queue = null;
        }

        protected void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            if (worker != null)
            {
                if ((!worker.CancellationPending) && (!e.Cancelled))
                {
                    worker.RunWorkerAsync();
                }
                else
                {
                    worker.Dispose();
                    worker = null;
                }
            }
        }

        protected void Application_End(object sender, EventArgs e)
        {
            if (worker != null && (!worker.CancellationPending))
            {
                worker.CancelAsync();
            }
        }

        protected void Application_Error(object sender, EventArgs e)
        {
            Server.Transfer("error-handler.aspx");
        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {
            HttpContext httpContext = HttpContext.Current;
            string[] redir = httpContext.Request.RawUrl.Split(';');
            string host = httpContext.Request.Url.Host.ToLower();

            if (host == "www." + Hyperlink.Domain)
            {
                Response.Redirect("http://" + Hyperlink.Domain);
                return;
            }

            string currentURI = null;
            Uri cUri = null;
            if (httpContext.Request.RawUrl.Contains(";http://") || httpContext.Request.RawUrl.Contains("?http://"))
            {
                if (redir.Length > 1)
                // Apache2/IIS
                {
                    currentURI = redir[1];
                    cUri = new Uri(currentURI);
                    currentURI = cUri.AbsolutePath;

                    if (currentURI.EndsWith("index.php", StringComparison.Ordinal))
                    {
                        currentURI = currentURI.Substring(0, currentURI.Length - 9);
                        Response.Redirect(currentURI, true);
                        Response.End();
                        return;
                    }

                    if (currentURI.EndsWith(".php", StringComparison.Ordinal))
                    {
                        currentURI = currentURI.Substring(0, currentURI.Length - 4);
                        Response.Redirect(currentURI, true);
                        Response.End();
                        return;
                    }
                }
                else
                // NGINX
                {
                    int i = httpContext.Request.RawUrl.IndexOf('?');
                    if (httpContext.Request.RawUrl.Length >= i)
                    {
                        currentURI = httpContext.Request.RawUrl.Substring(i + 1);
                    }
                    cUri = new Uri(currentURI);
                    currentURI = cUri.AbsolutePath;
                }
            }
            /*else
            {
                currentURI = httpContext.Request.RawUrl;
                cUri = new Uri(currentURI);
                currentURI = cUri.AbsolutePath;
            }*/

            if (!httpContext.Request.RawUrl.Contains("404.aspx"))
            {
                if (host == Hyperlink.Domain)
                {
                    return;
                }
                else
                {
                    if (httpContext.Request.RawUrl.Contains("default.aspx"))
                    {
                        cUri = httpContext.Request.Url;
                        currentURI = "/";
                    }
                }
            }

            if (currentURI != null)
            {
                List<string[]> patterns = new List<string[]>();
                if (host != Hyperlink.Domain)
                {
                    SelectQuery query = new SelectQuery("dns_records");
                    query.AddFields("dns_domain", "dns_owner_id", "dns_owner_type", "dns_owner_key");
                    query.AddCondition("dns_domain", host);

                    Mysql db = new Mysql(WebConfigurationManager.AppSettings["mysql-user"],
                        WebConfigurationManager.AppSettings["mysql-password"],
                        WebConfigurationManager.AppSettings["mysql-database"],
                        WebConfigurationManager.AppSettings["mysql-host"]);

                    long userTypeId = 0;
                    long groupTypeId = 0;

                    Dictionary<string, long> primitiveTypeIds;

                    System.Web.Caching.Cache cache;
                    object o = null;

                    if (HttpContext.Current != null && HttpContext.Current.Cache != null)
			        {
				        cache = HttpContext.Current.Cache;
			        }
			        else
			        {
                        cache = new System.Web.Caching.Cache();
			        }

                    if (cache != null)
                    {
                        try
                        {
                            o = cache.Get("primitiveTypeIds");
                        }
                        catch (NullReferenceException)
                        {
                        }
                    }

                    if (o != null && o.GetType() == typeof(System.Collections.Generic.Dictionary<string, long>))
                    {
                        primitiveTypeIds = (Dictionary<string, long>)o;

                        userTypeId = primitiveTypeIds[typeof(User).FullName];
                        groupTypeId = primitiveTypeIds[typeof(UserGroup).FullName];
                    }
                    else
                    {
                        SelectQuery query2 = new SelectQuery("item_types");
                        query2.AddFields("type_id", "type_namespace");
                        query2.AddCondition("type_primitive", true);

                        System.Data.Common.DbDataReader typeReader = db.ReaderQuery(query2);

                        primitiveTypeIds = new Dictionary<string, long>(256, StringComparer.Ordinal);
                        while(typeReader.Read())
                        {
                            primitiveTypeIds.Add((string)typeReader["type_namespace"], (long)typeReader["type_id"]);

                            if ((string)typeReader["type_namespace"] == typeof(User).FullName)
                            {
                                userTypeId = (long)typeReader["type_id"];
                            }
                            else if ((string)typeReader["type_namespace"] == typeof(UserGroup).FullName)
                            {
                                groupTypeId = (long)typeReader["type_id"];
                            }
                        }

                        typeReader.Close();
                        typeReader.Dispose();

                        if (cache != null)
                        {
                            cache.Add("primitiveTypeIds", primitiveTypeIds, null, System.Web.Caching.Cache.NoAbsoluteExpiration, new TimeSpan(12, 0, 0), CacheItemPriority.High, null);
                        }
                    }

                    System.Data.Common.DbDataReader dnsReader = db.ReaderQuery(query);

                    if (dnsReader.HasRows)
                    {
                        dnsReader.Read();
                        long typeId = (long)dnsReader["dns_owner_type"];
                        string dnsOwnerKey = (string)dnsReader["dns_owner_key"];

                        dnsReader.Close();
                        dnsReader.Dispose();

                        if (typeId == groupTypeId)
                        {
                                patterns.Add(new string[] { @"^/comment(/|)$", @"/comment.aspx" });

                                patterns.Add(new string[] { string.Format(@"^/styles/group/{0}.css$", dnsOwnerKey), string.Format(@"/groupstyle.aspx?gn={0}", dnsOwnerKey) });
                                patterns.Add(new string[] { string.Format(@"^/images/group/\_([a-z]+)/{0}.png$", dnsOwnerKey), string.Format(@"/identicon.aspx?gn={0}&mode=$1", dnsOwnerKey) });
                                patterns.Add(new string[] { string.Format(@"^/images/group/\_([a-z]+)/{0}@2x.png$", dnsOwnerKey), string.Format(@"/identicon.aspx?gn={0}&mode=$1&retina=true", dnsOwnerKey) });

                                patterns.Add(new string[] { @"^/account/([a-z\-]+)/([a-z\-]+)(/|)$", string.Format(@"/groupaccount.aspx?gn={0}&module=$1&sub=$2", dnsOwnerKey) });
                                patterns.Add(new string[] { @"^/account/([a-z\-]+)(/|)$", string.Format(@"/groupaccount.aspx?gn={0}&module=$1", dnsOwnerKey) });
                                patterns.Add(new string[] { @"^/account(/|)$", string.Format(@"/groupaccount.aspx?gn={0}", dnsOwnerKey) });

                                patterns.Add(new string[] { @"^(/|)$", string.Format(@"/grouppage.aspx?gn={0}&path=", dnsOwnerKey) });
                                patterns.Add(new string[] { @"^/(.+)(/|)$", string.Format(@"/grouppage.aspx?gn={0}&path=$1", dnsOwnerKey) });
                        }
                        if (typeId == userTypeId)
                        {
                            patterns.Add(new string[] { @"^/comment(/|)$", @"/comment.aspx" });

                            patterns.Add(new string[] { @"^/api/acl/get-groups(/|)$", @"/functions.aspx?fun=permission-groups-list" });
                            patterns.Add(new string[] { @"^/api/acl(/|)$", @"/acl.aspx" });
                            patterns.Add(new string[] { @"^/api/rate(/|)$", @"/rate.aspx" });
                            patterns.Add(new string[] { @"^/api/comment(/|)$", @"/comment.aspx" });
                            patterns.Add(new string[] { @"^/api/like(/|)$", @"/like.aspx" });
                            patterns.Add(new string[] { @"^/api/share(/|)$", @"/share.aspx" });
                            patterns.Add(new string[] { @"^/api/subscribe(/|)$", @"/subscribe.aspx" });
                            patterns.Add(new string[] { @"^/api/functions(/|)$", @"/functions.aspx" });
                            patterns.Add(new string[] { @"^/api/friends(/|)$", @"/functions.aspx?fun=friend-list" });
                            patterns.Add(new string[] { @"^/api/tags(/|)$", @"/functions.aspx?fun=tag-list" });
                            patterns.Add(new string[] { @"^/api/card(/|)$", @"/functions.aspx?fun=contact-card" });
                            patterns.Add(new string[] { @"^/api/feed(/|)$", @"/functions.aspx?fun=feed" });
                            patterns.Add(new string[] { @"^/api/oembed(/|)$", @"/functions.aspx?fun=embed" });

                            patterns.Add(new string[] { string.Format(@"^/styles/user/{0}.css$", dnsOwnerKey), string.Format(@"/userstyle.aspx?un={0}", dnsOwnerKey) });
                            patterns.Add(new string[] { string.Format(@"^/images/user/\_([a-z]+)/{0}.png$", dnsOwnerKey), string.Format(@"/identicon.aspx?un={0}&mode=$1", dnsOwnerKey) });
                            patterns.Add(new string[] { string.Format(@"^/images/user/\_([a-z]+)/{0}@2x.png$", dnsOwnerKey), string.Format(@"/identicon.aspx?un={0}&mode=$1&retina=true", dnsOwnerKey) });

                            patterns.Add(new string[] { @"^/account/([a-z\-]+)/([a-z\-]+)(/|)$", @"/account.aspx?module=$1&sub=$2" });
                            patterns.Add(new string[] { @"^/account/([a-z\-]+)(/|)$", @"/account.aspx?module=$1" });
                            patterns.Add(new string[] { @"^/account(/|)$", @"/account.aspx" });

                            patterns.Add(new string[] { @"^(/|)$", string.Format(@"/memberpage.aspx?un={0}&path=", dnsOwnerKey) });
                            patterns.Add(new string[] { @"^/(.+)(/|)$", string.Format(@"/memberpage.aspx?un={0}&path=$1", dnsOwnerKey) });
                        }
                    }
                    else
                    {
                        dnsReader.Close();
                        dnsReader.Dispose();

                        return;
                    }

                    db.CloseConnection();
                }
                else
                {
                    patterns.Add(new string[] { @"^/images/corners-(top|bottom|middle)-([0-9a-f\-_]{6})-([0-9\-_]+)-([0-9\-_]+).png$", @"/corners.aspx?location=$1&width=$3&roundness=$4&colour=$2&ext=png" });
                    patterns.Add(new string[] { @"^/images/corners-(top|bottom|middle)-([0-9a-f\-_]{6})-([0-9\-_]+)-([0-9\-_]+).gif$", @"/corners.aspx?location=$1&width=$3&roundness=$4&colour=$2&ext=gif" });

                    patterns.Add(new string[] { @"^/images/corners-(top|bottom|middle)-(left|right|centre)-([0-9a-f\-_]{6})-([0-9\-_]+)-([0-9\-_]+).png$", @"/corners.aspx?location=$1,$2&width=$4&roundness=$5&colour=$3&ext=png" });
                    patterns.Add(new string[] { @"^/images/corners-(top|bottom|middle)-(left|right|centre)-([0-9a-f\-_]{6})-([0-9\-_]+)-([0-9\-_]+).gif$", @"/corners.aspx?location=$1,$2&width=$4&roundness=$5&colour=$3&ext=gif" });

                    patterns.Add(new string[] { @"^/about(/|)$", @"/about.aspx" });
                    patterns.Add(new string[] { @"^/opensource(/|)$", @"/opensource.aspx" });
                    patterns.Add(new string[] { @"^/safety(/|)$", @"/safety.aspx" });
                    patterns.Add(new string[] { @"^/privacy(/|)$", @"/privacy.aspx" });
                    patterns.Add(new string[] { @"^/terms-of-service(/|)$", @"/tos.aspx" });
                    patterns.Add(new string[] { @"^/site-map(/|)$", @"/sitemap.aspx" });
                    patterns.Add(new string[] { @"^/copyright(/|)$", @"/copyright.aspx" });
                    patterns.Add(new string[] { @"^/register(/|)$", @"/register.aspx" });
                    patterns.Add(new string[] { @"^/sign-in(/|)$", @"/login.aspx" });
                    patterns.Add(new string[] { @"^/login(/|)$", @"/login.aspx" });
                    patterns.Add(new string[] { @"^/search(/|)$", @"/search.aspx" });
                    patterns.Add(new string[] { @"^/comment(/|)$", @"/comment.aspx" });

                    patterns.Add(new string[] { @"^/api/acl/get-groups(/|)$", @"/functions.aspx?fun=permission-groups-list" });
                    patterns.Add(new string[] { @"^/api/acl(/|)$", @"/acl.aspx" });
                    patterns.Add(new string[] { @"^/api/rate(/|)$", @"/rate.aspx" });
                    patterns.Add(new string[] { @"^/api/comment(/|)$", @"/comment.aspx" });
                    patterns.Add(new string[] { @"^/api/like(/|)$", @"/like.aspx" });
                    patterns.Add(new string[] { @"^/api/share(/|)$", @"/share.aspx" });
                    patterns.Add(new string[] { @"^/api/subscribe(/|)$", @"/subscribe.aspx" });
                    patterns.Add(new string[] { @"^/api/functions(/|)$", @"/functions.aspx" });
                    patterns.Add(new string[] { @"^/api/friends(/|)$", @"/functions.aspx?fun=friend-list" });
                    patterns.Add(new string[] { @"^/api/tags(/|)$", @"/functions.aspx?fun=tag-list" });
                    patterns.Add(new string[] { @"^/api/card(/|)$", @"/functions.aspx?fun=contact-card" });
                    patterns.Add(new string[] { @"^/api/feed(/|)$", @"/functions.aspx?fun=feed" });
                    patterns.Add(new string[] { @"^/api/oembed(/|)$", @"/functions.aspx?fun=embed" });
                    patterns.Add(new string[] { @"^/api/twitter/callback(/|)$", @"/functions.aspx?fun=twitter" });
                    patterns.Add(new string[] { @"^/api/googleplus/callback(/|)$", @"/functions.aspx?fun=googleplus" });
                    patterns.Add(new string[] { @"^/api/facebook/callback(/|)$", @"/functions.aspx?fun=facebook" });
                    patterns.Add(new string[] { @"^/api/tumblr/callback(/|)$", @"/functions.aspx?fun=tumblr" });

                    patterns.Add(new string[] { @"^/oauth/oauth/request_token(/|)$", @"/oauth.aspx?method=request_token" });
                    patterns.Add(new string[] { @"^/oauth/oauth/authorize(/|)$", @"/functions.aspx?fun=oauth&method=authorize" });
                    patterns.Add(new string[] { @"^/oauth/oauth/access_token(/|)$", @"/oauth.aspx?method=access_token" });

                    patterns.Add(new string[] { @"^/account/([a-z\-]+)/([a-z\-]+)(/|)$", @"/account.aspx?module=$1&sub=$2" });
                    patterns.Add(new string[] { @"^/account/([a-z\-]+)(/|)$", @"/account.aspx?module=$1" });
                    patterns.Add(new string[] { @"^/account(/|)$", @"/account.aspx" });

                    patterns.Add(new string[] { @"^/s/([A-Za-z0-9\-_]+)(/|)$", @"/shorturl.aspx?key=$1" });

                    patterns.Add(new string[] { @"^/group/([A-Za-z0-9\-_]+)/account/([a-z\-]+)/([a-z\-]+)(/|)$", @"/groupaccount.aspx?gn=$1&module=$2&sub=$3" });
                    patterns.Add(new string[] { @"^/group/([A-Za-z0-9\-_]+)/account/([a-z\-]+)(/|)$", @"/groupaccount.aspx?gn=$1&module=$2" });
                    patterns.Add(new string[] { @"^/group/([A-Za-z0-9\-_]+)/account(/|)$", @"/groupaccount.aspx?gn=$1" });

                    patterns.Add(new string[] { @"^/music/([A-Za-z0-9\-_]+)/account/([a-z\-]+)/([a-z\-]+)(/|)$", @"/musicaccount.aspx?mn=$1&module=$2&sub=$3" });
                    patterns.Add(new string[] { @"^/music/([A-Za-z0-9\-_]+)/account/([a-z\-]+)(/|)$", @"/musicaccount.aspx?mn=$1&module=$2" });
                    patterns.Add(new string[] { @"^/music/([A-Za-z0-9\-_]+)/account(/|)$", @"/musicaccount.aspx?mn=$1" });

                    patterns.Add(new string[] { @"^/styles/user/([A-Za-z0-9\-_\.]+).css$", @"/userstyle.aspx?un=$1" });
                    patterns.Add(new string[] { @"^/styles/group/([A-Za-z0-9\-_\.]+).css$", @"/groupstyle.aspx?gn=$1" });
                    patterns.Add(new string[] { @"^/styles/music/([A-Za-z0-9\-_\.]+).css$", @"/musicstyle.aspx?gn=$1" });

                    patterns.Add(new string[] { @"^/images/user/\_([a-z]+)/([A-Za-z0-9\-_\.]+).png$", @"/identicon.aspx?un=$2&mode=$1"});
                    patterns.Add(new string[] { @"^/images/user/\_([a-z]+)/([A-Za-z0-9\-_\.]+)@2x.png$", @"/identicon.aspx?un=$2&mode=$1&retina=true" });

                    patterns.Add(new string[] { @"^/images/group/\_([a-z]+)/([A-Za-z0-9\-_\.]+).png$", @"/identicon.aspx?gn=$2&mode=$1" });
                    patterns.Add(new string[] { @"^/images/group/\_([a-z]+)/([A-Za-z0-9\-_\.]+)@2x.png$", @"/identicon.aspx?gn=$2&mode=$1&retina=true" });

                    patterns.Add(new string[] { @"^/help(/|)$", @"/help.aspx" });
                    patterns.Add(new string[] { @"^/help/([a-z\-]+)(/|)$", @"/help.aspx?topic=$1" });

                    patterns.Add(new string[] { @"^/applications(/|)$", @"/viewapplications.aspx$1" });

                    patterns.Add(new string[] { @"^/application/([A-Za-z0-9\-_]+)/manage/([a-z\-]+)/([a-z\-]+)(/|)$", @"/applicationmanage.aspx?an=$1&module=$2&sub=$3" });
                    patterns.Add(new string[] { @"^/application/([A-Za-z0-9\-_]+)/manage/([a-z\-]+)(/|)$", @"/applicationmanage.aspx?an=$1&module=$2" });
                    patterns.Add(new string[] { @"^/application/([A-Za-z0-9\-_]+)/manage(/|)$", @"/applicationmanage.aspx?an=$1" });

                    patterns.Add(new string[] { @"^/application/([A-Za-z0-9\-_]+)(/|)$", @"/applicationpage.aspx?an=$1&path=" });

                    //patterns.Add(new string[] { @"^/groups/create(/|)$", @"/creategroup.aspx" });
                    patterns.Add(new string[] { @"^/groups/register(/|)$", @"/staticpage.aspx?path=groups/register" });
                    patterns.Add(new string[] { @"^/groups(/|)$", @"/staticpage.aspx?path=groups" });
                    patterns.Add(new string[] { @"^/groups/([A-Za-z0-9\-_]+)(/|)$", @"/staticpage.aspx?path=groups/$1" });

                    patterns.Add(new string[] { @"^/group/([A-Za-z0-9\-_]+)(/|)$", @"/grouppage.aspx?gn=$1&path=" });

                    //patterns.Add(new string[] { @"^/group/([A-Za-z0-9\-_]+)/images/([A-Za-z0-9\-_/\.]+)$", @"/viewimage.aspx?gn=$1&path=$2" });

                    patterns.Add(new string[] { @"^/networks(/|)$", @"/viewnetworks.aspx" });
                    patterns.Add(new string[] { @"^/networks/([A-Za-z0-9\-_\.]+)(/|)$", @"/viewnetworks.aspx?type=$1" });

                    patterns.Add(new string[] { @"^/network/([A-Za-z0-9\-_\.]+)(/|)$", @"/networkpage.aspx?nn=$1&path=" });

                    //patterns.Add(new string[] { @"^/network/([A-Za-z0-9\-_\.]+)/images/([A-Za-z0-9\-_/\.]+)$", @"/viewimage.aspx?nn=$1&path=$2" });

                    //patterns.Add(new string[] { @"^/musicians/create(/|)$", @"/createmusician.aspx" });
                    //patterns.Add(new string[] { @"^/musicians(/|)$", @"/viewmusicians.aspx$1" });
                    //patterns.Add(new string[] { @"^/musician/([A-Za-z0-9\-_]+)(/|)$", @"/viewmusicians.aspx?genre=$1" });
                    //patterns.Add(new string[] { @"^/musician/([A-Za-z0-9\-_]+)(/|)([A-Za-z0-9\-_]+)(/|)$", @"/viewmusicians.aspx?genre=$1&sub=$2" });

                    patterns.Add(new string[] { @"^/music/register(/|)$", @"/staticpage.aspx?path=music/register" });
                    patterns.Add(new string[] { @"^/music(/|)$", @"/staticpage.aspx?path=music" });
                    patterns.Add(new string[] { @"^/music/chart(/|)$", @"/staticpage.aspx?path=music/chart" });
                    patterns.Add(new string[] { @"^/music/directory(/|)$", @"/staticpage.aspx?path=music/directory" });
                    patterns.Add(new string[] { @"^/music/directory/genres(/|)$", @"/staticpage.aspx?path=music/directory/genres" });
                    patterns.Add(new string[] { @"^/music/directory/genre/([a-z0-9\-_\+]+)(/|)$", @"/staticpage.aspx?path=music/directory/genres/$1" });

                    patterns.Add(new string[] { @"^/music/([A-Za-z0-9\-_]+)(/|)$", @"/musicpage.aspx?mn=$1&path=" });

                    patterns.Add(new string[] { @"^/user/([A-Za-z0-9\-_\.]+)(/|)$", @"/memberpage.aspx?un=$1&path=" });

                    //patterns.Add(new string[] { @"^/([A-Za-z0-9\-_]+)/profile(/|)$", @"/viewprofile.aspx?un=$1" });

                    //patterns.Add(new string[] { @"^/([A-Za-z0-9\-_]+)/images/([A-Za-z0-9\-_/\.]+)$", @"/viewimage.aspx?un=$1&path=$2" });

                    /* Wildcard for application loader */
                    patterns.Add(new string[] { @"^/application/([A-Za-z0-9\-_]+)/(.+)(/|)$", @"/applicationpage.aspx?an=$1&path=$2" });
                    patterns.Add(new string[] { @"^/group/([A-Za-z0-9\-_]+)/(.+)(/|)$", @"/grouppage.aspx?gn=$1&path=$2" });
                    patterns.Add(new string[] { @"^/network/([A-Za-z0-9\-_\.]+)/(.+)(/|)$", @"/networkpage.aspx?nn=$1&path=$2" });
                    patterns.Add(new string[] { @"^/music/([A-Za-z0-9\-_]+)/(.+)(/|)$", @"/musicpage.aspx?mn=$1&path=$2" });
                    patterns.Add(new string[] { @"^/user/([A-Za-z0-9\-_\.]+)/(.+)(/|)$", @"/memberpage.aspx?un=$1&path=$2" });

                }

                // fast cull
                int ioc = currentURI.IndexOf('/', 1);
                if (ioc >= 1)
                {
                    for (int i = 0; i < patterns.Count; i++) // (string[] pattern in patterns)
                    {
                        string[] pattern = patterns[i];

                        int iop = pattern[0].IndexOf('/', 2);
                        if (iop >= 2)
                        {
                            if (currentURI.Substring(1, ioc - 1).Equals(pattern[0].Substring(2, iop - 2)))
                            {
                                if (Regex.IsMatch(currentURI, pattern[0]))
                                {
                                    Regex rex = new Regex(pattern[0]);
                                    currentURI = rex.Replace(currentURI, pattern[1]);
                                    if (currentURI.Contains("?"))
                                    {
                                        httpContext.RewritePath(currentURI.TrimEnd(new char[] { '/' }) + "&" + cUri.Query.TrimStart(new char[] { '?' }));
                                        return;
                                    }
                                    else
                                    {
                                        httpContext.RewritePath(currentURI.TrimEnd(new char[] { '/' }) + cUri.Query);
                                        return;
                                    }
                                }
                            }
                        }
                    }
                }

                // full catch all
                for (int i = 0; i < patterns.Count; i++) //foreach (string[] pattern in patterns)
                {
                    string[] pattern = patterns[i];
                    if (Regex.IsMatch(currentURI, pattern[0]))
                    {
                        Regex rex = new Regex(pattern[0]);
                        currentURI = rex.Replace(currentURI, pattern[1]);
                        if (currentURI.Contains("?"))
                        {
                            httpContext.RewritePath(currentURI.TrimEnd(new char[] { '/' }) + "&" + cUri.Query.TrimStart(new char[] { '?' }));
                            return;
                        }
                        else
                        {
                            httpContext.RewritePath(currentURI.TrimEnd(new char[] { '/' }) + cUri.Query);
                            return;
                        }
                    }
                }
            }
        }
    }
}
