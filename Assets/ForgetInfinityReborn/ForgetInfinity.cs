using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Text;
using System;

public class ForgetInfinity : MonoBehaviour {
	public KMSelectable[] Buttons;
	public KMSelectable SubmitButton;
	public KMSelectable ResetButton;
	public KMBombModule Module;
	public KMBombInfo Info;
	public TextMesh Screen;

	public static string[] ignoredModules = null;

	public int[] code = {0, 0, 0, 0, 0};
	private int codeIndex = 0;

	private bool solved = false;
	private bool bossMode = false;
	private bool canForget = false;

	public List<List<int>> stages = new List<List<int>>();
    public List<List<int>> solution = new List<List<int>>();
    private int stagePtr = 0;
	private int solveStagePtr = 0;
	private bool firstSolve = true;

    private bool containsFI = false;
    private int batteryCount = 0;

    private bool solveMode = false;
    public int delay = 0;

	private int lastThing = 0;

	// Use this for initialization
	void Awake() {
		if (ignoredModules == null)
			ignoredModules = GetComponent<KMBossModule>().GetIgnoredModules("Forget Infinity", new string[]{
                "Cookie Jars",
                "Cruel Purgatory",
                "Divided Squares",
                "Forget Enigma",
                "Forget Everything",
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
                "The Swan",
                "Tallordered Keys",
                "The Time Keeper",
                "Timing is Everything",
                "The Troll",
                "Turn The Key",
                "Übermodule",
                "The Very Annoying Button",
                "Forget Infinity" 

			});
        //"Forget" Modules excluding Forget Me Now: Requires this module to be solved without Boss Module Manager
        //Time Keeper, Timing is Everything, Turn The Key: Bomb Timer Sensitive that can stall bombs
        //Forget Infinity: DON'T HANG BOMBS WITH DUPLICATES OF THIS

	}

	List<int> GenerateRandom() {
		var t = new List<int>();
		for (int i = 0; i < 5; i++) {
			t.Add (UnityEngine.Random.Range(1, 6));
		}
		return t;
	}

	void Start () {
        var serial = KMBombInfoExtensions.KMBI.GetSerialNumber(Info);
        Module.OnActivate += delegate {
			BeginForgetting();
            containsFI = serial.Contains("F") || serial.Contains("I");
            batteryCount = KMBombInfoExtensions.KMBI.GetBatteryCount(Info);
        };
        for (int x = 0; x < Buttons.Count(); x++)
        {
            int there = x;
            Buttons[there].OnInteract += delegate
            {
                Buttons[there].AddInteractionPunch();
                Number(there + 1);
                return false;
            };
            

        }
		SubmitButton.OnInteract += delegate {
			Submit();
			return false;
		};
		ResetButton.OnInteract += delegate {
            ResetButton.AddInteractionPunch();
			Reset();
			return false;
		};
	}

	void BeginForgetting() {
		Debug.Log("[Forget Infinity] Module activated...! Let's forget!");
		canForget = true;
		updateScreen(new[]{ 0, 0, 0, 0, 0 });
	}
	
	// Update is called once per frame
	void Update () {
        if (solved||bossMode||!canForget)
            return;
        CheckForNewSolves();
	}

	string ListString(List<string> a) {
		var sb = new StringBuilder();
		foreach(var j in a) {
			sb.Append(j + " ");
		}
		return sb.ToString();
	}

	string ListString(List<int> a) {
		var sb = new StringBuilder();
		foreach(var j in a) {
			sb.Append(j.ToString() + " ");
		}
		return sb.ToString();
	}

	void NextStage() {
		Debug.Log("[Forget Infinity] advancing stage!");
		var rand = GenerateRandom();
		stages.Add(rand);
		stagePtr++;
		updateScreen(rand.ToArray());
		Debug.Log("[Forget Infinity] we are now on stage " + stagePtr.ToString());
		Debug.Log("[Forget Infinity] next stage is: " + ListString(rand));
	}

	void CheckForNewSolves() {
		var solvables = Info.GetSolvableModuleNames().Where(a => !ignoredModules.Contains (a));
		var list1 = Info.GetSolvedModuleNames().Where(a => solvables.Contains(a));
		if (list1.Count() >= solvables.Count() && !firstSolve) {
			Debug.Log("[Forget Infinity] All non-ignored solvables have been solved. activating boss mode.");
			bossMode = true;
			updateScreen(new[] { 0, 0, 0, 0, 0 });
			return;
		}
		if (list1.Count() != lastThing) {
			NextStage();
			firstSolve = false;
		}
		lastThing = list1.Count();
	}

    static int ConvertFromBase5(string number)
    {
        return number.Select(digit => (int)digit - 48).Aggregate(0, (x, y) => x * 5 + y);
    }

    void Submit() {
		SubmitButton.AddInteractionPunch();
        if (!bossMode) {
			Debug.Log("[Forget Infinity] boss mode not active. Strike! (submit button)");
			Module.HandleStrike();
			return;
		}
        if (solveMode)
        {
            Debug.Log("[Forget Infinity] Solve mode detected! not submitting!");
            solveMode = false;
            StringBuilder sb = new StringBuilder(); // base 5
            for (int i=0; i<5; i++)
            {
                var j = code[i];
                if (j > 0)
                {
                    j--;
                }
                sb.Append(j.ToString());
            }
            var dec = ConvertFromBase5(sb.ToString());
            Debug.Log("[Forget Infinity] input is " + sb.ToString() + ", in base 10 that's " + dec);
            if (stages.Count() > dec && dec >= 0)
            {
                updateScreen(stages[dec].ToArray());
            }
            Module.HandleStrike();
            Screen.color = new UnityEngine.Color(1, 1, 1);
            return;
        }
        List<int> stg = new List<int>(stages[solveStagePtr]);
        Debug.Log("not calculated: " + ListString(stg));
        if (batteryCount != 0)
        {
            Debug.Log("batteries " + batteryCount);
            for (int i=0; i<5; i++)
            {
                var t = stg[i];
                t = (t + batteryCount) % 5;
                if (t == 0)
                {
                    t = 5;
                }
                stg[i] = t;
            }
        }
        if (containsFI)
        {
            Debug.Log("Bomb contains letters, \"F/I\" in serial no.");
            for (int i=0; i<5; i++)
            {
                var t = stg[i];
                t = t - 1;
                if (t == 0)
                {
                    t = 5;
                }
                stg[i] = t;
            }
        }
		Debug.Log("calculated: " + ListString(stg));
		Debug.Log("solve stage ptr = " + solveStagePtr.ToString());
		Debug.Log("stage count = " + stages.Count());
		for (int i = 0; i < 5; i++) {
			if (code [i] != stg[i]) {
				Debug.Log("[Forget Infinity] Code is different from the expected input of " + ListString(stg) + ". Strike!");
				Module.HandleStrike();
				Reset(true);
                solveStagePtr = 0;
				return;
			}
		}
		if (stages.Count()-1 <= solveStagePtr) {
            Debug.Log("[Forget Infinity] All codes are correct! Solve!");
			solved = true;
			Module.HandlePass();
			Screen.text = "XXXXX";
			return;
		}
		solveStagePtr++;
		for (int i=0; i<5; i++) this.code[i] = 0;
		this.codeIndex = 0;
		updateScreen();
	}

	void updateScreen(int[] a) {
		string b = "";
		for (int i = 0; i < 5; i++) {
			b += a[i].ToString();
		}
		Screen.text = b;
	}

	void updateScreen() {
		updateScreen(code);
	}

	void Number(int button) {
		if (solved)
			return;
		if (!bossMode) {
			Debug.Log("[Forget Infinity] boss mode not active. Strike! (button "+button.ToString()+")");
			Module.HandleStrike();
			return;
		}
		Debug.Log ("[Forget Infinity] button " + button.ToString() + " pushed");
		if (this.codeIndex == this.code.Length)
			return;
		if (this.codeIndex < 5) {
			this.code[this.codeIndex++] = button;
		}
		updateScreen();
	}

	void Reset(bool fromSubmit) {
		if (solved)
			return;
		if (!bossMode) {
			Debug.Log("[Forget Infinity] boss mode not active. Strike! (reset button)");
			Module.HandleStrike();
			return;
		}
        if (solveMode && !fromSubmit)
        {
            Debug.Log("[Forget Infinity] reset pushed in solve mode, adding 0 to code");
            if (codeIndex == code.Length)
                return;
            if (codeIndex < 5)
            {
                code[codeIndex++] = 0;
            }
            updateScreen();
            return;
        }
        int codeDigits = 0;
        for (int i=0; i<5; i++)
        {
            if (code[i] == 0) codeDigits++;
        }
        if (codeDigits == 5 && !fromSubmit)
        {
            Debug.Log("[Forget Infinity] going into solve mode");
            solveMode = true;
            Screen.color = new UnityEngine.Color(0, 255, 0);
            return;
        }
        
		for (int i=0; i<5; i++) code[i] = 0;
		codeIndex = 0;
		updateScreen();
	}

    void Reset()
    {
        Reset(false);
    }

    // Twitch Plays support

    public readonly string TwitchHelpMessage = "Enter the sequence with \"!{0} press 1 2 3 4 5...\". Submit with \"!{0} submit\". Reset with \"!{0} reset\".";

    public KMSelectable[] ProcessTwitchCommand(string cmd)
    {
        if (solved)
            throw new System.FormatException("DansGame We're done!");
        if (!bossMode)
            throw new System.FormatException("A bit early, don't you think?");
        cmd = cmd.ToLowerInvariant();
        List<KMSelectable> l = new List<KMSelectable>();
        if (cmd.StartsWith("press "))
        {
            cmd = cmd.Substring(6);
            var split = cmd.Split(' ');
            foreach(var i in split)
            {
                int a = 0;
                var r = Int32.TryParse(i, out a);
                if (!r)
                {
                    throw new System.FormatException("That's not a number.");
                }
                if (a > 5 || a < 1)
                {
                    throw new System.FormatException("Out of bounds!");
                }
                l.Add(Buttons[a-1]);
            }
            if (l.Count() == 5)
            {
                l.Add(SubmitButton);
            }
            return l.ToArray();
        }
        else if (cmd.StartsWith("submit"))
        {
            return new KMSelectable[] { SubmitButton };
        }
        else if (cmd.StartsWith("reset"))
        {
            return new KMSelectable[] { ResetButton };
        }
        else
        {
            throw new System.FormatException("Use 'press' followed by some numbers, 'submit' or 'reset'.");
        }
    }
}
