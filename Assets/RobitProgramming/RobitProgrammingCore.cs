using KModkit;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using uernd = UnityEngine.Random;
public class RobitProgrammingCore : MonoBehaviour {

	private const int dirU = 0, dirD = 1, dirR = 2, dirL = 3;
    readonly string[] directionReference = { "Up", "Down", "Right", "Left" };

	public Color[] highlightColors;

	public KMAudio mAudio;
	public GridDisplayer gridToDisplay;
	public KMBombModule modSelf;
	public KMBombInfo bombInfo;
	public GameObject delayHandler, movementHandler;
    public KMSelectable genreateSelectable, playPauseSelectable, bit0, bit1, backspaceArrow;
	public KMSelectable[] deciSecArrows, centSecArrows, terminalArrows;
	public TextMesh bitText, delayCurText, bitMarkerText;

	public MeshRenderer[] quadrantRenderers, quadrantCornerMarkers;

	private static int moduleIDCnt = 1;

	private int mazeGenIdx = -1, currentXPos, currentYPos, moduleID, movesInQuadrant, commandsOverall, mazeGenDelay = 10;
	private int[] directions = new int[0];
	private Maze generatedMaze;
	private bool mazeDetermined = false, lockMazeGen = false, interactable = true;
	string binaryString = "";
	// Use this for initialization
	void Start () {
		moduleID = moduleIDCnt++;
		genreateSelectable.OnInteract += delegate {
			mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, genreateSelectable.transform);
			genreateSelectable.AddInteractionPunch();
			HandleGenerateButtonPress();
			return false;
		};
		// Adjust the delay on the maze generator.
		for (int x = 0; x < deciSecArrows.Length; x++)
        {
			int y = x;
			deciSecArrows[x].OnInteract += delegate {
				mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, deciSecArrows[y].transform);
				deciSecArrows[y].AddInteractionPunch();
				if (!lockMazeGen && interactable)
				{
					mazeGenDelay = Mathf.Max(0, Mathf.Min(100, mazeGenDelay + 10 * (2 * y - 1)));
					delayCurText.text = (mazeGenDelay / 100) + "." + (mazeGenDelay % 100).ToString("00");
				}
				return false;
			};
        }
		for (int x = 0; x < centSecArrows.Length; x++)
		{
			int y = x;
			centSecArrows[x].OnInteract += delegate {
				mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, centSecArrows[y].transform);
				centSecArrows[y].AddInteractionPunch();
				if (!lockMazeGen && interactable)
				{
					mazeGenDelay = Mathf.Max(0, Mathf.Min(100, mazeGenDelay + (2 * y - 1)));
					delayCurText.text = (mazeGenDelay / 100) + "." + (mazeGenDelay % 100).ToString("00");
				}
				return false;
			};
		}
		StartCoroutine(FlipDelayModifier());
	}
	void QuickLog(string value)
    {
		Debug.LogFormat("[Robit Programming #{0}] {1}", moduleID, value);
    }

	void HandleGenerateButtonPress()
    {
		if (lockMazeGen || !interactable) return;
		if (!mazeDetermined)
        {
			mazeDetermined = true;
			GenerateFirstTimeMaze();
        }
		if (!generatedMaze.GetState())
        {
			StartCoroutine(GenerateMaze());
        }
    }
	IEnumerator FlipDelayModifier()
    {
		yield return null;
        for (float x = 1f; x >= 0; x -= Time.deltaTime)
        {
			yield return null;
			delayHandler.transform.localEulerAngles = Vector3.forward * 180 * x;
        }
		delayHandler.transform.localEulerAngles = Vector3.zero;
	}

	void UpdateVisuals()
    {
		for (int x = 0; x < gridToDisplay.rowRenderers.Length; x++)
		{
			for (int y = 0; y < Mathf.Min(gridToDisplay.rowRenderers[x].wallRenderers.Length, gridToDisplay.rowRenderers[x].canRender.Length); y++)
			{
				/*
				if (gridToDisplay.rowRenderers[x].wallRenderers[y].enabled != gridToDisplay.rowRenderers[x].canRender[y])
					if (x % 2 != 0 || y % 2 != 0)
						if (gridToDisplay.rowRenderers[x].canRender[y])
							mAudio.PlaySoundAtTransform("Plop", transform);
						else
							mAudio.PlaySoundAtTransform("Plip", transform);
					else if (mazeDetermined)
						if (gridToDisplay.rowRenderers[x].canRender[y])
							mAudio.PlaySoundAtTransform("273766__n-audioman__pong", transform);
						else
							mAudio.PlaySoundAtTransform("275897__n-audioman__blip", transform);
				*/
				gridToDisplay.rowRenderers[x].wallRenderers[y].enabled = gridToDisplay.rowRenderers[x].canRender[y];
			}

		}
		for (int x = 0; x < generatedMaze.maze.GetLength(0); x++)
			for (int y = 0; y < generatedMaze.maze.GetLength(1); y++)
			{
				if (generatedMaze.GetState())
					gridToDisplay.rowRenderers[y * 2].wallRenderers[x * 2].material.color = generatedMaze.GetCurX() == x && generatedMaze.GetCurY() == y ? highlightColors[1] : generatedMaze.markSpecial[x, y] ? highlightColors[2] : highlightColors[0];
				else
					gridToDisplay.rowRenderers[y * 2].wallRenderers[x * 2].material.color = highlightColors[0];
			}
	}
	IEnumerator UpdateCanRendersMaze()
    {
		yield return null;
		do
		{
			for (int x = 0; x < generatedMaze.maze.GetLength(0); x++)
			{
				for (int y = 0; y < generatedMaze.maze.GetLength(1); y++)
				{
					if (x + 1 < generatedMaze.maze.GetLength(0))
						gridToDisplay.rowRenderers[y * 2].canRender[x * 2 + 1] = !generatedMaze.maze[x, y].Contains("R");

					if (x - 1 >= 0)
						gridToDisplay.rowRenderers[y * 2].canRender[x * 2 - 1] = !generatedMaze.maze[x, y].Contains("L");

					if (y - 1 >= 0)
						gridToDisplay.rowRenderers[y * 2 - 1].canRender[x * 2] = !generatedMaze.maze[x, y].Contains("U");

					if (y + 1 < generatedMaze.maze.GetLength(1))
						gridToDisplay.rowRenderers[y * 2 + 1].canRender[x * 2] = !generatedMaze.maze[x, y].Contains("D");

					gridToDisplay.rowRenderers[y * 2].canRender[x * 2] = generatedMaze.GetState() && ((generatedMaze.GetCurX() == x && generatedMaze.GetCurY() == y) || generatedMaze.markSpecial[x, y]);
				}
			}
			for (int x = 1; x < gridToDisplay.rowRenderers.Length; x += 2)
			{
				for (int y = 1; y < gridToDisplay.rowRenderers[x].canRender.Length; y += 2)
				{
					gridToDisplay.rowRenderers[x].canRender[y] = true;
				}
			}
			for (int x = 1; x < gridToDisplay.rowRenderers.Length; x += 2)
			{
				for (int y = 1; y < gridToDisplay.rowRenderers[x].canRender.Length; y += 2)
				{
					gridToDisplay.rowRenderers[x].canRender[y] =
						gridToDisplay.rowRenderers[x + 1].canRender[y] ||
						gridToDisplay.rowRenderers[x - 1].canRender[y] ||
						gridToDisplay.rowRenderers[x].canRender[y + 1] ||
						gridToDisplay.rowRenderers[x].canRender[y - 1];
				}
			}
			UpdateVisuals();
			yield return null;
		}
		while (generatedMaze.GetState());
	}
	IEnumerator GenerateMaze()
    {
		interactable = false;
		delayCurText.color = Color.gray;
		yield return null;
		generatedMaze.FillMaze();
		generatedMaze.MoveToNewPosition(generatedMaze.GetLength() / 2, generatedMaze.GetWidth() / 2);
		for (int x = 0; x < generatedMaze.maze.GetLength(0); x++)
		{
			for (int y = 0; y < generatedMaze.maze.GetLength(1); y++)
			{
				if (x + 1 < generatedMaze.maze.GetLength(0))
					gridToDisplay.rowRenderers[y * 2].canRender[x * 2 + 1] = !generatedMaze.maze[x, y].Contains("R");

				if (x - 1 >= 0)
					gridToDisplay.rowRenderers[y * 2].canRender[x * 2 - 1] = !generatedMaze.maze[x, y].Contains("L");

				if (y - 1 >= 0)
					gridToDisplay.rowRenderers[y * 2 - 1].canRender[x * 2] = !generatedMaze.maze[x, y].Contains("U");

				if (y + 1 < generatedMaze.maze.GetLength(1))
					gridToDisplay.rowRenderers[y * 2 + 1].canRender[x * 2] = !generatedMaze.maze[x, y].Contains("D");

			}
		}
		for (int x = 1; x < gridToDisplay.rowRenderers.Length; x += 2)
		{
			for (int y = 1; y < gridToDisplay.rowRenderers[x].canRender.Length; y += 2)
			{
				gridToDisplay.rowRenderers[x].canRender[y] =
					gridToDisplay.rowRenderers[x + 1].canRender[y] ||
					gridToDisplay.rowRenderers[x - 1].canRender[y] ||
					gridToDisplay.rowRenderers[x].canRender[y + 1] ||
					gridToDisplay.rowRenderers[x].canRender[y - 1];
			}
		}
		
		for (int x = 0; x < gridToDisplay.rowRenderers.Length; x ++)
		{
			for (int y = 0; y < gridToDisplay.rowRenderers[x].wallRenderers.Length; y++)
			{
				if (gridToDisplay.rowRenderers[x].wallRenderers[y].enabled != gridToDisplay.rowRenderers[x].canRender[y])
				{
					gridToDisplay.rowRenderers[x].wallRenderers[y].enabled = gridToDisplay.rowRenderers[x].canRender[y];
					/*
					if (gridToDisplay.rowRenderers[x].canRender[y])
						mAudio.PlaySoundAtTransform("Plop", transform);
					else
						mAudio.PlaySoundAtTransform("Plip", transform);
					*/
					yield return new WaitForSeconds(.05f);
				}
			}
		}

		StartCoroutine(UpdateCanRendersMaze());
		yield return generatedMaze.AnimateGeneratedMaze(mazeGenDelay / 100f);
		yield return UpdateCanRendersMaze();
		delayCurText.color = Color.white;
		interactable = true;
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
		QuickLog(string.Format("This gives the starting directions: [ {0} ]", directions.Select(a => directionReference.ElementAtOrDefault(a)).Join(", ")));
	}
	IEnumerator HandleSolveAnim()
    {
		modSelf.HandlePass();
        for (float x = 0; x < 1f; x += Time.deltaTime)
        {
			yield return null;
            for (int i = 0; i < gridToDisplay.rowRenderers.Length; i++)
            {
                RowRenderers renderers = gridToDisplay.rowRenderers[i];
				renderers.transform.localPosition += Vector3.down * Time.deltaTime;
            }
        }
    }
	// Update is called once per frame
	void Update () {
		
	}
	void TwitchHandleForcedSolve()
    {
		StartCoroutine(HandleSolveAnim());
    }
}
