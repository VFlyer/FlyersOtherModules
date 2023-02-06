using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class SimonStagesHandler : MonoBehaviour
{
    public KMAudio mAudio;
    public KMBombModule Module;

    public LightInformation[] lightDevices;
    public IndicatorInformation[] indicatorLights;
    public TextMesh indicatorText;
    public Color[] lightDeviceColorOptions;
    public Material[] lightBaseOptions;
    public string[] lightTextOptions;
    public string[] lightNameOptions;
    public AudioClip[] sfxOptions;
    private List<int> chosenIndicesDevices = new List<int>();
    private List<int> chosenIndicesIndicators = new List<int>();

    private int stagesCompleted = 0;

    List<int> selectedIndicatorIdx = new List<int>();
    List<List<int>> selectedFlashesIdxAllStages = new List<List<int>>();
    List<List<int>> expectedPressIdxesAllStages = new List<List<int>>();

    int curSequenceIdxInput = 0, curIdxInputInSequence = 0;
    private bool isAllCorrect = true, isCurSeqCorrect = true;

    //Logging
    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved = false;
    private bool moduleLocked = true;
    private bool reverse = false; // Used for the animation.
    private bool gameOn = false;
    private bool hasStruck = false, firstStrikeOnModule = false;
    private bool canPlaySound = false;
    private bool isHoldingButton = false;
    
    IEnumerator[] enumsHandlersLightInfo;
    IEnumerator flashHandler;
    float timeHeld = 0f;
    // Section for Souvenir Support. See https://github.com/Timwi/KtaneSouvenir/blob/master/Lib/ModulesS.cs#L677 for relevancy.
    public List<string> grabIndicatorColorsAll()
    {
        /*
        List<string> output = new List<string>();
        foreach (int oneIndCol in selectedIndicatorIdx)
        {
            output.Add(indicatorLights[oneIndCol].colorName);
        }
        */
        return selectedIndicatorIdx.Select(a => indicatorLights[a].colorName).ToList(); // Simpflied version of block above.
    }
    public List<string> grabSequenceColorsOneStage(int stageNum)
    {
        if (stageNum <= 0 || stageNum > selectedFlashesIdxAllStages.Count) return null; // Avoid an IndexOutOfBoundsException
        return selectedFlashesIdxAllStages[stageNum - 1].Select(a => lightDevices[a].colorName).ToList();
    }
    // End section for Souvenir Support
    void Awake()
    {
        moduleId = moduleIdCounter++;
        enumsHandlersLightInfo = new IEnumerator[lightDevices.Length];
        for (int i = 0; i < lightDevices.Length; i++)
        {
            LightInformation button = lightDevices[i];
            LightInformation pressedButton = button;
            enumsHandlersLightInfo[i] = PressFlash(pressedButton);
            var y = i;
            button.connectedButton.OnInteract += delegate () {
                ButtonPress(y);
                isHoldingButton = true;
                timeHeld = 0f;
                return false; };
            button.connectedButton.OnInteractEnded += delegate {
                isHoldingButton = false;
                if (timeHeld >= 1f && (curSequenceIdxInput > 0 || curIdxInputInSequence > 0))
                {
                    for (var x = 0; x < 10; x++)
                        mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.TypewriterKey, transform);
                    curIdxInputInSequence = 0;
                    curSequenceIdxInput = 0;
                    isRepeating = false;
                    isAllCorrect = true;
                    Debug.LogFormat("[Simon Stages #{0}] User cleared all inputs.", moduleId);
                }
            };
        }
    }
    bool HasInputted()
    {
        return curSequenceIdxInput > 0 || curIdxInputInSequence > 0;
    }

    void GenerateSequenceWithSolution(int currentLevel = 1)
    {
        int sequenceLength = Random.Range(3, 6);
        var newSequenceIdxFlashes = new List<int>();
        for (int i = 0; i < sequenceLength; i++)
        {
            int sequenceColourIdx = Random.Range(0, 10);
            newSequenceIdxFlashes.Add(sequenceColourIdx);
        }
        selectedFlashesIdxAllStages.Add(newSequenceIdxFlashes);
        var indicatorIdx = currentLevel <= 1 ? Random.Range(0, 10) : Enumerable.Range(0, 10).Where(a => a != selectedIndicatorIdx.Last()).PickRandom();
        selectedIndicatorIdx.Add(indicatorIdx);
        Debug.LogFormat("[Simon Stages #{0}]", moduleId, currentLevel);
        Debug.LogFormat("[Simon Stages #{0}] Stage #{1}: {2}.", moduleId, currentLevel, newSequenceIdxFlashes.Select(x => lightDevices[x].colorName).Join(", "));
        Debug.LogFormat("[Simon Stages #{0}] Indicator #{1}: {2}.", moduleId, currentLevel, indicatorLights[indicatorIdx].colorName);
        // Section to handle the solution, I.E. the expected inputs.
        var newExpectedPressIdxes = new List<int>();
        switch (indicatorLights[indicatorIdx].colorName)
        {
            case "red":
                {
                    for (int i = 0; i < newSequenceIdxFlashes.Count; i++)
                    {
                        newExpectedPressIdxes.Add(newSequenceIdxFlashes[i]);
                    }
                    break;
                }

            case "blue":
                {
                    for (int i = newSequenceIdxFlashes.Count - 1; i >= 0; i--)
                    {
                        newExpectedPressIdxes.Add(newSequenceIdxFlashes[i]);
                    }

                    break;
                }

            case "yellow":
                {
                    for (int i = 0; i < 2; i++)
                    {
                        newExpectedPressIdxes.Add(newSequenceIdxFlashes[i]);
                    }

                    break;
                }

            case "orange":
                {
                    for (int i = 1; i >= 0; i--)
                    {
                        newExpectedPressIdxes.Add(newSequenceIdxFlashes[i]);
                    }

                    break;
                }

            case "magenta":
                {
                    for (int i = newSequenceIdxFlashes.Count - 2; i < newSequenceIdxFlashes.Count; i++)
                    {
                        newExpectedPressIdxes.Add(newSequenceIdxFlashes[i]);
                    }

                    break;
                }

            case "green":
                {
                    for (int i = newSequenceIdxFlashes.Count - 1; i >= newSequenceIdxFlashes.Count - 2; i--)
                    {
                        newExpectedPressIdxes.Add(newSequenceIdxFlashes[i]);
                    }

                    break;
                }

            case "pink":
                {
                    for (int i = 0; i < newSequenceIdxFlashes.Count; i++)
                    {
                        newExpectedPressIdxes.Add((newSequenceIdxFlashes[i] + 5) % 10);
                    }

                    break;
                }

            case "lime":
                {
                    for (int i = newSequenceIdxFlashes.Count - 1; i >= 0; i--)
                    {
                        newExpectedPressIdxes.Add((newSequenceIdxFlashes[i] + 5) % 10);
                    }

                    break;
                }

            case "cyan":
                {
                    newExpectedPressIdxes.Add((newSequenceIdxFlashes.First() + 5) % 10);
                    newExpectedPressIdxes.Add((newSequenceIdxFlashes.Last() + 5) % 10);
                    break;
                }

            case "white":
                {
                    newExpectedPressIdxes.Add((newSequenceIdxFlashes[2] + 5) % 10);
                    newExpectedPressIdxes.Add((newSequenceIdxFlashes[1] + 5) % 10);
                }
                break;
        }
        expectedPressIdxesAllStages.Add(newExpectedPressIdxes);
        Debug.LogFormat("[Simon Stages #{0}] Solution #{1}: {2}.", moduleId, currentLevel, newExpectedPressIdxes.Select(x => lightDevices[x].colorName).Join(", "));
    }

    void Start()
    {
        indicatorText.text = "";
        moduleLocked = true;
        // Setup the shuffled index arrangements for the indicators and selectables;
        chosenIndicesDevices.AddRange(Enumerable.Range(0, 10));
        chosenIndicesIndicators.AddRange(Enumerable.Range(0, 10));
        chosenIndicesDevices.Shuffle();
        chosenIndicesIndicators.Shuffle();
        float scalar = transform.lossyScale.x;
        var soundIdxesPicked = Enumerable.Range(0, 10).ToArray().Shuffle();

        for (int i = 0; i < lightDevices.Length; i++)
        {
            LightInformation device = lightDevices[i];
            device.colorIndex = chosenIndicesDevices[i];
            device.soundIndex = soundIdxesPicked[i];

            
            device.ledGlow.range *= scalar;
            device.ledGlow.color = lightDeviceColorOptions[device.colorIndex];
            device.colorBase.material = lightBaseOptions[device.colorIndex];
            device.lightText.text = lightTextOptions[device.colorIndex];
            device.colorName = lightNameOptions[device.colorIndex];
            device.connectedSound = sfxOptions[device.soundIndex];
            device.ledGlow.enabled = false;
        }

        Debug.LogFormat("[Simon Stages #{0}] The arrangement of colors is: {1} // {2}", moduleId,
            string.Join(", ", lightDevices.Take(5).Select(ld => ld.colorName).ToArray()),
            string.Join(", ", lightDevices.Skip(5).Select(ld => ld.colorName).ToArray()));

        for (int i = 0; i < indicatorLights.Length; i++)
        {
            IndicatorInformation indic = indicatorLights[i];
            indic.colorIndex = chosenIndicesIndicators[i];

            indic.glow.range *= scalar;
            indic.glow.color = lightDeviceColorOptions[indic.colorIndex];
            indic.colorName = lightNameOptions[indic.colorIndex];
            indic.glow.enabled = false;
        }
        StartCoroutine(StartupFlash());
    }

    IEnumerator StartupFlash()
    {
        mAudio.PlaySoundAtTransform("scaryRiffREV", transform);
        int index = 0;
        int iterations = 0;
        int currentIdx = 0;
        while (iterations < 5)
        {
            lightDevices[9 - index].greyBase.enabled = false;
            lightDevices[9 - index].ledGlow.enabled = true;
            lightDevices[index].greyBase.enabled = false;
            lightDevices[index].ledGlow.enabled = true;
            indicatorLights[currentIdx].glow.enabled = true;
            indicatorText.text = lightTextOptions[indicatorLights[currentIdx].colorIndex];
            yield return new WaitForSeconds(0.05f);
            lightDevices[9 - index].greyBase.enabled = true;
            lightDevices[9 - index].ledGlow.enabled = false;
            lightDevices[index].greyBase.enabled = true;
            lightDevices[index].ledGlow.enabled = false;
            indicatorLights[currentIdx].glow.enabled = false;
            yield return new WaitForSeconds(0.025f);
            if (index < 10 && !reverse)
            {
                index++;
            }
            if (index == 5)
            {
                reverse = true;
                index = 4;
            }

            if (index >= 0 && reverse)
            {
                index--;
            }
            if (index < 0 && reverse)
            {
                reverse = false;
                iterations++;
                index = 1;
            }
            currentIdx = (currentIdx + 1) % 10;
        }
        indicatorText.text = "";
        int counter = 0;
        while (!gameOn)
        {
            for (int i = 0; i <= 9; i++)
            {
                lightDevices[i].greyBase.enabled = false;
                lightDevices[i].ledGlow.enabled = true;
                indicatorLights[i].glow.enabled = true;
            }
            yield return new WaitForSeconds(0.05f);
            for (int i = 0; i <= 9; i++)
            {
                lightDevices[i].greyBase.enabled = true;
                lightDevices[i].ledGlow.enabled = false;
                indicatorLights[i].glow.enabled = false;
            }
            yield return new WaitForSeconds(0.05f);
            counter++;
            if (counter >= 30)
            {
                moduleLocked = false;
                gameOn = true;
                for (var x = 1; x <= 5; x++)
                    GenerateSequenceWithSolution(x);
            }
        }
    }
    void StopFlashingSequence()
    {
        StopCoroutine(flashHandler);
        foreach (LightInformation lightDev in lightDevices)
        {
            lightDev.ledGlow.enabled = false;
            lightDev.greyBase.enabled = true;
        }
        indicatorText.text = "";
        foreach (IndicatorInformation indic in indicatorLights)
        {
            indic.glow.enabled = false;
        }
    }
    private void ButtonPress(int idxDevice)
    {
        if (moduleSolved || moduleLocked || !gameOn) return;
        var device = lightDevices[idxDevice];
        canPlaySound = true;
        //moduleLocked = true;
        if (curSequenceIdxInput == 0 && curIdxInputInSequence == 0)
            Debug.LogFormat("[Simon Stages #{0}] STAGE {1} RESPONSE:", moduleId, curSequenceIdxInput + 1);
        mAudio.PlaySoundAtTransform(device.connectedSound.name, transform);
        StopFlashingSequence();
        if (enumsHandlersLightInfo[idxDevice] != null)
            StopCoroutine(enumsHandlersLightInfo[idxDevice]);
        enumsHandlersLightInfo[idxDevice] = PressFlash(device);
        StartCoroutine(enumsHandlersLightInfo[idxDevice]);
        

        //StartCoroutine(PressFlash(device));

        // Check if the current press is correct;
        var isCurPressCorrect = idxDevice == expectedPressIdxesAllStages[curSequenceIdxInput][curIdxInputInSequence];
        isAllCorrect &= isCurPressCorrect;
        isCurSeqCorrect &= isCurPressCorrect;
        Debug.LogFormat("[Simon Stages #{0}] You pressed {1}. That is {2}correct.", moduleId, device.colorName, isCurPressCorrect ? "" : "in");
        curIdxInputInSequence++;

        if (curIdxInputInSequence >= expectedPressIdxesAllStages[curSequenceIdxInput].Count)
        {
            device.connectedButton.AddInteractionPunch();
            curIdxInputInSequence = 0;
            var result = isCurSeqCorrect ? "correct" : "incorrect";
            Debug.LogFormat("[Simon Stages #{0}] END OF STAGE {1}. The given sequence of inputs are {2}.", moduleId, curSequenceIdxInput + 1, result);
            curSequenceIdxInput++;
            if (curSequenceIdxInput <= stagesCompleted)
            {
                Debug.LogFormat("[Simon Stages #{0}] STAGE {1} RESPONSE:", moduleId, curSequenceIdxInput + 1);
            }
            isCurSeqCorrect = true;
        }
        else
        {
            device.connectedButton.AddInteractionPunch(0.25f);
        }

        if (curSequenceIdxInput > stagesCompleted)
        {
            moduleLocked = true;
            CheckEndGame();
        }
        
    }
    void CheckEndGame()
    {
        if (isAllCorrect)
        {
            stagesCompleted++;
            if (stagesCompleted >= 5) // If the defuser has successfully done 5 stages on the module.
            {
                Module.HandlePass();
                Debug.LogFormat("[Simon Stages #{0}] Inputs correct. Module disarmed.", moduleId);
                moduleSolved = true;
                StartCoroutine(SolveLights());
            }
            else
            {
                Debug.LogFormat("[Simon Stages #{0}] Inputs correct. However the module wants more. Advancing to stage {1}.", moduleId, stagesCompleted + 1);
                curIdxInputInSequence = 0;
                curSequenceIdxInput = 0;
            }
        }
        else
        {
            curIdxInputInSequence = 0;
            curSequenceIdxInput = 0;
            Debug.LogFormat("[Simon Stages #{0}] Strike! Your full sequence was incorrect.", moduleId);
            Module.HandleStrike();
            hasStruck = true; // Send a strike detection to the TP handler if the module is still requiring inputs.
            firstStrikeOnModule = true;
            isAllCorrect = true;
        }
        isRepeating = false;
    }
    int lastLevel = 0;
    bool isRepeating;
    IEnumerator RepeatSequence()
    {
        isRepeating = true;
        moduleLocked = true;
        yield return new WaitForSeconds(2f);
        foreach (LightInformation device in lightDevices)
        {
            device.ledGlow.enabled = false;
            device.greyBase.enabled = true;
        }
        lastLevel = stagesCompleted;
        while (lastLevel == stagesCompleted && !HasInputted())
        {
            //moduleLocked = true;
            for (int i = 0; i <= stagesCompleted; i++)
            {
                var sequences = selectedFlashesIdxAllStages[i];
                var selectedIndcIdx = selectedIndicatorIdx[i];

                indicatorText.text = lightTextOptions[indicatorLights[selectedIndcIdx].colorIndex];
                indicatorLights[selectedIndcIdx].glow.enabled = true;
                for (var p = 0; p < sequences.Count; p++)
                {
                    if (canPlaySound)
                        mAudio.PlaySoundAtTransform(lightDevices[sequences[p]].connectedSound.name, transform);

                    lightDevices[sequences[p]].ledGlow.enabled = true;
                    lightDevices[sequences[p]].greyBase.enabled = false;
                    yield return new WaitForSeconds(0.5f);
                    lightDevices[sequences[p]].ledGlow.enabled = false;
                    lightDevices[sequences[p]].greyBase.enabled = true;
                    if (HasInputted() || lastLevel != stagesCompleted)
                    {
                        yield break;
                    }
                    yield return new WaitForSeconds(0.25f);
                }
                foreach (IndicatorInformation indic in indicatorLights)
                {
                    indic.glow.enabled = false;
                }
            }
            foreach (LightInformation device in lightDevices)
            {
                device.ledGlow.enabled = false;
                device.greyBase.enabled = true;
            }
            indicatorText.text = "";
            foreach (IndicatorInformation indic in indicatorLights)
            {
                indic.glow.enabled = false;
            }
            moduleLocked = false;
            for (int x = 0; x < 20; x++)
            {
                yield return new WaitForSeconds(0.25f);
                if (curSequenceIdxInput > 0 || lastLevel != stagesCompleted)
                {
                    break;
                }
            }
        }
        isRepeating = false;
    }

    void Update()
    {
        if (!HasInputted() && gameOn && !moduleLocked && !isRepeating)
        {
            if (flashHandler != null)
                StopCoroutine(flashHandler);
            flashHandler = RepeatSequence();
            StartCoroutine(flashHandler);
        }
        if (isHoldingButton && timeHeld < 2f)
            timeHeld += Time.deltaTime;
    }
    
    IEnumerator PressFlash(LightInformation device)
    {
        device.greyBase.enabled = false;
        device.ledGlow.enabled = true;
        yield return new WaitForSeconds(0.4f);
        device.greyBase.enabled = true;
        device.ledGlow.enabled = false;
        moduleLocked = false;
    }

    IEnumerator SolveLights()
    {
        mAudio.PlaySoundAtTransform("solveRiff", transform);
        int solveCounter = 0;
        while (solveCounter < 2)
        {
            yield return new WaitForSeconds(1f);
            for (int i = 0; i <= 9; i++)
            {
                lightDevices[i].greyBase.enabled = false;
                lightDevices[i].ledGlow.enabled = true;
                indicatorLights[i].glow.enabled = true;
            }
            yield return new WaitForSeconds(1f);
            for (int i = 0; i <= 9; i++)
            {
                lightDevices[i].greyBase.enabled = true;
                lightDevices[i].ledGlow.enabled = false;
                indicatorLights[i].glow.enabled = false;
            }
            solveCounter++;
        }
        yield return new WaitForSeconds(1f);
        for (int i = 0; i <= 9; i++)
        {
            lightDevices[i].greyBase.enabled = false;
            lightDevices[i].ledGlow.enabled = true;
            indicatorLights[i].glow.enabled = true;
        }
    }
    
#pragma warning disable IDE0051 // Remove unused private members
    public readonly string TwitchHelpMessage = "To press a button: \"!{0} press RBYOMGPLCW\" Letters are dependent on the location on the module and \"press\" is optional."+
        "\nUse \"!{0} zoom\" to get the layout of where each button is. Press commands will be voided upon a new stage, solve, or strike."+
        "\nTo reset inputs on the module: \"!{0} resetinputs/clearinputs\" To mute the sound on this module: \"!{0} mute\" Certain phrases such as \"shuddup\",\"sush\" can be used to mute instead.";
#pragma warning restore IDE0051 // Remove unused private members
    private string[] idxStrings;
    /*
    IEnumerator HandleAutoSolve()
    {
        while (!gameOn)
            yield return true;
        curSequenceIdxInput = 0;
        curIdxInputInSequence = 0;
        while (!moduleSolved)
        {
            while (moduleLocked)
                yield return true;

            lightDevices[expectedPressIdxesAllStages[curSequenceIdxInput][curIdxInputInSequence]].connectedButton.OnInteract();
            yield return new WaitForSeconds(0.1f);
        }
        yield return true;
    }
    */
    IEnumerator TwitchHandleForcedSolve()
    {
        Debug.LogFormat("[Simon Stages #{0}] A force solve has been issued viva TP handler.", moduleId);
        while (!gameOn)
            yield return true;
        curSequenceIdxInput = 0;
        curIdxInputInSequence = 0;
        while (!moduleSolved)
        {
            while (moduleLocked)
                yield return true;

            lightDevices[expectedPressIdxesAllStages[curSequenceIdxInput][curIdxInputInSequence]].connectedButton.OnInteract();
            yield return new WaitForSeconds(0.1f);
        }
        yield return true;
    }
    int CountTotalPresses()
    {
        return expectedPressIdxesAllStages.Take(curSequenceIdxInput).Sum(a => a.Count) + curIdxInputInSequence;
    }
    IEnumerator ProcessTwitchCommand(string command)
    {
        if (!gameOn)
        {
            yield return "sendtochaterror The module is not ready yet. Please wait before sending this command.";
            yield break;
        }

        var intCommand = command.ToLower();
        if (Regex.IsMatch(intCommand, @"^(reset|clear)\s?inputs$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            if (curSequenceIdxInput + 1 >= stagesCompleted && curIdxInputInSequence + 1 >= expectedPressIdxesAllStages.Take(1 + stagesCompleted).Last().Count)
            {
                yield return "sendtochaterror Resetting inputs require at least 1 button press to be held that would NOT cause a strike. The next input WILL cause a strike upon doing so.";
                yield break;
            }
            yield return null;
            var randomLight = lightDevices.PickRandom();
            randomLight.connectedButton.OnInteract();
            yield return new WaitWhile(delegate { return timeHeld < 1.5f; });
            randomLight.connectedButton.OnInteractEnded();
            yield return "sendtochat Inputs cleared.";
            //Debug.LogFormat("[Simon Stages #{0}] Inputs resetted viva TP handler.", moduleId);
            yield break;
        }
        else if (Regex.IsMatch(intCommand, @"^(mute|shut up|shuddup|sush|shut the fuck up)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            canPlaySound = false;
            yield break;
        }
        intCommand = Regex.Replace(intCommand.Trim(), "^(press|hit|enter|push) ", "", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        // If the command contains the following start commands, trim it off.
        List<KMSelectable> presses = new List<KMSelectable>();
        
        if (idxStrings == null)
        {// Grab where each button is located based on the button presses, if the variable is not assigned yet.
            idxStrings = lightDevices.Select(device => device.colorName.Substring(0, 1)).ToArray();
            //print(idxStrings.Join()); // Show the array of buttons given in the given layout.
        }
        string[] segmentedCommand = intCommand.Split(' ');
        foreach (string part in segmentedCommand)
        {
            for (int x = 0; x < part.Length; x++)
            {
                string curInspect = part.Substring(x, 1);
                int idxCur = idxStrings.ToList().IndexOf(curInspect);
                if (idxCur < 0)
                {
                    yield return "sendtochaterror The character \"" + curInspect + "\" does not match any of the given labeled buttons.";
                    yield break;
                }
                presses.Add(lightDevices[idxCur].connectedButton);
            }
        }
        hasStruck = false; // Detect if the module has struck from inputting at the end of the full required sequence before all inputs are processed.
        int lastCurLv = stagesCompleted; // Required to detect if the module has to generate a new sequence after a correct set of inputs.
        int lastTotalPresses = CountTotalPresses(); // Required to detect if the input was correctly processed.
        for (int x = 0; x < presses.Count; x++)
        {
            yield return null;
            do
            {
                if (hasStruck || lastCurLv != stagesCompleted || moduleSolved) {// Check if the module has struck, entered another stage, or has been solved.
                    yield break;
                }
                yield return "trycancel Your command have been canceled after " + x + "/" + presses.Count + " presses.";
            }
            while (moduleLocked);
            presses[x].OnInteract();
            presses[x].OnInteractEnded();
            yield return new WaitForSeconds(0.1f);
            if (lastTotalPresses == CountTotalPresses()) // Check if the input correctly got processed. 
                x--;
            else
                lastTotalPresses = CountTotalPresses();
        }
        yield break;
    }
    
}
