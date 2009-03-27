using System;
using System.Collections.Generic;
using System.Text;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    public struct ApplicationSlugInfo
    {
        public string Stub;
        public string SlugEx;
        public AppPrimitives Primitives;

        public ApplicationSlugInfo(string stub, string slugEx, AppPrimitives primitives)
        {
            Stub = stub;
            SlugEx = slugEx;
            Primitives = primitives;
        }
    }

    public struct ApplicationModule
    {
        public string Slug;

        public ApplicationModule(string slug)
        {
            Slug = slug;
        }
    }

    public struct ApplicationCommentType
    {
        public string Type;

        public ApplicationCommentType(string type)
        {
            Type = type;
        }
    }

    public struct ApplicationItemAccessPermissions
    {
        public long TypeId;
        public string PermissionName;

        public ApplicationItemAccessPermissions(string name, long typeId)
        {
            TypeId = typeId;
            PermissionName = name;
        }
    }

    public class ApplicationInstallationInfo
    {
        private List<ApplicationSlugInfo> applicationSlugs = new List<ApplicationSlugInfo>();
        private List<ApplicationModule> applicationModules = new List<ApplicationModule>();
        private List<ApplicationCommentType> applicationCommentTypes = new List<ApplicationCommentType>();
        private List<ApplicationItemAccessPermissions> applicationItemAccessPermissions = new List<ApplicationItemAccessPermissions>();

        public void AddSlug(string stub, string slugEx, AppPrimitives primitives)
        {
            applicationSlugs.Add(new ApplicationSlugInfo(stub, slugEx, primitives));
        }

        public void AddModule(string slug)
        {
            applicationModules.Add(new ApplicationModule(slug));
        }

        public void AddCommentType(string type)
        {
            applicationCommentTypes.Add(new ApplicationCommentType(type));
        }

        public void AddItemAccessPermission(string name, long typeId)
        {
            applicationItemAccessPermissions.Add(new ApplicationItemAccessPermissions(name, typeId));
        }

        public void AddItemAccessPermission(params ApplicationItemAccessPermissions[] values)
        {
            applicationItemAccessPermissions.AddRange(values);
        }

        public List<ApplicationSlugInfo> ApplicationSlugs
        {
            get
            {
                return applicationSlugs;
            }
        }

        public List<ApplicationModule> ApplicationModules
        {
            get
            {
                return applicationModules;
            }
        }

        public List<ApplicationCommentType> ApplicationCommentTypes
        {
            get
            {
                return applicationCommentTypes;
            }
        }

        public List<ApplicationItemAccessPermissions> ApplicationItemAccessPermissions
        {
            get
            {
                return applicationItemAccessPermissions;
            }
        }
    }
}
