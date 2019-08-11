using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace SCARLET.VoxelMesh
{
    public class VoxelMeshVolume : MonoBehaviour
    {
        #region Exposed Data
        
        [Range(1, 16)] public int CellResolution = 1;
        public int VoxelResolution => CellResolution + 1;
        
        public int ChunkCountX = 2;
        public int ChunkCountY = 2;
        public int ChunkCountZ = 2;

        public float ChunkSize = 10f;

        [Header("Default Generation")]
        public float VoxelValueDefault = 0;
        public bool InterpolateEdgeIntersections = true;

        [Header("Gizmo Control")]
        public float GizmoSize = 0.01f;
        public bool DrawGizmos = false;
        #endregion

        #region Encapsulated Data

        private const float halfpoint = 0.5f;

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

            if (DrawGizmos && voxelChunks != null)
            {
                foreach (VoxelChunk chunk in voxelChunks)
                {
                    foreach (Voxel voxel in chunk.Voxels)
                    {
                        Gizmos.color = new Color(voxel.Value, voxel.Value, voxel.Value);
                        Gizmos.DrawSphere(voxel.Position + chunk.Position, GizmoSize);
                    }
                }
            }
        }

        #endregion

        #region Data Generation

        public void RegenerateChunks()
        {
            // Reset data
            for (int i = 0; i < voxelChunks.Length; i++)
            {
                for (int j = 0; j < voxelChunks[i].Voxels.Length; j++)
                {
                    voxelChunks[i].Voxels[j].Value = 0;
                }
            }

            // Triangulate
            TriangulateData(voxelChunks, out MeshData[] meshDataChunks);
            for (int i = 0; i < voxelChunks.Length; i++)
            {
                voxelChunks[i].MeshFilter.mesh = meshDataChunks[i].ToMesh();
            }
        }
        public void RegenerateChunks(SamplingMethod samplingFunction, float noiseOffset = 0)
        {
            // Edit Data
            for (int i = 0; i < voxelChunks.Length; i++)
            {
                var currentChunk = voxelChunks[i];
                for (int j = 0; j < voxelChunks[i].Voxels.Length; j++)
                {
                    var currentVoxel = currentChunk.Voxels[j];
                    currentVoxel.Value = samplingFunction(currentChunk.Position + currentVoxel.Position + (Vector3.right * noiseOffset));
                }
            }

            // Triangulate
            TriangulateData(voxelChunks, out MeshData[] meshDataChunks);
            for (int i = 0; i < voxelChunks.Length; i++)
            {
                voxelChunks[i].MeshFilter.mesh = meshDataChunks[i].ToMesh();
            }
        }

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
                        var chunkGameObj = new GameObject(Constants.ChunkName_Default + " (" + i + ") " + x + "," + y + "," + z);
                        var chunkPos = new Vector3(chunkPosX, chunkPosY, chunkPosZ) + Vector3.one * (chunkSize / 2);
                        var chunk = chunkGameObj.AddComponent<VoxelChunk>();
                        chunk.Voxels = GenerateVoxelChunk(chunkSize, CellResolution, Vector3.one * -(chunkSize / 2));
                        chunk.Position = chunkPos;

                        // Create gameobject for collider
                        chunkGameObj.transform.parent = transform;
                        chunkGameObj.transform.position = chunkPos;

                        // Create mesh components
                        chunk.MeshRenderer.sharedMaterial = CommonReferences.DefaultMaterial;

                        // Incriemnt
                        voxelChunks[i] = chunk;
                        chunkPosX += chunkSize;

                        // Assign self as neighbour to relevant chunks
                        if (x != 0) voxelChunks[i - 1].XNeighbour = voxelChunks[i];
                        if (y != 0) voxelChunks[i - chunkCountX].YNeighbour = voxelChunks[i];
                        if (z != 0) voxelChunks[i - (chunkCountX * chunkCountY)].ZNeighbour = voxelChunks[i];
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
                        var newVoxel = new Voxel(new Vector3(posX + offset.x, posY + offset.y, posZ + offset.z), VoxelValueDefault);
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
        
        #endregion

        #region Data Editing

        public void ApplyVoxelBrush(Vector3 point, VoxelBrush brush)
        {
            EditChunkDataAtPoint(point, brush, voxelChunks);

            TriangulateData(voxelChunks, out MeshData[] meshDataChunks);
            for (int i = 0; i < voxelChunks.Length; i++)
            {
                voxelChunks[i].MeshFilter.mesh = meshDataChunks[i].ToMesh();
            }
        }
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
            int voxel_Z = System.Math.DivRem(voxel_closestI, (int)Mathf.Pow(VoxelResolution, 2), out int zRem);
            int voxel_Y = System.Math.DivRem(zRem, VoxelResolution, out int voxel_X); 
            
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

        #region Triangulation

        private void TriangulateData(VoxelChunk[] voxelChunks, out MeshData[] meshDataChunks)
        {
            meshDataChunks = new MeshData[voxelChunks.Length];

            Vector3 offset_xNeighbour = new Vector3(ChunkSize, 0, 0);
            Vector3 offset_yNeighbour = new Vector3(0, ChunkSize, 0);
            Vector3 offset_zNeighbour = new Vector3(0, 0, ChunkSize);

            Vector3 offset_xyNeighbour = offset_xNeighbour + offset_yNeighbour;
            Vector3 offset_xzNeighbour = offset_xNeighbour + offset_zNeighbour;
            Vector3 offset_yzNeighbour = offset_yNeighbour + offset_zNeighbour;

            Vector3 offset_xyzNeighbour = offset_xNeighbour + offset_yNeighbour + offset_zNeighbour;

            
            // For each chunk
            for (int i = 0; i < voxelChunks.Length; i++)
            {
                var meshData = new MeshData();
                meshData.Verts = new List<Vector3>();
                meshData.Tris = new List<int>();

                int vertex_i = 0;

                int xMax = CellResolution;
                int yMax = VoxelResolution * CellResolution;
                int zMax = VoxelResolution * VoxelResolution * CellResolution;

                // Triangulate planes
                for (int z = 0; z < CellResolution; z++)
                {
                    int zNear = z * (int)Mathf.Pow(VoxelResolution, 2);
                    int zFar = (z + 1) * (int)Mathf.Pow(VoxelResolution, 2);

                    // Rows
                    for (int y = 0; y < CellResolution; y++)
                    {
                        int yLow = y * VoxelResolution;
                        int yHigh = (y + 1) * VoxelResolution;

                        // Triangulate all core cells
                        for (int x = 0; x < CellResolution; x++)
                        {
                            int i0 = x + yLow + zFar;
                            int i3 = x + yLow + zNear;
                            int i4 = x + yHigh + zFar;
                            int i7 = x + yHigh + zNear;
                            Voxel[] coreVoxels = new Voxel[]
                            {
                                voxelChunks[i].Voxels[i0],
                                voxelChunks[i].Voxels[i0+1],
                                voxelChunks[i].Voxels[i3+1],
                                voxelChunks[i].Voxels[i3],

                                voxelChunks[i].Voxels[i4],
                                voxelChunks[i].Voxels[i4+1],
                                voxelChunks[i].Voxels[i7+1],
                                voxelChunks[i].Voxels[i7]
                            };
                            TriangulateCell(coreVoxels, ref meshData, ref vertex_i);
                        }

                        // Triangulate x neighbour gap for this row
                        if (voxelChunks[i].XNeighbour != null)
                        {
                            Voxel[] xGapVoxels = new Voxel[]
                            {
                                voxelChunks[i].Voxels[CellResolution + yLow + zFar],
                                voxelChunks[i].XNeighbour.Voxels[yLow + zFar].ToNewVoxelFromOffset(offset_xNeighbour),
                                voxelChunks[i].XNeighbour.Voxels[yLow + zNear].ToNewVoxelFromOffset(offset_xNeighbour),
                                voxelChunks[i].Voxels[CellResolution + yLow + zNear],

                                voxelChunks[i].Voxels[CellResolution + yHigh + zFar],
                                voxelChunks[i].XNeighbour.Voxels[yHigh + zFar].ToNewVoxelFromOffset(offset_xNeighbour),
                                voxelChunks[i].XNeighbour.Voxels[yHigh + zNear].ToNewVoxelFromOffset(offset_xNeighbour),
                                voxelChunks[i].Voxels[CellResolution + yHigh + zNear]
                            };
                            TriangulateCell(xGapVoxels, ref meshData, ref vertex_i);
                        }

                    }
                    
                    // Triangulate a row of the y neighbour plane
                    if (voxelChunks[i].YNeighbour != null)
                    {
                        for (int x = 0; x < CellResolution; x++)
                        {
                            int i0 = x + yMax + zFar;
                            int i3 = x + yMax + zNear;
                            int i4 = x + 0 + zFar;
                            int i7 = x + 0 + zNear;
                            Voxel[] coreVoxels = new Voxel[]
                            {
                                voxelChunks[i].Voxels[i0],
                                voxelChunks[i].Voxels[i0+1],
                                voxelChunks[i].Voxels[i3+1],
                                voxelChunks[i].Voxels[i3],

                                voxelChunks[i].YNeighbour.Voxels[i4].ToNewVoxelFromOffset(offset_yNeighbour),
                                voxelChunks[i].YNeighbour.Voxels[i4+1].ToNewVoxelFromOffset(offset_yNeighbour),
                                voxelChunks[i].YNeighbour.Voxels[i7+1].ToNewVoxelFromOffset(offset_yNeighbour),
                                voxelChunks[i].YNeighbour.Voxels[i7].ToNewVoxelFromOffset(offset_yNeighbour)
                            };
                            TriangulateCell(coreVoxels, ref meshData, ref vertex_i);
                        }

                        // Triangulate xy neighbour gap for this row
                        if (voxelChunks[i].XNeighbour != null)
                        {
                            Voxel[] xGapVoxels = new Voxel[]
                            {
                                voxelChunks[i].Voxels[xMax + yMax + zFar],
                                voxelChunks[i].XNeighbour.Voxels[0 + yMax + zFar].ToNewVoxelFromOffset(offset_xNeighbour),
                                voxelChunks[i].XNeighbour.Voxels[0 + yMax + zNear].ToNewVoxelFromOffset(offset_xNeighbour),
                                voxelChunks[i].Voxels[xMax + yMax + zNear],

                                voxelChunks[i].YNeighbour.Voxels[xMax + zFar].ToNewVoxelFromOffset(offset_yNeighbour),
                                voxelChunks[i].YNeighbour.XNeighbour.Voxels[zFar].ToNewVoxelFromOffset(offset_xyNeighbour),
                                voxelChunks[i].YNeighbour.XNeighbour.Voxels[zNear].ToNewVoxelFromOffset(offset_xyNeighbour),
                                voxelChunks[i].YNeighbour.Voxels[xMax + zNear].ToNewVoxelFromOffset(offset_yNeighbour)
                            };
                            TriangulateCell(xGapVoxels, ref meshData, ref vertex_i);
                        }
                    }
                }

                // Triangulate Z neighbour plane
                if (voxelChunks[i].ZNeighbour != null)
                {
                    // Rows
                    for (int y = 0; y < CellResolution; y++)
                    {
                        int yLow = y * VoxelResolution;
                        int yHigh = (y + 1) * VoxelResolution;

                        // Cells
                        for (int x = 0; x < CellResolution; x++)
                        {
                            int i0 = x + yLow;
                            int i3 = x + yLow + zMax;
                            int i4 = x + yHigh;
                            int i7 = x + yHigh + zMax;
                            Voxel[] coreVoxels = new Voxel[]
                            {
                                voxelChunks[i].ZNeighbour.Voxels[i0].ToNewVoxelFromOffset(offset_zNeighbour),
                                voxelChunks[i].ZNeighbour.Voxels[i0+1].ToNewVoxelFromOffset(offset_zNeighbour),
                                voxelChunks[i].Voxels[i3+1],
                                voxelChunks[i].Voxels[i3],

                                voxelChunks[i].ZNeighbour.Voxels[i4].ToNewVoxelFromOffset(offset_zNeighbour),
                                voxelChunks[i].ZNeighbour.Voxels[i4+1].ToNewVoxelFromOffset(offset_zNeighbour),
                                voxelChunks[i].Voxels[i7+1],
                                voxelChunks[i].Voxels[i7]
                            };
                            TriangulateCell(coreVoxels, ref meshData, ref vertex_i);

                        }

                        // XZ Neighbour cell
                        if (voxelChunks[i].XNeighbour != null)
                        {
                            Voxel[] xGapVoxels = new Voxel[]
                            {
                                voxelChunks[i].ZNeighbour.Voxels[xMax + yLow].ToNewVoxelFromOffset(offset_zNeighbour),
                                voxelChunks[i].XNeighbour.ZNeighbour.Voxels[yLow].ToNewVoxelFromOffset(offset_xzNeighbour),
                                voxelChunks[i].XNeighbour.Voxels[yLow + zMax].ToNewVoxelFromOffset(offset_xNeighbour),
                                voxelChunks[i].Voxels[xMax + yLow + zMax],

                                voxelChunks[i].ZNeighbour.Voxels[xMax + yHigh].ToNewVoxelFromOffset(offset_zNeighbour),
                                voxelChunks[i].XNeighbour.ZNeighbour.Voxels[yHigh].ToNewVoxelFromOffset(offset_xzNeighbour),
                                voxelChunks[i].XNeighbour.Voxels[yHigh + zMax].ToNewVoxelFromOffset(offset_xNeighbour),
                                voxelChunks[i].Voxels[xMax + yHigh + zMax]
                            };
                            TriangulateCell(xGapVoxels, ref meshData, ref vertex_i);
                        }

                    }

                    // YZ Row
                    if (voxelChunks[i].YNeighbour != null)
                    {
                        for (int x = 0; x < CellResolution; x++)
                        {
                            int i0 = x + yMax + 0;
                            int i3 = x + yMax + zMax;
                            int i4 = x + 0 + 0;
                            int i7 = x + 0 + zMax;
                            Voxel[] coreVoxels = new Voxel[]
                            {
                                voxelChunks[i].ZNeighbour.Voxels[i0].ToNewVoxelFromOffset(offset_zNeighbour),
                                voxelChunks[i].ZNeighbour.Voxels[i0+1].ToNewVoxelFromOffset(offset_zNeighbour),
                                voxelChunks[i].Voxels[i3+1],
                                voxelChunks[i].Voxels[i3],

                                voxelChunks[i].YNeighbour.ZNeighbour.Voxels[i4].ToNewVoxelFromOffset(offset_yzNeighbour),
                                voxelChunks[i].YNeighbour.ZNeighbour.Voxels[i4+1].ToNewVoxelFromOffset(offset_yzNeighbour),
                                voxelChunks[i].YNeighbour.Voxels[i7+1].ToNewVoxelFromOffset(offset_yNeighbour),
                                voxelChunks[i].YNeighbour.Voxels[i7].ToNewVoxelFromOffset(offset_yNeighbour)
                            };
                            TriangulateCell(coreVoxels, ref meshData, ref vertex_i);
                        }

                        
                        // Triangulate XYZ corner cell
                        if (voxelChunks[i].XNeighbour != null)
                        {
                            if (voxelChunks[i].XNeighbour != null)
                            {
                                Voxel[] xGapVoxels = new Voxel[]
                                {
                                    voxelChunks[i].ZNeighbour.Voxels[xMax + yMax].ToNewVoxelFromOffset(offset_zNeighbour),
                                    voxelChunks[i].XNeighbour.ZNeighbour.Voxels[yMax].ToNewVoxelFromOffset(offset_xzNeighbour),
                                    voxelChunks[i].XNeighbour.Voxels[yMax + zMax].ToNewVoxelFromOffset(offset_xNeighbour),
                                    voxelChunks[i].Voxels[xMax+yMax+zMax],

                                    voxelChunks[i].YNeighbour.ZNeighbour.Voxels[xMax].ToNewVoxelFromOffset(offset_yzNeighbour),
                                    voxelChunks[i].XNeighbour.YNeighbour.ZNeighbour.Voxels[0].ToNewVoxelFromOffset(offset_xyzNeighbour),
                                    voxelChunks[i].XNeighbour.YNeighbour.Voxels[zMax].ToNewVoxelFromOffset(offset_xyNeighbour),
                                    voxelChunks[i].YNeighbour.Voxels[xMax + zMax].ToNewVoxelFromOffset(offset_yNeighbour)
                                };
                                TriangulateCell(xGapVoxels, ref meshData, ref vertex_i);
                            }

                        }
                    }

                }


                meshDataChunks[i] = meshData;
            }
        }
        private void TriangulateCell(Voxel[] voxels, ref MeshData meshData, ref int vertex_i)
        {
            // Define cell-based variables
            int cell_EdgeVertexCount = 0;
            byte cell_Mask = 0;
            byte workingBit = 0b_0000_0001;

            byte[] cornerIndicies = new byte[8];
            int[] edgeIndicies = Enumerable.Repeat(-1, 12).ToArray();
            Vector3[] edgePositions = new Vector3[]
            {
                PointBetweenVoxels(voxels, 0, 1),
                PointBetweenVoxels(voxels, 1, 2),
                PointBetweenVoxels(voxels, 2, 3),
                PointBetweenVoxels(voxels, 3, 0),

                PointBetweenVoxels(voxels, 4, 5),
                PointBetweenVoxels(voxels, 5, 6),
                PointBetweenVoxels(voxels, 6, 7),
                PointBetweenVoxels(voxels, 7, 4),

                PointBetweenVoxels(voxels, 0, 4),
                PointBetweenVoxels(voxels, 1, 5),
                PointBetweenVoxels(voxels, 2, 6),
                PointBetweenVoxels(voxels, 3, 7)
            };

            // Check corners and define cell type
            workingBit = 0b_0000_0001;
            for (byte v = 0; v < voxels.Length; v++)
            {
                if (voxels[v].Value <= 0)
                {
                    cell_Mask |= workingBit;
                }
                workingBit <<= 1;
            }
            // Use cell type to place edge verticies
            short edgeMask = TriangulationData.IntersectionPoints[cell_Mask];
            workingBit = 0b_0000_0001;
            for (byte v = 0; v < edgePositions.Length; v++)
            {
                if (edgeMask.Contains(workingBit))
                {
                    edgeIndicies[v] = cell_EdgeVertexCount;
                    AddVertexToMeshData(edgePositions[v], ref meshData, ref cell_EdgeVertexCount);
                }
                workingBit <<= 1;
            }

            // Stitch vertecies to create tris
            var triOrder = TriangulationData.TriangleFormations[cell_Mask];
            foreach (byte triVertIndex in triOrder)
            {
                // In marching cubes we don't have to worry about corner verts! only edge cuts need to be defined
                meshData.Tris.Add(vertex_i + edgeIndicies[triVertIndex]);
            }

            // Inriment indicies
            vertex_i = vertex_i + cell_EdgeVertexCount;
        }
        private void AddVertexToMeshData(Vector3 vert, ref MeshData meshData, ref int vertex_i)
        {
            meshData.Verts.Add(vert);
            vertex_i++;
        }
        private Vector3 PointBetweenVoxels(Voxel[] voxelArray, int a, int b)
        {
            if (a >= voxelArray.Length || b >= voxelArray.Length)
                throw new VoxelDensityException("Indicies a or b are out of range of voxel array " + voxelArray.ToString());
            return PointBetweenVoxels(voxelArray[a], voxelArray[b]);
        }
        private Vector3 PointBetweenVoxels(Voxel a, Voxel b)
        {
            if (InterpolateEdgeIntersections)
            {
                var t = Mathf.InverseLerp(a.Value, b.Value, 0);
                return Vector3.Lerp(a.Position, b.Position, t);
            }
            else
            {
                return Vector3.Lerp(a.Position, b.Position, halfpoint);
            }
        }

        #endregion

    }
}
