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
        
        [Range(1, 8)] public int Resolution = 1;        
        public int ChunkCountX = 2;
        public int ChunkCountY = 2;
        public float ChunkSize = 10f;

        #endregion

        #region Immutable / Private Variables
        
        private const float colliderDepth = 0.1f;
        private const float halfpoint = 0.5f;
        private const float gizmoSize = 0.05f;

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
            
            // Init
            voxelChunks = GenerateChunks(ChunkCountX, ChunkCountY, ChunkSize);
        }

        private void OnDrawGizmos()
        {
            if (voxelChunks != null)
            {
                foreach (VoxelChunk2D chunk in voxelChunks)
                {
                    foreach (Voxel2D voxel in chunk.Voxels)
                    {
                        Gizmos.color = new Color(voxel.Value, voxel.Value, voxel.Value);
                        Gizmos.DrawSphere(voxel.Position + chunk.Position, gizmoSize);
                    }
                }
            }
        }

        #endregion

        #region Operation Methods

        public void ApplyVoxelBrush(Vector3 point, VoxelBrush2D brush)
        {
            EditChunkDataAtPoint(point, brush, voxelChunks);

            TriangulateData(voxelChunks, out MeshData[] meshDataChunks);
            for (int i = 0; i < voxelChunks.Length; i++)
            {
                voxelChunks[i].MeshFilter.mesh = meshDataChunks[i].ToMesh();
            }
        }

        #endregion

        #region Working Methods

        private VoxelChunk2D[] GenerateChunks(int chunkCountX, int chunkCountY, float chunkSize)
        {
            VoxelChunk2D[] chunks = new VoxelChunk2D[chunkCountX * chunkCountY];

            // Locals
            Vector2 chunkSizeVectorHalved = Vector2.one * (chunkSize / 2);
            int i = 0;

            float chunkPosY = FindFirstChunkPos(chunkSize, chunkCountY);
            for (int y = 0; y < chunkCountY; y++)
            {
                float chunkPosX = FindFirstChunkPos(chunkSize, chunkCountX);
                for (int x = 0; x < chunkCountX; x++, i++)
                {
                    // Define location and voxels
                    Vector2 chunkPos = new Vector2(chunkPosX, chunkPosY) + chunkSizeVectorHalved;
                    VoxelChunk2D chunk = new VoxelChunk2D();
                    chunk.Voxels = GenerateVoxelChunk(chunkSize, Resolution, -chunkSizeVectorHalved);
                    chunk.Position = chunkPos;

                    // Create gameobject for collider
                    var chunkGameObj = new GameObject(Constants.ChunkName_Default + " " + x + "/" + y);
                    chunkGameObj.transform.parent = transform;
                    chunkGameObj.transform.position = chunkPos;

                    // Create collider
                    var boxCol = chunkGameObj.AddComponent<BoxCollider>();
                    boxCol.size = new Vector3(chunkSize, chunkSize, colliderDepth);
                    chunk.Collider = boxCol;

                    // Create mesh components
                    chunk.MeshFilter = chunkGameObj.AddComponent<MeshFilter>();
                    chunk.MeshRenderer = chunkGameObj.AddComponent<MeshRenderer>();
                    chunk.MeshRenderer.sharedMaterial = CommonReferences.DefaultMaterial;

                    // Incriemnt
                    chunks[i] = chunk;
                    chunkPosX += chunkSize;

                    // Assign self as neighbour to relevant chunks
                    if (x != 0) chunks[i - 1].XNeighbour = chunks[i];
                    if (y != 0) chunks[i - chunkCountX].YNeighbour = chunks[i];
                }
                chunkPosY += chunkSize;
            }

            return chunks;
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
        private float FindFirstChunkPos(float chunkSize, int chunkCount) => -(chunkSize * chunkCount) / 2;

        private void EditChunkDataAtPoint(Vector3 point, VoxelBrush2D brush, VoxelChunk2D[] voxelChunks)
        {
            // Find closest voxel to hit point
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
            int voxel_Y = System.Math.DivRem(voxel_closestI, Resolution + 1, out int voxel_X);

            // Modify voxels based on active brush
            for (int i = 0; i < brush.ValueDirectionPairs.Length; i++)
            {
                //Get this chunk's x and y indicies
                int chunk_editI = chunk_closestI;
                int chunk_X = 0;
                int chunk_Y = 0;
                for (int chunk_i = 0; chunk_i < (ChunkCountX) * ChunkCountY; chunk_i++)
                {
                    if (chunk_i == chunk_editI) break;
                    chunk_X++;

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

                if (!abort)
                    voxelChunks[chunk_editI]
                        .Voxels[editXI + (editYI * (Resolution + 1))]
                            .Value = brush.ValueDirectionPairs[i].Value;
            }
        }
        private void TryEditVoxelOnAxis(int voxel_N, int chunk_N, int nDirOffset, int dimensionOffset, out int voxel_editNI, ref int chunk_editI,  ref bool abort)
        {
            voxel_editNI = voxel_N + nDirOffset;
            
            if (voxel_editNI < 0 || Resolution < voxel_editNI)
            {
                // Get offset pos of chunk along n axis
                var chunk_offsetNI =
                    System.Math.DivRem(
                        voxel_editNI,
                        Resolution + 1,
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
                    chunk_editI += chunk_offsetNI * dimensionOffset;

                    if (voxel_editNI < 0) voxel_editNI += Mathf.Abs(chunk_offsetNI) * (Resolution + 1);
                    else voxel_editNI -= Mathf.Abs(chunk_offsetNI) * (Resolution + 1);
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
            byte edgeMask = TriangulationData2D.IntersectionPoints[cell_Mask];
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
            var triOrder = TriangulationData2D.TriangleFormations[cell_Mask];
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