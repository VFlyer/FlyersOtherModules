using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KModkit;

public class TimeAccumulationHandler : MonoBehaviour {


    public GameObject disableButton;
    public GameObject point1, point2;
    public KMSelectable selectDisable;
    public TextMesh textDisplay;
    public KMNeedyModule needyModule;
    public KMBombInfo bombInfo;
    public KMAudio audioKTANE;

    private int value = 0;
    private int solveCount = 0;
    private int strikeCount = 0;
    private bool localstrike = false;
    public bool canRun = false;
    public bool isLightsFirstOn = false;
    private bool forwardsAnim = false;
    private int animCount = 0;

    private static int modID = 1;
    private int localModID;
    private bool isInTimeMode = false;
    void Awake()
    {
        localModID = modID++;
    }
	// Use this for initialization
	void Start () {
        
        needyModule.OnTimerExpired += delegate () {
            if (canRun)
            {
                localstrike = true;
                Debug.LogFormat("[Time Accumulation #{0}]: Don't let this run out of time! Strike incurred.", localModID);
                needyModule.HandleStrike();
            }
        };
        selectDisable.OnInteract += delegate () {
            int curneedTime = Mathf.RoundToInt(needyModule.GetNeedyTimeRemaining());
            if (curneedTime >= 0)
            {
                value += curneedTime;
                needyModule.HandlePass();
                Debug.LogFormat("[Time Accumulation #{0}]: Adding {1} onto the counter. Counter currently at {2}.", localModID, curneedTime, value);
            }
            audioKTANE.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, transform);
            forwardsAnim = true;
            return false;
        };
        needyModule.OnNeedyActivation += delegate ()
        {
            if (!canRun)
            {
                needyModule.HandlePass();
            }
        };
        needyModule.OnActivate += delegate () {
            isInTimeMode = TimeModeActive;
            canRun = true;
            isLightsFirstOn = true;
            if (isInTimeMode)
            {
                Debug.LogFormat("[Time Accumulation #{0}]: Module detected in Time Mode, disabling strike count for this module...", localModID);
            }
        };
        selectDisable.OnInteractEnded += delegate ()
        {
            forwardsAnim = false;
            audioKTANE.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonRelease, transform);
        };
        bombInfo.OnBombExploded += delegate ()
        {
            canRun = false;
        };
        bombInfo.OnBombSolved += delegate ()
        {
            canRun = false;
        };
	}

    IEnumerator HandleFlashingAnim()
    {
        for (int i = 0; i < 10; i++)
        {
            textDisplay.color = Color.red;
            yield return new WaitForSeconds(0.2f);
            textDisplay.color = Color.white;
            yield return new WaitForSeconds(0.2f);
        }
        yield return null;
    }

    // Update is called once per frame
    void Update()
    {
        if (canRun)
        {
            int cursolcnt = bombInfo.GetSolvedModuleNames().Count;
            if (cursolcnt != solveCount)
            {
                var counted = 0;
                while (solveCount < cursolcnt)
                {
                    solveCount++;
                    value = Mathf.Max(0, value - 5);
                    counted++;
                }
                if (value > 0)
                {
                    Debug.LogFormat("[Time Accumulation #{0}]: {1} module(s) have solved, decreasing the counter by {3}. Counter logged at {2}.", localModID, counted, value, counted * 5);
                }
                if (cursolcnt == bombInfo.GetSolvableModuleNames().Count)
                    canRun = false;
            }
            if (!isInTimeMode)
            {
                int curstrcnt = bombInfo.GetStrikes();
                if (curstrcnt != strikeCount)
                {
                    if (localstrike)
                    {
                        localstrike = false;
                    }
                    else if (strikeCount < curstrcnt)
                    {
                        value += 25;
                        Debug.LogFormat("[Time Accumulation #{0}]: Added 25 due to increase in strike counter from an external module. Counter logged at {1}.", localModID, value);
                    }
                    strikeCount = curstrcnt;
                }
            }
            if (value > 99)
            {
                value = 0;
                localstrike = true;
                Debug.LogFormat("[Time Accumulation #{0}]: Too much! Counter resetted to 0 in exchange of a strike.", localModID);
                StartCoroutine(HandleFlashingAnim());
                needyModule.HandleStrike();
            }
            textDisplay.text = (value % 100).ToString("00");
        }
        else if (isLightsFirstOn)
        {
            textDisplay.text = UnityEngine.Random.Range(0, 100).ToString("00");
        }
        else
        {
            textDisplay.text = "";
        }
        if (forwardsAnim)
        {
            animCount = Mathf.Min(animCount + 1, 9);
        }
        else
        {
            animCount = Mathf.Max(animCount - 1, 0);
        }
        List<Vector3> posList = new List<Vector3>();
        for (int x = 0; x < 10; x++) 
        {
            if (x < animCount)
            {
                posList.Add(point2.transform.position);
            }
            else
            {
                posList.Add(point1.transform.position);
            }
        }
        Vector3 average = new Vector3();
        foreach (Vector3 pos in posList)
        {
            average += pos;
        }
        average /= posList.Count;
        disableButton.transform.position = average;
    }
    bool TimeModeActive;
    public readonly string TwitchHelpMessage = "Press the button at a given time by the commands \"!{0} press x#\" (1s digit of needy time remaining),\"!{0} press ##\" (At a very specific time remaining),\"!{0} press #x\" (10s digit of needy time remaining), or \"!{0} press\" (Any time.)";
    
    void TwitchHandleForcedSolve()
    {
        canRun = false;
        needyModule.SetResetDelayTime(float.PositiveInfinity, float.PositiveInfinity);
        needyModule.HandlePass();
    }
    
    IEnumerator ProcessTwitchCommand(string command)
    {
        string commandModified = command.ToLower();
        if (needyModule.GetNeedyTimeRemaining().Equals(-1f))
        {
            yield return "sendtochaterror The needy is not active yet. Please wait a bit until the needy reactivates.";
            yield break;
        }
        if (commandModified.RegexMatch(@"^press(\s(x\d|\dx|\d{2}))?$"))
        {
            string cmdShorten = command.Substring(5,command.Length-5).Trim();
            if (cmdShorten.Length == 2)
            {
                if (cmdShorten.RegexMatch(@"^\d{2}$"))
                {
                    yield return null;
                    int timeSpecified = int.Parse(cmdShorten);
                    if (timeSpecified > Mathf.RoundToInt(needyModule.GetNeedyTimeRemaining()))
                    {
                        yield return "sendtochaterror Sorry, but the specified time \"" + timeSpecified + "\" has already passed.";
                        yield break;
                    }
                    yield return "sendtochat About to press the button when the needy timer is at " + timeSpecified + ".";
                    do
                    {
                        yield return "trycancel Sorry but the specified command has been aborted.";
                    }
                    while (Mathf.RoundToInt(needyModule.GetNeedyTimeRemaining()) != timeSpecified && !needyModule.GetNeedyTimeRemaining().Equals(-1f));
                }
                else if (cmdShorten.RegexMatch(@"^\dx$"))
                {
                    yield return null;
                    int timeSpecified = int.Parse(cmdShorten.Substring(0, 1));
                    if (timeSpecified > Mathf.RoundToInt(needyModule.GetNeedyTimeRemaining()) / 10)
                    {
                        yield return "sendtochaterror Sorry, but the specified time is not possible.";
                        yield break;
                    }
                    yield return "sendtochat About to press the button when the needy timer is at X" + timeSpecified + ".";
                    do
                    {
                        yield return "trycancel Sorry but the specified command has been aborted.";
                    }
                    while (Mathf.RoundToInt(needyModule.GetNeedyTimeRemaining()) / 10 != timeSpecified && !needyModule.GetNeedyTimeRemaining().Equals(-1f));
                }
                else if (cmdShorten.RegexMatch(@"^x\d$"))
                {
                    yield return null;
                    int timeSpecified = int.Parse(cmdShorten.Substring(1, 1));
                    
                    if (timeSpecified > Mathf.RoundToInt(needyModule.GetNeedyTimeRemaining()) % 10 && Mathf.RoundToInt(needyModule.GetNeedyTimeRemaining()) < 10)
                    {
                        yield return "sendtochaterror Sorry, but the specified time is not possible.";
                        yield break;
                    }
                    yield return "sendtochat About to press the button when the needy timer is at "+timeSpecified+"X.";
                    do
                    {
                        yield return "trycancel Sorry but the specified command has been aborted.";
                    }
                    while (Mathf.RoundToInt(needyModule.GetNeedyTimeRemaining()) % 10 != timeSpecified && !needyModule.GetNeedyTimeRemaining().Equals(-1f));
                }
            }
            yield return "solve";
            yield return "strike";
            yield return selectDisable;
            yield return new WaitForSeconds(0.2f);
            yield return selectDisable;
            yield break;
        }
        yield break;
    }
}
