using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public partial class LinkedWordle : MonoBehaviour {
    public enum KeyIDx
    {
        KeyA = 0,
        KeyB = 1,
        KeyC = 2,
        KeyD = 3,
        KeyE = 4,
        KeyF = 5,
        KeyG = 6,
        KeyH = 7,
        KeyI = 8,
        KeyJ = 9,
        KeyK = 10,
        KeyL = 11,
        KeyM = 12,
        KeyN = 13,
        KeyO = 14,
        KeyP = 15,
        KeyQ = 16,
        KeyR = 17,
        KeyS = 18,
        KeyT = 19,
        KeyU = 20,
        KeyV = 21,
        KeyW = 22,
        KeyX = 23,
        KeyY = 24,
        KeyZ = 25,
        KeySub = 26,
        KeyBack = 27,
        KeyUnk = -1
    }
    static readonly KeyIDx[] allUsedKeyIdxes = new[] {
        KeyIDx.KeyA, KeyIDx.KeyB, KeyIDx.KeyC, KeyIDx.KeyD, KeyIDx.KeyE, KeyIDx.KeyF, KeyIDx.KeyG,
        KeyIDx.KeyH, KeyIDx.KeyI, KeyIDx.KeyJ, KeyIDx.KeyK, KeyIDx.KeyL, KeyIDx.KeyM, KeyIDx.KeyN,
        KeyIDx.KeyO, KeyIDx.KeyP, KeyIDx.KeyQ, KeyIDx.KeyR, KeyIDx.KeyS, KeyIDx.KeyT, KeyIDx.KeyU,
        KeyIDx.KeyV, KeyIDx.KeyW, KeyIDx.KeyX, KeyIDx.KeyY, KeyIDx.KeyZ, KeyIDx.KeySub, KeyIDx.KeyBack, };
    private static Dictionary<KMBomb, LinkedWordleGlobalHandler> allGlobalHandlers = new Dictionary<KMBomb, LinkedWordleGlobalHandler>();
    LinkedWordleGlobalHandler globalHandler;
    List<string> allWordQueries;
    List<int[]> allResponses;
    string selectedCorrectWord;
    static int modIDCnt;
    int curIDxQueryFirstVisible = 0, modID, positionedIdxInput;
    public KMSelectable[] keyboardSelectables;
    public KMSelectable[] scrollSelectable;
    public KMSelectable modSelf;
    public QuerySet[] allQueryVisuals;
    public TextMesh overlayTextMesh, numMeshQueries, overlayInvalidWord;
    public MeshRenderer overlayRenderer, overlayTextMeshRenderer, overlayInvalidWordMesh;
    public MeshRenderer[] keyboardRenderer;
    public KMAudio mAudio;
    public KMBombModule module;
    public Color[] keyboardShowColors, colorblindKeyboardShowColors, colorblindQueryColors, queryColors;
    public BarExtendedScript _3PartBar;
    public KMColorblindMode colorblindMode;
    const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    bool modSolved, modFocused, disableImmediateSolve, allowInteractions = false, TwitchPlaysActive, colorblindDetected;
    void Awake()
    {
        allGlobalHandlers.Clear();
        try
        {
            colorblindDetected = colorblindMode.ColorblindModeActive;
        }
        catch
        {
            colorblindDetected = false;
        }
    }
    void HandleQuery(string word)
    {
        allWordQueries.Add(word);
        var response = new int[word.Length];
        var distinctLettersInCorrectWord = selectedCorrectWord.Distinct();
        foreach (char distinctLetter in distinctLettersInCorrectWord)
        {
            var idxesMatchCurLetterInCorrectWord = Enumerable.Range(0, selectedCorrectWord.Length).Where(a => selectedCorrectWord[a] == distinctLetter);
            var idxesMatchCurLetterInSelectedWord = Enumerable.Range(0, globalHandler.curWordQuery.Length).Where(a => word[a] == distinctLetter);
            var idxesMatchBoth = idxesMatchCurLetterInSelectedWord.Intersect(idxesMatchCurLetterInCorrectWord);
            foreach (int idxMatch in idxesMatchBoth)
                response[idxMatch] = 1;
            var idxesMatchExcluded = idxesMatchCurLetterInSelectedWord.Except(idxesMatchCurLetterInCorrectWord).Take(idxesMatchCurLetterInCorrectWord.Count() - idxesMatchBoth.Count());
            foreach (int curSet in idxesMatchExcluded)
                response[curSet] = 2;
        }
        allResponses.Add(response);
        QuickLog("Queried {0}. Response: {1}", word, response.Select(a => new[] { "Absent", "Correct", "Almost" }[a]).Join(", "));
        if (word == selectedCorrectWord)
        {
            modSolved = true;
            mAudio.PlaySoundAtTransform(globalHandler.wordlesAll.Count(a => !a.modSolved) == 0 ? "disarmed" : "215415__unfa__ping_Trimmed", transform);
            UpdateKeyboard();
            disableImmediateSolve |= TwitchPlaysActive;
            if (!disableImmediateSolve)
                module.HandlePass();
            if (allWordQueries.Count > 6)
            for (var x = 0; x < Mathf.Min(allWordQueries.Count - 6, allQueryVisuals.Length); x++)
            {
                var curIDxSeeResult = allWordQueries.Count - 6 + x;
                allQueryVisuals[x].UpdateStatus(allWordQueries[curIDxSeeResult], allResponses[curIDxSeeResult]);
            }
            allQueryVisuals[positionedIdxInput].UpdateStatus(word, response);
            _3PartBar.progressDelta = Mathf.Min(6f / allWordQueries.Count, 1f);
            _3PartBar.curProgress = allWordQueries.Count < 6 ? 0f : (float)(allWordQueries.Count - 6) / allWordQueries.Count;
            _3PartBar.UpdateProgress();
            numMeshQueries.text = "\u2713";
        }
        else
        {
            curIDxQueryFirstVisible = Mathf.Max(0, allWordQueries.Count - 5);
            UpdateQueryVisuals();
        }
        if (positionedIdxInput < 5)
            positionedIdxInput++;
    }
    void UpdateQueryVisuals()
    {
        for (var x = 0; x < Mathf.Min(allWordQueries.Count, allQueryVisuals.Length - 1); x++)
        {
            var curIDxSeeResult = curIDxQueryFirstVisible + x;
            allQueryVisuals[x].UpdateStatus(allWordQueries[curIDxSeeResult], allResponses[curIDxSeeResult]);
        }
        var queuesLeft = globalHandler.maxQueriesAllowed - allWordQueries.Count;
        numMeshQueries.text = queuesLeft > 99 ? "99+" : queuesLeft.ToString("00");
        allQueryVisuals.Last().UpdateStatus();
        _3PartBar.progressDelta = Mathf.Min(6f / (allWordQueries.Count + 1), 1f);
        _3PartBar.curProgress = allWordQueries.Count < 6 ? 0f : (float)(allWordQueries.Count - 5) / (allWordQueries.Count + 1);
        _3PartBar.UpdateProgress();
        UpdateKeyboard();
    }
    void UpdateKeyboard()
    {
        for (var x = 0; x < keyboardRenderer.Length; x++)
        {
            var curLetter = alphabet[x];
            var rangesofQueriesWithGivenLetter = Enumerable.Range(0, allWordQueries.Count).Where(a => allWordQueries[a].Contains(curLetter));
            
            if (rangesofQueriesWithGivenLetter.Any())
            {
                var markedIdx = 1;
                for (var y = 0; y < rangesofQueriesWithGivenLetter.Count(); y++)
                {
                    var curIdxScan = rangesofQueriesWithGivenLetter.ElementAt(y);
                    var queriedWord = allWordQueries.ElementAt(curIdxScan);
                    var idxesFiltered = Enumerable.Range(0, queriedWord.Length).Where(a => queriedWord[a] == curLetter);
                    if (idxesFiltered.Any(a => allResponses[curIdxScan][a] == 1))
                    {
                        markedIdx = 3;
                        break;
                    }
                    else if (idxesFiltered.Any(a => allResponses[curIdxScan][a] == 2))
                        markedIdx = 2;
                }
                keyboardRenderer[x].material.color = (colorblindDetected ? colorblindKeyboardShowColors : keyboardShowColors)[markedIdx];
            }
            else
                keyboardRenderer[x].material.color = (colorblindDetected ? colorblindKeyboardShowColors : keyboardShowColors)[0];
        }
    }
    void QuickLog(string stuff, params object[] args)
    {
        Debug.LogFormat("[Linked Wordle #{0}] {1}", modID, string.Format(stuff,args));
    }
    void Start()
    {
        modID = ++modIDCnt;
        allWordQueries = new List<string>();
        allResponses = new List<int[]>();
        StartCoroutine(DelayActivation());
        HandleColorblindModeToggle();
        foreach (QuerySet queryVis in allQueryVisuals)
        {
            queryVis.UpdateStatus("");
        }
        modSelf.OnFocus += delegate { modFocused = true; };
        modSelf.OnDefocus += delegate { modFocused = false; };
        for (var x = 0; x < keyboardSelectables.Length; x++)
        {
            int y = x;
            keyboardSelectables[x].OnInteract += delegate {
                HandleKeyPress(y);
                return false;
            };
        }
        for (var x = 0; x < scrollSelectable.Length; x++)
        {
            int y = x;
            scrollSelectable[x].OnInteract += delegate {
                scrollSelectable[y].AddInteractionPunch(0.1f);
                mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, scrollSelectable[y].transform);
                if (modSolved && disableImmediateSolve)
                {
                    module.HandlePass();
                }
                else if (!modSolved && allowInteractions)
                {
                    HandleArrowPress(2 * y - 1);
                }
                return false;
            };
        }
        _3PartBar.progressDelta = 1f;
        _3PartBar.curProgress = 0f;
        _3PartBar.UpdateProgress();
        overlayInvalidWordMesh.enabled = false;
        overlayRenderer.enabled = false;
        overlayTextMeshRenderer.enabled = false;
    }
    void HandleColorblindModeToggle()
    {
        for (var x = 0; x < allQueryVisuals.Length; x++)
        {
            allQueryVisuals[x].responseColors = colorblindDetected ? colorblindQueryColors : queryColors;
        }
        for (var x = 0; x < Mathf.Min(allWordQueries.Count, allQueryVisuals.Length); x++)
        {
            var curIDxSeeResult = curIDxQueryFirstVisible + x;
            if (curIDxSeeResult < allWordQueries.Count)
                allQueryVisuals[x].UpdateStatus(allWordQueries[curIDxSeeResult], allResponses[curIDxSeeResult]);
            else
                allQueryVisuals[x].UpdateStatus(globalHandler.curWordQuery);
        }
        var curQueryVisual = allQueryVisuals[positionedIdxInput];
        for (var x = 0; x < curQueryVisual.displayTexts.Length; x++)
            curQueryVisual.displayTexts[x].color = Color.white;
        UpdateKeyboard();
    }

    void HandleArrowPress(int delta)
    {
        curIDxQueryFirstVisible = Mathf.Min(Mathf.Max(curIDxQueryFirstVisible + delta, 0), Mathf.Max(allWordQueries.Count + 1 - allQueryVisuals.Length, 0));
        for (var x = 0; x < Mathf.Min(allWordQueries.Count, allQueryVisuals.Length); x++)
        {
            var curIDxSeeResult = curIDxQueryFirstVisible + x;
            if (curIDxSeeResult < allWordQueries.Count)
                allQueryVisuals[x].UpdateStatus(allWordQueries[curIDxSeeResult], allResponses[curIDxSeeResult]);
            else
                allQueryVisuals[x].UpdateStatus(globalHandler.curWordQuery);
        }
        var curQueryVisual = allQueryVisuals[positionedIdxInput];
        for (var x = 0; x < curQueryVisual.displayTexts.Length; x++)
            curQueryVisual.displayTexts[x].color = Color.white;
        _3PartBar.curProgress = allWordQueries.Count < 6 ? 0f : (float)curIDxQueryFirstVisible / (allWordQueries.Count + 1);
        _3PartBar.UpdateProgress();
    }
    void HandleKeyPress(int idx)
    {
        if (allUsedKeyIdxes.Length <= idx || idx < 0) return;
        keyboardSelectables[idx].AddInteractionPunch(0.1f);
        mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, keyboardSelectables[idx].transform);
        if (modSolved && disableImmediateSolve)
        {
            module.HandlePass();
            return;
        }
        else if (modSolved || !allowInteractions) return;
        globalHandler.HandleGlobalInput(allUsedKeyIdxes[idx]);
    }

    IEnumerator DelayActivation()
    {
        yield return null;
        KMBomb bombAlone = modSelf.GetComponentInParent<KMBomb>(); // Get the bomb that the module is attached on. Required for intergration due to modified value.
        //Required for Multiple Bombs stable interaction in case of different bomb seeds.
        if (!allGlobalHandlers.ContainsKey(bombAlone))
            allGlobalHandlers[bombAlone] = new LinkedWordleGlobalHandler();
        globalHandler = allGlobalHandlers[bombAlone];
        globalHandler.wordlesAll.Add(this);
        if (globalHandler.referenceBomb == null)
            globalHandler.referenceBomb = bombAlone;
        globalHandler.StartGlobalHandling();
    }
    void HandleInvalidWord()
    {
        var curQueryVisual = allQueryVisuals[positionedIdxInput];
        for (var x = 0; x < curQueryVisual.displayTexts.Length; x++)
            curQueryVisual.displayTexts[x].color = Color.red;
        overlayInvalidWordMesh.enabled = true;
    }
    void Update()
    {
        if (modFocused)
        {
            if (Input.GetKeyDown(KeyCode.A))
                keyboardSelectables[0].OnInteract();
            if (Input.GetKeyDown(KeyCode.B))
                keyboardSelectables[1].OnInteract();
            if (Input.GetKeyDown(KeyCode.C))
                keyboardSelectables[2].OnInteract();
            if (Input.GetKeyDown(KeyCode.D))
                keyboardSelectables[3].OnInteract();
            if (Input.GetKeyDown(KeyCode.E))
                keyboardSelectables[4].OnInteract();
            if (Input.GetKeyDown(KeyCode.F))
                keyboardSelectables[5].OnInteract();
            if (Input.GetKeyDown(KeyCode.G))
                keyboardSelectables[6].OnInteract();
            if (Input.GetKeyDown(KeyCode.H))
                keyboardSelectables[7].OnInteract();
            if (Input.GetKeyDown(KeyCode.I))
                keyboardSelectables[8].OnInteract();
            if (Input.GetKeyDown(KeyCode.J))
                keyboardSelectables[9].OnInteract();
            if (Input.GetKeyDown(KeyCode.K))
                keyboardSelectables[10].OnInteract();
            if (Input.GetKeyDown(KeyCode.L))
                keyboardSelectables[11].OnInteract();
            if (Input.GetKeyDown(KeyCode.M))
                keyboardSelectables[12].OnInteract();
            if (Input.GetKeyDown(KeyCode.N))
                keyboardSelectables[13].OnInteract();
            if (Input.GetKeyDown(KeyCode.O))
                keyboardSelectables[14].OnInteract();
            if (Input.GetKeyDown(KeyCode.P))
                keyboardSelectables[15].OnInteract();
            if (Input.GetKeyDown(KeyCode.Q))
                keyboardSelectables[16].OnInteract();
            if (Input.GetKeyDown(KeyCode.R))
                keyboardSelectables[17].OnInteract();
            if (Input.GetKeyDown(KeyCode.S))
                keyboardSelectables[18].OnInteract();
            if (Input.GetKeyDown(KeyCode.T))
                keyboardSelectables[19].OnInteract();
            if (Input.GetKeyDown(KeyCode.U))
                keyboardSelectables[20].OnInteract();
            if (Input.GetKeyDown(KeyCode.V))
                keyboardSelectables[21].OnInteract();
            if (Input.GetKeyDown(KeyCode.W))
                keyboardSelectables[22].OnInteract();
            if (Input.GetKeyDown(KeyCode.X))
                keyboardSelectables[23].OnInteract();
            if (Input.GetKeyDown(KeyCode.Y))
                keyboardSelectables[24].OnInteract();
            if (Input.GetKeyDown(KeyCode.Z))
                keyboardSelectables[25].OnInteract();
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                keyboardSelectables[26].OnInteract();
            if (Input.GetKeyDown(KeyCode.Backspace))
                keyboardSelectables[27].OnInteract();
        }
    }
    IEnumerator TwitchHandleForcedSolve()
    {
        if (allWordQueries.Count + globalHandler.wordlesAll.Count(a => !a.modSolved) >= globalHandler.maxQueriesAllowed)
            globalHandler.maxQueriesAllowed++;
        while (!allowInteractions) yield return true;
        while (globalHandler.curWordQuery != selectedCorrectWord && !modSolved)
        {
            var curLength = globalHandler.curWordQuery.Length;
            for (var x = 0; x < curLength; x++)
            {
                keyboardSelectables[27].OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
            for (var x = 0; x < selectedCorrectWord.Length; x++)
            {
                keyboardSelectables["ABCDEFGHIJKLMNOPQRSTUVWXYZ".IndexOf(selectedCorrectWord[x])].OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
            keyboardSelectables[26].OnInteract();
        }
        yield return null;
        keyboardSelectables.Union(scrollSelectable).PickRandom().OnInteract();
    }
    readonly string TwitchHelpMessage = "Guess a word with \"!{0} <word>\" or \"!{0} guess/g <word>\". Seek the previous queries with \"!{0} query/q #\" (E.G. \"!{0} q 1\" will seek for the first query.) When the module is finished, do \"!{0} s\" to claim the solve. Toggle colorblind mode with \"!{0} colorblind/colourblind\"";
    IEnumerator ProcessTwitchCommand(string cmd)
    {
        var remainingCmd = cmd;
        var matchQueryScan = Regex.Match(cmd, @"^q(uery)?\s\d+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        var matchQueryWord = Regex.Match(cmd, @"^g(uess)?\s+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        var matchQueryColorblind = Regex.Match(cmd, @"^colou?rblind$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (matchQueryColorblind.Success)
        {
            yield return null;
            colorblindDetected ^= true;
            HandleColorblindModeToggle();
            yield break;
        }
        else if (matchQueryScan.Success)
        {
            var matchedValue = matchQueryScan.Value.Split().Last();
            int selectedIDx;
            if (!int.TryParse(matchedValue, out selectedIDx))
            {
                yield return string.Format("sendtochaterror \"{0}\" gave a value that was too big to process. Abandoning command.", matchedValue);
                yield break;
            }
            selectedIDx--;
            if (curIDxQueryFirstVisible <= selectedIDx && curIDxQueryFirstVisible + 5 >= selectedIDx)
            {
                yield return string.Format("sendtochaterror The module is already showing queue #{0}", selectedIDx + 1);
                yield break;
            }
            if (selectedIDx < 0 || selectedIDx >= allWordQueries.Count)
            {
                yield return string.Format("sendtochaterror I can't show queue #{0}. It doesn't exist yet.", selectedIDx + 1);
                yield break;
            }
            yield return null;
            while (curIDxQueryFirstVisible > selectedIDx || curIDxQueryFirstVisible + 5 < selectedIDx)
            {
                yield return "trycancel";
                scrollSelectable[curIDxQueryFirstVisible < selectedIDx ? 1 : 0].OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
            yield break;
        }
        else if (modSolved && cmd.EqualsIgnoreCase("s"))
        {
            yield return null;
            yield return "solve";
            keyboardSelectables.Union(scrollSelectable).PickRandom().OnInteract();
            yield break;
        }
        else if (matchQueryWord.Success)
            remainingCmd = cmd.Replace(matchQueryWord.Value, "");
        if (modSolved)
        {
            yield return "sendtochat {0}, this module is already ready to solve. Use \"!{1} s\" to claim the solve.";
            yield break;
        }
        var allSelectables = new List<KMSelectable>();
        var cmdRemovedSpaces = remainingCmd.ToUpperInvariant().Where(a => !char.IsWhiteSpace(a));
        foreach (char oneChar in cmdRemovedSpaces)
        {
            var idxCur = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".IndexOf(oneChar);
            if (idxCur == -1)
                yield break;
            allSelectables.Add(keyboardSelectables[idxCur]);
        }
        if (allSelectables.Count != 5)
        {
            yield return "sendtochaterror You are trying to query a word that is not exactly 5 letters long.";
            yield break;
        }
        allSelectables.Add(keyboardSelectables[26]);
        if (globalHandler.curWordQuery.Any())
            allSelectables.InsertRange(0, Enumerable.Repeat(keyboardSelectables[27], globalHandler.curWordQuery.Length));
        foreach (KMSelectable sel in allSelectables)
        {
            yield return null;
            sel.OnInteract();
            yield return new WaitForSeconds(0.1f);
        }
        yield break;
    }
}
