using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SevenHandler : MonoBehaviour {

	public KMBombModule modSelf;
	public KMAudio audioMod;
	public GameObject entireModule;
	public KMBombInfo info;
	public MeshRenderer[] segments, colorTriangles, colorTrianglesHL;
	public MeshRenderer LEDMesh;
	public KMSelectable[] segmentSelectables, colorTriangleSelectables;
	public KMSelectable LED, stageDisplay;
	public KMColorblindMode colorblindMode;
	private SevenSegmentCodings segmentCodings;
	public TextMesh stageIndc, colorblindIndc;
	public Material[] matSwitch = new Material[2];

	int[] finalValues = new int[3];

	List<int[]> displayedValues = new List<int[]>();
	List<int> idxOperations = new List<int>();

	readonly int[] segmentLogging = { 0, 5, 1, 6, 4, 2, 3 };
	readonly string colorList = "KRGYBMCW";

	int curSelectedColor = 0;
	int[] segmentsColored = new int[7], segmentsSolution = new int[7];

	string[] soundsPressNames = { "selectALT", "selectALT2" };
	static int modID;
	int curModID;

	// Detection and Logging
	bool zenDetected, timeDetected, hasStarted, isSubmitting, interactable = false, colorblinddetected;
	int curIdx = 0, curStrikeCount, localStrikes = 0;

	// Use this for initialization
	void Awake()
	{
		segmentCodings = new SevenSegmentCodings();
		curModID = ++modID;

		for (int x = 0; x < segmentSelectables.Length; x++)
		{
			int y = x;
			segmentSelectables[x].OnInteract += delegate {
				segmentSelectables[y].AddInteractionPunch();
				audioMod.PlaySoundAtTransform(soundsPressNames[Random.Range(0,soundsPressNames.Length)], transform);
				if (isSubmitting && interactable)
				{
					segmentsColored[y] = curSelectedColor;
					UpdateSegments(false);
				}
				return false;
			};
			segmentSelectables[x].OnHighlight += delegate {
				if (isSubmitting) return;
				//Debug.LogFormat("segment {0} Highlighted", y);
				Color segmentColor = segments[y].material.color;
				int idx = Mathf.RoundToInt(segmentColor.r)
				+ Mathf.RoundToInt(segmentColor.g) * 2
				+ Mathf.RoundToInt(segmentColor.b) * 4;
				if (idx >= 0 && idx < colorTrianglesHL.Length) {
					colorTrianglesHL[idx].enabled = true;
					colorTriangles[idx].material.color = new Color(idx % 2, idx / 2 % 2, idx / 4 % 2);
				}
			};
			segmentSelectables[x].OnHighlightEnded += delegate {
				if (isSubmitting) return;
				//Debug.LogFormat("segment {0} Dehighlighted", y);
				for (int z = 0; z < colorTrianglesHL.Length; z++)
				{
					colorTrianglesHL[z].enabled = false;
					colorTriangles[z].material.color = Color.black;
				}
			};
		}

		for (int x = 0; x < colorTriangleSelectables.Length; x++)
		{
			int y = x;
			colorTriangleSelectables[x].OnInteract += delegate {
				colorTriangleSelectables[y].AddInteractionPunch();
				audioMod.PlaySoundAtTransform("tick", transform);
				audioMod.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonRelease, transform);
				if (isSubmitting && interactable)
				{
					curSelectedColor = y;
					for (int idx = 0; idx < colorTrianglesHL.Length; idx++)
					{
						colorTrianglesHL[idx].enabled = y == idx;
					}
					UpdateSegments(false);
				}
				return false;
			};
		}

		LED.OnInteract += delegate {
			LED.AddInteractionPunch();
			audioMod.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonRelease, transform);
			if (hasStarted && interactable)
			{
				if (!isSubmitting)
				{
					curIdx++;
					if (curIdx >= displayedValues.Count)
						curIdx = 0;
					DisplayGivenValue(displayedValues[curIdx]);
				}
				else
				{
					curStrikeCount = info.GetStrikes();
					if (zenDetected || (timeDetected && localStrikes >= 2) || (!zenDetected && !timeDetected && curStrikeCount > 1))
					{
						isSubmitting = false;
						for (int x = 0; x < colorTriangles.Length; x++)
							colorTriangles[x].material.color = Color.black;
						DisplayGivenValue(displayedValues[curIdx]);
						for (int z = 0; z < colorTrianglesHL.Length; z++)
						{
							colorTrianglesHL[z].enabled = false;
						}
					}
				}
			}
			return false;
		};

		stageDisplay.OnInteract += delegate {
			stageDisplay.AddInteractionPunch(2);
			audioMod.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
			if (hasStarted && interactable)
			{
				if (!isSubmitting)
				{
					isSubmitting = true;
					UpdateSegments(false);
					LEDMesh.material.color = Color.black;
					colorblindIndc.text = "";
					stageIndc.text = "SUB";
					for (int x = 0; x < colorTriangles.Length; x++)
						colorTriangles[x].material.color = new Color(x % 2, x / 2 % 2, x / 4 % 2);
					for (int idx = 0; idx < colorTrianglesHL.Length; idx++)
					{
						colorTrianglesHL[idx].enabled = idx == curSelectedColor;
					}
				}
				else
				{
					if (segmentsColored.SequenceEqual(segmentsSolution))
					{
						audioMod.PlaySoundAtTransform("InputCorrect", transform);
						Debug.LogFormat("[7 #{0}]: Correct segment colors submitted. Module passed.", curModID);
						interactable = false;
						modSelf.HandlePass();
						for (int x = 0; x < colorTrianglesHL.Length; x++)
						{
							colorTrianglesHL[x].enabled = false;
						}
						stageIndc.text = "";
						StartCoroutine(PlaySolveAnim());
					}
					else
					{
						modSelf.HandleStrike();
						Debug.LogFormat("[7 #{0}]: Strike! You submitted the following segment colors in reading order: {1}", curModID, segmentsColored.Select(a => colorList[a]).Join(", "));
						UpdateSegments(true);
						localStrikes += timeDetected ? 1 : 0;
						segmentsColored = new int[7];
					}
				}
			}
			return false;
		};
		for (int x = 0; x < segments.Length; x++)
			segments[x].material = matSwitch[0];
		for (int x = 0; x < colorTriangles.Length; x++)
			colorTriangles[x].material = matSwitch[0];
		LEDMesh.material = matSwitch[0];
		stageIndc.text = "";

		try
		{
			colorblinddetected = colorblindMode.ColorblindModeActive;
		}
		catch
		{
			colorblinddetected = false;
		}
		colorblindIndc.text = "";
	}
	void Start () {
		modSelf.OnActivate += delegate {
			for (int x = 0; x < segments.Length; x++)
				segments[x].material = matSwitch[1];
			for (int z = 0; z < colorTriangles.Length; z++)
			{
				colorTriangles[z].material = matSwitch[1];
				colorTriangles[z].material.color = Color.black;
			}
			LEDMesh.material = matSwitch[1];
			int modCount = info.GetSolvableModuleNames().Count;
			int stagesToGenerate = Mathf.Min(modCount, 7);
			Debug.LogFormat("[7 #{0}]: Modules detected: {1}", curModID, modCount);
			GenerateStages(stagesToGenerate);
			CalculateSolution();
			zenDetected = ZenModeActive;
			timeDetected = TimeModeActive;
			DisplayGivenValue(displayedValues[curIdx]);
			hasStarted = true;
			interactable = true;

		};
	}

	void GenerateStages(int extStageCount)
	{
		Debug.LogFormat("[7 #{0}]: Extra stages to generate: {1}", curModID, extStageCount);
		for (int x = 0; x < finalValues.Length; x++)
			finalValues[x] = Random.Range(-9, 10);
		Debug.LogFormat("[7 #{0}]: Values are logged in RGB format ( R, G, B )", curModID, finalValues.Join(", "));
		Debug.LogFormat("[7 #{0}]: Initial Values: ( {1} )", curModID, finalValues.Join(", "));
		displayedValues.Add(finalValues.ToArray()); // Add the initial stage.
		idxOperations.Add(-1);
		for (int x = 0; x < extStageCount; x++)
		{
			int[] modifedNumbers = { Random.Range(-9, 10), Random.Range(-9, 10), Random.Range(-9, 10) };
			string[] operationStrings = { "Red", "Green", "Blue", "White" };
			int operationModifer = Random.Range(0, 4);
			Debug.LogFormat("[7 #{0}]: Stage {1}: LED: {3}, Values: ( {2} )", curModID, x + 1, modifedNumbers.Join(", "), operationStrings[operationModifer]);
			switch (operationModifer)
			{
				case 0:
					for (int p = 0; p < finalValues.Length; p++)
					{
						finalValues[p] += modifedNumbers[p];
					}
					goto default;
				case 1:
					for (int p = 0; p < finalValues.Length; p++)
					{
						finalValues[p] -= modifedNumbers[p];
					}
					goto default;
				case 2:
					for (int p = 0; p < finalValues.Length; p++)
					{
						finalValues[p] = modifedNumbers[p] - finalValues[p];
					}
					goto default;
				case 3:
					for (int p = 0; p < finalValues.Length; p++)
					{
						finalValues[p] *= modifedNumbers[p];
					}
					goto default;
				default:
					for (int p = 0; p < finalValues.Length; p++)
					{
						finalValues[p] %= 10;
					}
					Debug.LogFormat("[7 #{0}]: Stage {1}: Result after modification: ( {2} )", curModID, x + 1, finalValues.Join(", "));
					break;
			}
			displayedValues.Add(modifedNumbers);
			idxOperations.Add(operationModifer);
		}
	}
	
	void CalculateSolution()
	{
		for (int x = 0; x < segmentsSolution.Length; x++)
		{
			int segIdx = segmentLogging[x];
			bool[] displayCnl = { false, false, false };
			for (int y = 0; y < displayCnl.Length; y++)
			{
				int grabbedValue = finalValues[y];
				bool invert = finalValues[y] < 0;
				string absVal = Mathf.Abs(finalValues[y]).ToString();
				int valIdx = segmentCodings.possibleValues.IndexOf(absVal[0]);
				if (valIdx != -1)
				{
					displayCnl[y] = invert != segmentCodings.segmentStates[valIdx, segIdx];
				}
			}
			segmentsSolution[x] = (displayCnl[0] ? 1 : 0) + (displayCnl[1] ? 1 : 0) * 2 + (displayCnl[2] ? 1 : 0) * 4;
		}
		Debug.LogFormat("[7 #{0}]: This gives the final segment combinations in reading order: {1}", curModID, segmentsSolution.Select(a => colorList[a]).Join(", "));

	}

	void DisplayGivenValue(int[] curVal)
	{
		if (curVal.Length == 3)
		{
			for (int x = 0; x < segmentLogging.Length; x++)
			{
				int segIdx = segmentLogging[x];
				bool[] displayCnl = { false, false, false };
				
				for (int y = 0; y < displayCnl.Length; y++)
				{
					int grabbedValue = curVal[y];
					bool invert = curVal[y] < 0;
					string absVal = Mathf.Abs(curVal[y]).ToString();
					int valIdx = segmentCodings.possibleValues.IndexOf(absVal[0]);
					if (valIdx != -1)
					{
						displayCnl[y] = invert != segmentCodings.segmentStates[valIdx,segIdx];
					}
				}
				segments[x].material.color = new Color(displayCnl[0] ? 1: 0, displayCnl[1] ? 1 : 0, displayCnl[2] ? 1 : 0);
			}
			stageIndc.text = curIdx.ToString();
			Color[] cPallete = { Color.red, Color.green, Color.blue, Color.white };
			string[] cPalleteCBlind = { "R", "G", "B", "W" };
			LEDMesh.material.color = idxOperations[curIdx] < 0 || idxOperations[curIdx] >= 4 ? Color.black : cPallete[idxOperations[curIdx]] ;
			colorblindIndc.text = colorblinddetected ? (idxOperations[curIdx] < 0 || idxOperations[curIdx] >= 4 ? "K": cPalleteCBlind[idxOperations[curIdx]]) : "";
		}
	}
	void UpdateSegments(bool canValidCheck)
	{
		for (int x = 0; x < segments.Length; x++)
		{
			segments[x].material.color = canValidCheck
				? segmentsColored[x] == segmentsSolution[x] ? Color.green : Color.red
				: new Color(segmentsColored[x] % 2, segmentsColored[x] / 2 % 2, segmentsColored[x] / 4 % 2);
		}
	}
	// Update is called once per frame
	int animPrt = 0;
	void Update () {
		if (isSubmitting && interactable)
		{
			animPrt++;
			if (animPrt >= 90)
			{
				animPrt = 0;
			}
			else if (animPrt > 45)
			{
				if (zenDetected)
				{
					LEDMesh.material.color = Color.cyan;
				}
				else if (timeDetected && localStrikes >= 2)
				{
					LEDMesh.material.color = new Color(1, 0.5f, 0);
				}
				else if (!zenDetected && !timeDetected && info.GetStrikes() > 1)
				{
					LEDMesh.material.color = Color.red;
				}
			}
			else
			{
				LEDMesh.material.color = Color.black;
			}
		}
		else if (!interactable)
		{
			LEDMesh.material.color = Color.black;
		}
	}
	IEnumerator TurnOffTriangleLeds()
	{
		for (int x = 0; x < colorTriangles.Length; x++)
		{
			yield return new WaitForSeconds(0.25f);
			colorTriangles[x].material.color = Color.black;
			
		}
		yield return new WaitForSeconds(0.5f);
		for (int x = colorTriangles.Length; x > 0; x--)
		{
			yield return new WaitForSeconds(0f);
			colorTriangles[x-1].material.color = Color.green;
		}
		yield return new WaitForSeconds(1f);
		for (int x = colorTriangles.Length; x > 0; x--)
		{
			colorTriangles[x - 1].material.color = Color.black;
			
		}
		yield return null;
	}
	IEnumerator PlaySolveAnim()
	{
		string displayText = "yeah--";
		StartCoroutine(TurnOffTriangleLeds());
		foreach (char oneLetter in displayText)
		{
			for (int x = 0; x < segmentLogging.Length; x++)
			{
				int segIdx = segmentLogging[x];
				int valIdx = segmentCodings.possibleValues.IndexOf(oneLetter);
				segments[x].material.color = valIdx != -1 && segmentCodings.segmentStates[valIdx, segIdx] ? Color.green : Color.black;
			}
			yield return new WaitForSeconds(0.5f);
			for (int x = 0; x < segmentLogging.Length; x++)
			{
				segments[x].material.color = Color.black;
			}
			yield return new WaitForSeconds(0.05f);
		}
		
		yield return null;
	}
	// TP Handler Begins here

	IEnumerator TwitchHandleForcedSolve()
	{
		while (!hasStarted) yield return true;
		if (!isSubmitting)
		{
			stageDisplay.OnInteract();
			yield return new WaitForSeconds(0f);
		}
		List<int> segementsSolSorted = segmentsSolution.OrderBy(a => a).ToList();
		List<int> idxSorted = new int[] { 0, 1, 2, 3, 4, 5, 6 }.OrderBy(a => segmentsSolution[a]).ToList();


		Debug.Log(segementsSolSorted.Join());
		Debug.Log(idxSorted.Join());

		for (int x = 0; x < idxSorted.Count; x++)
		{
			if (segmentsColored[idxSorted[x]] != segementsSolSorted[x])
			{
				if (curSelectedColor != segementsSolSorted[x])
				{
					colorTriangleSelectables[segementsSolSorted[x]].OnInteract();
					yield return new WaitForSeconds(0.1f);
				}
				segmentSelectables[idxSorted[x]].OnInteract();
				yield return new WaitForSeconds(0.1f);
			}
		}

/*		for (int x = 0; x < segmentsSolution.Length; x++)
		{
			if (segmentsColored[x] != segmentsSolution[x])
			{
				if (curSelectedColor != segmentsSolution[x])
				{
					colorTriangleSelectables[segmentsSolution[x]].OnInteract();
					yield return new WaitForSeconds(0f);
				}
				segmentSelectables[x].OnInteract();
				yield return new WaitForSeconds(0f);
			}
		}*/
		stageDisplay.OnInteract();
		yield return true;
	}

#pragma warning disable IDE0044 // Add readonly modifier
	bool TimeModeActive;
	bool ZenModeActive;
	string TwitchHelpMessage = "\"!{0} R G B C M Y K W\" to select the color, \"!{0} 1 2 3 4 5 6 7\" to select the segments in reading order. These two commands can be chained, I.E \"!{0} R 1 C 2...\".\n"+
		"Cycle the stages with \"!{0} led cycle\" or \"!{0} led cycle # #\" for specific stages, go to a specific stage with \"!{0} led #\", or press the LED once with \"!{0} led\". Modify the cycle speed with \"!{0} cyclespeed #\" (1-9 seconds only) Highlight the segment's in reading order with \"!{0} segments\". Submit the current setup or enter submission mode with \"!{0} submit\"";
	bool TwitchShouldCancelCommand;
	int curCycleDelay = 3;

#pragma warning restore IDE0044 // Add readonly modifier
	IEnumerator ProcessTwitchCommand(string command)
	{
		string commandLower = command.ToLower();
		if (commandLower.RegexMatch(@"^led\scycle\s\d+\s\d+$"))
		{
			if (isSubmitting)
			{
				yield return "sendtochaterror The module is in submission phase. Use \"!{1} led\" to reaccess the stages once specific conditions have been satsfied.";
				yield break;
			}
			string[] leftovers = commandLower.Split();
			int leftValue;
			int rightValue;
			if (int.TryParse(leftovers[leftovers.Length - 2], out leftValue) && int.TryParse(leftovers[leftovers.Length - 1], out rightValue))
			{
				if (leftValue <= rightValue)
				{
					if (leftValue < 0 || leftValue >= displayedValues.Count || rightValue < 0 || rightValue >= displayedValues.Count)
					{
						yield return "sendtochaterror The cycle index for the following stage range " + leftValue + "," + rightValue + " do not contain all possible stages from the module.";
						yield break;
					}
					int lastStage = curIdx;
					yield return null;
					while (curIdx != leftValue)
					{
						yield return new WaitForSeconds(0.1f);
						LED.OnInteract();
					}
					for (int x = leftValue; x < rightValue + 1; x++)
					{
						yield return new WaitForSeconds(TwitchShouldCancelCommand ? 0.1f : curCycleDelay);
						LED.OnInteract();
					}
					while (curIdx != lastStage)
					{
						yield return new WaitForSeconds(0.1f);
						LED.OnInteract();
					}
				}
				else
				{
					yield return "sendtochaterror Your command is not valid for cycling. Do you mean \"!{1} led cycle "+rightValue+" "+leftValue+"\"?";
					yield break;
				}
			}
			else
			{
				yield return "sendtochaterror The stage numbers given can't seem to work that well. Retry the command again with a different condition.";
				yield break;
			}

		}
		else if (commandLower.RegexMatch(@"^led(\s(cycle|\d))?$"))
		{

			string leftover = commandLower.Length > 4 ? commandLower.Substring(4) : "";
			if (isSubmitting && leftover.Length != 0)
			{
				yield return "sendtochaterror The module is in submission phase. Use \"!{1} led\" to reaccess the stages once specific conditions have been satsfied.";
				yield break;
			}
			switch (leftover)
			{
				case "cycle":
					{
						int lastStage = curIdx;
						yield return null;
						while (curIdx != 0)
						{
							yield return new WaitForSeconds(0.1f);
							LED.OnInteract();
						}
						for (int x = 0; x < displayedValues.Count; x++)
						{
							yield return new WaitForSeconds(TwitchShouldCancelCommand ? 0.1f : curCycleDelay);
							LED.OnInteract();
						}
						while (curIdx != lastStage)
						{
							yield return new WaitForSeconds(0.1f);
							LED.OnInteract();
						}
						break;
					}
				case "0":
				case "1":
				case "2":
				case "3":
				case "4":
				case "5":
				case "6":
				case "7":
				case "8":
				case "9":
					{
						string stagesAccessible = "0123456789".Substring(0, displayedValues.Count);
						int specifiedStage = stagesAccessible.IndexOf(leftover);
						if (specifiedStage == -1)
						{
							yield return "sendtochaterror Sorry, but the specified stage \"" + leftover + "\" is not accessible.";
							yield break;
						}
						else if (specifiedStage == curIdx)
						{
							yield return "sendtochaterror Sorry, but the specified stage \"" + leftover + "\" is already being shown.";
							yield break;
						}
						while (curIdx != specifiedStage)
						{
							yield return new WaitForSeconds(0.1f);
							yield return null;
							LED.OnInteract();
						}
						break;
					}
				case "":
					yield return null;
					LED.OnInteract();
					yield break;
				default:
					yield return "sendtochaterror You aren't supposed to get this error.";
					yield break;
			}
		}
		else if (commandLower.RegexMatch(@"^cycle\s?speed\s\d+$"))
		{
			yield return null;
			string[] commandParts = commandLower.Split();
			string intereptedDigit = commandParts[commandParts.Length - 1];
			int timePossible = int.Parse(intereptedDigit);
			if (timePossible > 0 && timePossible < 10)
			{
				curCycleDelay = timePossible;
				yield return "sendtochat {0}, I have setted the cycle speed for this module to " + intereptedDigit + " second(s).";
			}
			else
				yield return "sendtochaterror {0}, I am not setting the cycle speed for this module to " + intereptedDigit + " second(s).";
			
		}
		else if (commandLower.RegexMatch(@"^segments$"))
		{
			for (int x = 0; x < segmentSelectables.Count(); x++)
			{
				yield return new WaitForSeconds(0.1f);
				yield return null;
				segmentSelectables[x].OnHighlight();
				yield return new WaitForSeconds(TwitchShouldCancelCommand ? 0.1f : 3f);
				segmentSelectables[x].OnHighlightEnded();
			}
		}
		else if (commandLower.RegexMatch(@"^sub(mit)?$"))
		{
			yield return null;
			stageDisplay.OnInteract();
			yield return "solve";
		}
		else
		{
			if (!isSubmitting)
			{
				yield return "sendtochaterror The module is not ready to submit yet. Use the \"submit\" command to make the module enter submission mode.";
				yield break;
			}
			List<string> segmentString = new List<string>() { "1", "2", "3", "4", "5", "6", "7" };
			List<KMSelectable> pressables = new List<KMSelectable>();
			foreach (string commandPart in commandLower.Split())
			{
				int idxSegments = segmentString.IndexOf(commandPart);
				int idxColors = colorList.ToLower().IndexOf(commandPart);
				if (commandPart.Length != 1 || (idxSegments == -1 && idxColors == -1))
				{
					yield return "sendtochaterror Sorry, but what does \"" + commandPart + "\" represent again?";
					yield break;
				}
				else if (idxColors != -1)
				{
					pressables.Add(colorTriangleSelectables[idxColors]);
				}
				else if (idxSegments != -1)
				{
					pressables.Add(segmentSelectables[idxSegments]);
				}
				else
				{
					yield return "sendtochaterror Sorry but what is \"" + commandPart + "\" supposed to be?";
					yield break;
				}
			}
			yield return null;
			yield return pressables.ToArray();
		}
		yield break;
	}

}
