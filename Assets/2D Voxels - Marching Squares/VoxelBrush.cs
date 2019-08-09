using UnityEngine;
using System.Collections;

namespace SCARLET.VoxelDensity
{
    #region Voxel Brush Data

    public class VoxelBrush2D
    {
        internal VoxelDirectionValuePair2D[] ValueDirectionPairs;

        public VoxelBrush2D()
        {
            ValueDirectionPairs = new VoxelDirectionValuePair2D[1]
            {
            new VoxelDirectionValuePair2D(0,0,1)
            };
        }
    }
    internal struct VoxelDirectionValuePair2D
    {
        internal int XDir;
        internal int YDir;
        internal float Value;

        internal VoxelDirectionValuePair2D(int xDir, int yDir, float value)
        {
            XDir = xDir;
            YDir = yDir;
            Value = value;
        }
    }

    #endregion
}