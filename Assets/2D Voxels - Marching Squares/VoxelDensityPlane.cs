using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    private VoxelChunk2D[] voxelChunks;

    private const float colliderDepth = 0.1f;
    private const float debug_stepTime = 0.1f;

    private VoxelBrush2D activeBrush = new VoxelBrush2D();
    private IEnumerator activeEditRoutine = null;
    #endregion

    #region Unity Messages

    private void Awake()
    {
        // Define Default brush
        activeBrush.ValueDirectionPairs = new VoxelDirectionValuePair2D[]
        {
            new VoxelDirectionValuePair2D(0,0,1),
            new VoxelDirectionValuePair2D(-1,0,1),
            new VoxelDirectionValuePair2D(-2,0,1),
            new VoxelDirectionValuePair2D(-3,0,1),
            new VoxelDirectionValuePair2D(-1,1,1),
            new VoxelDirectionValuePair2D(0,1,1),
            new VoxelDirectionValuePair2D(1,1,1),
            new VoxelDirectionValuePair2D(1,0,1),
            new VoxelDirectionValuePair2D(2,0,1),
            new VoxelDirectionValuePair2D(3,0,1),
            new VoxelDirectionValuePair2D(1,-1,1),
            new VoxelDirectionValuePair2D(0,-1,1),
            new VoxelDirectionValuePair2D(-1,-1,1)
        };

        // Generate chunks
        InitChunks();

    }

    private void Update()
    {
        EditChunks();
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

    private void EditChunks()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0))
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

                // Modify voxels based on active brush
                for (int i = 0; i < activeBrush.ValueDirectionPairs.Length; i++)
                {
                    // Establish where the voxel is in its chunk
                    int voxel_y = System.Math.DivRem(closestVoxelIndex, Resolution + 1, out int voxel_x);
                    int chunk_editI = closestChunkIndex;

                    //Get this chunk's x and y indicies
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
                        activeBrush.ValueDirectionPairs[i].YDir, 
                        dimensionOffset: ChunkCountX,
                        ref chunk_editI,
                        out int editYI,
                        ref abort
                        );
                    TryEditVoxelOnAxis(
                        voxel_x, chunk_x,
                        activeBrush.ValueDirectionPairs[i].XDir,
                        dimensionOffset: 1, 
                        ref chunk_editI, 
                        out int editXI, 
                        ref abort);
                    
                    if (!abort)
                        voxelChunks[chunk_editI]
                            .Voxels[editXI + (editYI * (Resolution + 1))]
                                .Value = activeBrush.ValueDirectionPairs[i].Value;
               }
            }
        }

        // end the routine & allow it to operate after
        activeEditRoutine = null;
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