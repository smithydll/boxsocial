using System;
using System.Collections.Generic;
using System.Text;

namespace BoxSocial.Internals
{
    public class Captcha
    {
        /// <summary>
        /// Generate a captcha string
        /// </summary>
        /// <returns></returns>
        public static string GenerateCaptchaString()
        {
            Random rand = new Random();
            string captchaString = "";
            
            char[] chars = { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'J', 'K', 'L', 'M', 'N', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
            
            for (int i = 0; i < 5; i++)
            {
                int j = (int)(rand.NextDouble() * chars.Length);
                captchaString += chars[j].ToString();
            }

            return captchaString;
        }

        public static string GenerateCaptchaSecurityToken()
        {
            Random rand = new Random();
            string captchaString = "";

            char[] chars = { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', '1', '2', '3', '4', '5', '6', '7', '8', '9', '0' };

            for (int i = 0; i < 20; i++)
            {
                int j = (int)(rand.NextDouble() * chars.Length);
                captchaString += chars[j].ToString();
            }

            return User.HashPassword(captchaString);
        }
    }
}
