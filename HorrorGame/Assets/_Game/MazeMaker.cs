using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class MazeMaker : MonoBehaviour
{
    [Header("Maze Configuration")]
    [SerializeField] private Vector2Int gridSize = new(2, 2); //this is the size of the grid
    [SerializeField] private Vector2 cellSize = new(5f, 5f); //this the size of each cell in the maze
    [SerializeField] private List<Vector2Int> openHorizWalls = new();
    [SerializeField] private List<Vector2Int> openVertWalls = new();
    [SerializeField] private List<Vector2Int> closedHorizWalls = new();
    [SerializeField] private List<Vector2Int> closedVertWalls = new();
    
    [Header("Spawnable Prefabs")] 
    [SerializeField] private GameObject hedgePrefab;
    
    [Header("Debug")]
    [SerializeField] private bool drawDebugGrid = true;
    [SerializeField] private float debugYOffset = -1.99f;

    enum EdgeState { Undecided = 0, Closed = 1, Open = 2 }

    private EdgeState[,] horizEdgeStates;
    private EdgeState[,] vertEdgeStates;

    void Start() {
        horizEdgeStates = new EdgeState[gridSize.x, gridSize.y + 1];
        vertEdgeStates = new EdgeState[gridSize.x + 1, gridSize.y];
        
        Debug.Log(horizEdgeStates[0, 0]);

        GenerateEdges();

        foreach (var (i, j) in HorizontalEdgeIndices()) {
            if (horizEdgeStates[i, j] == EdgeState.Open) continue;
            Vector3 pos = Jitter(HorizontalEdgePosition(i, j));
            Instantiate(hedgePrefab, pos + transform.position, Quaternion.identity, transform);
        }
        
        Quaternion quarterTurn = Quaternion.Euler(0f, 90f, 0f);
        foreach (var (i, j) in VerticalEdgeIndices()) {
            if (vertEdgeStates[i, j] == EdgeState.Open) continue;
            Vector3 pos = Jitter(VerticalEdgePosition(i, j));
            Instantiate(hedgePrefab, pos + transform.position, quarterTurn, transform);
        }
    }


    public void GenerateEdges() {
        // close off all outside walls
        for (int x = 0; x < gridSize.x; x++) {
            horizEdgeStates[x, 0] = EdgeState.Closed;
            horizEdgeStates[x, gridSize.y] = EdgeState.Closed;
        }
        for (int y = 0; y < gridSize.y; y++) {
            vertEdgeStates[0, y] = EdgeState.Closed;
            vertEdgeStates[gridSize.x, y] = EdgeState.Closed;
        }

        // open up user-supplied walls
        foreach (var horizWall in openHorizWalls) {
            horizEdgeStates[horizWall.x, horizWall.y] = EdgeState.Open;
        }
        foreach (var vertWall in openVertWalls) {
            vertEdgeStates[vertWall.x, vertWall.y] = EdgeState.Open;
        }
        
        // close off user-supplied walls
        foreach (var horizWall in closedHorizWalls) {
            if (horizEdgeStates[horizWall.x, horizWall.y] == EdgeState.Open) {
                Debug.LogWarning("Opened wall was overwritten with closed wall");                
            }
            horizEdgeStates[horizWall.x, horizWall.y] = EdgeState.Closed;
        }
        foreach (var vertWall in closedVertWalls) {
            if (vertEdgeStates[vertWall.x, vertWall.y] == EdgeState.Open) {
                Debug.LogWarning("Opened wall was overwritten with closed wall");                
            }
            vertEdgeStates[vertWall.x, vertWall.y] = EdgeState.Closed;
        }
        
        foreach (var (i, j) in HorizontalEdgeIndices()) {
            if (horizEdgeStates[i, j] == EdgeState.Undecided) {
                horizEdgeStates[i, j] = (Random.Range(0, 2) == 1) ? EdgeState.Open : EdgeState.Closed;
            }
        }
        foreach (var (i, j) in VerticalEdgeIndices()) {
            if (vertEdgeStates[i, j] == EdgeState.Undecided) {
                vertEdgeStates[i, j] = (Random.Range(0, 2) == 1) ? EdgeState.Open : EdgeState.Closed;
            }
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
        return new Vector3((x + 0.5f) * cellSize.x, 0, y * cellSize.y);
    }
    
    public IEnumerable<Tuple<int, int>> HorizontalEdgeIndices() {
        for (int j = 0; j < gridSize.y + 1; j++) {
            for (int i = 0; i < gridSize.x; i++) {
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
        return new Vector3(x * cellSize.x, 0, (y + 0.5f) * cellSize.y);
    }

    public IEnumerable<Tuple<int, int>> VerticalEdgeIndices() {
        for (int j = 0; j < gridSize.y; j++) {
            for (int i = 0; i < gridSize.x + 1; i++) {
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
        float endX = gridSize.x * cellSize.x;
        float startZ = 0;
        float endZ = gridSize.y * cellSize.y;
        
        // this offset of half of a cell is applied to every point
        Vector3 offset = transform.position + Vector3.up * debugYOffset;
        
        // calculate the lines going along the z-axis
        for (int i = 0; i <= gridSize.x; i++) {
            float x = cellSize.x * i;
            points.Add(offset + new Vector3(x, 0, startZ));
            points.Add(offset + new Vector3(x, 0, endZ));
        }
        
        // calculate the lines going along the x-axis
        for (int j = 0; j <= gridSize.y; j++) {
            float z = cellSize.y * j;
            points.Add(offset + new Vector3(startX, 0, z));
            points.Add(offset + new Vector3(endX, 0, z));
        }

        // draw the lines all at once
        ReadOnlySpan<Vector3> pointSpan = new(points.ToArray());
        Gizmos.DrawLineList(pointSpan);

        Gizmos.color = Color.orange;
        foreach (Vector2Int horizWall in openHorizWalls) {
            var pos = HorizontalEdgePosition(horizWall.x, horizWall.y) + offset;
            Gizmos.DrawSphere(pos, 0.2f);
        }
        foreach (Vector2Int vertWall in openVertWalls) {
            var pos = VerticalEdgePosition(vertWall.x, vertWall.y) + offset;
            Gizmos.DrawSphere(pos, 0.2f);
        }
        
        Gizmos.color = Color.green;
        foreach (Vector2Int horizWall in closedHorizWalls) {
            var pos = HorizontalEdgePosition(horizWall.x, horizWall.y) + offset;
            Gizmos.DrawSphere(pos, 0.2f);
        }
        foreach (Vector2Int vertWall in closedVertWalls) {
            var pos = VerticalEdgePosition(vertWall.x, vertWall.y) + offset;
            Gizmos.DrawSphere(pos, 0.2f);
        }
    }
}
