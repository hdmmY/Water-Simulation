using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class WaterSimulator : MonoBehaviour {

    public List<WaterParticle> m_waterParticles;

    public float m_gridSnap = 1;

    public int m_rows = 200;

    public int m_columns = 200;

    private Mesh _waterMesh;
    private MeshFilter _meshFilter;

    private List<Vector3> _vertices;
    private int[] _triangles;
                                           

    private void Start()
    {
        _meshFilter = GetComponent<MeshFilter>();
        _waterMesh = GenerateWaterMesh();
        _meshFilter.mesh = _waterMesh;

        foreach(var wparticle in m_waterParticles)
        {
            wparticle.m_orignTime = Time.time;
        }
    }

    private void Update()
    {
        for(int i = 0; i < _vertices.Count; i++)
        {
            float amplitude = 0f;
            foreach(var wparticle in m_waterParticles)
            {
                amplitude += wparticle.Deviation(_vertices[i], Time.time); 
            }
            _vertices[i] = new Vector3(_vertices[i].x, amplitude, _vertices[i].z);
        }

        _waterMesh.Clear();
        _waterMesh.SetVertices(_vertices);
        _waterMesh.SetIndices(_triangles, MeshTopology.Triangles, 0);
        _waterMesh.RecalculateNormals();
        _meshFilter.mesh = _waterMesh;
    }                      


    private Mesh GenerateWaterMesh()
    {

        _vertices = new List<Vector3>((m_rows + 1) * (m_columns + 1));

        Vector3 origin = transform.position;
        for (int row = 0; row < m_rows + 1; row++)
        {
            for (int column = 0; column < m_columns + 1; column++)
            {
                _vertices.Add(origin + m_gridSnap * new Vector3(column, 0, -row));
            }
        }

        _triangles = new int[(m_rows * m_columns) * 6];
        int _triangleCounter = 0;
        for (int row = 0; row < m_rows; row++)
        {
            int start = row * (m_columns + 1);
            for(int column = 0; column < m_columns; column++)
            {
                int offset = start + column;
                _triangles[_triangleCounter++] = offset;
                _triangles[_triangleCounter++] = offset + 1;
                _triangles[_triangleCounter++] = offset + m_columns + 1;

                _triangles[_triangleCounter++] = offset + 1;
                _triangles[_triangleCounter++] = offset + m_columns + 2;
                _triangles[_triangleCounter++] = offset + m_columns + 1;
            }                
        }

        List<Vector3> normals = new List<Vector3>();
        for(int i = 0; i < _vertices.Count; i++)
        {
            normals.Add(Vector3.up);
        }

        Mesh mesh = new Mesh();
        mesh.name = "Water Mesh";
        mesh.SetVertices(_vertices);
        mesh.SetIndices(_triangles, MeshTopology.Triangles, 0);
        mesh.SetNormals(normals);
        mesh.MarkDynamic();

        return mesh;
    }

}
