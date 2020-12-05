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

    Color[] colorList = {
        Color.cyan,
        Color.blue,
        Color.gray,
        Color.yellow,
        Color.red,
        Color.magenta
    };
    string colorString = "CBAYRM";

    bool[,] isSelected = new bool[9, 13];
    int[,] colorIdxAll = new int[9, 13];
    static int modIdCounter = 1;
    int modID;
    bool isActive = false, permDeactivate;
    List<Vector3> startRowCoords = new List<Vector3>();
    float timeLeftManaged = 10f, dynamicRate = .8f;

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
            }
            else
                needySelf.HandlePass();
        };
        List<KMSelectable> allCellSelectables = new List<KMSelectable>(); // Mainly for compacting lines, required for usage otherwise.
        for (int i = 0; i < rowRenderersWithSelectables.Length; i++)
        {
            RowRendererPlus rowRenderer = rowRenderersWithSelectables[i];
            allCellSelectables.AddRange(rowRenderer.rowSelectables);

            startRowCoords.Add(rowRenderer.transform.localPosition);

            for (int x = 0; x < rowRenderer.rowSelectables.Length; x++)
            {
                int tRow = x, tCol = i;
                rowRenderer.rowSelectables[x].OnInteract += delegate {
                    rowRenderer.rowSelectables[tRow].AddInteractionPunch(0.1f);
                    mAudio.PlaySoundAtTransform("tick", rowRenderer.rowSelectables[tRow].transform);
                    if (isActive)
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

        modSelfSelectable.Children = allCellSelectables.ToArray();
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
    }
    void QuickLog(string value)
    {
        Debug.LogFormat("[Collaspe #{0}] {1}", modID, value);
    }

    void LogBoard()
    {
        QuickLog("o---------o");
        for (int y = colorIdxAll.GetLength(1) - 1; y >= 0; y--)
        {
            List<string> valuesSet = new List<string>();
            for (int x = 0; x < colorIdxAll.GetLength(0); x++)
            {
                int value = colorIdxAll[x, y];
                valuesSet.Add(value == 0 ? " " : colorString[value - 1].ToString());
            }
            QuickLog(string.Format("|{0}|", valuesSet.Join("")));
        }
        QuickLog("o---------o");
    }

    void HandleDeactivation()
    {
        needySelf.SetResetDelayTime(float.MaxValue, float.MaxValue); // Makes it so that the needy is disabled forever.
        needySelf.HandlePass();
        needySelf.HandleStrike();
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
            if (colorIdxAll[row, col] == 0) return;
            DeselectAllTiles();
            SelectAllSimilarTiles(row, col, colorIdxAll[row, col]);
            if (CountSelectedTiles() < 3)
            {
                DeselectAllTiles();
            }
        }
        else
        {
            for (int x = 0; x < isSelected.GetLength(0); x++)
                for (int y = 0; y < isSelected.GetLength(1); y++)
                {
                    if (isSelected[x, y])
                        colorIdxAll[x, y] = 0;
                }
            StopCoroutine(blockLifter);
            StopCoroutine(timeManager);
            DeselectAllTiles();
            CollaspeAllTiles();
            StartCoroutine(blockLifter);
            StartCoroutine(timeManager);
        }
        UpdateGrid();
    }
    void CollaspeAllTiles()
    {
        for (int x = 0; x < colorIdxAll.GetLength(0); x++)
            for (int y = 0; y < colorIdxAll.GetLength(1) - 1; y++)
                for (int z = 0; z < colorIdxAll.GetLength(1) - 1; z++)
                {
                    if (colorIdxAll[x, z] == 0)
                    {
                        colorIdxAll[x, z] = colorIdxAll[x, z + 1];
                        colorIdxAll[x, z + 1] = 0;
                    }
                }
    }

    IEnumerator HandleGameOver()
    {
        for (float t = 0; t <= 1f; t = Mathf.Min(t + Time.deltaTime, 1f))
        {
            yield return null;
            for (int y = colorIdxAll.GetLength(1) - 1; y >= 0; y--)
            {
                for (int x = 0; x < colorIdxAll.GetLength(0); x++)
                {
                    int curColorIdx = colorIdxAll[x, y];
                    if (curColorIdx != 0)
                    {
                        rowRenderersWithSelectables[y].wallRenderers[x].material.color = colorList[curColorIdx - 1] * 0.5f * (1 - t) + Color.white * 0.5f * (1 - t);
                    }
                }
            }
            if (t >= 1f) break;
        }
        for (int y = colorIdxAll.GetLength(1) - 1; y >= 0; y--)
        {
            for (int x = 0; x < colorIdxAll.GetLength(0); x++)
            {
                colorIdxAll[x, y] = 0;
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
            int[] heightCounts = new int[9];
            for (int x = 0; x < colorIdxAll.GetLength(0); x++)
                for (int y = 0; y < colorIdxAll.GetLength(1); y++)
                    heightCounts[x] += colorIdxAll[x, y] != 0 ? 1 : 0;
            if (heightCounts.Max() < 13)
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
            int[] heightCounts = new int[9];
            for (int x = 0; x < colorIdxAll.GetLength(0); x++)
                for (int y = 0; y < colorIdxAll.GetLength(1); y++)
                    heightCounts[x] += colorIdxAll[x, y] != 0 ? 1 : 0;
            if (heightCounts.Max() < 13)
                yield return AnimateNextRowDynamicAnim();
            else
            {
                timeLeftManaged -= Time.deltaTime;
                //mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.NeedyWarning, transform);
                if (timeLeftManaged < 0f)
                {
                    HandleDeactivation();
                }
            }
        }
    }
    IEnumerator AnimateNextRowDynamicAnim()
    {
        int maxSolvables = Mathf.Max(bombInfo.GetSolvableModuleIDs().Count(), 1), currentlySolved = bombInfo.GetSolvedModuleIDs().Count();

        yield return null;

        int maxColorsPossible = 2 + (maxSolvables * 3 < currentlySolved * 10 ? 1 : 0) + (maxSolvables * 9 < currentlySolved * 10 ? 1 : 0);

        int[] nextRow = new int[9];
        for (int x = 0; x < nextRow.Length; x++)
        {
            nextRow[x] = uernd.Range(1, maxColorsPossible + 1);
        }
        rowRenderersNextSet.gameObject.SetActive(true);
        for (float x = 0; x < 1f; x += Time.deltaTime * dynamicRate * Mathf.Min(1f, (currentlySolved + 1f) / maxSolvables))
        {
            yield return null;
            rowRenderersNextSet.transform.localPosition = new Vector3(0, 0, -6f - (0.5f * (1f - x)));
            rowRenderersNextSet.transform.localScale = new Vector3(1, 1, x);

            currentlySolved = bombInfo.GetSolvedModuleIDs().Count();

            for (int z = 0; z < rowRenderersNextSet.wallRenderers.Length; z++)
            {
                rowRenderersNextSet.wallRenderers[z].material.color = colorList[nextRow[z] - 1] * 0.5f +
                    Color.gray * 0.5f;
            }
            for (int y = 0; y < rowRenderersWithSelectables.Length; y++)
            {
                rowRenderersWithSelectables[y].transform.localPosition = startRowCoords[y] + new Vector3(0, 0, x);
            }
        }
        for (int x = 0; x < colorIdxAll.GetLength(0); x++)
            for (int y = colorIdxAll.GetLength(1) - 1; y >= 0; y--)
            {
                if (y > 0)
                {
                    colorIdxAll[x, y] = colorIdxAll[x, y - 1];
                    isSelected[x, y] = isSelected[x, y - 1];
                }
                else
                {
                    colorIdxAll[x, y] = nextRow[x];
                    isSelected[x, y] = false;
                }
            }
        for (int y = 0; y < rowRenderersWithSelectables.Length; y++)
        {
            rowRenderersWithSelectables[y].transform.localPosition = startRowCoords[y];
        }
        rowRenderersNextSet.transform.localPosition = new Vector3(0, 0, -6.5f);
        rowRenderersNextSet.transform.localScale = new Vector3(1, 1, 0);
        rowRenderersNextSet.gameObject.SetActive(false);
        UpdateGrid();
    }
    IEnumerator AnimateNextRowAnim(float speed = 1f)
    {
        if (speed <= 0f) yield break;
        yield return null;
        int[] nextRow = new int[9];
        for (int x = 0; x < nextRow.Length; x++)
        {
            nextRow[x] = uernd.Range(1, 3);
        }
        rowRenderersNextSet.gameObject.SetActive(true);
        for (float x = 0; x < 1f; x += Time.deltaTime * speed)
        {
            yield return null;
            rowRenderersNextSet.transform.localPosition = new Vector3(0, 0, -6f - (0.5f * (1f - x)));
            rowRenderersNextSet.transform.localScale = new Vector3(1, 1, x);

            for (int z = 0; z < rowRenderersNextSet.wallRenderers.Length; z++)
            {
                rowRenderersNextSet.wallRenderers[z].material.color = colorList[nextRow[z] - 1] * 0.5f +
                    Color.gray *  0.5f;
            }
            for (int y = 0; y < rowRenderersWithSelectables.Length; y++)
            {
                rowRenderersWithSelectables[y].transform.localPosition = startRowCoords[y] + new Vector3(0, 0, x);
            }
        }
        for (int x = 0; x < colorIdxAll.GetLength(0); x++)
            for (int y = colorIdxAll.GetLength(1) - 1; y >= 0; y--)
            {
                if (y > 0)
                {
                    colorIdxAll[x, y] = colorIdxAll[x, y - 1];
                    isSelected[x, y] = isSelected[x, y - 1];
                }
                else
                {
                    colorIdxAll[x, y] = nextRow[x];
                    isSelected[x, y] = false;
                }
            }
        for (int y = 0; y < rowRenderersWithSelectables.Length; y++)
        {
            rowRenderersWithSelectables[y].transform.localPosition = startRowCoords[y];
        }
        rowRenderersNextSet.transform.localPosition = new Vector3(0, 0, -6.5f);
        rowRenderersNextSet.transform.localScale = new Vector3(1, 1, 0);
        rowRenderersNextSet.gameObject.SetActive(false);
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
            if (colorIdxAll[curX - 1, curY] == idxColor && !isSelected[curX - 1, curY])
                SelectAllSimilarTiles(curX - 1, curY, idxColor);
        }
        if (curX + 1 < colorIdxAll.GetLength(0))
        {
            if (colorIdxAll[curX + 1, curY] == idxColor && !isSelected[curX + 1, curY])
                SelectAllSimilarTiles(curX + 1, curY, idxColor);
        }
        if (curY - 1 >= 0)
        {
            if (colorIdxAll[curX, curY - 1] == idxColor && !isSelected[curX, curY - 1])
                SelectAllSimilarTiles(curX, curY - 1, idxColor);
        }
        if (curY + 1 < colorIdxAll.GetLength(1))
        {
            if (colorIdxAll[curX, curY + 1] == idxColor && !isSelected[curX, curY + 1])
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
	void RequestDetonation()
    {
        QuickLog("You let the needy run out. The bomb goes along as well.");
        if (Application.isEditor)
        {
            var bombComponent = GetComponentInParent<KMBomb>();
            if (bombComponent != null)
            {
                var timer = bombComponent.gameObject.transform.Find("TimerModule(Clone)");
                if (timer != null)
                {
                    var script = timer.GetComponent("TimerModule");
                    if (script != null)
                        script.SetValue("ExplodedToTime", true);
                }
                else
                    Debug.LogFormat("can't find component");
            }
            else
                Debug.LogFormat("can't find component");
        }
        else
        {
            var bombComponent = GetComponentInParent<KMBomb>();
            if (bombComponent != null)
            {
                var script = bombComponent.GetComponent("Bomb");
                if (script != null)
                {
                    var strikeCnt = script.GetValue<int>("NumStrikesToLose");
                    script.SetValue("NumStrikes", strikeCnt - 1);
                    var compSelf = GetComponent("BombComponent");
                    if (compSelf != null)
                        script.CallMethod("OnStrike", compSelf);
                }
                else
                    Debug.LogFormat("can't find component");
            }
            else
                Debug.LogFormat("can't find component");
        }
    }

    void UpdateGrid()
    {
        for (int x = 0; x < rowRenderersWithSelectables.Length; x++)
        {
            for (int y = 0; y < rowRenderersWithSelectables[x].wallRenderers.Length; y++)
            {
                int idxShown = colorIdxAll[y, x];
                rowRenderersWithSelectables[x].wallRenderers[y].enabled = idxShown > 0;
                rowRenderersWithSelectables[x].wallRenderers[y].material.color =
                    (idxShown > 0 ? colorList[idxShown - 1] : Color.black) * (isSelected[y, x] ? 1 : 0.5f) +
                    Color.white * (isSelected[y, x] ? 0f : 0.5f);
            }
        }
    }

	// Update is called once per frame
	void Update () {
	}
}
