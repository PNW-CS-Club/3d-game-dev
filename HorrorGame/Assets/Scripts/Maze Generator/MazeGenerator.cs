using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class MazeGenerator : MonoBehaviour
{
    private class Cell
    {
        public bool visited = false;
        public bool[] status = new bool[4]; // the status that tracks up/down/left/right
    }

    public Vector2Int size;
    public int startPos = 0;
    
    public GameObject CornerPath;
    public GameObject ThreeWay;
    public GameObject FourWay;
    public GameObject Straight;
    public GameObject DeadEnd;
    public GameObject EmptyObject; // this one can just hold a prefab with a single empty gameobject
    
    public Vector3 offset;
    List<Cell> board;

    // this will hold the model and rotation (in degrees) that should be used for every possible value of statuses
    Dictionary<bool4, Tuple<GameObject, float>> modelDict = new();
    
    
    void Start()
    {
        modelDict.Add(new(true, true, true, true), new(FourWay, 0f));
        
        modelDict.Add(new(true, true, true, false), new(ThreeWay, 0f));
        modelDict.Add(new(true, true, false, true), new(ThreeWay, -90f));
        modelDict.Add(new(true, false, true, true), new(ThreeWay, 90f));
        modelDict.Add(new(false, true, true, true), new(ThreeWay, 180f));

        modelDict.Add(new(true, false, false, true), new(CornerPath, 0f)); //this is should be a corner piece 
        modelDict.Add(new(false, false, true, true), new(CornerPath, 90f));
        modelDict.Add(new(true, true, false, false), new(CornerPath, -90f));
        modelDict.Add(new(false, true, true, false), new(CornerPath, 180));

        modelDict.Add(new(true, false, false, false), new(DeadEnd, 0f));
        modelDict.Add(new(false, true, false, false), new(DeadEnd, -90f));
        modelDict.Add(new(false, false, true, false), new(DeadEnd, 90f));
        modelDict.Add(new(false, false, false, true), new(DeadEnd, 180f));

        modelDict.Add(new(true, false, true, false), new(Straight, 0f)); //this will be the regular straight two way
        modelDict.Add(new(false, true, false, true), new(Straight, 90f));
        // ...and so on for all 2^4=16 entries...
        modelDict.Add(new(false,false,false,false), new(EmptyObject, 0f));
        
        TheMazeGenerator();
    }

    void GenerateMaze()
    {
        for (int i = 0; i < size.x; i++)
        {
            for (int j = 0; j < size.y; j++) {
                
                Cell currentCell = board[i * size.x + j];
                
                // retrieve a (GameObject, float) pair from the dictionary based on the 4 statuses
                (GameObject roomToInstantiate, float rotation) = modelDict[new(
                            currentCell.status[0],
                            currentCell.status[1],
                            currentCell.status[2],
                            currentCell.status[3])];
                
                var newRoom = Instantiate(
                    original: roomToInstantiate, 
                    position: new Vector3(i * offset.x, 0, -j * offset.y), 
                    rotation: Quaternion.Euler(0, rotation, 0), 
                    parent: transform
                );

                newRoom.name += " " + i + "-" + j;
            }
        }
    }

    void TheMazeGenerator()
    {
        board = new List<Cell>();

        for (int i = 0; i < size.x; i++) //this will go through the size of the vector
        {
            for (int j = 0; j < size.y; j++)
            {
                board.Add(new Cell());
            }
        }

        int currentCell = startPos; //this will keep track of which position we're at

        Stack<int> path = new Stack<int>(); //this will keep track of the path we made until the cell we're currently at

        int k = 0; //this will keep track in the while loop

        while(k < 1000) //this is the loop that will generate the maze
        {
            k++;

            board[currentCell].visited = true;


            //Check the cell's neighbors
            List<int> neighbors = CheckNeighbors(currentCell);

            if (neighbors.Count == 0)
            {
                if (path.Count == 0)
                {
                    break;
                }
                currentCell = path.Pop();
            } else
            {
                path.Push(currentCell);

                int newCell = neighbors[Random.Range(0, neighbors.Count)];

                if(newCell > currentCell)
                {
                    //down or right, this is where it determines the status of the bool4
                    if(newCell - 1 == currentCell)
                    {
                        board[currentCell].status[2] = true;
                        currentCell = newCell;
                        board[currentCell].status[3] = true;
                    } else
                    {
                        board[currentCell].status[1] = true;
                        currentCell = newCell;
                        board[currentCell].status[0] = true;
                    }
                }
                else
                {
                    //up or left
                    if(newCell + 1 == currentCell)
                    {
                        board[currentCell].status[3] = true;
                        currentCell = newCell;
                        board[currentCell].status[2] = true;
                    } else
                    {
                        board[currentCell].status[0] = true;
                        currentCell = newCell;
                        board[currentCell].status[1] = true;
                    }
                }
            }
        }
        GenerateMaze();
    }
    
    List<int> CheckNeighbors(int cell)
    {
        List<int> neighbors = new List<int>();

        //check the up neighbor of the cell
        if (cell - size.x >= 0 && !board[Mathf.FloorToInt(cell - size.x)].visited)
        {
            neighbors.Add(Mathf.FloorToInt(cell - size.x));
        }
        //check the down neighbor of the cell
        if (cell + size.x < board.Count && !board[Mathf.FloorToInt(cell + size.x)].visited)
        {
            neighbors.Add(Mathf.FloorToInt(cell + size.x));
        }
        //check the right neighbor of the cell
        if ((cell + 1) % size.x != 0 && !board[Mathf.FloorToInt(cell + 1)].visited)
        {
            neighbors.Add(Mathf.FloorToInt(cell + 1));
        }
        //check the left neighbor of the cell
        if (cell % size.x != 0 && !board[Mathf.FloorToInt(cell - 1)].visited)
        {
            neighbors.Add(Mathf.FloorToInt(cell - 1));
        }

        return neighbors;
    }
}
