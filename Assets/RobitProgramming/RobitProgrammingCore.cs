using KModkit;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

	private int mazeGenIdx = -1, currentXPos, currentYPos, moduleID, movesInQuadrant, commandsOverall;
	private int[] directions = new int[0];
	private Maze generatedMaze;
	private bool mazeDetermined = false;
	string binaryString = "";
	// Use this for initialization
	void Start () {

		moduleID = moduleIDCnt++;


	}
	void QuickLog(string value)
    {
		Debug.LogFormat("[Robit Programming #{0}] {1}", moduleID, value);
    }
	readonly string[] mazeAlgorithmNames = {
		"Recursive Backtracking",
		"Binary Tree",
		"Hunt And Kill",
		"Prim's",
		"Kruskal's",
		"Growing Tree",
		"Aldous-Broder",
		"Wilson's",
		"Sidewinder",
		"Recursive Division",
		"Eller's" };
	void GenerateFirstTimeMaze()
    {
		mazeGenIdx = uernd.Range(0, 11);
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
						directions = new int[] { dirL, dirR, dirU, dirD };
					}
					else
                    {
						bool vertScan = uernd.value < 0.5f, topToBottom = uernd.value < 0.5f, leftToRight = uernd.value < 0.5f;
						generatedMaze = new MazeHuntAndKill(5, 5, leftToRight, topToBottom, vertScan);
						if (vertScan)
						{
							directions = new int[] { dirL, dirD, dirU, dirR };
						}
						else
						{
							directions = new int[] { dirL, dirU, dirD, dirR };
						}

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
			case 5: // Growing Tree
				{
					generatedMaze = new MazeGrowingTree(5, 5, uernd.Range(1, 6), uernd.Range(0, 3), uernd.Range(1, 6));
					directions = new int[] { dirU, dirR, dirL, dirD };
					break;
				}
			case 6: // Aldous-Broder
				{
					generatedMaze = new MazeAldousBroder(5, 5);
                    directions = new int[] { dirL, dirU, dirR, dirD };
					break;
				}
			case 7: // Wilson's
				{
					generatedMaze = new MazeWilsons(5, 5);
					directions = new int[] { dirD, dirL, dirR, dirU };
					break;
				}
			case 8: // Sidewinder
				{
					bool vertGen = uernd.value < 0.5f, topToBottom = uernd.value < 0.5f, leftToRight = uernd.value < 0.5f;

					generatedMaze = new MazeSidewinder(5, 5, vertGen, topToBottom, leftToRight);
					if (vertGen)
					{
						if (leftToRight)
							directions = new int[] { dirR, dirD, dirL, dirU };
						else
							directions = new int[] { dirR, dirD, dirU, dirL };
					}
					else
                    {
						if (topToBottom)
							directions = new int[] { dirD, dirR, dirL, dirU };
						else
							directions = new int[] { dirD, dirR, dirU, dirL };
					}
					
					break;
				}
			case 9: // Recursive Division
				{
					generatedMaze = new MazeRecursiveDivision(5, 5);
					directions = new int[] { dirD, dirU, dirR, dirL };
					break;
				}
			case 10: // Eller's
				{
					bool vertGen = uernd.value < 0.5f, topToBottom = uernd.value < 0.5f, leftToRight = uernd.value < 0.5f;

					generatedMaze = new MazeEllers(5, 5, leftToRight, topToBottom, vertGen);
					if (vertGen)
						directions = new int[] { dirD, dirU, dirL, dirR };
					else
						directions = new int[] { dirU, dirD, dirR, dirL };
					break;
				}
			default:
				{
					generatedMaze = new Maze(5, 5);
                    directions = new int[] { -1, -1, -1, -1 };
					break;
				}
        }
		QuickLog(string.Format("The maze the module has selected to generate uses this algorithm: {0}", mazeGenIdx < mazeAlgorithmNames.Length ? mazeAlgorithmNames[mazeGenIdx] : "unknown"));
    }
	
	// Update is called once per frame
	void Update () {
		
	}
}
