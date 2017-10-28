using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjWater : MonoBehaviour
{
    public int m_row;

    public int m_column;

    public float m_baseHeight;

    public float m_waveAmplitude;

    
    private List<Vector2> _grid;

    [SerializeField]
    private Camera _camera;

    // The camera eight corner point in world space
    private List<Vector3> _camCornerWorld;

    private float _lowerHeight;
    private float _upperHeight;

    // Camera frustum intersect with the water bounding box
    private List<Vector3> _camIntersection;

    private Matrix4x4 _projectMatrix;

    private void Start()
    {
        _grid = CreateGrid();

        _camCornerWorld = GetCameraCornerInWorld();

        _lowerHeight = m_baseHeight + m_waveAmplitude;
        _upperHeight = m_baseHeight - m_waveAmplitude;

        _camIntersection = GetCameraInterSection();
        //if (_camIntersection.Count == 0) EndRenderer();

        _projectMatrix = AdjustAimProjector();
       

    }

    // Create a grid with x = [0, 1], y = [0, 1]
    private List<Vector2> CreateGrid()
    {
        List<Vector2> grid = new List<Vector2>((m_row + 1) * (m_column + 1));

        for (int row = 0; row < m_row + 1; row++)
        {
            for (int column = 0; column < m_column + 1; column++)
            {
                grid.Add(new Vector2(1.0f * column / m_column,
                                       1.0f * row / m_row));
            }
        }

        return grid;
    }

     
    
    // Create a custom aiming projector to avoid backfiring.
    private Matrix4x4 AdjustAimProjector()
    {
        Vector3 camPosition = _camera.transform.position;
        Vector3 camDirection = (_camera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 1f)) - camPosition);
        camDirection = camDirection.normalized;

        float t = (camPosition.y - _upperHeight) / camDirection.y;
        Vector3 intersectPoint = camPosition - camDirection * t;


        // not adjust view matrix to simplify
        Matrix4x4 Mpview = Matrix4x4.identity;
        Matrix4x4 Mperspective = Matrix4x4.Perspective(_camera.fieldOfView, _camera.aspect,
                            _camera.nearClipPlane, _camera.farClipPlane);
        Matrix4x4 Mprojector = Matrix4x4.Inverse(Mpview * Mperspective);

        // Project all intersection point onto base height plane
        for(int i = 0; i < _camIntersection.Count; i++)
        {
            _camIntersection[i] = Mprojector * _camIntersection
        }

        return _camera.projectionMatrix;
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


    // Get all points that intersection between the edge of the camera frustum and bound plane
    private List<Vector3> GetCameraInterSection()
    {
        List<Vector3> camIntersection = new List<Vector3>();

        // check for camera's far-near plane connection lines intersection 
        for (int i = 0; i < 8; i += 2)
        {
            float lineHigh = _camCornerWorld[i].y;
            float lineLow = _camCornerWorld[i + 1].y;
            float lineLength = lineHigh - lineLow;

            // check lowerHeight intersection
            if ((lineHigh > _lowerHeight) && (lineLow < _lowerHeight))
            {
                float t = (_lowerHeight - lineLow) / lineLength;
                camIntersection.Add(Vector3.Lerp(_camCornerWorld[i], _camCornerWorld[i + 1], t));
            }

            // check upperHeight intersection
            if ((lineHigh > _upperHeight) && (lineLow < _upperHeight))
            {
                float t = (_upperHeight - lineLow) / lineLength;
                camIntersection.Add(Vector3.Lerp(_camCornerWorld[i], _camCornerWorld[i + 1], t));
            }
        }

        // check for camera's far plane intersection 
        for (int i = 1; i < 8; i += 2)
        {
            int start = i;
            int end = (i == 7) ? 1 : i + 2;

            float lineHigh = _camCornerWorld[start].y;
            float lineLow = _camCornerWorld[end].y;
            float lineLength = lineHigh - lineLow;

            // check lowerHeight intersection
            if ((lineHigh > _lowerHeight) && (lineLow < _lowerHeight))
            {
                float t = (_lowerHeight - lineLow) / lineLength;
                camIntersection.Add(Vector3.Lerp(_camCornerWorld[start], _camCornerWorld[end], t));
            }

            // check upperHeight intersection
            if ((lineHigh > _upperHeight) && (lineLow < _upperHeight))
            {
                float t = (_upperHeight - lineLow) / lineLength;
                camIntersection.Add(Vector3.Lerp(_camCornerWorld[start], _camCornerWorld[end], t));
            }
        }

        // check for camera corner that in the bound box
        foreach (Vector3 corner in _camCornerWorld)
        {
            if ((corner.z > _lowerHeight) && (corner.z < _upperHeight))
            {
                camIntersection.Add(corner);
            }
        }

        return camIntersection;
    }



}
