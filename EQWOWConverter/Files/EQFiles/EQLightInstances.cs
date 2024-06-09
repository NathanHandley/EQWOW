﻿using EQWOWConverter.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EQWOWConverter.EQFiles
{
    internal class EQLightInstances
    {
        public List<LightInstance> LightInstances = new List<LightInstance>();

        public bool LoadFromDisk(string fileFullPath)
        {
            Logger.WriteDetail(" - Reading EQ Light Instances Data from '" + fileFullPath + "'...");
            if (File.Exists(fileFullPath) == false)
            {
                Logger.WriteError("- Could not find light instances file that should be at '" + fileFullPath + "'");
                return false;
            }

            // Load the data
            string inputData = File.ReadAllText(fileFullPath);
            string[] inputRows = inputData.Split(Environment.NewLine);
            foreach (string inputRow in inputRows)
            {
                // Nothing for blank lines
                if (inputRow.Length == 0)
                    continue;

                // # = comment
                else if (inputRow.StartsWith("#"))
                    continue;

                // 7-blocks is a light instance
                else
                {
                    string[] blocks = inputRow.Split(",");
                    if (blocks.Length != 7)
                    {
                        Logger.WriteError("- Light instance data is 7 components");
                        continue;
                    }
                    LightInstance newLightInstance = new LightInstance();
                    newLightInstance.Position.X = float.Parse(blocks[0]);
                    newLightInstance.Position.Y = float.Parse(blocks[1]);
                    newLightInstance.Position.Z = float.Parse(blocks[2]);
                    newLightInstance.Radius = float.Parse(blocks[3]);
                    newLightInstance.ColorRed = float.Parse(blocks[4]);
                    newLightInstance.ColorGreen = float.Parse(blocks[5]);
                    newLightInstance.ColorBlue = float.Parse(blocks[6]);
                    LightInstances.Add(newLightInstance);
                }
            }

            Logger.WriteDetail(" - Done reading EQ Light Instances from '" + fileFullPath + "'");
            return true;
        }
    }
}
