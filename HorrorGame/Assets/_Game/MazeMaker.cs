using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class MazeMaker : MonoBehaviour
{
    [Header("Maze Configuration")]
    [SerializeField] private Vector2Int _gridSize = new(2, 2); //this is the size of the grid
    [SerializeField] private Vector2 _cellSize = new(5f, 5f); //this the size of each cell in the maze
    
    [Header("Spawnable Prefabs")] 
    [SerializeField] private GameObject hedgePrefab;
    
    [Header("Debug")]
    [SerializeField] private bool drawDebugGrid = true;
    [SerializeField] private float debugYOffset = -2f;


    private bool[,] horizEdgeExists;
    private bool[,] vertEdgeExists;

    void Start() {
        horizEdgeExists = new bool[_gridSize.x, _gridSize.y + 1];
        vertEdgeExists = new bool[_gridSize.x + 1, _gridSize.y];

        foreach (var (i, j) in HorizontalEdgeIndices()) {
            Vector3 pos = Jitter(HorizontalEdgePosition(i, j));
            Instantiate(hedgePrefab, pos + transform.position, Quaternion.identity, transform);
        }
        
        Quaternion quarterTurn = Quaternion.Euler(0f, 90f, 0f);
        foreach (var (i, j) in VerticalEdgeIndices()) {
            Vector3 pos = Jitter(VerticalEdgePosition(i, j));
            Instantiate(hedgePrefab, pos + transform.position, quarterTurn, transform);
        }
    }

    
    /*
     *  The horizontal edges are indexed like this
     *
     *  +-2,0-+-2,1-+-2,2-+
     *  |     |     |     |
     *  +-1,0-+-1,1-+-1,2-+
     *  |     |     |     |
     *  +-0,0-+-0,1-+-0,2-+
     */
    public Vector3 HorizontalEdgePosition(int x, int y) {
        return new Vector3((x + 0.5f) * _cellSize.x, 0, y * _cellSize.y);
    }
    
    public IEnumerable<Tuple<int, int>> HorizontalEdgeIndices() {
        for (int j = 0; j < _gridSize.y + 1; j++) {
            for (int i = 0; i < _gridSize.x; i++) {
                yield return new Tuple<int, int>(i, j);
            }
        }
    }
    
    /*
     *  The vertical edges are indexed like this
     *
     *  +-----+-----+-----+
     * 1,0   1,1   1,2   0,3
     *  +-----+-----+-----+
     * 0,0   0,1   0,2   0,3
     *  +-----+-----+-----+
     */
    public Vector3 VerticalEdgePosition(int x, int y) {
        return new Vector3(x * _cellSize.x, 0, (y + 0.5f) * _cellSize.y);
    }

    public IEnumerable<Tuple<int, int>> VerticalEdgeIndices() {
        for (int j = 0; j < _gridSize.y; j++) {
            for (int i = 0; i < _gridSize.x + 1; i++) {
                yield return new Tuple<int, int>(i, j);
            }
        }
    }

    private static Vector3 Jitter(Vector3 v) {
        // this function prevents z-fighting between intersecting meshes in the same plane
        const float range = 0.002f;
        float x = Random.Range(-range, range);
        float y = Random.Range(-range, range);
        float z = Random.Range(-range, range);
        return v + new Vector3(x, y, z);
    }

    private void OnDrawGizmos() {
        // draws a grid in the editor that is the same size as the grid that will spawn in at runtime
        if (!drawDebugGrid) return;

        Gizmos.color = Color.purple;
        List<Vector3> points = new List<Vector3>();

        float startX = 0;
        float endX = _gridSize.x * _cellSize.x;
        float startZ = 0;
        float endZ = _gridSize.y * _cellSize.y;
        
        // this offset of half of a cell is applied to every point
        Vector3 offset = transform.position + Vector3.up * debugYOffset;
        
        // calculate the lines going along the z-axis
        for (int i = 0; i <= _gridSize.x; i++) {
            float x = _cellSize.x * i;
            points.Add(offset + new Vector3(x, 0, startZ));
            points.Add(offset + new Vector3(x, 0, endZ));
        }
        
        // calculate the lines going along the x-axis
        for (int j = 0; j <= _gridSize.y; j++) {
            float z = _cellSize.y * j;
            points.Add(offset + new Vector3(startX, 0, z));
            points.Add(offset + new Vector3(endX, 0, z));
        }

        // draw the lines all at once
        ReadOnlySpan<Vector3> pointSpan = new(points.ToArray());
        Gizmos.DrawLineList(pointSpan);
    }
}
