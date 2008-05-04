using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    public enum StyleGenerator
    {
        Theme,
        Standard,
        Advanced,
    }

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
                if (value.StartsWith("url"))
                {
                    try
                    {
                        return Regex.Match(value, "url(\\W*)\\((['\"\\W]*)([\\w]+?://[\\w\\#$%&~/.\\-;:=,?@\\[\\]+]*?)(['\"\\W]*)\\)").Groups[3].Value;
                    }
                    catch
                    {
                        return "";
                    }
                }
                else
                {
                    return value;
                }
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
            return string.Format("{0}: {1}",
                key, value);
        }
    }

    public sealed class StylePropertyList : StyleStyle
    {
        public StylePropertyList()
            : base("")
        {
        }

        public override string ToString()
        {
            StringBuilder style = new StringBuilder();

            foreach (string propertyKey in properties.Keys)
            {
                style.Append(properties[propertyKey].ToString() + ";");
            }

            return style.ToString();
        }
    }

    public class StyleStyle
    {
        private string key;
        protected Dictionary<string, StyleProperty> properties;

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

        public bool HasProperty(string property)
        {
            if (properties.ContainsKey(property))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public StyleProperty this[string property]
        {
            get
            {
                return properties[property];
            }
        }

        public void Parse(string input)
        {
            bool inValue = false;
            int strLength = input.Length;
            int lineIndex = 0;
            bool inQuote = false;
            bool inSingleQuote = false;
            bool inDoubleQuote = false;
            char current = '\0';
            char previous = '\0';
            char next = '\0';
            string key = "";
            string value = "";

            //int i = 0;
            //while (i < strLength)
            input.PadRight(1);
            for (int i = 0; i < input.Length - 1; i++)
            {
                previous = current;
                current = input[i];
                next = input[i + 1];

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

                if (!inValue)
                {
                    if (!inQuote)
                    {
                        if (current == ':')
                        {
                            inValue = true;
                            key = key.Trim(new char[] { ' ', '\t', '\r', '\n' });
                            continue;
                        }
                        else
                        {
                            key += current;
                        }
                    }
                    else
                    {
                        key += current;
                    }
                }
                else
                {
                    if (!inQuote)
                    {
                        if (current == ';' || i == strLength - 1)
                        {
                            inValue = false;

                            properties.Add(key, new StyleProperty(key, value.Trim(new char[] { ' ', '\t', '\r', '\n' })));

                            key = "";
                            value = "";

                            continue;
                        }
                        else
                        {
                            value += current;
                        }
                    }
                    else
                    {
                        value += current;
                    }
                }

                //i++;
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
                style.AppendLine(properties[propertyKey].ToString() + ";");
            }
            style.Append("}");

            return style.ToString();
        }
    }

    public class CascadingStyleSheet
    {
        private Dictionary<string, StyleStyle> styles;
        private StyleGenerator generator;

        public CascadingStyleSheet()
        {
            styles = new Dictionary<string, StyleStyle>();
            generator = StyleGenerator.Advanced;
        }

        public StyleGenerator Generator
        {
            get
            {
                return generator;
            }
            set
            {
                generator = value;
            }
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
            int lineNo = 0;
            bool inComment = false;
            bool inQuote = false;
            bool inSingleQuote = false;
            bool inDoubleQuote = false;
            char current = '\0';
            char previous = '\0';
            char next = '\0';
            string rule = "";
            string style = "";
            string line = "";

            //int i = 0;
            //while (i < strLength)
            input.PadRight(1);
            for (int i = 0; i < input.Length - 1; i++)
            {
                previous = current;
                current = input[i];
                next = input[i + 1];

                lineIndex++;
                if (current == '\n')
                {
                    if (lineNo == 0)
                    {
                        switch (line)
                        {
                            case "/*Theme*/":
                                generator = StyleGenerator.Theme;
                                break;
                            case "/*Standard*/":
                                generator = StyleGenerator.Standard;
                                break;
                            default:
                                generator = StyleGenerator.Advanced;
                                break;
                        }
                    }

                    lineIndex = -1;
                    lineNo++;
                    line = "";
                    continue;
                }
                else if (current != '\r')
                {
                    line += current;
                }

                if (current == '/' && next == '*')
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
                            rule = rule.Trim(new char[] { ' ', '\t', '\r', '\n' });
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
                            else
                            {
                                style += current;
                            }
                        }
                        else
                        {
                            style += current;
                        }
                    }
                }

                //i++;
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            switch (generator)
            {
                case StyleGenerator.Theme:
                    sb.AppendLine("/*Theme*/");
                    break;
                case StyleGenerator.Standard:
                    sb.AppendLine("/*Standard*/");
                    break;
            }

            foreach (string key in styles.Keys)
            {
                sb.AppendLine(styles[key].ToString());
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}
