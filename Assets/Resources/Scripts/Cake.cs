using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cake : MonoBehaviour
{
    private float speed = 0.17f;
    private float distanceBetweenCells = 0.438f;
    private Point targetPoint;
    private Vector2 firstCell;
    private bool isMovable = false;

    void Start()
    {
        firstCell = GameObject.FindGameObjectWithTag("Board").transform.GetChild(0).transform.position;
    }

    void FixedUpdate()
    {
        if (isMovable)
        {
            float posX = firstCell.x + distanceBetweenCells * targetPoint.GetX;
            float posY = firstCell.y - distanceBetweenCells * targetPoint.GetY;

            transform.position = Vector3.Lerp(transform.position, new Vector3(posX, posY, -0.01f), speed);

            if (transform.position == new Vector3(posX, posY, -0.01f))
            {
                isMovable = false;
            }
        }
    }

    public Point SetTarget 
    { 
        set 
        { 
            targetPoint = value;
            isMovable = true;
        } 
    }
    public int GetX { get { return targetPoint.GetX; } }
    public int GetY { get { return targetPoint.GetY; } }
}
