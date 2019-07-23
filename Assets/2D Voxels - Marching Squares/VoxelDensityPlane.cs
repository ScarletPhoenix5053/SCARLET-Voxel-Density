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
            new VoxelDirectionValuePair2D(-1,1,1),
            new VoxelDirectionValuePair2D(0,1,1),
            new VoxelDirectionValuePair2D(1,1,1),
            new VoxelDirectionValuePair2D(1,0,1),
            new VoxelDirectionValuePair2D(1,-1,1),
            new VoxelDirectionValuePair2D(0,-1,1),
            new VoxelDirectionValuePair2D(-1,-1,1)
        };

        // Generate chunks
        InitChunks();

    }

    private void Update()
    {
        if (activeEditRoutine == null)
        {
            activeEditRoutine = EditChunks();
            StartCoroutine(activeEditRoutine);
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

    private IEnumerator EditChunks()
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
                    // Iterate one cycle, only after space is pressed
                    yield return new WaitForSeconds(debug_stepTime);

                    var valueDir = activeBrush.ValueDirectionPairs[i];

                    // Establish where the voxel is in its chunk
                    int y = System.Math.DivRem(closestVoxelIndex, Resolution + 1, out int x);

                    //Debug.Log("Original: " + x + " " + y);

                    // Find position of voxel to edit
                    int editChunkI = closestChunkIndex;
                    int editXI = x + valueDir.XDir;
                    int editYI = y + valueDir.YDir;

                    // Ensure edit voxel is in row:

                    // Define row boundaries in each chunk
                    int rowMin = editYI * (Resolution + 1);
                    int rowMax = rowMin + Resolution;

                    /*
                    Debug.Log(
                        " x: " + editXI +
                        " y: " + editYI +
                        " min: " + rowMin + 
                        " max: " + rowMax
                        );
                        */

                    // If outside boundaries:
                    var xIsOutOfBounds = editXI < 0 || Resolution < editXI;
                    var yIsOutOfBounds = editYI < 0 || Resolution < editYI;
                    if (xIsOutOfBounds || yIsOutOfBounds)
                    {
                        //Debug.Log("x is Out of row");

                        // Find appropriate chunk along x
                        // break dist from row end down to chunks + indv voxels. indv voxels must be no greater than res+1. chunks away starts at 1
                        int chunkOffsetX = 0;
                        {
                            var divnd = editXI;
                            var divsr = Resolution + 1;
                            chunkOffsetX = System.Math.DivRem(divnd, divsr, out int posInNewChunk);
                            chunkOffsetX += (int)Mathf.Sign(editXI) * 1;
                            if (chunkOffsetX > 0) chunkOffsetX--;
                        }

                        int chunkOffsetY = 0;
                        {
                            var divnd = editYI;
                            var divsr = Resolution + 1;
                            chunkOffsetY = System.Math.DivRem(divnd, divsr, out int posInNewChunk);
                            chunkOffsetY += (int)Mathf.Sign(editYI) * 1;
                            if (chunkOffsetY > 0) chunkOffsetY--;
                        }

                        //Debug.Log(q);

                        // Locate current chunk's x and y
                        var cx = 0;
                        var cy = 0;
                        for (int ci = 0; ci < (ChunkCountX) * ChunkCountY; ci++)
                        {
                            if (ci == editChunkI) break;

                            cx++;

                            if (cx == ChunkCountX)
                            {
                                cy++;
                                cx = 0;
                            }
                        }

                        // Apply changes to editCI if chunk can be found
                        int cRowMin = cy * ChunkCountY;
                        int cRowMax = cRowMin + ChunkCountY - 1;
                        int editCX = cx + chunkOffsetX;
                        int editCY = cy + chunkOffsetY;
                        
                        Debug.Log(
                            " cx: " + cx +
                            " cy: " + cy +
                            " cmin: " + cRowMin +
                            " cmax: " + cRowMax +
                            " ceditx: " + editCX +
                            " cedity: " + editCY
                            );
                        
                        // limit / load x in neighbour chunk
                        if (editCX < 0 || ChunkCountX <= editCX)
                        {
                            Debug.Log(editCX + "x is out of bounds");
                            // stop edit if point is out of bounds
                            continue;
                        }
                        else
                        {
                            editChunkI += chunkOffsetX;

                            Debug.Log("edX (1) " + editXI);

                            if (editXI < 0)
                            {
                                editXI += Mathf.Abs(chunkOffsetX) * (Resolution + 1);
                            }
                            else
                            {
                                editXI -= Mathf.Abs(chunkOffsetX) * (Resolution + 1);
                            }

                            Debug.Log("edX (2) " + editXI);
                        }

                        // limit / load y in neighbour chunk
                        if (editCY < 0 || ChunkCountY <= editCY)
                        {
                            Debug.Log(editYI + "y is out of bounds");
                            // stop edit if point is out of bounds
                            continue;
                        }
                        else
                        {
                            editChunkI += chunkOffsetY * ChunkCountX;

                            Debug.Log("edY (1) " + editYI);

                            
                            if (editYI < 0)
                            {
                                editYI += Mathf.Abs(chunkOffsetY) * (Resolution + 1);
                            }
                            else
                            {
                                editYI -= Mathf.Abs(chunkOffsetY) * (Resolution + 1);
                            }

                            Debug.Log("edY (2) " + editYI);
                        }
                    }
                    voxelChunks[editChunkI].Voxels[editXI + (editYI * (Resolution + 1))].Value = valueDir.Value;
                }
            }
        }

        // end the routine & allow it to operate after
        activeEditRoutine = null;
        yield return null;

    }

    private bool SpaceWasPressed() => Input.GetKeyDown(KeyCode.Space);

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

    private const float gizmoSize = 0.1f;
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