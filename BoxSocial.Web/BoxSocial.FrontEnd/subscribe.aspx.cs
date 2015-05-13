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
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Web;
using BoxSocial;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.FrontEnd
{
    public partial class subscribe : TPage
    {
        public subscribe()
            : base("")
        {
            this.Load += new EventHandler(Page_Load);
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            bool isAjax = false;

            if (Request["ajax"] == "true")
            {
                isAjax = true;
            }

            if (!core.Session.SignedIn)
            {
                core.Response.ShowMessage("notSignedIn", "Subscription Error", "You must be logged in to subscribe.");
            }

            string mode = core.Http["mode"];
            long itemId = core.Functions.RequestLong("item", 0);
            long itemTypeId = core.Functions.RequestLong("type", 0);
			ItemKey itemKey = null;

			try
			{
				itemKey = new ItemKey(itemId, itemTypeId);
			}
			catch
			{
                core.Response.ShowMessage("invalidItem", "Invalid Item", "The item you have attempted to subscribe to is invalid.");
                return;
			}

            try
            {
                // This isn't the most elegant fix, but it works
                ApplicationEntry ae = null;
                if (core.IsPrimitiveType(itemTypeId))
                {
                    ae = core.GetApplication("GuestBook");
                }
                else
                {
                    ItemType itemType = new ItemType(core, itemTypeId);
                    if (itemType.ApplicationId == 0)
                    {
                        ae = core.GetApplication("GuestBook");
                    }
                    else
                    {
                        ae = new ApplicationEntry(core, itemType.ApplicationId);
                    }
                }

                BoxSocial.Internals.Application.LoadApplication(core, AppPrimitives.Any, ae);
            }
			catch (InvalidItemTypeException)
			{
                core.Response.ShowMessage("invalidItem", "Invalid Item", "The item you have attempted to subscribe to is invalid.");
                return;
			}
            catch (InvalidApplicationException)
            {
                core.Response.ShowMessage("invalidItem", "Invalid Item", "The item you have attempted to rate is invalid.");
                return;
            }

            bool success = false;
            try
            {
                switch (mode)
                {
                    case "subscribe":
                        success = Subscription.SubscribeToItem(core, itemKey);
                        Core.ItemSubscribed(itemKey, loggedInMember);

                        if (success)
                        {
                            if (isAjax)
                            {
                                core.Response.SendStatus("subscriptionAccepted");
                            }
                            else
                            {
                                core.Display.ShowMessage("Subscribed", "You have successfully subscribed.");
                            }
                        }
                        else
                        {
                            core.Response.ShowMessage("error", "Error", "Subscription unsuccessful.");
                        }
                        break;
                    case "unsubscribe":
                        success = Subscription.UnsubscribeFromItem(core, itemKey);
                        Core.ItemUnsubscribed(itemKey, loggedInMember);

                        if (success)
                        {
                            if (isAjax)
                            {
                                core.Response.SendStatus("unsubscriptionAccepted");
                            }
                            else
                            {
                                core.Display.ShowMessage("Unsubscribed", "You have successfully unsubscribed.");
                            }
                        }
                        else
                        {
                            core.Response.ShowMessage("error", "Error", "Unsubscription unsuccessful.");
                        }
                        break;
                }
            }
            catch (InvalidItemException)
            {
                core.Response.ShowMessage("invalidItem", "Invalid Item", "The item you have attempted to subscribe to is invalid.");
            }
            catch (InvalidSubscriptionException)
            {
                core.Response.ShowMessage("invalidSubscription", "Invalid Subscription", "The subscription is not valid.");
            }
            catch (AlreadySubscribedException)
            {
                core.Response.ShowMessage("alreadySubscribed", "Already Subscribed", "You have already subscribe to this item, you cannot subscribe to it again");
            }
        }
    }
}
