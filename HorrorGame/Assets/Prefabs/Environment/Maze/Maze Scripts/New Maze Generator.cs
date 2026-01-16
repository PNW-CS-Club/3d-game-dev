using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NewMazeGenerator : MonoBehaviour
{
    [SerializeField]
    private MazeCell _mazeCellPrefab;
    
    [Header("Maze Configuration")]
    [SerializeField] private Vector2Int _gridSize; //this is the size of the grid or of the board

    [SerializeField] private Vector2 _cellSize = new(6f, 6f); //this the size of each cell in the maze
    [SerializeField] private int entranceOffset;

    [Header("Spawnable Prefabs")]
    [SerializeField]
    private GameObject podiumPrefab;

    [SerializeField]
    private GameObject redKeyPrefab;

    [SerializeField]
    private GameObject greenKeyPrefab;

    [SerializeField]
    private GameObject blueKeyPrefab;

    [SerializeField]
    private GameObject blackKeyPrefab;

    [SerializeField]
    private GameObject exitDoorPrefab;

    [Header("Key Settings")]
    [SerializeField] private float podiumYOffset;
    [SerializeField] private Vector3 keyOffset;
    private Vector3 podiumOffset;

    private MazeCell[,] _mazeGrid; //this will hold the grid of cells

    
    IEnumerator Start()
    {
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        
        podiumOffset = Vector3.up * podiumYOffset; // Vector3.up is shorthand for writing Vector3(0, 1, 0)
        
        _mazeGrid = new MazeCell[_gridSize.x, _gridSize.y];

        for(int x = 0; x < _gridSize.x; x++)
        {
            for(int z = 0; z < _gridSize.y; z++)
            {
                _mazeGrid[x, z] = Instantiate(_mazeCellPrefab, new Vector3(x * _cellSize.x, 0, z * _cellSize.y), Quaternion.identity, transform); //this will actually create the cell and store it in the _mazeGrid array
                _mazeGrid[x, z].x = x;
                _mazeGrid[x, z].z = z;
            }
        }

        yield return GenerateMaze(null, _mazeGrid[entranceOffset, 0]);
        
        MazeCell entranceCell = _mazeGrid[entranceOffset, 0];
        entranceCell.ClearBackWall();
        
        MazeCell exitCell = _mazeGrid[entranceOffset, _gridSize.y - 1];
        exitCell.ClearFrontWall();

        SpawnObjects();

    }

    
    private IEnumerator GenerateMaze(MazeCell previousCell, MazeCell currentCell) //this method will called recursilvey to make sure that everything has been visited in teh maze
    {
        currentCell.Visit(); //this will make all the current walls visible
        ClearWalls(previousCell, currentCell);

        yield return new WaitForSeconds(0.01f);

        MazeCell nextCell;

        do //this loop will keep on going until there is not unvisisted neighbors remaining
        {
            nextCell = GetNextUnvisitedCell(currentCell);

            if(nextCell != null)
            {
                yield return GenerateMaze(currentCell, nextCell); //if this is not null it will recursly call teh generate maze function
            }
        } while(nextCell != null);
    }

    
    private MazeCell GetNextUnvisitedCell(MazeCell currentCell) //this will allow the current cell to move onto a neighboring cell, it will return a random neighboring cell at random
    {
        var unvisitedCells = GetUnvisitedCells(currentCell);

        // return a random unvisited cell
        return unvisitedCells.OrderBy(_ => Random.Range(1,10)).FirstOrDefault();

        //use the link to order the list randomly
        //takes the unvisited neighbor cells
        //assing them each a random number
        //sort them by the random number
        //pick the first one that gives a random unvisted neighbor or null
        //only return one thing
    }

    
    private IEnumerable<MazeCell> GetUnvisitedCells(MazeCell currentCell) //this will return all the unvisited neighbors, curerntCell will check around to see all the unvisited neighbors
    {
        int x = currentCell.x;
        int z = currentCell.z;

        //x always you to move left and right 
        
        if(x + 1 < _gridSize.x) //this will check if the next cell to the right is within the bounds of the grid
        {
            var cellToRight = _mazeGrid[x+1, z];

            if(cellToRight.IsVisited == false) //check if the cell to the right has been visited
            {
                yield return cellToRight; //if it has not been visited will return the results
            }
        }

        if(x - 1 >= 0) //check to see if the within the bonds of 0
        {
            var cellToLeft = _mazeGrid[x-1, z];

            if(cellToLeft.IsVisited == false) //check if the cell to the left has been visited
            {
                yield return cellToLeft;
            }
        }

        if(z + 1 < _gridSize.y)
        {
            var cellToFront = _mazeGrid[x, z + 1];

            if(cellToFront.IsVisited == false)
            {
                if(cellToFront.IsVisited == false) //check if the cell above/front has been visited
                {
                    yield return cellToFront;
                }
            }
        }

        if(z - 1 >= 0)
        {
            var cellToBack = _mazeGrid[x, z - 1];

            if(cellToBack.IsVisited == false) //check if the cell below/back has been visited
            {
                yield return cellToBack;
            }
        }
    }

    
    private void ClearWalls(MazeCell previousCell, MazeCell currentCell) //the previous cell is following behind the current cell
    {
        //IMPORTANT
        //have to implement here where an entrance can be created?
        //will find divide both x and z by half and that will give you the entrance 
        if(previousCell == null)
        {
            return;
        }

        if(previousCell.x < currentCell.x) //the previous cell will check if it's to the left of the current one and if it is we know that the algorithm has gone from LEFT TO RIGHT
        {
            previousCell.ClearRightWall(); //since it's gone from left to right, prev to current, we clear the prev right wall and the current left wall
            currentCell.ClearLeftWall();
            return;
        }

        if(previousCell.x > currentCell.x) //the previous cell will check if it's to the right of the current one and if it is we know that the algorithm has gone from RIGHT TO LEFT
        {
          previousCell.ClearLeftWall(); //clears current right wall and prev left wall
          currentCell.ClearRightWall();  
        }

        if(previousCell.z < currentCell.z) //the previous cell will check if it's above the current one and if it is we know that the algorithm has gone from BACK TO FRONT
        {
            previousCell.ClearFrontWall(); //clears the current back wall and prev front wall
            currentCell.ClearBackWall();
        }

        if(previousCell.z > currentCell.z) //the previous cell will check if it's below the current one and if it is we know that the algorithm has gone from FRONT TO BACK
        {
            previousCell.ClearBackWall(); //clears the current front wall and the prev back wall
            currentCell.ClearFrontWall();
        }
    }

    
    private void SpawnObjects()
    {
        /*** Determine grid dimensions ***/
        
        int x = _gridSize.x - 1;
        int z = _gridSize.y - 1;

        float gridMaxX = x * _cellSize.x;
        float gridMaxZ = z * _cellSize.y;

        
        /*** Spawn the exit door ***/
        
        Vector3 doorSpawnpoint = new Vector3(entranceOffset * _cellSize.x, 0, gridMaxZ);

        //spawn point for the exit door
        GameObject exitDoorObject = Instantiate(exitDoorPrefab, doorSpawnpoint, Quaternion.identity, transform);
        DoorScript doorScript = exitDoorObject.GetComponent<DoorScript>();
        //we're storing the door that we have created in the 'exitDoorObject' variable 
        //with the exitDoorObject we're getting the script that is attached to that door and storing it in 'doorScript'
        // The script is an object that has the type of 'DoorScript'

        
        /*** Spawn the four keys ***/
        
        Vector3 spawnpoint1 = new Vector3(0,0,0);
        Vector3 spawnpoint2 = new Vector3(0, 0, gridMaxZ);
        Vector3 spawnpoint3 = new Vector3(gridMaxX, 0, 0);
        Vector3 spawnpoint4 = new Vector3(gridMaxX, 0, gridMaxZ);
        
        SpawnKeyAndPodium(redKeyPrefab, "Red Key", spawnpoint1, doorScript);
        SpawnKeyAndPodium(greenKeyPrefab, "Green Key", spawnpoint2, doorScript);
        SpawnKeyAndPodium(blueKeyPrefab, "Blue Key", spawnpoint3, doorScript);
        SpawnKeyAndPodium(blackKeyPrefab, "Black Key", spawnpoint4, doorScript);
    }

    
    private void SpawnKeyAndPodium(GameObject keyPrefab, string keyObjectName, Vector3 spawnPoint, DoorScript doorScript) 
    {
        // spawn the podium for the key to sit on
        Instantiate(podiumPrefab, spawnPoint + podiumOffset, Quaternion.identity, transform);
        
        // spawn an instance of the given key prefab on top of the podium
        GameObject keyObject = Instantiate(keyPrefab, spawnPoint + keyOffset, Quaternion.identity, transform);
        keyObject.name = keyObjectName; // rename the key object in the scene to the name passed into this function
        
        // get the keyscript, and inside that keyscript, store a reference to the doorScript
        KeyScript keyScript = keyObject.GetComponent<KeyScript>();
        keyScript.doorScript = doorScript; //the = doorScript is telling it which door to control which is the one door
    }
}
