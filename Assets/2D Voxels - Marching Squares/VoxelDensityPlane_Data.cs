using UnityEngine;
using System.Collections;

namespace SCARLET.VoxelDensity
{
    #region Voxel Data

    internal class Voxel2D
    {
        internal Vector2 Position;
        internal float Value;

        internal Voxel2D(float posX, float posY) : this(posX, posY, 0) { }
        internal Voxel2D(float posX, float posY, float value) : this(new Vector2(posX, posY), value) { }
        internal Voxel2D(Vector2 pos, float value)
        {
            Value = value;
            Position = pos;
        }

        internal Voxel2D ToNewVoxelFromOffset(Vector2 offset) => new Voxel2D(Position + offset, Value);
    }
    internal class VoxelChunk2D
    {
        internal Vector2 Position = Vector2.zero;
        internal Voxel2D[] Voxels;

        internal BoxCollider Collider;
        internal MeshFilter MeshFilter;
        internal MeshRenderer MeshRenderer;

        internal VoxelChunk2D XNeighbour;
        internal VoxelChunk2D YNeighbour;
    }

    #endregion

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