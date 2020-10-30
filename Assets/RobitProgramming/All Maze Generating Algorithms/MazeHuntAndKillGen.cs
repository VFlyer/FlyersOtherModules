using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MazeHuntAndKill : Maze {

    bool leftToRightScan, topToBottomScan, HorizFirstScan;

    public MazeHuntAndKill(int length, int width)
    {
        maze = new string[length, width];
        curLength = length;
        curWidth = width;
        leftToRightScan = true;
        topToBottomScan = true;
        HorizFirstScan = true;
    }

    public MazeHuntAndKill(int length, int width, bool LRScan, bool TBScan, bool HFirst)
    {
        maze = new string[length, width];
        curLength = length;
        curWidth = width;
        leftToRightScan = LRScan;
        topToBottomScan = TBScan;
        HorizFirstScan = HFirst;
    }

    public void ChangeHorizAndVertScanning(bool newLR, bool newTB, bool newHFirst)
    {
        leftToRightScan = newLR;
        topToBottomScan = newTB;
        HorizFirstScan = newHFirst;
    }

	public override IEnumerator AnimateGeneratedMaze(float delay)
    {
        isGenerating = true;
        bool[,] isRevealed = new bool[curLength, curWidth];
        while (curX != -1 && curY != -1)
        {
            //Debug.LogFormat("({0})",visitedCells.Select(a => a.Join(",")).Join(");("));
            isRevealed[curX, curY] = true;
            bool[] validDirections = {
                curY - 1 >= 0 && !isRevealed[curX, curY - 1],
                curY + 1 < curWidth && !isRevealed[curX, curY + 1],
                curX + 1 < curLength && !isRevealed[curX + 1, curY],
                curX - 1 >= 0 && !isRevealed[curX - 1, curY],
            };
            if (validDirections.Any(a => a))
            {
                switch (new[] { directionUp, directionDown, directionRight, directionLeft }.Where(a => validDirections[a]).PickRandom())
                {
                    case directionUp:
                        {
                            CreatePassage(directionUp);
                            curY--;
                            break;
                        }
                    case directionDown:
                        {
                            CreatePassage(directionDown);
                            curY++;
                            break;
                        }
                    case directionRight:
                        {
                            CreatePassage(directionRight);
                            curX++;
                            break;
                        }
                    case directionLeft:
                        {
                            CreatePassage(directionLeft);
                            curX--;
                            break;
                        }
                }
            }
            else
            {
                var foundX = -1;
                var foundY = -1;
                if (HorizFirstScan)
                {
                    for (var scanX = 0; scanX < curLength && foundX == -1 && foundY == -1; scanX++)
                    {
                        for (var scanY = 0; scanY < curWidth && foundX == -1 && foundY == -1; scanY++)
                        {
                            var curScanX = leftToRightScan ? scanX : curLength - 1 - scanX;
                            var curScanY = topToBottomScan ? scanY : curWidth - 1 - scanY;
                            curX = curScanX;
                            curY = curScanY;
                            yield return new WaitForSeconds(delay);
                            if (!isRevealed[curScanX, curScanY])
                            {
                                if ((curScanX - 1 >= 0 && isRevealed[curScanX - 1, curScanY]) ||
                                    (curScanX + 1 < curLength && isRevealed[curScanX + 1, curScanY]) ||
                                    (curScanY - 1 >= 0 && isRevealed[curScanX, curScanY - 1]) ||
                                    (curScanY + 1 < curWidth && isRevealed[curScanX, curScanY + 1])
                                    )
                                {
                                    foundX = scanX;
                                    foundY = scanY;
                                    break;
                                }
                            }
                        }
                    }
                }
                else
                {
                    for (var scanY = 0; scanY < curWidth && foundX == -1 && foundY == -1; scanY++)
                    {
                        for (var scanX = 0; scanX < curLength && foundX == -1 && foundY == -1; scanX++)
                        {
                            var curScanX = leftToRightScan ? scanX : curLength - 1 - scanX;
                            var curScanY = topToBottomScan ? scanY : curWidth - 1 - scanY;
                            curX = curScanX;
                            curY = curScanY;
                            yield return new WaitForSeconds(delay);
                            if (!isRevealed[curScanX, curScanY])
                            {
                                if ((curScanX - 1 >= 0 && isRevealed[curScanX - 1, curScanY]) ||
                                    (curScanX + 1 < curLength && isRevealed[curScanX + 1, curScanY]) ||
                                    (curScanY - 1 >= 0 && isRevealed[curScanX, curScanY - 1]) ||
                                    (curScanY + 1 < curWidth && isRevealed[curScanX, curScanY + 1])
                                    )
                                {
                                    foundX = scanX;
                                    foundY = scanY;
                                    break;
                                }
                            }
                            
                        }
                    }
                }
                curX = foundX;
                curY = foundY;
                if (curX >= 0 && curX < curLength && curY < curWidth && curY >= 0)
                {
                    bool[] validConnections = {
                    curY - 1 >= 0 && isRevealed[curX, curY - 1],
                    curY + 1 < curWidth && isRevealed[curX, curY + 1],
                    curX + 1 < curLength && isRevealed[curX + 1, curY],
                    curX - 1 >= 0 && isRevealed[curX - 1, curY],
                    };
                    if (validConnections.Any(a => a))
                    {
                        switch (new[] { directionUp, directionDown, directionRight, directionLeft }.Where(a => validConnections[a]).PickRandom())
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
                }
            }
            yield return new WaitForSeconds(delay);
        }
        isGenerating = false;
		yield return null;
    }

}

