using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SCARLET.VoxelDensity
{
    #region Voxel Data

    internal class Voxel2D
    {
        internal Vector2 Position;
        internal float Value = 0;

        internal Voxel2D(float posX, float posY)
        {
            Position = new Vector2(posX, posY);
        }
    }
    internal class VoxelChunk2D
    {
        internal Vector2 Position = Vector2.zero;
        internal Voxel2D[] Voxels;
        internal BoxCollider Collider;
    }

    #endregion

    #region Voxel Brush Data

    internal class VoxelBrush2D
    {
        internal VoxelDirectionValuePair2D[] ValueDirectionPairs;

        internal VoxelBrush2D()
        {
            ValueDirectionPairs = new VoxelDirectionValuePair2D[1]
            {
            new VoxelDirectionValuePair2D(0,0,1)
            };
        }
    }
    internal struct VoxelDirectionValuePair2D
    {
        internal int XDir;
        internal int YDir;
        internal float Value;

        internal VoxelDirectionValuePair2D(int xDir, int yDir, float value)
        {
            XDir = xDir;
            YDir = yDir;
            Value = value;
        }
    }

    #endregion

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
        private const float debug_stepTime = 0.1f;

        private VoxelChunk2D[] voxelChunks;

        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;

        private VoxelBrush2D activeBrush = new VoxelBrush2D();
        private IEnumerator activeEditRoutine = null;
        #endregion

        #region Unity Messages

        private void Awake()
        {
            // Find attatched components
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();

            // Define Default brush
            activeBrush.ValueDirectionPairs = new VoxelDirectionValuePair2D[]
            {
                new VoxelDirectionValuePair2D(0,0,1)
            };

            // Generate chunks
            InitChunks();

        }

        private void Update()
        {
            if (Input.GetKey(KeyCode.Mouse0))
            {
                EditChunkDataWithBrush(activeBrush, voxelChunks);
                TriangulateData(voxelChunks, out MeshData meshData);
                meshFilter.mesh = meshData.ToMesh();
            }
        }

        private void OnDrawGizmos()
        {
            if (voxelChunks != null)
            {
                foreach (VoxelChunk2D chunk in voxelChunks)
                {
                    DrawGizmosForVoxels(chunk.Voxels);
                }
            }
        }

        #endregion

        #region Working Methods

        private void InitChunks()
        {
            voxelChunks = new VoxelChunk2D[ChunkCountX * ChunkCountY];
            float chunkPosY = -(ChunkSize * ChunkCountY) / 2;
            for (int y = 0, i = 0; y < ChunkCountY; y++)
            {
                float chunkPosX = -(ChunkSize * ChunkCountX) / 2;
                for (int x = 0; x < ChunkCountX; x++, i++)
                {
                    // Define location and voxels
                    var chunkPos = new Vector2(chunkPosX, chunkPosY);
                    var chunk = new VoxelChunk2D();
                    chunk.Voxels = GenerateVoxelChunk(ChunkSize, Resolution, chunkPos);

                    // Create gameobject for collider
                    var colliderGo = new GameObject("Chunk " + x + "/" + y + " collider");
                    colliderGo.transform.parent = transform;
                    colliderGo.transform.position = chunkPos + new Vector2(ChunkSize / 2, ChunkSize / 2);

                    // Create collider
                    var boxCol = colliderGo.AddComponent<BoxCollider>();
                    boxCol.size = new Vector3(ChunkSize, ChunkSize, colliderDepth);
                    chunk.Collider = boxCol;

                    // Incriemnt
                    voxelChunks[i] = chunk;
                    chunkPosX += ChunkSize;
                }
                chunkPosY += ChunkSize;
            }
        }

        private void EditChunkDataWithBrush(VoxelBrush2D brush, VoxelChunk2D[] voxelChunks)
        {
            // Allow editing of voxel values
            var hit = new RaycastHit();
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit))
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
                        if (Vector3.Distance(inspectedVoxel.Position, hit.point) <
                            Vector3.Distance(closestVoxel.Position, hit.point))
                        {
                            closestVoxel = inspectedVoxel;
                            closestVoxelIndex = vi;
                            closestChunkIndex = ci;
                        }
                    }
                }
                // Establish where the voxel is in its chunk
                int voxel_y = System.Math.DivRem(closestVoxelIndex, Resolution + 1, out int voxel_x);

                Debug.Log(
                    "Closest voxel to mouse is:" +
                    " C:  " + closestChunkIndex +
                    " I: " + closestVoxelIndex +
                    " x/y: " + voxel_x + "," + voxel_y
                    );

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


        private void TriangulateData(VoxelChunk2D[] voxelChunks, out MeshData meshData)
        {
            meshData = new MeshData();
            meshData.Verts = new List<Vector3>();
            meshData.Tris = new List<int>();

            int vertex_i = 0;

            // For each cell
            for (int y = 0; y < Resolution; y++)
            {
                for (int x = 0; x < Resolution; x++)
                {
                    int cellVertexCount = 0;

                    // Identify cell with a bitmask
                    int voxelAIndex = x + y * (Resolution + 1);
                    int voxelBIndex = x + y * (Resolution + 1) + 1;
                    int voxelCIndex = x + (y + 1) * (Resolution + 1);
                    int voxelDIndex = x + (y + 1) * (Resolution + 1) + 1;

                    Voxel2D a = voxelChunks[0].Voxels[voxelAIndex];
                    Voxel2D b = voxelChunks[0].Voxels[voxelBIndex];
                    Voxel2D c = voxelChunks[0].Voxels[voxelCIndex];
                    Voxel2D d = voxelChunks[0].Voxels[voxelDIndex];

                    byte cellMask = 0;

                    /*
                    Debug.Log("Triangulating cell " + x + "," + y);
                    Debug.Log("Voxel Indicies: " + voxelAIndex + " " + voxelBIndex + " " + voxelCIndex + " " + voxelDIndex);
                    Debug.Log("Voxel Values: " + a.Value + " " + b.Value + " " + c.Value + " " + d.Value);
                    */

                    // Must find a way to keep track of which voxels have been added
                    if (a.Value > 0) { cellMask |= 0b_0001; AddVertexToMeshData(a.Position, ref meshData, ref cellVertexCount); Debug.Log("v0"); }
                    if (b.Value > 0) { cellMask |= 0b_0010; AddVertexToMeshData(b.Position, ref meshData, ref cellVertexCount); Debug.Log("v1"); }
                    if (c.Value > 0) { cellMask |= 0b_0100; AddVertexToMeshData(c.Position, ref meshData, ref cellVertexCount); Debug.Log("v2"); }
                    if (d.Value > 0) { cellMask |= 0b_1000; AddVertexToMeshData(d.Position, ref meshData, ref cellVertexCount); Debug.Log("v3"); }

                    //Debug.Log("Bitmask " + System.Convert.ToString(cellMask, 2).ToString().PadLeft(4,'0'));

                    // Create edge veriticies
                    var edgeMask = TriangulationData.IntersectionPoints[cellMask];
                    //Debug.Log("Edge Intersections " + System.Convert.ToString(edgeMask, 2).PadLeft(4, '0'));
                    if (ByteContains(edgeMask, contains: 0b_0001)) AddVertexToMeshData(Vector3.Lerp(a.Position, b.Position, 0.5f), ref meshData, ref cellVertexCount);
                    if (ByteContains(edgeMask, contains: 0b_0010)) AddVertexToMeshData(Vector3.Lerp(a.Position, c.Position, 0.5f), ref meshData, ref cellVertexCount);
                    if (ByteContains(edgeMask, contains: 0b_0100)) AddVertexToMeshData(Vector3.Lerp(b.Position, d.Position, 0.5f), ref meshData, ref cellVertexCount);
                    if (ByteContains(edgeMask, contains: 0b_1000)) AddVertexToMeshData(Vector3.Lerp(c.Position, d.Position, 0.5f), ref meshData, ref cellVertexCount);

                    // Stitch vertecies to create tris
                    var triOrder = TriangulationData.TriangleFormations[cellMask];
                    foreach (byte cellVertexIndex in triOrder)
                    {
                        meshData.Tris.Add(vertex_i + cellVertexIndex);
                    }
                    
                    // Inriment indicies
                    vertex_i += cellVertexCount;
                }
            }
        }
        private void AddVertexToMeshData(Vector3 vert, ref MeshData meshData, ref int vertex_i)
        {
            meshData.Verts.Add(vert);
            vertex_i++;
        }
        private bool ByteContains(byte @byte, byte contains)
        {
            bool byteContains = (@byte & contains) == contains;
            if (byteContains)
            {
                switch (contains)
                {
                    case 0b_0001: Debug.Log("v4"); break;
                    case 0b_0010: Debug.Log("v5"); break;
                    case 0b_0100: Debug.Log("v6"); break;
                    case 0b_1000: Debug.Log("v7"); break;
                    default:
                        break;
                }
            }
            return byteContains;
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

        private const float gizmoSize = 0.3f;
        private void DrawGizmosForVoxels(Voxel2D[] voxels)
        {
            foreach (Voxel2D voxel in voxels)
            {
                Gizmos.color = new Color(voxel.Value, voxel.Value, voxel.Value);
                Gizmos.DrawSphere(voxel.Position, gizmoSize);
            }
        }

        #endregion

    }
}