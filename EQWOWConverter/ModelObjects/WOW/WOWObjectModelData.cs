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

using EQWOWConverter.Common;
using EQWOWConverter.ModelObjects;
using EQWOWConverter.Zones;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EQWOWConverter.Objects
{
    internal class WOWObjectModelData
    {
        public string Name = string.Empty;
        public List<ModelAnimation> ModelAnimations = new List<ModelAnimation>();
        public List<ModelVertex> ModelVerticies = new List<ModelVertex>();
        public List<ModelBone> ModelBones = new List<ModelBone>();
        public List<UInt16> ModelBoneKeyLookups = new List<UInt16>();
        public List<Int16> ModelBoneLookups = new List<Int16>();
        public List<ModelMaterial> ModelMaterials = new List<ModelMaterial>();
        public List<ModelTexture> ModelTextures = new List<ModelTexture>();
        public List<Int16> ModelTextureLookups = new List<Int16>();
        public List<Int16> ModelTextureMappingLookups = new List<Int16>();
        public List<Int16> ModelReplaceableTextureLookups = new List<Int16>();
        public List<Int16> ModelTextureTransparencyWeightsLookups = new List<Int16>();
        public List<ModelTextureTransparency> ModelTextureTransparencies = new List<ModelTextureTransparency>();
        public List<Int16> ModelTextureTransformationsLookup = new List<Int16>();
        public List<UInt16> ModelSecondTextureMaterialOverrides = new List<UInt16>();
        public List<TriangleFace> ModelTriangles = new List<TriangleFace>();

        public BoundingBox BoundingBox = new BoundingBox();
        public float BoundingSphereRadius = 0f;
        public BoundingBox CollisionBoundingBox = new BoundingBox();
        public float CollisionSphereRaidus = 0f;

        public WOWObjectModelData()
        {

        }

        // Note: Only working for static for now, but more to come
        public void LoadFromEQObject(string name, EQModelObjectData eqObject)
        {
            // Save Name
            Name = name;

            // Make one animation
            ModelAnimations.Add(new ModelAnimation());

            // Make one material for now
            ModelMaterials.Add(new ModelMaterial());

            // Change face orientation for culling differences between EQ and WoW
            List<TriangleFace> triangleFaces = new List<TriangleFace>();
            foreach (TriangleFace eqFace in eqObject.TriangleFaces)
            {
                TriangleFace newFace = new TriangleFace();
                newFace.MaterialIndex = eqFace.MaterialIndex;

                // Rotate the verticies for culling differences
                newFace.V1 = eqFace.V3;
                newFace.V2 = eqFace.V2;
                newFace.V3 = eqFace.V1;

                // Add it
                triangleFaces.Add(newFace);
            }

            if (eqObject.Verticies.Count != eqObject.TextureCoords.Count && eqObject.Verticies.Count != eqObject.Normals.Count)
            {
                Logger.WriteLine("Failed to load wowobject from eqobject named '" + name + "' since vertex count doesn't match texture coordinate count or normal count");
                return;
            }

            // Read in all the verticies
            for (int i = 0; i < eqObject.Verticies.Count; i++)
            {
                ModelVertex newModelVertex = new ModelVertex();

                // Read vertex, and account for world scale
                Vector3 curVertex = eqObject.Verticies[i];
                newModelVertex.Position.X = curVertex.X * Configuration.CONFIG_EQTOWOW_WORLD_SCALE;
                newModelVertex.Position.Y = curVertex.Y * Configuration.CONFIG_EQTOWOW_WORLD_SCALE;
                newModelVertex.Position.Z = curVertex.Z * Configuration.CONFIG_EQTOWOW_WORLD_SCALE;

                // Read texture coordinates, and factor for mapping differences between EQ and WoW
                TextureCoordinates curTextureCoordinates = eqObject.TextureCoords[i];
                newModelVertex.Texture1TextureCoordinates.X = curTextureCoordinates.X;
                newModelVertex.Texture1TextureCoordinates.Y = curTextureCoordinates.Y * -1;

                // Read normals
                Vector3 curNormal = eqObject.Normals[i];
                newModelVertex.Normal.X = curNormal.X;
                newModelVertex.Normal.Y = curNormal.Y;
                newModelVertex.Normal.Z = curNormal.Z;

                ModelVerticies.Add(newModelVertex);
            }

            // Read in the textures
            foreach(Material material in eqObject.Materials)
            {
                // Only grab first for now
                if (material.AnimationTextures.Count > 0)
                {
                    ModelTexture newModelTexture = new ModelTexture();
                    newModelTexture.TextureName = material.AnimationTextures[0];
                    ModelTextures.Add(newModelTexture);
                }
            }

            CalculateBoundingBoxesAndRadius();
        }

        private void CalculateBoundingBoxesAndRadius()
        {
            //// Calculate it by using the bounding box of all WorldModelObjects
            //BoundingBox = new BoundingBox();
            //foreach (WorldModelObject worldModelObject in WorldObjects)
            //{
            //    if (worldModelObject.BoundingBox.TopCorner.X > BoundingBox.TopCorner.X)
            //        BoundingBox.TopCorner.X = worldModelObject.BoundingBox.TopCorner.X;
            //    if (worldModelObject.BoundingBox.TopCorner.Y > BoundingBox.TopCorner.Y)
            //        BoundingBox.TopCorner.Y = worldModelObject.BoundingBox.TopCorner.Y;
            //    if (worldModelObject.BoundingBox.TopCorner.Z > BoundingBox.TopCorner.Z)
            //        BoundingBox.TopCorner.Z = worldModelObject.BoundingBox.TopCorner.Z;

            //    if (worldModelObject.BoundingBox.BottomCorner.X < BoundingBox.BottomCorner.X)
            //        BoundingBox.BottomCorner.X = worldModelObject.BoundingBox.BottomCorner.X;
            //    if (worldModelObject.BoundingBox.BottomCorner.Y < BoundingBox.BottomCorner.Y)
            //        BoundingBox.BottomCorner.Y = worldModelObject.BoundingBox.BottomCorner.Y;
            //    if (worldModelObject.BoundingBox.BottomCorner.Z < BoundingBox.BottomCorner.Z)
            //        BoundingBox.BottomCorner.Z = worldModelObject.BoundingBox.BottomCorner.Z;
            //}
        }
    }
}
