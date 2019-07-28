using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SCARLET.VoxelDensity
{
    [RequireComponent(
        typeof(MeshFilter),
        typeof(MeshRenderer)
        )]
    public class VoxelDensityPlane : MonoBehaviour
    {
        #region Modifiable Variables

        /// <summary>
        /// Number of voxel cells per chunk
        /// </summary>
        [Range(1, 8)] public int Resolution = 1;

        /// <summary>
        /// Number of chunks along the X axis
        /// </summary>
        public int ChunkCountX = 2;
        /// <summary>
        /// Number of chunks along the Y axis
        /// </summary>
        public int ChunkCountY = 2;

        /// <summary>
        /// Size of the chunk in world space. Chunks are square.
        /// </summary>
        public float ChunkSize = 10f;

        #endregion

        #region Immutable / Private Variables
        
        private const float colliderDepth = 0.1f;
        private const float halfpoint = 0.5f;
        private const float gizmoSize = 0.3f;

        private VoxelChunk2D[] voxelChunks;

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
            InitChunks();
        }

        private void Update()
        {
        }

        private void OnDrawGizmos()
        {
            if (voxelChunks != null)
            {
                //Debug.Log("Chunkcount: " + voxelChunks.Length);
                foreach (VoxelChunk2D chunk in voxelChunks)
                {
                    //Debug.Log(chunk.Position);
                    foreach (Voxel2D voxel in chunk.Voxels)
                    {
                        Gizmos.color = new Color(voxel.Value, voxel.Value, voxel.Value);
                        Gizmos.DrawSphere(voxel.Position + chunk.Position, gizmoSize);
                    }
                }
            }
        }

        #endregion

        #region Working Methods

        private void InitChunks()
        {
            Material defaultMaterial = Resources.Load<Material>("VoxelDensity_Default");

            voxelChunks = new VoxelChunk2D[ChunkCountX * ChunkCountY];

            float chunkPosY = -(ChunkSize * ChunkCountY) / 2;
            for (int y = 0, i = 0; y < ChunkCountY; y++)
            {
                float chunkPosX = -(ChunkSize * ChunkCountX) / 2;
                for (int x = 0; x < ChunkCountX; x++, i++)
                {
                    // Define location and voxels
                    var chunkPos = new Vector2(chunkPosX, chunkPosY) + new Vector2(ChunkSize / 2, ChunkSize / 2);
                    var chunk = new VoxelChunk2D();
                    chunk.Voxels = GenerateVoxelChunk(ChunkSize, Resolution, Vector2.one * -(ChunkSize/2));
                    chunk.Position = chunkPos;

                    // Create gameobject for collider
                    var chunkGameObj = new GameObject("Chunk " + x + "/" + y);
                    chunkGameObj.transform.parent = transform;
                    chunkGameObj.transform.position = chunkPos;

                    // Create collider
                    var boxCol = chunkGameObj.AddComponent<BoxCollider>();
                    boxCol.size = new Vector3(ChunkSize, ChunkSize, colliderDepth);
                    chunk.Collider = boxCol;

                    // Create mesh components
                    chunk.MeshFilter = chunkGameObj.AddComponent<MeshFilter>();
                    chunk.MeshRenderer = chunkGameObj.AddComponent<MeshRenderer>();
                    chunk.MeshRenderer.sharedMaterial = defaultMaterial;

                    // Incriemnt
                    voxelChunks[i] = chunk;
                    chunkPosX += ChunkSize;

                    // Assign self as neighbour to relevant chunks
                    if (x != 0) voxelChunks[i - 1].XNeighbour = voxelChunks[i];
                    if (y != 0) voxelChunks[i - ChunkCountX].YNeighbour = voxelChunks[i];
                }
                chunkPosY += ChunkSize;
            }
        }
        private Voxel2D[] GenerateVoxelChunk(float size, int resolution, Vector2 offset)
        {
            int cells = resolution * resolution;
            int voxelsPerRow = resolution + 1;
            float spacing = size / voxelsPerRow;

            Voxel2D[] voxelPlane = new Voxel2D[voxelsPerRow * voxelsPerRow];

            // Iterate and generate plane
            float posY = spacing / 2;
            for (int y = 0; y < voxelsPerRow; y++)
            {
                float posX = spacing / 2;
                for (int x = 0; x < voxelsPerRow; x++)
                {
                    var newVoxel = new Voxel2D(posX + offset.x, posY + offset.y);
                    voxelPlane[x + (y * voxelsPerRow)] = newVoxel;
                    posX += spacing;
                }
                posY += spacing;
            }

            return voxelPlane;
        }

        public void ApplyVoxelBrush(Vector3 point, VoxelBrush2D brush)
        {
            EditChunkDataAtPoint(point, brush, voxelChunks);

            TriangulateData(voxelChunks, out MeshData[] meshDataChunks);
            for (int i = 0; i < voxelChunks.Length; i++)
            {
                voxelChunks[i].MeshFilter.mesh = meshDataChunks[i].ToMesh();
            }
        }
        private void EditChunkDataAtPoint(Vector3 point, VoxelBrush2D brush, VoxelChunk2D[] voxelChunks)
        {
            // Find closest voxel to hit point
            var closestChunkIndex = 0;
            var closestVoxelIndex = 0;
            var closestVoxel = voxelChunks[0].Voxels[0];
            for (int ci = 0; ci < voxelChunks.Length; ci++)
            {
                for (int vi = 0; vi < voxelChunks[ci].Voxels.Length; vi++)
                {
                    var inspectedVoxel = voxelChunks[ci].Voxels[vi];
                    if (Vector3.Distance(inspectedVoxel.Position + voxelChunks[ci].Position, point) <
                        Vector3.Distance(closestVoxel.Position + voxelChunks[closestChunkIndex].Position, point))
                    {
                        closestVoxel = inspectedVoxel;
                        closestVoxelIndex = vi;
                        closestChunkIndex = ci;
                    }
                }
            }
            // Establish where the voxel is in its chunk
            int voxel_y = System.Math.DivRem(closestVoxelIndex, Resolution + 1, out int voxel_x);

            // Modify voxels based on active brush
            for (int i = 0; i < brush.ValueDirectionPairs.Length; i++)
            {
                //Get this chunk's x and y indicies
                int chunk_editI = closestChunkIndex;
                int chunk_x = 0;
                int chunk_y = 0;
                for (int chunk_i = 0; chunk_i < (ChunkCountX) * ChunkCountY; chunk_i++)
                {
                    if (chunk_i == chunk_editI) break;
                    chunk_x++;

                    if (chunk_x == ChunkCountX)
                    {
                        chunk_y++;
                        chunk_x = 0;
                    }
                }

                // Try edit the voxel
                bool abort = false;

                TryEditVoxelOnAxis(
                    voxel_y, chunk_y,
                    brush.ValueDirectionPairs[i].YDir,
                    dimensionOffset: ChunkCountX,
                    ref chunk_editI,
                    out int editYI,
                    ref abort
                    );
                TryEditVoxelOnAxis(
                    voxel_x, chunk_x,
                    brush.ValueDirectionPairs[i].XDir,
                    dimensionOffset: 1,
                    ref chunk_editI,
                    out int editXI,
                    ref abort);

                if (!abort)
                    voxelChunks[chunk_editI]
                        .Voxels[editXI + (editYI * (Resolution + 1))]
                            .Value = brush.ValueDirectionPairs[i].Value;
            }
        }
        private void TryEditVoxelOnAxis(int voxel_N, int chunk_N, int nDirOffset, int dimensionOffset, ref int chunk_editI, out int voxel_editN, ref bool abort)
        {
            voxel_editN = voxel_N + nDirOffset;

            var nIsOutOfBounds = voxel_editN < 0 || Resolution < voxel_editN;
            if (nIsOutOfBounds)
            {
                // Get offset pos of chunk along n axis
                var chunkoffsetN =
                    System.Math.DivRem(
                        voxel_editN,
                        Resolution + 1,
                        out int posInNewChunk
                        );
                chunkoffsetN += (int)Mathf.Sign(voxel_editN) * 1;
                if (chunkoffsetN > 0) chunkoffsetN--;

                // Try modify n in other chunk. Prevent edit if chunk does not exist
                int editCN = chunk_N + chunkoffsetN;
                if (editCN < 0 || ChunkCountY <= editCN) { abort = true; return; }
                else
                {
                    // Dimension offset is used for converting additional dimensions back into the singular dimension of the chunk array.
                    // 1st dim uses 1
                    // 2nd dim uses length of 1st
                    chunk_editI += chunkoffsetN * dimensionOffset;

                    if (voxel_editN < 0) voxel_editN += Mathf.Abs(chunkoffsetN) * (Resolution + 1);
                    else voxel_editN -= Mathf.Abs(chunkoffsetN) * (Resolution + 1);
                }
            }
        }

        private void TriangulateData(VoxelChunk2D[] voxelChunks, out MeshData[] meshDataChunks)
        {
            meshDataChunks = new MeshData[voxelChunks.Length];

            Vector2 offset_yNeighbour = new Vector2(0, ChunkSize);
            Vector2 offset_xNeighbour = new Vector2(ChunkSize, 0);
            Vector2 offset_xyNeighbour = new Vector2(ChunkSize, ChunkSize);

            // For each chunk
            for (int i = 0; i < voxelChunks.Length; i++)
            {
                var meshData = new MeshData();
                meshData.Verts = new List<Vector3>();
                meshData.Tris = new List<int>();
                
                int vertex_i = 0;

                // Triangulate all cells except top row
                for (int y = 0; y < Resolution; y++)
                {
                    // Triangulate row
                    for (int x = 0; x < Resolution; x++)
                    {
                        Voxel2D[] voxels = new Voxel2D[]
                        {
                            voxelChunks[i].Voxels[x + y * (Resolution + 1)],
                            voxelChunks[i].Voxels[x + y * (Resolution + 1) + 1],
                            voxelChunks[i].Voxels[x + (y + 1) * (Resolution + 1)],
                            voxelChunks[i].Voxels[x + (y + 1) * (Resolution + 1) + 1]
                        };
                        TriangulateCell(voxels, ref meshData, ref vertex_i);
                    }

                    // Triangulate gap cell (if possible)                    
                    if (voxelChunks[i].XNeighbour != null)
                    {
                        Voxel2D[] voxels = new Voxel2D[]
                        {
                            voxelChunks[i].Voxels[(Resolution) + y * (Resolution + 1)],
                            voxelChunks[i].XNeighbour.Voxels[y * (Resolution + 1)].ToNewVoxelFromOffset(offset_xNeighbour),
                            voxelChunks[i].Voxels[(Resolution) + (y + 1) * (Resolution + 1)],
                            voxelChunks[i].XNeighbour.Voxels[(y + 1) * (Resolution + 1)].ToNewVoxelFromOffset(offset_xNeighbour)
                        };
                        TriangulateCell(voxels, ref meshData, ref vertex_i);
                    }                    
                }

                // Triangulate top row (if possible)
                if (voxelChunks[i].YNeighbour != null)
                {
                    int y = Resolution;
                    for (int x = 0; x < Resolution; x++)
                    {
                        Voxel2D[] voxels = new Voxel2D[]
                        {
                            voxelChunks[i].Voxels[x + y * (Resolution + 1)],
                            voxelChunks[i].Voxels[x + y * (Resolution + 1) + 1],
                            voxelChunks[i].YNeighbour.Voxels[x].ToNewVoxelFromOffset(offset_yNeighbour),
                            voxelChunks[i].YNeighbour.Voxels[x+1].ToNewVoxelFromOffset(offset_yNeighbour)
                        };
                        TriangulateCell(voxels, ref meshData, ref vertex_i);
                    }
                }

                // Triangulate top-right gap (if possible)          
                if (voxelChunks[i].XNeighbour != null && voxelChunks[i].YNeighbour != null)
                {
                    int y = Resolution;

                    Voxel2D[] voxels = new Voxel2D[]
                    {
                        voxelChunks[i].Voxels[Resolution + y * (Resolution + 1)],
                        voxelChunks[i].XNeighbour.Voxels[y * (Resolution + 1)].ToNewVoxelFromOffset(offset_xNeighbour),
                        voxelChunks[i].YNeighbour.Voxels[Resolution].ToNewVoxelFromOffset(offset_yNeighbour),
                        voxelChunks[i].XNeighbour.YNeighbour.Voxels[0].ToNewVoxelFromOffset(offset_xyNeighbour)
                    };
                    TriangulateCell(voxels, ref meshData, ref vertex_i);
                }

                meshDataChunks[i] = meshData;
            }
        }
        private void TriangulateCell(Voxel2D[] voxels, ref MeshData meshData, ref int vertex_i)
        {
            // Define cell-based variables
            byte cell_VertexCount = 0;
            byte cell_Mask = 0;
            byte workingBit = 0b_0001;

            Vector3[] edgePositions = new Vector3[]
            {
                PointBetweenVoxels(voxels, 0, 1),
                PointBetweenVoxels(voxels, 0, 2),
                PointBetweenVoxels(voxels, 1, 3),
                PointBetweenVoxels(voxels, 2, 3)
            };

            // Check corners and define cell type
            workingBit = 0b_0001;
            for (int v = 0; v < voxels.Length; v++)
            {
                if (voxels[v].Value > 0)
                {
                    cell_Mask |= workingBit;
                    AddVertexToMeshData(voxels[v].Position, ref meshData, ref cell_VertexCount);
                }
                workingBit <<= 1;
            }

            // Use cell type to plce edge verticies
            byte edgeMask = TriangulationData.IntersectionPoints[cell_Mask];
            workingBit = 0b_0001;
            for (int v = 0; v < edgePositions.Length; v++)
            {
                if (edgeMask.Contains(workingBit))
                {
                    AddVertexToMeshData(edgePositions[v], ref meshData, ref cell_VertexCount);
                }
                workingBit <<= 1;
            }

            // Stitch vertecies to create tris
            var triOrder = TriangulationData.TriangleFormations[cell_Mask];
            foreach (byte cellVertexIndex in triOrder)
            {
                meshData.Tris.Add(vertex_i + cellVertexIndex);
            }

            // Inriment indicies
            vertex_i += cell_VertexCount;
        }
        private void AddVertexToMeshData(Vector3 vert, ref MeshData meshData, ref byte vertex_i)
        {
            meshData.Verts.Add(vert);
            vertex_i++;
        }
        private Vector3 PointBetweenVoxels(Voxel2D[] voxelArray, int a, int b)
        {
            if (a >= voxelArray.Length || b >= voxelArray.Length)
                throw new VoxelDensityException("Indicies a or b are out of range of voxel array " + voxelArray.ToString());
            return PointBetweenVoxels(voxelArray[a], voxelArray[b]);
        }
        private Vector3 PointBetweenVoxels(Voxel2D a, Voxel2D b) => Vector3.Lerp(a.Position, b.Position, halfpoint);
        #endregion
    }
}