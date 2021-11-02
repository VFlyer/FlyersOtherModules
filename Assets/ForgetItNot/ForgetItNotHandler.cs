using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ForgetItNotHandler : MonoBehaviour {

    public KMBombInfo bombInfo;
    public KMBossModule bossHandler;
    public KMSelectable[] digitSelectables = new KMSelectable[10];
    public MeshRenderer[] leds = new MeshRenderer[10];
    public KMBombModule modSelf;
    public Material[] statusLEDClr = new Material[2];
    public KMAudio kMAudio;

    public TextMesh inputTextMesh, textMeshBig, textMeshStage;

    private int curstagenum = -1;
    private int correctinputs = 0;
    private string combinedDisplayString = "";
    private string[] ignoredModules;

    private int totalstages;

    private static int modID = 1;
    private int curModID;
    private bool canStart = false;


    private void Awake()
    {
        curModID = modID++;
        inputTextMesh.text = "";
        textMeshBig.text = "";
        textMeshStage.text = "";
    }

    // Use this for initialization
    void Start()
    {
        // Check on Ignore List on Repo
        if (ignoredModules == null)
            ignoredModules = bossHandler.GetIgnoredModules("Forget It Not", new string[]{
                "14",
                "Cruel Purgatory",
                "Forget Enigma",
                "Forget Everything",
                "Forget It Not",
                "Forget Me Later",
                "Forget Me Not",
                "Forget Perspective",
                "Forget Them All",
                "Forget This",
                "Forget Us Not",
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
            });
        //
        // Default Ignore Reasons:
        // Forget It Not: DON'T HANG BOMBS WITH DUPLICATES OF THIS 
        // Other "Forget" modules, Tallordered Keys, Simon's Stages, Übermodule: Requires this module to be solved without Boss Module Manager updated
        // Purgatory + "Cruel" variant: Rare last condition that can hang a bomb
        // The Time Keeper, Timing is Everything, Turn The Key: Bomb Timer sensitive, bomb stalling is NOT FUN.
        // Souvenir: Requires this module to be solved, can eat time when this module is not in "finale" phase.
        // Organization: Requires this module to be solved, MUST enter force-solve phase if requested.
        // The Very Annoying Button, The Troll: Worst case senario requires this module to be solved or the bomb hangs.
        //
        modSelf.OnActivate += delegate () { 
            totalstages = bombInfo.GetSolvableModuleNames().Where(a => !ignoredModules.Contains(a)).ToList().Count;
            if (totalstages > 0)
            {
                Debug.LogFormat("[Forget It Not #{0}]: {1} stage(s) generatable.", curModID, totalstages);
                // Start generating stages
                for (int x = 0; x < totalstages; x++)
                {
                    combinedDisplayString += Random.Range(0, 10).ToString();
                }
                string DebugString = "";
                int o = 0;
                for (int i = 0; i < combinedDisplayString.Length / 3; i++)
                {
                    //print(combinedDisplayString.Substring(i * 3, 3));
                    DebugString += combinedDisplayString.Substring(i * 3, 3) + " ";
                    o++;
                }
                DebugString += combinedDisplayString.Substring(o * 3, Mathf.Min(3, combinedDisplayString.Length - (o * 3)));
                Debug.LogFormat("[Forget It Not #{0}]: Display: {1}", curModID, DebugString.Trim());
                canStart = true;
            }
            else
            {
                Debug.LogFormat("[Forget It Not #{0}]: Auto solving... Module cannot generate stages.", curModID);
                modSelf.HandlePass();
            }
        };
        bombInfo.OnBombExploded += delegate () {
            if (curstagenum < totalstages)
            {
                if (curstagenum + 1 < totalstages)
                    Debug.LogFormat("[Forget It Not #{0}]: The module displayed up to {1} stage(s) before the bomb detonated.", curModID, curstagenum + 1);
                else
                    Debug.LogFormat("[Forget It Not #{0}]: All {1} stage(s) were displayed before the bomb detonated.", curModID, curstagenum + 1);
            }
            else if (correctinputs < totalstages)
            {
                Debug.LogFormat("[Forget It Not #{0}]: Up to {1} stage(s) were inputted correct before the bomb detonated.", curModID, correctinputs);
            }
        };
        for (int x = 0; x < digitSelectables.Count(); x++)
        {
            int y = x;
            digitSelectables[x].OnInteract += delegate() {
                digitSelectables[y].AddInteractionPunch(0.05f);
                kMAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
                if (curstagenum < totalstages)
                {
                    modSelf.HandleStrike();
                    Debug.LogFormat("[Forget It Not #{0}]: Not yet! Defuser pressed a digit before module is ready for input.", curModID);
                    return false;
                }
                if (correctinputs < totalstages)
                {
                    int currentDigit = int.Parse(combinedDisplayString.Substring(correctinputs, 1));
                    if (currentDigit == y)
                    {
                        correctinputs++;
                        ShowCurrentInput();
                        if (correctinputs >= totalstages)
                        {

                            modSelf.HandlePass();
                            Debug.LogFormat("[Forget It Not #{0}]: Module defused!", curModID);
                            StartCoroutine(ActivateSolveAnim());
                        }
                    }
                    else
                    {
                        HandleIncorrectPress();
                        Debug.LogFormat("[Forget It Not #{0}]: For the input on stage {1}, {2} is incorrectly pressed.", curModID,correctinputs+1,y);
                        modSelf.HandleStrike();
                    }
                }
                return false;
            };
        }

    }
    private int idxlit = -1;
    private void HandleIncorrectPress()
    {
        if (correctinputs < curstagenum)
        {
            int currentDigit = int.Parse(combinedDisplayString.Substring(correctinputs, 1));
            idxlit = currentDigit;
            for (int i = 0; i < leds.Length; i++)
            {
                leds[i].material = currentDigit == i ? statusLEDClr[0] : statusLEDClr[1];
            }
        }
        if (correctinputs < 99)
            textMeshStage.text = (correctinputs + 1).ToString();
        else
        {
            textMeshStage.text = ((curstagenum + 1) % 100).ToString("00");
        }
    }

    private void AdvanceStage()
    {
        if (curstagenum < 99)
            textMeshStage.text = (curstagenum + 1).ToString();
        else
        {
            textMeshStage.text = ((curstagenum + 1) % 100).ToString("00");
        }
        textMeshBig.text = combinedDisplayString.Substring(curstagenum, 1);
        inputTextMesh.text = "";
    }
    bool isShowingInputs = false;
    private IEnumerator ShowInputs()// Animate the number of inputs required for this module. "Showoff."
    {
        isShowingInputs = true;
        int stagesCounted = 0;
        while (inputTextMesh.text.Length < 31 && stagesCounted < totalstages)
        {
            inputTextMesh.text += "-";
            if ((inputTextMesh.text.Length + 1) % 16 == 0)
            {
                inputTextMesh.text += "\n";
            }
            else
            if ((inputTextMesh.text.Length + 1) % 4 == 0)
            {
                inputTextMesh.text += " ";
            }
            stagesCounted++;
            yield return new WaitForSeconds(0.05f);
        }
        isShowingInputs = false;
        yield return null;
    }
    private int offsetStageCnt = 0;
    private void ShowCurrentInput()
    {
        //
        // Referenced Display:
        // ### ### ### ###\n
        // ### ### ### ###
        //
        for (int i = 0; i < leds.Length; i++)
        {
            leds[i].material = statusLEDClr[1];
        }
        idxlit = -1;
        textMeshBig.text = "";
        textMeshStage.text = "--";
        if (correctinputs - offsetStageCnt > 24 && offsetStageCnt + 12 < combinedDisplayString.Length)
        {
            offsetStageCnt += 12;
        }
        string prev = combinedDisplayString.Substring(offsetStageCnt, Mathf.Min(combinedDisplayString.Length-offsetStageCnt,24));
        string toInputDisplay = "";
        for (int x = 0; x < Mathf.Min(combinedDisplayString.Length - offsetStageCnt, 24); x++)
        {

            if (offsetStageCnt + x < correctinputs)
            {
                toInputDisplay += prev[x];
            }
            else
            {
                toInputDisplay += "-";
            }
            if ((x + 1) % 12 == 0)
            {
                toInputDisplay += "\n";
            }
            else if ((x + 1) % 3 == 0)
            {
                toInputDisplay += " ";
            }
        }

        inputTextMesh.text = toInputDisplay;
    }
    private IEnumerator ActivateSolveAnim()
    {
        while (inputTextMesh.text.Length > 0)
        {
            string toOut = inputTextMesh.text;
            inputTextMesh.text = toOut.Substring(0, toOut.Length - 1).Trim();
            yield return new WaitForSeconds(0.05f);
        }
        while (textMeshStage.text.Length > 0)
        {
            string toOut = textMeshStage.text;
            textMeshStage.text = toOut.Substring(0, toOut.Length - 1).Trim();
            yield return new WaitForSeconds(1f);
        }
        yield return null;
    }
    private IEnumerator ActivateFinalePhase()// Animate the finale phase for this module.
    {
        canStart = false;

        string lastText = textMeshStage.text;
        if (lastText.Length == 1)
        {
            for (int i = 0; i <= 50; i++)
            {
                textMeshStage.text = Random.Range(0, 10).ToString();
                textMeshBig.text = Random.Range(0, 10).ToString();
                textMeshBig.color = new Color(textMeshBig.color.r, textMeshBig.color.g, textMeshBig.color.b,(float)(50-i)/50);
                yield return null;
            }
            textMeshBig.text = "";
            IEnumerator handleRandomizingScreen = ShowInputs();
            StartCoroutine(handleRandomizingScreen);
            while (isShowingInputs)
            {
                textMeshStage.text = "-" + Random.Range(0, 10).ToString();
                yield return null;
            }
            textMeshStage.text = "--";
        }
        else
        {
            for (int i = 0; i < 100; i++)
            {
                textMeshStage.text = Random.Range(0, 10).ToString()+ Random.Range(0, 10).ToString();
                textMeshBig.text = Random.Range(0, 10).ToString();
                textMeshBig.color = new Color(textMeshBig.color.r, textMeshBig.color.g, textMeshBig.color.b, (float)(99 - i) / 99);
                yield return null;
            }
            textMeshBig.text = "";
            StartCoroutine(ShowInputs());
            for (int i = 0; i < 100; i++)
            {
                textMeshStage.text = "-" + Random.Range(0, 10).ToString();
                yield return null;
            }
            textMeshStage.text = "--";
        }
        curstagenum++;
        yield return null;
        canStart = true;
    }

    private float cooldown = 0;
    // Update is called once per frame
    void Update()
    {
        if (canStart)
        {
            if (cooldown > 0)
                cooldown -= Time.deltaTime;
            else
            {
                int solvecount = bombInfo.GetSolvedModuleNames().Where(a => !ignoredModules.Contains(a)).ToList().Count;
                if (solvecount > curstagenum)
                {
                    curstagenum++;
                    if (curstagenum >= totalstages)
                    {
                        if (bombInfo.GetTime() > Mathf.Max(totalstages * 5 / 2, 120))
                        {
                            StartCoroutine(ActivateFinalePhase());
                        }
                        else
                        {
                            ShowCurrentInput();
                        }
                    }
                    else
                    { 
                        AdvanceStage();
                    }
                    cooldown = 1.4f;
                }
            }
        }
    }
    // Start Twitch Plays Handler

    IEnumerator HandleForceSolve()
    {
        for (int i = correctinputs; i < combinedDisplayString.Length; i++)
        {
            int d = int.Parse(combinedDisplayString.Substring(i, 1));
            digitSelectables[d].OnInteract();
            yield return new WaitForSeconds(0);
        }
        yield return null;
    }
    #pragma warning disable 0414
        string TwitchHelpMessage = "Enter the Forget It Not sequence with \"!{0} press 531820...\" or \"!{0} submit 531820...\". The sequence length depends on how many stages were shown on the module. You may use spaces and commas in the digit sequence.";
        bool TwitchShouldCancelCommand;
    #pragma warning restore 0414

    public void TwitchHandleForcedSolve() {
        Debug.LogFormat("[Forget It Not #{0}] A force solve has been issued viva TP handler.",curModID);
        curstagenum = totalstages;
        ShowCurrentInput();
        StartCoroutine(HandleForceSolve());
    }
    private bool hasAced = true;
    public IEnumerator ProcessTwitchCommand(string cmd)
    {
        List<int> digits = new List<int>();
        List<string> cmdlist = cmd.Split(' ', ',').ToList();
        int lastcorrectDigits = correctinputs;
        if (!(cmdlist[0].EqualsIgnoreCase("press") || cmdlist[0].EqualsIgnoreCase("submit")))// Is the starting command valid?
        {
            yield return "sendtochaterror Your command is invalid. The command must start with \"press\" or \"submit\" followed by a string of digits.";
            yield break;
        }
        cmdlist.RemoveAt(0);
        foreach (string dgtcmd in cmdlist)// Check for each portion of the command in the string.
        {
            char[] chrcmd = dgtcmd.ToCharArray();
            for (int i = 0; i < chrcmd.Length; i++)
            {
                    string singlecmd = chrcmd[i].ToString();
                    if (singlecmd.RegexMatch(@"[0-9]"))
                    {
                        digits.Add(int.Parse(singlecmd));
                    }
                    else
                    {
                        yield return "sendtochaterror Your command is invalid. The character \""+ chrcmd[i] + "\" is invalid.";
                        yield break;
                    }
            }
        }

        if (!digits.Any()) // Operates the same as (digits.Count <= 0)
        {
            yield break;
        }
        if (digits.Count + correctinputs > totalstages)
        {
            yield return "sendtochaterror Your command has too many digits. Please reinput the command with fewer digits.";
            yield break;
        }
        yield return null;
        if (curstagenum < totalstages)
        {
            //yield return "Forget It Not"; // Suggestively unnecessary 
            yield return "sendtochat Too early. Don't try to press a digit until this module is ready for input.";
            
            digitSelectables[digits[0]].OnInteract();
            yield break;
        }
        // Assign Mode for inputting
        string mode = "DEFAULT";
        if (bombInfo.GetTime() > Mathf.Max(totalstages * 5 / 2, 120))
            switch (Random.Range(0, 2))
            {
                case 0:
                    mode = "PATIENT";
                    yield return "waiting music";
                    break;
                default:
                    break;
            }
        else
        {
            mode = "PANIC";
        }
        //yield return "Forget It Not"; // Suggestively unnecessary 
        yield return "multiple strikes";
        yield return mode.EqualsIgnoreCase("PANIC") ? "sendtochat panicBasket Got to get this out now!" : bombInfo.GetSolvableModuleNames().Count - 1 == bombInfo.GetSolvedModuleNames().Count ? "sendtochat Let's finish this!" : "sendtochat This better be it!";
        int patientDigitsLeft = Random.Range(Mathf.Min(totalstages / 50, 1), 6);
        foreach (int d in digits)
        {
            if (!mode.EqualsIgnoreCase("PANIC") && TwitchShouldCancelCommand)
            {
                mode = "PANIC";
                yield return "end waiting music";
                yield return "sendtochat I'm hurrying already!";
            }
            else if (mode.EqualsIgnoreCase("PATIENT"))
            {
                patientDigitsLeft = Mathf.Max(0, patientDigitsLeft - 1);
                if (patientDigitsLeft <= 0)
                {
                    mode = "NORMAL";
                    yield return "end waiting music";
                }
            }
            yield return null;
            digitSelectables[d].OnInteract();
            if (idxlit != -1)
            {
                hasAced = false;
                if (correctinputs - lastcorrectDigits <= 5)
                {
                    yield return "sendtochat DansGame Oh come on!";
                }
                else if (correctinputs * 10 >= 9 * totalstages)
                {
                    yield return "sendtochat DansGame The end is right there!! Why!?";
                }
                else
                {
                    yield return "sendtochat DansGame I'm not done yet!?";
                }
                yield return "sendtochat A total of " + correctinputs + " digit(s) were entered correctly when the strike occured.";
                break;
            }
            if (correctinputs >= totalstages)
            {
                if (hasAced)
                {
                    if (totalstages >= 30)
                    {
                        if (bombInfo.GetSolvableModuleNames().Count == bombInfo.GetSolvedModuleNames().Count)
                        {
                            yield return "sendtochat PogChamp PraiseIt And that's how it's done!";
                        }
                        else
                        {
                            yield return "sendtochat PogChamp Aced it!";
                        }
                    }
                    else
                    {
                        yield return "sendtochat Kreygasm Too easy.";
                    }
                }
                else
                {
                    if (bombInfo.GetSolvableModuleNames().Count == bombInfo.GetSolvedModuleNames().Count)
                    {
                        yield return "sendtochat Kreygasm PraiseIt We're done!";
                    }
                    else
                    {
                        yield return "sendtochat Kreygasm It's done!";
                    }
                }
                break;
            }
            yield return new WaitForSeconds(mode.EqualsIgnoreCase("PANIC") || TwitchShouldCancelCommand ? 0f : mode.EqualsIgnoreCase("PATIENT") && patientDigitsLeft > 0 ? 1f : 0.1f);
        }
        yield return "end multiple strikes";
        yield return "end waiting music";
        yield break;
    }
}
