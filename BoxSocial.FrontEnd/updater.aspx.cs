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
            if (core.Session.LoggedInMember.UserName != "smithy_dll")
            {
                return;
            }

            SelectQuery query = new SelectQuery("user_keys uk");
            query.AddFields("user_id");
            query.AddCondition("user_name", ConditionEquality.NotEqual, "Anonymous");

            DataTable users = core.Db.Query(query);

            foreach (DataRow kr in users.Rows)
            {
                long userId = (long)(int)kr["user_id"];
                core.LoadUserProfile(userId);

                Response.Write("User: " + userId.ToString() + "<br />\n");
				
                query = new SelectQuery("primitive_apps pa");
                //query.AddFields(ApplicationEntry.APPLICATION_FIELDS, ApplicationEntry.USER_APPLICATION_FIELDS);
                // TODO
                query.AddJoin(JoinTypes.Inner, "applications ap", "pa.application_id", "ap.application_id");
                query.AddCondition("pa.item_type_id", ItemKey.GetTypeId(typeof(BoxSocial.Internals.User)).ToString());
                query.AddCondition("pa.item_id", userId);

                DataTable applications = core.Db.Query(query);

                foreach (DataRow dr in applications.Rows)
                {
                    ApplicationEntry ae = new ApplicationEntry(core, dr);

                    ae.UpdateInstall(core, core.PrimitiveCache[userId]);

                    Response.Write(ae.AssemblyName + "<br />\n");
                }

                // Install a couple of applications
                ApplicationEntry profileAe = new ApplicationEntry(core, null, "Profile");
                    profileAe.Install(core, core.PrimitiveCache[userId]);


                    ApplicationEntry galleryAe = new ApplicationEntry(core, null, "Gallery");
                    galleryAe.Install(core, core.PrimitiveCache[userId]);


                    ApplicationEntry guestbookAe = new ApplicationEntry(core, null, "GuestBook");
                    guestbookAe.Install(core, core.PrimitiveCache[userId]);

                    ApplicationEntry groupsAe = new ApplicationEntry(core, null, "Groups");
                    groupsAe.Install(core, core.PrimitiveCache[userId]);

                    ApplicationEntry networksAe = new ApplicationEntry(core, null, "Networks");
                    networksAe.Install(core, core.PrimitiveCache[userId]);

                    ApplicationEntry calendarAe = new ApplicationEntry(core, null, "Calendar");
                    calendarAe.Install(core, core.PrimitiveCache[userId]);
                
            }

            EndResponse();
        }
    }
}
