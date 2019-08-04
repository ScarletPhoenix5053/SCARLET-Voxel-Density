﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SCARLET.VoxelDensity
{
    public class VoxelDensity_EditorRegeneration : MonoBehaviour
    {
        [Header("Voxel Density Objects")]
        public VoxelDensityPlane VoxelDensityPlane;
        public VoxelDensityVolume VoxelDensityVolume;

        [Header("Sampling Optionss")]
        public float NoiseFrequency = 10f;
        public float NoiseLacunarity = 2f;
        public float NoisePersistence = 0.5f;
        public int NoiseOctaves = 3;

        public enum MethodSelection
        {
            Min,
            Max,
            Random,
            Value1D,
            Value2D,
            Value3D,
            Value1DFractal,
            Value2DFractal,
            Value3DFractal,
            Perlin1D,
            Perlin2D,
            Perlin3D,
            Perlin1DFractal,
            Perlin2DFractal,
            Perlin3DFractal
        }
        public MethodSelection FillType = MethodSelection.Perlin2DFractal;

        private void OnValidate()
        {
            Sample.NoiseFrequency = NoiseFrequency;
            Sample.NoiseLacunarity = NoiseLacunarity;
            Sample.NoisePersistence = NoisePersistence;
            Sample.NoiseOctaves = NoiseOctaves;
        }

        public SamplingMethod[] SamplingMethods = new SamplingMethod[]
        {
            Sample.Min,
            Sample.Max,
            Sample.Random,
            Sample.Value1D,
            Sample.Value2D,
            Sample.Value3D,
            Sample.Value1DFractal,
            Sample.Value2DFractal,
            Sample.Value3DFractal,
            Sample.Perlin1D,
            Sample.Perlin2D,
            Sample.Perlin3D,
            Sample.Perlin1DFractal,
            Sample.Perlin2DFractal,
            Sample.Perlin3DFractal
        };

        private float noiseOffset = 0;
        public float noiseOffsetSpeed = 1f;
        public bool NoiseMotion = false;

        public void TriggerRegeneration()
        {
            VoxelDensityPlane?.RegenerateChunks(SamplingMethods[(int)FillType]);
            VoxelDensityVolume?.RegenerateChunks(SamplingMethods[(int)FillType]);
            Debug.Log(Sample.NoiseOctaves);
        }

        private void FixedUpdate()
        {
            if (NoiseMotion)
            {
                noiseOffset += Time.fixedDeltaTime * noiseOffsetSpeed;

                VoxelDensityPlane?.RegenerateChunks(SamplingMethods[(int)FillType], noiseOffset);
                VoxelDensityVolume?.RegenerateChunks(SamplingMethods[(int)FillType], noiseOffset);
            }
        }
    }
}

