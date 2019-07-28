using UnityEngine;
using System;

namespace SCARLET.VoxelDensity
{
    public class VoxelDensityException : Exception
    {
        public VoxelDensityException() { }
        public VoxelDensityException(string message) : base(message) { }
        public VoxelDensityException(string message, Exception inner) : base(message, inner) { }
    }   
}
