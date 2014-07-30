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

using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    public class UnitTests
    {
    }

    [TestFixture]
    public class FunctionsTests
    {
        private Mysql db = null;
        private Template template = null;
        private Core core = null;
        private Functions functions = null;

        [SetUp]
        public void SetUpForNonStaticMembers()
        {
            db = new Mysql("root", "", "zinzam0_test", "192.168.56.101");
            template = new Template(Path.Combine(@"c:\SVN\BoxSocial\BoxSocial", "templates"), "default.html");

            core = new Core(db, template);
            functions = new Functions(core);
        }

        [Test]
        public void IntToMonthTest()
        {
            Assert.AreEqual(functions.IntToMonth(1), "January");
            Assert.AreEqual(functions.IntToMonth(2), "February");
            Assert.AreEqual(functions.IntToMonth(3), "March");
            Assert.AreEqual(functions.IntToMonth(4), "April");
            Assert.AreEqual(functions.IntToMonth(5), "May");
            Assert.AreEqual(functions.IntToMonth(6), "June");
            Assert.AreEqual(functions.IntToMonth(7), "July");
            Assert.AreEqual(functions.IntToMonth(8), "August");
            Assert.AreEqual(functions.IntToMonth(9), "September");
            Assert.AreEqual(functions.IntToMonth(10), "October");
            Assert.AreEqual(functions.IntToMonth(11), "November");
            Assert.AreEqual(functions.IntToMonth(12), "December");
        }

        [Test]
        public void CheckPageNameValidTest()
        {
            Assert.True(Functions.CheckPageNameValid("lachlan"));
            Assert.True(Functions.CheckPageNameValid("Lachlan"));
            Assert.True(Functions.CheckPageNameValid("LACHLAN"));
            Assert.True(Functions.CheckPageNameValid("lachlansmith1"));
            Assert.False(Functions.CheckPageNameValid("lachlan.aspx"));
            Assert.False(Functions.CheckPageNameValid("lachlan.php"));
            Assert.False(Functions.CheckPageNameValid("lachlan.html"));
            Assert.False(Functions.CheckPageNameValid("lachlan.gif"));
            Assert.False(Functions.CheckPageNameValid("lachlan.png"));
            Assert.False(Functions.CheckPageNameValid("lachlan.js"));
            Assert.False(Functions.CheckPageNameValid("lachlan.bmp"));
            Assert.False(Functions.CheckPageNameValid("lachlan.jpg"));
            Assert.False(Functions.CheckPageNameValid("lachlan.jpeg"));
            Assert.False(Functions.CheckPageNameValid("lachlan.zip"));
            Assert.False(Functions.CheckPageNameValid("lachlan.jsp"));
            Assert.False(Functions.CheckPageNameValid("lachlan.cfm"));
            Assert.False(Functions.CheckPageNameValid("lachlan.exe"));
            Assert.False(Functions.CheckPageNameValid("lachlan."));
            Assert.False(Functions.CheckPageNameValid(".lachlan"));
            Assert.False(Functions.CheckPageNameValid("0"));
            Assert.False(Functions.CheckPageNameValid("a"));
            Assert.True(Functions.CheckPageNameValid("abcdefghabcdefghabcdefghabcdefghabcdefghabcdefghabcdefghabcdefgh"));
            Assert.False(Functions.CheckPageNameValid("abcdefghabcdefghabcdefghabcdefghabcdefghabcdefghabcdefghabcdefghi"));
            /*Assert.False(Functions.CheckPageNameValid("lachlan@smith"));
            Assert.True(Functions.CheckPageNameValid("lachlan.smith"));
            Assert.True(Functions.CheckPageNameValid("lachlan-smith"));
            Assert.True(Functions.CheckPageNameValid("lachlan_smith"));
            Assert.True(Functions.CheckPageNameValid("lachlan!smith"));
            Assert.True(Functions.CheckPageNameValid("~lachlan"));
            Assert.True(Functions.CheckPageNameValid("*lachlan*"));
            Assert.True(Functions.CheckPageNameValid("&lachlan&"));
            Assert.True(Functions.CheckPageNameValid("==lachlan=="));
            Assert.True(Functions.CheckPageNameValid("$lachlan$"));
            Assert.True(Functions.CheckPageNameValid("'lachlan'"));*/
        }

        [Test]
        public void GenerateBreadCrumbsTest()
        {
            Assert.AreEqual(Functions.GenerateBreadCrumbs("lachlan", "profile"), "<a href=\"/lachlan\">lachlan</a> <strong>&#8249;</strong> <a href=\"/lachlan/profile\">profile</a>");
            Assert.AreEqual(Functions.GenerateBreadCrumbs("lachlan", "profile/comments"), "<a href=\"/lachlan\">lachlan</a> <strong>&#8249;</strong> <a href=\"/lachlan/profile\">profile</a> <strong>&#8249;</strong> <a href=\"/lachlan/profile/comments\">comments</a>");
        }

        [Test]
        public void TrimStringToWordTest()
        {

            Assert.AreEqual(Functions.TrimStringToWord("How are you doing today maam? The weather today cannot be faulted."), "How are you doing today maam? The weather today cannot be");
            Assert.AreEqual(Functions.TrimStringToWord("How are you doing today maam? The weather today cannot be faulted.", 20), "How are you doing");
            Assert.AreEqual(Functions.TrimStringToWord("How are you doing today maam? The weather today cannot be faulted.", 3), "How");
            Assert.AreEqual(Functions.TrimStringToWord("How are you doing today maam? The weather today cannot be faulted.", 4), "How");
            Assert.AreEqual(Functions.TrimStringToWord("How are you doing today maam? The weather today cannot be faulted.", 5), "How");
            Assert.AreEqual(Functions.TrimStringToWord("How are you doing today maam? The weather today cannot be faulted.", 6), "How");
            Assert.AreEqual(Functions.TrimStringToWord("abcdefghijklmnopqrstuvwxyzabc", 26), "abcdefghijklmnopqrstuvwxyz");
        }

        [Test]
        public void TrimStringWithExtensionTest()
        {
            Assert.AreEqual(Functions.TrimStringWithExtension("mono.exe"), "mono.exe");
            Assert.AreEqual(Functions.TrimStringWithExtension("mono.exe", 5), "m.exe");
            Assert.AreEqual(Functions.TrimStringWithExtension("abcdefghijklmnopqrstuvwxyzabc.exe", 26), "abcdefghijklmnopqrstuv.exe");
            Assert.AreEqual(Functions.TrimStringWithExtension("abcdefghijklmnopqrstuvwxyzabc.c", 26), "abcdefghijklmnopqrstuvwx.c");
        }

        [Test]
        public void TrimStringTest()
        {
            Assert.AreEqual(Functions.TrimString("How are you doing today maam? The weather today cannot be faulted."), "How are you doing today maam? The weather today cannot be fa");
            Assert.AreEqual(Functions.TrimString("How are you doing today maam? The weather today cannot be faulted.", 1), "H");
            Assert.AreEqual(Functions.TrimString("How are you doing today maam? The weather today cannot be faulted.", 3), "How");
            Assert.AreEqual(Functions.TrimString("How", 3), "How");
        }

        [Test]
        public void LimitPageToStartTest()
        {
            Assert.AreEqual(Functions.LimitPageToStart(1, 10), 0);
            Assert.AreEqual(Functions.LimitPageToStart(2, 10), 10);
            Assert.AreEqual(Functions.LimitPageToStart(2, 20), 20);
            Assert.AreEqual(Functions.LimitPageToStart(3, 20), 40);
        }

        [Test]
        public void ParseNumberTest()
        {
            Assert.AreEqual(Functions.ParseNumber("one"), 1);
            Assert.AreEqual(Functions.ParseNumber("two"), 2);
            Assert.AreEqual(Functions.ParseNumber("three"), 3);
            Assert.AreEqual(Functions.ParseNumber("four"), 4);
            Assert.AreEqual(Functions.ParseNumber("five"), 5);
            Assert.AreEqual(Functions.ParseNumber("six"), 6);
            Assert.AreEqual(Functions.ParseNumber("seven"), 7);
            Assert.AreEqual(Functions.ParseNumber("eight"), 8);
            Assert.AreEqual(Functions.ParseNumber("nine"), 9);
            Assert.AreEqual(Functions.ParseNumber("ten"), 10);
            Assert.AreEqual(Functions.ParseNumber("eleven"), 11);
            Assert.AreEqual(Functions.ParseNumber("twelve"), 12);
            Assert.AreEqual(Functions.ParseNumber("thirteen"), 13);
            Assert.AreEqual(Functions.ParseNumber("fourteen"), 14);
            Assert.AreEqual(Functions.ParseNumber("fifteen"), 15);
            Assert.AreEqual(Functions.ParseNumber("sixteen"), 16);
            Assert.AreEqual(Functions.ParseNumber("seventeen"), 17);
            Assert.AreEqual(Functions.ParseNumber("eighteen"), 18);
            Assert.AreEqual(Functions.ParseNumber("ninteen"), 19);
            Assert.AreEqual(Functions.ParseNumber("twenty"), 20);
            Assert.AreEqual(Functions.ParseNumber("twenty one"), 21);
            Assert.AreEqual(Functions.ParseNumber("twenty two"), 22);
            Assert.AreEqual(Functions.ParseNumber("thirty"), 30);
            Assert.AreEqual(Functions.ParseNumber("fourty"), 40);
            Assert.AreEqual(Functions.ParseNumber("fifty"), 50);
            Assert.AreEqual(Functions.ParseNumber("sixty"), 60);
            Assert.AreEqual(Functions.ParseNumber("seventy"), 70);
            Assert.AreEqual(Functions.ParseNumber("eighty"), 80);
            Assert.AreEqual(Functions.ParseNumber("ninety"), 90);
            Assert.AreEqual(Functions.ParseNumber("ninety nine"), 99);
            Assert.AreEqual(Functions.ParseNumber("one hundred"), 100);
            Assert.AreEqual(Functions.ParseNumber("one hundred and one"), 101);
            Assert.AreEqual(Functions.ParseNumber("one hundred and two"), 102);
            Assert.AreEqual(Functions.ParseNumber("two hundred"), 200);
            Assert.AreEqual(Functions.ParseNumber("two hundred and twelve"), 212);
            Assert.AreEqual(Functions.ParseNumber("one thousand"), 1000);
            Assert.AreEqual(Functions.ParseNumber("one thousand one hundred"), 1100);
            Assert.AreEqual(Functions.ParseNumber("one hundred and two thousand"), 102000);
            Assert.AreEqual(Functions.ParseNumber("one hundred and two thousand and five"), 102005);
            Assert.AreEqual(Functions.ParseNumber("one hundred and two thousand and seventy five"), 102075);
            Assert.AreEqual(Functions.ParseNumber("one million"), 1000000);
            Assert.AreEqual(Functions.ParseNumber("one million two hundred and thirty four thousand five hundred and sixty seven"), 1234567);
        }

        [Test]
        public void ParseNumberPartTest()
        {
            Assert.AreEqual(Functions.ParseNumberPart("one"), 1);
            Assert.AreEqual(Functions.ParseNumberPart("two"), 2);
            Assert.AreEqual(Functions.ParseNumberPart("three"), 3);
            Assert.AreEqual(Functions.ParseNumberPart("four"), 4);
            Assert.AreEqual(Functions.ParseNumberPart("five"), 5);
            Assert.AreEqual(Functions.ParseNumberPart("six"), 6);
            Assert.AreEqual(Functions.ParseNumberPart("seven"), 7);
            Assert.AreEqual(Functions.ParseNumberPart("eight"), 8);
            Assert.AreEqual(Functions.ParseNumberPart("nine"), 9);
            Assert.AreEqual(Functions.ParseNumberPart("ten"), 10);
            Assert.AreEqual(Functions.ParseNumberPart("eleven"), 11);
            Assert.AreEqual(Functions.ParseNumberPart("twelve"), 12);
            Assert.AreEqual(Functions.ParseNumberPart("thirteen"), 13);
            Assert.AreEqual(Functions.ParseNumberPart("fourteen"), 14);
            Assert.AreEqual(Functions.ParseNumberPart("fifteen"), 15);
            Assert.AreEqual(Functions.ParseNumberPart("sixteen"), 16);
            Assert.AreEqual(Functions.ParseNumberPart("seventeen"), 17);
            Assert.AreEqual(Functions.ParseNumberPart("eighteen"), 18);
            Assert.AreEqual(Functions.ParseNumberPart("ninteen"), 19);
            Assert.AreEqual(Functions.ParseNumberPart("twenty"), 20);
            Assert.AreEqual(Functions.ParseNumberPart("thirty"), 30);
            Assert.AreEqual(Functions.ParseNumberPart("fourty"), 40);
            Assert.AreEqual(Functions.ParseNumberPart("fifty"), 50);
            Assert.AreEqual(Functions.ParseNumberPart("sixty"), 60);
            Assert.AreEqual(Functions.ParseNumberPart("seventy"), 70);
            Assert.AreEqual(Functions.ParseNumberPart("eighty"), 80);
            Assert.AreEqual(Functions.ParseNumberPart("ninety"), 90);
            Assert.AreEqual(Functions.ParseNumberPart("hundred"), 100);
            Assert.AreEqual(Functions.ParseNumberPart("thousand"), 1000);
            Assert.AreEqual(Functions.ParseNumberPart("million"), 1000000);
            Assert.AreEqual(Functions.ParseNumberPart("billion"), 1000000000);
        }
    }

    [TestFixture]
    public class DisplayTests
    {
        [Test]
        public void SqlEscapeTest()
        {
            Assert.AreEqual(Display.SqlEscape("hi"), "hi");
            Assert.AreEqual(Display.SqlEscape("hi'"), "hi''");
            Assert.AreEqual(Display.SqlEscape("h'i"), "h''i");
            Assert.AreEqual(Display.SqlEscape("'hi"), "''hi");
            Assert.AreEqual(Display.SqlEscape("h''i"), "h''''i");
        }
    }

    [TestFixture]
    public class ItemTests
    {

        [Test]
        public void GetPrimaryKeyTest()
        {
            Assert.AreEqual(Item.GetPrimaryKey(typeof(User)), "user_id");
            Assert.AreEqual(Item.GetPrimaryKey(typeof(ApplicationEntry)), "application_id");
            Assert.AreEqual(Item.GetPrimaryKey(typeof(UserEmail)), "email_id");
        }

        [Test]
        public void GetParentFieldTest()
        {
            Assert.Catch(GetParentFieldTestCase1);
            Assert.AreEqual(Item.GetParentField(typeof(User), typeof(UserEmail)), "email_user_id");
        }

        private void GetParentFieldTestCase1()
        {
            Item.GetParentField(typeof(User));
        }

        [Test]
        public void GetSelectQueryStubText()
        {
            //Assert.AreEqual(Item.GetSelectQueryStub(typeof(UserEmail)), "SELECT `user_emails`.`email_id`, `user_emails`.`email_user_id`, `user_emails`.`email_email`, `user_emails`.`email_verified`, `user_emails`.`email_time_ut`, `user_emails`.`email_activate_code` FROM user_emails;");
        }

        [Test]
        public void GetTableTest()
        {
            Assert.AreEqual(Item.GetTable(typeof(User)), "user_keys");
            Assert.AreEqual(Item.GetTable(typeof(ApplicationEntry)), "applications");
            Assert.AreEqual(Item.GetTable(typeof(UserEmail)), "user_emails");
        }
    }
}
