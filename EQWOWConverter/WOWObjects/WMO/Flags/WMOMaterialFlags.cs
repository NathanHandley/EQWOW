﻿//  Author: Nathan Handley (nathanhandley@protonmail.com)
//  Copyright (c) 2024 Nathan Handley
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EQWOWConverter.WOWObjects
{
    internal enum WMOMaterialFlags : UInt32
    {
        DisableLighting = 0x1,
        DisableFogShadow = 0x2,
        Unculled = 0x4,
        ExteriorLit = 0x8,
        LightAtNight = 0x10,
        IsWindow = 0x20,
        ClampTextureS = 0x40,
        ClampTextureT = 0x80,
        Unused = 0x100
    }
}
