using System;
using System.Collections.Generic;
using UnityEngine;


public class MazeCell : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField]
    private GameObject _leftWall;

    [SerializeField]
    private GameObject _rightWall;

    [SerializeField]
    private GameObject _frontWall;

    [SerializeField]
    private GameObject _backWall;

    [SerializeField]
    private GameObject _unvisitedBlock;

    [SerializeField]
    private GameObject _floor;

    public int x, z;

    public bool IsVisited {
        get; //allows other scrips to read the values
        private set; //on this class can change the value
    } //this stores wehther cell has been visited. store true or false
    //whenever you call IsVisted it will check the to see if it has been visited or not. 

    public void Visit() //this cell will be called when visited by the generator algorithm
    {
        IsVisited = true; //this tells the cell that it has been visited dont visit again
        _unvisitedBlock.SetActive(false); //this will deactive the univisted block and make the walls visible
    }

    public void ClearLeftWall()
    {
        _leftWall.SetActive(false); //this will remove the left wall
    }

    public void ClearRightWall()
    {
        _rightWall.SetActive(false);
    }

    public void ClearBackWall()
    {
        _backWall.SetActive(false);
    }

    public void ClearFrontWall()
    {
        _frontWall.SetActive(false);
    }

}
