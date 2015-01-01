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
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Configuration;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    public enum ApplicationLoadOptions : byte
    {
        Key = 0x01,
        Info = Key | 0x02,
        Common = Key | Info,
        All = Key | Info,
    }

    public enum ApplicationType : byte
    {
        Native = 0x00,
        OAuth = 0x01,
    }

    [Cacheable]
    [DataTable("applications", "APPLICATION")]
    [Primitive("APPLICATION", ApplicationLoadOptions.All, "application_id", "application_assembly_name")]
    [Permission("COMMENT", "Can comment on the application", PermissionTypes.Interact)]
    [Permission("RATE", "Can rate the application", PermissionTypes.Interact)]
    [Permission("UPDATE", "Can update the application", PermissionTypes.CreateAndEdit)]
    public class ApplicationEntry : Primitive, ICommentableItem, IPermissibleItem
    {
        [DataField("application_id", DataFieldKeys.Primary)]
        private long applicationId;
        [DataField("user_id")]
        private int creatorId;
        [DataField("application_title", 63)]
        private string title;
        [DataField("application_description", MYSQL_TEXT)]
        private string description;
        [DataField("application_icon", 63)]
        private string applicationIcon;
        [DataField("application_thumb", 63)]
        private string applicationThumb;
        [DataField("application_tile", 63)]
        private string applicationTile;
        [DataField("application_square", 63)]
        private string applicationSquare;
        [DataField("application_gallery_icon")]
        private long applicationGalleryIcon;
        [DataField("application_name", DataFieldKeys.Unique, 63)]
        private string applicationName;
        [DataField("application_assembly_name", DataFieldKeys.Unique, 63)]
        private string assemblyName;
        [DataField("application_primitive")]
        private bool isPrimitive;
        [DataField("application_primitives")]
        private byte primitives;
        [DataField("application_date_ut")]
        private long dateRaw;
        [DataField("application_comment")]
        private bool usesComments;
        [DataField("application_rating")]
        private bool usesRatings;
        [DataField("application_style")]
        private bool hasStyleSheet;
        [DataField("application_script")]
        private bool hasJavaScript;
        [DataField("application_locked")]
        private bool isLocked;
        [DataField("application_update")]
        private bool updateQueued;
        [DataField("application_simple_permissions")]
        private bool simplePermissions;
        [DataField("application_type")]
        private byte applicationType;

        //TODO: USER_APPLICATION_FIELDS
        private ushort permissions;
        private long itemId;

        private List<string> slugExs;
        private List<string> modules;
        private string displayNameOwnership;

        private Access access; // primitive application access rights
        private Assembly assembly;

        public event CommentHandler OnCommentPosted;

        public Assembly Assembly
        {
            get
            {
                if (assembly == null)
                {
                    assembly = Application.LoadedAssemblies[Id];
                }
                return assembly;
            }
        }

        public long ApplicationId
        {
            get
            {
                return applicationId;
            }
        }

        public override long Id
        {
            get
            {
                return applicationId;
            }
        }

        public override string Key
        {
            get
            {
                return applicationName;
            }
        }


        public override string Type
        {
            get
            {
                return "APPLICATION";
            }
        }

        public ApplicationType ApplicationType
        {
            get
            {
                return (ApplicationType)applicationType;
            }
            internal set
            {
                SetPropertyByRef(new { applicationType }, value);
            }
        }

        public override AppPrimitives AppPrimitive
        {
            get
            {
                return AppPrimitives.Application;
            }
        }

        public int CreatorId
        {
            get
            {
                return creatorId;
            }
        }

        public string Title
        {
            get
            {
                return title;
            }
            set
            {
                SetPropertyByRef(new { title }, value);
            }
        }

        public override string DisplayName
        {
            get
            {
                return title;
            }
        }

        public override string DisplayNameOwnership
        {
            get
            {
                if (displayNameOwnership == null)
                {
                    displayNameOwnership = (title != string.Empty) ? title : assemblyName;

                    if (displayNameOwnership.EndsWith("s", StringComparison.Ordinal))
                    {
                        displayNameOwnership = displayNameOwnership + "'";
                    }
                    else
                    {
                        displayNameOwnership = displayNameOwnership + "'s";
                    }
                }
                return displayNameOwnership;
            }
        }

        public override string TitleNameOwnership
        {
            get
            {
                return "the application " + DisplayNameOwnership;
            }
        }

        public override string TitleName
        {
            get
            {
                return "the application " + DisplayName;
            }
        }

        public string Description
        {
            get
            {
                return description;
            }
            set
            {
                SetPropertyByRef(new { description }, value);
            }
        }

        public string AssemblyName
        {
            get
            {
                return assemblyName;
            }
        }

        public string ApplicationName
        {
            get
            {
                return applicationName;
            }
        }

        public bool IsPrimitive
        {
            get
            {
                return isPrimitive;
            }
            set
            {
                SetPropertyByRef(new { isPrimitive }, value);
            }
        }

        public bool IsLocked
        {
            get
            {
                return isLocked;
            }
            set
            {
                SetPropertyByRef(new { isLocked }, value);
            }
        }

        public bool UpdateQueued
        {
            get
            {
                return updateQueued;
            }
            set
            {
                SetPropertyByRef(new { updateQueued }, value);
            }
        }

        public ushort Permissions
        {
            get
            {
                return permissions;
            }
        }

        public long Comments
        {
            get
            {
                return Info.Comments;
            }
        }

        public List<string> Modules
        {
            get
            {
                return modules;
            }
        }

        public long GalleryIcon
        {
            get
            {
                return applicationGalleryIcon;
            }
        }

        public override string Icon
        {
            get
            {
                if (ApplicationType == Internals.ApplicationType.Native)
                {
                    return applicationIcon;
                }
                else
                {
                    if (GalleryIcon > 0)
                    {
                        return string.Format("{0}images/_icon/_{1}.png",
                            UriStub, Key);
                    }
                    else
                    {
                        return core.Hyperlink.AppendCoreSid(string.Format("/images/application/_icon/{0}.png",
                            Key));
                    }
                }
            }
            /*set
            {
                SetProperty("applicationIcon", value);
            }*/
        }

        public override string Thumbnail
        {
            get
            {
                if (ApplicationType == Internals.ApplicationType.Native)
                {
                    return applicationThumb;
                }
                else
                {
                    if (GalleryIcon > 0)
                    {
                        return string.Format("{0}images/_thumb/_{1}.png",
                            UriStub, Key);
                    }
                    else
                    {
                        return core.Hyperlink.AppendCoreSid(string.Format("/images/application/_thumb/{0}.png",
                            Key));
                    }
                }
            }
            /*set
            {
                SetProperty("applicationThumb", value);
            }*/
        }

        public override string Tile
        {
            get
            {
                if (ApplicationType == Internals.ApplicationType.Native)
                {
                    return applicationTile;
                }
                else
                {
                    if (GalleryIcon > 0)
                    {
                        return string.Format("{0}images/_tile/_{1}.png",
                            UriStub, Key);
                    }
                    else
                    {
                        return core.Hyperlink.AppendCoreSid(string.Format("/images/application/_tile/{0}.png",
                            Key));
                    }
                }
            }
            /*set
            {
                SetProperty("applicationTile", value);
            }*/
        }

        public override string Square
        {
            get
            {
                if (ApplicationType == Internals.ApplicationType.Native)
                {
                    return applicationSquare;
                }
                else
                {
                    if (GalleryIcon > 0)
                    {
                        return string.Format("{0}images/_square/_{1}.png",
                            UriStub, Key);
                    }
                    else
                    {
                        return core.Hyperlink.AppendCoreSid(string.Format("/images/application/_square/{0}.png",
                            Key));
                    }
                }
            }
            /*set
            {
                SetProperty("applicationSquare", value);
            }*/
        }

        public bool HasIcon
        {
            get
            {
                if (string.IsNullOrEmpty(applicationIcon) && applicationGalleryIcon == 0)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        public bool HasStyleSheet
        {
            get
            {
                return hasStyleSheet;
            }
            set
            {
                SetPropertyByRef(new { hasStyleSheet }, value);
            }
        }

        public bool HasJavascript
        {
            get
            {
                return hasJavaScript;
            }
            set
            {
                SetPropertyByRef(new { hasJavaScript }, value);
            }
        }

        public AppPrimitives SupportedPrimitives
        {
            get
            {
                return (AppPrimitives)primitives;
            }
            set
            {
                SetPropertyByRef(new { primitives }, value);
            }
        }

        public bool UsesComments
        {
            get
            {
                return usesComments;
            }
            set
            {
                SetPropertyByRef(new { usesComments }, value);
            }
        }

        public bool UsesRatings
        {
            get
            {
                return usesRatings;
            }
            set
            {
                SetPropertyByRef(new { usesRatings }, value);
            }
        }

        public void LoadSlugEx(string ex)
        {
            if (slugExs == null)
            {
                slugExs = new List<string>();
            }
            slugExs.Add(ex);
        }

        public void AddModule(string module)
        {
            if (modules == null)
            {
                modules = new List<string>();
            }
            modules.Add(module);
        }

        public bool SlugMatch(string uri)
        {
            if (slugExs != null)
            {
                foreach (string slugEx in slugExs)
                {
                    Regex rex = new Regex(slugEx);
                    Match pathMatch = rex.Match(uri);
                    if (pathMatch.Success)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public ApplicationEntry(Core core)
            : base(core)
        {
            Assembly asm = Assembly.GetCallingAssembly();

            string assemblyName = asm.GetName().Name;
            assembly = asm;

            ItemLoad += new ItemLoadHandler(ApplicationEntry_ItemLoad);

            try
            {
                LoadItem("application_assembly_name", assemblyName);
            }
            catch (InvalidItemException)
            {
                throw new InvalidApplicationException();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public ApplicationEntry(Core core, bool isThis)
            : base(core)
        {
            if (isThis)
            {
                Assembly asm = Assembly.GetCallingAssembly();

                string assemblyName = asm.GetName().Name;
                assembly = asm;

                ItemLoad += new ItemLoadHandler(ApplicationEntry_ItemLoad);

                try
                {
                    LoadItem("application_assembly_name", assemblyName);
                }
                catch (InvalidItemException)
                {
                    throw new InvalidApplicationException();
                }
            }
        }

        public ApplicationEntry(Core core, string assemblyName)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(ApplicationEntry_ItemLoad);

            try
            {
                LoadItem("application_name", assemblyName);
            }
            catch (InvalidItemException)
            {
                throw new InvalidApplicationException();
            }
        }
		
        public ApplicationEntry(Core core, long applicationId)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(ApplicationEntry_ItemLoad);

            try
            {
                LoadItem(applicationId);
            }
            catch (InvalidItemException)
            {
                throw new InvalidApplicationException();
            }
        }

        public ApplicationEntry(Core core, DataRow applicationRow, ApplicationLoadOptions loadOptions)
            : this(core, applicationRow)
        {
        }

        public ApplicationEntry(Core core, System.Data.Common.DbDataReader applicationRow, ApplicationLoadOptions loadOptions)
            : this(core, applicationRow)
        {
        }

        public ApplicationEntry(Core core, DataRow applicationRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(ApplicationEntry_ItemLoad);

            loadItemInfo(applicationRow);
        }

        public ApplicationEntry(Core core, System.Data.Common.DbDataReader applicationRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(ApplicationEntry_ItemLoad);

            loadItemInfo(applicationRow);
        }

        protected override void loadItemInfo(DataRow applicationRow)
        {
            loadApplication(applicationRow);
        }

        protected override void loadItemInfo(System.Data.Common.DbDataReader applicationRow)
        {
            loadApplication(applicationRow);
        }

        protected void loadApplication(DataRow applicationRow)
        {
            loadValue(applicationRow, "application_id", out applicationId);
            loadValue(applicationRow, "user_id", out creatorId);
            loadValue(applicationRow, "application_name", out applicationName);
            loadValue(applicationRow, "application_title", out title);
            loadValue(applicationRow, "application_description", out description);
            loadValue(applicationRow, "application_icon", out applicationIcon);
            loadValue(applicationRow, "application_thumb", out applicationThumb);
            loadValue(applicationRow, "application_tile", out applicationTile);
            loadValue(applicationRow, "application_square", out applicationSquare);
            loadValue(applicationRow, "application_assembly_name", out assemblyName);
            loadValue(applicationRow, "application_primitive", out isPrimitive);
            loadValue(applicationRow, "application_primitives", out primitives);
            loadValue(applicationRow, "application_date_ut", out dateRaw);
            loadValue(applicationRow, "application_comment", out usesComments);
            loadValue(applicationRow, "application_rating", out usesRatings);
            loadValue(applicationRow, "application_style", out hasStyleSheet);
            loadValue(applicationRow, "application_script", out hasJavaScript);
            loadValue(applicationRow, "application_locked", out isLocked);
            loadValue(applicationRow, "application_update", out updateQueued);
            loadValue(applicationRow, "application_simple_permissions", out simplePermissions);
            loadValue(applicationRow, "application_type", out applicationType);
            loadValue(applicationRow, "application_gallery_icon", out applicationGalleryIcon);

            itemLoaded(applicationRow);
            core.ItemCache.RegisterItem((NumberedItem)this);
        }

        protected void loadApplication(System.Data.Common.DbDataReader applicationRow)
        {
            loadValue(applicationRow, "application_id", out applicationId);
            loadValue(applicationRow, "user_id", out creatorId);
            loadValue(applicationRow, "application_name", out applicationName);
            loadValue(applicationRow, "application_title", out title);
            loadValue(applicationRow, "application_description", out description);
            loadValue(applicationRow, "application_icon", out applicationIcon);
            loadValue(applicationRow, "application_thumb", out applicationThumb);
            loadValue(applicationRow, "application_tile", out applicationTile);
            loadValue(applicationRow, "application_square", out applicationSquare);
            loadValue(applicationRow, "application_assembly_name", out assemblyName);
            loadValue(applicationRow, "application_primitive", out isPrimitive);
            loadValue(applicationRow, "application_primitives", out primitives);
            loadValue(applicationRow, "application_date_ut", out dateRaw);
            loadValue(applicationRow, "application_comment", out usesComments);
            loadValue(applicationRow, "application_rating", out usesRatings);
            loadValue(applicationRow, "application_style", out hasStyleSheet);
            loadValue(applicationRow, "application_script", out hasJavaScript);
            loadValue(applicationRow, "application_locked", out isLocked);
            loadValue(applicationRow, "application_update", out updateQueued);
            loadValue(applicationRow, "application_simple_permissions", out simplePermissions);
            loadValue(applicationRow, "application_type", out applicationType);
            loadValue(applicationRow, "application_gallery_icon", out applicationGalleryIcon);

            itemLoaded(applicationRow);
            core.ItemCache.RegisterItem((NumberedItem)this);
        }

        private void ApplicationEntry_ItemLoad()
        {
            OnCommentPosted += new CommentHandler(ApplicationEntry_CommentPosted);
        }

        public void CommentPosted(CommentPostedEventArgs e)
        {
            if (OnCommentPosted != null)
            {
                OnCommentPosted(e);
            }
        }

        bool ApplicationEntry_CommentPosted(CommentPostedEventArgs e)
        {
            return true;
        }
        private void loadApplicationUserInfo(DataRow applicationRow)
        {
            itemId = (long)applicationRow["item_id"];
        }

        public static ApplicationEntry Create(Core core, string assembly, Application application, bool isPrimitive)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            InsertQuery iQuery = new InsertQuery(typeof(ApplicationEntry));
            iQuery.AddField("application_name", assembly);
            iQuery.AddField("application_assembly_name", assembly);
            iQuery.AddField("user_id", core.LoggedInMemberId);
            iQuery.AddField("application_date_ut", UnixTime.UnixTimeStamp());
            iQuery.AddField("application_title", application.Title);
            iQuery.AddField("application_description", application.Description);
            iQuery.AddField("application_primitive", isPrimitive);
            iQuery.AddField("application_primitives", (byte)application.GetAppPrimitiveSupport());
            iQuery.AddField("application_comment", application.UsesComments);
            iQuery.AddField("application_rating", application.UsesRatings);
            iQuery.AddField("application_style", !string.IsNullOrEmpty(application.StyleSheet));
            iQuery.AddField("application_script", !string.IsNullOrEmpty(application.JavaScript));
            iQuery.AddField("application_icon", string.Format(@"/images/{0}/icon.png", assembly));
            iQuery.AddField("application_thumb", string.Format(@"/images/{0}/thumb.png", assembly));
            iQuery.AddField("application_tile", string.Format(@"/images/{0}/tile.png", assembly));
            iQuery.AddField("application_type", (byte)ApplicationType.Native);

            long applicationId = core.Db.Query(iQuery);

            ApplicationEntry newApplication = new ApplicationEntry(core, applicationId);

            ApplicationDeveloper developer = ApplicationDeveloper.Create(core, newApplication, core.Session.LoggedInMember);

            try
            {
                ApplicationEntry guestbookAe = core.GetApplication("Profile");
                guestbookAe.Install(core, newApplication);
            }
            catch
            {
            }

            try
            {
                ApplicationEntry guestbookAe = core.GetApplication("GuestBook");
                guestbookAe.Install(core, newApplication);
            }
            catch
            {
            }

            return newApplication;
        }

        public override string UriStub
        {
            get
            {
                if (core.Http.Domain != Hyperlink.Domain)
                {
                    return core.Hyperlink.Uri + "application/" + applicationName + "/";
                }
                else
                {
                    return string.Format("/application/{0}/",
                        applicationName);
                }
            }
        }

        public override string UriStubAbsolute
        {
            get
            {
                return core.Hyperlink.AppendAbsoluteSid(UriStub);
            }
        }

        public override string Uri
        {
            get
            {
                return core.Hyperlink.AppendSid(UriStub);
            }
        }

        public string GetUri(long typeId, long id)
        {
            if (typeId == ItemKey.GetTypeId(core, typeof(User)))
            {
                return Uri;
            }
            else
            {
                return core.Hyperlink.AppendSid(string.Format("/application/{0}?type={1}&id={2}",
                    applicationName, typeId, id));
            }
        }

        public override bool CanModerateComments(User member)
        {
            return false;
        }

        public override bool IsItemOwner(User member)
        {
            if (member != null)
            {
                if (member.UserId == creatorId)
                {
                    return true;
                }
            }
            return false;
        }

        public void GetCan(ushort accessBits, User viewer, out bool canRead, out bool canComment, out bool canCreate, out bool canChange)
        {
            if (viewer != null)
            {
                if (viewer.UserId == creatorId)
                {
                    canRead = true;
                    canComment = true;
                    canCreate = true;
                    canChange = true;
                }
                else
                {
                    canRead = true;
                    canComment = true;
                    canCreate = false;
                    canChange = false;
                }
            }
            else
            {
                canRead = true;
                canComment = false;
                canCreate = false;
                canChange = false;
            }
        }

        public bool HasInstalled(Core core, Primitive owner)
        {
            if (owner != null)
            {
                SelectQuery query = PrimitiveApplicationInfo.GetSelectQueryStub(core, typeof(PrimitiveApplicationInfo));
                query.AddCondition("application_id", Id);
                query.AddCondition("item_id", owner.Id);
                query.AddCondition("item_type_id", owner.TypeId);

                DataTable viewerTable = core.Db.Query(query);

                if (viewerTable.Rows.Count > 0)
                {
                    return true;
                }
            }

            return false;
        }

        public bool Install(Core core, User owner)
        {
            return Install(core, owner, owner);
        }

        public bool Install(Core core, Primitive owner)
        {
            return Install(core, core.Session.LoggedInMember, owner);
        }

        // bool finaliseTransaction
        public bool Install(Core core, User viewer, Primitive owner)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            if ((owner.AppPrimitive & SupportedPrimitives) != owner.AppPrimitive)
            {
                //HttpContext.Current.Response.Write("<br />Primitive bitmap error.");
                return false;
            }

            if (!HasInstalled(core, owner))
            {
                Application newApplication = Application.GetApplication(core, owner.AppPrimitive, this);

                Dictionary<string, PageSlugAttribute> slugs = newApplication.GetPageSlugs(owner.AppPrimitive);

                if (slugs != null)
                {
                    foreach (string slug in slugs.Keys)
                    {
                        if ((slugs[slug].Primitive & owner.AppPrimitive) == owner.AppPrimitive)
                        {
                            string tSlug = slug;
                            Page myPage = Page.Create(core, false, owner, slugs[slug].PageTitle, ref tSlug, 0, string.Empty, PageStatus.PageList, 0, Classifications.None);

                            if (myPage != null)
                            {
                                if (viewer is User)
                                {
                                    myPage.Access.Viewer = (User)viewer;
                                }

                                if (myPage.ListOnly)
                                {
                                    if (HasIcon)
                                    {
                                        myPage.Icon = Icon;
                                        try
                                        {
                                            myPage.Update();
                                        }
                                        catch (UnauthorisedToUpdateItemException)
                                        {
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                newApplication.InitialisePrimitive(owner);

                InsertQuery iQuery = new InsertQuery("primitive_apps");
                iQuery.AddField("application_id", applicationId);
                iQuery.AddField("item_id", owner.Id);
                iQuery.AddField("item_type_id", owner.TypeId);
                iQuery.AddField("app_email_notifications", true);
                // TODO: ACLs

                if (core.Db.Query(iQuery) > 0)
                {
                    return true;
                }
            }
            //HttpContext.Current.Response.Write("<br />Primitive install status error.");
            return false;
        }

        public bool UpdateInstall(Core core, Primitive viewer)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            if (!HasInstalled(core, viewer))
            {
                Install(core, viewer);
            }
            else
            {
                Application newApplication = Application.GetApplication(core, AppPrimitives.Member, this);

                Dictionary<string, PageSlugAttribute> slugs = newApplication.GetPageSlugs(viewer.AppPrimitive);

                foreach (string slug in slugs.Keys)
                {
                    if ((slugs[slug].Primitive & viewer.AppPrimitive) == viewer.AppPrimitive)
                    {
                        SelectQuery query = new SelectQuery("user_pages");
                        query.AddFields("page_id");
                        query.AddCondition("page_item_id", viewer.Id);
                        query.AddCondition("page_item_type_id", viewer.TypeId);
                        query.AddCondition("page_title", slugs[slug].PageTitle);
                        query.AddCondition("page_slug", slug);
                        query.AddCondition("page_parent_path", string.Empty);

                        if (core.Db.Query(query).Rows.Count == 0)
                        {
                            string tSlug = slug;
                            Page myPage = Page.Create(core, false, viewer, slugs[slug].PageTitle, ref tSlug, 0, string.Empty, PageStatus.PageList, 0, Classifications.None);

                            if (myPage != null)
                            {
                                if (viewer is User)
                                {
                                    myPage.Access.Viewer = (User)viewer;
                                }

                                if (myPage.ListOnly)
                                {
                                    if (HasIcon)
                                    {
                                        myPage.Icon = Icon;
                                        try
                                        {
                                            myPage.Update();
                                        }
                                        catch (UnauthorisedToUpdateItemException)
                                        {
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            try
                            {
                                Page myPage = new Page(core, viewer, slug, string.Empty);

                                if (viewer is User)
                                {
                                    myPage.Access.Viewer = (User)viewer;
                                }

                                if (myPage.ListOnly)
                                {
                                    myPage.Title = slugs[slug].PageTitle;
                                    if (!string.IsNullOrEmpty(Icon))
                                    {
                                        myPage.Icon = Icon;
                                    }

                                    myPage.Update();
                                }
                            }
                            catch (PageNotFoundException)
                            {
                                string tSlug = slug;
                                Page myPage = Page.Create(core, false, viewer, slugs[slug].PageTitle, ref tSlug, 0, string.Empty, PageStatus.PageList, 0, Classifications.None);

                                if (viewer is User)
                                {
                                    myPage.Access.Viewer = (User)viewer;
                                }

                                if (myPage.ListOnly)
                                {
                                    if (!string.IsNullOrEmpty(Icon))
                                    {
                                        myPage.Icon = Icon;
                                    }

                                    myPage.Update();
                                }
                            }
                        }
                    }
                    else
                    {
                        // Delete any hanging
                        try
                        {
                            Page myPage = new Page(core, viewer, slug, string.Empty);

                            if (myPage.ListOnly)
                            {
                                myPage.Delete();
                            }
                        }
                        catch (PageNotFoundException)
                        {
                        }
                    }
                }
                return true;
            }
            return false;
        }

        public bool Deauthorise(Core core, Primitive viewer, Primitive owner)
        {
            if (this.ApplicationType != Internals.ApplicationType.OAuth) return false;

            try
            {
                PrimitiveApplicationInfo pai = new PrimitiveApplicationInfo(core, owner, this.Id);

                OAuthToken token = new OAuthToken(core, pai.OAuthAccessToken);
                token.UseToken();
                token.Update();

                DeleteQuery dQuery = new DeleteQuery(typeof(PrimitiveApplicationInfo));
                dQuery.AddCondition("application_id", Id);
                dQuery.AddCondition("item_id", owner.Id);
                dQuery.AddCondition("item_type_id", owner.TypeId);

                if (core.Db.Query(dQuery) > 0)
                {
                    return true;
                }
            }
            catch (InvalidPrimitiveAppInfoException)
            {
            }
            catch (InvalidOAuthTokenException)
            {
            }

            return false;
        }

        public bool Uninstall(Core core, Primitive viewer, Primitive owner)
        {
            return Uninstall(core, viewer, owner, false);
        }

        public bool Uninstall(Core core, Primitive viewer, Primitive owner, bool force)
        {
            if (this.ApplicationType != Internals.ApplicationType.Native) return false;

            if (!force)
            {
                if (isPrimitive)
                {
                    // Groups and Networks are primitive applications
                    return false;
                }

                switch (assemblyName.ToLower())
                {
                    case "profile":
                    case "networks":
                    case "groups":
                    case "gallery":
                    case "mail":
                        return false;
                    case "calendar":
                        if (owner.ItemKey.Equals(viewer.ItemKey))
                        {
                            return false;
                        }
                        break;
                }
            }

            if (HasInstalled(core, owner))
            {
                Application newApplication = Application.GetApplication(core, owner.AppPrimitive, this);

                Dictionary<string, PageSlugAttribute> slugs = newApplication.GetPageSlugs(owner.AppPrimitive);

                foreach (string slug in slugs.Keys)
                {
                    Page page = new Page(core, owner, slug, string.Empty);
                    page.Delete();
                }

                DeleteQuery dQuery = new DeleteQuery(typeof(PrimitiveApplicationInfo));
                dQuery.AddCondition("application_id", Id);
                dQuery.AddCondition("item_id", owner.Id);
                dQuery.AddCondition("item_type_id", owner.TypeId);

                if (core.Db.Query(dQuery) > 0)
                {
                    return true;
                }
            }
            return false;
        }

        public override string GenerateBreadCrumbs(List<string[]> parts)
        {
            string output = string.Empty;
            string path = this.UriStub;

            if (core.IsMobile)
            {
                if (parts.Count > 1)
                {
                    bool lastAbsolute = parts[parts.Count - 2][0].StartsWith("!", StringComparison.Ordinal);
                    if (!lastAbsolute)
                    {
                        for (int i = 0; i < parts.Count - 2; i++)
                        {
                            bool absolute = parts[i][0].StartsWith("!", StringComparison.Ordinal);
                            bool ignore = parts[i][0].StartsWith("*", StringComparison.Ordinal);

                            if ((!ignore) && (!absolute))
                            {
                                path += parts[i][0] + "/";
                            }
                        }
                    }

                    output += string.Format("<span class=\"breadcrumbs\"><strong>&#8249;</strong> <a href=\"{1}\">{0}</a></span>",
                        parts[parts.Count - 2][1], core.Hyperlink.AppendSid((!lastAbsolute ? path : string.Empty) + parts[parts.Count - 2][0].TrimStart(new char[] { '*', '!' })));
                }
                if (parts.Count == 1)
                {
                    output += string.Format("<span class=\"breadcrumbs\"><strong>&#8249;</strong> <a href=\"{1}\">{0}</a></span>",
                        DisplayName, core.Hyperlink.AppendSid(path));
                }
                if (parts.Count == 0)
                {
                    output += string.Format("<span class=\"breadcrumbs\"><strong>&#8249;</strong> <a href=\"{1}\">{0}</a></span>",
                        core.Prose.GetString("HOME"), core.Hyperlink.AppendCoreSid("/"));
                }
            }
            else
            {
                output = string.Format("<a href=\"{1}\">{0}</a>",
                        DisplayName, core.Hyperlink.AppendSid(path));

                for (int i = 0; i < parts.Count; i++)
                {
                    if (parts[i][0] != string.Empty)
                    {
                        bool absolute = parts[i][0].StartsWith("!", StringComparison.Ordinal);
                        bool ignore = parts[i][0].StartsWith("*", StringComparison.Ordinal);

                        output += string.Format(" <strong>&#8249;</strong> <a href=\"{1}\">{0}</a>",
                            parts[i][1], core.Hyperlink.AppendSid((!absolute ? path : string.Empty) + parts[i][0].TrimStart(new char[] { '*', '!' })));
                        if ((!ignore) && (!absolute))
                        {
                            path += parts[i][0] + "/";
                        }
                    }
                }
            }

            return output;
        }

        public void QueueNotifications(Core core, ItemKey itemKey, string notifyFunction)
        {
            core.Queue.PushJob(new Job(core.Settings.QueueNotifications, Id, core.LoggedInMemberId, itemKey.TypeId, itemKey.Id, notifyFunction));
        }

        public void QueueNotifications(Core core, ItemKey itemKey, string notifyFunction, string body)
        {
            core.Queue.PushJob(new Job(core.Settings.QueueNotifications, Id, core.LoggedInMemberId, itemKey.TypeId, itemKey.Id, notifyFunction, body));
        }

        public void SendNotification(Core core, ItemKey donotNotify, User actionBy, ItemKey itemOwnerKey, ItemKey itemKey, string verb, string url)
        {
            long userTypeId = ItemType.GetTypeId(core, typeof(User));
            List<ItemKey> receiverKeys = Subscription.GetSubscribers(core, itemKey, 0, 0);

            foreach (ItemKey receiverKey in receiverKeys)
            {
                if (receiverKey.TypeId == userTypeId && (!receiverKey.Equals(donotNotify)))
                {
                    core.LoadUserProfile(receiverKey.Id);
                }
            }

            foreach (ItemKey receiverKey in receiverKeys)
            {
                if (receiverKey.TypeId == userTypeId && (!receiverKey.Equals(donotNotify)))
                {
                    SendNotification(core, actionBy, core.PrimitiveCache[receiverKey.Id], itemOwnerKey, itemKey, verb, url);
                }
            }
        }

        public void SendNotification(Core core, User actionBy, User receiver, ItemKey itemOwnerKey, ItemKey itemKey, string verb, string url)
        {
            SendNotification(core, actionBy, receiver, itemOwnerKey, itemKey, verb, url, string.Empty);
        }

        public void SendNotification(Core core, User actionBy, User receiver, ItemKey itemOwnerKey, ItemKey itemKey, string verb, string url, string action)
        {
            if (canNotify(core, receiver))
            {
                Notification notification = Notification.Create(core, this, actionBy, receiver, itemOwnerKey, itemKey, verb, url, action);

                if (receiver.UserInfo.EmailNotifications)
                {
                    // Header so we can use the same emailBody for multiple subscribers
                    Template emailTemplate = new Template(core.TemplateEmailPath, "notification.html");

                    emailTemplate.Parse("SITE_TITLE", core.Settings.SiteTitle);
                    emailTemplate.Parse("U_SITE", core.Hyperlink.StripSid(core.Hyperlink.AppendAbsoluteSid(core.Hyperlink.BuildHomeUri())));
                    emailTemplate.Parse("TO_NAME", receiver.DisplayName);
                    core.Display.ParseBbcode(emailTemplate, "NOTIFICATION_MESSAGE", notification.NotificationString, receiver, false, string.Empty, string.Empty, true);

                    // TODO parse action links
                    if (itemKey.GetType(core).Notifiable)
                    {
                        Dictionary<string, string> actions = notification.NotifiedItem.GetNotificationActions(action);

                        foreach (string a in actions.Keys)
                        {
                            VariableCollection actionsVariableCollection = emailTemplate.CreateChild("actions_list");

                            actionsVariableCollection.Parse("ACTION", actions[a]);
                            actionsVariableCollection.Parse("U_ACTION", core.Hyperlink.AppendAbsoluteSid(core.Hyperlink.AppendNvid(notification.NotifiedItem.GetNotificationActionUrl(a), notification.VerificationString)));
                        }
                    }

                    core.Email.SendEmail(receiver.UserInfo.PrimaryEmail, HttpUtility.HtmlDecode(core.Bbcode.Flatten(HttpUtility.HtmlEncode(notification.NotificationString))), emailTemplate);
                }
            }
        }

        private bool canNotify(Core core, User owner)
        {
            SelectQuery query = new SelectQuery("notifications");
            query.AddField(new QueryFunction("notification_id", QueryFunctions.Count, "twentyfour")); //"COUNT(notification_id) as twentyfour");
            query.AddCondition("notification_primitive_id", owner.Id);
            query.AddCondition("notification_primitive_type_id", owner.TypeId);
            query.AddCondition("notification_application", applicationId);
            query.AddCondition("notification_time_ut", ConditionEquality.GreaterThan, UnixTime.UnixTimeStamp() - 60 * 60 * 24);

            // maximum ten per application per day
            // TODO: change this
            if ((long)core.Db.Query(query).Rows[0]["twentyfour"] < 20)
            {
                // this used to return true only if e-mail notifications which is should be checked separately
                return true;
            }
            return false;
        }

        public void PublishToFeed(Core core, User owner, IActionableItem item, string description)
        {
            PublishToFeed(core, owner, item, new List<NumberedItem>(), description);
        }

        public void PublishToFeed(Core core, User owner, IActionableItem item, NumberedItem subItem, string description)
        {
            if (subItem != null)
            {
                PublishToFeed(core, owner, item, new List<NumberedItem> { subItem }, description);
            }
            else
            {
                PublishToFeed(core, owner, item, new List<NumberedItem>(), description);
            }
        }

        public void PublishToFeed(Core core, User owner, IActionableItem item, List<NumberedItem> subItems, string description)
        {
            ItemKey itemKey = item.ItemKey;
            List<ItemKey> subItemKeys = new List<Internals.ItemKey>();

            foreach (NumberedItem i in subItems)
            {
                subItemKeys.Add(i.ItemKey);
            }

            SelectQuery query = new SelectQuery(typeof(Action));
            query.AddField(new QueryFunction("action_id", QueryFunctions.Count, "twentyfour"));
            query.AddCondition("action_primitive_id", owner.ItemKey.Id);
            query.AddCondition("action_primitive_type_id", owner.ItemKey.TypeId);
            query.AddCondition("action_application", applicationId);
            query.AddCondition("action_time_ut", ConditionEquality.GreaterThan, UnixTime.UnixTimeStamp() - 60 * 60 * 24);

            // maximum 48 per application per day
            if ((long)core.Db.Query(query).Rows[0]["twentyfour"] < 48)
            {
                /* Post to Twitter, Facebook, individual */
                if ((owner.UserInfo.TwitterSyndicate && owner.UserInfo.TwitterAuthenticated) || (owner.UserInfo.FacebookSyndicate && owner.UserInfo.FacebookAuthenticated) || (owner.UserInfo.TumblrSyndicate && owner.UserInfo.TumblrAuthenticated))
                {
                    ItemInfo info = item.Info;
                    IActionableItem sharedItem = item;
                    ItemKey sharedItemKey = item.ItemKey;

                    if (subItems.Count == 1)
                    {
                        sharedItemKey = subItems[0].ItemKey;
                        try
                        {
                            info = new ItemInfo(core, subItems[0]);
                        }
                        catch (InvalidIteminfoException)
                        {
                            info = ItemInfo.Create(core, subItems[0]);
                        }

                        if (subItems[0] is IActionableItem)
                        {
                            sharedItem = (IActionableItem)subItems[0];
                        }
                    }

                    if (sharedItemKey.GetType(core).Shareable)
                    {
                        bool publicItem = true;

                        if (item is IPermissibleItem)
                        {
                            IPermissibleItem pitem = (IPermissibleItem)item;
                            publicItem = pitem.Access.IsPublic();
                        }

                        if (item is IPermissibleSubItem)
                        {
                            IPermissibleSubItem pitem = (IPermissibleSubItem)item;
                            publicItem = pitem.PermissiveParent.Access.IsPublic();
                        }

                        if (publicItem)
                        {
                            string sharePrefix = core.Http.Form["share"];
                            if (owner.UserInfo.TwitterAuthenticated)
                            {
                                string twitterDescription = Functions.TrimStringToWord(description, 140 - 7 - Hyperlink.Domain.Length - 3 - 11 - 1, true);

                                if ((sharePrefix == null && owner.UserInfo.TwitterSyndicate) || ((!string.IsNullOrEmpty(sharePrefix)) && core.Http.Form[sharePrefix + "-share-twitter"] != null))
                                {
                                    core.Queue.PushJob(new Job(core.Settings.QueueDefaultPriority, 0, core.LoggedInMemberId, sharedItemKey.TypeId, sharedItemKey.Id, "publishTweet", twitterDescription));
                                }
                            }

                            if (owner.UserInfo.TumblrAuthenticated)
                            {
                                Uri shareUri = new Uri(info.ShareUri);

                                if ((sharePrefix == null && owner.UserInfo.TumblrSyndicate) || ((!string.IsNullOrEmpty(sharePrefix)) && core.Http.Form[sharePrefix + "-share-tumblr"] != null))
                                {
                                    core.Queue.PushJob(new Job(core.Settings.QueueDefaultPriority, 0, core.LoggedInMemberId, sharedItemKey.TypeId, sharedItemKey.Id, "publishTumblr", core.Bbcode.Parse(HttpUtility.HtmlEncode(sharedItem.PostType == ActionableItemType.Photo ? sharedItem.Caption : sharedItem.GetActionBody(subItemKeys)), owner, true, string.Empty, string.Empty) + "<p><a href=\"" + info.ShareUri + "\">" + shareUri.Authority + shareUri.PathAndQuery + "</a></p>"));
                                }
                            }

                            if (owner.UserInfo.FacebookAuthenticated)
                            {
                                if ((sharePrefix == null && owner.UserInfo.FacebookSyndicate) || ((!string.IsNullOrEmpty(sharePrefix)) && core.Http.Form[sharePrefix + "-share-facebook"] != null))
                                {
                                    core.Queue.PushJob(new Job(core.Settings.QueueDefaultPriority, 0, core.LoggedInMemberId, sharedItemKey.TypeId, sharedItemKey.Id, "publishFacebook", description));
                                }
                            }
                        }
                    }
                }

                /* Post to Box Social feed, coalesce */
                SelectQuery query2 = Action.GetSelectQueryStub(core, typeof(Action));
                query2.AddCondition("action_primitive_id", owner.ItemKey.Id);
                query2.AddCondition("action_primitive_type_id", owner.ItemKey.TypeId);
                query2.AddCondition("action_item_id", itemKey.Id);
                query2.AddCondition("action_item_type_id", itemKey.TypeId);
                query2.AddCondition("action_application", applicationId);
                query2.AddCondition("action_time_ut", ConditionEquality.GreaterThan, UnixTime.UnixTimeStamp() - 60 * 60 * 24);
                query2.AddSort(SortOrder.Descending, "action_time_ut");

                DataTable actionDataTable = core.Db.Query(query2);

                if (actionDataTable.Rows.Count > 0)
                {
                    List<ItemKey> subItemsShortList = new List<ItemKey>();
                    
                    Action action = new Action(core, owner, actionDataTable.Rows[0]);

                    SelectQuery query3 = ActionItem.GetSelectQueryStub(core, typeof(ActionItem));
                    query3.AddCondition("action_id", action.Id);
                    query3.LimitCount = 3;

                    DataTable actionItemDataTable = core.Db.Query(query3);

                    if (actionItemDataTable.Rows.Count < 3)
                    {
                        for (int i = 0; i < actionItemDataTable.Rows.Count; i++)
                        {
                            subItemsShortList.Add(new ItemKey((long)actionItemDataTable.Rows[i]["item_id"], (long)actionItemDataTable.Rows[i]["item_type_id"]));
                        }

                        for (int i = 0; i < subItems.Count; i++)
                        {
                            if (subItemsShortList.Count == 3) break;
                            subItemsShortList.Add(subItemKeys[i]);
                        }

                        string body = item.GetActionBody(subItemsShortList);
                        string bodyCache = string.Empty;

                        if (!body.Contains("[user") && !body.Contains("sid=true]"))
                        {
                            bodyCache = Bbcode.Parse(HttpUtility.HtmlEncode(body), null, owner, true, string.Empty, string.Empty);
                        }

                        UpdateQuery uquery = new UpdateQuery(typeof(Action));
                        uquery.AddField("action_time_ut", UnixTime.UnixTimeStamp());
                        uquery.AddField("action_title", item.Action);
                        uquery.AddField("action_body", body);
                        uquery.AddField("action_body_cache", bodyCache);

                        if (subItemsShortList != null && subItemsShortList.Count == 1)
                        {
                            uquery.AddField("interact_item_id", subItemsShortList[0].Id);
                            uquery.AddField("interact_item_type_id", subItemsShortList[0].TypeId);
                        }
                        else
                        {
                            uquery.AddField("interact_item_id", itemKey.Id);
                            uquery.AddField("interact_item_type_id", itemKey.TypeId);
                        }

                        uquery.AddCondition("action_id", action.Id);

                        core.Db.Query(uquery);

                        if (subItems != null)
                        {
                            foreach (ItemKey subItem in subItemKeys)
                            {
                                InsertQuery iquery = new InsertQuery(typeof(ActionItem));
                                iquery.AddField("action_id", action.Id);
                                iquery.AddField("item_id", subItem.Id);
                                iquery.AddField("item_type_id", subItem.TypeId);

                                core.Db.Query(iquery);
                            }
                        }
                    }
                }
                else
                {
                    string body = item.GetActionBody(subItemKeys);
                    string bodyCache = string.Empty;

                    if (!body.Contains("[user") && !body.Contains("sid=true]"))
                    {
                        bodyCache = Bbcode.Parse(HttpUtility.HtmlEncode(body), null, owner, true, string.Empty, string.Empty);
                    }

                    InsertQuery iquery = new InsertQuery(typeof(Action));
                    iquery.AddField("action_primitive_id", owner.ItemKey.Id);
                    iquery.AddField("action_primitive_type_id", owner.ItemKey.TypeId);
                    iquery.AddField("action_item_id", itemKey.Id);
                    iquery.AddField("action_item_type_id", itemKey.TypeId);
                    iquery.AddField("action_title", item.Action);
                    iquery.AddField("action_body", body);
                    iquery.AddField("action_body_cache", bodyCache);
                    iquery.AddField("action_application", applicationId);
                    iquery.AddField("action_time_ut", UnixTime.UnixTimeStamp());

                    if (subItems != null && subItems.Count == 1)
                    {
                        iquery.AddField("interact_item_id", subItemKeys[0].Id);
                        iquery.AddField("interact_item_type_id", subItemKeys[0].TypeId);
                    }
                    else
                    {
                        iquery.AddField("interact_item_id", itemKey.Id);
                        iquery.AddField("interact_item_type_id", itemKey.TypeId);
                    }

                    long actionId = core.Db.Query(iquery);

                    if (subItems != null)
                    {
                        foreach (ItemKey subItem in subItemKeys)
                        {
                            iquery = new InsertQuery(typeof(ActionItem));
                            iquery.AddField("action_id", actionId);
                            iquery.AddField("item_id", subItem.Id);
                            iquery.AddField("item_type_id", subItem.TypeId);

                            core.Db.Query(iquery);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns an Action or null
        /// </summary>
        /// <param name="owner"></param>
        /// <returns></returns>
        public Action GetMostRecentFeedAction(Core core, User owner)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            SelectQuery query = Action.GetSelectQueryStub(core, typeof(Action));
            query.AddSort(SortOrder.Descending, "action_time_ut");
            query.AddCondition("action_application", Id);
            query.AddCondition("action_primitive_id", owner.Id);
            query.AddCondition("action_primitive_type_id", owner.TypeId);
            query.LimitCount = 1;

            DataTable feedTable = core.Db.Query(query);

            if (feedTable.Rows.Count == 1)
            {
                return new Action(core, owner, feedTable.Rows[0]);
            }
            else
            {
                return null;
            }
        }

        public static void ShowPage(Core core, APage page)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            core.Template.SetTemplate("viewapplication.html");
            page.Signature = PageSignature.viewapplication;

            long typeId = core.Functions.RequestLong("type", 0);
            long id = core.Functions.RequestLong("id", 0);

            if (core.Session.IsLoggedIn)
            {
                Primitive viewer = core.Session.LoggedInMember;

                if (typeId > 0)
                {
                    core.PrimitiveCache.LoadPrimitiveProfile(id, typeId);
                    viewer = core.PrimitiveCache[id, typeId];
                }

                if (page.AnApplication.ApplicationType == ApplicationType.Native)
                {
                    if (page.AnApplication.HasInstalled(core, viewer))
                    {
                        core.Template.Parse("U_UNINSTALL", core.Hyperlink.AppendSid(string.Format("{1}dashboard/applications?mode=uninstall&id={0}",
                            page.AnApplication.ApplicationId, viewer.AccountUriStub), true));
                    }
                    else
                    {
                        core.Template.Parse("U_INSTALL", core.Hyperlink.AppendSid(string.Format("{1}dashboard/applications?mode=install&id={0}",
                            page.AnApplication.ApplicationId, viewer.AccountUriStub), true));
                    }
                }

                if (page.AnApplication.ApplicationType == ApplicationType.OAuth)
                {
                    if (page.AnApplication.HasInstalled(core, viewer))
                    {
                        core.Template.Parse("U_REVOKE", core.Hyperlink.AppendSid(string.Format("{1}dashboard/applications?mode=revoke&id={0}",
                            page.AnApplication.ApplicationId, viewer.AccountUriStub), true));
                    }
                }

                if (core.Session.LoggedInMember.Id == page.AnApplication.CreatorId)
                {
                    core.Template.Parse("U_MANAGE", core.Hyperlink.AppendSid(page.AnApplication.AccountUriStub));
                }
            }

            User Creator = new User(core, page.AnApplication.CreatorId, UserLoadOptions.All);

            core.Template.Parse("APPLICATION_NAME", page.AnApplication.Title);
            core.Template.Parse("U_APPLICATION", page.AnApplication.Uri);
            core.Template.Parse("DESCRIPTION", page.AnApplication.Description);
            core.Template.Parse("CREATOR_DISPLAY_NAME", Creator.DisplayName);
            if (page.AnApplication.Thumbnail != null)
            {
                core.Template.Parse("I_THUMBNAIL", page.AnApplication.Thumbnail);
            }

            

            core.InvokeHooks(new HookEventArgs(core, AppPrimitives.Application, page.AnApplication));
        }

        public override string AccountUriStub
        {
            get
            {
                return string.Format("/application/{0}/manage/",
                    Key);
            }
        }

        #region ICommentableItem Members


        public SortOrder CommentSortOrder
        {
            get
            {
                return SortOrder.Descending;
            }
        }

        public byte CommentsPerPage
        {
            get
            {
                return 10;
            }
        }

        #endregion

        public override Access Access
        {
            get
            {
                if (access == null)
                {
                    access = new Access(core, this);
                }

                return access;
            }
        }

        public override bool IsSimplePermissions
        {
            get
            {
                return simplePermissions;
            }
            set
            {
                SetPropertyByRef(new { simplePermissions }, value);
            }
        }

        public override List<AccessControlPermission> AclPermissions
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool IsItemGroupMember(ItemKey viewer, ItemKey key)
        {
            return false;
        }

        public override List<PrimitivePermissionGroup> GetPrimitivePermissionGroups()
        {
            List<PrimitivePermissionGroup> ppgs = new List<PrimitivePermissionGroup>();

            ppgs.Add(new PrimitivePermissionGroup(ItemType.GetTypeId(core, typeof(User)), -1, "L_CREATOR", null, string.Empty));
            ppgs.Add(new PrimitivePermissionGroup(ItemType.GetTypeId(core, typeof(User)), -2, "L_EVERYONE", null, string.Empty));

            return ppgs;
        }

        public override List<User> GetPermissionUsers()
        {
            List<User> users = new List<User>();

            return users;
        }

        public override List<User> GetPermissionUsers(string namePart)
        {
            List<User> users = new List<User>();

            return users;
        }

        public new IPermissibleItem PermissiveParent
        {
            get
            {
                return Owner;
            }
        }

        public ItemKey PermissiveParentKey
        {
            get
            {
                return ItemKey;
            }
        }

        public override bool GetIsMemberOfPrimitive(ItemKey viewer, ItemKey primitiveKey)
        {
            if (primitiveKey.TypeId == ItemType.GetTypeId(core, typeof(User)))
            {

                switch (primitiveKey.Id)
                {
                    case -1: // OWNER
                        if (CreatorId == viewer.Id)
                        {
                            return true;
                        }
                        break;
                    case -2: // EVERYONE
                        if (core.LoggedInMemberId > 0)
                        {
                            return true;
                        }
                        break;
                }
            }

            if (primitiveKey.TypeId == ItemType.GetTypeId(core, typeof(ApplicationEntry)))
            {
                if (primitiveKey.Id == Id)
                {
                    if (CreatorId == viewer.Id)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public override bool CanEditPermissions()
        {
            if (CreatorId == core.LoggedInMemberId)
            {
                return true;
            }

            return false;
        }

        public override bool CanEditItem()
        {
            if (CreatorId == core.LoggedInMemberId)
            {
                return true;
            }

            return false;
        }

        public override bool CanDeleteItem()
        {
            if (CreatorId == core.LoggedInMemberId)
            {
                return true;
            }

            return false;
        }

        public override bool GetDefaultCan(string permission, ItemKey viewer)
        {
            return false;
        }

        public override string DisplayTitle
        {
            get
            {
                return "Application: " + Title;
            }
        }

        public override string ParentPermissionKey(Type parentType, string permission)
        {
            return permission;
        }

        public static ItemKey GetApplicationDevelopersGroupKey(Core core)
        {
            return new ItemKey(-1, ItemType.GetTypeId(core, typeof(ApplicationDeveloper)));
        }

        public string Noun
        {
            get
            {
                return "guest book";
            }
        }

        public override string CoverPhoto
        {
            get
            {
                return string.Empty;
            }
        }

        public override string MobileCoverPhoto
        {
            get
            {
                return string.Empty;
            }
        }

        public static bool CheckApplicationNameValid(string applicationName)
        {
            int matches = 0;

            List<string> disallowedNames = new List<string>();
            disallowedNames.Add("about");
            disallowedNames.Add("copyright");
            disallowedNames.Add("register");
            disallowedNames.Add("sign-in");
            disallowedNames.Add("log-in");
            disallowedNames.Add("help");
            disallowedNames.Add("safety");
            disallowedNames.Add("privacy");
            disallowedNames.Add("terms-of-service");
            disallowedNames.Add("site-map");
            disallowedNames.Add(WebConfigurationManager.AppSettings["boxsocial-title"].ToLower());
            disallowedNames.Add("blogs");
            disallowedNames.Add("profiles");
            disallowedNames.Add("search");
            disallowedNames.Add("communities");
            disallowedNames.Add("community");
            disallowedNames.Add("constitution");
            disallowedNames.Add("profile");
            disallowedNames.Add("my-profile");
            disallowedNames.Add("history");
            disallowedNames.Add("get-active");
            disallowedNames.Add("statistics");
            disallowedNames.Add("blog");
            disallowedNames.Add("categories");
            disallowedNames.Add("members");
            disallowedNames.Add("users");
            disallowedNames.Add("upload");
            disallowedNames.Add("support");
            disallowedNames.Add("account");
            disallowedNames.Add("history");
            disallowedNames.Add("browse");
            disallowedNames.Add("feature");
            disallowedNames.Add("featured");
            disallowedNames.Add("favourites");
            disallowedNames.Add("likes");
            disallowedNames.Add("dev");
            disallowedNames.Add("dcma");
            disallowedNames.Add("coppa");
            disallowedNames.Add("guidelines");
            disallowedNames.Add("press");
            disallowedNames.Add("jobs");
            disallowedNames.Add("careers");
            disallowedNames.Add("feedback");
            disallowedNames.Add("create");
            disallowedNames.Add("subscribe");
            disallowedNames.Add("subscriptions");
            disallowedNames.Add("rate");
            disallowedNames.Add("comment");
            disallowedNames.Add("mail");
            disallowedNames.Add("video");
            disallowedNames.Add("videos");
            disallowedNames.Add("music");
            disallowedNames.Add("podcast");
            disallowedNames.Add("podcasts");
            disallowedNames.Add("security");
            disallowedNames.Add("bugs");
            disallowedNames.Add("beta");
            disallowedNames.Add("friend");
            disallowedNames.Add("friends");
            disallowedNames.Add("family");
            disallowedNames.Add("promotion");
            disallowedNames.Add("birthday");
            disallowedNames.Add("account");
            disallowedNames.Add("settings");
            disallowedNames.Add("admin");
            disallowedNames.Add("administrator");
            disallowedNames.Add("administrators");
            disallowedNames.Add("root");
            disallowedNames.Add("my-account");
            disallowedNames.Add("member");
            disallowedNames.Add("anonymous");
            disallowedNames.Add("legal");
            disallowedNames.Add("contact");
            disallowedNames.Add("aonlinesite");
            disallowedNames.Add("images");
            disallowedNames.Add("image");
            disallowedNames.Add("styles");
            disallowedNames.Add("style");
            disallowedNames.Add("theme");
            disallowedNames.Add("header");
            disallowedNames.Add("footer");
            disallowedNames.Add("head");
            disallowedNames.Add("foot");
            disallowedNames.Add("bin");
            disallowedNames.Add("images");
            disallowedNames.Add("templates");
            disallowedNames.Add("cgi-bin");
            disallowedNames.Add("cgi");
            disallowedNames.Add("web.config");
            disallowedNames.Add("report");
            disallowedNames.Add("rules");
            disallowedNames.Add("script");
            disallowedNames.Add("scripts");
            disallowedNames.Add("css");
            disallowedNames.Add("img");
            disallowedNames.Add("App_Data");
            disallowedNames.Add("test");
            disallowedNames.Add("sitepreview");
            disallowedNames.Add("plesk-stat");
            disallowedNames.Add("jakarta");
            disallowedNames.Add("storage");
            disallowedNames.Add("netalert");
            disallowedNames.Add("group");
            disallowedNames.Add("groups");
            disallowedNames.Add("create");
            disallowedNames.Add("edit");
            disallowedNames.Add("delete");
            disallowedNames.Add("remove");
            disallowedNames.Add("sid");
            disallowedNames.Add("network");
            disallowedNames.Add("networks");
            disallowedNames.Add("cart");
            disallowedNames.Add("api");
            disallowedNames.Add("feed");
            disallowedNames.Add("rss");
            disallowedNames.Sort();

            if (disallowedNames.BinarySearch(applicationName.ToLower()) >= 0)
            {
                matches++;
            }

            if (!Regex.IsMatch(applicationName, @"^([A-Za-z0-9\-_\.\!~\*'&=\$].+)$"))
            {
                matches++;
            }

            applicationName = applicationName.Normalize().ToLower();

            if (applicationName.Length < 2)
            {
                matches++;
            }

            if (applicationName.Length > 64)
            {
                matches++;
            }

            if (applicationName.EndsWith(".aspx", StringComparison.Ordinal))
            {
                matches++;
            }

            if (applicationName.EndsWith(".asax", StringComparison.Ordinal))
            {
                matches++;
            }

            if (applicationName.EndsWith(".php", StringComparison.Ordinal))
            {
                matches++;
            }

            if (applicationName.EndsWith(".html", StringComparison.Ordinal))
            {
                matches++;
            }

            if (applicationName.EndsWith(".gif", StringComparison.Ordinal))
            {
                matches++;
            }

            if (applicationName.EndsWith(".png", StringComparison.Ordinal))
            {
                matches++;
            }

            if (applicationName.EndsWith(".js", StringComparison.Ordinal))
            {
                matches++;
            }

            if (applicationName.EndsWith(".bmp", StringComparison.Ordinal))
            {
                matches++;
            }

            if (applicationName.EndsWith(".jpg", StringComparison.Ordinal))
            {
                matches++;
            }

            if (applicationName.EndsWith(".jpeg", StringComparison.Ordinal))
            {
                matches++;
            }

            if (applicationName.EndsWith(".zip", StringComparison.Ordinal))
            {
                matches++;
            }

            if (applicationName.EndsWith(".jsp", StringComparison.Ordinal))
            {
                matches++;
            }

            if (applicationName.EndsWith(".cfm", StringComparison.Ordinal))
            {
                matches++;
            }

            if (applicationName.EndsWith(".exe", StringComparison.Ordinal))
            {
                matches++;
            }

            if (applicationName.StartsWith(".", StringComparison.Ordinal))
            {
                matches++;
            }

            if (applicationName.EndsWith(".", StringComparison.Ordinal))
            {
                matches++;
            }

            if (matches > 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public static bool CheckApplicationNameUnique(Core core, string applicationName)
        {
            if (core.Db.Query(string.Format("SELECT application_name FROM applications WHERE LCASE(application_name) = '{0}';",
                Mysql.Escape(applicationName.ToLower()))).Rows.Count > 0)
            {
                return false;
            }
            return true;
        }

    }

    public class InvalidApplicationException : Exception
    {
    }
}
