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
    internal class TextureCoordinates
    {
        public float X;
        public float Y;

        public TextureCoordinates()
        {

        }

        public TextureCoordinates(float x, float y)
        {
            X = x;
            Y = y;
        }

        public bool HasOversizedCoordinates()
        {
            if (X - float.Epsilon > 1.0f)
                return true;
            else if (X + float.Epsilon < -1.0f)
                return true;
            else if (Y - float.Epsilon > 1.0f)
                return true;
            else if (Y + float.Epsilon < -1.0f)
                return true;

            return false;
        }

        public List<byte> ToBytes()
        {
            List<byte> returnBytes = new List<byte>();
            returnBytes.AddRange(BitConverter.GetBytes(X));
            returnBytes.AddRange(BitConverter.GetBytes(Y));
            return returnBytes;
        }
    }
}
