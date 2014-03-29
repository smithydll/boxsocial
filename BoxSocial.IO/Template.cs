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
using System.IO;
using System.Collections.Generic;
using System.Resources;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Caching;
using BoxSocial.Forms;

namespace BoxSocial.IO
{
    public struct TemplateVariable
    {
        public string Name;
        public int Index;

        public TemplateVariable(string name, int index)
        {
            this.Name = name;
            this.Index = index;
        }
    }

    public class VariableCollection
    {
        private DisplayMedium medium;
        private string loopName;
        private Dictionary<string, List<VariableCollection>> childLoops = new Dictionary<string, List<VariableCollection>>(4, StringComparer.Ordinal);
        private Dictionary<string, string> variables = new Dictionary<string, string>(128, StringComparer.Ordinal);
        private VariableCollection parentCollection = null;

        public DisplayMedium Medium
        {
            get
            {
                return medium;
            }
            set
            {
                medium = value;
            }
        }

        internal VariableCollection(DisplayMedium medium)
        {
            this.medium = medium;
            loopName = String.Empty;
        }

        public VariableCollection(DisplayMedium medium, string name)
        {
            this.medium = medium;
            loopName = name;
        }

        public VariableCollection CreateChild(string name)
        {
            string fullName = (string.IsNullOrEmpty(loopName)) ? name : loopName + "." + name;

            VariableCollection vc = new VariableCollection(medium, fullName);
            vc.parentCollection = this;

            if (!ContainsLoop(fullName))
            {
                childLoops.Add(fullName, new List<VariableCollection>());
            }

            childLoops[fullName].Add(vc);

            if (!variables.ContainsKey(name))
            {
                variables.Add(name, "IS_DEF");
            }

            return vc;
        }

        public void Parse(string key, string value)
        {
            if (!variables.ContainsKey(key))
            {
                variables.Add(key, HttpUtility.HtmlEncode(value));
            }
        }

        public void Parse(string key, long value)
        {
            if (!variables.ContainsKey(key))
            {
                variables.Add(key, HttpUtility.HtmlEncode(value.ToString()));
            }
        }

        public void Parse(string key, long value, bool longNumberFormat)
        {
            if (!variables.ContainsKey(key))
            {
                if (longNumberFormat)
                {
                    throw new NotImplementedException("Use Functions.LargeIntegerToString instead");
                }
                else
                {
                    variables.Add(key, HttpUtility.HtmlEncode(value.ToString()));
                }
            }
        }

        public void Parse(string key, FormField formField)
        {
            if (formField.GetType().Assembly.GetName().Name == "BoxSocial.Forms" ||
                formField.GetType().Assembly.GetName().Name == "BoxSocial.Internals")
            {
                if (!variables.ContainsKey(key))
                {
                    variables.Add(key, formField.ToString(medium));
                }
            }
        }

        public void Parse(string key, Template template)
        {
                if (!variables.ContainsKey(key))
                {
                    variables.Add(key, template.ToString());
                }
        }

        /// <summary>
        /// Parse raw data to a template, only valid for Box Social internals
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ParseRaw(string key, string value)
        {
            Assembly asm = Assembly.GetCallingAssembly();
            if (asm.GetName().Name == "BoxSocial.Internals")
            {
                if (!variables.ContainsKey(key))
                {
                    variables.Add(key, value);
                }
            }
        }

        internal void parseRaw(string key, string value)
        {
            if (!variables.ContainsKey(key))
            {
                variables.Add(key, value);
            }
        }

        public void ParseVariables(string key, string value)
        {
            try
            {
                variables.Add(key, value);
            }
            catch
            {
            }
        }

        public void ParseVariables(Dictionary<string, string> vars)
        {
            foreach (string key in vars.Keys)
            {
                variables.Add(key, vars[key]);
            }
        }

        internal bool ContainsLoop(string name)
        {
            return childLoops.ContainsKey(name);
        }

        internal int LoopCount(string name)
        {
            return childLoops[name].Count;
        }

        internal string this[string key]
        {
            get
            {
                /*if (variables.ContainsKey(key))
                {
                    return variables[key];
                }
                else
                {
                    if (parentCollection != null)
                    {
                        return parentCollection[key];
                    }
                }
                return String.Empty;*/
                string returnValue;
                if (TryGetValue(key, out returnValue))
                {
                    return returnValue;
                }
                else
                {
                    return String.Empty;
                }
            }
        }

        internal bool ContainsKey(string key)
        {
            if (variables.ContainsKey(key))
            {
                return true;
            }
            else
            {
                if (parentCollection != null)
                {
                    return parentCollection.ContainsKey(key);
                }
            }
            return false;
        }
		
		internal bool TryGetValue(string key, out string value)
		{
			if (!variables.TryGetValue(key, out value))
			{
				if (parentCollection != null)
                {
					return parentCollection.TryGetValue(key, out value);
				}
				else
				{
					return false;
				}
			}
			else
			{
				return true;
			}
		}

        internal List<VariableCollection> GetChildCollection(string name)
        {
            return childLoops[name];
        }

        internal string Path
        {
            get
            {
                return loopName;
            }
        }
    }

    public class Template
    {
        private IProse prose;
        protected VariableCollection variables;
        private Dictionary<string, Assembly> pageAssembly = new Dictionary<string, Assembly>(8, StringComparer.Ordinal);

        private string template;
        //private Dictionary<string, string> loopTemplates;

        private string path;
        private string templateName;
        private string templateAssembly;
        
        private static Object pathLock = new object();
        private static string templatePath = null;

        private DisplayMedium medium;

        private Dictionary<string, int> instances = new Dictionary<string,int>();
        
        public static string Path
        {
            set
            {
                lock (pathLock)
                {
                    if (templatePath == null)
                    {
                        templatePath = value;
                    }
                }
            }
            get
            {
                return templatePath;
            }
        }

        public DisplayMedium Medium
        {
            get
            {
                return medium;
            }
            set
            {
                medium = value;
                variables.Medium = medium;
            }
        }

        public void AddPageAssembly(Assembly value)
        {
            if (!pageAssembly.ContainsKey(value.GetName().Name))
            {
                pageAssembly.Add(value.GetName().Name, value);
            }
        }

        public Template()
        {
            this.variables = new VariableCollection(medium);
            this.path = Path;
        }

        public Template(Assembly assembly, string templateName)
        {
            this.variables = new VariableCollection(medium);
            this.path = Path;

            AddPageAssembly(assembly);

            this.templateAssembly = assembly.GetName().Name;
            this.templateName = templateName;
        }

        public Template(string fileName)
        {
            this.variables = new VariableCollection(medium);
            this.path = Path;
            templateName = fileName;
        }

        public Template(string path, string fileName)
        {
            this.variables = new VariableCollection(medium);
            this.path = path;
            templateName = fileName;
        }

        public void SetTemplate(string fileName)
        {
            templateAssembly = null;
            templateName = fileName;
        }

        public void SetTemplate(string assembly, string fileName)
        {
            templateAssembly = assembly;
            templateName = fileName;
        }

        public void SetTemplate(Assembly assembly, string fileName)
        {
            AddPageAssembly(assembly);

            templateAssembly = assembly.GetName().Name;
            templateName = fileName;
        }

        // TODO: reconsider this into the constructor
        public void SetProse(IProse prose)
        {
            this.prose = prose;
        }

        public void Parse(string key, string value)
        {
            variables.Parse(key, value);
        }

        public void Parse(string key, object value)
        {
            variables.Parse(key, value.ToString());
        }

        public void Parse(string key, FormField formField)
        {
            if (formField.GetType().Assembly.GetName().Name == "BoxSocial.Forms" ||
                formField.GetType().Assembly.GetName().Name == "BoxSocial.Internals")
            {
                variables.Parse(key, formField);
            }
        }

        /// <summary>
        /// Parse raw data to a template, only valid for Box Social internals
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ParseRaw(string key, string value)
        {
            Assembly asm = Assembly.GetCallingAssembly();
            if (asm.GetName().Name == "BoxSocial.Internals")
            {
                variables.parseRaw(key, value);
            }
        }

        public void ParseVariables(string key, string value)
        {
            variables.Parse(key, value);
        }

        public void ParseVariables(Dictionary<string, string> vars)
        {
            variables.ParseVariables(vars);
        }

        public VariableCollection CreateChild(string name)
        {
            return variables.CreateChild(name);
        }

        // We want to cache the template file as accessing resources can be slow
        private static Object templatesLock = new object();
        private static Dictionary<string, string> templates = new Dictionary<string, string>(128, StringComparer.Ordinal);

        public static bool populateTemplateCache()
        {
            System.Web.Caching.Cache cache;
            object o = null;

            if (HttpContext.Current != null && HttpContext.Current.Cache != null)
            {
                cache = HttpContext.Current.Cache;
            }
            else
            {
                cache = new Cache();
            }

            if (cache != null)
            {
                try
                {
                    o = cache.Get("templates");
                    return true;
                }
                catch (NullReferenceException)
                {
                }
            }

            lock (templatesLock)
            {
                if (o != null && o.GetType() == typeof(System.Collections.Generic.Dictionary<string, string>))
                {
                    templates = (Dictionary<string, string>)o;
                }
                else
                {
                    templates = new Dictionary<string, string>(128, StringComparer.Ordinal);

                    if (cache != null)
                    {
                        cache.Add("templates", templates, null, Cache.NoAbsoluteExpiration, new TimeSpan(1, 0, 0), CacheItemPriority.High, null);
                    }
                }
            }
            return false;
        }

        private static void saveTemplateCache(Dictionary<string, string> templates)
        {
            System.Web.Caching.Cache cache;

            if (HttpContext.Current != null && HttpContext.Current.Cache != null)
            {
                cache = HttpContext.Current.Cache;
            }
            else
            {
                cache = new Cache();
            }

            if (cache != null)
            {
                cache.Add("templates", templates, null, Cache.NoAbsoluteExpiration, new TimeSpan(1, 0, 0), CacheItemPriority.High, null);
            }
        }

        public string LoadTemplateFile(string templateName)
        {
            string template;

            bool loadedCache = populateTemplateCache();
            bool loadedFromCache = false;

            if (templateAssembly != null && (!templateName.EndsWith(".html")))
            {
                lock (templatesLock)
                {
                    if (!templates.TryGetValue(templateAssembly + "." + templateName + "." + Medium.ToString(), out template))
                    {
                        try
                        {
                            ResourceManager rm;

                            switch (templateAssembly)
                            {
                                case "Groups":
                                case "Networks":
                                case "Musician":
                                    rm = new ResourceManager("BoxSocial." + templateAssembly + ".Templates", pageAssembly[templateAssembly]);
                                    break;
                                default:
                                    rm = new ResourceManager("BoxSocial.Applications." + templateAssembly + ".Templates", pageAssembly[templateAssembly]);
                                    break;
                            }

                            object templateObject = null;

                            switch (Medium)
                            {
                                case DisplayMedium.Mobile:
                                    templateObject = rm.GetObject(templateName + "_mobile");
                                    break;
                                case DisplayMedium.Tablet:
                                    templateObject = rm.GetObject(templateName + "_tablet");
                                    break;
                                default:
                                    templateObject = rm.GetObject(templateName);
                                    break;
                            }

                            if (!(templateObject is string || templateObject is byte[]))
                            {
                                templateObject = rm.GetObject(templateName);
                            }

                            if (templateObject is string)
                            {
                                template = (string)templateObject;
                            }
                            else if (templateObject is byte[])
                            {
                                template = System.Text.UTF8Encoding.UTF8.GetString((byte[])templateObject);
                                if (template.StartsWith("\xEF\xBB\xBF"))
                                {
                                    template = template.Remove(0, 3);
                                }
                                if (template.StartsWith("\xBF"))
                                {
                                    template = template.Remove(0, 1);
                                }

                                templates.Add(templateAssembly + "." + templateName + "." + Medium.ToString(), template);
                            }
                            else
                            {
                                template = string.Format("Unknown template type {1} in assembly {0}, medium = {2}",
                                templateAssembly, templateName, Medium.ToString());
                            }
                        }
                        catch (Exception ex)
                        {
                            template = string.Format("Could not load template {1} from assembly {0}.<hr />Additionally the following exception was thrown.<br />{2}",
                                templateAssembly, templateName, ex.ToString().Replace("\n", "\n<br />"));
                        }
                    }
                    else
                    {
                        loadedFromCache = true;
                    }
                }
            }
            else
            {
                lock (templatesLock)
                {
                    if (!templates.TryGetValue(templateName + "." + Medium.ToString(), out template))
                    {
                        string templatePath = System.IO.Path.Combine(path, templateName);

                        switch (Medium)
                        {
                            case DisplayMedium.Mobile:
                                string mobileTemplatePath = System.IO.Path.Combine(System.IO.Path.Combine(path, "mobile"), templateName);
                                if (File.Exists(mobileTemplatePath))
                                {
                                    templatePath = mobileTemplatePath;
                                }
                                break;
                            case DisplayMedium.Tablet:
                                string tabletTemplatePath = System.IO.Path.Combine(System.IO.Path.Combine(path, "tablet"), templateName);
                                if (File.Exists(tabletTemplatePath))
                                {
                                    templatePath = tabletTemplatePath;
                                }
                                break;
                            case DisplayMedium.Desktop:
                            default:
                                // default is defined as fallback
                                break;
                        }

                        template = Template.OpenTextFile(templatePath);
                        if (template == null)
                        {
                            template = string.Format("Could not load template {0}",
                                templateName);
                        }
                        else
                        {
                            // COMMENT/UNCOMMENT TO CACHE THE TEMPLATE, better to uncomment when debugging
                            templates.Add(templateName + "." + Medium.ToString(), template);
                        }
                    }
                    else
                    {
                        loadedFromCache = true;
                    }
                }
            }

            // If we didn't load the template from some form of cache, save it to the cache
            if (!loadedFromCache)
            {
                saveTemplateCache(templates);
            }

            return template;
        }

        /// <summary>
        /// ONLY PUT A SINGLE IF STATEMENT ON A SINGLE LINE OF xHTML
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            template = LoadTemplateFile(templateName);

            StringBuilder output = new StringBuilder();
            string[] lines = template.Replace("\r", String.Empty).Split('\n');
            int lineAdjust = 0;

            ProcessLines(lines, output, variables);

            return output.ToString();
        }

        private void ProcessLines(string[] lines, StringBuilder output, VariableCollection variables)
        {
            ProcessLines(lines, output, variables, -1);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="output"></param>
        /// <param name="parentPath">Path to the childs parent</param>
        /// <param name="childIndex">Index of the child when doing a loop</param>
        private void ProcessLines(string[] lines, StringBuilder output, VariableCollection variables, int childIndex)
        {
            int inIf = 0; /* depth we are into an If block */
            Stack<bool> inElse = new Stack<bool>(); /* depth within an else block */
            Stack<bool> conditionTrue = new Stack<bool>(); /* keep track of wether the condition for the current block is true or false */
            int rootFalse = 0;
            string line;
            bool inLoop = false;
            string loopName = String.Empty;
            List<string> loopCache = new List<string>();

            // Static Variables
            variables.Parse("$_INDEX", childIndex);
            if (childIndex % 2 == 0)
            {
                variables.Parse("$_INDEX_EVEN", "TRUE");
                variables.Parse("$_INDEX_ODD", "FALSE");
            }
            else
            {
                variables.Parse("$_INDEX_EVEN", "FALSE");
                variables.Parse("$_INDEX_ODD", "TRUE");
            }
            if (childIndex == 0)
            {
                variables.Parse("$_INDEX_FIRST", "TRUE");
            }
            else
            {
                variables.Parse("$_INDEX_FIRST", "FALSE");
            }

            if (Medium == DisplayMedium.Mobile)
            {
                //HttpContext.Current.Response.Write("mobile\n");
                variables.Parse("$_IS_MOBILE", "TRUE");
            }
            else
            {
                variables.Parse("$_IS_MOBILE", "FALSE");
            }

            if (Medium == DisplayMedium.Tablet)
            {
                //HttpContext.Current.Response.Write("tablet\n");
                variables.Parse("$_IS_TABLET", "TRUE");
            }
            else
            {
                variables.Parse("$_IS_TABLET", "FALSE");
            }

            if (Medium == DisplayMedium.Desktop)
            {
                //HttpContext.Current.Response.Write("desktop\n");
                variables.Parse("$_IS_DESKTOP", "TRUE");
            }
            else
            {
                variables.Parse("$_IS_DESKTOP", "FALSE");
            }

            for (int i = 0; i < lines.Length; i++)
            {
                line = lines[i];

                /* loops */
                if (!inLoop)
                {
                    int iob = line.IndexOf("<!-- BEGIN ");
                    if (iob >= 0)
                    {
                        int iobe = line.IndexOf(" -->", iob);
                        //Match lm = Regex.Match(line, @"\<\!\-\- BEGIN ([A-Za-z0-9\.\-_]+) \-\-\>", RegexOptions.Compiled);

                        //if (lm.Success)
                        if (iobe >= 0)
                        {
                            inLoop = true;
                            //loopName = lm.Groups[1].Value;
                            loopName = line.Substring(iob + 11, iobe - (iob + 11));
                            loopCache.Clear();

                            //line = line.Remove(lm.Index, lm.Length);
                            line = line.Remove(iob, (iobe + 4) - iob);
                            continue;
                        }
                    }
                }
                else
                {
                    int ioe = line.IndexOf("<!-- END " + loopName + " -->");
                    if (ioe >= 0)
                    {
                        //int ioee = line.IndexOf(loopName + " -->", ioe);
                        //Match lm = Regex.Match(line, @"\<\!\-\- END " + loopName + @" \-\-\>", RegexOptions.Compiled);

                        //if (lm.Success)
                        //if (ioee >= 0)
                        {
                            inLoop = false;
                            int l;
                            if (variables.ContainsLoop(loopName))
                            //if (loopVariables.ContainsKey(loopName))
                            {
                                // Fix a bug where loops are evaluated in false conditions
                                if (inIf == 0 || inIf > 0 && conditionTrue.Peek() || inElse.Peek() && (!conditionTrue.Peek()))
                                {
                                    for (l = 0; l < variables.GetChildCollection(loopName).Count; l++)
                                    {
                                        ProcessLines(loopCache.ToArray(), output, variables.GetChildCollection(loopName)[l], l);
                                    }
                                }
                            }
                            //line = line.Remove(lm.Index, lm.Length);
                            line = line.Remove(ioe, loopName.Length + 9 + 4);
                            continue;
                        }
                        /*else
                        {
                            loopCache.Add(line);
                            continue;
                        }*/
                    }
                    else
                    {
                        loopCache.Add(line);
                        continue;
                    }
                }

                /* Conditional statements */
                /*
                 * To ensure the proper start tag ends the proper end tag, all tag sets have to be matched
                 */
                //Match rm = null;
                int ioi = line.IndexOf("<!-- IF ");
                if (ioi >= 0)
                {
                    int ioie = line.IndexOf(" -->", ioi);
                    //rm = Regex.Match(line, @"\<\!\-\- IF ([A-Za-z0-9\.\-_]+) \-\-\>", RegexOptions.Compiled);

                    /* if we have found an if on the line */
                    //if (rm.Success)
                    if (ioie >= 0)
                    {
                        string condition = line.Substring(ioi + 8, ioie - (ioi + 8));
                        bool conditionEvaluated = false;

                        // we should understand if we are in an ELSE section then IF can appear in here as well
                        if (inIf == 0 || inIf > 0 && conditionTrue.Peek() || inElse.Peek() && (!conditionTrue.Peek()))
                        {
                            inIf++;

                            string[] conditionPhrases = condition.Split(new char[] { ' ', '\t' });

                           // condition = string.Empty;
                            bool lastOR = false;
                            bool lastAND = false;
                            bool lastNOT = false;
                            for (int j = 0; j < conditionPhrases.Length; j++)
                            {
                                conditionPhrases[j] = conditionPhrases[j].Trim();

                                if (conditionPhrases[j] == "OR")
                                {
                                    lastOR = true;
                                    continue;
                                }
                                else if (conditionPhrases[j] == "AND")
                                {
                                    lastAND = true;
                                    continue;
                                }
                                else if (conditionPhrases[j] == "NOT")
                                {
                                    //HttpContext.Current.Response.Write(condition + "\n");
                                    lastNOT = true;
                                    continue;
                                }
                                else
                                {
                                    if (lastOR)
                                    {
                                        conditionEvaluated = conditionEvaluated || ((!lastNOT) == EvaluateCondition(variables, conditionPhrases[j], childIndex));
                                    }
                                    else if (lastAND)
                                    {
                                        conditionEvaluated = conditionEvaluated && ((!lastNOT) == EvaluateCondition(variables, conditionPhrases[j], childIndex));
                                    }
                                    else
                                    {
                                        conditionEvaluated = (!lastNOT) == EvaluateCondition(variables, conditionPhrases[j], childIndex);
                                    }
                                    //HttpContext.Current.Response.Write((lastNOT ? "TRUE " : "FALSE ") + conditionPhrases[j] + (EvaluateCondition(variables, conditionPhrases[j], childIndex) ? " TRUE " : " FALSE ") + "\n");
                                    lastOR = false;
                                    lastAND = false;
                                    lastNOT = false;
                                }
                            }

                            if (conditionEvaluated)
                            {
                                conditionTrue.Push(true);
                            }
                            else
                            {
                                conditionTrue.Push(false);
                                rootFalse = inIf;
                            }
                        }
                        else
                        {
                            inIf++;
                            conditionTrue.Push(false);
                        }

                        inElse.Push(false);

                        if (conditionTrue.Peek())
                        {
                            //line = line.Remove(rm.Index, rm.Length);
                            line = line.Remove(ioi, (ioie + 4) - ioi);
                        }
                        else
                        {
                            //line = line.Remove(rm.Index);
                            line = line.Remove(ioi);
                        }
                    }
                }

                // END IF

                /*Match rmei = Regex.Match(line, @"\<\!\-\- ELSEIF ([A-Za-z0-9\.\-_]+) \-\-\>", RegexOptions.Compiled);

                if (rmei.Success && inIf > 0)
                {
                    if (conditionTrue.Count == inIf)
                    {
                        if (!conditionTrue.Peek())
                        {
                            if (rm.Success)
                            {
                                line = line.Remove(rm.Index, (rmei.Index - rm.Index + rmei.Length));
                            }
                            else
                            {
                                line = line.Remove(0, rmei.Index + rmei.Length);
                            }
                        }
                        / * change the top most from false to true * /
                        inElse.Pop();
                        inElse.Push(true);
                        if (rootFalse == 0)
                        {
                            rootFalse = inIf;
                        }
                        else if (rootFalse == inIf)
                        {
                            rootFalse = 0;
                        }
                    }
                }*/

                int iol = line.IndexOf("<!-- ELSE -->");
                if (iol >= 0 && inIf > 0)
                {
                    //Match rme = Regex.Match(line, @"\<\!\-\- ELSE \-\-\>", RegexOptions.Compiled);

                    //if (rme.Success && inIf > 0)
                    {
                        if (conditionTrue.Count == inIf)
                        {
                            if (!conditionTrue.Peek())
                            {
                                //if (rm != null && rm.Success)
                                if (ioi >= 0)
                                {
                                    //line = line.Remove(rm.Index, (rme.Index - rm.Index + rme.Length));
                                    line = line.Remove(ioi, (iol - ioi + 13));
                                }
                                else
                                {
                                    line = line.Remove(0, iol + 13);
                                }
                            }
                            /* change the top most from false to true */
                            inElse.Pop();
                            inElse.Push(true);
                            if (rootFalse == 0)
                            {
                                rootFalse = inIf;
                            }
                            else if (rootFalse == inIf)
                            {
                                rootFalse = 0;
                            }
                        }
                    }
                }

                int ion = line.IndexOf("<!-- ENDIF -->");
                if (ion >= 0 && inIf > 0)
                {
                    //Match rmd = Regex.Match(line, @"\<\!\-\- ENDIF \-\-\>", RegexOptions.Compiled);
                    //if (rmd.Success && inIf > 0)
                    {
                        if (conditionTrue.Peek() & !inElse.Peek() || !conditionTrue.Peek() && inElse.Peek())
                        {
                            //line = line.Remove(rmd.Index, rmd.Length);
                            line = line.Remove(ion, 14);
                        }
                        else if (conditionTrue.Peek() & inElse.Peek() || !conditionTrue.Peek() && !inElse.Peek())
                        {
                            //line = line.Remove(0, rmd.Index + rmd.Length);
                            line = line.Remove(0, ion + 14);
                        }

                        if (rootFalse == inIf)
                        {
                            rootFalse = 0;
                        }

                        /* we have finished this level of 'if', we decrement our counter */
                        inIf--;
                        inElse.Pop();
                        conditionTrue.Pop();
                    }
                }

                if (conditionTrue.Count == inIf && inIf > 0)
                {
                    if (!conditionTrue.Peek() && !inElse.Peek())
                    {
                        // don't append the line, stop processing the line
                        continue;
                    }
                    if (conditionTrue.Peek() && inElse.Peek())
                    {
                        // don't append the line, stop processing the line
                        continue;
                    }

                    if (inIf > rootFalse && rootFalse != 0)
                    {
                        continue;
                    }
                }

                /* Includes */
                int iou;
                //MatchCollection mc = Regex.Matches(line, @"\<\!\-\- INCLUDE ([A-Za-z0-9\.\-_]+) \-\-\>", RegexOptions.Compiled);
                //foreach (Match ma in mc)
                //for (int j = 0; j < mc.Count; j++)
                while ((iou = line.IndexOf("<!-- INCLUDE ")) >= 0)
                {
                    int ioue = line.IndexOf(" -->", iou);
                    if (ioue >= 0)
                    {
                        string file = line.Substring(iou + 13, ioue - (iou + 13));

                        if (instances.ContainsKey(file))
                        {
                            instances[file]++;
                        }
                        else
                        {
                            instances.Add(file, 1);
                        }

                        StringBuilder includeOutput = new StringBuilder();
                        //string[] includeLines = Template.OpenTextFile(Path.Combine(path, ma.Groups[1].Value)).Replace("\r", "").Split('\n');
                        //string[] includeLines = LoadTemplateFile(mc[j].Groups[1].Value).Replace("\r", String.Empty).Split('\n');
                        string[] includeLines = LoadTemplateFile(file).Replace("\r", String.Empty).Split('\n');
                        ProcessLines(includeLines, includeOutput, variables, instances[file] - 1);
                        //line = line.Replace(string.Format("<!-- INCLUDE {0} -->", mc[j].Groups[1].Value), includeOutput.ToString());
                        line = line.Replace(string.Format("<!-- INCLUDE {0} -->", file), includeOutput.ToString());
                    }
                }

                /* experimental */
                int offset = 0;
                //MatchCollection varMatches = Regex.Matches(line, @"{([A-Z\-_]+)}", RegexOptions.Compiled);
                //foreach (Match varMatch in varMatches)
                List<TemplateVariable> varMatches = GetVariablesFromLine(line);
                foreach (TemplateVariable tv in varMatches)
                {
                    //int nextOffset = -varMatch.Length;
                    //string key = varMatch.Groups[1].Value;
                    //line = line.Remove(varMatch.Index + offset, varMatch.Length);
                    string key = tv.Name;
                    int nextOffset = -(key.Length + 2);
                    line = line.Remove(tv.Index + offset, key.Length + 2);
                    string value4 = null;
                    //if (variables.ContainsKey(key))
                    if (variables.TryGetValue(key, out value4))
                    {
                        //if (variables[key] != null)
                        if (value4 != null)
                        {
                            //nextOffset += variables[key].Length;
                            //line = line.Insert(varMatch.Index + offset, variables[key]);
                            nextOffset += value4.Length;
                            line = line.Insert(tv.Index + offset, value4);
                        }
                    }
                        // These are static keys
                    /*else if (key.StartsWith("$_"))
                    {
                        if (childIndex % 2 == 0)
                        {

                        }
                    }*/
                    else if (prose != null && key.StartsWith("L_"))
                    {
                        string proseKey = key.Substring(2).ToUpper();
                        string fragment = null;

                        if (prose.ContainsKey(templateAssembly, proseKey))
                        {
                            fragment = prose.GetString(templateAssembly, proseKey);
                        }
                        else if (prose.ContainsKey(proseKey))
                        {
                            fragment = prose.GetString(proseKey);
                        }
                        else if ((!string.IsNullOrEmpty(templateAssembly)) && prose.ContainsKey(templateAssembly, proseKey))
                        {
                            fragment = prose.GetString(templateAssembly, proseKey);
                        }

                        if (fragment != null)
                        {
                            nextOffset += fragment.Length;
                            //line = line.Insert(varMatch.Index + offset, fragment);
                            line = line.Insert(tv.Index + offset, fragment);
                        }
                    }
                    offset += nextOffset;
                }

                /* loops/repetition blocks */
                if (childIndex >= 0)
                {
                    offset = 0;
                    //MatchCollection varMatches2 = Regex.Matches(line, string.Format(@"{{{0}\.([A-Z\-_]+)}}", Regex.Escape(variables.Path)), RegexOptions.Compiled);
                    List<TemplateVariable> varMatches2 = GetLoopVariablesFromLine(variables.Path, line);
                    int varPathLength = variables.Path.Length;
                    //foreach (Match varMatch in varMatches2)
                    foreach (TemplateVariable tv in varMatches2)
                    {
                        /*int nextOffset = -varMatch.Length;
                        string key = varMatch.Groups[1].Value;
                        line = line.Remove(varMatch.Index + offset, varMatch.Length);*/
                        string key = tv.Name;
                        int nextOffset = -(key.Length + 3 + varPathLength);
                        line = line.Remove(tv.Index + offset, key.Length + 3 + varPathLength);
                        string value3 = null;
                        if (variables.TryGetValue(key, out value3))
                        //if (variables.ContainsKey(key))
                        {
                            if (value3 != null)
                            {
                                nextOffset += value3.Length;
                                line = line.Insert(tv.Index + offset, value3);
                            }
                        }
                        offset += nextOffset;
                    }
                }
                output.AppendLine(line);
            }
        }

        public bool EvaluateCondition(VariableCollection variables, string condition, int childIndex)
        {
            string value1 = null;
            bool returnFlag = false;

            if (variables.TryGetValue(condition, out value1))
            {
                //if (variables[rm.Groups[1].Value] != null)
                if (value1 != null)
                {
                    //if (variables[rm.Groups[1].Value].ToLower() != "false" && variables[rm.Groups[1].Value] != "0" && variables[rm.Groups[1].Value] != String.Empty)
                    if (value1.ToLower() != "false" && value1 != "0" && value1 != String.Empty)
                    {
                        returnFlag = true;
                    }
                    else
                    {
                        returnFlag = false;
                        //rootFalse = inIf;
                    }
                }
                else
                {
                    returnFlag = false;
                    //rootFalse = inIf;
                }
            }
            else
            {
                if (childIndex >= 0)
                {
                    //string loopConditionVar = rm.Groups[1].Value;
                    string loopConditionVar = condition;
                    if (loopConditionVar.StartsWith(variables.Path + "."))
                    {
                        loopConditionVar = loopConditionVar.Substring(variables.Path.Length + 1);
                    }
                    string value2 = null;
                    if (variables.TryGetValue(loopConditionVar, out value2))
                    //if (variables.ContainsKey(loopConditionVar))
                    {
                        //if (variables[loopConditionVar] == null)
                        if (value2 == null)
                        {
                            returnFlag = false;
                            //rootFalse = inIf;
                        }
                        else
                        {
                            //if (variables[loopConditionVar].ToLower() != "false" && variables[loopConditionVar] != "0" && variables[loopConditionVar] != String.Empty)
                            if (value2.ToLower() != "false" && value2 != "0" && value2 != String.Empty)
                            {
                                returnFlag = true;
                            }
                            else
                            {
                                returnFlag = false;
                                //rootFalse = inIf;
                            }
                        }
                    }
                    else
                    {
                        returnFlag = false;
                        //rootFalse = inIf;
                    }
                }
                else
                {
                    returnFlag = false;
                    //rootFalse = inIf;
                }
            }

            return returnFlag;
        }

        public static TemplateVariable GetConstructFromLine(string constructType, string line)
        {

            return new TemplateVariable("HI", 0);
        }

        private static List<TemplateVariable> GetVariablesFromLine(string line)
        {
            List<TemplateVariable> mc = new List<TemplateVariable>();
            string varName = String.Empty;
            bool inVar = false;
            int varStart = 0;

            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] == '{')
                {
                    inVar = true;
                    varName = String.Empty;
                    varStart = i;
                    continue;
                }

                if (inVar)
                {
                    if (line[i] == '}')
                    {
                        mc.Add(new TemplateVariable(varName, varStart));
                        continue;
                    }

                    if ((line[i] >= 'A' && line[i] <= 'Z') || line[i] == '_' || line[i] == '-' || line[i] == '$' || (line[i] >= '0' && line[i] <= '9' && varName != string.Empty))
                    {
                        varName += line[i];
                    }
                    else
                    {
                        inVar = false;
                        continue;
                    }
                }
            }

            return mc;
        }

        private static List<TemplateVariable> GetLoopVariablesFromLine(string parent, string line)
        {
            List<TemplateVariable> mc = new List<TemplateVariable>();
            string varName = String.Empty;
            bool inVar = false;
            int varStart = 0;
            int parentIndex = 0;
            int parentMaxIndex = parent.Length - 1;

            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] == '{')
                {
                    inVar = true;
                    varName = String.Empty;
                    varStart = i;
                    parentIndex = 0;
                    continue;
                }

                if (inVar)
                {
                    if (line[i] == '}')
                    {
                        mc.Add(new TemplateVariable(varName, varStart));
                        continue;
                    }

                    if (parentIndex < parent.Length)
                    {
                        if (line[i] == parent[parentIndex])
                        {
                            parentIndex++;
                        }
                        else
                        {
                            inVar = false;
                            continue;
                        }
                    }
                    else if (parentIndex == parent.Length)
                    {
                        if (line[i] == '.')
                        {
                            parentIndex++;
                        }
                        else
                        {
                            inVar = false;
                            continue;
                        }
                    }
                    else if ((line[i] >= 'A' && line[i] <= 'Z') || line[i] == '_' || line[i] == '-' || line[i] == '$' || (line[i] >= '0' && line[i] <= '9' && varName != string.Empty))
                    {
                        varName += line[i];
                    }
                    else
                    {
                        inVar = false;
                        continue;
                    }
                }
            }

            return mc;
        }

        #region file handling

        /// <summary>
        /// Open a text file
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        protected static string OpenTextFile(string fileName)
        {
            StreamReader myStreamReader;
            string temp;
            try
            {
                myStreamReader = File.OpenText(fileName);
                temp = myStreamReader.ReadToEnd();
                myStreamReader.Close();
            }
            catch
            {
                temp = String.Empty;
            }
            return temp;
        }

        protected static string OpenTextStream(Stream file)
        {
            StreamReader myStreamReader;
            string temp;
            try
            {
                myStreamReader = new StreamReader(file);
                temp = myStreamReader.ReadToEnd();
                myStreamReader.Close();
            }
            catch (Exception ex)
            {
                temp = "Error reading stream.<br />" + ex.ToString();
            }
            return temp;
        }

        /// <summary>
        /// Save a text file
        /// </summary>
        /// <param name="fileToSave"></param>
        /// <param name="fileName"></param>
        protected static void SaveTextFile(string fileToSave, string fileName)
        {
            StreamWriter myStreamWriter = File.CreateText(fileName);
            myStreamWriter.Write(fileToSave);
            myStreamWriter.Close();
        }

        #endregion
    }
}
