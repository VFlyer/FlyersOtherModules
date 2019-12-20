using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class TallorderedKeysScript : MonoBehaviour
{

    public KMAudio Audio;
    public KMBombInfo bomb;
    public KMColorblindMode ColorblindMode;
    public KMBombModule modSelf;
    public List<KMSelectable> keys;
    public Renderer[] meter;
    public Renderer[] keyID;
    public TextMesh disp;
    public Material[] keyColours;

    private static List<List<List<string>>> keyList = new List<List<List<string>>>
    { new List<List<string>> { new List<string> { "RR1", "RR2", "RR3", "RR4", "RR5", "RR6" },
                               new List<string> { "RG1", "RG2", "RG3", "RG4", "RG5", "RG6" },
                               new List<string> { "RB1", "RB2", "RB3", "RB4", "RB5", "RB6" },
                               new List<string> { "RC1", "RC2", "RC3", "RC4", "RC5", "RC6" },
                               new List<string> { "RM1", "RM2", "RM3", "RM4", "RM5", "RM6" },
                               new List<string> { "RY1", "RY2", "RY3", "RY4", "RY5", "RY6" } },
      new List<List<string>> { new List<string> { "GR1", "GR2", "GR3", "GR4", "GR5", "GR6" },
                               new List<string> { "GG1", "GG2", "GG3", "GG4", "GG5", "GG6" },
                               new List<string> { "GB1", "GB2", "GB3", "GB4", "GB5", "GB6" },
                               new List<string> { "GC1", "GC2", "GC3", "GC4", "GC5", "GC6" },
                               new List<string> { "GM1", "GM2", "GM3", "GM4", "GM5", "GM6" },
                               new List<string> { "GY1", "GY2", "GY3", "GY4", "GY5", "GY6" } },
      new List<List<string>> { new List<string> { "BR1", "BR2", "BR3", "BR4", "BR5", "BR6" },
                               new List<string> { "BG1", "BG2", "BG3", "BG4", "BG5", "BG6" },
                               new List<string> { "BB1", "BB2", "BB3", "BB4", "BB5", "BB6" },
                               new List<string> { "BC1", "BC2", "BC3", "BC4", "BC5", "BC6" },
                               new List<string> { "BM1", "BM2", "BM3", "BM4", "BM5", "BM6" },
                               new List<string> { "BY1", "BY2", "BY3", "BY4", "BY5", "BY6" } },
      new List<List<string>> { new List<string> { "CR1", "CR2", "CR3", "CR4", "CR5", "CR6" },
                               new List<string> { "CG1", "CG2", "CG3", "CG4", "CG5", "CG6" },
                               new List<string> { "CB1", "CB2", "CB3", "CB4", "CB5", "CB6" },
                               new List<string> { "CC1", "CC2", "CC3", "CC4", "CC5", "CC6" },
                               new List<string> { "CM1", "CM2", "CM3", "CM4", "CM5", "CM6" },
                               new List<string> { "CY1", "CY2", "CY3", "CY4", "CY5", "CY6" } },
      new List<List<string>> { new List<string> { "MR1", "MR2", "MR3", "MR4", "MR5", "MR6" },
                               new List<string> { "MG1", "MG2", "MG3", "MG4", "MG5", "MG6" },
                               new List<string> { "MB1", "MB2", "MB3", "MB4", "MB5", "MB6" },
                               new List<string> { "MC1", "MC2", "MC3", "MC4", "MC5", "MC6" },
                               new List<string> { "MM1", "MM2", "MM3", "MM4", "MM5", "MM6" },
                               new List<string> { "MY1", "MY2", "MY3", "MY4", "MY5", "MY6" } },
      new List<List<string>> { new List<string> { "YR1", "YR2", "YR3", "YR4", "YR5", "YR6" },
                               new List<string> { "YG1", "YG2", "YG3", "YG4", "YG5", "YG6" },
                               new List<string> { "YB1", "YB2", "YB3", "YB4", "YB5", "YB6" },
                               new List<string> { "YC1", "YC2", "YC3", "YC4", "YC5", "YC6" },
                               new List<string> { "YM1", "YM2", "YM3", "YM4", "YM5", "YM6" },
                               new List<string> { "YY1", "YY2", "YY3", "YY4", "YY5", "YY6" } } };
    private int[][][] keyvals = new int[6][][] { new int[6][] { new int[6], new int[6], new int[6], new int[6], new int[6], new int[6] }, new int[6][] { new int[6], new int[6], new int[6], new int[6], new int[6], new int[6] }, new int[6][] { new int[6], new int[6], new int[6], new int[6], new int[6], new int[6] }, new int[6][] { new int[6], new int[6], new int[6], new int[6], new int[6], new int[6] }, new int[6][] { new int[6], new int[6], new int[6], new int[6], new int[6], new int[6] }, new int[6][] { new int[6], new int[6], new int[6], new int[6], new int[6], new int[6] }};
    private string[] selectedKeys = new string[6];
    private int[][] info = new int[6][] { new int[3], new int[3], new int[3], new int[3], new int[3], new int[3] };
    private readonly string[] colorList = new string[] {"R", "G", "B", "C", "M", "Y" }; // The source code obtained from the version of Tallordered Keys did NOT have the colorblind issue fixed. This is here for the lack of updating source code. -VFlyer
    private static string[] exempt = null;
    private int stage;
    private int pressCount;
    private int resetCount;
    private IEnumerator sequence;
    private bool[] alreadypressed = new bool[6] { true, true, true, true, true, true };
    private bool pressable;
    private List<string> presses = new List<string> { };
    private List<int> answer = new List<int> { };
    private List<string> labelList = new List<string> { };
    private bool colorblind;
    private bool starting = true;
    private bool startboss;
    private int buffer;

    //Logging
    static int moduleCounter = 1;
    int moduleID;
    private bool moduleSolved;

    private void Awake()
    {
        moduleID = moduleCounter++;
        sequence = Shuff();
        disp.text = string.Empty;
        foreach (Renderer m in meter)
        {
            m.material = keyColours[7];
        }
        foreach (KMSelectable key in keys)
        {
            key.transform.localPosition = new Vector3(0, 0, -1f);
            key.OnInteract += delegate () { KeyPress(key); return false; };
        }
        exempt = GetComponent<KMBossModule>().GetIgnoredModules("Tallordered Keys", new string[]
        {
            "Forget Me Not",
            "Forget Everything",
            "Forget This",
            "Forget Infinity",
            "Forget Them All",
            "Simon's Stages",
            "Turn The Key",
            "The Time Keeper",
            "Timing is Everything",
            "Alchemy",
            "Cookie Jars",
            "Purgatory",
            "Hogwarts",
            "Souvenir",
            "The Swan",
            "Divided Squares",
            "Tallordered Keys",
            "Simon Supervises",
            "Bad Mouth",
            "Bad TV",
            "Simon Superintends"
        });
    }
    bool hasStarted = false;
    void Start()
    {
        colorblind = ColorblindMode.ColorblindModeActive;
        modSelf.OnActivate += delegate ()
        {
            stage = bomb.GetSolvableModuleNames().Where(x => !exempt.Contains(x)).Count();
            List<int> initialList = new List<int> { 1, 2, 3, 4, 5, 6 };
            for(int i = 0; i < 6; i++)
            {
                int rand = Random.Range(0, initialList.Count());
                answer.Add(initialList[rand]);
                initialList.RemoveAt(rand);
            }
            hasStarted = true;
            StopCoroutine(ShuffleBeforeActivation());
            Reset();
        };
        StartCoroutine(ShuffleBeforeActivation());// Scramble the keys before the module starts to give a vibe that it hasn't detected yet.
    }
    
    private void Update()
    {
        buffer++;
        if(buffer > 300)// increase buffer to a larger number for reading each given stage
        {
            if(stage != bomb.GetSolvableModuleNames().Where(x => !exempt.Contains(x)).Count() - bomb.GetSolvedModuleNames().Where(x => !exempt.Contains(x)).Count() && stage != 0)
            {
                stage--;// Change "stage = bomb.GetSolvableModuleNames().Where(x => !exempt.Contains(x)).Count() - bomb.GetSolvedModuleNames().Where(x => !exempt.Contains(x)).Count()" to "stage--" to enforce stages to generate and to fix for synced solves in the future.
                Reset();
                buffer = 0;
            }
        }
    }

    private void KeyPress(KMSelectable key)
    {
        if (alreadypressed[keys.IndexOf(key)] == false && moduleSolved == false && pressable == true)
        {
            GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
            alreadypressed[keys.IndexOf(key)] = true;
            presses.Add((keys.IndexOf(key) + 1).ToString());
            key.transform.localPosition = new Vector3(0, 0, -1f);
            key.AddInteractionPunch();
            if (pressCount < 5)
            {
                pressCount++;
            }
            else
            {
                pressCount = 0;
                string[] answ = new string[6];
                for (int i = 0; i < 6; i++)
                {
                    answ[i] = (answer.IndexOf(i + 1) + 1).ToString();
                }
                string ans = string.Join(string.Empty, answ);
                string[] press = presses.ToArray();
                string pr = string.Join(string.Empty, press);
                Debug.LogFormat("[Tallordered Keys #{0}] After {1} reset(s), the buttons were pressed in the order: {2}", moduleID, resetCount, pr);
                if (ans == pr)
                {                   
                    Audio.PlaySoundAtTransform("InputCorrect", transform);
                    moduleSolved = true;
                }
                else
                {
                    GetComponent<KMBombModule>().HandleStrike();
                }
                presses.Clear();
                labelList.Clear();
                resetCount++;
                Reset();
            }
        }
    }

    private void setKey(int keyIndex, int? color = null, int? labelColor = null, int? label = null)
    {
        if (stage == 0)
        {
            keyID[keyIndex].material = keyColours[color ?? info[keyIndex][0]];
            switch (labelColor ?? info[keyIndex][1])
            {
                case 0:
                    keys[keyIndex].GetComponentInChildren<TextMesh>().color = new Color32(255, 25, 25, 255);
                    break;
                case 1:
                    keys[keyIndex].GetComponentInChildren<TextMesh>().color = new Color32(25, 255, 25, 255);
                    break;
                case 2:
                    keys[keyIndex].GetComponentInChildren<TextMesh>().color = new Color32(25, 25, 255, 255);
                    break;
                case 3:
                    keys[keyIndex].GetComponentInChildren<TextMesh>().color = new Color32(25, 255, 255, 255);
                    break;
                case 4:
                    keys[keyIndex].GetComponentInChildren<TextMesh>().color = new Color32(255, 75, 255, 255);
                    break;
                default:
                    keys[keyIndex].GetComponentInChildren<TextMesh>().color = new Color32(255, 255, 75, 255);
                    break;
            }
            var labelStr = (label ?? info[keyIndex][2] + 1).ToString();
            if (colorblind)
                labelStr += "\n" + colorList[info[keyIndex][1]] + "\n\n" + colorList[info[keyIndex][0]]; // The source code obtained from the version of Tallordered Keys did NOT have the colorblind issue fixed. This is here for the lack of updating source code. -VFlyer
            keys[keyIndex].GetComponentInChildren<TextMesh>().text = labelStr;
        }
        else
        {
            keyID[keyIndex].material = keyColours[color ?? "RGBCMY".IndexOf(selectedKeys[keyIndex][0])];
            switch (labelColor ?? "RGBCMY".IndexOf(selectedKeys[keyIndex][1]))
            {
                case 0:
                    keys[keyIndex].GetComponentInChildren<TextMesh>().color = new Color32(255, 25, 25, 255);
                    break;
                case 1:
                    keys[keyIndex].GetComponentInChildren<TextMesh>().color = new Color32(25, 255, 25, 255);
                    break;
                case 2:
                    keys[keyIndex].GetComponentInChildren<TextMesh>().color = new Color32(25, 25, 255, 255);
                    break;
                case 3:
                    keys[keyIndex].GetComponentInChildren<TextMesh>().color = new Color32(25, 255, 255, 255);
                    break;
                case 4:
                    keys[keyIndex].GetComponentInChildren<TextMesh>().color = new Color32(255, 75, 255, 255);
                    break;
                default:
                    keys[keyIndex].GetComponentInChildren<TextMesh>().color = new Color32(255, 255, 75, 255);
                    break;
            }
            var labelStr = (label ?? "#123456".IndexOf(selectedKeys[keyIndex][2])).ToString();
            if (colorblind)
                labelStr += "\n" + selectedKeys[keyIndex][1] + "\n\n" + selectedKeys[keyIndex][0];
            keys[keyIndex].GetComponentInChildren<TextMesh>().text = labelStr;
        }
    }

    private void Reset()
    {
        if (moduleSolved == false)
        {
            disp.text = stage.ToString();
            labelList.Clear();
            if (stage > 0)
            {
                for (int i = 0; i < 6; i++)
                {
                    info[i][0] = Random.Range(0, keyList.Count());
                    info[i][1] = Random.Range(0, keyList[info[i][0]].Count());
                    info[i][2] = Random.Range(0, keyList[info[i][0]][info[i][1]].Count());
                    labelList.Add(keyList[info[i][0]][info[i][1]][info[i][2]]);
                    selectedKeys[i] = keyList[info[i][0]][info[i][1]][info[i][2]];
                    keyvals[info[i][0]][info[i][1]][info[i][2]] = i + 1;
                    alreadypressed[i] = true;
                }
                string[] label = labelList.ToArray();
                string l = string.Join(", ", label);
                Debug.LogFormat("[Tallordered Keys #{0}] Stage {1}: the keys were {2}", moduleID, stage, l);
                StartCoroutine(sequence);
            }
            else if(stage == 0 && starting == true)
            {
                moduleSolved = true;
                StartCoroutine(sequence);
            }
            else if(stage == 0 && starting == false)
            {
                string[] identifier = new string[6];
                for(int i = 0; i < 6; i++)
                {
                    while (true)
                    {
                        info[i][0] = Random.Range(0, 6);
                        info[i][1] = Random.Range(0, 6);
                        info[i][2] = Random.Range(0, 6);
                        if (keyvals[info[i][0]][info[i][1]][info[i][2]] == answer[i])
                        {
                            identifier[i] = keyList[info[i][0]][info[i][1]][info[i][2]];
                            break;
                        }
                    }
                    labelList.Add(identifier[i]);
                }
                if(startboss == false)
                {
                    startboss = true;
                    string[] ans = new string[6];
                    for(int i = 0; i < 6; i++)
                    {
                        ans[i] = (answer.IndexOf(i + 1) + 1).ToString();
                    }
                    Debug.LogFormat("[Tallordered Keys #{0}] The keys should be pressed in the order: {2}", moduleID, resetCount, string.Join(string.Empty, ans));
                }
                string[] label = labelList.ToArray();
                string l = string.Join(", ", label);
                Debug.LogFormat("[Tallordered Keys #{0}] After {1} reset(s): the keys were {2}", moduleID, resetCount, l);
                StartCoroutine(sequence);
            }
        }
        else
        {
            StartCoroutine(sequence);
        }
        starting = false;
    }

    private IEnumerator ShuffleBeforeActivation()
    {
        while (!hasStarted)
        {
            for (int j = 0; j < 6; j++)
            {
                if (alreadypressed[j] == true)
                {
                    setKey(j, Random.Range(0, 6), Random.Range(0, 6), Random.Range(1, 7));
                }
            }
            yield return new WaitForSeconds(0.1f);
        }
        yield return null;
    }
    private IEnumerator Shuff()
    {
        for (int i = 0; i < 18; i++)
        {
            if (i % 3 == 2)
            {
                if (moduleSolved == true)
                {
                    disp.text = string.Empty;
                    alreadypressed[(i - 2) / 3] = false;
                    meter[(i - 2) / 3].material = keyColours[8];
                    keyID[(i - 2) / 3].material = keyColours[6];
                    keys[(i - 2) / 3].GetComponentInChildren<TextMesh>().color = new Color32(0, 0, 0, 255);
                    keys[(i - 2) / 3].GetComponentInChildren<TextMesh>().text = "0";
                    if (i == 17)
                    {
                        GetComponent<KMBombModule>().HandlePass();
                        GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
                    }
                }
                else
                {
                    if (stage == 0)
                    {
                        keys[(i - 2) / 3].transform.localPosition = new Vector3(0, 0, 0);
                        GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonRelease, transform);
                    }
                    alreadypressed[(i - 2) / 3] = false;
                    setKey((i - 2) / 3);
                }
                if (i == 17)
                {
                    i = -1;
                    if (stage == 0)
                    {
                        pressable = true;
                    }
                    StopCoroutine(sequence);
                }
            }
            else
            {
                for (int j = 0; j < 6; j++)
                {
                    if (alreadypressed[j] == true)
                    {
                        setKey(j, Random.Range(0, 6), Random.Range(0, 6), Random.Range(1, 7));
                    }
                }
            }
            yield return new WaitForSeconds(0.1f);
        }
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} press 123456 [position in reading order] | !{0} colorblind";
#pragma warning restore 414

    private IEnumerator ProcessTwitchCommand(string command)
    {
        if (Regex.IsMatch(command, @"^\s*colorblind\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            colorblind = true;
            for (int i = 0; i < keys.Count; i++)
                setKey(i);
            yield return null;
            yield break;
        }

        var m = Regex.Match(command, @"^\s*(?:press\s*)?([123456 ,;]+)\s*$");
        if (!m.Success)
            yield break;
        if (!startboss)
        {
            yield return "sendtochaterror Tallordered Keys is not ready for inputs yet.";
            yield break;
        }
        foreach (var keyToPress in m.Groups[1].Value.Where(ch => ch >= '1' && ch <= '6').Select(ch => keys[ch - '1']))
        {
            yield return null;
            while (!pressable)
                yield return "trycancel";
            yield return new[] { keyToPress };
        }
        yield return "solve"; // Does the current source code have this?
    }
}

