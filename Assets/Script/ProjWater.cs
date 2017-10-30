using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjWater : MonoBehaviour
{
    // Number of row in mesh
    public int m_row;

    // Number of column in mesh
    public int m_column;

    // The base hight of the sea
    public float m_baseHeight;

    // The amplitude of the wave, use for determin the total sea bound
    public float m_waveAmplitude;

    // Girds that consist of the sea mesh
    private List<Vector4> _grid;

    [SerializeField]
    private Camera _camera;                   

    // The lowest hight of the sea
    private float _lowerHeight;

    // The upper hight of the sea
    private float _upperHeight;

    // Camera frustum intersect with the water bounding box
    private List<Vector3> _camIntersection;

    // The matrix that transfer a point in clip space to world space
    private Matrix4x4 _projectMatrix;

    
    // Use for render
    private Mesh _mesh;
    private MeshFilter _meshFilter;
    private int[] _triangles;
    private List<Vector3> _vertices;
    private List<Vector3> _normals;

    private bool _canRenderer;

    private void Start()
    {
        SetInitReference();

        _lowerHeight = m_baseHeight + m_waveAmplitude;
        _upperHeight = m_baseHeight - m_waveAmplitude;

        AdjustAimProjector();

        SetCameraInterSection();
        if(_camIntersection.Count == 0)
        {
            _canRenderer = false;
        }
        else
        {
            _canRenderer = true;
        }

        // Get grid point in world space
        float farRowInterval = 2 * _camera.farClipPlane / m_row;
        float farColInterval = 2 * _camera.farClipPlane / m_column;
        float nearRawInterval = 2 * _camera.nearClipPlane / m_row;
        float nearColInterval = 2 * _camera.nearClipPlane / m_column;
        _grid = new List<Vector4>((m_column + 1) * (m_row + 1));
        for (int i = 0; i < m_row + 1; i++)
        {
            for (int j = 0; j < m_column + 1; j++)
            {
                Vector4 upperPoint = new Vector4(
                        i * nearRawInterval - _camera.nearClipPlane, 
                        j * nearColInterval - _camera.nearClipPlane,
                        -_camera.nearClipPlane, _camera.nearClipPlane);
                upperPoint = _projectMatrix * upperPoint;

                Vector4 underPoint = new Vector4(
                        i * farRowInterval - _camera.farClipPlane, 
                        j * farColInterval - _camera.farClipPlane,
                        _camera.farClipPlane, _camera.farClipPlane);
                underPoint = _projectMatrix * underPoint;

                float t = (upperPoint.y - m_baseHeight) / (upperPoint.y - underPoint.y);
                _grid[i] = (Vector4.Lerp(upperPoint, underPoint, t));
            }
        }


        RenderGrid(_grid);
    }

    private void SetInitReference()
    {
        _grid = new List<Vector4>(new Vector4[(m_column + 1) * (m_row + 1)]);
                                                 
        _camIntersection = new List<Vector3>();

        _mesh = new Mesh();
        _mesh.MarkDynamic();

        _meshFilter = GetComponent<MeshFilter>();

        _vertices = new List<Vector3>(new Vector3[(m_column + 1) * (m_row + 1)]);
        _triangles = new int[6 * m_column * m_row];
        _normals = new List<Vector3>(new Vector3[(m_column + 1) * (m_row + 1)]);
    }
                                                 

    /// <summary>
    /// Step 1: Create a custom aiming projector to avoid backfiring.
    /// </summary>
    /// <returns></returns>
    private void AdjustAimProjector()
    {
        // not adjust view matrix to simplify
        Matrix4x4 Mpview = GetViewMatrix(_camera);
        Matrix4x4 Mperspective = GetFrustumMatrix(_camera);

        _projectMatrix = Matrix4x4.Inverse(Mperspective * Mpview);
    }

    /// <summary>
    /// Step 2: Get all points that intersection between the edge of the camera frustum and bound plane
    /// </summary>
    /// <returns></returns>
    private void SetCameraInterSection()
    {
        List<Vector3> camCorners = GetCameraCornerInWorld();

        _camIntersection.Clear();

        // check for camera's far-near plane connection lines intersection 
        for (int i = 0; i < 8; i += 2)
        {
            float lineHigh = camCorners[i].y;
            float lineLow = camCorners[i + 1].y;

            // check lowerHeight intersection
            if ((lineHigh > _lowerHeight) && (lineLow < _lowerHeight))
            {
                float t = (lineHigh - _lowerHeight) / (lineHigh - lineLow);
                _camIntersection.Add(Vector3.Lerp(camCorners[i], camCorners[i + 1], t));
            }

            // check upperHeight intersection
            if ((lineHigh > _upperHeight) && (lineLow < _upperHeight))
            {
                float t = (lineHigh - _upperHeight) / (lineHigh - lineLow);
                _camIntersection.Add(Vector3.Lerp(camCorners[i], camCorners[i + 1], t));
            }
        }

        // check for camera's far plane intersection 
        for (int i = 1; i < 8; i += 2)
        {
            int start = i;
            int end = (i == 7) ? 1 : i + 2;

            float lineHigh = camCorners[start].y;
            float lineLow = camCorners[end].y;

            // check lowerHeight intersection
            if ((lineHigh > _lowerHeight) && (lineLow < _lowerHeight))
            {
                float t = (lineHigh - _lowerHeight) / (lineHigh - lineLow);
                _camIntersection.Add(Vector3.Lerp(camCorners[start], camCorners[end], t));
            }

            // check upperHeight intersection
            if ((lineHigh > _upperHeight) && (lineLow < _upperHeight))
            {
                float t = (lineHigh - _upperHeight) / (lineHigh - lineLow);
                _camIntersection.Add(Vector3.Lerp(camCorners[start], camCorners[end], t));
            }
        }

        // check for camera corner that in the bound box
        foreach (Vector3 corner in camCorners)
        {
            if ((corner.z > _lowerHeight) && (corner.z < _upperHeight))
            {
                _camIntersection.Add(corner);
            }
        }
    }


    /// <summary>
    /// Step 5: render the grid on the screen
    /// </summary>
    /// <param name="grid"></param>
    private void RenderGrid(List<Vector4> grid)
    {
        if (!_canRenderer) return;

        // Init the vertices and normal
        for (int i = 0; i < grid.Count; i++)
        {
            _normals[i] = Vector3.up;
            _vertices[i] = grid[i];
        }

        // Set triangles
        int triangleCount = 0;
        for (int i = 0; i < m_row; i++)
        {
            for (int j = 0; j < m_column; j++)
            {
                int offset = i * (m_column + 1) + j;
                _triangles[3 * triangleCount] = offset;
                _triangles[3 * triangleCount + 1] = offset + 1;
                _triangles[3 * triangleCount + 2] = offset + m_column + 1;
                triangleCount++;
                _triangles[3 * triangleCount] = offset + 1;
                _triangles[3 * triangleCount + 1] = offset + m_column + 2;
                _triangles[3 * triangleCount + 2] = offset + m_column + 1;
                triangleCount++;
            }
        }
        _mesh.SetVertices(_vertices);
        _mesh.SetTriangles(_triangles, 0);
        _mesh.SetNormals(_normals);

        _meshFilter.mesh = _mesh;
    }


    private Matrix4x4 GetRangeMatrix(List<Vector3> intersectionPoints)
    {
        // Project all intersection point onto base height plane
        for (int i = 0; i < intersectionPoints.Count; i++)
        {
            Vector3 newIntersectionPoint = intersectionPoints[i];
            newIntersectionPoint.y = m_baseHeight;
            intersectionPoints[i] = newIntersectionPoint;
        }

        // Get the range matrix
        float xMax = float.MinValue, xMin = float.MaxValue;
        float zMax = float.MinValue, zMin = float.MaxValue;
        foreach (var intersectionPoint in intersectionPoints)
        {
            if (intersectionPoint.x > xMax) xMax = intersectionPoint.x;
            if (intersectionPoint.x < xMin) xMin = intersectionPoint.x;
            if (intersectionPoint.z > zMax) zMax = intersectionPoint.z;
            if (intersectionPoint.z < zMin) zMin = intersectionPoint.z;
        }

        return new Matrix4x4(
            new Vector4(xMax - xMin, 0, 0, 0),
            new Vector4(0, 1, 0, 0),
            new Vector4(0, 0, zMax - zMin, 0),
            new Vector4(xMin, 0, zMin, 1));
    }

    // Get a camera's frustum matrix
    private Matrix4x4 GetFrustumMatrix(Camera camera)
    {
        float cotFOV = 1f / Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f);
        float far = camera.farClipPlane;
        float near = camera.nearClipPlane;

        var result = new Matrix4x4(
            new Vector4(cotFOV / camera.aspect, 0, 0, 0),
            new Vector4(0, cotFOV, 0, 0),
            new Vector4(0, 0, -(far + near) / (far - near), -1),
            new Vector4(0, 0, -2 * near * far / (far - near), 0));

        return result;
    }

    // Get the transform matrix that transform a world point into camera space point
    private Matrix4x4 GetViewMatrix(Camera camera)
    {
        Vector3 xVector = camera.transform.right;
        Vector3 yVector = camera.transform.up;
        Vector3 zVector = camera.transform.forward;
        Vector3 origin = camera.transform.position;

        Matrix4x4 viewToWorld = new Matrix4x4(
            new Vector4(xVector.x, xVector.y, xVector.z, 0),
            new Vector4(yVector.x, yVector.y, yVector.z, 0),
            new Vector4(zVector.x, zVector.y, zVector.z, 0),
            new Vector4(origin.x, origin.y, origin.z, 1));

        Matrix4x4 nagate = new Matrix4x4(
            new Vector4(1, 0, 0, 0),
            new Vector4(0, 1, 0, 0),
            new Vector4(0, 0, -1, 0),
            new Vector4(0, 0, 0, 1));

        return nagate * Matrix4x4.Inverse(viewToWorld);
    }


    // Get camera corners (+-1, +-1, +-1) in world space
    private List<Vector3> GetCameraCornerInWorld()
    {
        List<Vector3> corners = new List<Vector3>(8);

        for (int x = 0; x <= 1; x++)
        {
            for (int y = 0; y <= 1; y++)
            {
                corners.Add(_camera.ViewportToWorldPoint(
                    new Vector3(x, y, _camera.nearClipPlane)));

                corners.Add(_camera.ViewportToWorldPoint(
                    new Vector3(x, y, _camera.farClipPlane)));
            }
        }

        return corners;
    }


                

}
