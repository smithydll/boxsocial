/*
 * Box Social™
 * http://boxsocial.net/
 * Copyright © 2007, David Lachlan Smith
 * 
 * $Id: AccountBlog.cs,v 1.1 2007/11/18 00:22:42 Bakura\lachlan Exp $
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
using System.Text;

namespace BoxSocial.Internals
{
    public enum AppPrimitives : byte
    {
        None = 0x00,
        Member = 0x01,
        Group = 0x02,
        Network = 0x04,
        Any = 0x08,
    }

    public interface IAppInfo
    {
        void Initialise(Core core);

        AppPrimitives GetAppPrimitiveSupport();
    }
}
