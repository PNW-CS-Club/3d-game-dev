using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MazeGenerator : MonoBehaviour
{
    public class Cell
    {
        public bool visited = false;
        public bool[] status = new bool[5]; //the status that tracks up down left and right
    }

    public Vector2 size;
    public int startPos = 0;
    public GameObject TwoWay;
    public Vector3 offset;
    List<Cell> board;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        TheMazeGenerator();
    }

    // Update is called once per frame
    void Update()
    {

    }

    void GenerateMaze()
    {
        for(int i = 0; i < size.x; i++)
        {
            for(int j = 0; j < size.y; j++)
            {
                var newRoom = Instantiate(TwoWay, new Vector3(i * offset.x, 0, -j * offset.y), Quaternion.identity, transform);

                newRoom.name += "" + i + "-" + j;
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
            }
            path.Push(currentCell);

            int newCell = neighbors[Random.Range(0, neighbors.Count)];

            if(newCell > currentCell)
            {
                //down or right
                if(newCell - 1 == currentCell)
                {
                    board[currentCell].status[2] = true;
                    currentCell = newCell;
                    board[currentCell].status[3] = true;
                } else
                {
                    board[currentCell].status[1] = true;
                    currentCell = newCell;
                    board[currentCell].status[4] = true;
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
        if ((cell + 1) % size.x != board.Count && !board[Mathf.FloorToInt(cell + 1)].visited)
        {
            neighbors.Add(Mathf.FloorToInt(cell + 1));
        }
        //check the left neighbor of the cell
        if ((cell - 1) % size.x != board.Count && !board[Mathf.FloorToInt(cell - 1)].visited)
        {
            neighbors.Add(Mathf.FloorToInt(cell - 1));
        }

        return neighbors;
    }
}
