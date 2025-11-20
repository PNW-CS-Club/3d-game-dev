using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NewMazeGenerator : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    [SerializeField]
    private MazeCell _mazeCellPrefab;

    [SerializeField]
    private int _mazeWidth;

    [SerializeField]
    private int _mazeDepth;

    private MazeCell[,] _mazeGrid; //this will hold the grid of cells

    IEnumerator Start()
    {
        _mazeGrid = new MazeCell[_mazeWidth, _mazeDepth];


        for(int x = 0; x < _mazeWidth; x+=4)
        {
            for(int z = 0; z < _mazeDepth; z+=4)
            {
                _mazeGrid[x, z] = Instantiate(_mazeCellPrefab, new Vector3(x, 0, z), Quaternion.identity); //this will actually create the cell and store it in the _mazeGrid array
            }
        }

        yield return GenerateMaze(null, _mazeGrid[0,0]);
    }

    private IEnumerator GenerateMaze(MazeCell previousCell, MazeCell currentCell) //this method will called recursilvey to make sure that everything has been visited in teh maze
    {
        currentCell.Visit(); //this will make all the current walls visible
        ClearWalls(previousCell, currentCell);

        yield return new WaitForSeconds(0.05f);

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
        int x = (int)currentCell.transform.position.x; //these correspond to the index within the grid
        int z = (int)currentCell.transform.position.z; 

        //x always you to move left and right 
        
        if(x + 4 < _mazeWidth) //this will check if the next cell to the right is within the bounds of the grid
        {
            var cellToRight = _mazeGrid[x+4, z];

            if(cellToRight.IsVisted == false) //check if the cell to the right has been visited
            {
                yield return cellToRight; //if it has not been visited will return the results
            }
        }

        if(x - 4 >= 0) //check to see if the within the bonds of 0
        {
            var cellToLeft = _mazeGrid[x-4, z];

            if(cellToLeft.IsVisted == false) //check if the cell to the left has been visited
            {
                yield return cellToLeft;
            }
        }

        if(z + 4 < _mazeDepth)
        {
            var cellToFront = _mazeGrid[x, z + 4];

            if(cellToFront.IsVisted == false)
            {
                if(cellToFront.IsVisted == false) //check if the cell above/front has been visited
                {
                    yield return cellToFront;
                }
            }
        }

        if(z - 4 >= 0)
        {
            var cellToBack = _mazeGrid[x, z - 4];

            if(z == _mazeDepth / 4)
            {
                
            }

            if(cellToBack.IsVisted == false) //check if the cell below/back has been visited
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

        if(previousCell.transform.position.x < currentCell.transform.position.x) //the previous cell will check if it's to the left of the current one and if it is we know that the algorithm has gone from LEFT TO RIGHT
        {
            previousCell.ClearRightWall(); //since it's gone from left to right, prev to current, we clear the prev right wall and the current left wall
            currentCell.ClearLeftWall();
            return;
        }

        if(previousCell.transform.position.x > currentCell.transform.position.x) //the previous cell will check if it's to the right of the current one and if it is we know that the algorithm has gone from RIGHT TO LEFT
        {
          previousCell.ClearLeftWall(); //clears current right wall and prev left wall
          currentCell.ClearRightWall();  
        }

        if(previousCell.transform.position.z < currentCell.transform.position.z) //the previous cell will check if it's above the current one and if it is we know that the algorithm has gone from BACK TO FRONT
        {
            previousCell.ClearFrontWall(); //clears the current back wall and prev front wall
            currentCell.ClearBackWall();
        }

        if(previousCell.transform.position.z > currentCell.transform.position.z) //the previous cell will check if it's below the current one and if it is we know that the algorithm has gone from FRONT TO BACK
        {
            previousCell.ClearBackWall(); //clears the current front wall and the prev back wall
            currentCell.ClearFrontWall();
        }
    }

    private void entrance(MazeCell currentCell)
    {
        currentCell.ClearBackWall();
    }

    private void exits(MazeCell currentCell)
    {
        currentCell.ClearFrontWall();
    }
}
