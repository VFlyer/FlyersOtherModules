using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;
using System.Reflection;
using KModkit;

public class ForgetInfinity : MonoBehaviour {
	public KMSelectable[] ButtonDigits = new KMSelectable[10];
	public KMSelectable BackSpaceButton;
	public KMBombModule ModSelf;
    public KMAudio AudioHandler;
	public KMBombInfo Info;
	public TextMesh ScreenStages;
    public TextMesh ScreenStatus;

    private List<string> ignoredModuleNames;
    private List<string> organIgnoredModNames;



    private bool solved = false, inFinale = false, hasStarted = false, isRecapturing = false, autosolvable = false, delayed = false, hasStruck = false,interactable = true;

    public List<int[]> stages = new List<int[]>();// The displayed values for each of the given stages
    public List<int[]> solution = new List<int[]>();// The solution for each of the given stages
    public List<int> possibleStages = new List<int>();

    public string input = "";

    private int stagestoGenerate = 0;
    private int currentStage = 0;

    private static int modID = 1;
    private int curModID, earliestSolveCountOrgan = -1;

    private float PPAScaling;
    private ForgetInfintySettings FIConfig = new ForgetInfintySettings();
    public KMModSettings modSettings;
	// Use this for initialization
	void Awake() {
		if (ignoredModuleNames == null)
			ignoredModuleNames = GetComponent<KMBossModule>().GetIgnoredModules("Forget Infinity", new string[]{
                "14",
                "Bamboozling Time Keeper",
                "Brainf---",
                "Cookie Jars",
                "Cruel Purgatory",
                "Divided Squares",
                "Forget Enigma",
                "Forget Everything",
                "Forget Infinity",
                "Forget It Not",
                "Forget Me Later",
                "Forget Me Not",
                "Forget Perspective",
                "Forget The Colors",
                "Forget Them All",
                "Forget This",
                "Forget Us Not",
                "Hogwarts",
                "Organization",
                "Purgatory",
                "Simon Forgets",
                "Simon's Stages",
                "Souvenir",
                "Tallordered Keys",
                "The Time Keeper",
                "Timing is Everything",
                "The Troll",
                "Turn The Key",
                "Übermodule",
                "Ültimate Custom Night",
                "The Very Annoying Button"
            }).ToList();
        /*
        if (organIgnoredModNames == null)
            organIgnoredModNames = GetComponent<KMBossModule>().GetIgnoredModules("Organization").ToList();
        */
        //See Forget It Not's ignore list for reasons
        curModID = modID++;
        try
        {
            ModConfig<ForgetInfintySettings> modConfig = new ModConfig<ForgetInfintySettings>("ForgetInfintySettings");
            // Read from settings file, or create one if one doesn't exist
            FIConfig = modConfig.Settings;
            // Update settings file incase of error during read
            modConfig.Settings = FIConfig;
            modSettings.RefreshSettings();

            PPAScaling = FIConfig.PPAScaleFactor;
        }
        catch
        {
            Debug.LogErrorFormat("[Forget Infinty #{0}]: The settings for Forget Infinty do not work as intended! The module will use default settings instead.", curModID);
            PPAScaling = 0.5f;
        }
    }
    void Start()
    {

        ModSelf.OnActivate += delegate
        {
            StartCoroutine(DelayActivation());
        };
        BackSpaceButton.OnInteract += delegate
        {
            AudioHandler.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
            BackSpaceButton.AddInteractionPunch();
            StartCoroutine(HandleButtonAnim(BackSpaceButton.gameObject));
            if (!interactable || solved) return false;
            if (inFinale)
            {
                if (input.Length <= 0)
                {
                    isRecapturing = !isRecapturing;
                    ScreenStages.color = isRecapturing ? Color.green : Color.white;
                }
                else if (!isRecapturing)
                {
                    input = input.Substring(0, input.Length - 1);
                }
                else
                {
                    int stageToGrab = int.Parse(input);
                    Debug.LogFormat("[Forget Infinity #{0}]: Attempting to recapture stage {1} at a cost of a strike", curModID, stageToGrab);
                    if (!hasStruck)
                        ModSelf.HandleStrike();
                    else Debug.LogFormat("[Forget Infinity #{0}]: The module has a free capture; consuming it.", curModID);
                    hasStruck = false;
                    interactable = false;
                    isRecapturing = false;
                    StartCoroutine(RegrabStage(stageToGrab));
                    input = "";
                    ScreenStages.color =  Color.white;
                }
            }
            else if (autosolvable)
            {
                solved = true;
                StartCoroutine(AnimateSolveAnim());
            }
            else
            {
                ModSelf.HandleStrike();
                Debug.LogFormat("[Forget Infinity #{0}]: Defuser pressed a button before module is ready.", curModID);
            }
            return false;
        };
        for (int x = 0; x < ButtonDigits.Length; x++)
        {
            int y = x;
            ButtonDigits[x].OnInteract += delegate
            {
                AudioHandler.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
                ButtonDigits[y].AddInteractionPunch();
                StartCoroutine(HandleButtonAnim(ButtonDigits[y].gameObject));
                if (!interactable || solved) return false;
                if (inFinale)
                {
                    if (input.Length < 5)
                        input += y.ToString();
                    if (input.Length >= 5 && !isRecapturing)
                    {
                        interactable = false;
                        StartCoroutine(ProcessSubmittion());
                    }
                }
                else if (autosolvable)
                {
                    
                    solved = true;
                    StartCoroutine(AnimateSolveAnim());
                }
                else
                {
                    ModSelf.HandleStrike();
                    Debug.LogFormat("[Forget Infinity #{0}]: Defuser pressed a button before module is ready.", curModID);
                }
                return false;
            };
        }
    }
    void GenerateStages(int numStagesRequested)
    {
        List<string> allModNames = Info.GetModuleNames().ToList();

        Debug.LogFormat("[Forget Infinity #{0}]: Total stages requested: {1}", curModID, numStagesRequested);
        Debug.LogFormat("[Forget Infinity #{0}]: All stages: ", curModID, numStagesRequested);

        for (int x = 0; x < numStagesRequested; x++)
        {
            int[] output = new int[5];
            do
                for (int a = 0; a < output.Length; a++)
                {
                    output[a] = UnityEngine.Random.Range(0, 10);
                }
            while (stages.Count > 0 && stages.Contains(output));
            stages.Add(output);
        }
        for (int x = 0; x < stages.Count; x++)
        {
            bool hasSwapped = false;
            int[] finalStageNumbers = new int[5];
            stages[x].CopyTo(finalStageNumbers, 0);

            int lastDigitInSerial = Info.GetSerialNumberNumbers().Any() ? Info.GetSerialNumberNumbers().Last() : 0;
            int firstDigitInSerial = Info.GetSerialNumberNumbers().Any() ? Info.GetSerialNumberNumbers().First() : 0;
            int smallestDigitInSerial = Info.GetSerialNumberNumbers().Any() ? Info.GetSerialNumberNumbers().Min() : 0;
            int largestDigitInSerial = Info.GetSerialNumberNumbers().Any() ? Info.GetSerialNumberNumbers().Max() : 0;
            // Begin Solution Calculations
            // Culumulative Slot Calculations
            if (Info.IsPortPresent(Port.StereoRCA))
            {
                finalStageNumbers = finalStageNumbers.Reverse().ToArray();
                hasSwapped = true;
            }

            int batterycount = Info.GetBatteryCount();
            for (int idx = 0; idx < finalStageNumbers.Length; idx++)
                finalStageNumbers[idx] += batterycount;

            List<char> LettersInSerial = Info.GetSerialNumberLetters().ToList();
            if (LettersInSerial.Contains('F') || LettersInSerial.Contains('I'))
                for (int idx = 0; idx < finalStageNumbers.Length; idx++)
                    finalStageNumbers[idx] -= LettersInSerial.Count;
            // Individual Slots
            // Slot 1
            if (allModNames.Contains("Tetris"))
                finalStageNumbers[0] = stages[x][0] + 7;
            else if (finalStageNumbers[0] >= 10 && finalStageNumbers[0] % 2 == 0)
                finalStageNumbers[0] /= 2;
            else if (finalStageNumbers[0] < 5)
                finalStageNumbers[0] += lastDigitInSerial;
            else
                finalStageNumbers[0] += 1;
            // Slot 2
            if (Info.CountDuplicatePorts() > 0)
                finalStageNumbers[1] += Info.CountDuplicatePorts();
            else if (Info.GetPortCount() == 0)
                finalStageNumbers[1] += stages[x][0] + stages[x][2];
            else
                finalStageNumbers[1] += Info.GetPortCount();
            // Slot 3
            if (!hasSwapped)
            {
                if (finalStageNumbers[2] >= 7)
                {
                    int currentValue = stages[x][2];
                    int finalValueSlot3 = 0;
                    while (currentValue > 0)
                    {
                        finalValueSlot3 += currentValue % 2;
                        currentValue /= 2;
                    }
                    finalStageNumbers[2] = finalValueSlot3;
                }
                else if (finalStageNumbers[2] < 3)
                    finalStageNumbers[2] = Math.Abs(finalStageNumbers[2]);
                else
                    finalStageNumbers[2] = stages[x][2] + smallestDigitInSerial;
            }
            // Slot 4
            if (finalStageNumbers[3] < 0)
                finalStageNumbers[3] += largestDigitInSerial;
            else if (stagestoGenerate > 5)
                finalStageNumbers[3] = 18 - finalStageNumbers[3];
            // Slot 5
            int[,] slotTable5th = new int[,] {
                            { 0, 1, 2, 3, 4 },
                            { 5, 6, 7, 8, 9 },
                            { stages[x][4], 1 + stages[x][4], 9 - stages[x][4], stages[x][4] - 1, stages[x][4] + 5 },
                            { 9, 8, 5, 6, 7 },
                            { 4, 3, 0, 1, 2 }
                        };
            int rowCellToGrab = finalStageNumbers[4] - (Mathf.FloorToInt(finalStageNumbers[4] / 5.0f) * 5);
            finalStageNumbers[4] = slotTable5th[rowCellToGrab, firstDigitInSerial / 2];
            // Within 0-9
            while (!finalStageNumbers.ToList().TrueForAll(a => a >= 0 && a <= 9))
            {
                for (int idx = 0; idx < finalStageNumbers.Length; idx++)
                    if (finalStageNumbers[idx] < 0)
                        finalStageNumbers[idx] += 10;
                    else if (finalStageNumbers[idx] > 9)
                        finalStageNumbers[idx] -= 10;
            }
            solution.Add(finalStageNumbers);
            // End Solution Calculations
            Debug.LogFormat("[Forget Infinity #{0}]: Stage {1}: Display = {2}, Answer = {3}", curModID, (x + 1).ToString("00"), stages[x].Join(""), solution[x].Join(""));
        }
        while (possibleStages.Count < Math.Min(stagestoGenerate, 3))// Memoryless Randomizer Starts Here
        {
            int randomStage = UnityEngine.Random.Range(0, stagestoGenerate);
            if (!possibleStages.Contains(randomStage))
                possibleStages.Add(randomStage);
        }
        Debug.LogFormat("[Forget Infinity #{0}]: Stages required to solve: {1}", curModID, FormatIntListWithCommas(possibleStages.ToArray()));
    }

    IEnumerator DelayActivation()
    {
        yield return null;
        try
        {
            // Handle Ignore List Grabbing
            var fakeOrgansDetected = 0;

            var bombSelf = ModSelf.gameObject.GetComponentInParent<KMBomb>(); // Find the KMBomb that is attached to this GameObject.
            if (bombSelf != null)
            {
                var allOrganizations = bombSelf.gameObject.GetComponentsInChildren<KMBombModule>().Where(a => a.ModuleType == "organizationModule"); // Find the KMBombModule from the KMBomb attached.
                foreach (var selectedOrgan in allOrganizations)
                {
                    var organScript = selectedOrgan.GetComponent("OrganizationScript"); // Check if there is an OrganizationScript attached
                    if (organScript != null)
                    {
                        var isNotTrueOrgan = false;

                        if (organIgnoredModNames == null)
                        {
                            var ignoredMods = organScript.GetValue<string[]>("ignoredModules");
                            if (ignoredMods != null) // If the module has the variable ignoredModules
                            {
                                organIgnoredModNames = ignoredMods.ToList();
                                Debug.LogFormat("<Forget Infinity #{0}>: DEBUG: Detected Ignore List From Organization: {1}", curModID, ignoredMods.Any() ? ignoredMods.Join(", ") : "null");
                            }
                            else
                            {
                                isNotTrueOrgan = true;
                            }
                        }

                        var solveOrder = organScript.GetValue<List<string>>("order");
                        if (solveOrder != null) // If the module has the variable order. This is used to determine the solve order of the modules in general.
                        {
                            Debug.LogFormat("<Forget Infinity #{0}>: DEBUG: Detected Solve Order: {1}", curModID, solveOrder.Any() ? solveOrder.Join(", ") : "null");
                            var idx = solveOrder.IndexOf(ModSelf.ModuleDisplayName);
                            if (idx != -1)
                            {
                                earliestSolveCountOrgan = earliestSolveCountOrgan == -1 ? idx : Math.Min(earliestSolveCountOrgan, idx);
                            }
                        }
                        else
                            isNotTrueOrgan = true;

                        if (isNotTrueOrgan)
                            fakeOrgansDetected++;
                    }
                    else
                    {
                        fakeOrgansDetected++;
                    }
                }
                if (fakeOrgansDetected > 0)
                    Debug.LogFormat("[Forget Infinity #{0}]: Detected a total of {1} modules that is detected as Organization but not actually Organization.", curModID, fakeOrgansDetected);
            }

            List<string> solvablemodNames = Info.GetSolvableModuleNames().Where(a => !ignoredModuleNames.Contains(a)).ToList();
            List<string> allModNames = Info.GetModuleNames().ToList();
            if (solvablemodNames.Count > 1)
            {
                if (!allModNames.Contains("Organization") || organIgnoredModNames.Contains("Forget Infinity"))
                {
                    stagestoGenerate = solvablemodNames.Count > 3 ? UnityEngine.Random.Range(3, Math.Min(solvablemodNames.Count, 100)) : solvablemodNames.Count - 1;

                    if (solvablemodNames.Count <= 100)
                        Debug.LogFormat("[Forget Infinity #{0}]: Total stages generatable: {1}", curModID, solvablemodNames.Count - 1);
                    else Debug.LogFormat("[Forget Infinity #{0}]: Too many non-ignored modules, capping at 99 total stages generatable.", curModID);

                    GenerateStages(stagestoGenerate);
                }
                else
                { // Implement Failsafe to enforce this module to be solvable if Forget Infinity is NOT ignored by Organization AND Organization is present on the bomb.
                    /*
                    string[] dialog = new string[]
                    {// A reminder of why Forget Infinity was received poorly to the community. Yes. It's a thing still. -VFlyer
                        "Organization: Why do you even exist!? No one wanted you to show up anyway!",
                        "Forget Infinity: But... I am made by a Tetris legend who has made bunch of Tetris videos!",
                        "Organization: It doesn't matter! These people saw you a few times and they didn't like how you operate in the factory.",
                        "Forget Infinity: But... This one person said I would get a second chance, right?",
                        "Organization: Pff. I saw an module better than yours and that module, who goes by Forget It Not, is more praiseworthy than you! Get out.",
                        "Forget Infinity: But...",
                        "Organization: GET OUT! No more \"but's\"! I'm done talking with you!"
                    };
                    foreach (string line in dialog)
                        Debug.LogFormat("[Forget Infinity #{0}]: {1}", curModID,line);

                    Debug.LogFormat("[Forget Infinity #{0}]: Organization is present AND not ignoring Forget Infinity! This module can be auto-solved by pressing any button.", curModID);
                    autosolvable = true;
                    */
                    // Old handler enforces autosolve regardless of when FI is shown to be ready to solve.
                    Debug.LogFormat("[Forget Infinity #{0}]: Use Boss Module Manager to prevent situations like this from happening. This was created to prevent a bricked bomb issue. - VFlyer", curModID);
                    Debug.LogFormat("[Forget Infinity #{0}]: Organization requests this module to be solved {1} module(s) after it.", curModID, earliestSolveCountOrgan);

                    if (earliestSolveCountOrgan > 1)
                    {
                        stagestoGenerate = earliestSolveCountOrgan > 3 ?
                            UnityEngine.Random.Range(3, Math.Min(earliestSolveCountOrgan, 100)) :
                            earliestSolveCountOrgan - 1;
                        GenerateStages(stagestoGenerate);
                    }
                    else
                    {
                        Debug.LogFormat("[Forget Infinity #{0}]: No stages can be generated, the module can be auto-solved by pressing any button.", curModID);
                        autosolvable = true;
                    }
                }
            }
            else
            {
                Debug.LogFormat("[Forget Infinity #{0}]: No stages can be generated, the module can be auto-solved by pressing any button.", curModID);
                autosolvable = true;
            }
        }
        catch (Exception error)
        {

            Debug.LogErrorFormat("[Forget Infinity #{0}]: Looks like you found a bug, the module has been automatically primed to auto-solve because of this.", curModID);
            Debug.LogErrorFormat("[Forget Infinity #{0}]: The error is the following:", curModID);
            Debug.LogException(error);
            Debug.LogFormat("[Forget Infinity #{0}]: For reference, the module's display stages were the following: ", curModID);
            for (int x = 0; x < stages.Count; x++)
            {
                Debug.LogFormat("[Forget Infinity #{0}]: Stage {1}: Display = {2}", curModID, x + 1, stages[x].Join(""));
            }
            Debug.LogFormat("[Forget Infinity #{0}]: Please send this log to VFlyer so that he can get this fixed.", curModID);
            Debug.LogFormat("[Forget Infinity #{0}]: Press any button to solve the module.", curModID);
            autosolvable = true;
        }
        finally
        {
            hasStarted = true;
        }
    }

    string FormatListInt(List<int> IntArray)
    {
        string output = "";
        foreach (int sgl in IntArray)
        {
            output += sgl;
        }
        return output;
    }
    string FormatListInt(int[] IntArray)
    {
        string output = "";
        foreach (int sgl in IntArray)
        {
            output += sgl;
        }
        return output;
    }
    string FormatIntListWithCommas(int[] IntArray)
    {
        bool addComma = false;
        string output = "";
        foreach (int sgl in IntArray)
        {
            output += addComma ? "," : "";
            output += sgl + 1;
            addComma = true;
        }
        return output;
    }
    IEnumerator HandleButtonAnim(GameObject selected)
    {
        for (int x = 0; x < 5; x++)
        {
            selected.transform.localPosition += new Vector3(0, -.001f, 0);
            yield return new WaitForSeconds(0);
        }
        for (int x = 5; x > 0; x--)
        {
            selected.transform.localPosition += new Vector3(0, +.001f, 0);
            yield return new WaitForSeconds(0);
        }
    }
    IEnumerator AnimateSolveAnim()
    {
        Debug.LogFormat("[Forget Infinity #{0}]: Module solved.", curModID);
        ModSelf.HandlePass();
        while (ScreenStatus.text.Length > 0 || ScreenStages.text.Length > 0)
        {
            if (ScreenStatus.text.Length > 0)
            {
                ScreenStatus.text = ScreenStatus.text.Substring(0, ScreenStatus.text.Length - 1);
                string outputDisplay = "";
                for (int x = 0; x < ScreenStatus.text.Length; x++)
                {
                    outputDisplay += ScreenStatus.text.Substring(x, 1).RegexMatch(@"[0-9]") ? UnityEngine.Random.Range(0, 10).ToString("0") : ScreenStatus.text.Substring(x, 1);
                }
                ScreenStatus.text = outputDisplay;
            }
            if (ScreenStages.text.Length > 0)
                ScreenStages.text = ScreenStages.text.Substring(0, ScreenStages.text.Length - 1).Trim();
            yield return new WaitForSeconds(0f);
        }
    }
    IEnumerator ProcessSubmittion()
    {
        bool canStrike = true;
        int crtStgIdx = 0;
        int localDelay = 89;
        ScreenStages.text = input;
        
        foreach (int oneStg in possibleStages)
        {
            if (oneStg >= 0 && oneStg < stages.Count)
            {
                if (FormatListInt(solution[oneStg]).Equals(input))
                {
                    canStrike = false;
                    crtStgIdx = oneStg;
                }
            }
        }
        if (canStrike)
        {
            Debug.LogFormat("[Forget Infinity #{0}]: {1} does not match for any of the remaining stages required to solve.", curModID, input);
            Debug.LogFormat("[Forget Infinity #{0}]: For reference, the remaining required stages to solve upon this strike are: {1}", curModID, FormatIntListWithCommas(possibleStages.Where(a => a >= 0 && a < stages.Count).ToArray()));
            ModSelf.HandleStrike();
            hasStruck = true;
            ScreenStages.color = Color.red;
        }
        else
        {
            possibleStages[possibleStages.IndexOf(crtStgIdx)] = -1;
            if (possibleStages.TrueForAll(a => a == -1))
            {
                Debug.LogFormat("[Forget Infinity #{0}]: All required stages have been solved.", curModID);
                solved = true;
                ScreenStages.color = Color.yellow;
                ScreenStatus.color = Color.yellow;
                yield return new WaitForSeconds(0f);
                StartCoroutine(AnimateSolveAnim());
                yield break;
            }
            else
                Debug.LogFormat("[Forget Infinity #{0}]: Required stage {1} has been inputted correctly.", curModID, crtStgIdx + 1);
        }
        while (localDelay >= 0)
        {
            string result = "";
            for (int x = 0; x < possibleStages.Count; x++)
            {
                if (possibleStages[x] >= 0)
                    result += (possibleStages[x] + 1).ToString("00") + " ";
                else
                    result += UnityEngine.Random.Range(0, 100).ToString("00") + " ";
            }
            ScreenStatus.text = result.Trim();
            if (localDelay % 18 == 0)
            {
                input = input.Substring(0, input.Length - 1);
                AudioHandler.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.TypewriterKey,transform);
            }
            string inputSeq = "";
            for (int x = 0; x < 5 - input.Length; x++)
            {
                inputSeq += "-";
            }
            ScreenStages.text = inputSeq + input;
            yield return new WaitForSeconds(0);
            localDelay--;
        }
        input = "";
        ScreenStages.color = Color.white;
        interactable = true;
        yield return null;
    }
    private int curStgdelay = 0;
    private int curAnimDelay = 0;
    IEnumerator RegrabStage(int requiredStg)
    {
        if (requiredStg >= 1 && requiredStg <= stages.Count)
        {
            ScreenStages.text = FormatListInt(stages[requiredStg - 1]);
            delayed = true;
            curStgdelay = 5;
            ScreenStatus.color = Color.white;
            while (curStgdelay > 0)
            {
                curStgdelay--;
                ScreenStatus.text = requiredStg.ToString("00") + "/" + stagestoGenerate.ToString("00") + " " + (curStgdelay % 10).ToString();
                yield return new WaitForSeconds(1);
            }
            delayed = false;
        }
        else Debug.LogFormat("[Forget Infinity #{0}]: Stage {1} does not exist.", curModID, requiredStg);
        interactable = true;
        yield return null;
    }

    IEnumerator CountdownStage()
    {
        if (currentStage < stagestoGenerate)
        {
            currentStage++;
            ScreenStages.text = FormatListInt(stages[currentStage - 1]);
            delayed = true;
            curStgdelay = 5;
            while (curStgdelay > 0)
            {
                curStgdelay--;
                yield return new WaitForSeconds(1);
            }
            delayed = false;
        }
        else
        {
            inFinale = true;
            Debug.LogFormat("[Forget Infinity #{0}]: The module is now in its finale phase.", curModID);
        }
        yield return null;
    }

    void Update()
    {
        if (hasStarted && !solved)
        {
            if (inFinale)
            {
                if (!interactable) return;
                string result = "";
                for (int x = 0; x < possibleStages.Count; x++)
                {
                    if (possibleStages[x] >= 0)
                        result += (possibleStages[x] + 1).ToString("00") + " ";
                    else
                        result += UnityEngine.Random.Range(0, 100).ToString("00") + " ";
                }
                ScreenStatus.text = result.Trim();
                string inputSeq = "";
                for (int x = 0; x < 5 - input.Length; x++)
                {
                    inputSeq += "-";
                }
                ScreenStages.text = inputSeq + input;
            }
            else
            {
                ScreenStatus.text = currentStage.ToString("00") + "/" + stagestoGenerate.ToString("00") + " " + (curStgdelay%10).ToString();
                if (!autosolvable && !delayed && currentStage < Info.GetSolvedModuleNames().Where(a => !ignoredModuleNames.Contains(a)).Count())
                {
                    StartCoroutine(CountdownStage());
                }
                if (currentStage - 1 < 0)
                {
                    ScreenStages.text = "-----";
                }
            }
            if (autosolvable)
            {
                curAnimDelay = curAnimDelay <= 0 ? 89 : curAnimDelay - 1;
                ScreenStages.color = curAnimDelay < 45 ? Color.red : Color.white;
                ScreenStatus.color = curAnimDelay < 45 ? Color.red : Color.white;
            }
            else if (hasStruck)
            {
                curAnimDelay = curAnimDelay <= 0 ? 49 : curAnimDelay - 1;
                ScreenStatus.color = curAnimDelay < 25 ? Color.green : Color.white;
            }
        }
    }
    // Mod Settings
    public class ForgetInfintySettings
    {
        public float PPAScaleFactor = 0.5f;
    }
    /*
    static readonly Dictionary<string, object>[] TweaksEditorSettings = new Dictionary<string, object>[]
      {
            new Dictionary<string, object>
            {
                { "Filename", "ForgetInfintySettings.json" },
                { "Name", "Forget Infinty Settings" },
                { "Listing", new List<Dictionary<string, object>>{
                    new Dictionary<string, object>
                    {
                        { "Key", "PPAScaleFactor" },
                        { "Text", "The scale factor of the number of points to award based on how many stages were grabbed from this module." },
                    },
                } }
            }
      };*/
    // Twitch Plays support
#pragma warning disable IDE0051 // Remove unused private members
    public readonly string TwitchHelpMessage = "Enter the sequence with \"!{0} press 01234\". To press the back space button, append as many \"back\" commands as needed to press the backspace button. 0-9 are acceptable digits. Space out the commands (digits excluded)!";
#pragma warning restore IDE0051 // Remove unused private members
    IEnumerator HandleAutoSolve()
    {
        if (autosolvable)
        {
            ButtonDigits[0].OnInteract();
        }
        else
        {
            inFinale = true;
            yield return new WaitForFixedUpdate();
            List<int> autoSolveStages = possibleStages.Where(a => a >= 0).ToList();
            isRecapturing = false;
            while (input.Length > 0)
            {
                BackSpaceButton.OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
            foreach (int oneStage in autoSolveStages)
            {
                while (!interactable)
                    yield return new WaitForSeconds(0);
                for (int x = 0; x < stages[oneStage].Length; x++)
                {
                    ButtonDigits[solution[oneStage][x]].OnInteract();
                    yield return new WaitForSeconds(0.1f);
                }

            }
        }
    }
    void TwitchHandleForcedSolve()
    {
        Debug.LogFormat("[Forget Infinity #{0}]: A force solve has been issued viva TP handler.", curModID);
        StartCoroutine(HandleAutoSolve());
    }
    public IEnumerator ProcessTwitchCommand(string cmd)
    {
        string[] commandLowerSet = cmd.ToLower().Split(' ');
        List<KMSelectable> pressSet = new List<KMSelectable>();
        if (commandLowerSet.Length == 0) yield break;
        if (!commandLowerSet[0].EqualsIgnoreCase("press"))
        {
            yield return "sendtochaterror The command must start with \"press\", case insensitive, to interact with this module.";
            yield break;
        }
        for (int x = 1; x < commandLowerSet.Length; x++)
        {
            if (commandLowerSet[x].RegexMatch(@"^\d+$"))
            {
                foreach (char onechar in commandLowerSet[x])
                {
                    pressSet.Add(ButtonDigits[int.Parse(onechar.ToString())]);
                }
            }
            else if (commandLowerSet[x].EqualsIgnoreCase("back"))
            {
                pressSet.Add(BackSpaceButton);
            }
            else
            {
                yield return "sendtochaterror Your command is invalid. The section \"" + commandLowerSet[x] + "\" is not valid";
                yield break;
            }
        }

        if (pressSet.Count <= 0) yield break;
        if (!inFinale)
        {
            yield return "sendtochat It's too early to do that, isn't it?";
            yield return null;
            pressSet[0].OnInteract();
            yield break;
        }
        List<int> unsolvedStages = possibleStages.Where(a => a != -1).ToList();
        foreach (KMSelectable button in pressSet)
        {
            if (!interactable)
            {
                yield return "sendtochaterror The module stopped allowing inputs to process. The rest of the inputs have been voided.";
                yield break;
            }
            yield return null;
            button.OnInteract();
            if (input.Length >= 5)
            {
                if (unsolvedStages.Count == 1 && input.Equals(solution[unsolvedStages[0]].Join("")))
                    yield return "awardpointsonsolve " + Math.Max(Mathf.CeilToInt(PPAScaling * stagestoGenerate), 1).ToString();
                yield return "solve";
                yield return "strike";
            }
            yield return new WaitForSeconds(0.1f);
        }
        
    }
}
