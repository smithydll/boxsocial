using System;
using System.Collections.Generic;
using System.Text;

namespace BoxSocial.Forms
{
    public class Image : FormField
    {
        private string uri;
        private StyleLength width;
        private StyleLength height;
        private ScriptProperty script;
        private string styleClass;

        public StyleLength Width
        {
            get
            {
                return width;
            }
            set
            {
                width = value;
            }
        }

        public StyleLength Height
        {
            get
            {
                return height;
            }
            set
            {
                height = value;
            }
        }

        public ScriptProperty Script
        {
            get
            {
                return script;
            }
        }

        public Image(string name, string uri)
        {
            this.name = name;

            this.uri = uri;
            this.width = new StyleLength();
            this.height = new StyleLength();
            this.script = new ScriptProperty();
            this.styleClass = string.Empty;
        }

        public override string ToString()
        {
            return ToString(Forms.DisplayMedium.Desktop);
        }

        public override string ToString(DisplayMedium medium)
        {
            string style = string.Empty;
            if (Width.Length > 0 || Width.Length < 0)
            {
                style = "width: " + Width.ToString() + "; ";
            }
            if (Height.Length > 0 || Height.Length < 0)
            {
                style = "height: " + Height.ToString() + "; ";
            }

            return string.Format("<img src=\"{0}\" name=\"{1}\" style=\"{2}\"{3} />",
                uri,
                name,
                style,
                (string.IsNullOrEmpty(styleClass)) ? string.Empty : " class=\"" + styleClass + "\"");

        }

        public override void SetValue(string value)
        {
            // Do Nothin
        }
    }
}
