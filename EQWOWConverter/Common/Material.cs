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
using System.Xml.Linq;

namespace EQWOWConverter.Common
{
    internal class Material
    {
        public UInt32 Index = 0;
        public MaterialType MaterialType = MaterialType.Diffuse;
        public string Name = string.Empty;
        public List<string> TextureNameArray = new List<string>();
        public MaterialAnimationType TextureAnimationType = MaterialAnimationType.None;
        public string CombinedTransformationAnimationTextureName = string.Empty;
        public UInt32 AnimationDelayMs = 0;
        public int OriginalTextureWidth = 0;
        public int OriginalTextureHeight = 0;

        public List<string> RenderTextureNames
        {
            get
            {
                List<string> preparedTextureNames = new List<string>();
                if (CombinedTransformationAnimationTextureName != string.Empty)
                    preparedTextureNames.Add(CombinedTransformationAnimationTextureName);
                else
                    preparedTextureNames = TextureNameArray;
                return preparedTextureNames;
            }
        }

        public Material(string name, UInt32 index, MaterialType materialType, List<string> sourceTextureNameArray,
            UInt32 animationDelayMS, int sourceTextureWidth, int sourceTextureHeight, string combinedTransformAnimationTextureName)
        {
            Name = name;
            Index = index;
            MaterialType = materialType;
            TextureNameArray = sourceTextureNameArray;
            AnimationDelayMs = animationDelayMS;
            OriginalTextureWidth = sourceTextureWidth;
            OriginalTextureHeight = sourceTextureHeight;
            CombinedTransformationAnimationTextureName = combinedTransformAnimationTextureName;
        }

        public bool IsAnimated()
        {
            if (AnimationDelayMs > 0)
                return true;
            else
                return false;
        }

        public bool IsRenderable()
        {
            if (MaterialType == MaterialType.Invisible)
                return false;
            if (MaterialType == MaterialType.Boundary)
                return false;
            if (MaterialType == MaterialType.DiffuseSkydome)
                return false;
            if (MaterialType == MaterialType.TransparentSkydome)
                return false;
            if (TextureNameArray.Count == 0)
                return false;
            return true;
        }

        public int NumOfAnimationFrames()
        {
            return TextureNameArray.Count();
        }

        public string GetTextureSuffix()
        {
            switch (MaterialType)
            {
                case MaterialType.Transparent25Percent: return "a25";
                case MaterialType.Transparent50Percent: return "a50";
                case MaterialType.Transparent75Percent: return "a75";
                default: return "";
            }
        }

        public Vector3 GetTranslationForAnimationFrame(int frameIndex)
        {
            // 2x2 animation texture
            if (NumOfAnimationFrames() <= 4)
            {
                switch (frameIndex)
                {
                    case 0: return new Vector3(0.0f, 0.0f, 0.0f);
                    case 1: return new Vector3(0.5f, 0.0f, 0.0f);
                    case 2: return new Vector3(0.0f, 0.5f, 0.0f);
                    case 3: return new Vector3(0.5f, 0.5f, 0.0f);
                    default:
                    {
                        Logger.WriteError("GetTranslationForAnimationFrame Error, frame index for material '" + Name + "' was '" + frameIndex + "' when it was a 2x2 texture");
                        return new Vector3(0.0f, 0.0f, 0.0f);
                    }
                }
            }
            // 4x4 animation texture
            else if (NumOfAnimationFrames() <= 16)
            {
                switch (frameIndex)
                {
                    case 0: return new Vector3(0.0f, 0.0f, 0.0f);
                    case 1: return new Vector3(-0.25f, 0.0f, 0.0f);
                    case 2: return new Vector3(-0.5f, 0.0f, 0.0f);
                    case 3: return new Vector3(-0.75f, 0.0f, 0.0f);

                    case 4: return new Vector3(0.0f, 0.25f, 0.0f);
                    case 5: return new Vector3(-0.25f, 0.25f, 0.0f);
                    case 6: return new Vector3(-0.5f, 0.25f, 0.0f);
                    case 7: return new Vector3(-0.75f, 0.25f, 0.0f);

                    case 8: return new Vector3(0.0f, 0.5f, 0.0f);
                    case 9: return new Vector3(-0.25f, 0.5f, 0.0f);
                    case 10: return new Vector3(-0.5f, 0.5f, 0.0f);
                    case 11: return new Vector3(-0.75f, 0.5f, 0.0f);

                    case 12: return new Vector3(0.0f, 0.75f, 0.0f);
                    case 13: return new Vector3(-0.25f, 0.75f, 0.0f);
                    case 14: return new Vector3(-0.5f, 0.75f, 0.0f);
                    case 15: return new Vector3(-0.75f, 0.75f, 0.0f);
                    default:
                    {
                        Logger.WriteError("GetTranslationForAnimationFrame Error, frame index for material '" + Name + "' was '" + frameIndex + "' when it was a 4x4 texture");
                        return new Vector3(0.0f, 0.0f, 0.0f);
                    }

                }
            }
            else
            {
                Logger.WriteError("GetTranslationForAnimationFrame Error, unhandled for animations > 16 frames");
                return new Vector3(0.0f, 0.0f, 0.0f);
            }
        }

        // This returns an animation-aware and half-pixel corrected (on edge) coordinate set
        public TextureCoordinates GetCorrectedBaseCoordinates(TextureCoordinates uncorrectedCoordinates)
        {
            TextureCoordinates correctedCoordinates = new TextureCoordinates();

            // How much to proportion for based on the animation data
            float proportionFactor = 1.0f;
            if (TextureAnimationType == MaterialAnimationType.TextureTransform)
            {
                // 2x2
                if (NumOfAnimationFrames() <= 4)
                    proportionFactor = 0.5f;
                else if (NumOfAnimationFrames() <= 16)
                    proportionFactor = 0.25f;
                else
                {
                    Logger.WriteError("GetCorrectedBaseCoordinates Error, unhandled for animations > 16 frames");
                    correctedCoordinates.X = uncorrectedCoordinates.X;
                    correctedCoordinates.Y = uncorrectedCoordinates.Y;
                    return correctedCoordinates;
                }
            }

            // u(x) == 0, half-pixel correct
            if (uncorrectedCoordinates.X <= float.Epsilon && uncorrectedCoordinates.X >= float.Epsilon)
            {
                correctedCoordinates.X = 0.5f / Convert.ToSingle(OriginalTextureWidth);
            }
            // u(x) == 1, half-pixel correct and factor animation
            else if (uncorrectedCoordinates.X <= (1.0f + float.Epsilon) && uncorrectedCoordinates.X >= (1.0f - float.Epsilon))
            {
                float workingXCoordinate = 1.0f - (0.5f / Convert.ToSingle(OriginalTextureWidth));
                if (TextureAnimationType == MaterialAnimationType.TextureTransform)
                    workingXCoordinate *= proportionFactor;
                correctedCoordinates.X = workingXCoordinate;
            }
            // u(x) == -1 half-pixel correct and factor animation
            else if (uncorrectedCoordinates.X >= (-1.0f - float.Epsilon) && uncorrectedCoordinates.X <= (-1.0f + float.Epsilon))
            {
                float workingXCoordinate = -1.0f + (0.5f / Convert.ToSingle(OriginalTextureWidth));
                if (TextureAnimationType == MaterialAnimationType.TextureTransform)
                    workingXCoordinate *= proportionFactor;
                correctedCoordinates.X = workingXCoordinate;
            }
            // Other cases are just animation factor (revisit if there are 'even' 2x and 3x+ coordinate cases
            else
            {
                if (TextureAnimationType == MaterialAnimationType.TextureTransform)
                    correctedCoordinates.X = uncorrectedCoordinates.X * proportionFactor;
                else
                    correctedCoordinates.X = uncorrectedCoordinates.X;
            }

            // u(y) == 0, half-pixel correct
            if (uncorrectedCoordinates.Y <= float.Epsilon && uncorrectedCoordinates.Y >= float.Epsilon)
            {
                correctedCoordinates.Y = 0.5f / Convert.ToSingle(OriginalTextureHeight);
            }
            // u(y) == 1, half-pixel correct and factor animation
            else if (uncorrectedCoordinates.Y <= (1.0f + float.Epsilon) && uncorrectedCoordinates.Y >= (1.0f - float.Epsilon))
            {
                float workingYCoordinate = 1.0f - (0.5f / Convert.ToSingle(OriginalTextureHeight));
                if (TextureAnimationType == MaterialAnimationType.TextureTransform)
                    workingYCoordinate *= proportionFactor;
                correctedCoordinates.Y = workingYCoordinate;
            }
            // u(y) == -1 half-pixel correct and factor animation
            else if (uncorrectedCoordinates.Y >= (-1.0f - float.Epsilon) && uncorrectedCoordinates.Y <= (-1.0f + float.Epsilon))
            {
                float workingYCoordinate = -1.0f + (0.5f / Convert.ToSingle(OriginalTextureHeight));
                if (TextureAnimationType == MaterialAnimationType.TextureTransform)
                    workingYCoordinate *= proportionFactor;
                correctedCoordinates.Y = workingYCoordinate;
            }
            // Other cases are just animation factor (revisit if there are 'even' 2x and 3x+ coordinate cases
            else
            {
                if (TextureAnimationType == MaterialAnimationType.TextureTransform)
                    correctedCoordinates.Y = uncorrectedCoordinates.Y * proportionFactor;
                else
                    correctedCoordinates.Y = uncorrectedCoordinates.Y;
            }

            return correctedCoordinates;
        }
    }
}
