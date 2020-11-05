using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using uernd = UnityEngine.Random;
public class RobitProgrammingCore : MonoBehaviour {

	private const int dirU = 0, dirD = 1, dirR = 2, dirL = 3;

	public GridDisplayer gridToDisplay;
	public KMBombModule modSelf;
	public KMBombInfo bombInfo;
    public KMSelectable genreateSelectable, playPauseSelectable, bit0, bit1, backspaceArrow;
	public KMSelectable[] deciSecArrows, centSecArrows, terminalArrows;


	private static int moduleIDCnt = 1;

	private int mazeGenIdx = -1, currentXPos, currentYPos, moduleID;
	private int[] directions;
	private Maze generatedMaze;
	private bool mazeDetermined = false;



	// Use this for initialization
	void Start () {

		moduleID = moduleIDCnt++;




	}
	void QuickLog(string value)
    {
		Debug.LogFormat("[Robit Programming #{0}] {1}", moduleID, value);
    }
	void GenerateFirstTimeMaze()
    {
		mazeGenIdx = uernd.Range(0, 5);
		switch(mazeGenIdx)
        {
			case 0: // Backtracking Generator
                {
					generatedMaze = new MazeBacktracker(5, 5);
					directions = new int[] { dirU, dirD, dirL, dirR };
					break;
                }
			case 1: // Binary Tree
                {
					bool skewEast = uernd.value < 0.5f;
					bool skewSouth = uernd.value < 0.5f;
					generatedMaze = new MazeBinaryTree(5, 5, skewEast, skewSouth);
					switch (skewEast)
                    {
						case true:
							{
								switch (skewSouth)
								{
									case true:
                                        directions = new int[] { dirR, dirU, dirL, dirD };
										break;
									case false:
										directions = new int[] { dirU, dirR, dirD, dirL };
										break;
								}
								break;
							}
						case false:
							{
								switch (skewSouth)
								{
									case true:
										directions = new int[] { dirL, dirD, dirR, dirU };
										break;
									case false:
										directions = new int[] { dirD, dirL, dirU, dirR };
										break;
								}
								break;
							}
                    }
					break;
                }
			case 2: // Hunt And Kill
                {
					bool randomScan = uernd.value < 0.5f;
					if (randomScan)
                    {
						generatedMaze = new MazeHuntAndKill(5, 5);
                    }
					else
                    {

                    }
					break;
                }
			case 3: // Prim's
                {
					generatedMaze = new MazePrims(5, 5);
                    directions = new int[] { dirU, dirL, dirD, dirR };
					break;
                }
			case 4: // Kruskal's
                {
					generatedMaze = new MazeKruskal(5, 5);
                    directions = new int[] { dirU, dirR, dirL, dirD };
					break;
                }
			default:
				{
					generatedMaze = new Maze(5, 5);
                    directions = new int[] { -1, -1, -1, -1 };
					break;
				}
        }
    }
	
	// Update is called once per frame
	void Update () {
		
	}
}
