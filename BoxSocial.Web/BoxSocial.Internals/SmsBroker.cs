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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    public class SmsBroker
    {
        private Core core;
        private SmsGateway gateway;
        private Dictionary<string, SmsGateway> gateways;

        public SmsBroker(Core core)
        {
            this.core = core;
        }

        public bool SendSms(string toNumber, string message)
        {
            if (IsPhoneNumber(toNumber))
            {
                int countryCode = GetCountryCode(toNumber);

                List<string> prefixes = new List<string>(core.Settings.SmsPrefixes.Split(new char[] { ',' }));

                if (prefixes.Contains(countryCode.ToString()))
                {
                    if (GetSmsGateway(countryCode) != null)
                    {
                        GetSmsGateway(countryCode).SendSms(toNumber, message);
                        return true;
                    }
                }
                
                if (Sms != null)
                {
                    Sms.SendSms(toNumber, message);
                    return true;
                }
            }

            return false;
        }

        public SmsGateway Sms
        {
            get
            {
                if (gateway == null)
                {
                    if (core.Settings.SmsProvider == "http")
                    {
                        gateway = new HttpSmsGateway(core.Settings.SmsHttpGateway);
                    }
                    else if (core.Settings.SmsProvider == "oauth")
                    {
                        gateway = new OAuthSmsGateway(core.Settings.SmsOAuthTokenUri, core.Settings.SmsOAuthSmsUri, core.Settings.SmsOAuthKey, core.Settings.SmsOAuthSecret);
                    }
                    else if (core.Settings.SmsProvider == "oauth2")
                    {
                        gateway = new OAuth2SmsGateway(core.Settings.SmsOAuthTokenUri, core.Settings.SmsOAuthSmsUri, core.Settings.SmsOAuthKey, core.Settings.SmsOAuthSecret, core.Settings.SmsOauthTokenParameters, core.Settings.SmsOAuthSmsAuthorization, core.Settings.SmsOAuthSmsBody);
                    }
                }
                return gateway;
            }
        }

        public SmsGateway GetSmsGateway(int countryCode)
        {
            if (gateways == null)
            {
                gateways = new Dictionary<string, SmsGateway>();
            }
            if (!gateways.ContainsKey(countryCode.ToString()))
            {
                SmsGateway sms = null;

                if (core.Settings.GetSmsProvider(countryCode.ToString()) == "http")
                {
                    sms = new HttpSmsGateway(core.Settings.GetSmsHttpGateway(countryCode.ToString()));
                }
                else if (core.Settings.GetSmsProvider(countryCode.ToString()) == "oauth")
                {
                    sms = new OAuthSmsGateway(core.Settings.GetSmsOAuthTokenUri(countryCode.ToString()), core.Settings.GetSmsOAuthSmsUri(countryCode.ToString()), core.Settings.GetSmsOAuthKey(countryCode.ToString()), core.Settings.GetSmsOAuthSecret(countryCode.ToString()));
                }
                else if (core.Settings.GetSmsProvider(countryCode.ToString()) == "oauth2")
                {
                    sms = new OAuth2SmsGateway(core.Settings.GetSmsOAuthTokenUri(countryCode.ToString()), core.Settings.GetSmsOAuthSmsUri(countryCode.ToString()), core.Settings.GetSmsOAuthKey(countryCode.ToString()), core.Settings.GetSmsOAuthSecret(countryCode.ToString()), core.Settings.GetSmsOauthTokenParameters(countryCode.ToString()), core.Settings.GetSmsOAuthSmsAuthorization(countryCode.ToString()), core.Settings.GetSmsOAuthSmsBody(countryCode.ToString()));
                }

                gateways.Add(countryCode.ToString(), sms);
                return sms;
            }
            else
            {
                return gateways[countryCode.ToString()];
            }
        }

        public bool IsPhoneNumber(string toNumber)
        {
            return true;
        }

        public int GetCountryCode(string toNumber)
        {
            bool international = false;
            int firstDigit, secondDigit, thirdDigit;

            if (toNumber.StartsWith("+"))
            {
                international = true;

                int.TryParse(toNumber[1].ToString(), out firstDigit);
                int.TryParse(toNumber[2].ToString(), out secondDigit);
                int.TryParse(toNumber[3].ToString(), out thirdDigit);
            }
            else
            {
                int.TryParse(toNumber[0].ToString(), out firstDigit);
                int.TryParse(toNumber[1].ToString(), out secondDigit);
                int.TryParse(toNumber[2].ToString(), out thirdDigit);
            }

            switch (firstDigit)
            {
                case 1:
                    return 1;
                case 6:
                    switch (secondDigit)
                    {
                        case 0:
                            return 60;
                        case 1:
                            return 61;
                        case 2:
                            return 62;
                        case 3:
                            return 63;
                        case 4:
                            return 64;
                        case 5:
                            return 65;
                        case 6:
                            return 66;
                        case 7:
                            switch (thirdDigit)
                            {
                                case 0:
                                    return 670;
                                case 1:
                                    return 671;
                                case 2:
                                    return 672;
                                case 3:
                                    return 673;
                                case 4:
                                    return 674;
                                case 5:
                                    return 675;
                                case 6:
                                    return 676;
                                case 7:
                                    return 677;
                                case 8:
                                    return 678;
                                case 9:
                                    return 679;
                            }
                            break;
                        case 8:
                            switch (thirdDigit)
                            {
                                case 0:
                                    return 680;
                                case 1:
                                    return 681;
                                case 2:
                                    return 682;
                                case 3:
                                    return 683;
                                case 4:
                                    return 684;
                                case 5:
                                    return 685;
                                case 6:
                                    return 686;
                                case 7:
                                    return 687;
                                case 8:
                                    return 688;
                                case 9:
                                    return 689;
                            }
                            break;
                        case 9:
                            switch (thirdDigit)
                            {
                                case 0:
                                    return 690;
                                case 1:
                                    return 691;
                                case 2:
                                    return 692;
                                case 3:
                                    return 693;
                                case 4:
                                    return 694;
                                case 5:
                                    return 695;
                                case 6:
                                    return 696;
                                case 7:
                                    return 697;
                                case 8:
                                    return 698;
                                case 9:
                                    return 699;
                            }
                            break;
                    }
                    break;
            }

            return 0;
        }
    }
}
