using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using uernd = UnityEngine.Random;

public class MazeEllers : Maze {

    bool leftToRight, topToBottom, vertStart;

    public MazeEllers(int length, int width)
    {
        maze = new string[length, width];
        markSpecial = new bool[length, width];
        curLength = length;
        curWidth = width;
        leftToRight = false;
        topToBottom = false;
        vertStart = false;
    }
    public MazeEllers(int length, int width, bool VStart)
    {
        maze = new string[length, width];
        markSpecial = new bool[length, width];
        curLength = length;
        curWidth = width;
        leftToRight = false;
        topToBottom = false;
        vertStart = VStart;
    }
    public MazeEllers(int length, int width, bool LRBias, bool TBBias, bool VStart)
    {
        maze = new string[length, width];
        markSpecial = new bool[length, width];
        curLength = length;
        curWidth = width;
        leftToRight = LRBias;
        topToBottom = TBBias;
        vertStart = VStart;
    }
    public void FlipStart()
    {
        vertStart = !vertStart;
    }
    public void ChangeBias(bool newLR, bool newTB)
    {
        leftToRight = newLR;
        topToBottom = newTB;
    }

	public override IEnumerator AnimateGeneratedMaze(float delay)
    {
        isGenerating = true;
        bool[,] isRevealed = new bool[curLength, curWidth];
        if (vertStart) // Generating corridors column per column
        {
            
        }
        else
        { // Generating corridors row per row
            int[] curGroupSet = new int[curWidth];
            int counterSetIdx = 0;
            for (int y = 0; y < curWidth - 1; y++)
            {
                curY = topToBottom ? y : curWidth - 1 - y;
                for (int x = 0; x < curLength; x++)
                {
                    curGroupSet[x] = counterSetIdx++;
                }
                for (int x = 0; x < curLength - 1; x++)
                {
                    curGroupSet[x] = counterSetIdx++;
                    curX = leftToRight ? x : curLength - 1 - x;
                    bool generateHorizCorridor = uernd.value < 0.5f;
                    if (x + 1 < curLength && generateHorizCorridor) // Check if the corridor can be generated horizontally.
                    {
                        curGroupSet[x + 1] = curGroupSet[x];
                        if (leftToRight)
                        {
                            CreatePassage(directionRight);
                        }
                        else
                        {
                            CreatePassage(directionLeft);
                        }
                    }
                    yield return new WaitForSeconds(delay);
                }

                int[] lastGroupSet = curGroupSet.ToArray();
                curGroupSet = new int[curWidth];
                curY = topToBottom ? y + 1 : curWidth - 2 - y;
                List<int> groupIdxsStretched = new List<int>();
                for (int x = 0; x < curLength; x++)
                {
                    curX = leftToRight ? x : curLength - 1 - x;
                    if (!groupIdxsStretched.Contains(lastGroupSet[x]))
                    {
                        groupIdxsStretched.Add(lastGroupSet[x]);
                    }
                    else
                    {

                    }
                    yield return new WaitForSeconds(delay);
                }


            }
        }
        isGenerating = false;
		yield return null;
    }

}

