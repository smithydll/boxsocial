using System;
using System.Collections.Generic;
using System.Text;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    public struct StyleProperty
    {
        private string key;
        private string value;

        public string Key
        {
            get
            {
                return key;
            }
        }

        public string Value
        {
            get
            {
                return value;
            }
            set
            {
                this.value = value;
            }
        }

        public void SetValue(string value)
        {
            this.value = value;
        }

        public StyleProperty(string key, string value)
        {
            this.key = key;
            this.value = value;
        }

        public override string ToString()
        {
            return string.Format("{0} : {1}",
                key, value);
        }
    }

    public class StyleStyle
    {
        private string key;
        private Dictionary<string, StyleProperty> properties;

        public StyleStyle(string key)
        {
            this.key = key;
            properties = new Dictionary<string, StyleProperty>();
        }

        public void SetProperty(string key, string value)
        {
            if (properties.ContainsKey(key))
            {
                properties[key].SetValue(value);
            }
            else
            {
                properties.Add(key, new StyleProperty(key, value));
            }
        }

        public override string ToString()
        {
            StringBuilder style = new StringBuilder();

            style.Append(key);
            style.AppendLine(" {");
            foreach (string propertyKey in properties.Keys)
            {
                style.Append("\t");
                style.AppendLine(properties[propertyKey].ToString());
            }
            style.AppendLine("}");

            return style.ToString();
        }
    }

    public class CascadingStyleSheet
    {
        private Dictionary<string, StyleStyle> styles;

        public CascadingStyleSheet()
        {
            styles = new Dictionary<string, StyleStyle>();
        }

        public void AddStyle(string key)
        {
            styles.Add(key, new StyleStyle(key));
        }
    }
}
