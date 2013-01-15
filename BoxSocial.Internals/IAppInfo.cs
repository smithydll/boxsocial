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
using System.Text;

namespace BoxSocial.Internals
{
    public enum AppPrimitives : byte
    {
        None = 0x00,
        Member = 0x01,
        Group = 0x02,
        Network = 0x04,
        Application = 0x08,
        Musician = 0x10,
        SubGroup = 0x20,
        Any = Member | Group | Network | Application | Musician | SubGroup,
    }

    public interface IAppInfo
    {
        void Initialise(Core core);

        AppPrimitives GetAppPrimitiveSupport();
    }
}
