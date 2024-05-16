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
using EQWOWConverter.ModelObjects.WOW;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EQWOWConverter.ModelObjects
{
    internal class ModelBone
    {
        public Int32 KeyBoneID = -1;
        public ModelBoneFlags Flags = 0;
        public Int16 ParentBone = -1; // Why is this Int16 instead of Int32?
        public UInt16 SubMeshID = 0;
        public UInt32 BoneNameCRC = 3391571450;  // Took this from Sack01.  Figure out how to generate this
        public ModelTrackSequences<Vector3> TranslationTrack = new ModelTrackSequences<Vector3>();
        public ModelTrackSequences<Quaternion> RotationTrack = new ModelTrackSequences<Quaternion>();
        public ModelTrackSequences<Vector3> ScaleTrack = new ModelTrackSequences<Vector3>();
        public Vector3 PivotPoint = new Vector3();

        public ModelBone()
        {

        }
        
        public UInt32 GetHeaderSize()
        {
            UInt32 size = 0;
            size += 4; // KeyBoneID
            size += 4; // ModelBoneFlags
            size += 2; // ParentBone
            size += 2; // SubMeshID
            size += 4; // BoneNameCRC
            size += TranslationTrack.GetHeaderSize();
            size += RotationTrack.GetHeaderSize();
            size += ScaleTrack.GetHeaderSize();
            size += 12; // PivotPoint
            return size;
        }

        public void AddDataAndUpdateOffsets(ref List<byte> boneDataSpace, UInt32 dataSpaceStartOffset)
        {
            TranslationTrack.AddDataAndUpdateOffsets(ref boneDataSpace, dataSpaceStartOffset);
            RotationTrack.AddDataAndUpdateOffsets(ref boneDataSpace, dataSpaceStartOffset);
            ScaleTrack.AddDataAndUpdateOffsets(ref boneDataSpace, dataSpaceStartOffset);
        }

        public List<byte> GetHeaderBytes()
        {
            List<byte> bytes = new List<byte>();
            bytes.AddRange(BitConverter.GetBytes(KeyBoneID));
            bytes.AddRange(BitConverter.GetBytes(Convert.ToUInt32(Flags)));
            bytes.AddRange(BitConverter.GetBytes(ParentBone));
            bytes.AddRange(BitConverter.GetBytes(SubMeshID));
            bytes.AddRange(BitConverter.GetBytes(BoneNameCRC));
            bytes.AddRange(TranslationTrack.GetHeaderBytes());
            bytes.AddRange(RotationTrack.GetHeaderBytes());
            bytes.AddRange(ScaleTrack.GetHeaderBytes());
            bytes.AddRange(PivotPoint.ToBytes());
            return bytes;
        }
    }
}
