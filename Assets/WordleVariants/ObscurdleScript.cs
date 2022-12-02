using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Text.RegularExpressions;

public class ObscurdleScript : MonoBehaviour {
    public KMBombModule modSelf;
    public KMAudio mAudio;
    public KMColorblindMode mColorblindMode;
    public QuerySetUI[] querySetUIs;
    public KMSelectable[] keyboardSelectable, arrowSelectable;
    public CanvasGroup invalidWordHandler, barHandler;
    public Text resultText;
    public Image resultImage;
    public KMSelectable modSelfSelectable;
    public BarExtendedScriptUI _3PartBar;
    public Sprite[] possibleSprites;
    public Color[] possibleColors;
    public string[] possibleColorNames;
    public Text[] huhText;
    protected enum PossibleResponse
    {
        Absent,
        Correct,
        Almost
    }
    readonly static PossibleResponse[] allPossibleResponses = { PossibleResponse.Absent, PossibleResponse.Correct, PossibleResponse.Almost };
    protected enum PossibleQuirks
    {
        None,
        Fibble,
        Xordle,
        Symble,
        FiveOhOh,
        Warmle,
        Peaks
    }
    readonly static int[] colorblindIdxColors = { 0, 3, 4 };
    readonly static PossibleQuirks[] allPossibleQuirks = {
        PossibleQuirks.Fibble,
        PossibleQuirks.Xordle,
        PossibleQuirks.FiveOhOh,
        PossibleQuirks.Symble,
        PossibleQuirks.Warmle,
        //PossibleQuirks.Peaks,
    };
    const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    protected class QueryResponse
    {
        public string wordQueried;
        public IEnumerable<PossibleResponse> displayResponse;
        public List<IEnumerable<PossibleResponse>> actualResponses;
    }
    readonly static Dictionary<PossibleQuirks, string> quirkDescriptions = new Dictionary<PossibleQuirks, string>()
    {
        { PossibleQuirks.Fibble, "Exactly one of the letters in each word you guess will lie about its result." },
        { PossibleQuirks.Xordle, "Two words, that have no letters in common, will need to be guessed correctly. Each word guessed will show the \"xored\" result from the two correct words. Successfully finding one word will make the module behave like the original counterpart until the other word is found." },
        { PossibleQuirks.Symble, "The response is flipped so that the word you guessed is treated as the answer and the answer is treated as a guess. In addition, other colors may show up that do not accurately represent Wordle." },
        { PossibleQuirks.FiveOhOh, "Wordle, but it is Mastermind. If you know how Mastermind works, this is less of a challenge for you." },
        { PossibleQuirks.Warmle, "The response is modified so that the distance from the alphabetic position of the letter queried to the letter of the correct word is compared. The response wraps around." },
        { PossibleQuirks.Peaks, "The response is modified so that the alphabetic position of the letter queried is compared higher or lower to the alphabetic position of the letter of the correct word. This response does NOT wrap around." },
    };
    PossibleQuirks selectedQuirk;
    List<QueryResponse> allResponses;
    List<string> correctWords;
    List<int> blacklistIdxesCorrect;
    bool moduleSolved, interactable, colorblindDetected, modFocused, quickChanged = false;
    string curWord = "";
    static int modIDCnt;
    int curIDxQueryFirstVisible = 0, moduleID, positionedIdxInput;
    Color[] selectedColors;
    IEnumerable<int> selectedSpriteIdxes;
    IEnumerator revealErrorHandler;
    void QuickLog(string stuff, params object[] args)
    {
        Debug.LogFormat("[Obscurdle #{0}] {1}", moduleID, string.Format(stuff, args));
    }
    void Awake()
    {
        try
        {
            colorblindDetected = mColorblindMode.ColorblindModeActive;
        }
        catch
        {
            colorblindDetected = false;
        }
    }
    // Use this for initialization
    void Start () {
        moduleID = ++modIDCnt;
        PrepModule();
        modSelfSelectable.OnFocus += delegate {
            modFocused = true;
        };
        modSelfSelectable.OnDefocus += delegate {
            modFocused = false;
        };
        for (var x = 0; x < keyboardSelectable.Length; x++)
        {
            var y = x;
            keyboardSelectable[x].OnInteract += delegate {
                mAudio.PlaySoundAtTransform("tick", keyboardSelectable[y].transform);
                keyboardSelectable[y].AddInteractionPunch(0.1f);
                if (interactable)
                {
                    TypeLetter(y);
                }
                return false;
            };
        }
        for (var x = 0; x < arrowSelectable.Length; x++)
        {
            var y = x;
            arrowSelectable[x].OnInteract += delegate {
                mAudio.PlaySoundAtTransform("tick", arrowSelectable[y].transform);
                arrowSelectable[y].AddInteractionPunch(0.1f);
                if (interactable)
                {
                    HandleArrowModify(2 * y - 1);
                }
                return false;
            };
        }
	}
    void HandleQuery()
    {
        var wordAlreadyQueried = false;
        for (var x = 0; x < allResponses.Count && !wordAlreadyQueried; x++)
            wordAlreadyQueried |= curWord == allResponses[x].wordQueried;
        if (wordAlreadyQueried)
        {
            QuickLog("\"{0}\" has already been guessed! You are NOT allowed to guess the same word more than once.", curWord);
            modSelf.HandleStrike();
            if (revealErrorHandler != null)
                StopCoroutine(revealErrorHandler);
            revealErrorHandler = HandleRevealError(string.Format("Already used \"{0}\"!", curWord), true);
            StartCoroutine(revealErrorHandler);
            return;
        }
        if (!(Data.GenerousWordList.Contains(curWord) || Data.ObscureWordList.Contains(curWord)))
        {
            if (revealErrorHandler != null)
                StopCoroutine(revealErrorHandler);
            revealErrorHandler = HandleRevealError(string.Format("\"{0}\" is not a word!", curWord));
            StartCoroutine(revealErrorHandler);
            return;
        }
        var nextResponse = new QueryResponse();
        QuickLog("Querying the word: {0}", curWord);
        nextResponse.wordQueried = curWord;
        var allAnswers = selectedQuirk == PossibleQuirks.Symble ? new List<string> { curWord } : correctWords;
        var guess = selectedQuirk == PossibleQuirks.Symble ? correctWords.Single() : curWord;
        
        var responsesCurBatch = new List<IEnumerable<PossibleResponse>>();
        for (int i = 0; i < allAnswers.Count; i++)
        {
            if (blacklistIdxesCorrect.Contains(i)) continue;
            string answer = allAnswers[i];
            var distinctLettersInAnswer = answer.Distinct();
            var curResponse = new PossibleResponse[5];
            foreach (char aLetter in distinctLettersInAnswer)
            {
                var idxesMatchCurLetterInAnswer = Enumerable.Range(0, answer.Length).Where(a => answer[a] == aLetter);
                var idxesMatchCurLetterInGuess = Enumerable.Range(0, guess.Length).Where(a => guess[a] == aLetter);
                var idxesMatchCurLetterBoth = idxesMatchCurLetterInGuess.Intersect(idxesMatchCurLetterInAnswer);
                foreach (int idxMatch in idxesMatchCurLetterBoth)
                    curResponse[idxMatch] = PossibleResponse.Correct;
                var idxesOutsideGuessCurLetter = idxesMatchCurLetterInGuess.Except(idxesMatchCurLetterInAnswer);
                foreach (int idxMatch in idxesOutsideGuessCurLetter.Take(idxesMatchCurLetterInAnswer.Count() - idxesMatchCurLetterBoth.Count()))
                    curResponse[idxMatch] = PossibleResponse.Almost;
            }
            QuickLog("When comparing the guess \"{0}\" with the answer \"{1}\", the response provided is {2}", guess, answer, curResponse.Join(", "));
            responsesCurBatch.Add(curResponse);
        }
        nextResponse.actualResponses = responsesCurBatch;
        var toDisplayResponse = responsesCurBatch.First().ToArray();
        switch (selectedQuirk)
        {
            case PossibleQuirks.Fibble:
                {
                    var idxManipulate = Random.Range(0, 5);
                    var fakeResponse = allPossibleResponses.Where(b => b != toDisplayResponse.ElementAt(idxManipulate)).PickRandom();
                    if (correctWords.First() == guess) goto default;
                    nextResponse.displayResponse = Enumerable.Range(0, 5)
                        .Select(a => idxManipulate == a ? fakeResponse : toDisplayResponse.ElementAt(a)).ToArray();
                }
                break;
            case PossibleQuirks.FiveOhOh:
                nextResponse.displayResponse = toDisplayResponse.OrderBy(a => a == 0 ? 3 : (int)a);
                break;
            case PossibleQuirks.Xordle:
                {
                    for (var x = 0; x < toDisplayResponse.Length; x++)
                    {
                        if (toDisplayResponse[x] == PossibleResponse.Absent)
                            toDisplayResponse[x] = responsesCurBatch.Last().ElementAt(x);
                    }
                }
                goto default;
            case PossibleQuirks.Warmle:
                {
                    for (var x = 0; x < toDisplayResponse.Length; x++)
                    {
                        var disABCCorToGuess = alphabet.IndexOf(correctWords.First()[x]) - alphabet.IndexOf(guess[x]);
                        var disABCGuessToCor = alphabet.IndexOf(guess[x]) - alphabet.IndexOf(correctWords.First()[x]);
                            toDisplayResponse[x] = correctWords.First()[x] == guess[x] ? PossibleResponse.Correct :
                            (disABCCorToGuess + 26) % 26 <= 3 || (disABCGuessToCor + 26) % 26 <= 3 ?
                            PossibleResponse.Almost : PossibleResponse.Absent;
                    }
                }
                goto default;
            case PossibleQuirks.Peaks:
                {
                    for (var x = 0; x < toDisplayResponse.Length; x++)
                    {
                        var disABCGuessToCor = alphabet.IndexOf(guess[x]) - alphabet.IndexOf(correctWords.First()[x]);
                            toDisplayResponse[x] = correctWords.First()[x] == guess[x] ? PossibleResponse.Correct :
                            disABCGuessToCor > 0 ?
                            PossibleResponse.Almost : PossibleResponse.Absent;
                    }
                }
                goto default;
            default:
                nextResponse.displayResponse = toDisplayResponse;
                break;
        }
        QuickLog("The response being displayed with the active quirk is {0}", nextResponse.displayResponse.Join(", "));
        allResponses.Add(nextResponse);

        var idxMatchAnswer = correctWords.IndexOf(curWord);
        if (idxMatchAnswer != -1 && !blacklistIdxesCorrect.Contains(idxMatchAnswer))
        {
            blacklistIdxesCorrect.Add(idxMatchAnswer);

            if (blacklistIdxesCorrect.Count >= allAnswers.Count)
            {
                QuickLog("You correctly guessed all of the words. It took you {0} total guesses to solve this module, with the active quirk.", allResponses.Count);
                moduleSolved = true;
                mAudio.PlaySoundAtTransform("disarmed", transform);
                modSelf.HandlePass();
            }
            else
            {
                QuickLog("You correctly guessed one of the words.");
                mAudio.PlaySoundAtTransform("215415__unfa__ping_Trimmed", transform);
            }
        }
        curIDxQueryFirstVisible = Mathf.Max(0, allResponses.Count - 5);
        UpdateQueryVisuals();
        if (positionedIdxInput < 5)
            positionedIdxInput++;
        if (allResponses.Count == 6)
        {
            StartCoroutine(HandleRevealBar());
        }
        curWord = "";
    }
    void HandleArrowModify(int delta)
    {
        curIDxQueryFirstVisible = Mathf.Min(Mathf.Max(curIDxQueryFirstVisible + delta, 0), Mathf.Max(allResponses.Count + 1 - querySetUIs.Length, 0));
        for (var x = 0; x < Mathf.Min(allResponses.Count, querySetUIs.Length); x++)
        {
            var curIDxSeeResult = curIDxQueryFirstVisible + x;
            if (curIDxSeeResult < allResponses.Count)
            {
                var curResponse = allResponses[curIDxSeeResult];
                querySetUIs[x].UpdateStatus(curResponse.wordQueried.ToCharArray(), curResponse.displayResponse.Select(a => (int)a).ToArray());
                var isNotCorrectWord = curResponse.displayResponse.All(a => a == PossibleResponse.Correct) && !curResponse.actualResponses.Any(a => a.All(b => b == PossibleResponse.Correct));
                huhText[x].text = "huh?";
                huhText[x].enabled = isNotCorrectWord;
                var curStatusRenderers = querySetUIs[x].statusRenderers;
                for (var a = 0; a < curStatusRenderers.Length; a++)
                    curStatusRenderers[a].sprite = colorblindDetected && selectedQuirk == PossibleQuirks.Symble ? possibleSprites[(int)curResponse.displayResponse.ElementAt(a)] : possibleSprites[0];
            }
            else
            {
                querySetUIs[x].UpdateStatus(curWord);
                huhText[x].text = "";
                huhText[x].enabled = false;
            }
        }
        _3PartBar.curProgress = allResponses.Count < 6 ? 0f : (float)curIDxQueryFirstVisible / (allResponses.Count + 1);
        _3PartBar.UpdateProgress();
    }
    void TypeLetter(int idx)
    {
        if (!interactable || moduleSolved) return;
        var lastEnum = revealErrorHandler;
        switch (idx)
        {
            case 26:
                if (curWord.Length > 0)
                    curWord = curWord.Substring(0, curWord.Length - 1);
                break;
            case 27:
            case 28:
                if (curWord.Length < 5)
                {
                    if (revealErrorHandler != null)
                        StopCoroutine(revealErrorHandler);
                    revealErrorHandler = HandleRevealError("Type more letters!");
                    StartCoroutine(revealErrorHandler);
                    break;
                }
                HandleQuery();
                break;
            default:
                var idxLetter = alphabet.ElementAtOrDefault(idx);
                if (idxLetter != 0 && curWord.Length < 5)
                    curWord += idxLetter;
                break;

        }
        if (lastEnum != null && lastEnum.MoveNext())
            quickChanged = true;
        UpdateForceQueryVisuals();
    }
    IEnumerator HandleRevealBar()
    {
        for (float t = 0; t <= 1f; t += Time.deltaTime * 4f)
        {
            barHandler.alpha = t;
            yield return null;
        }
        barHandler.alpha = 1f;
    }

    IEnumerator HandleRevealError(string textToDisplay, bool isMajor = false, float speed = 2f)
    {
        resultText.text = textToDisplay;
        resultImage.color = isMajor ? Color.red : Color.black;
        for (float t = 0; t <= 1f; t += Time.deltaTime * speed)
        {
            invalidWordHandler.alpha = t;
            yield return null;
        }
        invalidWordHandler.alpha = 1f;

        for (float t = 0; t < 3f && !quickChanged; t += Time.deltaTime)
            yield return null;
        for (float t = 0; t < 1f; t += Time.deltaTime * speed)
        {
            invalidWordHandler.alpha = 1f - t;
            yield return null;
        }
        invalidWordHandler.alpha = 0f;
        quickChanged = false;
    }
    void HandleColorblindModeToggle()
    {
        if (selectedQuirk != PossibleQuirks.Symble)
        {
            selectedColors = (colorblindDetected ? colorblindIdxColors.Select(a => possibleColors[a]) : possibleColors.Take(3)).ToArray();
            for (var u = 0; u < querySetUIs.Length; u++)
            {
                querySetUIs[u].responseColors = selectedColors;
            }
        }
        UpdateQueryVisuals();
    }
    void UpdateQueryVisuals()
    {
        for (var x = 0; x < Mathf.Min(allResponses.Count, querySetUIs.Length - 1); x++)
        {
            var curIDxSeeResult = curIDxQueryFirstVisible + x;
            var curResponse = allResponses[curIDxSeeResult];
            querySetUIs[x].UpdateStatus(curResponse.wordQueried.ToCharArray(), curResponse.displayResponse.Select(a => (int)a).ToArray());
            var isNotCorrectWord = curResponse.displayResponse.All(a => a == PossibleResponse.Correct) && !curResponse.actualResponses.Any(a => a.All(b => b == PossibleResponse.Correct));
            huhText[x].text = "huh?";
            huhText[x].enabled = isNotCorrectWord;
            var curStatusRenderers = querySetUIs[x].statusRenderers;
            for (var a = 0; a < curStatusRenderers.Length; a++)
                curStatusRenderers[a].sprite = colorblindDetected && selectedQuirk == PossibleQuirks.Symble ? possibleSprites[(int)curResponse.displayResponse.ElementAt(a)] : possibleSprites[0];
        }
        _3PartBar.progressDelta = Mathf.Min(6f / (allResponses.Count + 1), 1f);
        _3PartBar.curProgress = allResponses.Count < 6 ? 0f : (float)(allResponses.Count - 5) / (allResponses.Count + 1);
        _3PartBar.UpdateProgress();
    }
    void UpdateForceQueryVisuals()
    {
        if (allResponses.Count > 5)
        {
            curIDxQueryFirstVisible = allResponses.Count - 5;
            for (var u = 0; u < querySetUIs.Length - 1; u++)
            {
                var curResponse = allResponses[u + curIDxQueryFirstVisible];
                querySetUIs[u].UpdateStatus(curResponse.wordQueried.ToCharArray(), curResponse.displayResponse.Select(a => (int)a).ToArray());
                var isNotCorrectWord = curResponse.displayResponse.All(a => a == PossibleResponse.Correct) && !curResponse.actualResponses.Any(a => a.All(b => b == PossibleResponse.Correct));
                huhText[u].text = "huh?";
                huhText[u].enabled = isNotCorrectWord;
                var curStatusRenderers = querySetUIs[u].statusRenderers;
                for (var a = 0; a < curStatusRenderers.Length; a++)
                    curStatusRenderers[a].sprite = colorblindDetected && selectedQuirk == PossibleQuirks.Symble ? possibleSprites[(int)curResponse.displayResponse.ElementAt(a)] : possibleSprites[0];
            }
        }
        querySetUIs[positionedIdxInput].UpdateStatus(curWord);
    }
    void PrepModule()
    {
        allResponses = new List<QueryResponse>();
        correctWords = new List<string>();
        blacklistIdxesCorrect = new List<int>();
        selectedQuirk = allPossibleQuirks.PickRandom();
        QuickLog("Selected Quirk: {0}", selectedQuirk.ToString());
        QuickLog("Description: {0}", quirkDescriptions.ContainsKey(selectedQuirk) ? quirkDescriptions[selectedQuirk] : "None or unknown gimmick active.");
        correctWords.Add(Data.GenerousWordList.PickRandom());
        if (selectedQuirk == PossibleQuirks.Xordle)
        {
            var nonOverlappingWords = Data.GenerousWordList.Where(aWord => !aWord.Any(chr => correctWords.Any(a => a.Contains(chr))));
            //QuickLog("{0}", nonOverlappingWords.Count());
            correctWords.Add(nonOverlappingWords.PickRandom());
        }
        QuickLog("Selected correct word(s): {0}", correctWords.Join(", "));
        selectedColors = (colorblindDetected ? colorblindIdxColors.Select(a => possibleColors[a]) : possibleColors.Take(3)).ToArray();
        selectedSpriteIdxes = Enumerable.Repeat(0, 3);
        if (selectedQuirk == PossibleQuirks.Symble)
        {
            var newIdxesToSelect = Enumerable.Range(0, possibleColors.Length).ToArray().Shuffle();
            while (Enumerable.Range(0, 3).All(a => newIdxesToSelect.Take(3).Contains(a)))
                newIdxesToSelect.Shuffle();
            var selectedIdxesSymble = newIdxesToSelect.Take(3);
            selectedColors = selectedIdxesSymble.Select(a => possibleColors[a]).ToArray();
            QuickLog("Correct letters in their correct position will be represented by this color: {0}", possibleColorNames[selectedIdxesSymble.ElementAt(1)]);
            QuickLog("Correct letters not in their correct position will be represented by this color: {0}", possibleColorNames[selectedIdxesSymble.ElementAt(2)]);
            QuickLog("Letters not present in the correct word will be represented by this color: {0}", possibleColorNames[selectedIdxesSymble.ElementAt(0)]);
            selectedSpriteIdxes = Enumerable.Range(0, possibleSprites.Length).ToArray().Shuffle().Take(3);
        }
        for (var x = 0; x < querySetUIs.Length; x++)
        {
            querySetUIs[x].UpdateStatus("");
            querySetUIs[x].responseColors = selectedColors;
        }


        interactable = true;
        
    }
	// Update is called once per frame
	void Update () {
        if (modFocused)
        {
            var activeInputs = new[] {
            Input.GetKeyDown(KeyCode.A),
            Input.GetKeyDown(KeyCode.B),
            Input.GetKeyDown(KeyCode.C),
            Input.GetKeyDown(KeyCode.D),
            Input.GetKeyDown(KeyCode.E),
            Input.GetKeyDown(KeyCode.F),
            Input.GetKeyDown(KeyCode.G),
            Input.GetKeyDown(KeyCode.H),
            Input.GetKeyDown(KeyCode.I),
            Input.GetKeyDown(KeyCode.J),
            Input.GetKeyDown(KeyCode.K),
            Input.GetKeyDown(KeyCode.L),
            Input.GetKeyDown(KeyCode.M),
            Input.GetKeyDown(KeyCode.N),
            Input.GetKeyDown(KeyCode.O),
            Input.GetKeyDown(KeyCode.P),
            Input.GetKeyDown(KeyCode.Q),
            Input.GetKeyDown(KeyCode.R),
            Input.GetKeyDown(KeyCode.S),
            Input.GetKeyDown(KeyCode.T),
            Input.GetKeyDown(KeyCode.U),
            Input.GetKeyDown(KeyCode.V),
            Input.GetKeyDown(KeyCode.W),
            Input.GetKeyDown(KeyCode.X),
            Input.GetKeyDown(KeyCode.Y),
            Input.GetKeyDown(KeyCode.Z),
            Input.GetKeyDown(KeyCode.Backspace),
            Input.GetKeyDown(KeyCode.KeypadEnter),
            Input.GetKeyDown(KeyCode.Return),
            Input.GetKeyDown(KeyCode.UpArrow),
            Input.GetKeyDown(KeyCode.DownArrow),
            };
            var idxesPressed = Enumerable.Range(0, activeInputs.Length).Where(a => activeInputs[a]);
            if (idxesPressed.Any())
            {
                foreach (int firstPressed in idxesPressed)
                    switch (firstPressed)
                    {
                        case 29:
                            HandleArrowModify(-1);
                            break;
                        case 30:
                            HandleArrowModify(1);
                            break;
                        default:
                            TypeLetter(firstPressed);
                            break;
                    }
            }
        }
	}
    readonly string TwitchHelpMessage = "Guess a word with \"!{0} <word>\" or \"!{0} guess/g <word>\". Seek the previous queries with \"!{0} query/q #\" (E.G. \"!{0} q 1\" will seek for the first query.) Toggle colorblind mode with \"!{0} colorblind/colourblind\"";
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
            if (selectedIDx < 0 || selectedIDx >= allResponses.Count)
            {
                yield return string.Format("sendtochaterror I can't show queue #{0}. It doesn't exist yet.", selectedIDx + 1);
                yield break;
            }
            yield return null;
            while (curIDxQueryFirstVisible > selectedIDx || curIDxQueryFirstVisible + 5 < selectedIDx)
            {
                arrowSelectable[curIDxQueryFirstVisible < selectedIDx ? 1 : 0].OnInteract();
                yield return "trywaitcancel 0.1";
            }
            yield break;
        }
        else if (matchQueryWord.Success)
            remainingCmd = cmd.Replace(matchQueryWord.Value, "");
        var allSelectables = new List<KMSelectable>();
        var cmdRemovedSpaces = remainingCmd.ToUpperInvariant().Where(a => !char.IsWhiteSpace(a));
        foreach (char oneChar in cmdRemovedSpaces)
        {
            var idxCur = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".IndexOf(oneChar);
            if (idxCur == -1)
                yield break;
            allSelectables.Add(keyboardSelectable[idxCur]);
        }
        if (allSelectables.Count != 5)
        {
            yield return "sendtochaterror You are trying to query a word that is not exactly 5 letters long.";
            yield break;
        }
        allSelectables.Add(keyboardSelectable[27]);
        if (curWord.Any())
            allSelectables.InsertRange(0, Enumerable.Repeat(keyboardSelectable[26], curWord.Length));
        foreach (KMSelectable sel in allSelectables)
        {
            yield return null;
            sel.OnInteract();
            yield return new WaitForSeconds(0.1f);
        }
        yield break;
    }
    IEnumerator TwitchHandleForcedSolve()
    {
        while (!interactable) yield return true;
        while (blacklistIdxesCorrect.Count < correctWords.Count)
        {
            var remainingUnsolvedWords = Enumerable.Range(0, correctWords.Count).Except(blacklistIdxesCorrect).Select(a => correctWords[a]);
            foreach (var oneWord in remainingUnsolvedWords)
            {
                var curLength = curWord.Length;
                for (var x = 0; x < curLength; x++)
                {
                    keyboardSelectable[26].OnInteract();
                    yield return new WaitForSeconds(0.1f);
                }
                for (var x = 0; x < oneWord.Length; x++)
                {
                    keyboardSelectable[alphabet.IndexOf(oneWord[x])].OnInteract();
                    yield return new WaitForSeconds(0.1f);
                }
                keyboardSelectable[27].OnInteract();
            }
        }
    }
}
