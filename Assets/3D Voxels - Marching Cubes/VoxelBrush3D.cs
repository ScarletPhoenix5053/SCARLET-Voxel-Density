using UnityEngine;
using System.Collections;

namespace SCARLET.VoxelMesh
{
    #region Voxel Brush Data

    public class VoxelBrush
    {
        internal VoxelDirectionValuePair[] ValueDirectionPairs;

        public VoxelBrush()
        {
            ValueDirectionPairs = new VoxelDirectionValuePair[1]
            {
                new VoxelDirectionValuePair(0,0,0,1)
            };
        }
    }
    internal struct VoxelDirectionValuePair
    {
        internal int XDir;
        internal int YDir;
        internal int ZDir;
        internal float Value;

        internal VoxelDirectionValuePair(int xDir, int yDir, int zDir, float value)
        {
            XDir = xDir;
            YDir = yDir;
            ZDir = zDir;
            Value = value;
        }
    }

    #endregion
}