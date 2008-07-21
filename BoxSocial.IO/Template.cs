/*
 * Box Social™
 * http://boxsocial.net/
 * Copyright © 2007, David Lachlan Smith
 * 
 * $Id:$
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License version 3 as
 * published by the Free Software Foundation.
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
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Caching;

namespace BoxSocial.IO
{
    public class VariableCollection
    {
        private string loopName;
        private Dictionary<string, List<VariableCollection>> childLoops = new Dictionary<string, List<VariableCollection>>();
        private Dictionary<string, string> variables = new Dictionary<string, string>();
        private VariableCollection parentCollection = null;

        internal VariableCollection()
        {
            loopName = "";
        }

        public VariableCollection(string name)
        {
            loopName = name;
        }

        public VariableCollection CreateChild(string name)
        {
            string fullName = (string.IsNullOrEmpty(loopName)) ? name : loopName + "." + name;

            VariableCollection vc = new VariableCollection(fullName);
            vc.parentCollection = this;

            if (!ContainsLoop(fullName))
            {
                childLoops.Add(fullName, new List<VariableCollection>());
            }

            childLoops[fullName].Add(vc);

            return vc;
        }

        public void Parse(string key, string value)
        {
            if (!variables.ContainsKey(key))
            {
                variables.Add(key, HttpUtility.HtmlEncode(value));
            }
        }

        /// <summary>
        /// Parse raw data to a template, only valid for Box Social internals
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
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
                /*HttpContext.Current.Response.Write("<hr /><dl><dd>");
                HttpContext.Current.Response.Write(key);
                HttpContext.Current.Response.Write("</dd><dt>");
                HttpContext.Current.Response.Write(value);
                HttpContext.Current.Response.Write("<hr />");*/
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
                if (variables.ContainsKey(key))
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
                return "";
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
        protected VariableCollection variables = new VariableCollection();
        private Dictionary<string, Assembly> pageAssembly = new Dictionary<string, Assembly>();

        private string template;
        private Dictionary<string, string> loopTemplates;

        private string path;
        private string templateName;
        private string templateAssembly;

        public void AddPageAssembly(Assembly value)
        {
            if (!pageAssembly.ContainsKey(value.GetName().Name))
            {
                pageAssembly.Add(value.GetName().Name, value);
            }
        }

        public Template()
        {
            this.path = HttpContext.Current.Server.MapPath("./templates/");
        }

        public Template(Assembly assembly, string templateName)
        {
            this.path = HttpContext.Current.Server.MapPath("./templates/");

            AddPageAssembly(assembly);

            this.templateAssembly = assembly.GetName().Name;
            this.templateName = templateName;
        }

        public Template(string fileName)
        {
            this.path = HttpContext.Current.Server.MapPath("./templates/");
            templateName = fileName;
        }

        public Template(string path, string fileName)
        {
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

        // TODO: reconsider this into the constructor
        public void SetProse(IProse prose)
        {
            this.prose = prose;
        }

        public void Parse(string key, string value)
        {
            variables.Parse(key, value);
        }

        /// <summary>
        /// Parse raw data to a template, only valid for Box Social internals
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
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

        /// <summary>
        /// ONLY PUT A SINGLE IF STATEMENT ON A SINGLE LINE OF xHTML
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (templateAssembly != null)
            {
                try
                {
                    ResourceManager rm;

                    switch (templateAssembly)
                    {
                        case "Groups":
                        case "Networks":
                            rm = new ResourceManager("BoxSocial." + templateAssembly + ".Templates", pageAssembly[templateAssembly]);
                            break;
                        default:
                            rm = new ResourceManager("BoxSocial.Applications." + templateAssembly + ".Templates", pageAssembly[templateAssembly]);
                            break;
                    }

                    object templateObject = rm.GetObject(templateName);

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
                    }
                    else
                    {
                        template = string.Format("Unknown template type {1} in assembly {0}",
                        templateAssembly, templateName);
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
                template = Template.OpenTextFile(Path.Combine(path, templateName));
                if (template == null)
                {
                    template = string.Format("Could not load template {0}",
                        templateName);
                }
            }
            StringBuilder output = new StringBuilder();
            string[] lines = template.Replace("\r", "").Split('\n');
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
            string loopName = "";
            List<string> loopCache = new List<string>();

            for (int i = 0; i < lines.Length; i++)
            {
                line = lines[i];

                /* loops */
                if (!inLoop)
                {
                    Match lm = Regex.Match(line, @"\<\!\-\- BEGIN ([A-Za-z0-9\.\-_]+) \-\-\>", RegexOptions.Compiled);

                    if (lm.Success)
                    {
                        inLoop = true;
                        loopName = lm.Groups[1].Value;
                        loopCache.Clear();

                        line = line.Remove(lm.Index, lm.Length);
                        continue;
                    }
                }
                else
                {
                    Match lm = Regex.Match(line, @"\<\!\-\- END " + loopName + @" \-\-\>", RegexOptions.Compiled);

                    if (lm.Success)
                    {
                        inLoop = false;
                        int l;
                        if (variables.ContainsLoop(loopName))
                        //if (loopVariables.ContainsKey(loopName))
                        {
                            for (l = 0; l < variables.GetChildCollection(loopName).Count; l++)
                            {
                                ProcessLines(loopCache.ToArray(), output, variables.GetChildCollection(loopName)[l], l);
                            }
                        }
                        line = line.Remove(lm.Index, lm.Length);
                        continue;
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
                Match rm = Regex.Match(line, @"\<\!\-\- IF ([A-Za-z0-9\.\-_]+) \-\-\>", RegexOptions.Compiled);

                /* if we have found an if on the line */
                if (rm.Success)
                {
                    if (inIf == 0 || inIf > 0 && conditionTrue.Peek())
                    {
                        inIf++;
                        if (variables.ContainsKey(rm.Groups[1].Value))
                        {
                            if (variables[rm.Groups[1].Value] != null)
                            {
                                if (variables[rm.Groups[1].Value].ToLower() != "false" && variables[rm.Groups[1].Value] != "0" && variables[rm.Groups[1].Value] != "")
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
                                conditionTrue.Push(false);
                                rootFalse = inIf;
                            }
                        }
                        else
                        {
                            if (childIndex >= 0)
                            {
                                string loopConditionVar = rm.Groups[1].Value;
                                if (loopConditionVar.StartsWith(variables.Path + "."))
                                {
                                    loopConditionVar = loopConditionVar.Substring(variables.Path.Length + 1);
                                }
                                if (variables.ContainsKey(loopConditionVar))
                                {
                                    if (variables[loopConditionVar] == null)
                                    {
                                        conditionTrue.Push(false);
                                        rootFalse = inIf;
                                    }
                                    else
                                    {
                                        if (variables[loopConditionVar].ToLower() != "false" && variables[loopConditionVar] != "0" && variables[loopConditionVar] != "")
                                        {
                                            conditionTrue.Push(true);
                                        }
                                        else
                                        {
                                            conditionTrue.Push(false);
                                            rootFalse = inIf;
                                        }
                                    }
                                }
                                else
                                {
                                    conditionTrue.Push(false);
                                    rootFalse = inIf;
                                }
                            }
                            else
                            {
                                conditionTrue.Push(false);
                                rootFalse = inIf;
                            }
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
                        line = line.Remove(rm.Index, rm.Length);
                    }
                    else
                    {
                        line = line.Remove(rm.Index);
                    }
                }

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

                Match rme = Regex.Match(line, @"\<\!\-\- ELSE \-\-\>", RegexOptions.Compiled);

                if (rme.Success && inIf > 0)
                {
                    if (conditionTrue.Count == inIf)
                    {
                        if (!conditionTrue.Peek())
                        {
                            if (rm.Success)
                            {
                                line = line.Remove(rm.Index, (rme.Index - rm.Index + rme.Length));
                            }
                            else
                            {
                                line = line.Remove(0, rme.Index + rme.Length);
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

                Match rmd = Regex.Match(line, @"\<\!\-\- ENDIF \-\-\>", RegexOptions.Compiled);
                if (rmd.Success && inIf > 0)
                {
                    if (conditionTrue.Peek() & !inElse.Peek() || !conditionTrue.Peek() && inElse.Peek())
                    {
                        line = line.Remove(rmd.Index, rmd.Length);
                    }
                    else if (conditionTrue.Peek() & inElse.Peek() || !conditionTrue.Peek() && !inElse.Peek())
                    {
                        line = line.Remove(0, rmd.Index + rmd.Length);
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
                MatchCollection mc = Regex.Matches(line, @"\<\!\-\- INCLUDE ([A-Za-z0-9\.\-_]+) \-\-\>", RegexOptions.Compiled);
                foreach (Match ma in mc)
                {
                    StringBuilder includeOutput = new StringBuilder();
                    string[] includeLines = Template.OpenTextFile(Path.Combine(path, ma.Groups[1].Value)).Replace("\r", "").Split('\n');
                    ProcessLines(includeLines, includeOutput, variables);
                    line = line.Replace(string.Format("<!-- INCLUDE {0} -->", ma.Groups[1].Value), includeOutput.ToString());
                }

                /* experimental */
                int offset = 0;
                MatchCollection varMatches = Regex.Matches(line, @"{([A-Z\-_]+)}", RegexOptions.Compiled);
                foreach (Match varMatch in varMatches)
                {
                    int nextOffset = -varMatch.Length;
                    string key = varMatch.Groups[1].Value;
                    line = line.Remove(varMatch.Index + offset, varMatch.Length);
                    if (variables.ContainsKey(key))
                    {
                        if (variables[key] != null)
                        {
                            nextOffset += variables[key].Length;
                            line = line.Insert(varMatch.Index + offset, variables[key]);
                        }
                    }
                    else if (prose != null)
                    {
                        string fragment = null;

                        if (prose.ContainsKey(key))
                        {
                            fragment = prose.GetString(key);
                        }
                        else if ((!string.IsNullOrEmpty(templateAssembly)) && prose.ContainsKey(templateAssembly, key))
                        {
                            fragment = prose.GetString(templateAssembly, key);
                        }

                        if (fragment != null)
                        {
                            nextOffset += fragment.Length;
                            line = line.Insert(varMatch.Index + offset, fragment);
                        }
                    }
                    offset += nextOffset;
                }

                /* loops/repetition blocks */
                if (childIndex >= 0)
                {
                    offset = 0;
                    varMatches = Regex.Matches(line, string.Format(@"{{{0}\.([A-Z\-_]+)}}", Regex.Escape(variables.Path)), RegexOptions.Compiled);
                    foreach (Match varMatch in varMatches)
                    {
                        int nextOffset = -varMatch.Length;
                        string key = varMatch.Groups[1].Value;
                        line = line.Remove(varMatch.Index + offset, varMatch.Length);
                        if (variables.ContainsKey(key))
                        {
                            if (variables[key] != null)
                            {
                                nextOffset += variables[key].Length;
                                line = line.Insert(varMatch.Index + offset, variables[key]);
                            }
                        }
                        offset += nextOffset;
                    }
                }
                output.AppendLine(line);
            }
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
                temp = "";
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
