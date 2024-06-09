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
using EQWOWConverter.Objects;
using EQWOWConverter.Zones.WOW;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EQWOWConverter.Zones
{
    internal class WOWZoneData
    {
        private string ShortName = string.Empty;
        private bool IsLoaded = false;
        private static UInt32 CURRENT_WMOID = Configuration.CONFIG_DBCID_WMOID_START;
        private static UInt32 CURRENT_AREAID = Configuration.CONFIG_DBCID_AREAID_START;
        private static int CURRENT_MAPID = Configuration.CONFIG_DBCID_MAPID_START;

        public List<WorldModelObject> WorldObjects = new List<WorldModelObject>();
        public List<WOWObjectModelData> GeneratedZoneObjects = new List<WOWObjectModelData>();
        public List<Material> Materials = new List<Material>();
        public ColorRGBA AmbientLight = new ColorRGBA();
        public List<LightInstance> LightInstances = new List<LightInstance>();
        public List<WorldModelObjectDoodadInstance> DoodadInstances = new List<WorldModelObjectDoodadInstance>();
        public BoundingBox BoundingBox = new BoundingBox();
        public Fog FogSettings = new Fog();
        public UInt32 AreaID;
        public UInt32 WMOID;
        public int MapID;
        public int LoadingScreenID;
        public ZoneProperties ZoneProperties;

        public Vector3 SafePosition = new Vector3();

        public WOWZoneData(ZoneProperties zoneProperties)
        {
            // Gen/Update IDs
            AreaID = CURRENT_AREAID;
            CURRENT_AREAID++;
            WMOID = CURRENT_WMOID;
            CURRENT_WMOID++;
            MapID = CURRENT_MAPID;
            CURRENT_MAPID++;
            ZoneProperties = zoneProperties;
        }

        public void LoadFromEQZone(EQZoneData eqZoneData)
        {
            if (IsLoaded == true)
                return;
            ShortName = ZoneProperties.ShortName;
            Materials = eqZoneData.Materials;
            AmbientLight = new ColorRGBA(eqZoneData.AmbientLight.R, eqZoneData.AmbientLight.G, eqZoneData.AmbientLight.B, AmbientLight.A);
            LightInstances = eqZoneData.LightInstances; // TODO: Factor for scale

            MeshData meshData = new MeshData();

            // Change face orientation for culling differences between EQ and WoW
            foreach (TriangleFace eqFace in eqZoneData.MeshData.TriangleFaces)
            {
                TriangleFace newFace = new TriangleFace();
                newFace.MaterialIndex = eqFace.MaterialIndex;

                // Rotate the vertices for culling differences
                newFace.V1 = eqFace.V3;
                newFace.V2 = eqFace.V2;
                newFace.V3 = eqFace.V1;

                // Add it
                meshData.TriangleFaces.Add(newFace);
            }

            // Change texture mapping differences between EQ and WoW
            foreach (TextureCoordinates uv in eqZoneData.MeshData.TextureCoordinates)
            {
                TextureCoordinates curTextureCoords = new TextureCoordinates(uv.X, uv.Y * -1);
                meshData.TextureCoordinates.Add(curTextureCoords);
            }

            // Adjust vertices for world scale and rotate around the Z axis 180 degrees
            foreach (Vector3 vertex in eqZoneData.MeshData.Vertices)
            {
                vertex.X *= Configuration.CONFIG_EQTOWOW_WORLD_SCALE;
                vertex.X = -vertex.X;
                vertex.Y *= Configuration.CONFIG_EQTOWOW_WORLD_SCALE;
                vertex.Y = -vertex.Y;
                vertex.Z *= Configuration.CONFIG_EQTOWOW_WORLD_SCALE;
                meshData.Vertices.Add(vertex);
            }

            // Add object instances
            foreach (ObjectInstance objectInstance in eqZoneData.ObjectInstances)
            {
                WorldModelObjectDoodadInstance doodadInstance = new WorldModelObjectDoodadInstance();
                doodadInstance.ObjectName = objectInstance.ModelName;
                doodadInstance.Position.X = objectInstance.Position.X * Configuration.CONFIG_EQTOWOW_WORLD_SCALE;
                // Invert Z and Y because of mapping differences
                doodadInstance.Position.Z = objectInstance.Position.Y * Configuration.CONFIG_EQTOWOW_WORLD_SCALE;
                doodadInstance.Position.Y = objectInstance.Position.Z * Configuration.CONFIG_EQTOWOW_WORLD_SCALE;

                // Also rotate the X and Y positions around Z axis 180 degrees
                doodadInstance.Position.X = -doodadInstance.Position.X;
                doodadInstance.Position.Y = -doodadInstance.Position.Y;

                // Calculate the rotation
                float rotateYaw = Convert.ToSingle(Math.PI / 180) * -objectInstance.Rotation.Z;
                float rotatePitch = Convert.ToSingle(Math.PI / 180) * objectInstance.Rotation.X;
                float rotateRoll = Convert.ToSingle(Math.PI / 180) * objectInstance.Rotation.Y;
                System.Numerics.Quaternion rotationQ = System.Numerics.Quaternion.CreateFromYawPitchRoll(rotateYaw, rotatePitch, rotateRoll);
                doodadInstance.Orientation.X = rotationQ.X;
                doodadInstance.Orientation.Y = rotationQ.Y;
                doodadInstance.Orientation.Z = rotationQ.Z;
                doodadInstance.Orientation.W = -rotationQ.W; // Flip the sign for handedness

                // Scale is confirmed to always have the same value in x, y, z
                doodadInstance.Scale = objectInstance.Scale.X;

                // Add it
                DoodadInstances.Add(doodadInstance);
            }

            // Generate world objects
            meshData.Normals = eqZoneData.MeshData.Normals;
            meshData.VertexColors = eqZoneData.MeshData.VertexColors;

            WorldObjects.Clear();

            // Build liquid wmos first
            foreach (ZonePropertiesLiquidVolume liquidVolume in ZoneProperties.LiquidVolumes)
            {
                // Generate and add the world model object
                WorldModelObject curWorldModelObject = new WorldModelObject(liquidVolume.VolumeBox, WorldModelObjectType.LiquidVolume);
                WorldObjects.Add(curWorldModelObject);
            }

            // Determine which materials are animated and create objects to represent them
            foreach (Material material in Materials)
                if (material.IsAnimated() && material.IsRenderable())
                {
                    MeshData allMeshData = new MeshData();
                    GenerateAndAddObjectInstancesForZoneMaterial(material, meshData);
                }

            // If this can be generated as a single WMO, just do that
            if (meshData.TriangleFaces.Count <= Configuration.CONFIG_WOW_MAX_FACES_PER_WMOGROUP)
            {
                List<string> materialNames = new List<string>();
                foreach(Material material in Materials)
                    materialNames.Add(material.Name);
                GenerateWorldModelObjectByMaterials(materialNames, meshData.TriangleFaces, meshData);
            }
            // Otherwise, break into parts
            else
            {
                // Generate the world groups by splitting the map down into subregions as needed
                BoundingBox fullBoundingBox = BoundingBox.GenerateBoxFromVectors(meshData.Vertices, Configuration.CONFIG_EQTOWOW_ADDED_BOUNDARY_AMOUNT);
                List<string> materialNames = new List<string>();
                foreach (Material material in Materials)
                    materialNames.Add(material.Name);
                GenerateWorldModelObjectsByXYRegion(fullBoundingBox, materialNames, meshData.TriangleFaces, meshData);
            }

            // Save the loading screen
            switch (ZoneProperties.Continent)
            {
                case ZoneContinent.Antonica:
                case ZoneContinent.Faydwer:
                case ZoneContinent.Development:
                case ZoneContinent.Odus:
                    {
                        LoadingScreenID = Configuration.CONFIG_DBCID_LOADINGSCREENID_START;
                    } break;
                case ZoneContinent.Kunark:
                    {
                        LoadingScreenID = Configuration.CONFIG_DBCID_LOADINGSCREENID_START + 1;
                    }
                    break;
                case ZoneContinent.Velious:
                    {
                        LoadingScreenID = Configuration.CONFIG_DBCID_LOADINGSCREENID_START + 2;
                    }
                    break;                
                default:
                    {
                        Logger.WriteError("Error setting loading screen, as the passed continent was not handled");
                    } break;
            }

            // Rebuild the bounding box
            BoundingBox = BoundingBox = BoundingBox.GenerateBoxFromVectors(meshData.Vertices, Configuration.CONFIG_EQTOWOW_ADDED_BOUNDARY_AMOUNT);
            IsLoaded = true;
        }

        private void GenerateWorldModelObjectsByXYRegion(BoundingBox boundingBox, List<string> materialNames, List<TriangleFace> faces, MeshData meshData)
        {
            // If there are too many triangles to fit in a single box, cut the box into two and generate two child world model objects
            if (faces.Count > Configuration.CONFIG_WOW_MAX_FACES_PER_WMOGROUP)
            {
                // Create two new bounding boxes
                SplitBox splitBox = SplitBox.GenerateXYSplitBoxFromBoundingBox(boundingBox);

                // Calculate what triangles fit into these boxes
                List<TriangleFace> aBoxTriangles = new List<TriangleFace>();
                List<TriangleFace> bBoxTriangles = new List<TriangleFace>();

                foreach (TriangleFace triangle in faces)
                {
                    // Get center point
                    Vector3 v1 = meshData.Vertices[triangle.V1];
                    Vector3 v2 = meshData.Vertices[triangle.V2];
                    Vector3 v3 = meshData.Vertices[triangle.V3];
                    Vector3 center = new Vector3((v1.X + v2.X + v3.X) / 3, (v1.Y + v2.Y + v3.Y) / 3, (v1.Z + v2.Z + v3.Z) / 3);

                    // Align to the first box if it is inside it (only based on xy), otherwise put in the other box
                    // and don't do if/else since there is intentional overlap
                    if (center.X >= splitBox.BoxA.BottomCorner.X && center.X <= splitBox.BoxA.TopCorner.X &&
                        center.Y >= splitBox.BoxA.BottomCorner.Y && center.Y <= splitBox.BoxA.TopCorner.Y)
                    {
                        aBoxTriangles.Add(new TriangleFace(triangle));
                    }
                    if (center.X >= splitBox.BoxB.BottomCorner.X && center.X <= splitBox.BoxB.TopCorner.X &&
                        center.Y >= splitBox.BoxB.BottomCorner.Y && center.Y <= splitBox.BoxB.TopCorner.Y)
                    {
                        bBoxTriangles.Add(new TriangleFace(triangle));
                    }
                }

                // Generate for the two sub boxes
                GenerateWorldModelObjectsByXYRegion(splitBox.BoxA, materialNames, aBoxTriangles, meshData);
                GenerateWorldModelObjectsByXYRegion(splitBox.BoxB, materialNames, bBoxTriangles, meshData);
            }
            else
            {
                GenerateWorldModelObjectByMaterials(materialNames, faces, meshData);
            }
        }

         private void GenerateWorldModelObjectByMaterials(List<string> materialNames, List<TriangleFace> faceToProcess, MeshData meshData)
        {
            List<UInt32> materialIDs = new List<UInt32>();
            bool materialFound = false;

            // Get the related materials
            foreach (string materialName in materialNames)
            {
                foreach (Material material in Materials)
                {
                    if (material.Name == materialName)
                    {
                        materialIDs.Add(material.Index);
                        materialFound = true;
                        break;
                    }
                }
                if (materialFound == false)
                {
                    Logger.WriteError("Error generating world model object, as material named '" + materialName +"' could not be found");
                    return;
                }
            }

            // Build a list of faces specific to these materials, controlling for overflow
            bool facesLeftToProcess = true;
            while (facesLeftToProcess)
            {
                facesLeftToProcess = false;
                List<TriangleFace> facesInGroup = new List<TriangleFace>();
                SortedSet<int> faceIndexesToDelete = new SortedSet<int>();
                for (int i = 0; i < faceToProcess.Count; i++)
                {
                    // Skip anything not matching the material
                    if (materialIDs.Contains(Convert.ToUInt32(faceToProcess[i].MaterialIndex)) == false)
                        continue;

                    // Save it
                    facesInGroup.Add(faceToProcess[i]);
                    faceIndexesToDelete.Add(i);

                    // Only go up to a maximum to avoid overflowing the model arrays
                    if (facesInGroup.Count >= Configuration.CONFIG_WOW_MAX_FACES_PER_WMOGROUP)
                    {
                        facesLeftToProcess = true;
                        break;
                    }
                }

                // Purge the faces from the original list
                foreach (int faceIndex in faceIndexesToDelete.Reverse())
                    faceToProcess.RemoveAt(faceIndex);

                // Generate the world model object
                GenerateWorldModelObjectFromFaces(facesInGroup, meshData);
            }
        }

        private void GenerateAndAddObjectInstancesForZoneMaterial(Material material, MeshData allMeshData)
        {
            MeshData extractedMeshData = allMeshData.GetMeshDataForMaterial(material);

            // Generate the object
            string name = "ZO_" + ShortName + "_" + material.Name;
            WOWObjectModelData newObject = new WOWObjectModelData();
            newObject.Load(name, new List<Material> { material }, extractedMeshData, new List<Vector3>(), new List<TriangleFace>(), false);
            GeneratedZoneObjects.Add(newObject);

            // Add as a doodad
            WorldModelObjectDoodadInstance doodadInstance = new WorldModelObjectDoodadInstance();
            doodadInstance.ObjectName = name;
            doodadInstance.Position = new Vector3(0, 0, 0);
            DoodadInstances.Add(doodadInstance);
        }

        private void GenerateWorldModelObjectFromFaces(List<TriangleFace> faces, MeshData meshData)
        {
            // Generate and add the world model object
            MeshData extractedMeshData = meshData.GetMeshDataForFaces(faces);
            WorldModelObject curWorldModelObject = new WorldModelObject(extractedMeshData, Materials, DoodadInstances, ZoneProperties);
            WorldObjects.Add(curWorldModelObject);
        }
    }
}
