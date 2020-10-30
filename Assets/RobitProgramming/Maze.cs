using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class Maze {

	protected const int directionUp = 0, directionDown = 1, directionRight = 2, directionLeft = 3;

	public string[,] maze;
	protected int curLength, curWidth, curX, curY;
	public int startingRow, startingCol;
	protected bool isGenerating = false;
	public Maze()
    {
		maze = new string[1, 1];
		curWidth = 1;
		curLength = 1;
	}
	public Maze(int length, int width)
    {
		maze = new string[length, width];
		curLength = length;
		curWidth = width;
    }

	public int GetLength()
    {
		return curLength;
    }
	public int GetWidth()
    {
		return curWidth;
    }
	public bool GetState()
    {
		return isGenerating;
    }

	public int GetCurX()
	{
		return curX;
	}

	public int GetCurY()
	{
		return curY;
	}

	public void FillMaze()
    {
		for (int x = 0; x < maze.GetLength(0); x++)
		{
			for (int y = 0; y < maze.GetLength(1); y++)
			{
				maze[x, y] = "";
			}
		}
	}
	public void CreatePassage(int directionIdx)
    {
		switch (directionIdx)
        {
			case directionUp:
                {
					maze[curX, curY - 1] += "D";
					maze[curX, curY] += "U";
					break;
                }
			case directionDown:
				{
					maze[curX, curY + 1] += "U";
					maze[curX, curY] += "D";
					break;
				}
			case directionRight:
				{
					maze[curX + 1, curY] += "L";
					maze[curX, curY] += "R";
					break;
				}
			case directionLeft:
				{
					maze[curX - 1, curY] += "R";
					maze[curX, curY] += "L";
					break;
				}
			default:
				break;
        }
    }

	public void MoveToNewPosition(int newX, int newY)
    {
		curX = newX;
		curY = newY;
    }

	public IEnumerator AnimateGeneratedMaze()
    {
		yield return AnimateGeneratedMaze(0f);
    }

	public virtual IEnumerator AnimateGeneratedMaze(float delay)
	{
		yield return null;
	}

}
