using System;
using System.Collections.Generic;
using System.Text;

namespace BoxSocial.Internals
{
    public sealed class HookEventArgs
    {
        public Core core;
        private AppPrimitives pageType;
        private Primitive owner;

        public AppPrimitives PageType
        {
            get
            {
                return pageType;
            }
        }

        public Primitive Owner
        {
            get
            {
                return owner;
            }
        }

        public HookEventArgs(Core core, AppPrimitives pageType, Primitive owner)
        {
            this.core = core;
            this.pageType = pageType;
            this.owner = owner;
        }
    }
}
