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

        public void Parse(string input)
        {
            int strLength = input.Length;
            int lineIndex = 0;
            bool inQuote = false;
            bool inSingleQuote = false;
            bool inDoubleQuote = false;
            char current = '\0';
            char previous = '\0';

            int i = 0;
            while (i < strLength)
            {
                previous = current;
                current = input[i];
                lineIndex++;
                if (current == '\n')
                {
                    lineIndex = -1;
                    continue;
                }

                if (!inQuote && current == '\'')
                {
                    inQuote = inSingleQuote = true;
                }

                if (!inQuote && current == '"')
                {
                    inQuote = inDoubleQuote = true;
                }

                if (inSingleQuote && current == '\'')
                {
                    inQuote = inSingleQuote = false;
                }

                if (inDoubleQuote && current == '"')
                {
                    inQuote = inDoubleQuote = false;
                }

                i++;
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

        public bool HasKey(string key)
        {
            if (styles.ContainsKey(key))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public StyleStyle this[string key]
        {
            get
            {
                return styles[key];
            }
        }

        public void Parse(string input)
        {
            bool inStyle = false;
            int strLength = input.Length;
            int lineIndex = 0;
            bool inComment = false;
            bool inQuote = false;
            bool inSingleQuote = false;
            bool inDoubleQuote = false;
            char current = '\0';
            char previous = '\0';
            string rule = "";
            string style = "";

            int i = 0;
            while (i < strLength)
            {
                previous = current;
                current = input[i];
                lineIndex++;
                if (current == '\n')
                {
                    lineIndex = -1;
                    continue;
                }

                if (current == '*' && previous == '/')
                {
                    inComment = true;
                    continue;
                }

                if (current == '/' && previous == '*')
                {
                    inComment = false;
                    continue;
                }

                if (!inQuote && current == '\'')
                {
                    inQuote = inSingleQuote = true;
                }

                if (!inQuote && current == '"')
                {
                    inQuote = inDoubleQuote = true;
                }

                if (inSingleQuote && current == '\'')
                {
                    inQuote = inSingleQuote = false;
                }

                if (inDoubleQuote && current == '"')
                {
                    inQuote = inDoubleQuote = false;
                }

                if (!inComment)
                {
                    if (!inStyle)
                    {
                        if (current == '{')
                        {
                            inStyle = true;
                            rule.Trim(new char[] { ' ', '\t' });
                            continue;
                        }

                        if (string.IsNullOrEmpty(rule))
                        {
                            if (current != '\t' && current != ' ')
                            {
                                rule = current.ToString();
                            }
                        }
                        else
                        {
                            rule += current;
                        }
                    }
                    else
                    {
                        if (!inQuote)
                        {
                            if (current == '}')
                            {
                                inStyle = false;

                                StyleStyle tempStyle = new StyleStyle(rule);
                                tempStyle.Parse(style);
                                styles.Add(rule, tempStyle);

                                style = "";
                                rule = "";
                                continue;
                            }
                        }
                        else
                        {
                            style += current;
                        }
                    }
                }

                i++;
            }
        }
    }
}
