using System;
using System.Collections.Generic;
using System.Text;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    public struct ApplicationSlug
    {
        public string Stub;
        public string SlugEx;
        public AppPrimitives Primitives;

        public ApplicationSlug(string stub, string slugEx, AppPrimitives primitives)
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

    public class ApplicationInstallationInfo
    {
        private List<ApplicationSlug> applicationSlugs = new List<ApplicationSlug>();
        private List<ApplicationModule> applicationModules = new List<ApplicationModule>();
        private List<ApplicationCommentType> applicationCommentTypes = new List<ApplicationCommentType>();

        public void AddSlug(string stub, string slugEx, AppPrimitives primitives)
        {
            applicationSlugs.Add(new ApplicationSlug(stub, slugEx, primitives));
        }

        public void AddModule(string slug)
        {
            applicationModules.Add(new ApplicationModule(slug));
        }

        public void AddCommentType(string type)
        {
            applicationCommentTypes.Add(new ApplicationCommentType(type));
        }

        public List<ApplicationSlug> ApplicationSlugs
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
    }
}
