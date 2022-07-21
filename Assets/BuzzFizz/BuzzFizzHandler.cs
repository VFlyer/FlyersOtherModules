using KModkit;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuzzFizzHandler : MonoBehaviour {

    public GameObject dial;
    public KMSelectable dialSelect;
    public TextMesh textnumber;
    public KMNeedyModule needyModule;

    private int targetnumber;
    private int goalVal = 0;

    private int rotatePos = 0;

    private readonly int[] poslist = new int[] { 0, 1, 3, 2 };
    private readonly string[] conText = new string[] { "Number", "Buzz", "Fizz", "BuzzFizz" };

    private static int modid = 1; // Changable Mod ID
    private int cmodID; // current mod ID
    private bool iswarning = false, forceDisable = false;
    void Awake()
    {
        cmodID = modid++;
    }
	// Use this for initialization
	void Start () {
        textnumber.text = "Not FizzBuzz.";

        needyModule.OnNeedyActivation += delegate ()
        {
            if (forceDisable)
            {
                needyModule.HandlePass();
                return;
            }

            targetnumber = Random.Range(0, int.MaxValue);
            textnumber.text = targetnumber.ToString();

            goalVal += targetnumber % 3 == 0 ? 1 : 0;
            goalVal += targetnumber % 5 == 0 ? 2 : 0;
        };

        needyModule.OnTimerExpired += delegate ()
        {
            if (poslist[rotatePos % poslist.Length] != goalVal)
            {
                needyModule.HandleStrike();
                Debug.LogFormat("[BuzzFizz #{0}]: \"{2}\" was expected but \"{3}\" was set for {1}.", cmodID, targetnumber, conText[goalVal], conText[poslist[rotatePos % 4]]);
            }
            goalVal = 0;
            iswarning = false;
        };
        needyModule.OnNeedyDeactivation += delegate ()
        {
            textnumber.color = Color.white;
            iswarning = false;
        };
        needyModule.OnActivate += delegate ()
        {
            textnumber.text = "Pending...";
            textnumber.color = Color.white;
            Debug.LogFormat("[BuzzFizz #{0}]: This module will only log incorrect answers upon deactivation. All correct answers will NOT be logged.", cmodID);
        };
        dialSelect.OnInteract += delegate ()
        {
            dial.transform.Rotate(new Vector3(0, 90, 0));
            rotatePos = (rotatePos + 1) % 4;
            return false;
        };
	}



	// Update is called once per frame
	void Update () {
        float timeLeft = needyModule.GetNeedyTimeRemaining();
        if (timeLeft >= 0 && timeLeft < 5 && !iswarning)
        {
            StartCoroutine(WarnFlash());
        }

	}

    IEnumerator WarnFlash()
    {
        iswarning = true;
        while (needyModule.GetNeedyTimeRemaining() > 0)
        {
            textnumber.color = Color.white;
            yield return new WaitForSeconds(0.25f);
            textnumber.color = Color.red;
            yield return new WaitForSeconds(0.25f);
        }
        textnumber.color = Color.white;
        textnumber.text = "Pending...";
        yield return null;
    }

    void TwitchHandleForcedSolve()
    {
        Debug.LogFormat("[BuzzFizz #{0}]: Forcably disabling the module viva TP Handler.", cmodID);
        forceDisable = true;
        needyModule.ResetDelayMin = float.MaxValue;
        needyModule.ResetDelayMax = float.MaxValue;
        //needyModule.SetResetDelayTime(float.MaxValue, float.MaxValue);
        needyModule.HandlePass();
    }

    public readonly string TwitchHelpMessage = "To turn the dial a specific number of times, do \"!{0} turn/rotate #\". The dial turns # % 4 times based on the command inputted. (4n turns will turn the dial 4 times.)";
    KMSelectable[] ProcessTwitchCommand(string input)
    {

        string locinput = input.ToLower();
        string[] interepetedCommand = input.Split(' ');
        if (locinput.RegexMatch(@"^(turn|rotate) \d+$"))
        {
            List<KMSelectable> output = new List<KMSelectable>();
            int tctemp = int.Parse(interepetedCommand[1]) % 4;
            int turnCount = tctemp == 0 ? 4 : tctemp;
            for (int x = 0; x < turnCount; x++)
                output.Add(dialSelect);
            return output.ToArray();
        }
        return null;
    }
}
