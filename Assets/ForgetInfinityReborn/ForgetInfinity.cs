using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Text;
using System;
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
    private static int curModID;

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
        if (organIgnoredModNames == null)
            organIgnoredModNames = GetComponent<KMBossModule>().GetIgnoredModules("Organization").ToList();
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
            Debug.LogErrorFormat("[Forget Infinty #{0}]: The settings for Forget Infinty does not exist! The module will use default settings instead.", curModID);
            PPAScaling = 0.5f;
        }
    }
    void Start()
    {
        ModSelf.OnActivate += delegate
        {
            try
            {
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

                        Debug.LogFormat("[Forget Infinity #{0}]: Total stages generated: {1}", curModID, stagestoGenerate);
                        Debug.LogFormat("[Forget Infinity #{0}]: All stages: ", curModID, stagestoGenerate);

                        for (int x = 0; x < stagestoGenerate; x++)
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

                            int FiLetters = Info.GetSerialNumberLetters().Where(a => a.EqualsAny('F', 'I')).ToList().Count;
                            for (int idx = 0; idx < finalStageNumbers.Length; idx++)
                                finalStageNumbers[idx] -= FiLetters;
                            // Individual Slots
                            // Slot 1
                            if (solvablemodNames.Contains("Tetris"))
                                finalStageNumbers[0] = stages[x][0] + 7;
                            else if (finalStageNumbers[0] >= 10 && finalStageNumbers[0] % 2 == 0)
                                finalStageNumbers[0] /= 2;
                            else if (finalStageNumbers[0] < 0)
                                finalStageNumbers[0] *= -1;
                            else
                                finalStageNumbers[0] += 1;
                            // Slot 2
                            if (Info.CountDuplicatePorts() > 0)
                                finalStageNumbers[1] += Info.CountDuplicatePorts();
                            else if (Info.GetPortCount() == 0)
                                finalStageNumbers[1] += stages[x][0] + stages[x][2];
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
                            else if (finalStageNumbers[3] % 3 != Info.GetSolvableModuleNames().Count % 3)
                                finalStageNumbers[3] = 18 - finalStageNumbers[3];
                            // Slot 5
                            int[,] slotTable5th = new int[,] {
                            { 0, 1, 2, 3, 4 },
                            { 5, 6, 7, 8, 9 },
                            { stages[x][4], 1 + stages[x][4], 9 - stages[x][4], stages[x][4] - 1, stages[x][4] + 5 },
                            { 9, 8, 7, 6, 5 },
                            { 4, 3, 2, 1, 0 }
                        };
                            int rowCellToGrab = finalStageNumbers[4] - (Mathf.FloorToInt(finalStageNumbers[4] / 5.0f) * 5);
                            finalStageNumbers[4] = slotTable5th[rowCellToGrab, lastDigitInSerial / 2];
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
                    else
                    { // Implement Failsafe to enforce this module to be solvable if Forget Infinity is NOT ignored by Organization AND Organization is present on the bomb.
                        Debug.LogFormat("[Forget Infinity #{0}]: Organization: Why do you even exist!? No one wanted you to show up anyway!", curModID);
                        Debug.LogFormat("[Forget Infinity #{0}]: Forget Infinity: But... I am made by a Tetris legend who has made bunch of Tetris bootleg videos!", curModID);
                        Debug.LogFormat("[Forget Infinity #{0}]: Organization: It doesn't matter! These people saw you a few times and they didn't like how you operate in the factory.", curModID);
                        Debug.LogFormat("[Forget Infinity #{0}]: Forget Infinity: But... I am an easier module... Right?", curModID);
                        Debug.LogFormat("[Forget Infinity #{0}]: Organization: Pff. I saw an module easier than yours and that module is more likeable than you! Get out.", curModID);
                        Debug.LogFormat("[Forget Infinity #{0}]: Forget Infinity: But...", curModID);
                        Debug.LogFormat("[Forget Infinity #{0}]: Organization: GET OUT! No more \"but's\"!", curModID);
                        Debug.LogFormat("[Forget Infinity #{0}]: Organization is present AND not ignoring Forget Infinity! This module can be auto-solved by pressing any button.", curModID);
                        autosolvable = true;
                    }
                }
                else
                {
                    Debug.LogFormat("[Forget Infinity #{0}]: No stages can be generated, the module can be auto-solved by pressing any button.", curModID);
                    autosolvable = true;
                }
            }
            catch
            {
                Debug.LogFormat("[Forget Infinity #{0}]: Looks like you found a bug, the module has been automatically primed to auto-solve because of this.", curModID);
                Debug.LogFormat("[Forget Infinity #{0}]: For reference, the module's display stages were the following: ", curModID);
                for (int x = 0; x < stages.Count; x++)
                {
                    Debug.LogFormat("[Forget Infinity #{0}]: Stage {1}: Display = {2}", curModID, x + 1, stages[x].Join(""));
                }
                Debug.LogFormat("[Forget Infinity #{0}]: Please report this log to VFlyer so that he can get this fixed.", curModID);
                autosolvable = true;
            }
            finally
            {
                hasStarted = true;
            }
        };
        BackSpaceButton.OnInteract += delegate
        {
            AudioHandler.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
            BackSpaceButton.AddInteractionPunch();
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
                ModSelf.HandlePass();
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
                    ModSelf.HandlePass();
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

    IEnumerator AnimateSolveAnim()
    {
        Debug.LogFormat("[Forget Infinity #{0}]: Module solved.", curModID);
        while (ScreenStatus.text.Length > 0)
        {
            ScreenStatus.text = ScreenStatus.text.Substring(0, ScreenStatus.text.Length - 1);
            string outputDisplay = "";
            for (int x = 0; x < ScreenStatus.text.Length; x++)
            {
                outputDisplay += ScreenStatus.text.Substring(x, 1).RegexMatch(@"[0-9]") ? UnityEngine.Random.Range(0, 10).ToString("0") : ScreenStatus.text.Substring(x, 1);
            }
            ScreenStatus.text = outputDisplay;
            yield return new WaitForSeconds(0);
        }
        while (ScreenStages.text.Length > 0)
        {
            ScreenStages.text = ScreenStages.text.Substring(0, ScreenStages.text.Length - 1).Trim();
            yield return new WaitForSeconds(0.2f);
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
                ModSelf.HandlePass();
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
      };
    // Twitch Plays support

    public readonly string TwitchHelpMessage = "Enter the sequence with \"!{0} press 01234\". To press the back space button, append as many \"back\" commands as needed to press the backspace button. 0-9 are acceptable digits. Space out the commands (digits excluded)!";

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
                if (possibleStages.Count == 1 && input.Equals(solution[possibleStages.Where(a => a != -1).ToArray()[0]]))
                    yield return "awardpoints " + Math.Min(Mathf.RoundToInt(PPAScaling * stagestoGenerate), 1).ToString();
                yield return "solve";
                yield return "strike";
            }
            yield return new WaitForSeconds(0.1f);
        }
        
    }
}
