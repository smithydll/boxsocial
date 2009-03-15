using System;
using System.Data;
using System.Configuration;
using System.Collections;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.FrontEnd
{
    public partial class updater : TPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (core.session.LoggedInMember.UserName != "smithy_dll")
            {
                return;
            }

            SelectQuery query = new SelectQuery("user_keys uk");
            query.AddFields("user_id");
            query.AddCondition("user_name", ConditionEquality.NotEqual, "Anonymous");

            DataTable users = core.db.Query(query);

            foreach (DataRow kr in users.Rows)
            {
                long userId = (long)(int)kr["user_id"];
                core.LoadUserProfile(userId);

                Response.Write("User: " + userId + "<br />\n");
				
				ItemType itemType = new ItemType(core, typeof(BoxSocial.Internals.User).FullName);

                query = new SelectQuery("primitive_apps pa");
                query.AddFields(ApplicationEntry.APPLICATION_FIELDS, ApplicationEntry.USER_APPLICATION_FIELDS);
                query.AddJoin(JoinTypes.Inner, "applications ap", "pa.application_id", "ap.application_id");
                query.AddCondition("pa.item_type_id", itemType.TypeId.ToString());
                query.AddCondition("pa.item_id", userId);

                DataTable applications = core.db.Query(query);

                foreach (DataRow dr in applications.Rows)
                {
                    ApplicationEntry ae = new ApplicationEntry(core, dr);

                    ae.UpdateInstall(core, core.UserProfiles[userId]);

                    Response.Write(ae.AssemblyName + "<br />\n");
                }

                // Install a couple of applications
                ApplicationEntry profileAe = new ApplicationEntry(core, null, "Profile");
                    profileAe.Install(core, core.UserProfiles[userId]);


                    ApplicationEntry galleryAe = new ApplicationEntry(core, null, "Gallery");
                    galleryAe.Install(core, core.UserProfiles[userId]);


                    ApplicationEntry guestbookAe = new ApplicationEntry(core, null, "GuestBook");
                    guestbookAe.Install(core, core.UserProfiles[userId]);

                    ApplicationEntry groupsAe = new ApplicationEntry(core, null, "Groups");
                    groupsAe.Install(core, core.UserProfiles[userId]);

                    ApplicationEntry networksAe = new ApplicationEntry(core, null, "Networks");
                    networksAe.Install(core, core.UserProfiles[userId]);

                    ApplicationEntry calendarAe = new ApplicationEntry(core, null, "Calendar");
                    calendarAe.Install(core, core.UserProfiles[userId]);
                
            }

            EndResponse();
        }
    }
}
