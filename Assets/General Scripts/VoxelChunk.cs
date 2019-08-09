using UnityEngine;
using System.Collections;

namespace SCARLET.VoxelDensity
{
    [RequireComponent(
        typeof(MeshRenderer),
        typeof(MeshFilter)
        )]
    public class VoxelChunk : MonoBehaviour
    {
        internal Transform Parent { get => transform.parent; set { transform.parent = value; } }
        internal Vector3 Position { get => transform.position; set { transform.position = value; } }
        internal Voxel[] Voxels;

        public MeshFilter MeshFilter => GetComponent<MeshFilter>();
        public MeshRenderer MeshRenderer => GetComponent<MeshRenderer>();

        public VoxelChunk XNeighbour;
        public VoxelChunk YNeighbour;
        public VoxelChunk ZNeighbour;
    }

    internal class Voxel
    {
        internal const float DefaultVoxelValue = 0f;

        internal Vector3 Position;
        internal float Value;

        internal Voxel(Vector3 pos) : this(pos, DefaultVoxelValue) { }
        internal Voxel(Vector3 pos, float value)
        {
            Value = value;
            Position = pos;
        }

        internal Voxel ToNewVoxelFromOffset(Vector3 offset) => new Voxel(Position + offset, Value);
    }
}