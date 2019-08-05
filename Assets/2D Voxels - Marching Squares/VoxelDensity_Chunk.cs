using UnityEngine;
using System.Collections;

namespace SCARLET.VoxelDensity
{
    public class VoxelDensityPlane_Chunk : MonoBehaviour
    {
        internal Vector2 Position { get => transform.position; set { transform.position = value; } }
        internal Voxel2D[] Voxels;

        public BoxCollider Collider;
        public MeshFilter MeshFilter;
        public MeshRenderer MeshRenderer;

        public VoxelDensityPlane_Chunk XNeighbour;
        public VoxelDensityPlane_Chunk YNeighbour;
    }
}