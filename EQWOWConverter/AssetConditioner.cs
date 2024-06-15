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
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EQWOWConverter.Common;
using EQWOWConverter.WOWFiles;
using EQWOWConverter.Zones;
using static System.Net.Mime.MediaTypeNames;
using System.Xml.Linq;
using EQWOWConverter.EQFiles;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace EQWOWConverter
{
    internal class AssetConditioner
    {
        public AssetConditioner() { }

        private static uint objectMeshesCondensed = 0;
        private static uint objectMaterialsCondensed = 0;
        private static uint objectTexturesCondensed = 0;
        private static uint transparentTexturesGenerated = 0;

        public bool ConditionEQOutput(string eqExportsRawPath, string eqExportsCondensedPath)
        {
            // Reset counters
            objectMeshesCondensed = 0;
            objectMaterialsCondensed = 0;
            objectTexturesCondensed = 0;
            transparentTexturesGenerated = 0;

            // Make sure the raw path exists
            if (Directory.Exists(eqExportsRawPath) == false)
            {
                Logger.WriteError("Error - Raw input path of '" + eqExportsRawPath + "' does not exist.");
                Logger.WriteError("Conditioning Failed!");
                return false;
            }

            // Create base folder
            FileTool.CreateBlankDirectory(eqExportsCondensedPath, false);

            // Delete/Recreate the output folders
            string outputObjectsFolderRoot = Path.Combine(eqExportsCondensedPath, "objects");
            string outputObjectsTexturesFolderRoot = Path.Combine(outputObjectsFolderRoot, "textures");
            string outputObjectsMeshesFolderRoot = Path.Combine(outputObjectsFolderRoot, "meshes");
            string outputObjectsMaterialsFolderRoot = Path.Combine(outputObjectsFolderRoot, "materiallists");
            string outputCharactersFolderRoot = Path.Combine(eqExportsCondensedPath, "characters");
            string outputSoundsFolderRoot = Path.Combine(eqExportsCondensedPath, "sounds");
            string outputMusicFolderRoot = Path.Combine(eqExportsCondensedPath, "music");
            string outputEquipmentFolderRoot = Path.Combine(eqExportsCondensedPath, "equipment");
            string outputMiscImagesFolderRoot = Path.Combine(eqExportsCondensedPath, "miscimages");
            string outputZoneFolderRoot = Path.Combine(eqExportsCondensedPath, "zones");
            string tempFolderRoot = Path.Combine(eqExportsCondensedPath, "temp");
            FileTool.CreateBlankDirectory(outputObjectsFolderRoot, false);
            FileTool.CreateBlankDirectory(outputObjectsTexturesFolderRoot, false);
            FileTool.CreateBlankDirectory(outputObjectsMeshesFolderRoot, false);
            FileTool.CreateBlankDirectory(outputObjectsMaterialsFolderRoot, false);
            FileTool.CreateBlankDirectory(outputCharactersFolderRoot, false);
            FileTool.CreateBlankDirectory(outputSoundsFolderRoot, false);
            FileTool.CreateBlankDirectory(outputMusicFolderRoot, false);
            FileTool.CreateBlankDirectory(outputEquipmentFolderRoot, false);
            FileTool.CreateBlankDirectory(outputMiscImagesFolderRoot, false);
            FileTool.CreateBlankDirectory(outputZoneFolderRoot, false);
            FileTool.CreateBlankDirectory(tempFolderRoot, false);

            // Iterate through each exported directory and process objects and zones
            string[] topDirectories = Directory.GetDirectories(eqExportsRawPath);
            foreach (string topDirectory in topDirectories)
            {
                // Get just the folder name itself for later
                string topDirectoryFolderNameOnly = topDirectory.Split('\\').Last();

                // Skip any disabled expansions
                if (Configuration.CONFIG_GENERATE_KUNARK_ZONES == false && Configuration.CONFIG_LOOKUP_KUNARK_ZONE_SHORTNAMES.Contains(topDirectoryFolderNameOnly))
                    continue;
                if (Configuration.CONFIG_GENERATE_VELIOUS_ZONES == false && Configuration.CONFIG_LOOKUP_VELIOUS_ZONE_SHORTNAMES.Contains(topDirectoryFolderNameOnly))
                    continue;

                // Bring in the objects of this directory
                FileTool.CopyDirectoryAndContents(topDirectory, tempFolderRoot, true, true);

                // If it's the character, music, equipment, or sound folder then copy it as-is
                if (topDirectoryFolderNameOnly == "characters" || topDirectoryFolderNameOnly == "sounds" || topDirectoryFolderNameOnly == "music" || topDirectoryFolderNameOnly == "equipment")
                {
                    Logger.WriteDetail("- [" + topDirectoryFolderNameOnly + "] Copying special folder containing these objects");
                    string outputFolder = Path.Combine(eqExportsCondensedPath, topDirectoryFolderNameOnly);
                    FileTool.CopyDirectoryAndContents(tempFolderRoot, outputFolder, true, true);
                    continue;
                }

                // If it's a bmpwad (contains misc images) then copy that into the miscimages folder
                if (topDirectoryFolderNameOnly.StartsWith("bmpwad"))
                {
                    Logger.WriteDetail("- [" + topDirectoryFolderNameOnly + "] Copying special folder containing these objects, and resizing loading screens");
                    string outputMiscImagesFolder = Path.Combine(eqExportsCondensedPath, "miscimages");
                    FileTool.CopyDirectoryAndContents(tempFolderRoot, outputMiscImagesFolder, false, true);

                    // Resize the loading screens
                    if (topDirectoryFolderNameOnly == "bmpwad")
                    {
                        string inputLoadingScreenFileName = Path.Combine(tempFolderRoot, "logo03.png");
                        string outputLoadingScreenFileName = Path.Combine(outputMiscImagesFolder, "logo03resized.png");
                        GenerateResizedImage(inputLoadingScreenFileName, outputLoadingScreenFileName, 1024, 1024);
                    }
                    else if (topDirectoryFolderNameOnly == "bmpwad4")
                    {
                        string inputLoadingScreenFileName = Path.Combine(tempFolderRoot, "eqkload.png");
                        string outputLoadingScreenFileName = Path.Combine(outputMiscImagesFolder, "eqkloadresized.png");
                        GenerateResizedImage(inputLoadingScreenFileName, outputLoadingScreenFileName, 1024, 1024);
                    }
                    else if (topDirectoryFolderNameOnly == "bmpwad5")
                    {
                        string inputLoadingScreenFileName = Path.Combine(tempFolderRoot, "eqvload.png");
                        string outputLoadingScreenFileName = Path.Combine(outputMiscImagesFolder, "eqvloadresized.png");
                        GenerateResizedImage(inputLoadingScreenFileName, outputLoadingScreenFileName, 1024, 1024);
                    }
                    continue;
                }

                // Determine what objects exist in this folder
                string tempZoneFolder = Path.Combine(tempFolderRoot, "Zone");
                string tempObjectsFolder = Path.Combine(tempFolderRoot, "Objects");
                bool topDirectoryHasZone = Directory.Exists(tempZoneFolder);
                bool topDirectoryHasObjects = Directory.Exists(tempObjectsFolder);

                // Process objects
                if (topDirectoryHasObjects)
                {
                    // Process the object textures
                    ProcessAndCopyObjectTextures(topDirectoryFolderNameOnly, tempObjectsFolder, outputObjectsTexturesFolderRoot);

                    // Process the object materials
                    ProcessAndCopyObjectMaterials(topDirectoryFolderNameOnly, tempObjectsFolder, outputObjectsMaterialsFolderRoot);

                    // Process the object meshes
                    ProcessAndCopyObjectMeshes(topDirectoryFolderNameOnly, tempObjectsFolder, tempZoneFolder, outputObjectsMeshesFolderRoot);
                }

                // Process Zones
                if (topDirectoryHasZone)
                {
                    // Copy the core zone folder
                    string outputZoneFolder = Path.Combine(eqExportsCondensedPath, "zones", topDirectoryFolderNameOnly);
                    FileTool.CopyDirectoryAndContents(tempZoneFolder, outputZoneFolder, true, true);

                    // Bring over any object vertex colors
                    string tempZoneObjectVertexColorFolder = Path.Combine(tempObjectsFolder, "VertexColors");
                    if (Directory.Exists(tempZoneObjectVertexColorFolder))
                    {
                        string outputZoneObjectVertexColorFolder = Path.Combine(outputZoneFolder, "StaticObjectVertexColors");
                        FileTool.CopyDirectoryAndContents(tempZoneObjectVertexColorFolder, outputZoneObjectVertexColorFolder, true, true);
                    }

                    // Copy files that were missing in the original folders for some reason
                    if (topDirectoryFolderNameOnly == "fearplane")
                    {
                        Logger.WriteDetail("- [" + topDirectoryFolderNameOnly + "] Copying texture file 'maywall' not found in the original zone folder...");
                        string inputFileName = Path.Combine(tempObjectsFolder, "Textures", "maywall.png");
                        string outputFileName = Path.Combine(outputZoneFolder, "Textures", "maywall.png");
                        File.Copy(inputFileName, outputFileName, true);
                    }
                    else if (topDirectoryFolderNameOnly == "oasis")
                    {
                        Logger.WriteDetail("- [" + topDirectoryFolderNameOnly + "] Copying texture file 'canwall1' not found in the original zone folder...");
                        string inputFileName = Path.Combine(tempObjectsFolder, "Textures", "canwall1.png");
                        string outputFileName = Path.Combine(outputZoneFolder, "Textures", "canwall1.png");
                        File.Copy(inputFileName, outputFileName, true);
                    }
                    else if (topDirectoryFolderNameOnly == "swampofnohope")
                    {
                        Logger.WriteDetail("- [" + topDirectoryFolderNameOnly + "] Copying texture file 'kruphse3' not found in the original zone folder...");
                        string inputFileName = Path.Combine(tempObjectsFolder, "Textures", "kruphse3.png");
                        string outputFileName = Path.Combine(outputZoneFolder, "Textures", "kruphse3.png");
                        File.Copy(inputFileName, outputFileName, true);
                    }
                }
            }

            // Clean up the temp folder
            Directory.Delete(tempFolderRoot, true);

            // Save the texture coordinates in the material lists
            Logger.WriteInfo("Saving texture sizes in material lists...");
            topDirectories = Directory.GetDirectories(eqExportsCondensedPath);
            foreach (string topDirectory in topDirectories)
            {
                // Get just the folder name itself for later
                string topDirectoryFolderNameOnly = topDirectory.Split('\\').Last();

                // Different logic based on type of folder
                if (topDirectoryFolderNameOnly == "zones")
                {
                    string[] zoneDirectories = Directory.GetDirectories(topDirectory);
                    foreach(string zoneDirectory in zoneDirectories)
                    {
                        // Get just the zone folder
                        string zoneDirectoryFolderNameOnly = topDirectory.Split('\\').Last();
                        SaveTextureCoordinatesInMaterialLists(zoneDirectoryFolderNameOnly, zoneDirectory);                    
                    }
                }
                else if (topDirectoryFolderNameOnly == "characters" || topDirectoryFolderNameOnly == "objects" || topDirectoryFolderNameOnly == "equipment")
                {
                    SaveTextureCoordinatesInMaterialLists(topDirectoryFolderNameOnly, topDirectory);
                }
            }

            // Generate any needed transparent textures
            Logger.WriteInfo("Generating transparent textures...");
            topDirectories = Directory.GetDirectories(eqExportsCondensedPath);
            foreach (string topDirectory in topDirectories)
            {
                // Get just the folder name itself for later
                string topDirectoryFolderNameOnly = topDirectory.Split('\\').Last();

                // Different logic based on type of folder
                if (topDirectoryFolderNameOnly == "zones")
                {
                    string[] zoneDirectories = Directory.GetDirectories(topDirectory);
                    foreach (string zoneDirectory in zoneDirectories)
                    {
                        // Get just the zone folder
                        string zoneDirectoryFolderNameOnly = topDirectory.Split('\\').Last();
                        GenerateTransparentImagesForMaterials(zoneDirectory);
                    }
                }
                else if (topDirectoryFolderNameOnly == "characters" || topDirectoryFolderNameOnly == "objects" || topDirectoryFolderNameOnly == "equipment")
                {
                    GenerateTransparentImagesForMaterials(topDirectory);
                }
            }

            Logger.WriteInfo("Conditioning complete. Totals:");
            Logger.WriteInfo(" - Object Meshes condensed: " + objectMeshesCondensed);
            Logger.WriteInfo(" - Object Textures condensed: " + objectTexturesCondensed);
            Logger.WriteInfo(" - Object Materials condensed: " + objectMaterialsCondensed);
            Logger.WriteInfo(" - Transparent Textures generated: " + transparentTexturesGenerated);
            return true;
        }

        // TODO: Look for solution to image operations.  These image methods are why this project is windows only.
        private void GenerateResizedImage(string inputFilePath, string outputFilePath, int newWidth, int newHeight)
        {
            // Resize the image to the passed parameters
            Bitmap inputImage = new Bitmap(inputFilePath);
            Bitmap outputImage = new Bitmap(newWidth, newHeight);
            outputImage.SetResolution(outputImage.HorizontalResolution, outputImage.VerticalResolution);
            using (var graphics = Graphics.FromImage(outputImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                using (ImageAttributes wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    Rectangle outputRectangle = new Rectangle(0, 0, newWidth, newHeight);
                    graphics.DrawImage(inputImage, outputRectangle, 0, 0, inputImage.Width, inputImage.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }
            outputImage.Save(outputFilePath);
            outputImage.Dispose();
            inputImage.Dispose();
        }

        private void GenerateTransparentImagesForMaterials(string workingRootFolderPath)
        {
            string materialListFolder = Path.Combine(workingRootFolderPath, "MaterialLists");
            string textureFolder = Path.Combine(workingRootFolderPath, "Textures");
            string[] materialListFiles = Directory.GetFiles(materialListFolder);
            foreach (string materialListFile in materialListFiles)
            {
                bool doWriteOutMaterialListFile = false;
                string materialFileText = File.ReadAllText(materialListFile);
                string[] materialFileRows = materialFileText.Split(Environment.NewLine);
                string[] materialFileRowsForWrite = materialFileText.Split(Environment.NewLine);
                EQMaterialList materialListData = new EQMaterialList();
                materialListData.LoadFromDisk(materialListFile);
                int rowIndex = -1;
                foreach (string materialFileRow in materialFileRows)
                {
                    rowIndex++;
                    // Skip blank lines and comments
                    if (materialFileRow.Length == 0 || materialFileRow.StartsWith("#"))
                        continue;

                    // Grab material details
                    string[] blocks = materialFileRow.Split(",");
                    UInt32 materialIndex = UInt32.Parse(blocks[0]);
                    Material curMaterial = materialListData.Materials[Convert.ToInt32(materialIndex)];

                    // Generate transparent versions as required
                    if (curMaterial.MaterialType == MaterialType.Transparent25Percent || curMaterial.MaterialType == MaterialType.Transparent50Percent || curMaterial.MaterialType == MaterialType.Transparent75Percent)
                    {
                        if (curMaterial.TextureNames.Count > 0)
                        {
                            for (int i = 0; i < curMaterial.TextureNames.Count; ++i)
                            {
                                // Generate a new texture name
                                string newTextureName = curMaterial.TextureNames[i] + curMaterial.GetTextureSuffix();

                                // Check if texture already exists and if not, create it
                                string existingTextureFullPath = Path.Combine(textureFolder, curMaterial.TextureNames[i] + ".png");
                                string newTextureFullPath = Path.Combine(textureFolder, newTextureName + ".png");
                                if (File.Exists(newTextureFullPath) == false)
                                {
                                    GenerateTransparentImage(existingTextureFullPath, newTextureFullPath, curMaterial.MaterialType);
                                    Logger.WriteDetail("Generated a transparent texture with full path '" + newTextureFullPath + "'");
                                }

                                // Update texture references for the material
                                materialFileRowsForWrite[rowIndex] = materialFileRowsForWrite[rowIndex].Replace(curMaterial.TextureNames[i], newTextureName);
                                doWriteOutMaterialListFile = true;
                                curMaterial.TextureNames[i] = newTextureName;
                            }
                        }
                    }
                }
                if (doWriteOutMaterialListFile == true)
                {
                    StringBuilder newMaterialListFileContents = new StringBuilder();
                    foreach (string materialFileRow in materialFileRowsForWrite)
                        newMaterialListFileContents.AppendLine(materialFileRow);
                    File.WriteAllText(materialListFile, newMaterialListFileContents.ToString());
                }
            }
        }

        private bool GenerateTransparentImage(string inputFilePath, string outputFilePath, MaterialType materialType)
        {
            // Calculate the new alpha value
            double newPixelAlphaMultiplier;
            switch (materialType)
            {
                case MaterialType.Transparent25Percent: newPixelAlphaMultiplier = 0.75; break;
                case MaterialType.Transparent50Percent: newPixelAlphaMultiplier = 0.50;break;
                case MaterialType.Transparent75Percent: newPixelAlphaMultiplier = 0.25;break;
                default:
                    {
                        Logger.WriteError("GenerateTransparentImage Error.  Passed image of '" + inputFilePath + "' has material type of '" + materialType.ToString() + "'");
                        return false;
                    }
            }

            // Edit the alpha pixel value
            Bitmap inputImage = new Bitmap(inputFilePath);
            for (int x = 0; x < inputImage.Width; x++)
            {
                for (int y = 0; y < inputImage.Height; y++)
                {
                    Color curPixelColor = inputImage.GetPixel(x, y);
                    byte newAlpha = Convert.ToByte(Math.Round(Convert.ToDouble(curPixelColor.A) * newPixelAlphaMultiplier, 0, MidpointRounding.ToZero));
                    Color newPixelColor = Color.FromArgb(newAlpha, curPixelColor.R, curPixelColor.G, curPixelColor.B);
                    inputImage.SetPixel(x, y, newPixelColor);
                }
            }

            // Save the new image
            inputImage.Save(outputFilePath);
            inputImage.Dispose();
            transparentTexturesGenerated++;
            return true;
        }

        private void SaveTextureCoordinatesInMaterialLists(string topFolderName, string workingRootFolderPath)
        {
            string materialListFolder = Path.Combine(workingRootFolderPath, "MaterialLists");
            string textureFolder = Path.Combine(workingRootFolderPath, "Textures");
            string[] materialListFiles = Directory.GetFiles(materialListFolder);
            foreach (string materialListFile in materialListFiles)
            {
                string materialFileText = File.ReadAllText(materialListFile);
                string[] materialFileRows = materialFileText.Split(Environment.NewLine);
                string[] materialFileRowsForWrite = materialFileText.Split(Environment.NewLine);
                EQMaterialList materialListData = new EQMaterialList();
                materialListData.LoadFromDisk(materialListFile);
                int rowIndex = -1;
                foreach (string materialFileRow in materialFileRows)
                {
                    rowIndex++;
                    // Skip blank lines and comments
                    if (materialFileRow.Length == 0 || materialFileRow.StartsWith("#"))
                        continue;

                    // Grab material details
                    string[] blocks = materialFileRow.Split(",");
                    UInt32 materialIndex = UInt32.Parse(blocks[0]);
                    Material curMaterial = materialListData.Materials[Convert.ToInt32(materialIndex)];

                    // Get texture dimensions, if there is one
                    if (curMaterial.TextureNames.Count > 0)
                    {
                        string textureFullPath = Path.Combine(textureFolder, curMaterial.TextureNames[0] + ".png");
                        Bitmap textureinputImage = new Bitmap(textureFullPath);
                        int imageHeight = textureinputImage.Height;
                        int imageWidth = textureinputImage.Width;
                        textureinputImage.Dispose();

                        // Throw an error if this texture isn't a power of 2
                        HashSet<int> validDimensions = new HashSet<int>() { 1, 2, 4, 8, 16, 32, 64, 128, 256, 512, 1024 };
                        if (validDimensions.Contains(imageHeight) == false)
                            Logger.WriteError("Texture '" + textureFullPath + "' height is invalid with value '" + imageHeight + "', it must be a power of 2 and <= 1024");
                        if (validDimensions.Contains(imageWidth) == false)
                            Logger.WriteError("Texture '" + textureFullPath + "' width is invalid with value '" + imageWidth + "', it must be a power of 2 and <= 1024");

                        // Write this to the end of the row
                        materialFileRowsForWrite[rowIndex] = materialFileRow + "," + imageWidth.ToString() + "," + imageHeight.ToString();
                    }
                }

                // Update the material file
                StringBuilder newMaterialListFileContents = new StringBuilder();
                foreach (string materialFileRow in materialFileRowsForWrite)
                    newMaterialListFileContents.AppendLine(materialFileRow);
                File.WriteAllText(materialListFile, newMaterialListFileContents.ToString());
            }
        }

        private void ProcessAndCopyObjectTextures(string topDirectory, string tempObjectsFolder, string outputObjectsTexturesFolderRoot)
        {
            // Look for texture collisions for different texture files
            string tempObjectTextureFolderName = Path.Combine(tempObjectsFolder, "Textures");
            string tempMaterialsFolderName = Path.Combine(tempObjectsFolder, "MaterialLists");
            string[] objectTexturesFiles = Directory.GetFiles(tempObjectTextureFolderName);
            foreach (string objectTextureFile in objectTexturesFiles)
            {
                // Calculate the full paths for comparison
                string objectTextureFileNameOnly = Path.GetFileName(objectTextureFile);
                string originalObjectTextureFileNameOnlyNoExtension = Path.GetFileNameWithoutExtension(objectTextureFile);
                string sourceObjectTextureFile = Path.Combine(tempObjectTextureFolderName, objectTextureFileNameOnly);
                string targetObjectTextureFile = Path.Combine(outputObjectsTexturesFolderRoot, objectTextureFileNameOnly);

                // Loop until there is no unresolved file collision
                bool doesUnresolvedFileCollisionExist;
                uint altIteration = 1;
                do
                {
                    // Compare the files if the destination file already exist
                    doesUnresolvedFileCollisionExist = false;
                    if (File.Exists(targetObjectTextureFile) == true)
                    {
                        // If the files collide but are not the exact same, create a new version
                        if (FileTool.AreFilesTheSame(targetObjectTextureFile, sourceObjectTextureFile) == false)
                        {
                            // Update the file name
                            string newObjectTextureFileNameOnly = originalObjectTextureFileNameOnlyNoExtension + "alt" + altIteration.ToString() + Path.GetExtension(objectTextureFile);
                            altIteration++;
                            Logger.WriteDetail("- [" + topDirectory + "] Object Texture Collision with name '" + objectTextureFileNameOnly + "' but different contents so renaming to '" + newObjectTextureFileNameOnly + "'");
                            File.Move(sourceObjectTextureFile, Path.Combine(tempObjectTextureFolderName, newObjectTextureFileNameOnly));

                            // Update texture references in material files
                            string[] objectMaterialFiles = Directory.GetFiles(tempMaterialsFolderName);
                            foreach (string objectMaterialFile in objectMaterialFiles)
                            {
                                EQMaterialList eqMaterialList = new EQMaterialList();
                                eqMaterialList.LoadFromDisk(objectMaterialFile);
                                bool materialHasTextureName = false;
                                string objectTextureFileNameNoExtension = Path.GetFileNameWithoutExtension(objectTextureFileNameOnly);
                                foreach (Material material in eqMaterialList.Materials)
                                {
                                    if (material.UniqueName == objectTextureFileNameNoExtension)
                                        materialHasTextureName = true;
                                    break;
                                }
                                if (materialHasTextureName)
                                {
                                    string fileText = File.ReadAllText(objectMaterialFile);
                                    if (fileText.Contains(":" + objectTextureFileNameNoExtension))
                                    {
                                        string newObjectTextureFileNameNoExtension = Path.GetFileNameWithoutExtension(newObjectTextureFileNameOnly);
                                        Logger.WriteDetail("- [" + topDirectory + "] Object material file '" + objectMaterialFile + "' contained texture '" + objectTextureFileNameNoExtension + "' which was renamed to '" + newObjectTextureFileNameNoExtension + "'. Updating material file...");
                                        fileText = fileText.Replace(":" + objectTextureFileNameNoExtension, ":" + newObjectTextureFileNameNoExtension);
                                        File.WriteAllText(objectMaterialFile, fileText);
                                    }
                                }
                            }

                            // Continue loop using this new file name as a base
                            objectTextureFileNameOnly = newObjectTextureFileNameOnly;
                            sourceObjectTextureFile = Path.Combine(tempObjectTextureFolderName, objectTextureFileNameOnly);
                            targetObjectTextureFile = Path.Combine(outputObjectsTexturesFolderRoot, objectTextureFileNameOnly);
                            doesUnresolvedFileCollisionExist = true;
                        }
                        else
                        {
                            objectTexturesCondensed++;
                        }
                    }
                }
                while (doesUnresolvedFileCollisionExist);

                // Copy the file
                File.Copy(sourceObjectTextureFile, targetObjectTextureFile, true);
            }
        }

        // Bug: This is over-naming material names.  See \MaterialLists\freportw material "t50_wla50".  Liquid is using this wrong name, so
        // review liquidproperties if/when this bug is fixed.
        private void ProcessAndCopyObjectMaterials(string topDirectory, string tempObjectsFolder, string outputObjectsMaterialsFolderRoot)
        {
            // Look for material collisions for different material files
            string tempObjectMaterialsFolderName = Path.Combine(tempObjectsFolder, "MaterialLists");
            string tempObjectMeshesFolderName = Path.Combine(tempObjectsFolder, "Meshes");            
            string[] objectMaterialFiles = Directory.GetFiles(tempObjectMaterialsFolderName);
            foreach (string objectMaterialFile in objectMaterialFiles)
            {
                // Calculate the full paths for comparison
                string objectMaterialFileNameOnly = Path.GetFileName(objectMaterialFile);
                string originalObjectMaterialFileNameOnlyNoExtension = Path.GetFileNameWithoutExtension(objectMaterialFile);
                string sourceObjectMaterialFile = Path.Combine(tempObjectMaterialsFolderName, objectMaterialFileNameOnly);
                string targetObjectMaterialFile = Path.Combine(outputObjectsMaterialsFolderRoot, objectMaterialFileNameOnly);

                // Loop until there is no unresolved file collision
                bool doesUnresolvedFileCollisionExist;
                uint altIteration = 1;
                do
                {
                    // Compare the files if the destination file already exist
                    doesUnresolvedFileCollisionExist = false;
                    if (File.Exists(targetObjectMaterialFile) == true)
                    {
                        // If the files collide but are not the exact same, create a new version
                        if (FileTool.AreFilesTheSame(targetObjectMaterialFile, sourceObjectMaterialFile) == false)
                        {
                            // Update the file name
                            string newObjectMaterialFileNameOnly = originalObjectMaterialFileNameOnlyNoExtension + "alt" + altIteration.ToString() + Path.GetExtension(objectMaterialFile);
                            altIteration++;
                            Logger.WriteDetail("- [" + topDirectory + "] Object Material Collision with name '" + objectMaterialFileNameOnly + "' but different contents so renaming to '" + newObjectMaterialFileNameOnly + "'");
                            File.Move(sourceObjectMaterialFile, Path.Combine(tempObjectMaterialsFolderName, newObjectMaterialFileNameOnly));

                            // Update material references in mesh files
                            string[] objectMeshFiles = Directory.GetFiles(tempObjectMeshesFolderName);
                            foreach (string objectMeshFile in objectMeshFiles)
                            {
                                EQMesh curMesh = new EQMesh();
                                curMesh.LoadFromDisk(objectMeshFile);
                                string objectMaterialFileNameNoExtension = Path.GetFileNameWithoutExtension(objectMaterialFileNameOnly);
                                if (curMesh.MaterialListFileName == objectMaterialFileNameNoExtension)
                                {
                                    string fileText = File.ReadAllText(objectMeshFile);
                                    string newObjectMaterialFileNameNoExtension = Path.GetFileNameWithoutExtension(newObjectMaterialFileNameOnly);
                                    Logger.WriteDetail("- [" + topDirectory + "] Object mesh file '" + objectMeshFile + "' contained material '" + objectMaterialFileNameNoExtension + "' which was renamed to '" + newObjectMaterialFileNameNoExtension + "'. Updating mesh file...");
                                    fileText = fileText.Replace("," + objectMaterialFileNameNoExtension, "," + newObjectMaterialFileNameNoExtension);
                                    File.WriteAllText(objectMeshFile, fileText);
                                }
                            }

                            // Continue loop using this new file name as a base
                            objectMaterialFileNameOnly = newObjectMaterialFileNameOnly;
                            sourceObjectMaterialFile = Path.Combine(tempObjectMaterialsFolderName, objectMaterialFileNameOnly);
                            targetObjectMaterialFile = Path.Combine(outputObjectsMaterialsFolderRoot, objectMaterialFileNameOnly);
                            doesUnresolvedFileCollisionExist = true;
                        }
                        else
                        {
                            objectMaterialsCondensed++;
                        }
                    }
                }
                while (doesUnresolvedFileCollisionExist);

                // Copy the file
                File.Copy(sourceObjectMaterialFile, targetObjectMaterialFile, true);
            }
        }

        private void ProcessAndCopyObjectMeshes(string topDirectory, string tempObjectsFolder, string tempZoneFolder, string outputObjectsMeshesFolderRoot)
        {
            // Look for mesh collisions for different mesh files
            string tempObjectMeshesFolderName = Path.Combine(tempObjectsFolder, "Meshes");
            string[] objectMeshFiles = Directory.GetFiles(tempObjectMeshesFolderName);
            foreach (string objectMeshFile in objectMeshFiles)
            {
                // Skip collision meshes
                if (objectMeshFile.Contains("_collision.txt"))
                    continue;

                // Calculate the full paths for comparison
                string objectMeshFileNameOnly = Path.GetFileName(objectMeshFile);
                string originalObjectMeshFileNameOnlyNoExtension = Path.GetFileNameWithoutExtension(objectMeshFile);
                string sourceObjectMeshFile = Path.Combine(tempObjectMeshesFolderName, objectMeshFileNameOnly);
                string targetObjectMeshFile = Path.Combine(outputObjectsMeshesFolderRoot, objectMeshFileNameOnly);

                // Loop until there is no unresolved file collision
                bool doesUnresolvedFileCollisionExist;
                uint altIteration = 1;
                do
                {
                    // Compare the files if the destination file already exist
                    doesUnresolvedFileCollisionExist = false;
                    if (File.Exists(targetObjectMeshFile) == true)
                    {
                        // If the files collide but are not the exact same, create a new version
                        if (FileTool.AreFilesTheSame(targetObjectMeshFile, sourceObjectMeshFile) == false)
                        {
                            // Update the file name
                            string newObjectMeshFileNameOnly = originalObjectMeshFileNameOnlyNoExtension + "alt" + altIteration.ToString() + Path.GetExtension(objectMeshFile);
                            altIteration++;
                            Logger.WriteDetail("- [" + topDirectory + "] Object Mesh Collision with name '" + objectMeshFileNameOnly + "' but different contents so renaming to '" + newObjectMeshFileNameOnly + "'");
                            File.Move(sourceObjectMeshFile, Path.Combine(tempObjectMeshesFolderName, newObjectMeshFileNameOnly));

                            // Also update the collision file, if there was one
                            string collisionMeshFileName = Path.GetFileNameWithoutExtension(objectMeshFileNameOnly) + "_collision.txt";
                            string collisionMeshFilePath = Path.Combine(tempObjectMeshesFolderName, collisionMeshFileName);
                            if (File.Exists(collisionMeshFilePath))
                            {
                                string newCollisionMeshFilePath = Path.Combine(tempObjectMeshesFolderName, originalObjectMeshFileNameOnlyNoExtension + "alt" + altIteration.ToString() + "_collision.txt");
                                Logger.WriteDetail("- [" + topDirectory + "] Object Mesh also had a collision mesh with name '" + collisionMeshFileName + "', so changing that as well");
                                File.Move(collisionMeshFilePath, newCollisionMeshFilePath);
                            }

                            // Update mesh references in zone object instances file
                            string zoneObjectInstancesFile = Path.Combine(tempZoneFolder, "object_instances.txt");
                            if (File.Exists(zoneObjectInstancesFile) == false)
                            {
                                Logger.WriteDetail("- [" + topDirectory + "] No object_instances file to update");
                            }
                            else
                            {
                                EQObjectInstances eqObjectInstances = new EQObjectInstances();
                                if (eqObjectInstances.LoadFromDisk(zoneObjectInstancesFile) == false)
                                { 
                                    Logger.WriteError("- [" + topDirectory + "] Issue loading object instances file '" + zoneObjectInstancesFile + "'");
                                }
                                else
                                {
                                    bool meshFoundInFile = false;
                                    string objectMeshFileNameNoExtension = Path.GetFileNameWithoutExtension(objectMeshFileNameOnly);
                                    foreach (ObjectInstance objectInstance in eqObjectInstances.ObjectInstances)
                                    {
                                        if (objectInstance.ModelName == objectMeshFileNameNoExtension)
                                        {
                                            meshFoundInFile = true;
                                            break;
                                        }
                                    }
                                    if (meshFoundInFile == true)
                                    {
                                        string fileText = File.ReadAllText(zoneObjectInstancesFile);
                                        if (fileText.Contains(objectMeshFileNameNoExtension + ","))
                                        {
                                            string newObjectMeshFileNameNoExtension = Path.GetFileNameWithoutExtension(newObjectMeshFileNameOnly);
                                            Logger.WriteDetail("- [" + topDirectory + "] Zone object_instances file '" + zoneObjectInstancesFile + "' contained mesh '" + objectMeshFileNameNoExtension + "' which was renamed to '" + newObjectMeshFileNameNoExtension + "'. Updating object_instances file...");
                                            fileText = fileText.Replace(objectMeshFileNameNoExtension + ",", newObjectMeshFileNameNoExtension + ",");
                                            File.WriteAllText(zoneObjectInstancesFile, fileText);
                                        }
                                    }
                                }
                            }

                            // Continue loop using this new file name as a base
                            objectMeshFileNameOnly = newObjectMeshFileNameOnly;
                            sourceObjectMeshFile = Path.Combine(tempObjectMeshesFolderName, objectMeshFileNameOnly);
                            targetObjectMeshFile = Path.Combine(outputObjectsMeshesFolderRoot, objectMeshFileNameOnly);
                            doesUnresolvedFileCollisionExist = true;
                        }
                        else
                        {
                            objectMeshesCondensed++;
                        }
                    }
                }
                while (doesUnresolvedFileCollisionExist);

                // Copy the files
                File.Copy(sourceObjectMeshFile, targetObjectMeshFile, true);
                string collisionMeshFileNameOnly = Path.GetFileNameWithoutExtension(sourceObjectMeshFile) + "_collision.txt";
                string sourceCollisionMeshFile = Path.Combine(tempObjectMeshesFolderName, collisionMeshFileNameOnly);
                if (File.Exists(sourceCollisionMeshFile))
                {
                    string targetCollisionMeshFile = Path.Combine(outputObjectsMeshesFolderRoot, collisionMeshFileNameOnly);
                    File.Copy(sourceCollisionMeshFile, targetCollisionMeshFile, true);
                }
            }
        }
    }
}
