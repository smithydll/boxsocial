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
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Web;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Applications.Profile
{
    [AccountSubModule("profile", "style")]
    public class AccountStyle : AccountSubModule
    {
        public override string Title
        {
            get
            {
                return "Profile Style";
            }
        }

        public override int Order
        {
            get
            {
                return 7;
            }
        }

        public AccountStyle()
        {
            this.Load += new EventHandler(AccountStyle_Load);
            this.Show += new EventHandler(AccountStyle_Show);
        }

        void AccountStyle_Load(object sender, EventArgs e)
        {
        }

        void AccountStyle_Show(object sender, EventArgs e)
        {
            SetTemplate("account_style");

            string mode = Request["mode"];

            CascadingStyleSheet css = new CascadingStyleSheet();
            css.Parse(loggedInMember.GetUserStyle());

            StyleGenerator editor = css.Generator;

            if (string.IsNullOrEmpty(loggedInMember.GetUserStyle()))
            {
                editor = StyleGenerator.Theme;
            }

            if (!string.IsNullOrEmpty(mode))
            {
                switch (mode.ToLower())
                {
                    case "theme":
                        editor = StyleGenerator.Theme;
                        break;
                    case "standard":
                        editor = StyleGenerator.Standard;
                        break;
                    case "advanced":
                        editor = StyleGenerator.Advanced;
                        break;
                }
            }

            string backgroundRepeat = "no-repeat";
            string backgroundPosition = "left top";
            bool backgroundAttachment = false; // False == scroll, true == fixed

            if (editor == StyleGenerator.Theme)
            {
                template.Parse("THEME_EDITOR", "TRUE");

                if (css.Hue == -1)
                {
                    template.Parse("DEFAULT_SELECTED", " checked=\"checked\"");
                }

                for (int i = 0; i < 8; i++)
                {
                    VariableCollection themeVariableCollection = template.CreateChild("theme");
                    double baseHue = (50.0 + i * (360 / 8.0)) % 360.0;

                    System.Drawing.Color one = Display.HlsToRgb(baseHue, 0.5F, 0.5F); // background colour
                    System.Drawing.Color two = Display.HlsToRgb(baseHue, 0.6F, 0.2F); // link colour
                    System.Drawing.Color three = Display.HlsToRgb(baseHue, 0.4F, 0.85F); // box title colour
                    System.Drawing.Color four = Display.HlsToRgb(baseHue - 11F - 180F, 0.7F, 0.2F); // box border
                    System.Drawing.Color five = Display.HlsToRgb(baseHue - 11F - 180F, 0.4F, 0.85F); // box background colour

                    themeVariableCollection.Parse("C_ONE", string.Format("{0:x2}{1:x2}{2:x2}", one.R, one.G, one.B));
                    themeVariableCollection.Parse("C_TWO", string.Format("{0:x2}{1:x2}{2:x2}", two.R, two.G, two.B));
                    themeVariableCollection.Parse("C_THREE", string.Format("{0:x2}{1:x2}{2:x2}", three.R, three.G, three.B));
                    themeVariableCollection.Parse("C_FOUR", string.Format("{0:x2}{1:x2}{2:x2}", four.R, four.G, four.B));
                    themeVariableCollection.Parse("C_FIVE", string.Format("{0:x2}{1:x2}{2:x2}", five.R, five.G, five.B));
                    themeVariableCollection.Parse("HUE", ((int)baseHue).ToString());

                    if ((int)baseHue == css.Hue)
                    {
                        themeVariableCollection.Parse("SELECTED", " checked=\"checked\"");
                    }
                }
            }

            if (editor == StyleGenerator.Standard)
            {
                template.Parse("STANDARD_EDITOR", "TRUE");

                if (css.HasKey("body"))
                {
                    if (css["body"].HasProperty("background-color"))
                    {
                        template.Parse("BACKGROUND_COLOUR", css["body"]["background-color"].Value);
                    }
                    if (css["body"].HasProperty("color"))
                    {
                        template.Parse("FORE_COLOUR", css["body"]["color"].Value);
                    }
                    if (css["body"].HasProperty("background-image"))
                    {
                        template.Parse("BACKGROUND_IMAGE", css["body"]["background-image"].Value);
                    }
                    if (css["body"].HasProperty("background-repeat"))
                    {
                        backgroundRepeat = css["body"]["background-repeat"].Value;
                    }
                    if (css["body"].HasProperty("background-position"))
                    {
                        backgroundPosition = css["body"]["background-position"].Value;
                    }
                    if (css["body"].HasProperty("background-attachment"))
                    {
                        if (css["body"]["background-attachment"].Value == "fixed")
                        {
                            backgroundAttachment = true;
                        }
                        else
                        {
                            backgroundAttachment = false;
                        }
                    }
                }
                else if (css.HasKey("html"))
                {
                    if (css["html"].HasProperty("background-color"))
                    {
                        template.Parse("BACKGROUND_COLOUR", css["html"]["background-color"].Value);
                    }
                    if (css["html"].HasProperty("color"))
                    {
                        template.Parse("FORE_COLOUR", css["html"]["color"].Value);
                    }
                    if (css["html"].HasProperty("background-image"))
                    {
                        template.Parse("BACKGROUND_IMAGE", css["html"]["background-image"].Value);
                    }
                    if (css["html"].HasProperty("background-repeat"))
                    {
                        backgroundRepeat = css["html"]["background-repeat"].Value;
                    }
                    if (css["html"].HasProperty("background-position"))
                    {
                        backgroundPosition = css["html"]["background-position"].Value;
                    }
                    if (css["html"].HasProperty("background-attachment"))
                    {
                        if (css["html"]["background-attachment"].Value == "fixed")
                        {
                            backgroundAttachment = true;
                        }
                        else
                        {
                            backgroundAttachment = false;
                        }
                    }
                }

                if (css.HasKey("a"))
                {
                    if (css["a"].HasProperty("color"))
                    {
                        template.Parse("LINK_COLOUR", css["a"]["color"].Value);
                    }
                }

                if (css.HasKey("#pane-profile div.pane"))
                {
                    if (css["#pane-profile div.pane"].HasProperty("border-color"))
                    {
                        template.Parse("BOX_BORDER_COLOUR", css["#pane-profile div.pane"]["border-color"].Value);
                    }

                    if (css["#pane-profile div.pane"].HasProperty("background-color"))
                    {
                        template.Parse("BOX_BACKGROUND_COLOUR", css["#pane-profile div.pane"]["background-color"].Value);
                    }

                    if (css["#pane-profile div.pane"].HasProperty("color"))
                    {
                        template.Parse("BOX_FORE_COLOUR", css["#pane-profile div.pane"]["color"].Value);
                    }
                }

                if (css.HasKey("#pane-profile div.pane h3"))
                {
                    if (css["#pane-profile div.pane h3"].HasProperty("background-color"))
                    {
                        template.Parse("BOX_H_BACKGROUND_COLOUR", css["#pane-profile div.pane h3"]["background-color"].Value);
                    }

                    if (css["#pane-profile div.pane h3"].HasProperty("color"))
                    {
                        template.Parse("BOX_H_FORE_COLOUR", css["#pane-profile div.pane h3"]["color"].Value);
                    }
                }

                List<SelectBoxItem> repeatBoxItems = new List<SelectBoxItem>();
                repeatBoxItems.Add(new SelectBoxItem("no-repeat", "None", "/images/bg-no-rpt.png"));
                repeatBoxItems.Add(new SelectBoxItem("repeat-x", "Horizontal", "/images/bg-rpt-x.png"));
                repeatBoxItems.Add(new SelectBoxItem("repeat-y", "Vertical", "/images/bg-rpt-y.png"));
                repeatBoxItems.Add(new SelectBoxItem("repeat", "Tile", "/images/bg-rpt.png"));

                Display.ParseRadioArray(template, "S_BACKGROUND_REPEAT", "background-repeat", 2, repeatBoxItems, backgroundRepeat);

                List<SelectBoxItem> positionBoxItems = new List<SelectBoxItem>();
                positionBoxItems.Add(new SelectBoxItem("left top", "Top Left", "/images/bg-t-l.png"));
                positionBoxItems.Add(new SelectBoxItem("center top", "Top Centre", "/images/bg-t-c.png"));
                positionBoxItems.Add(new SelectBoxItem("right top", "Top Right", "/images/bg-t-r.png"));
                positionBoxItems.Add(new SelectBoxItem("left center", "Middle Left", "/images/bg-m-l.png"));
                positionBoxItems.Add(new SelectBoxItem("center center", "Middle Centre", "/images/bg-m-c.png"));
                positionBoxItems.Add(new SelectBoxItem("right center", "Middle Right", "/images/bg-m-r.png"));
                positionBoxItems.Add(new SelectBoxItem("left bottom", "Bottom Left", "/images/bg-b-l.png"));
                positionBoxItems.Add(new SelectBoxItem("center bottom", "Bottom Centre", "/images/bg-b-c.png"));
                positionBoxItems.Add(new SelectBoxItem("right bottom", "Bottom Right", "/images/bg-b-r.png"));

                Display.ParseRadioArray(template, "S_BACKGROUND_POSITION", "background-position", 3, positionBoxItems, backgroundPosition);

                if (backgroundAttachment)
                {
                    template.Parse("BACKGROUND_IMAGE_FIXED", "checked=\"checked\"");
                }
            }

            if (editor == StyleGenerator.Advanced)
            {
                template.Parse("ADVANCED_EDITOR", "TRUE");

                css.Generator = StyleGenerator.Advanced;
                template.Parse("STYLE", css.ToString());
            }

            Save(new EventHandler(AccountStyle_Save));
        }

        void AccountStyle_Save(object sender, EventArgs e)
        {
            string mode = Request["mode"];

            CascadingStyleSheet css = new CascadingStyleSheet();

            switch (mode.ToLower())
            {
                case "theme":
                    css.Generator = StyleGenerator.Theme;

                    int baseHue = Functions.FormInt("theme", -1);

                    if (baseHue == -1)
                    {
                        css.Generator = StyleGenerator.Theme;
                        css.Hue = -1;
                    }
                    else
                    {
                        css.Generator = StyleGenerator.Theme;
                        css.Hue = baseHue;

                        System.Drawing.Color one = Display.HlsToRgb(baseHue, 0.5F, 0.5F); // background colour
                        System.Drawing.Color two = Display.HlsToRgb(baseHue, 0.6F, 0.2F); // link colour
                        System.Drawing.Color three = Display.HlsToRgb(baseHue, 0.4F, 0.85F); // box title colour
                        System.Drawing.Color four = Display.HlsToRgb(baseHue - 11F - 180F, 0.7F, 0.2F); // box border
                        System.Drawing.Color five = Display.HlsToRgb(baseHue - 11F - 180F, 0.4F, 0.85F); // box background colour

                        string backgroundColour = string.Format("#{0:x2}{1:x2}{2:x2}", one.R, one.G, one.B);
                        string linkColour = string.Format("#{0:x2}{1:x2}{2:x2}", two.R, two.G, two.B);
                        string boxTitleColour = string.Format("#{0:x2}{1:x2}{2:x2}", three.R, three.G, three.B);
                        string boxBorderColour = string.Format("#{0:x2}{1:x2}{2:x2}", four.R, four.G, four.B);
                        string boxBackgroundColour = string.Format("#{0:x2}{1:x2}{2:x2}", five.R, five.G, five.B);

                        css.AddStyle("body");
                        css["body"].SetProperty("background-color", backgroundColour);
                        css["body"].SetProperty("color", "#000000");

                        css.AddStyle("a");
                        css["a"].SetProperty("color", linkColour);

                        css.AddStyle("#pane-profile div.pane");
                        css["#pane-profile div.pane"].SetProperty("background-color", boxBackgroundColour);
                        css["#pane-profile div.pane"].SetProperty("border-color", boxBorderColour);
                        css["#pane-profile div.pane"].SetProperty("color", "#000000");

                        css.AddStyle("#profile div.pane");
                        css["#profile div.pane"].SetProperty("background-color", boxBackgroundColour);
                        css["#profile div.pane"].SetProperty("border-color", boxBorderColour);
                        css["#profile div.pane"].SetProperty("color", "#000000");

                        css.AddStyle("#overview-profile");
                        css["#overview-profile"].SetProperty("background-color", boxBackgroundColour);
                        css["#overview-profile"].SetProperty("border-color", boxBorderColour);
                        css["#overview-profile"].SetProperty("color", "#000000");

                        css.AddStyle("#pane-profile div.pane h3");
                        css["#pane-profile div.pane h3"].SetProperty("background-color", boxBorderColour);
                        css["#pane-profile div.pane h3"].SetProperty("border-color", boxBorderColour);
                        css["#pane-profile div.pane h3"].SetProperty("color", boxTitleColour);

                        css.AddStyle("#pane-profile div.pane h3 a");
                        css["#pane-profile div.pane h3 a"].SetProperty("color", boxTitleColour);

                        css.AddStyle("#profile div.pane h3");
                        css["#profile div.pane h3"].SetProperty("background-color", boxBorderColour);
                        css["#profile div.pane h3"].SetProperty("border-color", boxBorderColour);
                        css["#profile div.pane h3"].SetProperty("color", boxTitleColour);

                        css.AddStyle("#profile div.pane h3 a");
                        css["#profile div.pane h3 a"].SetProperty("color", boxTitleColour);

                        css.AddStyle("#overview-profile div.info");
                        css["#overview-profile div.info"].SetProperty("background-color", boxBorderColour);
                        css["#overview-profile div.info"].SetProperty("border-color", boxBorderColour);
                        css["#overview-profile div.info"].SetProperty("color", boxTitleColour);

                        css.AddStyle("#overview-profile div.info a");
                        css["#overview-profile div.info a"].SetProperty("color", boxTitleColour);
                    }

                    break;
                case "standard":
                    css.Generator = StyleGenerator.Standard;
                    css.AddStyle("body");
                    css["body"].SetProperty("background-color", Request.Form["background-colour"]);
                    css["body"].SetProperty("color", Request.Form["fore-colour"]);
                    if (!string.IsNullOrEmpty(Request.Form["background-image"]))
                    {
                        css["body"].SetProperty("background-image", "url('" + Request.Form["background-image"] + "')");
                        css["body"].SetProperty("background-repeat", Request.Form["background-repeat"]);
                        css["body"].SetProperty("background-position", Request.Form["background-position"]);
                        if (Request.Form["background-image-fixed"] == "true")
                        {
                            css["body"].SetProperty("background-attachment", "fixed");
                        }
                        else
                        {
                            css["body"].SetProperty("background-attachment", "scroll");
                        }
                    }

                    css.AddStyle("a");
                    css["a"].SetProperty("color", Request.Form["link-colour"]);

                    css.AddStyle("#pane-profile div.pane");
                    css["#pane-profile div.pane"].SetProperty("background-color", Request.Form["box-background-colour"]);
                    css["#pane-profile div.pane"].SetProperty("border-color", Request.Form["box-border-colour"]);
                    css["#pane-profile div.pane"].SetProperty("color", Request.Form["box-fore-colour"]);

                    css.AddStyle("#profile div.pane");
                    css["#profile div.pane"].SetProperty("background-color", Request.Form["box-background-colour"]);
                    css["#profile div.pane"].SetProperty("border-color", Request.Form["box-border-colour"]);
                    css["#profile div.pane"].SetProperty("color", Request.Form["box-fore-colour"]);

                    css.AddStyle("#overview-profile");
                    css["#overview-profile"].SetProperty("background-color", Request.Form["box-background-colour"]);
                    css["#overview-profile"].SetProperty("border-color", Request.Form["box-border-colour"]);
                    css["#overview-profile"].SetProperty("color", Request.Form["box-fore-colour"]);

                    css.AddStyle("#pane-profile div.pane h3");
                    css["#pane-profile div.pane h3"].SetProperty("background-color", Request.Form["box-h3-background-colour"]);
                    css["#pane-profile div.pane h3"].SetProperty("border-color", Request.Form["box-border-colour"]);
                    css["#pane-profile div.pane h3"].SetProperty("color", Request.Form["box-h3-fore-colour"]);

                    css.AddStyle("#pane-profile div.pane h3 a");
                    css["#pane-profile div.pane h3 a"].SetProperty("color", Request.Form["box-h3-fore-colour"]);

                    css.AddStyle("#profile div.pane h3");
                    css["#profile div.pane h3"].SetProperty("background-color", Request.Form["box-h3-background-colour"]);
                    css["#profile div.pane h3"].SetProperty("border-color", Request.Form["box-border-colour"]);
                    css["#profile div.pane h3"].SetProperty("color", Request.Form["box-h3-fore-colour"]);

                    css.AddStyle("#profile div.pane h3 a");
                    css["#profile div.pane h3 a"].SetProperty("color", Request.Form["box-h3-fore-colour"]);

                    css.AddStyle("#overview-profile div.info");
                    css["#overview-profile div.info"].SetProperty("background-color", Request.Form["box-h3-background-colour"]);
                    css["#overview-profile div.info"].SetProperty("border-color", Request.Form["box-border-colour"]);
                    css["#overview-profile div.info"].SetProperty("color", Request.Form["box-h3-fore-colour"]);

                    css.AddStyle("#overview-profile div.info a");
                    css["#overview-profile div.info a"].SetProperty("color", Request.Form["box-h3-fore-colour"]);

                    break;
                case "advanced":
                    css.Generator = StyleGenerator.Advanced;
                    css.Parse(Request.Form["css-style"]);
                    break;
            }

            if (db.Query(string.Format("SELECT user_id FROM user_style WHERE user_id = {0}",
                loggedInMember.UserId)).Rows.Count == 0)
            {
                db.UpdateQuery(string.Format("INSERT INTO user_style (user_id, style_css) VALUES ({0}, '{1}');",
                    loggedInMember.UserId, Mysql.Escape(css.ToString())));
            }
            else
            {
                db.UpdateQuery(string.Format("UPDATE user_style SET style_css = '{1}' WHERE user_id = {0};",
                    loggedInMember.UserId, Mysql.Escape(css.ToString())));
            }

            SetRedirectUri(BuildUri());
            Display.ShowMessage("Style Saved", "Your profile style has been saved in the database.");
        }
    }
}
