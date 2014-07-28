using System;
using System.Collections.Generic;
using System.Text;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    public class ApplicationSlugInfo
    {
        public string Stub;
        public string SlugEx;
        public AppPrimitives Primitives;
        public bool IsStatic;

        public ApplicationSlugInfo(string stub, string slugEx, AppPrimitives primitives, bool isStatic)
        {
            Stub = stub;
            SlugEx = slugEx;
            Primitives = primitives;
            IsStatic = isStatic;
        }

        public ApplicationSlugInfo(ShowAttribute showattr)
        {
            Stub = showattr.Stub;
            SlugEx = showattr.Slug;
            Primitives = showattr.Primitives;
            IsStatic = false;
        }

        public ApplicationSlugInfo(StaticShowAttribute showattr)
        {
            Stub = showattr.Stub;
            SlugEx = showattr.Slug;
            Primitives = AppPrimitives.None;
            IsStatic = true;
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
        private List<ApplicationSlugInfo> applicationSlugs = new List<ApplicationSlugInfo>();
        private List<ApplicationModule> applicationModules = new List<ApplicationModule>();
        private List<ApplicationCommentType> applicationCommentTypes = new List<ApplicationCommentType>();

        private void addSlug(ApplicationSlugInfo slugInfo)
        {
            bool found = false;
            for (int i = 0; i < applicationSlugs.Count; i++)
            {
                if (applicationSlugs[i].Stub == slugInfo.Stub && applicationSlugs[i].SlugEx == slugInfo.SlugEx && applicationSlugs[i].IsStatic == slugInfo.IsStatic)
                {
                    found = true;
                    ApplicationSlugInfo asi = applicationSlugs[i];
                    asi.Primitives = asi.Primitives | slugInfo.Primitives;
                    break;
                }
            }

            if (!found)
            {
                applicationSlugs.Add(slugInfo);
            }
        }

        public void AddSlug(string stub, string slugEx, AppPrimitives primitives, bool isStatic)
        {
            addSlug(new ApplicationSlugInfo(stub, slugEx, primitives, isStatic));
        }

        public void AddSlug(string stub, string slugEx, AppPrimitives primitives)
        {
            addSlug(new ApplicationSlugInfo(stub, slugEx, primitives, false));
        }

        public void AddSlug(ShowAttribute showAttr)
        {
            addSlug(new ApplicationSlugInfo(showAttr));
        }

        public void AddSlug(StaticShowAttribute showAttr)
        {
            addSlug(new ApplicationSlugInfo(showAttr));
        }

        public void AddModule(string slug)
        {
            applicationModules.Add(new ApplicationModule(slug));
        }

        public void AddCommentType(string type)
        {
            applicationCommentTypes.Add(new ApplicationCommentType(type));
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
    }
}
