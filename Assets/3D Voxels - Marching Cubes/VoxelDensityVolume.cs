using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SCARLET.VoxelDensity
{
    public class VoxelDensityVolume : MonoBehaviour
    {
        #region Exposed Data
        
        [Range(1, 8)] public int CellResolution = 1;
        public int VoxelResolution => CellResolution + 1;
        
        public int ChunkCountX = 2;
        public int ChunkCountY = 2;
        public int ChunkCountZ = 2;

        public float ChunkSize = 10f;

        #endregion

        #region Encapsulated Data

        private const float halfpoint = 0.5f;
        private const float gizmoSize = 0.2f;

        private VoxelChunk[] voxelChunks;

        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;

        #endregion

        #region Unity Messages

        private void Awake()
        {
            // Find attatched components
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();

            // Generate chunks
            voxelChunks = GenerateChunks(ChunkCountX, ChunkCountY, ChunkCountZ, ChunkSize);
        }

        private void OnDrawGizmos()
        {
            if (voxelChunks != null)
            {
                foreach (VoxelChunk chunk in voxelChunks)
                {
                    foreach (Voxel voxel in chunk.Voxels)
                    {
                        Gizmos.color = new Color(voxel.Value, voxel.Value, voxel.Value);
                        Gizmos.DrawSphere(voxel.Position + chunk.Position, gizmoSize);
                    }
                }
            }
        }

        #endregion

        #region Operation Methods

        public void ApplyVoxelBrush(Vector3 point, VoxelBrush brush)
        {
            EditChunkDataAtPoint(point, brush, voxelChunks);

            /*
            TriangulateData(voxelChunks, out MeshData[] meshDataChunks);
            for (int i = 0; i < voxelChunks.Length; i++)
            {
                voxelChunks[i].MeshFilter.mesh = meshDataChunks[i].ToMesh();
            }
            */
        }

        #endregion

        #region Working Methods

        private VoxelChunk[] GenerateChunks(int chunkCountX, int chunkCountY, int chunkCountZ, float chunkSize)
        {
            VoxelChunk[] voxelChunks = new VoxelChunk[chunkCountX * chunkCountY * chunkCountZ];
            int i = 0;

            float chunkPosZ = FindFirstChunkPos(chunkSize, ChunkCountZ);
            for (int z = 0; z < chunkCountZ; z++)
            {
                float chunkPosY = FindFirstChunkPos(chunkSize, ChunkCountY);
                for (int y = 0; y < chunkCountY; y++)
                {
                    float chunkPosX = FindFirstChunkPos(chunkSize, ChunkCountX);
                    for (int x = 0; x < chunkCountX; x++, i++)
                    {
                        // Define location and voxels
                        var chunkPos = new Vector3(chunkPosX, chunkPosY, chunkPosZ) + Vector3.one * (chunkSize / 2);
                        var chunk = new VoxelChunk();
                        chunk.Voxels = GenerateVoxelChunk(chunkSize, CellResolution, Vector3.one * -(chunkSize / 2));
                        chunk.Position = chunkPos;

                        // Create gameobject for collider
                        var chunkGameObj = new GameObject(Constants.ChunkName_Default + " (" + i + ") " + x + "," + y + "," + z);
                        chunkGameObj.transform.parent = transform;
                        chunkGameObj.transform.position = chunkPos;

                        // Create mesh components
                        chunk.MeshFilter = chunkGameObj.AddComponent<MeshFilter>();
                        chunk.MeshRenderer = chunkGameObj.AddComponent<MeshRenderer>();
                        chunk.MeshRenderer.sharedMaterial = CommonReferences.DefaultMaterial;

                        // Incriemnt
                        voxelChunks[i] = chunk;
                        chunkPosX += chunkSize;

                        // Assign self as neighbour to relevant chunks
                        /*
                        if (x != 0) voxelChunks[i - 1].XNeighbour = voxelChunks[i];
                        if (y != 0) voxelChunks[i - chunkCountX].YNeighbour = voxelChunks[i];
                        */
                    }
                    chunkPosY += chunkSize;
                }
                chunkPosZ += chunkSize;
            }

            return voxelChunks;
        }
        private Voxel[] GenerateVoxelChunk(float size, int resolution, Vector3 offset)
        {
            int cells = resolution * resolution;
            int voxelsPerRow = resolution + 1;
            float spacing = size / voxelsPerRow;

            Voxel[] voxelVolume = new Voxel[voxelsPerRow * voxelsPerRow * voxelsPerRow];

            // Iterate and generate volume
            float posZ = spacing / 2;
            for (int z = 0; z < voxelsPerRow; z++)
            {
                float posY = spacing / 2;
                for (int y = 0; y < voxelsPerRow; y++)
                {
                    float posX = spacing / 2;
                    for (int x = 0; x < voxelsPerRow; x++)
                    {
                        var newVoxel = new Voxel(posX + offset.x, posY + offset.y, posZ + offset.z, 0);
                        voxelVolume[x + (y * voxelsPerRow) + (int)(z * Mathf.Pow(voxelsPerRow, 2))] = newVoxel;
                        posX += spacing;
                    }
                    posY += spacing;
                }
                posZ += spacing;
            }

            return voxelVolume;
        }
        private float FindFirstChunkPos(float chunkSize, int chunkCount) => -(chunkSize * chunkCount) / 2;


        private void EditChunkDataAtPoint(Vector3 point, VoxelBrush brush, VoxelChunk[] voxelChunks)
        {
            // Find closest voxel to point
            var chunk_closestI = 0;
            var voxel_closestI = 0;
            for (int ci = 0; ci < voxelChunks.Length; ci++)
            {
                for (int vi = 0; vi < voxelChunks[ci].Voxels.Length; vi++)
                {
                    if (Vector3.Distance(voxelChunks[ci].Voxels[vi].Position + voxelChunks[ci].Position, point) <
                        Vector3.Distance(voxelChunks[chunk_closestI].Voxels[voxel_closestI].Position + voxelChunks[chunk_closestI].Position, point))
                    {
                        voxel_closestI = vi;
                        chunk_closestI = ci;
                    }
                }
            }

            // Establish where the voxel is in its chunk
            //int voxel_Y = System.Math.DivRem(voxel_closestI,VoxelResolution, out int voxel_X);
            /*
            int voxel_Z = voxel_closestI / (int)Mathf.Pow(VoxelResolution, 2);
            int voxel_Y = (voxel_closestI - voxel_Z) / VoxelResolution;
            int voxel_X = voxel_closestI - voxel_Z - voxel_Y;
            */
            int voxel_Z = System.Math.DivRem(voxel_closestI, (int)Mathf.Pow(VoxelResolution, 2), out int zRem);
            int voxel_Y = System.Math.DivRem(zRem, VoxelResolution, out int voxel_X); 

            Debug.Log("Editing Voxel at: " + voxel_X + "," + voxel_Y + "," + voxel_Z + " in Chunk: " + chunk_closestI);
            
            // Modify voxels based on active brush
            for (int i = 0; i < brush.ValueDirectionPairs.Length; i++)
            {
                //Get this chunk's x and y indicies
                int chunk_editI = chunk_closestI;
                int chunk_X = 0;
                int chunk_Y = 0;
                int chunk_Z = 0;
                for (int chunk_i = 0; chunk_i < ChunkCountX * ChunkCountY * ChunkCountZ; chunk_i++)
                {
                    if (chunk_i == chunk_editI) break;
                    chunk_X++;

                    if (chunk_Y == ChunkCountY)
                    {
                        chunk_Z++;
                        chunk_Y = 0;
                    }
                    if (chunk_X == ChunkCountX)
                    {
                        chunk_Y++;
                        chunk_X = 0;
                    }
                }

                // Try edit the voxel
                bool abort = false;

                TryEditVoxelOnAxis(
                    voxel_X, chunk_X,
                    brush.ValueDirectionPairs[i].XDir,
                    dimensionOffset: 1,
                    out int editXI,
                    ref chunk_editI,
                    ref abort);
                TryEditVoxelOnAxis(
                    voxel_Y, chunk_Y,
                    brush.ValueDirectionPairs[i].YDir,
                    dimensionOffset: ChunkCountX,
                    out int editYI,
                    ref chunk_editI,
                    ref abort
                    );
                TryEditVoxelOnAxis(
                    voxel_Z, chunk_Z,
                    brush.ValueDirectionPairs[i].ZDir,
                    dimensionOffset: ChunkCountX * ChunkCountY,
                    out int editZI,
                    ref chunk_editI,
                    ref abort
                    );

                if (!abort)
                    voxelChunks[chunk_editI]
                        .Voxels[editXI + (editYI * VoxelResolution) + (editZI * ((int)Mathf.Pow(VoxelResolution,2)))]
                            .Value = brush.ValueDirectionPairs[i].Value;
            }
        }
        private void TryEditVoxelOnAxis(int voxel_N, int chunk_N, int nDirOffset, int dimensionOffset, out int voxel_editNI, ref int chunk_editI, ref bool abort)
        {
            voxel_editNI = voxel_N + nDirOffset;

            if (voxel_editNI < 0 || CellResolution < voxel_editNI)
            {
                // Get offset pos of chunk along n axis
                var chunk_offsetNI =
                    System.Math.DivRem(
                        voxel_editNI,
                        CellResolution + 1,
                        out int posInNewChunk
                        );
                chunk_offsetNI += (int)Mathf.Sign(voxel_editNI) * 1;
                if (chunk_offsetNI > 0) chunk_offsetNI--;

                // Try modify n in other chunk. Prevent edit if chunk does not exist
                if (chunk_N + chunk_offsetNI < 0 ||
                    ChunkCountY <= chunk_N + chunk_offsetNI
                    )
                {
                    abort = true;
                    return;
                }
                else
                {
                    // Dimension offset is used for converting additional dimensions back into the singular dimension of the chunk array.
                    // 1st dim uses 1
                    // 2nd dim uses length of 1st
                    // 3rd dim uses area of 1st and 2nd
                    chunk_editI += chunk_offsetNI * dimensionOffset;

                    if (voxel_editNI < 0) voxel_editNI += Mathf.Abs(chunk_offsetNI) * (CellResolution + 1);
                    else voxel_editNI -= Mathf.Abs(chunk_offsetNI) * (CellResolution + 1);
                }
            }
        }

        #endregion
    }
}
