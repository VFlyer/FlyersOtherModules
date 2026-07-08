using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class ButtonMemoryScript : MonoBehaviour {

	public KMBombModule modSelf;
	public KMBombInfo bombInfo;
	public KMAudio mAudio;
	public KMSelectable[] btnSelectables;
	public KMRuleSeedable ruleSeed;
	public MeshRenderer[] btnRenderers, statusRenderers;
	public TextMesh[] btnTexts;
	public TextMesh displayText;
	public MeshRenderer displayRenderer;
	public Transform[] btnTransforms;

	static int modIDCnt;
	int moduleID;
	
	static readonly string[] labels = new[] { "ABORT", "DETONATE", "HOLD", "PRESS", },
		colorNames = new[] { "Red", "Yellow", "Blue", "White" };
	public Color[] possibleColorsBtns, possibleColorsLEDs;
	bool[][] instructionIsHold;
	string[][] encodedInstructions;
	int[] lastDigitBtnExp;

	int idxButtonHeld = -1, stagesCompleted;
	float timeHeld = 0f;
	bool moduleSolved, confirmHold, interactable, heldInteractable = false, allStageGen;

	const string chrProps = "PLC", digits = "1234567890";

	List<BtnMemStage> allStages = new List<BtnMemStage>();

	List<Vector3> storedBtnInitPos;
	FlyersOtherSettings otherSettings;

    #region Souvenir Support
	public string[] GetAllLEDColorsPerStage()
    {
		return allStages.Select(a => a.isHold ? colorNames[a.idxHoldLEDColor] : "NOHOLD").ToArray();
    }
	public string[] GetAllDisplaysPerStage()
    {
		return allStages.Select(a => labels[a.idxDisplay]).ToArray();
    }
    #endregion


    void QuickLog(string toLog, params object[] args)
    {
		Debug.LogFormat("[{0} #{1}] {2}", modSelf.ModuleDisplayName, moduleID, string.Format(toLog, args));
    }
	void QuickLogDebug(string toLog, params object[] args)
    {
		Debug.LogFormat("<{0} #{1}> {2}", modSelf.ModuleDisplayName, moduleID, string.Format(toLog, args));
    }
	void HandleRuleSeed()
    {
		MonoRandom rsHandler = ruleSeed == null ? new MonoRandom(1) : ruleSeed.GetRNG();
		if (ruleSeed == null)
			QuickLog("Rule seed handler does not exist. Using default instructions.");
		else
			QuickLog("Using rule seed {0} to generate instructions.", rsHandler.Seed);
		if (rsHandler.Seed == 1)
        {
			lastDigitBtnExp = new int[] { 6, 3, 8, 4 }; // Red, Yellow, Blue, White/Other
			instructionIsHold = new bool[][] {
				new[] { true, false, false, true }, // Ordered with displays ABORT, DETONATE, HOLD, PRESS in that order.
				new[] { false, true, false, true },
				new[] { false, false, true, true },
				new[] { true, true, false, false },
				new[] { false, true, true, false },
			};
			encodedInstructions = new string[][] {
				new[] { "P4", "C3", "P1", "P2", }, // Ordered with displays ABORT, DETONATE, HOLD, PRESS in that order.
				new[] { "S1C", "S1L", "S1P", "C3", },
				new[] { "S2L", "S1P", "S2C", "S1L", },
				new[] { "S2C", "S1P", "S1L", "S3P", },
				new[] { "S4C", "S3C", "S1L", "S2L", },
			};
			return;
        }
		instructionIsHold = new bool[5][];
		encodedInstructions = new string[5][];
		var allPossibleDigits = Enumerable.Range(0, 10).ToArray();
		lastDigitBtnExp = rsHandler.ShuffleFisherYates(allPossibleDigits).Take(4).ToArray();
		var allEncodedInstructions = new[] {
			"P1", "P2", "P3", "P4", "L1", "L2", "L3", "L4", "C1", "C2", "C3", "C4",
			"S1P", "S1L", "S1C", "S2P", "S2L", "S2C", "S3P", "S3L", "S3C", "S4P", "S4L", "S4C",
		};
		for (var x = 0; x < 5; x++)
        {
			var possibleInstructions =  rsHandler.ShuffleFisherYates(allEncodedInstructions.Take(12 + 3 * x).ToArray());
			if (x > 2)
				possibleInstructions = possibleInstructions.Concat(possibleInstructions.Skip(12)).ToArray();
			if (x == 4)
				possibleInstructions = possibleInstructions.Skip(12).ToArray();
			encodedInstructions[x] = possibleInstructions.Take(4).ToArray();
			instructionIsHold[x] = rsHandler.ShuffleFisherYates(Enumerable.Range(0, 4).Select(a => a % 2 == 1).ToArray());
        }
    }

	void GenerateNewStage(bool allAtOnce = false)
    {
		for (var x = 0; x < (allAtOnce ? 5 : 1); x++)
		{
			var y = x;
			var newStage = new BtnMemStage();
			newStage.idxDisplay = Random.Range(0, 4);
			newStage.btnColorIdx = Enumerable.Range(0, 4).ToArray().Shuffle();
			newStage.btnLabelIdx = Enumerable.Range(0, 4).ToArray().Shuffle();
			newStage.idxHoldLEDColor = Random.Range(0, 4);
			#region RuleProcessing
			var curInstruction = encodedInstructions[allAtOnce ? y : stagesCompleted][newStage.idxDisplay];
			QuickLogDebug("Stage {0}, Instruction to process: {1}", (allAtOnce ? y : stagesCompleted) + 1, curInstruction);
			newStage.isHold = instructionIsHold[allAtOnce ? y : stagesCompleted][newStage.idxDisplay];
			var curInstTemp = curInstruction.Select(b =>
			digits.Contains(b) ? '#' :
			chrProps.Contains(b) ? 'D' : b).Join("");
			// Encode the instruction as a template instruction for the set underneath.
			switch (curInstTemp)
			{
				case "D#": // Characteristic, Value
					{
						var idxVal = digits.IndexOf(curInstruction[1]);
						var idxTablePosExp = new[] {
						idxVal, // Position
						newStage.btnLabelIdx.IndexOf(a => a == idxVal), // Label
						newStage.btnColorIdx.IndexOf(a => a == idxVal) // Color
					};
						newStage.idxBtnExpected = idxTablePosExp[chrProps.IndexOf(a => a == curInstruction[0])];
					}
					break;
				case "S#D": // Stage (Value), Characteristic
					{
						var idxVal = digits.IndexOf(curInstruction[1]);
						if (idxVal >= (allAtOnce ? y : stagesCompleted) || idxVal == -1) break; // Don't check if the stage does not exist.
						var usedStage = allStages[idxVal];
						switch (curInstruction[2])
						{
							case 'C': // Color
								var idxColorUsed = usedStage.btnColorIdx[usedStage.idxBtnExpected];
								newStage.idxBtnExpected = newStage.btnColorIdx.IndexOf(a => a == idxColorUsed);
								break;
							case 'L': // Label
								var idxLabelUsed = usedStage.btnLabelIdx[usedStage.idxBtnExpected];
								newStage.idxBtnExpected = newStage.btnLabelIdx.IndexOf(a => a == idxLabelUsed);
								break;
							case 'P': // Position
								newStage.idxBtnExpected = usedStage.idxBtnExpected;
								break;
						}


					}
					break;
			}
			#endregion
			allStages.Add(newStage);
			#region LogNewStage
			QuickLog("-------------- Stage {0} --------------", (allAtOnce ? y : stagesCompleted) + 1);
			QuickLog("Labels displayed from left to right: {0}", newStage.btnLabelIdx.Select(a => labels[a]).Join(", "));
			QuickLog("Colors displayed from left to right: {0}", newStage.btnColorIdx.Select(a => colorNames[a]).Join(", "));
			QuickLog("Display: {0}", labels[newStage.idxDisplay]);

			var expectedIdx = newStage.idxBtnExpected;
			QuickLog("Expected action: {0} the {3} {2} button in position {1}.", newStage.isHold ? "Hold" : "Tap",
				expectedIdx + 1,
				labels[newStage.btnLabelIdx[expectedIdx]],
				colorNames[newStage.btnColorIdx[expectedIdx]]);
			#endregion
		}
    }

    // Use this for initialization
    void Start () {
		storedBtnInitPos = btnTransforms.Select(a => a.localPosition).ToList();

        try
        {
			var universalSettings = new ModConfig<FlyersOtherSettings>("FlyersOtherSettings");
			otherSettings = universalSettings.Settings;
			universalSettings.Settings = otherSettings;
			allStageGen = !otherSettings.DynamicStageGen;
        }
		catch
        {
			allStageGen = true;
        }
		moduleID = ++modIDCnt;
		HandleRuleSeed();
        for (var x = 0; x < btnSelectables.Length; x++)
        {
			var y = x;
			btnSelectables[x].OnInteract += delegate {
				heldInteractable = interactable;
				if (interactable)
				{
					confirmHold = false;
					timeHeld = 0f;
					mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, btnSelectables[y].transform);
					if (idxButtonHeld == -1)
					{
						idxButtonHeld = y;
						UpdateButtonsTransforms();
					}
				}
				return false;
			};
			btnSelectables[x].OnInteractEnded += HandleButtonRelease;
        }
		modSelf.OnActivate += delegate {
			GenerateNewStage(allStageGen);
			StartCoroutine(HandleRevealStage());
		};
		displayText.text = "";
		for (var x = 0; x < btnTexts.Length; x++)
			btnTexts[x].text = "";
		for (var x = 0; x < btnTransforms.Length; x++)
			btnTransforms[x].localPosition = storedBtnInitPos[x] + Vector3.down * 0.02f;
	}

	void UpdateButtonsTransforms()
    {
		for (var x = 0; x < btnTransforms.Length; x++)
			btnTransforms[x].localPosition = storedBtnInitPos[x] + (x == idxButtonHeld ? Vector3.down * 0.01f : Vector3.zero);
	}

	void UpdateStageLEDs()
    {
		var curStage = allStages.ElementAtOrDefault(stagesCompleted);
		for (var x = 0; x < statusRenderers.Length; x++)
			statusRenderers[x].material.color = x < stagesCompleted ? possibleColorsLEDs[4] : x == stagesCompleted && confirmHold && idxButtonHeld != -1 ? possibleColorsLEDs[curStage.idxHoldLEDColor] :  possibleColorsLEDs[5];
    }

	void HandleButtonRelease()
    {
		if (!interactable || !heldInteractable) return;
		mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonRelease, btnSelectables[idxButtonHeld].transform);
		var storedIdxBtnHeld = idxButtonHeld;
		idxButtonHeld = -1;
		UpdateButtonsTransforms();
		if (moduleSolved) return;
		var curStage = allStages[stagesCompleted];
		var correctButton = storedIdxBtnHeld == curStage.idxBtnExpected; // Start with checking if the button interacted is the right button.
		var correctAction = curStage.isHold ?
			confirmHold && (int)(bombInfo.GetTime() % 10) == lastDigitBtnExp[curStage.idxHoldLEDColor]
			: !confirmHold; // If the button needs to be held, check if it's held and released correctly.
		if (correctAction && correctButton)
        {
			stagesCompleted++;
			if (stagesCompleted >= 5)
            {
				moduleSolved = true;
				modSelf.HandlePass();
            }
			else if (allStages.Count < 5)
				GenerateNewStage();
        }
		else
        {
			QuickLog("{0} the {3} button in position {1}, label {2}{4}.", confirmHold ? "Held" : "Tapped",
			storedIdxBtnHeld + 1,
			labels[curStage.btnLabelIdx[storedIdxBtnHeld]],
			colorNames[curStage.btnColorIdx[storedIdxBtnHeld]],
			allStageGen ? string.Format(" on stage {0}", stagesCompleted + 1) : "");

			if (confirmHold)
				QuickLog("Button released when the last seconds digit was {0}", (int)(bombInfo.GetTime() % 10));
			stagesCompleted = 0;
			modSelf.HandleStrike();
			allStages.Clear();
			GenerateNewStage(allStageGen);
        }
		interactable = false;
		UpdateStageLEDs();
		StartCoroutine(HandleHideStage(!moduleSolved));
	}
	IEnumerator HandleHideStage(bool callRevealStage = false)
	{
		displayText.text = "";
		for (float t = 0; t < 1.3f; t += Time.deltaTime)
		{
			for (var x = 0; x < btnTransforms.Length; x++)
			{
				var offset = 0.1f * x;
				btnTransforms[x].localPosition = storedBtnInitPos[x] + Vector3.Slerp(Vector3.zero, Vector3.down * 0.02f, t - offset);
			}
			yield return null;
		}
		for (var x = 0; x < btnTransforms.Length; x++)
			btnTransforms[x].localPosition = storedBtnInitPos[x] + Vector3.down * 0.02f;
		if (callRevealStage)
			yield return HandleRevealStage();
		yield break;
	}
	IEnumerator HandleRevealStage()
    {
		var curStage = allStages.ElementAtOrDefault(stagesCompleted);
		if (curStage != null)
        {
			for (var x = 0; x < btnRenderers.Length; x++)
				btnRenderers[x].material.color = possibleColorsBtns[curStage.btnColorIdx[x]];
			for (var x = 0; x < btnTexts.Length; x++)
				btnTexts[x].text = labels[curStage.btnLabelIdx[x]];
			displayText.text = labels[curStage.idxDisplay];
		}

        for (float t = 0; t < 1.3f; t += Time.deltaTime)
        {
			for (var x = 0; x < btnTransforms.Length; x++)
			{
				var offset = 0.1f * x;
				btnTransforms[x].localPosition = storedBtnInitPos[x] + Vector3.Slerp(Vector3.zero, Vector3.down * 0.02f, 1f - (t - offset));
			}
			yield return null;
		}
		for (var x = 0; x < btnTransforms.Length; x++)
			btnTransforms[x].localPosition = storedBtnInitPos[x];
		interactable = true;
		yield break;
    }

	// Update is called once per frame
	void Update() {
		if (idxButtonHeld != -1)
        {
			timeHeld += Time.deltaTime;
			if (timeHeld >= 0.5f && !confirmHold)
            {
				confirmHold = true;
				var curStage = allStages.ElementAtOrDefault(stagesCompleted);
				QuickLog("Holding the button{2} emitted a {0} LED. Release the button when the last seconds digit of the countdown timer is {1}.",
					colorNames[curStage.idxHoldLEDColor],
					lastDigitBtnExp[curStage.idxHoldLEDColor],
					allStageGen ? string.Format(" on stage {0}", stagesCompleted + 1) : "");
				UpdateStageLEDs();
            }
			else if (confirmHold)
            {
				var curStage = allStages.ElementAtOrDefault(stagesCompleted);
				var timeAnim = (Mathf.Sin(timeHeld - 0.5f) + 1) / 8f;
				statusRenderers[stagesCompleted].material.color = Color.Lerp(possibleColorsLEDs[curStage.idxHoldLEDColor], Color.white, timeAnim);
			}
        }
	}
#pragma warning disable IDE0051 // Remove unused private members
	readonly string TwitchHelpMessage = "\"!{0} hold/tap #\" [Holds/Taps the button in the #th position; positions numbered 1-4 from left to right.] | \"!{0} release #\" [Releases the button when the last seconds digit is #]";
#pragma warning restore IDE0051 // Remove unused private members
	IEnumerator ProcessTwitchCommand(string cmd)
    {
		if (!interactable)
        {
			yield return "sendtochaterror The module is not accepting inputs right now. Wait a bit until the module allows inputs.";
			yield break;
		}

		if (Regex.IsMatch(cmd, @"^release\s[0-9]$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
		{
			var lastPart = cmd.Split().Last();
			int lastDigitCmd;
			if (!int.TryParse(lastPart, out lastDigitCmd))
			{
				yield return string.Format("sendtochaterror The specified digit \"{0}\" is not valid!", lastPart);
				yield break;
			}
			else if (idxButtonHeld == -1)
			{
				yield return "sendtochaterror You are not holding a button right now! Specify a button to hold first!";
				yield break;
			}
			yield return null;
			while ((int)(bombInfo.GetTime() % 10) != lastDigitCmd)
				yield return "trycancel Button release command has been canceled!";
			btnSelectables[idxButtonHeld].OnInteractEnded();
		}
		else if (Regex.IsMatch(cmd, @"^(tap|hold)\s[1-4]$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
		{
			var lastPart = cmd.Split().Last();
			var firstPart = cmd.Split().First().ToLowerInvariant();
			var idxBtn = digits.IndexOf(lastPart);
			if (idxBtn == -1)
            {
				yield return string.Format("sendtochaterror The specified button \"{0}\" is not a valid button!", lastPart);
				yield break;
			}
			if (idxButtonHeld != -1)
			{
				yield return "sendtochaterror You are holding a button right now! Release the button first before specifing a new button!";
				yield break;
			}
			yield return null;
			btnSelectables[idxBtn].OnInteract();
            if (firstPart == "hold")
                while (!confirmHold)
                    yield return null;
            else
                btnSelectables[idxBtn].OnInteractEnded();
        }
    }

	IEnumerator TwitchHandleForcedSolve()	
    {
		while (!moduleSolved)
        {
			while (!interactable)
				yield return true;
			var curStage = allStages[stagesCompleted];
			btnSelectables[curStage.idxBtnExpected].OnInteract();
			if (curStage.isHold)
            {
				while (!confirmHold)
					yield return true;
				var lsdExpected = lastDigitBtnExp[curStage.idxHoldLEDColor];
				while ((int)(bombInfo.GetTime() % 10) != lsdExpected)
					yield return true;
			}
			btnSelectables[curStage.idxBtnExpected].OnInteractEnded();
			yield return new WaitForSeconds(0.1f);
        }
    }
}
