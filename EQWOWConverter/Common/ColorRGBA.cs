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

namespace EQWOWConverter.Common
{
    internal class ColorRGBA
    {
        public byte R = 0;
        public byte G = 0;
        public byte B = 0;
        public byte A = 0;

        public ColorRGBA()
        {

        }

        public ColorRGBA(ColorRGB colorRGB)
        {
            R = Convert.ToByte(colorRGB.R);
            G = Convert.ToByte(colorRGB.G);
            B = Convert.ToByte(colorRGB.B);
        }

        public ColorRGBA(byte r, byte g, byte b, byte a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public List<byte> ToBytes()
        {
            List<byte> returnBytes = new List<byte>();
            returnBytes.Add(R);
            returnBytes.Add(G);
            returnBytes.Add(B);
            returnBytes.Add(A);
            return returnBytes;
        }
    }
}
