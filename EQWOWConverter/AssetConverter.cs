﻿using System;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using EQWOWConverter.EQObjects;
using EQWOWConverter.Common;
using Vector3 = EQWOWConverter.Common.Vector3;

namespace EQWOWConverter
{
    internal class AssetConverter
    {
        public static bool ConvertEQZonesToWOW(string eqExportsCondensedPath)
        {
            // TODO: Move this to a config
            UInt32 curWMOID = 7000; // Reserving 7000-7200

            Logger.WriteLine("Converting EQ zones to WOW zones...");

            // Make sure the root path exists
            if (Directory.Exists(eqExportsCondensedPath) == false)
            {
                Logger.WriteLine("ERROR - Condensed path of '" + eqExportsCondensedPath + "' does not exist.");
                Logger.WriteLine("Conversion Failed!");
                return false;
            }

            // Make sure the zone folder path exists
            string zoneFolderRoot = Path.Combine(eqExportsCondensedPath, "zones");
            if (Directory.Exists(zoneFolderRoot) == false)
            {
                Logger.WriteLine("ERROR - Zone folder that should be at path '" + zoneFolderRoot + "' does not exist.");
                Logger.WriteLine("Conversion Failed!");
                return false;
            }

            // Go through the subfolders for each zone and convert to wow zone
            DirectoryInfo zoneRootDirectoryInfo = new DirectoryInfo(zoneFolderRoot);
            DirectoryInfo[] zoneDirectoryInfos = zoneRootDirectoryInfo.GetDirectories();
            foreach (DirectoryInfo zoneDirectory in zoneDirectoryInfos)
            {
                // Load the EQ zone
                string curZoneDirectory = Path.Combine(zoneFolderRoot, zoneDirectory.Name);
                Logger.WriteLine("- [" + zoneDirectory.Name + "]: Importing EQ zone '" + zoneDirectory.Name + "' at '" + curZoneDirectory);
                EQZone curZone = new EQZone(zoneDirectory.Name, curZoneDirectory, curWMOID);
                curWMOID++;
                Logger.WriteLine("- [" + zoneDirectory.Name + "]: Importing of EQ zone '" + zoneDirectory.Name + "' complete");

                // Convert to WOW zone
                CreateWoWZoneFromEQZone(curZone);
            }

            // Update the 
            Logger.WriteLine(" TODO: WMOAreaTable.dbc ");
            Logger.WriteLine(" TODO: AreaTable.dbc ");

            Logger.WriteLine("Conversion Successful");
            return true;
        }

        public static void CreateWoWZoneFromEQZone(EQZone zone)
        {
            Logger.WriteLine("- [" + zone.Name + "]: Converting zone '" + zone.Name + "' into a wow zone...");

            // Create the chunk byte blocks
            // MVER (Version) ---------------------------------------------------------------------
            UInt32 version = 17;
            List<byte> MVERChunkByteBlock = WrapInChunk("MVER", BitConverter.GetBytes(version));

            // MOHD (Header) ----------------------------------------------------------------------
            List<byte> MOHDBytes = new List<byte>();
            MOHDBytes.AddRange(BitConverter.GetBytes(zone.TextureCount));   // Number of Textures
            MOHDBytes.AddRange(BitConverter.GetBytes(Convert.ToUInt32(1))); // Number of Groups (always 1)
            MOHDBytes.AddRange(BitConverter.GetBytes(Convert.ToUInt32(0))); // Number of Portals (Zero for now, but may cause problems?)
            MOHDBytes.AddRange(BitConverter.GetBytes(Convert.ToUInt32(zone.LightInstances.Count()))); // Number of Lights
            MOHDBytes.AddRange(BitConverter.GetBytes(Convert.ToUInt32(0))); // Number of Models
            MOHDBytes.AddRange(BitConverter.GetBytes(Convert.ToUInt32(0))); // Number of Doodad Definitions
            MOHDBytes.AddRange(BitConverter.GetBytes(Convert.ToUInt32(0))); // Number of Doodad Sets
            MOHDBytes.AddRange(zone.AmbientLight.ToBytes());                // Ambiant Light
            MOHDBytes.AddRange(BitConverter.GetBytes(zone.WMOID));          // WMOID (inside WMOAreaTable.dbc)
            MOHDBytes.AddRange(zone.BoundingBox.ToBytes());                 // Axis aligned bounding box for the zone mesh(es)


            // Assemble the byte blocks

            Logger.WriteLine("- [" + zone.Name + "]: Converting of zone '" + zone.Name + "' complete");
        }

        private static List<byte> WrapInChunk(string token, byte[] dataBlock)
        {
            if (token.Length != 4)
                Logger.WriteLine("Error, WrapInChunk has a token that isn't a length of 4 (value = '" + token + "')");
            List<byte> wrappedChunk = new List<byte>();
            wrappedChunk.AddRange(Encoding.ASCII.GetBytes(token));
            wrappedChunk.AddRange(BitConverter.GetBytes(Convert.ToUInt32(dataBlock.Length)));
            wrappedChunk.AddRange(dataBlock);
            return wrappedChunk;
        }            
    }
}
