using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

public class MazeBinaryTree : Maze {

    bool biasEast, biasSouth;

    public MazeBinaryTree(int length, int width)
    {
        maze = new string[length, width];
        curLength = length;
        curWidth = width;
        biasEast = false;
        biasSouth = false;
    }

    public MazeBinaryTree(int length, int width, bool LRBias, bool TBBias)
    {
        maze = new string[length, width];
        curLength = length;
        curWidth = width;
        biasEast = LRBias;
        biasSouth = TBBias;
    }

    public void ChangeBias(bool newLR, bool newTB)
    {
        biasEast = newLR;
        biasSouth = newTB;
    }

	public override IEnumerator AnimateGeneratedMaze(float delay)
    {
        isGenerating = true;
        bool[,] isRevealed = new bool[curLength, curWidth];
        for (int x = 0; x < curLength; x++)
        {
            for (int y = 0; y < curWidth; y++)
            {
                curX = biasEast ? x : curLength - 1 - x;
                curY = biasSouth ? y : curWidth - 1 - y;
                bool[] validDirections = {
                    curY - 1 >= 0 && !isRevealed[curX, curY - 1] && !biasSouth,
                    curY + 1 < curWidth && !isRevealed[curX, curY + 1] && biasSouth,
                    curX + 1 < curLength && !isRevealed[curX + 1, curY] && biasEast,
                    curX - 1 >= 0 && !isRevealed[curX - 1, curY] && !biasEast,
                };
                if (validDirections.Any(a => a))
                {
                    switch (new[] { directionUp, directionDown, directionRight, directionLeft }.Where(a => validDirections[a]).PickRandom())
                    {
                        case directionUp:
                            {
                                CreatePassage(directionUp);
                                break;
                            }
                        case directionDown:
                            {
                                CreatePassage(directionDown);
                                break;
                            }
                        case directionRight:
                            {
                                CreatePassage(directionRight);
                                break;
                            }
                        case directionLeft:
                            {
                                CreatePassage(directionLeft);
                                break;
                            }
                    }
                }
                yield return new WaitForSeconds(delay);
            }
        }
        isGenerating = false;
		yield return null;
    }

}

