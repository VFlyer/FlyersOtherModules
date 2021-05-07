using KModkit;
using Newtonsoft.Json.Bson;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using uernd = UnityEngine.Random;

public class CollapseCore : MonoBehaviour {

    public KMAudio mAudio;
    public KMGameCommands gameCommands;
    public KMSelectable modSelfSelectable;
    public KMNeedyModule needySelf;
    public KMBombInfo bombInfo;
    public RowRendererPlus[] rowRenderersWithSelectables;
    public RowRenderers rowRenderersNextSet;

    public Color[] colorList = {
        Color.cyan,
        Color.blue,
        Color.gray,
        Color.yellow,
        Color.red,
        Color.magenta
    };
    public string colorString = "CBAYRM";
    public Texture[] colorblindTextures;
    int[] colorIdxSet = { 0, 1, 2, 3, 4, 5 };

    bool[,] isSelected = new bool[10, 15];
    int[,] colorIdxBoard = new int[10, 15];
    static int modIdCounter = 1;
    int modID;
    bool isActive = false, permDeactivate, pauseLift = false, interactable = false;
    List<Vector3> startRowCoords = new List<Vector3>();
    float timeLeftManaged = 10f, dynamicRate = .8f;

    List<KMSelectable> allSelectablesDuringAnimation;
    List<KMSelectable> allSelectablesDefault;

    IEnumerator blockLifter, timeManager;

    // Use this for initialization
    void Start() {
        modID = modIdCounter++;
        needySelf.OnNeedyActivation += delegate
        {
            if (!permDeactivate)
            {
                isActive = true;
                blockLifter = HandleHeightManagment();
                timeManager = ModifyNeedyTimer();
                StartCoroutine(blockLifter);
                StartCoroutine(timeManager);
                interactable = true;
            }
            else
                needySelf.HandlePass();
        };
        allSelectablesDefault = new List<KMSelectable>(); // Mainly for compacting lines, required for usage otherwise.
        allSelectablesDuringAnimation = new List<KMSelectable>();
        for (int i = 0; i < rowRenderersWithSelectables.Length; i++)
        {
            RowRendererPlus rowRenderer = rowRenderersWithSelectables[i];
            allSelectablesDefault.AddRange(rowRenderer.rowSelectables);
            if (i != 14)
                allSelectablesDuringAnimation.AddRange(rowRenderer.rowSelectables);
            startRowCoords.Add(rowRenderer.transform.localPosition);

            for (int x = 0; x < rowRenderer.rowSelectables.Length; x++)
            {
                int tRow = x, tCol = i;
                rowRenderer.rowSelectables[x].OnInteract += delegate {
                    rowRenderer.rowSelectables[tRow].AddInteractionPunch(0.1f);
                    mAudio.PlaySoundAtTransform("tick", rowRenderer.rowSelectables[tRow].transform);
                    if (isActive && interactable)
                        ProcessTile(tRow, tCol);
                    return false;
                };
            }
        }
        bombInfo.OnBombExploded += delegate {
            if (!permDeactivate)
            {
                QuickLog("Board upon detonation: ");
                LogBoard();
            }
        };

        modSelfSelectable.Children = allSelectablesDefault.ToArray();
        modSelfSelectable.UpdateChildren(); // Required for gamepad usage and updating.
        StartCoroutine(delayRotation());
        needySelf.OnNeedyDeactivation += delegate {
            isActive = false;
            if (!permDeactivate)
            {
                QuickLog("Board upon deactivation: ");
                LogBoard();
            }
        };
        UpdateGrid();
        rowRenderersNextSet.transform.localPosition = new Vector3(0, 0, -6.5f);
        rowRenderersNextSet.transform.localScale = new Vector3(1, 1, 0);
        rowRenderersNextSet.gameObject.SetActive(false);
        colorIdxSet.Shuffle();
    }
    void QuickLog(string value)
    {
        Debug.LogFormat("[Collaspe #{0}] {1}", modID, value);
    }

    void LogBoard()
    {
        QuickLog("o----------o");
        for (int y = colorIdxBoard.GetLength(1) - 1; y >= 0; y--)
        {
            List<string> valuesSet = new List<string>();
            for (int x = 0; x < colorIdxBoard.GetLength(0); x++)
            {
                int value = colorIdxBoard[x, y];
                valuesSet.Add(value == 0 ? " " : colorString[value - 1].ToString());
            }
            QuickLog(string.Format("|{0}|", valuesSet.Join("")));
        }
        QuickLog("o----------o");
    }

    void HandleDeactivation()
    {
        needySelf.SetResetDelayTime(float.PositiveInfinity, float.PositiveInfinity); // Makes it so that the needy is disabled forever.
        needySelf.HandlePass();
        isActive = false;
        permDeactivate = true;
        QuickLog("Board upon deactivation: ");
        LogBoard();
        StopAllCoroutines();
        StartCoroutine(HandleGameOver());
        //RequestDetonation();
    }

    void ProcessTile(int row, int col)
    {
        if (!isSelected[row, col])
        {
            if (colorIdxBoard[row, col] == 0) return;
            DeselectAllTiles();
            SelectAllSimilarTiles(row, col, colorIdxBoard[row, col]);
            if (CountSelectedTiles() < 3)
            {
                DeselectAllTiles();
            }
        }
        else
        {
            /*
            for (int x = 0; x < isSelected.GetLength(0); x++)
                for (int y = 0; y < isSelected.GetLength(1); y++)
                {
                    if (isSelected[x, y])
                        colorIdxBoard[x, y] = 0;
                }
            DeselectAllTiles();
            CollapseAllTiles();
            */
            //StopCoroutine(blockLifter);
            //StopCoroutine(timeManager);
            pauseLift = true;
            interactable = false;
            
            StartCoroutine(AnimateBreakingAnim());
            //StartCoroutine(timeManager);
        }
        UpdateGrid();
    }
    IEnumerator AnimateBreakingAnim()
    {
        List<int[]> coordSet = new List<int[]>();

        for (int x = 0; x < isSelected.GetLength(0); x++)
            for (int y = 0; y < isSelected.GetLength(1); y++)
            {
                if (isSelected[x, y])
                {
                    coordSet.Add(new[] { x, y });
                }
            }
        foreach (int[] oneCoord in coordSet.Shuffle())
        {
            colorIdxBoard[oneCoord[0], oneCoord[1]] = 0;
            UpdateGrid();
            mAudio.PlaySoundAtTransform("275897__n-audioman__blip", transform);
            yield return new WaitForSeconds(0.1f);
        }

        DeselectAllTiles();
        for (int y = 0; y < colorIdxBoard.GetLength(1) - 1; y++)
            for (int x = 0; x < colorIdxBoard.GetLength(0); x++)
                for (int z = 0; z < colorIdxBoard.GetLength(1) - 1; z++)
                {
                    if (colorIdxBoard[x, z] == 0 && colorIdxBoard[x, z + 1] != 0)
                    {
                        colorIdxBoard[x, z] = colorIdxBoard[x, z + 1];
                        colorIdxBoard[x, z + 1] = 0;
                        UpdateGrid();
                        yield return new WaitForSeconds(0.01f);
                    }
                }
        yield return null;
        pauseLift = false;
        interactable = true;

    }
    void CollapseAllTiles()
    {
        for (int x = 0; x < colorIdxBoard.GetLength(0); x++)
            for (int y = 0; y < colorIdxBoard.GetLength(1) - 1; y++)
                for (int z = 0; z < colorIdxBoard.GetLength(1) - 1; z++)
                {
                    if (colorIdxBoard[x, z] == 0)
                    {
                        colorIdxBoard[x, z] = colorIdxBoard[x, z + 1];
                        colorIdxBoard[x, z + 1] = 0;
                    }
                }
    }

    IEnumerator HandleGameOver()
    {
        for (float t = 0; t <= 1f; t = Mathf.Min(t + Time.deltaTime, 1f))
        {
            yield return null;
            for (int y = colorIdxBoard.GetLength(1) - 1; y >= 0; y--)
            {
                for (int x = 0; x < colorIdxBoard.GetLength(0); x++)
                {
                    int curColorIdx = colorIdxBoard[x, y];
                    if (curColorIdx != 0)
                    {
                        rowRenderersWithSelectables[y].wallRenderers[x].material.color = colorList[colorIdxSet[curColorIdx - 1]] * 0.5f * (1 - t) + Color.white * 0.5f * (1 - t);
                    }
                }
            }
            if (t >= 1f) break;
        }
        for (int y = colorIdxBoard.GetLength(1) - 1; y >= 0; y--)
        {
            for (int x = 0; x < colorIdxBoard.GetLength(0); x++)
            {
                colorIdxBoard[x, y] = 0;
            }
        }
        UpdateGrid();
    }

    IEnumerator ModifyNeedyTimer()
    {
        yield return null;
        var timer = transform.Find("NeedyTimer(Clone)");
        if (timer != null)
        {
            var timerScript = timer.GetComponent("NeedyTimer");
            if (timerScript != null)
                timerScript.SetValue("WarnTime", 50f);
        }
        while (isActive)
        {
            yield return null;
            needySelf.SetNeedyTimeRemaining(timeLeftManaged * 9.9f);
            int[] heightCounts = new int[10];
            for (int x = 0; x < colorIdxBoard.GetLength(0); x++)
                for (int y = 0; y < colorIdxBoard.GetLength(1); y++)
                    heightCounts[x] += colorIdxBoard[x, y] != 0 ? 1 : 0;
            if (heightCounts.Max() < 15)
                timeLeftManaged = Mathf.Min(10f, timeLeftManaged + Time.deltaTime);
        }
    }
    IEnumerator HandleHeightManagment()
    {
        for (int x = 0; x < 3; x++)
            yield return AnimateNextRowAnim(5f);
        while (isActive)
        {
            yield return null;
            int[] heightCounts = new int[10];
            for (int x = 0; x < colorIdxBoard.GetLength(0); x++)
                for (int y = 0; y < colorIdxBoard.GetLength(1); y++)
                    heightCounts[x] += colorIdxBoard[x, y] != 0 ? 1 : 0;
            if (heightCounts.Max() < 15)
                yield return AnimateNextRowDynamicAnim();
            else
            {
                if (!pauseLift)
                    timeLeftManaged -= Time.deltaTime;
                //mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.NeedyWarning, transform);
                if (timeLeftManaged < 0f)
                {
                    needySelf.HandleStrike();
                    HandleDeactivation();
                }
            }
        }
    }
    IEnumerator AnimateNextRowDynamicAnim()
    {
        int maxSolvables = Mathf.Max(bombInfo.GetSolvableModuleIDs().Count(), 1), currentlySolved = bombInfo.GetSolvedModuleIDs().Count();

        yield return null;

        int maxColorsPossible = 2 + (maxSolvables * 1 < currentlySolved * 2 ? 1 : 0) + (maxSolvables * 9 < currentlySolved * 10 ? 1 : 0);

        int[] nextRow = new int[10];
        for (int x = 0; x < nextRow.Length; x++)
        {
            nextRow[x] = uernd.Range(1, maxColorsPossible + 1);
        }
        rowRenderersNextSet.gameObject.SetActive(true);
        modSelfSelectable.Children = allSelectablesDuringAnimation.ToArray();
        modSelfSelectable.UpdateChildren(); // Required for gamepad usage and updating.
        for (float x = 0; x < 1f; x += Time.deltaTime * dynamicRate * Mathf.Min(1f, (currentlySolved + 1f) / maxSolvables) * (pauseLift ? 0f : 1f))
        {
            yield return null;
            rowRenderersNextSet.transform.localPosition = new Vector3(0, 0.1f, -7f - (0.5f * (1f - x)));
            rowRenderersNextSet.transform.localScale = new Vector3(1, 1, x);

            currentlySolved = bombInfo.GetSolvedModuleIDs().Count();

            for (int z = 0; z < rowRenderersNextSet.wallRenderers.Length; z++)
            {
                rowRenderersNextSet.wallRenderers[z].material.color = colorList[colorIdxSet[nextRow[z] - 1]] * 0.5f +
                    Color.gray * 0.5f;
                rowRenderersNextSet.wallRenderers[z].material.SetTexture("_MainTex", colorblindTextures[colorIdxSet[nextRow[z] - 1]]);
            }
            for (int y = 0; y < rowRenderersWithSelectables.Length; y++)
            {
                rowRenderersWithSelectables[y].transform.localPosition = startRowCoords[y] + new Vector3(0, 0, x);
            }
        }
        for (int x = 0; x < colorIdxBoard.GetLength(0); x++)
            for (int y = colorIdxBoard.GetLength(1) - 1; y >= 0; y--)
            {
                if (y > 0)
                {
                    colorIdxBoard[x, y] = colorIdxBoard[x, y - 1];
                    isSelected[x, y] = isSelected[x, y - 1];
                }
                else
                {
                    colorIdxBoard[x, y] = nextRow[x];
                    isSelected[x, y] = false;
                }
            }
        for (int y = 0; y < rowRenderersWithSelectables.Length; y++)
        {
            rowRenderersWithSelectables[y].transform.localPosition = startRowCoords[y];
        }
        rowRenderersNextSet.transform.localPosition = new Vector3(0, 0, -7.5f);
        rowRenderersNextSet.transform.localScale = new Vector3(1, 1, 0);
        rowRenderersNextSet.gameObject.SetActive(false);
        modSelfSelectable.Children = allSelectablesDefault.ToArray();
        modSelfSelectable.UpdateChildren(); // Required for gamepad usage and updating.
        UpdateGrid();
    }
    IEnumerator AnimateNextRowAnim(float speed = 1f)
    {
        if (speed <= 0f) yield break;
        yield return null;
        int[] nextRow = new int[10];
        for (int x = 0; x < nextRow.Length; x++)
        {
            nextRow[x] = uernd.Range(1, 3);
        }
        rowRenderersNextSet.gameObject.SetActive(true);
        modSelfSelectable.Children = allSelectablesDuringAnimation.ToArray();
        modSelfSelectable.UpdateChildren(); // Required for gamepad usage and updating.
        for (float x = 0; x < 1f; x += Time.deltaTime * speed)
        {
            yield return null;
            rowRenderersNextSet.transform.localPosition = new Vector3(0, 0.1f, -7f - (0.5f * (1f - x)));
            rowRenderersNextSet.transform.localScale = new Vector3(1, 1, x);

            for (int z = 0; z < rowRenderersNextSet.wallRenderers.Length; z++)
            {
                rowRenderersNextSet.wallRenderers[z].material.color = colorList[colorIdxSet[nextRow[z] - 1]] * 0.5f +
                    Color.gray *  0.5f;
                rowRenderersNextSet.wallRenderers[z].material.SetTexture("_MainTex", colorblindTextures[colorIdxSet[nextRow[z] - 1]]);
            }
            for (int y = 0; y < rowRenderersWithSelectables.Length; y++)
            {
                rowRenderersWithSelectables[y].transform.localPosition = startRowCoords[y] + new Vector3(0, 0, x);
            }
        }
        for (int x = 0; x < colorIdxBoard.GetLength(0); x++)
            for (int y = colorIdxBoard.GetLength(1) - 1; y >= 0; y--)
            {
                if (y > 0)
                {
                    colorIdxBoard[x, y] = colorIdxBoard[x, y - 1];
                    isSelected[x, y] = isSelected[x, y - 1];
                }
                else
                {
                    colorIdxBoard[x, y] = nextRow[x];
                    isSelected[x, y] = false;
                }
            }
        for (int y = 0; y < rowRenderersWithSelectables.Length; y++)
        {
            rowRenderersWithSelectables[y].transform.localPosition = startRowCoords[y];
        }
        rowRenderersNextSet.transform.localPosition = new Vector3(0, 0, -7.5f);
        rowRenderersNextSet.transform.localScale = new Vector3(1, 1, 0);
        rowRenderersNextSet.gameObject.SetActive(false);
        modSelfSelectable.Children = allSelectablesDefault.ToArray();
        modSelfSelectable.UpdateChildren(); // Required for gamepad usage and updating.
        UpdateGrid();
    }

    int CountSelectedTiles()
    {
        int output = 0;
        for (int x = 0; x < isSelected.GetLength(0); x++)
            for (int y = 0; y < isSelected.GetLength(1); y++)
                output += isSelected[x, y] ? 1 : 0;
        return output;
    }

    void DeselectAllTiles()
    {
        for (int x = 0; x < isSelected.GetLength(0); x++)
            for (int y = 0; y < isSelected.GetLength(1); y++)
                isSelected[x, y] = false;
    }

    void SelectAllSimilarTiles(int curX, int curY, int idxColor)
    {
        isSelected[curX, curY] = true;
        if (curX - 1 >= 0)
        {
            if (colorIdxBoard[curX - 1, curY] == idxColor && !isSelected[curX - 1, curY])
                SelectAllSimilarTiles(curX - 1, curY, idxColor);
        }
        if (curX + 1 < colorIdxBoard.GetLength(0))
        {
            if (colorIdxBoard[curX + 1, curY] == idxColor && !isSelected[curX + 1, curY])
                SelectAllSimilarTiles(curX + 1, curY, idxColor);
        }
        if (curY - 1 >= 0)
        {
            if (colorIdxBoard[curX, curY - 1] == idxColor && !isSelected[curX, curY - 1])
                SelectAllSimilarTiles(curX, curY - 1, idxColor);
        }
        if (curY + 1 < colorIdxBoard.GetLength(1))
        {
            if (colorIdxBoard[curX, curY + 1] == idxColor && !isSelected[curX, curY + 1])
                SelectAllSimilarTiles(curX, curY + 1, idxColor);
        }
        return;
    }
    IEnumerator delayRotation()
    {
        var needyTimer = gameObject.transform.Find("NeedyTimer(Clone)");
        if (needyTimer != null)
        {
            needyTimer.gameObject.transform.Rotate(Vector3.up * -90);
        }
        yield break;
    }

    void UpdateGrid()
    {
        for (int x = 0; x < rowRenderersWithSelectables.Length; x++)
        {
            for (int y = 0; y < rowRenderersWithSelectables[x].wallRenderers.Length; y++)
            {
                int idxShown = colorIdxBoard[y, x];
                rowRenderersWithSelectables[x].wallRenderers[y].enabled = idxShown > 0;
                rowRenderersWithSelectables[x].wallRenderers[y].material.color =
                    (idxShown > 0 ? colorList[colorIdxSet[idxShown - 1]] : Color.black) * (isSelected[y, x] ? 1 : 0.5f) +
                    Color.white * (isSelected[y, x] ? 0f : 0.5f);
                if (idxShown > 0)
                    rowRenderersWithSelectables[x].wallRenderers[y].material.SetTexture("_MainTex", colorblindTextures[colorIdxSet[idxShown - 1]]);
            }
        }
    }
    
    void TwitchHandleForcedSolve()
    {
        StopAllCoroutines();
        HandleDeactivation();
    }

}
