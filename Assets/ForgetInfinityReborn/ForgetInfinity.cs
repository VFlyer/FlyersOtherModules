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

    public List<int[]> stages = new List<int[]>();
    public List<int[]> solution = new List<int[]>();
    public List<int> possibleStages = new List<int>();

    public string input = "";

    private int stagestoGenerate = 0;
    private int currentStage = 0;

    
    private int inputStagesRequired = 0;

    private static int modID = 1;
    private static int curModID;

	// Use this for initialization
	void Awake() {
		if (ignoredModuleNames == null)
			ignoredModuleNames = GetComponent<KMBossModule>().GetIgnoredModules("Forget Infinity", new string[]{
                "14",
                "Bamboozling Time Keeper",
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
        //"Forget" Modules in this list, Simon's Stages, Souvenir, Tallordered Keys: Requires this module to be solved without Boss Module Manager
        //Time Keeper, Timing is Everything, Turn The Key: Bomb Timer Sensitive that can stall bombs
        //
        //Forget Infinity: DON'T HANG BOMBS WITH DUPLICATES OF THIS
        curModID = modID++;
	}
    void Start()
    {
        ModSelf.OnActivate += delegate
        {
            List<string> solvablemodNames = Info.GetSolvableModuleNames().Where(a => !ignoredModuleNames.Contains(a)).ToList();
            List<string> allModNames = Info.GetModuleNames().ToList();
            if (solvablemodNames.Count > 1)
            {
                if (!allModNames.Contains("Organization") || organIgnoredModNames.Contains("Forget Infinity"))
                {
                    stagestoGenerate = solvablemodNames.Count > 3 ? UnityEngine.Random.Range(3, Math.Min(solvablemodNames.Count,100)) : solvablemodNames.Count - 1;

                    if (solvablemodNames.Count <= 100)
                    Debug.LogFormat("[Forget Infinity {0}]: Total stages generatable: {1}", curModID, solvablemodNames.Count - 1);
                    else Debug.LogFormat("[Forget Infinity {0}]: Too many non-ignored modules, capping at 99 total stages generatable.", curModID);

                    Debug.LogFormat("[Forget Infinity {0}]: Total stages generated: {1}", curModID,stagestoGenerate);
                    Debug.LogFormat("[Forget Infinity {0}]: All stages: ", curModID, stagestoGenerate);
                    print("##|Init.|Solut");
                    
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
                        int[] finalStageNumbers = new int[5];
                        stages[x].CopyTo(finalStageNumbers, 0);
                        // Begin Solution Calculations
                        // Culumulative Slot Calculations
                        if (Info.IsPortPresent(Port.StereoRCA))
                        {
                            finalStageNumbers = finalStageNumbers.Reverse().ToArray();
                        }

                        int batterycount = Info.GetBatteryCount();
                        for (int idx = 0; idx < finalStageNumbers.Length; idx++)
                            finalStageNumbers[idx] += batterycount;

                        int FiLetters = Info.GetSerialNumberLetters().Where(a => a.EqualsAny('F', 'I')).ToList().Count;
                        for (int idx = 0; idx < finalStageNumbers.Length; idx++)
                            finalStageNumbers[idx] -= FiLetters;
                        //Individual Slots

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
                        print((x+1).ToString("00")+"|"+FormatListInt(stages[x]) + "|" + FormatListInt(solution[x]));
                    }
                    while (possibleStages.Count < Math.Min(stagestoGenerate, 3))// Memoryless Randomizer Starts Here
                    {
                        int randomStage = UnityEngine.Random.Range(0,stagestoGenerate);
                        if (!possibleStages.Contains(randomStage))
                            possibleStages.Add(randomStage);
                    }
                    inputStagesRequired = possibleStages.Count;
                    Debug.LogFormat("[Forget Infinity {0}]: Stages required to solve: {1}", curModID,FormatIntListWithCommas(possibleStages.ToArray()));
                }
                else
                { // Implement Failsafe to enforce this module to be solvable if Forget Infinity is NOT ignored by Organization AND Organization is present on the bomb.
                    Debug.LogFormat("[Forget Infinity {0}]: Organization is present AND not ignoring Forget Infinity! This module can be auto-solved by pressing any button.", curModID);
                    autosolvable = true;
                }
            }
            else
            {
                Debug.LogFormat("[Forget Infinity {0}]: No stages can be generated, the module can be auto-solved by pressing any button.",curModID);
                autosolvable = true;
            }
            hasStarted = true;
        };
        BackSpaceButton.OnInteract += delegate
        {
            if (!interactable || solved) return false;
            AudioHandler.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
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
                    Debug.LogFormat("[Forget Infinity {0}]: Attempting to recapture stage {1} at a cost of a strike", curModID, stageToGrab);
                    if (!hasStruck)
                        ModSelf.HandleStrike();
                    else Debug.LogFormat("[Forget Infinity {0}]: The module has a free capture; consuming it.", curModID);
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
                Debug.LogFormat("[Forget Infinity {0}]: Module solved.", curModID);
                solved = true;
                ModSelf.HandlePass();
            }
            else
            {
                ModSelf.HandleStrike();
                Debug.LogFormat("[Forget Infinity {0}]: Defuser pressed a button before module is ready.", curModID);
            }
            return false;
        };
        for (int x = 0; x < ButtonDigits.Length; x++)
        {
            int y = x;
            ButtonDigits[x].OnInteract += delegate
            {
                if (!interactable || solved) return false;
                AudioHandler.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress,transform);
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
                    
                    Debug.LogFormat("[Forget Infinity {0}]: Module solved.", curModID);
                    solved = true;
                    ModSelf.HandlePass();
                }
                else
                {
                    ModSelf.HandleStrike();
                    Debug.LogFormat("[Forget Infinity {0}]: Defuser pressed a button before module is ready.", curModID);
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
    IEnumerator ProcessSubmittion()
    {
        bool canStrike = true;
        int crtStgIdx = 0;
        ScreenStages.text = input;
        yield return new WaitForSeconds(1);
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
            ModSelf.HandleStrike();
            Debug.LogFormat("[Forget Infinity {0}]: {1} does not match for any of the given stages required to solve.", curModID,input);
            hasStruck = true;
            ScreenStages.color = Color.red;
            yield return new WaitForSeconds(1);
            ScreenStages.color = Color.white;
        }

        input = "";
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
        else Debug.LogFormat("[Forget Infinity {0}]: Stage {1} does not exist.", curModID, requiredStg);
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
            Debug.LogFormat("[Forget Infinity {0}]: The module is now in its finale phase.", curModID);
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
                    result += (possibleStages[x] + 1).ToString("00") + " ";
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
    // Twitch Plays support

    public readonly string TwitchHelpMessage = "Enter the sequence with \"!{0} press 1 2 3 4 5...\". Reset with \"!{0} reset\".";

    public IEnumerator ProcessTwitchCommand(string cmd)
    {

        yield break;
    }
}
