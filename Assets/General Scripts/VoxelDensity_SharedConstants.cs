using UnityEngine;
using System.Collections;

namespace SCARLET.VoxelDensity
{
    internal static class Constants
    {
        internal const string ChunkName_Default = "Voxel Chunk";

        internal const string MaterialResourcePath_Default = "VoxelDensity_Default";
    }
    internal static class CommonReferences
    {
        internal static Material DefaultMaterial => Resources.Load<Material>(Constants.MaterialResourcePath_Default);
    }
}