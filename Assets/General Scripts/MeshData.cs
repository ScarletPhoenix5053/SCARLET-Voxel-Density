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

        var normals = new Vector3[Verts.Count];
        for (int i = 0; i < normals.Length; i++) normals[i] = Vector3.back;       

        return newMesh;
    }
}
