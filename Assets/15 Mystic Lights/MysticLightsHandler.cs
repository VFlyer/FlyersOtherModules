using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MysticLightsHandler : MonoBehaviour {

    public Light[] lights = new Light[16];
    public Light animLight;

    public KMSelectable[] tileSelectables = new KMSelectable[16];
    public GameObject AnimPointTL, AnimPointBR, AnimPointD, TileAnim, StatusLight;
    public MeshRenderer[] meshRenderers = new MeshRenderer[16];
    public TextMesh[] textMeshes = new TextMesh[16];
    public GameObject[] tiles = new GameObject[16];
    public Material[] materials = new Material[2];
    public KMBombModule moduleSelf;
    public KMAudio kMAudio;
    public KMColorblindMode colorblindMode;
    public KMBombInfo bombInfo;

    private bool hasExitedInitial = false;
    private bool?[,] lightStates = new bool?[4,4];
    

    private bool solved = false;
    private bool playingAnim = false;
    private bool isGenerating = false;
    private bool colorblindEnabled = false;

    private readonly List<int> inputsOverall = new List<int>();
    private int index = 0;

    private static int modid = 1;
    private int curmodid;

    private readonly string[] debugCoordCol = new string[] { "A", "B", "C", "D" };
    private readonly string[] debugCoordRow = new string[] { "1", "2", "3", "4" };

    void Awake()
    {
        curmodid = modid++;
        try
        {
            colorblindEnabled = colorblindMode.ColorblindModeActive;
        }
        finally
        {
        }
    }
    // Use this for initialization
    void Start () {

        float scalar = transform.lossyScale.x;// Account for scale factor for this module being smaller, check KTANE official discord in #modding
        foreach (Light onelight in lights)
        {
            onelight.range *= scalar;
        }
        animLight.range *= scalar;
        TileAnim.SetActive(false);
        Generate4x4Puzzle();
        for (int selidx = 0; selidx < tileSelectables.Length; selidx++)
        {
            int y = selidx;
            tileSelectables[selidx].OnInteract += delegate
            {
                tileSelectables[y].AddInteractionPunch();
                kMAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonRelease, transform);
                if (!solved)
                {
                    inputsOverall.Add(y);
                }
                return false;
            };
        }
        bombInfo.OnBombExploded += delegate
        {
            if (solved || isGenerating) return;

            string debugMoves = "";
            foreach (int inIdx in inputsOverall)
            {
                debugMoves += debugCoordCol[inIdx % 4] + debugCoordRow[inIdx / 4 % 4] + " ";
            }
            debugMoves.Trim();
            Debug.LogFormat("[15 Mystic Lights #{0}]: The following moves used on the given board when the bomb blew were {1}", curmodid, debugMoves);
        };

        moduleSelf.OnActivate += delegate {
            if (TwitchPlaysActive)
            {
                StatusLight.SetActive(true);
            }
            else
            {
                StatusLight.SetActive(false);
            }
        };
        //StatusLight.SetActive(false);
    }
    void UpdateAllLights()
    {
        for (int x = 0; x < lightStates.GetLength(0); x++)
        {
            for (int y = 0; y < lightStates.GetLength(1); y++)
            {
                bool? ligStaSGL = lightStates[x, y];
                if (ligStaSGL != null)
                {
                    tiles[4 * x + y].SetActive(true);
                    lights[4 * x + y].color = (bool)ligStaSGL ? Color.yellow : Color.blue;
                    meshRenderers[4 * x + y].material = (bool)ligStaSGL ? materials[0] : materials[1];
                    textMeshes[4 * x + y].text = (bool)ligStaSGL ? "1" : "0";
                    textMeshes[4 * x + y].color = (bool)ligStaSGL ? Color.black : Color.white;
                }
                else
                {
                    tiles[4 * x + y].SetActive(false);
                    textMeshes[4 * x + y].text = "";
                }
            }
        }
        if (colorblindEnabled)
        {
            for (int idx = 0; idx < textMeshes.Length; idx++)
            {
                textMeshes[idx].gameObject.SetActive(true);
            }
        }
        else
        for (int idx = 0; idx < textMeshes.Length; idx++)
        {
            textMeshes[idx].gameObject.SetActive(false);
        }
    }
    void UpdateSpecificLight(int xIndex,int yIndex)
    {
        if (xIndex >= 0 && xIndex < 4 && yIndex >= 0 && yIndex < 4)
        {
            bool? ligStaSGL = lightStates[xIndex, yIndex];
            if (ligStaSGL != null)
            {
                tiles[4 * xIndex + yIndex].SetActive(true);
                lights[4 * xIndex + yIndex].color = (bool)ligStaSGL ? Color.yellow : Color.blue;
                meshRenderers[4 * xIndex + yIndex].material = (bool)ligStaSGL ? materials[0] : materials[1];
                textMeshes[4 * xIndex + yIndex].text = (bool)ligStaSGL ? "1" : "0";
                textMeshes[4 * xIndex + yIndex].color = (bool)ligStaSGL ? Color.black : Color.white;
            }
            else
            {
                tiles[4 * xIndex + yIndex].SetActive(false);
                textMeshes[4 * xIndex + yIndex].text = "";
            }
        }
    }
    void HandleInteraction(int posX, int posY)
    {
        //if (!isGenerating)
        //    print("Handling interaction at (" + posX + "," + posY + ")");
        int handleMovement = -1;
        // Handle Inverting Lights
        if (posX < lightStates.GetLength(0) && posX >= 0 && posY < lightStates.GetLength(1) && posY >= 0 && lightStates[posX,posY] != null)
        {
            lightStates[posX, posY] = !lightStates[posX, posY];
            if (posX - 1 >= 0)
            {
                if (lightStates[posX - 1, posY] != null)
                    lightStates[posX - 1, posY] = !lightStates[posX - 1, posY];
                else
                    handleMovement = 0;
            }
            if (posX + 1 < lightStates.GetLength(0))
            {
                if (lightStates[posX + 1, posY] != null)
                    lightStates[posX + 1, posY] = !lightStates[posX + 1, posY];
                else
                    handleMovement = 2;
            }
            if (posY - 1 >= 0)
            {
                if (lightStates[posX, posY - 1] != null)
                    lightStates[posX, posY - 1] = !lightStates[posX, posY - 1];
                else
                    handleMovement = 1;
            }
            if (posY + 1 < lightStates.GetLength(1))
            {
                if (lightStates[posX, posY + 1] != null)
                    lightStates[posX, posY + 1] = !lightStates[posX, posY + 1];
                else
                    handleMovement = 3;
            }
        }
        // Handle Moving Tiles
        if (handleMovement == 0)// Moving -x on the grid
        {
            lightStates[posX - 1, posY] = lightStates[posX, posY];
            lightStates[posX, posY] = null;
        }
        else if (handleMovement == 1)// Moving -y on the grid
        {
            lightStates[posX, posY - 1] = lightStates[posX, posY];
            lightStates[posX, posY] = null;
        }
        else if (handleMovement == 2)// Moving +x on the grid
        {
            lightStates[posX + 1, posY] = lightStates[posX, posY];
            lightStates[posX, posY] = null;
        }
        else if (handleMovement == 3)// Moving +y on the grid
        {
            lightStates[posX, posY + 1] = lightStates[posX, posY];
            lightStates[posX, posY] = null;
        }
        if (!isGenerating)
            StartCoroutine(PlaySlidingAnim(handleMovement));
    }

    // Update is called once per frame
    void Update()
    {
        if (!playingAnim && !solved && !isGenerating)
        {
            if (index < inputsOverall.Count)
            {
                var y = inputsOverall[index];
                //Debug.LogFormat("[15 Mystic Lights #{0}]: Handling button press at {1}{2}", curmodid, debugCoordCol[y / 4 % 4], debugCoordRow[y % 4]);
                HandleInteraction(y / 4, y % 4);
                index++;
            }
        }
        else if (inputsOverall.Count > 0 && (solved || isGenerating))
        {
            string debugMoves = "";
            foreach (int inIdx in inputsOverall)
            {
                debugMoves += debugCoordCol[inIdx % 4] + debugCoordRow[inIdx /4 % 4] + " ";
            }
            debugMoves.Trim();
            Debug.LogFormat("[15 Mystic Lights #{0}]: The following moves used on the given board were {1}", curmodid, debugMoves);
            inputsOverall.Clear();
            index = 0;
        }
    }

    void DebugCurrentBoard()
    {
        string toDebug = "";
        for (int xidx = 0; xidx < lightStates.GetLength(0); xidx++)
        {
            for (int yidx = 0; yidx < lightStates.GetLength(1); yidx++)
            {
                if (lightStates[xidx, yidx] == null)
                {
                    toDebug += " ";
                }
                else
                {
                    toDebug += (bool)lightStates[xidx, yidx] ? "1" : "0";
                }
            }
            toDebug += "\n";
        }
        Debug.LogFormat("[15 Mystic Lights #{0}]: Light states generated are: \n{1}", curmodid, toDebug);
    }
    void Generate4x4Puzzle()
    {
        isGenerating = true;
        while (isAllCorrect())
        { 
            bool[] choices = new bool[] { true, false };
            bool startingState = choices[Random.Range(0, choices.Length)];
            for (int xidx = 0; xidx < lightStates.GetLength(0); xidx++)
            {
                for (int yidx = 0; yidx < lightStates.GetLength(1); yidx++)
                {
                    lightStates[xidx, yidx] = startingState;
                }
            }
            int moveCount = Random.Range(0, 16) + 1;
            for (int x = 0; x < moveCount; x++)
            {
                int xIndex = Random.Range(0, 4);
                int yIndex = Random.Range(0, 4);
                HandleInteraction(xIndex, yIndex);
            }
        }
        isGenerating = false;
        DebugCurrentBoard();
        UpdateAllLights();
    }
    void Generate4x4WithHolePuzzle()
    {
        isGenerating = true;
        while (isAllCorrect())
        {
            bool[] choices = new bool[] { true, false };
            bool startingState = choices[Random.Range(0, choices.Length)];
            int holeX = Random.Range(0, 4);
            int holeY = Random.Range(0, 4);
            for (int xidx = 0; xidx < lightStates.GetLength(0); xidx++)
            {
                for (int yidx = 0; yidx < lightStates.GetLength(1); yidx++)
                {
                    if (xidx == holeX && yidx == holeY)
                    {
                        lightStates[xidx, yidx] = null;
                    }
                    else
                    {
                        lightStates[xidx, yidx] = startingState;
                    }
                }
            }
            int moveCount = Random.Range(9, 25) + 1;
            for (int x = 0; x < moveCount; x++)
            {
                int xIndex = Random.Range(0, 4);
                int yIndex = Random.Range(0, 4);
                HandleInteraction(xIndex, yIndex);
            }
        }

        isGenerating = false;
        DebugCurrentBoard();
        UpdateAllLights();
    }
    bool isAllCorrect()
    {
        bool output = true;
        List<bool?> uniqueinputs = new List<bool?>();
        foreach (bool? lightSingle in lightStates)
        {
            if (lightSingle != null && !uniqueinputs.Contains(lightSingle))
            {
                uniqueinputs.Add(lightSingle);
            }
        }
        output = uniqueinputs.Count <= 1;
        return output;
    }
    int[] GetFirstNullTile()
    {
        for (int x = 0; x < lightStates.GetLength(0); x++)
        {
            for (int y = 0; y < lightStates.GetLength(1); y++)
            {
                bool? ligStaSGL = lightStates[x, y];
                if (ligStaSGL == null)
                {
                    return new int[] { x, y };
                }
            }
        }
        return new int[] { -1, -1 };
    }
    bool? GetFirstNonNullTileState()
    {
        for (int x = 0; x < lightStates.GetLength(0); x++)
        {
            for (int y = 0; y < lightStates.GetLength(1); y++)
            {
                bool? ligStaSGL = lightStates[x, y];
                if (ligStaSGL != null)
                {
                    return ligStaSGL;
                }
            }
        }
        return null;
    }
    int animDelay = 30;
    IEnumerator PlayTransitionAnim()
    {
        isGenerating = true;
        playingAnim = true;
        for (int idx = 0; idx < textMeshes.Length; idx++)
        {
            textMeshes[idx].gameObject.SetActive(false);
        }
        kMAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.WireSequenceMechanism, transform);
        Vector3 pointD = AnimPointD.transform.localPosition;
        for (int x = 0; x <= animDelay; x++)
        {
            float zAnim = 0;
            for (int p = 0; p < x; p++)
            {
                zAnim += pointD.z;
            }
            zAnim /= animDelay;
            for (int idx = 0; idx < tiles.Length; idx++)
            {
                tiles[idx].transform.localPosition = new Vector3(tiles[idx].transform.localPosition.x, tiles[idx].transform.localPosition.y, zAnim);
            }
            yield return new WaitForSeconds(0);
        }
        Generate4x4WithHolePuzzle();
        isGenerating = true;
        for (int x = animDelay; x >= 0; x--)
        {
            float zAnim = 0;
            for (int p = 0; p < x; p++)
            {
                zAnim += pointD.z;
            }
            zAnim /= animDelay;
            for (int idx = 0; idx < tiles.Length; idx++)
            {
                tiles[idx].transform.localPosition = new Vector3(tiles[idx].transform.localPosition.x, tiles[idx].transform.localPosition.y, zAnim);
            }
            yield return new WaitForSeconds(0);
        }
        playingAnim = false;
        isGenerating = false;
        yield return null;
    }
    IEnumerator PlayResetAnim()
    {
        
        isGenerating = true;
        playingAnim = true;
        for (int idx = 0; idx < textMeshes.Length; idx++)
        {
            textMeshes[idx].gameObject.SetActive(false);
        }
        kMAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.WireSequenceMechanism, transform);
        Vector3 pointD = AnimPointD.transform.localPosition;
        for (int x = 0; x <= animDelay; x++)
        {
            float zAnim = 0;
            for (int p = 0; p < x; p++)
            {
                zAnim += pointD.z;
            }
            zAnim /= animDelay;
            for (int idx = 0; idx < tiles.Length; idx++)
            {
                tiles[idx].transform.localPosition = new Vector3(tiles[idx].transform.localPosition.x, tiles[idx].transform.localPosition.y, zAnim);
            }
            yield return new WaitForSeconds(0);
        }

        for (int posx = 0;posx<4;posx++)
        {
            for (int posy = 0; posy < 4; posy++)
            {
                lightStates[posx, posy] = false;
                UpdateSpecificLight(posx, posy);
            }
        }
        if (hasExitedInitial)
        {
            Generate4x4WithHolePuzzle();
        }
        else
        {
            Generate4x4Puzzle();
        }
        isGenerating = true;
        for (int x = animDelay; x >= 0; x--)
        {
            float zAnim = 0;
            for (int p = 0; p < x; p++)
            {
                zAnim += pointD.z;
            }
            zAnim /= animDelay;
            for (int idx = 0; idx < tiles.Length; idx++)
            {
                tiles[idx].transform.localPosition = new Vector3(tiles[idx].transform.localPosition.x, tiles[idx].transform.localPosition.y, zAnim);
            }
            yield return new WaitForSeconds(0);
        }
        playingAnim = false;
        isGenerating = false;
        yield return null;
    }
    IEnumerator PlaySlidingAnim(int direction)
    {
        playingAnim = true;
        TileAnim.SetActive(true);
        int[] emptyTile = GetFirstNullTile();
        int xIdx = emptyTile[0];
        int yIdx = emptyTile[1];
        if (xIdx < lightStates.GetLength(0) && xIdx >= 0 && yIdx < lightStates.GetLength(1) && yIdx >= 0 && direction >= 0 && direction < 4)
        {
            float[] coordStart = new float[2] { 0, 0 };
            float[] coordEnd = new float[2] { 0, 0 };
            Vector3 pointTL = AnimPointTL.transform.localPosition;
            Vector3 pointBR = AnimPointBR.transform.localPosition;
            // The idea for this is to grab the specified point by using local coordinates to manipulate the position.
            for (int p = 0; p < 3; p++)
            {
                // Grab end points of the animation
                if (xIdx > p)
                    coordEnd[0] += pointBR.x;
                else
                    coordEnd[0] += pointTL.x;
                if (yIdx > p)
                    coordEnd[1] += pointBR.y;
                else
                    coordEnd[1] += pointTL.y;
                // Grab start points of the animation
                if (direction % 2 == 0)// If the direction involves the x-axis
                {
                    if (direction / 2 <= 0)// If the direction is negative
                    {
                        if (p < xIdx - 1)
                            coordStart[0] += pointBR.x;
                        else
                            coordStart[0] += pointTL.x;
                    }
                    else// If the direction is positive
                    {
                        if (p < xIdx + 1)
                            coordStart[0] += pointBR.x;
                        else
                            coordStart[0] += pointTL.x;
                    }
                    if (p < yIdx)
                        coordStart[1] += pointBR.y;
                    else
                        coordStart[1] += pointTL.y;
                }
                else// if the direction involves the y-axis
                {
                    if (direction / 2 > 0)// If the direction is negative
                    {
                        if (p < yIdx + 1)
                            coordStart[1] += pointBR.y;
                        else
                            coordStart[1] += pointTL.y;
                    }
                    else// If the direction is positive
                    {
                        if (p < yIdx - 1)
                            coordStart[1] += pointBR.y;
                        else
                            coordStart[1] += pointTL.y;
                    }
                    if (p < xIdx)
                        coordStart[0] += pointBR.x;
                    else
                        coordStart[0] += pointTL.x;
                }
            }
            coordEnd[0] /= 3f;
            coordEnd[1] /= 3f;
            coordStart[0] /= 3f;
            coordStart[1] /= 3f;
            int xIntIdx = direction == 0 ? xIdx - 1 : direction == 2 ? xIdx + 1 : xIdx; // X coordinate of where the module was interacted
            int yIntIdx = direction == 1 ? yIdx - 1 : direction == 3 ? yIdx + 1 : yIdx; // Y coordinate of where the module was interacted
            //print("Suggest last interaction was at (" + xIntIdx + "," + yIntIdx + ")");
            // Update the light around the tile that was moved.
            if (yIdx + 1 < 4 && direction != 3)
            {
                UpdateSpecificLight(xIdx, yIdx + 1);
            }
            if (yIdx - 1 >= 0 && direction != 1)
            {
                UpdateSpecificLight(xIdx, yIdx - 1);
            }
            if (xIdx + 1 < 4 && direction != 2)
            {
                UpdateSpecificLight(xIdx + 1, yIdx);
            }
            if (xIdx - 1 >= 0 && direction != 0)
            {
                UpdateSpecificLight(xIdx - 1, yIdx);
            }
            for (int i = 0; i < animDelay/2; i++)
            {
                float finalposX = 0;
                float finalposY = 0;
                for (int x = 0; x < animDelay/2; x++)
                {
                    if (x < i)
                    {
                        finalposX += coordStart[0];
                        finalposY += coordStart[1];
                    }
                    else
                    {
                        finalposX += coordEnd[0];
                        finalposY += coordEnd[1];
                    }
                    
                }
                finalposX /= animDelay/2;
                finalposY /= animDelay/2;
                TileAnim.transform.localPosition = new Vector3(finalposY,finalposX,0.0f);
                bool lgtSteSgl = (bool)lightStates[xIntIdx, yIntIdx];
                TileAnim.GetComponent<MeshRenderer>().material = lgtSteSgl ? materials[0] : materials[1];
                animLight.color = lgtSteSgl ? Color.yellow : Color.blue;
                animLight.enabled = true;
                UpdateSpecificLight(xIdx, yIdx);
                yield return new WaitForSeconds(0f);
            }
            kMAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);   
        }
        TileAnim.SetActive(false);
        animLight.enabled = false;
        
        UpdateAllLights();
        if (isAllCorrect())
        {
            Debug.LogFormat("[15 Mystic Lights #{0}]: All the lights are in 1 color.", curmodid);
            if (hasExitedInitial)
            {

                int[] firstEmpty = GetFirstNullTile();
                StartCoroutine(PlaySolveAnim(firstEmpty[0], firstEmpty[1]));
                solved = true;
            }
            else
            {
                StartCoroutine(PlayTransitionAnim());
                hasExitedInitial = true;
            }
        }
        playingAnim = false;
        yield return null;
    }
    IEnumerator PlaySolveAnim(int ycor, int xcor)
    {
        playingAnim = true;
        kMAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.WireSequenceMechanism, transform);
        Vector3 pointTL = AnimPointTL.transform.localPosition;
        Vector3 pointBR = AnimPointBR.transform.localPosition;
        Vector3 pointD = AnimPointD.transform.localPosition;
        //Vector3 LastStatLightScale = new Vector3(StatusLight.transform.localScale.x, StatusLight.transform.localScale.y, StatusLight.transform.localScale.z);
        bool lgtSteSgl = (bool)GetFirstNonNullTileState();
        for (int idx = 0; idx < textMeshes.Length; idx++)
        {
            textMeshes[idx].gameObject.SetActive(false);
        }
        for (int x = animDelay*2; x >= 0; x--)
        {
            float localX = 0;
            float localY = 0;
            float localZ = 0;
            for (int p = 0; p < 3; p++)
            {
                // Grab end points of the animation
                if (xcor > p)
                    localX += pointBR.x;
                else
                    localX += pointTL.x;
                if (ycor > p)
                    localY += pointBR.y;
                else
                    localY += pointTL.y;
            }
            for (int y = 0; y < x; y++)
            {
                localZ += pointD.z;
            }
            localZ /= animDelay*2;
            localX /= 3;
            localY /= 3;
            TileAnim.SetActive(true);
            StatusLight.SetActive(true);
            TileAnim.transform.localPosition = new Vector3(localX, localY, localZ);
            StatusLight.transform.localPosition = new Vector3(localX, localY, localZ+.005f);
            
            TileAnim.GetComponent<MeshRenderer>().material = lgtSteSgl ? materials[0] : materials[1];
            animLight.color = lgtSteSgl ? Color.yellow : Color.blue;
            animLight.enabled = true;
            yield return new WaitForSeconds(0);
        }
        TileAnim.SetActive(false);
        int tileIndex = xcor + 4 * ycor;
        tiles[tileIndex].SetActive(true);
        meshRenderers[tileIndex].material = lgtSteSgl ? materials[0] : materials[1];
        lights[tileIndex].enabled = true;
        lights[tileIndex].color = lgtSteSgl ? Color.yellow : Color.blue;
        moduleSelf.HandlePass();
        yield return null;
    }
    public readonly string TwitchHelpMessage = "Press a button with “!{0} A1 B2 C3 D4...”. Columns are labeled A-D from left to right, rows are labeled 1-4 from top to bottom. Commands may be voided if the module enters a generation state or a solve state. \"press\" is optional.\nTo activate colorblind: \"!{0} colorblind\" To generate a new board: \"!{0} regen[erate]\" or \"!{0} reset\". You can only regenerate up to 3 times on this module!";

    bool TwitchPlaysActive;
    readonly string RowIDX = "abcd";
    readonly string ColIDX = "1234";
    readonly string[] resetString = new string[] { "0th", "first", "second", "last" };
    int resetsCounted = 0;
    IEnumerator ProcessTwitchCommand(string command)
    {
        string proCmd = command.ToLower();
        while (isGenerating || playingAnim)
        {
            yield return "trycancel";
        }
        if (proCmd.RegexMatch(@"^regen(erate)?$")|| proCmd.RegexMatch(@"^reset$"))
        {
            if (resetsCounted < 3)
            {
                Debug.LogFormat("[15 Mystic Lights #{0}]: Board Regen Issued by TP.", curmodid);
                StartCoroutine(PlayResetAnim());
                resetsCounted++;

                yield return "sendtochat This module is on the " +resetString[resetsCounted]+" reset.";
                yield break;
            }
            else
            {
                yield return "sendtochaterror This module can no longer accept resets. All chances to reset have been used up.";
                yield break;
            }
        }
        else if (proCmd.RegexMatch(@"^colou?rblind$"))
        {
            colorblindEnabled = true;
            UpdateAllLights();
            yield return null;
            yield break;
        }
        if (proCmd.StartsWith("press "))
        {
            proCmd = proCmd.Substring(6, proCmd.Length - 6);
        }
        List<int> cordList = new List<int>();
        foreach (string cord in proCmd.Split(' '))
        {
            if (cord.Length == 2 && cord.RegexMatch(@"^[a-d][1-4]$"))
            {
                char[] cordchrs = cord.ToCharArray();
                cordList.Add(RowIDX.IndexOf(cordchrs[0]) + ColIDX.IndexOf(cordchrs[1]) * 4);
            }
            else
            {
                yield return "sendtochaterror I'm sorry but what is " + cord + "?";
                yield break;
            }
        }
        for (int x=0;x<cordList.Count;x++)
        {
            yield return null;
            if (!(isGenerating || solved))
            {
                yield return "solve";
                tileSelectables[cordList[x]].OnInteract();
                yield return new WaitForSeconds(0);
            }
            else
            {
                yield return "sendtochat Sorry, the rest of the command has been voided after " +(x).ToString()+ " presses due to a sudden change in the module.";
                yield break;
            }
        }
        yield return null;
    }
}
