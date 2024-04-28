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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EQWOWConverter.WOWObjects
{
    internal class WMOGroup : WOWChunkedObject
    {
        public List<byte> GroupBytes = new List<byte>();

        public WMOGroup(Zone zone, WMORoot wmoRoot)
        {
            // MVER (Version) ---------------------------------------------------------------------
            GroupBytes.AddRange(GenerateMVERChunk(zone));

            // MOGP (Container for all other chunks) ----------------------------------------------
            GroupBytes.AddRange(GenerateMOGPChunk(zone, wmoRoot));
        }

        /// <summary>
        /// MVER (Version)
        /// </summary>
        private List<byte> GenerateMVERChunk(Zone zone)
        {
            UInt32 version = 17;
            return WrapInChunk("MVER", BitConverter.GetBytes(version));
        }

        /// <summary>
        /// MOGP (Main container for all other chunks)
        /// </summary>
        private List<byte> GenerateMOGPChunk(Zone zone, WMORoot wmoRoot)
        {
            List<byte> chunkBytes = new List<byte>();

            // Group name offsets in MOGN
            chunkBytes.AddRange(BitConverter.GetBytes(wmoRoot.GroupNameOffset));
            chunkBytes.AddRange(BitConverter.GetBytes(wmoRoot.GroupNameDescriptiveOffset));

            // Flags
            UInt32 groupHeaderFlags = GetPackedFlags(Convert.ToUInt32(WMOGroupFlags.IsOutdoors));
            chunkBytes.AddRange(BitConverter.GetBytes(groupHeaderFlags));

            // Bounding box
            chunkBytes.AddRange(zone.BoundingBox.ToBytes());

            // Portal references (zero for now)
            chunkBytes.AddRange(BitConverter.GetBytes(Convert.ToUInt16(0))); // First portal index
            chunkBytes.AddRange(BitConverter.GetBytes(Convert.ToUInt16(0))); // Number of portals

            // NOTE: Temp code in place. Making everything a single render batch for testing.
            // What exactly is "transbatchcount"?
            chunkBytes.AddRange(BitConverter.GetBytes(Convert.ToUInt16(0))); // transBatchCount
            chunkBytes.AddRange(BitConverter.GetBytes(Convert.ToUInt16(0))); // internalBatchCount
            chunkBytes.AddRange(BitConverter.GetBytes(Convert.ToUInt16(1))); // externalBatchCount
            chunkBytes.AddRange(BitConverter.GetBytes(Convert.ToUInt16(0))); // padding/unknown

            // This fog Id list may be wrong, but hoping that 0 works
            chunkBytes.AddRange(BitConverter.GetBytes(Convert.ToUInt32(0))); // 4 fog IDs that are all zero, I hope...

            // Liquid type (zero until I figure this out)
            chunkBytes.AddRange(BitConverter.GetBytes(Convert.ToUInt32(0)));

            // WMOGroupID (inside WMOAreaTable) - Need to calculate later, so make it 30000
            chunkBytes.AddRange(BitConverter.GetBytes(Convert.ToUInt32(30000)));

            // Unknown values.  Hopefully not needed
            chunkBytes.AddRange(BitConverter.GetBytes(Convert.ToUInt32(0)));
            chunkBytes.AddRange(BitConverter.GetBytes(Convert.ToUInt32(0)));

            // ------------------------------------------------------------------------------------
            // SUB CHUNKS
            // ------------------------------------------------------------------------------------
            // MOPY (Material info for triangles) -------------------------------------------------
            chunkBytes.AddRange(GenerateMOPYChunk(zone));

            // MOVI (MapObject Vertex Indicies) ---------------------------------------------------
            chunkBytes.AddRange(GenerateMOVIChunk(zone));

            // MOVT (Verticies) -------------------------------------------------------------------
            chunkBytes.AddRange(GenerateMOVTChunk(zone));

            // MONR (Normals) ---------------------------------------------------------------------
            chunkBytes.AddRange(GenerateMONRChunk(zone));

            // MOTV (Texture Coordinates) ---------------------------------------------------------
            chunkBytes.AddRange(GenerateMOTVChunk(zone));

            // MOBA (Render Batches) --------------------------------------------------------------
            chunkBytes.AddRange(GenerateMOBAChunk(zone));

            // MOLR (Light References) ------------------------------------------------------------
            // -- If has Lights

            // MODR (Doodad References) -----------------------------------------------------------
            // -- If has Doodads

            // MOBN (Nodes of the BSP tree, used also for collision?) -----------------------------
            // -- If HasBSPTree flag

            // MOBR (Face / Triangle Incidies) ----------------------------------------------------
            // -- If HasBSPTree flag

            // MOCV (Vertex Colors) ---------------------------------------------------------------
            // - If HasVertexColor Flag

            // MLIQ (Liquid/Water details) --------------------------------------------------------
            // - If HasWater flag

            // Note: There can be two MOTV and MOCV blocks depending on flags.  May need to factor for that

            return WrapInChunk("MOGP", chunkBytes.ToArray());
        }

        /// <summary>
        /// MOPY (Material info for triangles)
        /// </summary>
        private List<byte> GenerateMOPYChunk(Zone zone)
        {
            List<byte> chunkBytes = new List<byte>();

            // For now, just one material
            byte polyMaterialFlag = GetPackedFlags(Convert.ToByte(WMOPolyMaterialFlags.Collision),
                                                   Convert.ToByte(WMOPolyMaterialFlags.CollideHit),
                                                   Convert.ToByte(WMOPolyMaterialFlags.Render));
            chunkBytes.Add(polyMaterialFlag);
            chunkBytes.Add(0); // This is the material index, which we'll make 0 so it's the first for now

            return WrapInChunk("MOPY", chunkBytes.ToArray());
        }

        /// <summary>
        /// MOVI (MapObject Vertex Indicies)
        /// </summary>
        private List<byte> GenerateMOVIChunk(Zone zone)
        {
            List<byte> chunkBytes = new List<byte>();

            Logger.WriteLine("WARNING, poly indexes are restricted to short int so big maps will overflow...");
            foreach(PolyIndex polyIndex in zone.RenderMesh.Indicies)
                chunkBytes.AddRange(polyIndex.ToBytes());

            return WrapInChunk("MOVI", chunkBytes.ToArray());
        }

        /// <summary>
        /// MOVT (Verticies)
        /// </summary>
        private List<byte> GenerateMOVTChunk(Zone zone)
        {
            List<byte> chunkBytes = new List<byte>();

            foreach (Vector3 vertex in zone.RenderMesh.Verticies)
                chunkBytes.AddRange(vertex.ToBytes());

            return WrapInChunk("MOVT", chunkBytes.ToArray());
        }

        /// <summary>
        /// MONR (Normals)
        /// </summary>
        private List<byte> GenerateMONRChunk(Zone zone)
        {
            List<byte> chunkBytes = new List<byte>();

            foreach (Vector3 normal in zone.RenderMesh.Normals)
                chunkBytes.AddRange(normal.ToBytes());

            return WrapInChunk("MONR", chunkBytes.ToArray());
        }

        /// <summary>
        /// MOTV (Texture Coordinates)
        /// </summary>
        private List<byte> GenerateMOTVChunk(Zone zone)
        {
            List<byte> chunkBytes = new List<byte>();

            foreach (TextureUv textureCoords in zone.RenderMesh.TextureCoords)
                chunkBytes.AddRange(textureCoords.ToBytes());

            return WrapInChunk("MOTV", chunkBytes.ToArray());
        }

        /// <summary>
        /// MOBA (Render Batches)
        /// </summary>
        private List<byte> GenerateMOBAChunk(Zone zone)
        {
            List<byte> chunkBytes = new List<byte>();

            // TODO: Make this work with multiple render batches, as it a render batch needs to be 1 material only
            // Bounding Box
            chunkBytes.AddRange(zone.BoundingBox.ToBytes());

            // Poly Start Index, 0 for now
            chunkBytes.AddRange(BitConverter.GetBytes(Convert.ToUInt32(0)));

            // Number of poly indexes
            chunkBytes.AddRange(BitConverter.GetBytes(Convert.ToUInt16(zone.RenderMesh.Indicies.Count)));

            // Vertex Start Index, 0 for now
            chunkBytes.AddRange(BitConverter.GetBytes(Convert.ToUInt16(0)));

            // Vertex End Index
            chunkBytes.AddRange(BitConverter.GetBytes(Convert.ToUInt16(zone.RenderMesh.Verticies.Count-1)));

            // Byte padding (or unknown flag, unsure)
            chunkBytes.Add(0);

            // Index of the material, which will be blank for now so it picks the first material
            chunkBytes.Add(0);

            return WrapInChunk("MOBA", chunkBytes.ToArray());
        }

        /// <summary>
        /// MOLR (Light References)
        /// Optional.  Only if it has lights
        /// </summary>
        private List<byte> GenerateMOLRChunk(Zone zone)
        {
            List<byte> chunkBytes = new List<byte>();

            Logger.WriteLine("MOLR Generation unimplemented!");

            return WrapInChunk("MOLR", chunkBytes.ToArray());
        }

        /// <summary>
        /// MODR (Doodad References)
        /// Optional.  If has Doodads
        /// </summary>
        private List<byte> GenerateMODRChunk(Zone zone)
        {
            List<byte> chunkBytes = new List<byte>();

            Logger.WriteLine("MODR Generation unimplemented!");

            return WrapInChunk("MODR", chunkBytes.ToArray());
        }

        /// <summary>
        /// MOBN (Nodes of the BSP tree, used also for collision?)
        /// Optional.  If HasBSPTree flag.
        /// </summary>
        private List<byte> GenerateMOBNChunk(Zone zone)
        {
            List<byte> chunkBytes = new List<byte>();

            Logger.WriteLine("MOBN Generation unimplemented!");

            return WrapInChunk("MOBN", chunkBytes.ToArray());
        }

        /// <summary>
        /// MOBR (Face / Triangle Incidies)
        /// Optional.  If HasBSPTree flag.
        /// </summary>
        private List<byte> GenerateMOBRChunk(Zone zone)
        {
            List<byte> chunkBytes = new List<byte>();

            Logger.WriteLine("MOBR Generation unimplemented!");

            return WrapInChunk("MOBR", chunkBytes.ToArray());
        }

        /// <summary>
        /// MOCV (Vertex Colors)
        /// Optional.  If HasVertexColor Flag
        /// </summary>
        private List<byte> GenerateMOCVChunk(Zone zone)
        {
            List<byte> chunkBytes = new List<byte>();

            Logger.WriteLine("MOCV Generation unimplemented!");

            return WrapInChunk("MOCV", chunkBytes.ToArray());
        }

        /// <summary>
        /// MLIQ (Liquid/Water details)
        /// Optional.  If HasWater flag
        /// </summary>
        private List<byte> GenerateMLIQChunk(Zone zone)
        {
            List<byte> chunkBytes = new List<byte>();

            Logger.WriteLine("MLIQ Generation unimplemented!");

            return WrapInChunk("MLIQ", chunkBytes.ToArray());
        }
    }
}