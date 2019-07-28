using UnityEngine;
using System.Collections;

namespace SCARLET.VoxelDensity
{
    #region Voxel Data

    internal class Voxel
    {
        internal Vector3 Position;
        internal float Value;

        internal Voxel(float posX, float posY) : this(posX, posY, 0) { }
        internal Voxel(float posX, float posY, float value) : this(new Vector2(posX, posY), value) { }
        internal Voxel(Vector2 pos, float value)
        {
            Value = value;
            Position = pos;
        }

        internal Voxel ToNewVoxelFromOffset(Vector3 offset) => new Voxel(Position + offset, Value);
    }
    internal class VoxelChunk
    {
#pragma warning disable 0649
        internal Vector3 Position = Vector2.zero;
        internal Voxel2D[] Voxels;

        internal BoxCollider Collider;
        internal MeshFilter MeshFilter;
        internal MeshRenderer MeshRenderer;

        internal VoxelChunk2D XNeighbour;
        internal VoxelChunk2D YNeighbour;
#pragma warning restore 0649
    }

    #endregion

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
        internal float Value;

        internal VoxelDirectionValuePair(int xDir, int yDir, int zDir, float value)
        {
            XDir = xDir;
            YDir = yDir;
            Value = value;
        }
    }

    #endregion
}