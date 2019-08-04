using UnityEngine;
using System.Collections.Generic;

public struct MeshData
{
    public List<Vector3> Verts;
    public List<int> Tris;

    public Mesh ToMesh()
    {
        var newMesh = new Mesh();

        newMesh.vertices = Verts.ToArray();
        newMesh.triangles = Tris.ToArray();
        newMesh.RecalculateNormals();

        return newMesh;
    }
}
