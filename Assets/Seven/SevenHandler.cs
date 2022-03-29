using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KeepCoding;
using System;
using System.Text.RegularExpressions;
using Random = UnityEngine.Random;

public class SevenHandler : MonoBehaviour {

	public KMBombModule modSelf;
	public KMAudio audioMod;
	public GameObject entireModule;
	public KMBombInfo info;
	public MeshRenderer[] segments, colorTriangles, colorTrianglesHL;
	public MeshRenderer LEDMesh, stageRender;
	public KMSelectable[] segmentSelectables, colorTriangleSelectables;
	public KMSelectable LED, stageDisplay;
	public KMColorblindMode colorblindMode;
	private SevenSegmentCodings segmentCodings;
	public TextMesh stageIndc, colorblindIndc;
	public TextMesh[] colorblindTextTri, colorblindTextSeg;
	public Material[] matSwitch = new Material[2];

	int[] finalValues = new int[3];

	List<int[]> displayedValues = new List<int[]>();
	List<int> idxOperations = new List<int>();

	const float authorPPAScaling = 1.25f;
	const int authorMaxPPA = -1;

	readonly int[] segmentLogging = { 0, 5, 1, 6, 4, 2, 3 };
	readonly string colorList = "KRGYBMCW";

	int curSelectedColor = 0;
	int[] segmentsColored = new int[7], segmentsSolution = new int[7];

	string[] soundsPressNames = { "selectALT", "selectALT2" };
	static int modID;
	int curModID;

	// Detection and Logging
	bool zenDetected, timeDetected, hasStarted, isSubmitting, interactable, colorblindDetected,
		uncapAll, delayChallenge, hasStruckTimeMode, hasEnteredSubmission, hasSubmitted, fastReads, disableUncapTP, disableRecapTP;
	int curIdx = 0, curStrikeCount, maxPPA = -1;
	float PPAScaling;

	IEnumerator currentHandler;
	FlyersOtherSettings universalSettings = new FlyersOtherSettings();

	// Use this for initialization
	void Awake()
	{
		segmentCodings = new SevenSegmentCodings();
		curModID = ++modID;
		for (int x = 0; x < segments.Length; x++)
		{
			segments[x].material = matSwitch[0];
			colorblindTextSeg[x].text = "";
		}
		for (int x = 0; x < colorTriangles.Length; x++)
		{
			colorTriangles[x].material = matSwitch[0];
			colorblindTextTri[x].text = "";
		}
		LEDMesh.material = matSwitch[0];
		stageIndc.text = "";
		try {
			ModConfig<FlyersOtherSettings> universalConfig = new ModConfig<FlyersOtherSettings>("FlyersOtherSettings");
			universalSettings = universalConfig.Settings;
			universalConfig.Settings = universalSettings;

			uncapAll = !universalSettings.SevenHardCapStageGeneration;
			fastReads = universalSettings.SevenForceFastReads;
			disableUncapTP = universalSettings.SevenNoTPUncapping;
			disableRecapTP = universalSettings.SevenNoTPRecapping;
			PPAScaling = universalConfig.Settings.UseAuthorSuggestedDynamicScoring ? authorPPAScaling : universalSettings.SevenPPAScale;
			maxPPA = universalConfig.Settings.UseAuthorSuggestedDynamicScoring ? authorMaxPPA : universalSettings.SevenMaxPPA;
		}
		catch {
			Debug.LogWarningFormat("<7 #{0}>: Settings for 7 do not work as intended! Using default settings instead!", curModID);
			uncapAll = false;
			fastReads = false;
			PPAScaling = 1f;
			maxPPA = -1;
			disableUncapTP = false;
			disableRecapTP = false;
		}
		finally
		{
			try
			{
				colorblindDetected = colorblindMode.ColorblindModeActive;
			}
			catch
			{
				colorblindDetected = false;
			}
		}
		colorblindIndc.text = "";
	}
	void TryOverrideMission()
    {
		try
		{
			var missionID = Application.isEditor ? "freeplay" : Game.Mission.ID;
			switch (missionID)
			{
				case "freeplay":
					Debug.LogFormat("<7 #{0}> MISSION DETECTED AS FREEPLAY. NOT OVERRIDING SETTINGS.", modID);
					return;
				case "mod_missionpack_VFlyer_mission47thWrathFlyer":
					Debug.LogFormat("<7 #{0}> DETECTED MISSION BY ID, OVERRIDING SETTINGS.", modID);
					disableUncapTP = true;
					disableRecapTP = true;
					uncapAll = true;
					return;
			}
			var desc = Game.Mission.Description ?? "";
			Match regexMatchCountVariants = Regex.Match(desc, @"\[7Override\]\s(Uncapped|Capped|Choice)");
			if (regexMatchCountVariants.Success)
			{
				var valueMatches = regexMatchCountVariants.Value;
				switch (valueMatches.Split().Last())
                {
					case "Uncapped":
						disableUncapTP = true;
						disableRecapTP = true;
						uncapAll = true;
						break;
					case "Capped":
						disableRecapTP = true;
						disableUncapTP = true;
						uncapAll = false;
						break;
					default:
						Debug.LogFormat("<7 #{0}> DETECTED OVERRIDE BY DESCRIPTION. LET THE DEFUSER DECIDE ON THIS.", modID);
						return;
                }
				Debug.LogFormat("<7 #{0}> DETECTED OVERRIDE BY DESCRIPTION. UNCAPPING? {1}", modID, uncapAll ? "YES" : "NO");
			}
			else
				Debug.LogFormat("<7 #{0}> UNABLE TO OVERRIDE BY ID AND DESCRIPTION. LET THE DEFUSER DECIDE ON THIS.", modID);
		}
		catch (Exception error)
		{
			Debug.LogErrorFormat("<Labeled Priorities Plus #{0}> EXCEPTION OCCURED. USING SETTINGS INSTEAD. PLEASE SEEK OUT THE CREATOR ON HOW TO FIX THIS.", modID);
			Debug.LogException(error);

		}
	}
	void Start () {
		TryOverrideMission();
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
			int stagesToGenerate = uncapAll ? modCount : Mathf.Min(modCount, 7);
			Debug.LogFormat("[7 #{0}]: Modules detected: {1}", curModID, modCount);
			GenerateStages(stagesToGenerate);
			CalculateSolution();
			zenDetected = ZenModeActive;
			timeDetected = TimeModeActive;
			DisplayGivenValue(displayedValues[curIdx]);
			hasStarted = true;
			interactable = true;

		};
		for (int x = 0; x < segmentSelectables.Length; x++)
		{
			int y = x;
			segmentSelectables[x].OnInteract += delegate {
				segmentSelectables[y].AddInteractionPunch();
				audioMod.PlaySoundAtTransform(soundsPressNames[Random.Range(0, soundsPressNames.Length)], transform);
				if (isSubmitting && interactable)
				{
					segmentsColored[y] = curSelectedColor;
					UpdateSegments(false);
				}
				return false;
			};
			segmentSelectables[x].OnHighlight += delegate {
				if (isSubmitting || !hasStarted) return;
				//Debug.LogFormat("segment {0} Highlighted", y);
				Color segmentColor = segments[y].material.color;
				int idx = Mathf.RoundToInt(segmentColor.r)
				+ Mathf.RoundToInt(segmentColor.g) * 2
				+ Mathf.RoundToInt(segmentColor.b) * 4;
				if (idx >= 0 && idx < colorTrianglesHL.Length)
				{
					colorTrianglesHL[idx].enabled = true;
					colorTriangles[idx].material.color = new Color(idx % 2, idx / 2 % 2, idx / 4 % 2);
					if (colorblindDetected)
						colorblindTextTri[idx].text = colorList.Substring(idx, 1);
				}
			};
			segmentSelectables[x].OnHighlightEnded += delegate {
				if (isSubmitting || !hasStarted) return;
				//Debug.LogFormat("segment {0} Dehighlighted", y);
				for (int z = 0; z < colorTrianglesHL.Length; z++)
				{
					colorTrianglesHL[z].enabled = false;
					colorTriangles[z].material.color = Color.black;
					colorblindTextTri[z].text = "";
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
					if (zenDetected || (timeDetected && hasStruckTimeMode) || (!zenDetected && !timeDetected && curStrikeCount > 0))
					{
						isSubmitting = false;
						for (int x = 0; x < colorTriangles.Length; x++)
						{
							colorTriangles[x].material.color = Color.black;
							colorblindTextTri[x].text = "";
						}
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
					hasEnteredSubmission = true;
					UpdateSegments(false);
					LEDMesh.material.color = Color.black;
					colorblindIndc.text = "";
					stageIndc.text = "SUB";
					stageIndc.color = Color.white;
					for (int x = 0; x < colorTriangles.Length; x++)
					{
						colorTriangles[x].material.color = new Color(x % 2, x / 2 % 2, x / 4 % 2);
						if (colorblindDetected)
							colorblindTextTri[x].text = colorList.Substring(x, 1);
					}
					for (int idx = 0; idx < colorTrianglesHL.Length; idx++)
					{
						colorTrianglesHL[idx].enabled = idx == curSelectedColor;
					}
				}
				else
				{
					hasSubmitted = true;
					if (segmentsColored.SequenceEqual(segmentsSolution))
					{
						audioMod.PlaySoundAtTransform("InputCorrect", transform);
						Debug.LogFormat("[7 #{0}]: Correct segment colors submitted. Module passed.", curModID);
						interactable = false;
						modSelf.HandlePass();
						foreach (TextMesh mesh in colorblindTextSeg)
							mesh.text = "";

						for (int x = 0; x < colorTrianglesHL.Length; x++)
						{
							colorTrianglesHL[x].enabled = false;
							colorblindTextTri[x].text = "";
						}
						stageIndc.text = "";
						StartCoroutine(PlaySolveAnim());
					}
					else
					{
						
						Debug.LogFormat("[7 #{0}]: Strike! You submitted the following segment colors in reading order: {1}", curModID, segmentsColored.Select(a => colorList[a]).Join(", "));
						modSelf.HandleStrike();
						UpdateSegments(true);
						hasStruckTimeMode = timeDetected;
						segmentsColored = new int[7];
						if (currentHandler != null)
							StopCoroutine(currentHandler);
						currentHandler = AnimateText();
						StartCoroutine(currentHandler);
					}
				}
			}
			return false;
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
				bool invert = grabbedValue < 0;
				string absVal = Mathf.Abs(grabbedValue).ToString();
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
					bool invert = grabbedValue < 0;
					string absVal = Mathf.Abs(grabbedValue).ToString();
					int valIdx = segmentCodings.possibleValues.IndexOf(absVal[0]);
					if (valIdx != -1)
					{
						displayCnl[y] = invert != segmentCodings.segmentStates[valIdx,segIdx];
					}
				}
				segments[x].material.color = new Color(displayCnl[0] ? 1: 0, displayCnl[1] ? 1 : 0, displayCnl[2] ? 1 : 0);
				if (fastReads && colorblindDetected)
					colorblindTextSeg[x].text = colorList.Substring((displayCnl[0] ? 1 : 0) + (displayCnl[1] ? 2 : 0) + (displayCnl[2] ? 4 : 0), 1);
				else
					colorblindTextSeg[x].text = "";
			}
			stageIndc.text = curIdx.ToString();
			stageIndc.color = uncapAll && displayedValues.Count() > 8 ? new Color(0.5f, 1f, 1f) : Color.white;
			Color[] cPallete = { Color.red, Color.green, Color.blue, Color.white };
			string[] cPalleteCBlind = { "R", "G", "B", "W" };
			LEDMesh.material.color = idxOperations[curIdx] < 0 || idxOperations[curIdx] >= 4 ? Color.black : cPallete[idxOperations[curIdx]] ;
			colorblindIndc.text = colorblindDetected ? (idxOperations[curIdx] < 0 || idxOperations[curIdx] >= 4 ? "K": cPalleteCBlind[idxOperations[curIdx]]) : "";
		}
	}
	IEnumerator AnimateText()
	{
		Material stageMat = stageRender.material;
		if (stageMat.HasProperty("_MainTex"))
		{
			string allTextPossible = segmentCodings.possibleValues;
			for (int x = 0; x < 20; x++)
			{
				stageMat.SetTextureScale("_MainTex", Vector2.one * Mathf.Abs(x - 10) / 10f);
				stageIndc.color = Color.red;
				stageIndc.text = new char[] { allTextPossible[Random.Range(0, allTextPossible.Length)], allTextPossible[Random.Range(0, allTextPossible.Length)], allTextPossible[Random.Range(0, allTextPossible.Length)] }.Join("");
				yield return new WaitForSeconds(Time.deltaTime);
			}
			stageMat.SetTextureScale("_MainTex", Vector2.one);
		}
		stageIndc.text = "SUB";
		stageIndc.color = Color.white;
	}
	void UpdateSegments(bool canValidCheck = false)
	{
		for (int x = 0; x < segments.Length; x++)
		{
			segments[x].material.color = canValidCheck
				? segmentsColored[x] == segmentsSolution[x] ? Color.green : Color.red
				: new Color(segmentsColored[x] % 2, segmentsColored[x] / 2 % 2, segmentsColored[x] / 4 % 2);
			colorblindTextSeg[x].text = colorblindDetected ? (canValidCheck
					? segmentsColored[x] == segmentsSolution[x] ? "!" : "X"
					: colorList.Substring(segmentsColored[x], 1)) : "";
				
		}
	}
	// Update is called once per frame
	float animDelay = 0;
	void Update () {
		if (isSubmitting && interactable)
		{
            animDelay += Time.deltaTime;
			if (animDelay >= 1f)
			{
				animDelay = 0;
			}
			else if (animDelay > 0.5f)
			{
				if (zenDetected)
				{
					LEDMesh.material.color = Color.cyan;
				}
				else if (timeDetected && hasStruckTimeMode)
				{
					LEDMesh.material.color = new Color(1, 0.5f, 0);
				}
				else if (!zenDetected && !timeDetected && info.GetStrikes() > 0)
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
	// Uncapping Stage Gen
	bool isSegmentAnim = false; 
	IEnumerator HandleUncapSegmentDisplay()
	{
		
		string allTextPossible = segmentCodings.possibleValues;
		string displayText = "uncapped";
		foreach (char letter in displayText)
		{
			for (int x = 0; x < segmentLogging.Length; x++)
			{
				int segIdx = segmentLogging[x];
				int valIdx = allTextPossible.IndexOf(letter);
				segments[x].material.color = valIdx != -1 && segmentCodings.segmentStates[valIdx, segIdx] ? Color.white : Color.black;
			}
			yield return new WaitForSeconds(0.25f);
			for (int x = 0; x < segmentLogging.Length; x++)
			{
				segments[x].material.color = Color.black;
			}
			yield return new WaitForSeconds(0.05f);
		}
		isSegmentAnim = false;
		yield return true;
	}
	IEnumerator UncapStageGen()
	{
		uncapAll = true;
		string allTextPossible = segmentCodings.possibleValues;
		audioMod.PlaySoundAtTransform("StaticStart", transform);
		hasStarted = false;
		isSegmentAnim = true;
		StartCoroutine(HandleUncapSegmentDisplay());
		Debug.LogFormat("[7 #{0}]: Uncapping 7 to the number of solvable modules on the bomb! Restarting entire procedure...", curModID);
		while (isSegmentAnim)
		{
			stageIndc.color = new Color(Random.value, Random.value, Random.value);
			stageIndc.text = new char[] { allTextPossible[Random.Range(0, allTextPossible.Length)], allTextPossible[Random.Range(0, allTextPossible.Length)], allTextPossible[Random.Range(0, allTextPossible.Length)] }.Join("");
			yield return new WaitForSeconds(0.05f);
		}
		audioMod.PlaySoundAtTransform("StaticEnd", transform);
		int totalSolvableCount = info.GetSolvableModuleNames().Count;
		displayedValues.Clear();
		idxOperations.Clear();
		stageIndc.color = Color.white;
		GenerateStages(totalSolvableCount);
		curIdx = 0;
		DisplayGivenValue(displayedValues[curIdx]);
		CalculateSolution();
		hasStarted = true;
		interactable = true;
		yield return true;
	}
	IEnumerator RecapStageGen()
	{
		uncapAll = false;
		audioMod.PlaySoundAtTransform("StaticEnd", transform);
		hasStarted = false;
		Debug.LogFormat("[7 #{0}]: Recapping 7! Restarting entire procedure...", curModID);
		for (int u = 0; u < 5; u++)
		{
			yield return new WaitForSeconds(0.1f);
			for (int x = 0; x < segments.Length; x++)
			{
				if ((u % 2 == 0 && x % 3 == 0 && u / 2 == x / 3) || (u % 2 != 0 && u / 2 == x / 3))
				{
					segments[x].material = matSwitch[0];
					colorblindTextSeg[x].text = "";
				}
			}
			stageIndc.text = segmentCodings.possibleValues[Random.Range(0, segmentCodings.possibleValues.Length)].ToString();

			yield return new WaitForSeconds(0.1f);
		}
		displayedValues.Clear();
		idxOperations.Clear();
		stageIndc.color = Color.white;
		isSubmitting = false;
		for (int x = 0; x < colorTriangles.Length; x++)
		{
			colorTriangles[x].material.color = Color.black;
			colorblindTextTri[x].text = "";
		}
		for (int z = 0; z < colorTrianglesHL.Length; z++)
		{
			colorTrianglesHL[z].enabled = false;

		}
		GenerateStages(7);
		curIdx = 0;
		DisplayGivenValue(displayedValues[curIdx]);
		CalculateSolution();
		hasStarted = true;
		interactable = true;
		yield return true;
	}

	// TP Handler Begins here
	IEnumerator TwitchHandleForcedSolve()
	{
		while (!hasStarted) yield return true;
		Debug.LogFormat("<7 #{0}>: Debug, Enforcing autosolve handling for TP", curModID);
		interactable = true;
		if (!isSubmitting)
		{
			stageDisplay.OnInteract();
			yield return new WaitForSeconds(0f);
		}
		List<int> segementsSolSorted = segmentsSolution.OrderBy(a => a).ToList();
		List<int> idxSorted = new int[] { 0, 1, 2, 3, 4, 5, 6 }.OrderBy(a => segmentsSolution[a]).ToList();

		Debug.LogFormat("<7 #{0}>: Debug, Solution Segments Sorted: {1}", curModID, segementsSolSorted.Join());
		Debug.LogFormat("<7 #{0}>: Debug, Idx Segments Sorted by Solution Segments: {1}", curModID, idxSorted.Join());

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
		// Old auto-solve handler.
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
	string TwitchHelpMessage = "\"!{0} R G B C M Y K W\" to select the color, \"!{0} 1 2 3 4 5 6 7\" to select the segments in reading order. These two commands can be chained, I.E \"!{0} R 1 C 2...\".\n" +
		"Cycle the stages with \"!{0} led cycle\" or \"!{0} led cycle # #\" for specific stages, go to a specific stage with \"!{0} led #\", or press the LED once with \"!{0} led\". Modify the cycle speed with \"!{0} cyclespeed #\" (0.5-9.5 seconds only) Highlight the segment's in reading order with \"!{0} segments\".\n" +
		"Submit the current setup or enter submission mode with \"!{0} submit\". Colorblind mode can be enabled viva \"!{0} colorblind\". Get each segment's color quicker with colorblind enabled by doing \"!{0} colorblind fastreads\".";
	bool TwitchShouldCancelCommand;
	float curCycleDelay = 3;
#pragma warning restore IDE0044 // Add readonly modifier
	IEnumerator HandleDelay()
	{
		delayChallenge = true;
		yield return new WaitForSecondsRealtime(5f);
		delayChallenge = false;
	}
	IEnumerator ProcessTwitchCommand(string command)
	{
		if (!hasStarted)
		{
			yield return "sendtochaterror I'm not letting you interact with this module immediately. Wait for a bit until the module is ready.";
			yield break;
		}
		string commandLower = command.ToLower();
		if (commandLower.RegexMatch(@"^((re)?cap|too\s?scared|nomore)$"))
        {
			int totalSolvableCount = info.GetSolvableModuleNames().Count;
			if (!uncapAll)
			{
				yield return "sendtochaterror The module already capped to 8 stages.";
				yield break;
			}
			else if (totalSolvableCount <= 7)
			{
				yield return "sendtochaterror Why is this necessary? You don't need to use that.";
				yield break;
			}
			else if (!hasSubmitted)
            {
				yield return "sendtochat {0}, you need to submit something 7 (#{1}) in order to recap this.";
			}
			else if (!delayChallenge)
			{
				StartCoroutine(HandleDelay());
				yield return "sendtochat {0}, are you sure you want to recap 7 (#{1})? Type in the same command within 5 seconds to confirm.";
				yield break;
			}
			else
			{
				StopCoroutine("HandleDelay");
				yield return null;
				StartCoroutine(RecapStageGen());
				yield return "sendtochat {0}, I don't blame you. It was too hard anyway.";
				yield break;
			}
		}
		else
		if (commandLower.RegexMatch(@"^(uncap|challenge\s?me)$"))
		{
			int totalSolvableCount = info.GetSolvableModuleNames().Count;
			if (uncapAll)
			{
				yield return "sendtochaterror The module already uncapped the stage limits.";
				yield break;
			}
			else if (totalSolvableCount <= 7)
			{
				yield return "sendtochaterror Uncapping seems to be redundant with this few modules on the bomb. Maybe do it when there are more modules on the bomb.";
				yield break;
			}
			else if (isSubmitting || hasSubmitted || hasEnteredSubmission)
			{
				yield return "sendtochat {0}, someone already tampered with 7 (#{1}). You'll have to do this when the module has not yet entered submission.";
				yield break;
			}
			else if (!delayChallenge)
			{
				if (disableUncapTP)
                {
					yield return "sendtochaterror Uncapping 7 is too dangerous! Who knows how long this attempt will last for!?";
                }
				StartCoroutine(HandleDelay());
				yield return "sendtochat {0}, are you sure you want to uncap 7 (#{1})? Type in the same command within 5 seconds to confirm.";
				yield break;
			}
			else
			{
				StopCoroutine("HandleDelay");
				yield return null;
				StartCoroutine(UncapStageGen());
				yield return "sendtochat {0}, you've asked for it.";
				yield break;
			}
		}
		else if (commandLower.RegexMatch(@"^led\scycle\s\d+\s\d+$"))
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
					for (int x = leftValue; x < rightValue + 1 && !TwitchShouldCancelCommand; x++)
					{
						float timeProcessed = 0;
						do
						{
							yield return new WaitForSeconds(Time.deltaTime);
							timeProcessed += Time.deltaTime;
						}
						while (timeProcessed < curCycleDelay && !TwitchShouldCancelCommand);
						LED.OnInteract();
					}
					while (curIdx != lastStage)
					{
						yield return new WaitForSeconds(0.1f);
						LED.OnInteract();
					}
					if (TwitchShouldCancelCommand)
						yield return "cancelled";
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
		else if (commandLower.RegexMatch(@"^led(\s(cycle|\d+))?$"))
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
						for (int x = 0; x < displayedValues.Count && !TwitchShouldCancelCommand; x++)
						{
							float timeProcessed = 0;
							do
							{
								yield return new WaitForSeconds(Time.deltaTime);
								timeProcessed += Time.deltaTime;
							}
							while (timeProcessed < curCycleDelay && !TwitchShouldCancelCommand);
							
							LED.OnInteract();
						}
						while (curIdx != lastStage)
						{
							yield return new WaitForSeconds(0.1f);
							LED.OnInteract();
						}
						if (TwitchShouldCancelCommand)
							yield return "cancelled";
						break;
					}
				case "":
					yield return null;
					LED.OnInteract();
					yield break;
				default:
					{
						
						List<string> stagesAccessible = new List<string>();
						for (var x = 0; x < idxOperations.Count; x++)
						{
							stagesAccessible.Add(x.ToString());
						}
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
						while (curIdx != specifiedStage && !TwitchShouldCancelCommand)
						{
							yield return new WaitForSeconds(0.1f);
							yield return null;
							LED.OnInteract();
						}
						if (TwitchShouldCancelCommand)
							yield return "cancelled";
						break;
					}
			}
		}
		else if (commandLower.RegexMatch(@"^cycle\s?speed\s\d+(\.\d+)?$"))
		{
			yield return null;
			string[] commandParts = commandLower.Split();
			string intereptedValue = commandParts[commandParts.Length - 1];
			float timePossible = float.Parse(intereptedValue);
			if (timePossible >= 0.5f && timePossible <= 9.5f)
			{
				curCycleDelay = timePossible;
				yield return "sendtochat {0}, I have setted the cycle speed for 7 (#{1}) to " + timePossible.ToString("0.00") + " second(s).";
			}
			else
				yield return "sendtochaterror {0}, I am not setting the cycle speed for 7 (#{1}) to " + timePossible.ToString("0.00") + " second(s).";
			
		}
		else if (commandLower.RegexMatch(@"^colou?rblind(\s(fastreads?|toggle))?$"))
		{
			string[] splitted = commandLower.Split();
			if (splitted.Length <= 1 || splitted[1] == "toggle")
			{
				yield return null;
				colorblindDetected = !colorblindDetected;
				if (!isSubmitting)
					DisplayGivenValue(displayedValues[curIdx]);
				else
				{
					UpdateSegments(false);
					for (int x = 0; x < colorTriangles.Length; x++)
					{
						colorTriangles[x].material.color = new Color(x % 2, x / 2 % 2, x / 4 % 2);
						colorblindTextTri[x].text = colorblindDetected ? colorList.Substring(x, 1) : "";
					}
				}
			}
			else
            {
				yield return null;
				fastReads = !fastReads;
				colorblindDetected = true;
				if (!isSubmitting)
					DisplayGivenValue(displayedValues[curIdx]);
				else
				{
					UpdateSegments(false);
					for (int x = 0; x < colorTriangles.Length; x++)
					{
						colorTriangles[x].material.color = new Color(x % 2, x / 2 % 2, x / 4 % 2);
						colorblindTextTri[x].text = colorblindDetected ? colorList.Substring(x, 1) : "";
					}
				}
				yield return "sendtochat Colorblind fast read for 7 (#{1}) have been " + (fastReads ? "enabled" : "disabled")+".";
			}
		}
		else if (commandLower.RegexMatch(@"^segments$"))
		{
			if (isSubmitting)
            {
				yield return "sendtochaterror I'm not going to get the segments' colors while the module is in submission phase.";
				yield break;
			}
			for (int x = 0; x < segmentSelectables.Count() && !TwitchShouldCancelCommand; x++)
			{
				var curSelected = segmentSelectables[x].Highlight.gameObject;
				var highlight = curSelected.transform.Find("Highlight(Clone)");
				if (highlight != null)
					curSelected = highlight.gameObject ?? curSelected;

				yield return new WaitForSeconds(0.1f);
				yield return null;
				segmentSelectables[x].OnHighlight();
				curSelected.SetActive(true);
				yield return new WaitForSeconds(TwitchShouldCancelCommand ? 0.1f : 3f);
				segmentSelectables[x].OnHighlightEnded();
				curSelected.SetActive(false);
			}
			if (TwitchShouldCancelCommand)
				yield return "cancelled";
		}
		else if (commandLower.RegexMatch(@"^sub(mit)?$"))
		{
			if (isSubmitting && uncapAll && displayedValues.Count > 8)
			{
				yield return "multiple strikes";
				yield return "sendtochat Was it worth it?";
				if (segmentsColored.SequenceEqual(segmentsSolution))
                {
					int pointsToGive = Mathf.FloorToInt((idxOperations.Count - 8) * PPAScaling);
					if (pointsToGive > 0)
						yield return "awardpointsonsolve " + (maxPPA >= 0 ? Mathf.Min(maxPPA, pointsToGive).ToString() : pointsToGive.ToString());
				}
				yield return null;
				stageDisplay.OnInteract();
				yield return segmentsColored.SequenceEqual(segmentsSolution) ? "sendtochat It sure is." : "sendtochat It wasn't. Of course.";
				yield return "end multiple strikes";
				yield break;
			}
			yield return null;
			stageDisplay.OnInteract();
			yield return "solve";
		}
		else
		{
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
			if (!isSubmitting)
			{
				yield return "sendtochaterror The module is not ready to submit yet. Use the \"submit\" command to make the module enter submission mode.";
				yield break;
			}
			yield return null;
			yield return pressables.ToArray();
		}
		yield break;
	}

}
