using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using KModkit;
using UnityEngine;
using uernd = UnityEngine.Random;

public class MastermindCruelRestrictedCore : MastermindRestrictedCore {

	public MeshRenderer backingRenderer;
	public KMBombInfo bombInfo;
	public TextMesh colorblindDisplayTextL, colorblindDisplayTextM, colorblindDisplayTextR;
	public MeshRenderer cbDisplayL, cbDisplayM, cbDisplayR;
	public GameObject coreRotatable;
	private static int modCounter = 1;
	long startTimeMin;
	private List<int> modifierColorIdxA = new List<int>(), modifierColorIdxB = new List<int>(), modifierColorIdxC = new List<int>();
	private Color[] idxLeftColors = { Color.red, Color.yellow, Color.cyan, Color.white, Color.magenta, Color.green },
		idxCenterColors = { Color.cyan, Color.green, Color.yellow, Color.magenta, Color.white, Color.red },
		idxRightColors = { Color.cyan, Color.green, Color.yellow, Color.magenta, Color.white, Color.red };

	Vector3 startPosL, startPosM, startPosR;
	// Use this for initialization
	void Start() {
		loggingID = modCounter++;
		ResetModule();
		resetButton.OnInteract += delegate {
			resetButton.AddInteractionPunch();
			audioKM.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, resetButton.transform);
			if (interactable)
				ResetModule();
			return false;
		};
		queryButton.OnInteract += delegate {
			queryButton.AddInteractionPunch();
			audioKM.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, queryButton.transform);
			if (interactable)
				QueryModule();
			return false;
		};
		for (int x = 0; x < possibleSelectables.Length; x++)
		{
			int y = x;
			possibleSelectables[x].OnInteract += delegate {
				audioKM.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, queryButton.transform);
				if (interactable)
				{
					currentInputs[y] = (currentInputs[y] + 1) % maxPossible;
					UpdateCurrentDisplay();
				}
				return false;
			};
		}
		if (!colorblindDetected)
		{
			foreach (TextMesh aMesh in correctColorblindText)
			{
				aMesh.text = "";
			}
			colorblindDisplayTextL.text = "";
			colorblindDisplayTextM.text = "";
			colorblindDisplayTextR.text = "";
		}

		startTimeMin = (long)bombInfo.GetTime() / 60;


		startPosL = correctBothDisplay.transform.localPosition;
		startPosM = correctColorDisplay.transform.localPosition;
		startPosR = queryLeftDisplay.transform.localPosition;
		HandleColorblindToggle();
	}
	void HandleColorblindToggle()
	{
		cbDisplayL.enabled = colorblindDetected;
		cbDisplayM.enabled = colorblindDetected;
		cbDisplayR.enabled = colorblindDetected;
		correctBothDisplay.transform.localPosition = startPosL + (colorblindDetected ? Vector3.forward : Vector3.zero) * .2f;
		correctColorDisplay.transform.localPosition = startPosM + (colorblindDetected ? Vector3.forward : Vector3.zero) * .2f;
		queryLeftDisplay.transform.localPosition = startPosR + (colorblindDetected ? Vector3.forward : Vector3.zero) * .2f;
	}
	protected override void QuickLog(string toLog)
	{
		Debug.LogFormat("[Mastermind Restricted Cruel #{0}]: {1}", loggingID, toLog);
	}

	protected override void ResetModule()
	{
		queryCorrectColorAndPos.Clear();
		queryCorrectColorNotPos.Clear();
		allQueries.Clear();
		modifierColorIdxA.Clear();
		modifierColorIdxB.Clear();
		modifierColorIdxC.Clear();
		currentInputs = new int[selectableRenderer.Length];
		correctInputs = new int[selectableRenderer.Length];
		queriesLeft = 15;
		maxPossible = new[] { colorList.Length, colorblindLetters.Length, invertColorblindLetter.Length }.Min();
		for (int x = 0; x < correctInputs.Length; x++)
		{
			correctInputs[x] = uernd.Range(0, maxPossible);
		}
		QuickLog(string.Format("The correct answer is now [{0}] Get this within {1} distant queries to disarm the module.", correctInputs.Select(a => colorblindLetters[a]).Join(), queriesLeft));
		UpdateCurrentDisplay();
		correctBothDisplay.text = "";
		correctColorDisplay.text = "";
		queryLeftDisplay.text = "";
		colorblindDisplayTextL.text = "";
		colorblindDisplayTextM.text = "";
		colorblindDisplayTextR.text = "";
	}

	readonly int[][] combinationSets = {
		new[] { 0, 1, 2 },
		new[] { 1, 2, 0 },
        new[] { 2, 0, 1 },
		new[] { 0, 2, 1 },
		new[] { 2, 1, 0 },
		new[] { 1, 0, 2 },
	};

	protected override void QueryModule()
	{
		// Modifier Table
		IEnumerable<int> serialNoDigits = bombInfo.GetSerialNumberNumbers();

		int[,] modiferTable = new int[,] {
			{ bombInfo.GetOnIndicators().Count(), bombInfo.GetOffIndicators().Count(), bombInfo.GetIndicators().Count() },
			{ bombInfo.GetSolvedModuleIDs().Count(), bombInfo.GetSolvableModuleIDs().Count() - bombInfo.GetSolvedModuleIDs().Count(), bombInfo.GetModuleIDs().Count() - bombInfo.GetSolvableModuleIDs().Count() },
			{ bombInfo.GetBatteryCount(Battery.AA), bombInfo.GetBatteryCount(Battery.D), bombInfo.GetBatteryHolderCount() },
			{ bombInfo.GetStrikes(), bombInfo.GetModuleIDs().Count(), (int)(startTimeMin % 60) },
			{ bombInfo.CountUniquePorts(), bombInfo.CountDuplicatePorts(), bombInfo.GetPortPlateCount() },
			{ serialNoDigits.Count(), serialNoDigits.Sum(), bombInfo.GetSerialNumberLetters().Count() },
		};

		var loggingOffsets = new[] { "C", "G", "Y", "M", "W", "R", };
		Debug.LogFormat("<Mastermind Restricted Cruel #{0}> Query at {1} results:", loggingID, bombInfo.GetFormattedTime());
		for (var x = 0; x < modiferTable.GetLength(0); x++)
        {
			var toLog = "";
			for (var y = 0; y < modiferTable.GetLength(1); y++)
			{
				toLog += modiferTable[x, y] + " ";
			}
			Debug.LogFormat("<Mastermind Restricted Cruel #{0}> {1}: {2}", loggingID, loggingOffsets[x], toLog.Trim());
		}

		int idx = -1;
		for (int x = 0; x < allQueries.Count; x++)
		{
			if (currentInputs.SequenceEqual(allQueries[x]))
			{
				idx = x;
				break;
			}
		}
		if (idx != -1)
		{

			int selectedIdxL = modifierColorIdxA[idx],
				selectedIdxM = modifierColorIdxB[idx],
				selectedIdxR = modifierColorIdxC[idx];

			int[] baseArray = new int[] { queryCorrectColorAndPos[idx], queryCorrectColorNotPos[idx], 5 - queryCorrectColorAndPos[idx] - queryCorrectColorNotPos[idx], queriesLeft };

			int idxSelected = selectedIdxL;

			int offsetModifier = selectedIdxR == 0 ? 1 : selectedIdxR == 5 ? 2 : 0;

			var displayLeft = ((selectedIdxR == 1 ? queriesLeft : baseArray[combinationSets[idxSelected][0]]) + modiferTable[selectedIdxM, (0 + offsetModifier) % 3]) % 100;
			var displayCenter = ((selectedIdxR == 2 ? queriesLeft : baseArray[combinationSets[idxSelected][1]]) + modiferTable[selectedIdxM, (1 + offsetModifier) % 3]) % 100;
			var displayRight = ((selectedIdxR == 3 ? queriesLeft : baseArray[combinationSets[idxSelected][2]]) + modiferTable[selectedIdxM, (2 + offsetModifier) % 3]) % 100;

			correctBothDisplay.text = displayLeft.ToString();
			correctColorDisplay.text = displayCenter.ToString();
			queryLeftDisplay.text = displayRight.ToString();
			correctBothDisplay.color = idxLeftColors[selectedIdxL];
			correctColorDisplay.color = idxCenterColors[selectedIdxM];
			queryLeftDisplay.color = idxRightColors[selectedIdxR];
			colorblindDisplayTextL.text = new[] { "R", "Y", "C", "W", "M", "G", }[selectedIdxL];
			colorblindDisplayTextM.text = new[] { "C", "G", "Y", "M", "W", "R", }[selectedIdxM];
			colorblindDisplayTextR.text = new[] { "C", "G", "Y", "M", "W", "R", }[selectedIdxR];
		}
		else
		{
			queriesLeft--;
			// Process correct inputs.
			int correctColors = 0, correctPosandColors = 0;
			var debugColorSets = new List<string>();
			for (int x = 0; x < maxPossible; x++) // Start by filtering out each color separately to determine the states of each
			{
				int[] filteredCorrectInputs = correctInputs.Select(a => a == x ? a : -1).ToArray(),
					filteredCurrentInputs = currentInputs.Select(a => a == x ? a : -1).ToArray();
				int correctInOnePos = 0;
				int correctColorOnly = 0;
				for (int y = 0; y < filteredCorrectInputs.Length; y++) // Check if there the current color in the list matches the position exactly.
				{
					if (filteredCurrentInputs[y] != -1 && filteredCorrectInputs[y] != -1 && filteredCorrectInputs[y] == filteredCurrentInputs[y])
						correctInOnePos++;
				}
				if (filteredCurrentInputs.Count(a => a == x) >= filteredCorrectInputs.Count(a => a == x)) // Then check if there are more of 1 color than another color.
				{
					correctColorOnly = filteredCorrectInputs.Count(a => a == x) - correctInOnePos;

				}
				//correctColorOnly = Mathf.Max(filteredCorrectInputs.Count(a => a == x) - correctInOnePos, 0);
				
				correctColors += correctColorOnly;
				correctPosandColors += correctInOnePos;
				debugColorSets.Add(string.Format("{2} only: J = {0}, O = {1}", correctInOnePos, correctColorOnly, colorblindLetters[x]));
			}
			Debug.LogFormat("<Mastermind Restricted Cruel #{0}> ({1})", loggingID, debugColorSets.Join("),("));
			// Modify the result of the query
			
			int selectedIdxL = Enumerable.Range(0, idxLeftColors.Length).PickRandom(),
				selectedIdxM = Enumerable.Range(0, idxCenterColors.Length).PickRandom(),
				selectedIdxR = Enumerable.Range(0, idxRightColors.Length).PickRandom();

			int[] baseArray = new int[] { correctPosandColors, correctColors, 5 - correctPosandColors - correctColors, queriesLeft };
			allQueries.Add(currentInputs.ToArray());

			queryCorrectColorAndPos.Add(correctPosandColors);
			queryCorrectColorNotPos.Add(correctColors);

			modifierColorIdxA.Add(selectedIdxL);
			modifierColorIdxB.Add(selectedIdxM);
			modifierColorIdxC.Add(selectedIdxR);

			// Display the result of this query
			int offsetModifier = selectedIdxR == 0 ? 1 : selectedIdxR == 5 ? 2 : 0;
			int idxSelected = selectedIdxL;
			var displayLeft = ((selectedIdxR == 1 ? queriesLeft : baseArray[combinationSets[idxSelected][0]]) + modiferTable[selectedIdxM, (0 + offsetModifier) % 3]) % 100;
			var displayCenter = ((selectedIdxR == 2 ? queriesLeft : baseArray[combinationSets[idxSelected][1]]) + modiferTable[selectedIdxM, (1 + offsetModifier) % 3]) % 100;
			var displayRight = ((selectedIdxR == 3 ? queriesLeft : baseArray[combinationSets[idxSelected][2]]) + modiferTable[selectedIdxM, (2 + offsetModifier) % 3]) % 100;

			correctBothDisplay.text = displayLeft.ToString();
			correctColorDisplay.text = displayCenter.ToString();
			queryLeftDisplay.text = displayRight.ToString();
			correctBothDisplay.color = idxLeftColors[selectedIdxL];
			correctColorDisplay.color = idxCenterColors[selectedIdxM];
			queryLeftDisplay.color = idxRightColors[selectedIdxR];
			colorblindDisplayTextL.text = new[] { "R", "Y", "C", "W", "M", "G", }[selectedIdxL];
			colorblindDisplayTextM.text = new[] { "C", "G", "Y", "M", "W", "R", }[selectedIdxM];
			colorblindDisplayTextR.text = new[] { "C", "G", "Y", "M", "W", "R", }[selectedIdxR];

			QuickLog(string.Format("Query: [{0}]. Result: {1} correct color(s) in correct position, {2} correct color(s) not in correct position. ({3} color(s) that are not present at all.)",
				currentInputs.Select(a => colorblindLetters[a]).Join(), correctPosandColors, correctColors, 5 - correctColors - correctPosandColors));

			QuickLog(string.Format("This is being displayed as the following: {0} in {1}, {2} in {3}, {4} in {5}",
				displayLeft, new[] { "Red", "Yellow", "Cyan", "White", "Magenta", "Green", }[selectedIdxL],
				displayCenter, new[] { "Cyan", "Green", "Yellow", "Magenta", "White", "Red" }[selectedIdxM],
				displayRight,new[] { "Cyan", "Green", "Yellow", "Magenta", "White", "Red" }[selectedIdxR]));

			if (currentInputs.SequenceEqual(correctInputs))
			{
				QuickLog(string.Format("You got the correct sequence! Module disarmed."));
				StartCoroutine(HandleCruelDisarmAnim());
				
				interactable = false;
			}
			else if (queriesLeft <= 0)
			{
				interactable = false;
				QuickLog(string.Format("You've ran out of queries to get the correct answer. Strike! The module will now reveal correct answer and then reset."));
				StartCoroutine(HandleQueryExhaustAnim());
			}
		}
	}

	IEnumerator HandleCycleByOneAnim()
    {
		yield return new WaitForSeconds(0.2f);
		for (int x = 0; x < currentInputs.Length; x++)
		{
			currentInputs[x] = (currentInputs[x] + 1) % colorList.Length;
			UpdateCurrentDisplay();
		}
	}
	IEnumerator RandomizeDisplayColors()
    {
        for (int x = 0; x < 100; x++)
        {
			yield return new WaitForSeconds(0.2f);
			correctBothDisplay.color = new Color(uernd.value, uernd.value, uernd.value);
			correctColorDisplay.color = new Color(uernd.value, uernd.value, uernd.value);
			queryLeftDisplay.color = new Color(uernd.value, uernd.value, uernd.value);
			queryLeftDisplay.text = uernd.Range(0, 100).ToString();
			correctBothDisplay.text = uernd.Range(0, 100).ToString();
			correctColorDisplay.text = uernd.Range(0, 100).ToString();

			colorblindDisplayTextL.text = "!";
			colorblindDisplayTextM.text = "!";
			colorblindDisplayTextR.text = "!";
			for (int y = 0; y < currentInputs.Length; y++)
			{
				currentInputs[y] = (currentInputs[y] + 1) % colorList.Length;
				UpdateCurrentDisplay();
			}
		}
    }
	IEnumerator HandleCruelDisarmAnim()
	{
		var colorCycleAnim = RandomizeDisplayColors();

		StartCoroutine(colorCycleAnim);

		audioKM.PlaySoundAtTransform("StaticEnd", transform);

		Vector2 selectedDirection = uernd.insideUnitCircle;

		for (int y = 0; y < 1; y++)
		{
			for (float x = 0; x < 1f; x += 0.2f)
			{
				yield return new WaitForSeconds(0.1f);
				backingRenderer.material.color = Color.white * x + Color.red * (1 - x);
				coreRotatable.transform.localEulerAngles = new Vector3(selectedDirection.x * 10, 180, selectedDirection.y * 10);
				//coreRotatable.transform.localScale = new Vector3(uernd.value, uernd.value, uernd.value);
				selectedDirection = uernd.insideUnitCircle;
			}
            
			for (float x = 0; x < 1f; x += 0.2f)
			{
				yield return new WaitForSeconds(0.1f);
				backingRenderer.material.color = Color.red * x + Color.white * (1 - x);
				coreRotatable.transform.localEulerAngles = new Vector3(selectedDirection.x * 10, 180, selectedDirection.y * 10);
				coreRotatable.transform.localScale = new Vector3(uernd.value, uernd.value, uernd.value);
				selectedDirection = uernd.insideUnitCircle;
			}
			
			
		}
		Vector3 lastScale = coreRotatable.transform.localScale, lastRotation = coreRotatable.transform.localEulerAngles;
		for (float x = 0; x < 1f; x += Time.deltaTime)
		{
			yield return null;
			backingRenderer.material.color = Color.white * x + Color.red * (1 - x);
			coreRotatable.transform.localEulerAngles = lastRotation * (1f - x);
			coreRotatable.transform.localScale = lastScale * (1f - x) + Vector3.one * x;
		}
		coreRotatable.transform.localEulerAngles = Vector3.zero;
		coreRotatable.transform.localScale = Vector3.one;
		StopCoroutine(colorCycleAnim);
		backingRenderer.material.color = Color.white;
		currentInputs = correctInputs;
		UpdateCurrentDisplay();

		correctBothDisplay.color = Color.white;
		correctColorDisplay.color = Color.white;
		queryLeftDisplay.color = Color.white;
		correctBothDisplay.text = "5";
		correctColorDisplay.text = "0";
		queryLeftDisplay.text = queriesLeft.ToString();

		audioKM.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
		modSelf.HandlePass();
		yield return RevealCorrectAnim();
	}

#pragma warning disable IDE0051 // Remove unused private members
	readonly string TwitchHelpMessage = "Query the current state with \"!{0} query\", or specific colors with \"!{0} query W W W W W\"; Set the colors instead of trying to query it with \"!{0} set W W W W W\" Available colors are white, magenta, yellow, green, red, blue, orange, purple. Reset the module with \"!{0} reset\". Toggle colorblind mode with \"!{0} colorblind/colourblind\".";
#pragma warning restore IDE0051 // Remove unused private members
	Dictionary<int, string[]> intereptedValues = new Dictionary<int, string[]> {
		{ 0, new string[] { "white", "w", } },
		{ 1, new string[] { "magenta", "m", } },
		{ 2, new string[] { "yellow", "y", } },
		{ 3, new string[] { "green", "g", } },
		{ 4, new string[] { "red", "r", } },
		{ 5, new string[] { "blue", "b", } },
		{ 6, new string[] { "orange", "o", } },
		{ 7, new string[] { "purple", "p", } },
	};
	protected override IEnumerator ProcessTwitchCommand(string cmd)
	{
		if (Application.isEditor)
			cmd = cmd.Trim();
		if (!interactable)
		{
			yield return string.Format("sendtochaterror The module cannot be interacted right now. Wait a bit until you can interact with the module again.");
			yield break;
		}
		if (Regex.IsMatch(cmd, @"^colou?rblind$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
		{
			yield return null;
			colorblindDetected = !colorblindDetected;
			HandleColorblindToggle();
			UpdateCurrentDisplay();
		}
		else if (Regex.IsMatch(cmd, @"^set\s*", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
		{
			string modifiedCommand = cmd.Substring(3).Trim().ToLower();
			string[] splittedCommands = modifiedCommand.Split();
			if (splittedCommands.Any() && !string.IsNullOrEmpty(modifiedCommand))
			{
				List<int> cmdInput = new List<int>();
				for (int x = 0; x < splittedCommands.Length; x++)
				{
					bool successful = false;
					foreach (KeyValuePair<int, string[]> givenValue in intereptedValues)
					{
						if (givenValue.Value.Contains(splittedCommands[x]))
						{
							cmdInput.Add(givenValue.Key);
							successful = true;
							break;
						}
					}
					if (!successful)
					{
						yield return string.Format("sendtochaterror I do not know of a color \"{0}\" on the module. Valid colors are white, magenta, yellow, green, red, blue, orange, purple.", splittedCommands[x]);
						yield break;
					}
				}
				if (cmdInput.Count != 5)
				{
					yield return string.Format("sendtochaterror You provided {0} color(s) for this module when I expected exactly 5.", cmdInput.Count);
					yield break;
				}
				for (int x = 0; x < currentInputs.Length; x++)
				{
					yield return null;
					while (currentInputs[x] != cmdInput[x])
					{
						possibleSelectables[x].OnInteract();
						yield return new WaitForSeconds(0.1f);
					}
				}
			}
		}
		else if (Regex.IsMatch(cmd, @"^query\s*", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
		{
			string modifiedCommand = cmd.Substring(5).Trim().ToLower();
			string[] splittedCommands = modifiedCommand.Split();
			if (splittedCommands.Any() && !string.IsNullOrEmpty(modifiedCommand))
			{
				List<int> cmdInput = new List<int>();
				for (int x = 0; x < splittedCommands.Length; x++)
				{
					bool successful = false;
					foreach (KeyValuePair<int, string[]> givenValue in intereptedValues)
					{
						if (givenValue.Value.Contains(splittedCommands[x]))
						{
							cmdInput.Add(givenValue.Key);
							successful = true;
							break;
						}
					}
					if (!successful)
					{
						yield return string.Format("sendtochaterror I do not know of a color \"{0}\" on the module. Valid colors are white, magenta, yellow, green, red, blue, orange, purple.", splittedCommands[x]);
						yield break;
					}
				}
				if (cmdInput.Count != 5)
				{
					yield return string.Format("sendtochaterror You provided {0} color(s) for this module when I expected exactly 5.", cmdInput.Count);
					yield break;
				}
				for (int x = 0; x < currentInputs.Length; x++)
				{
					yield return null;
					while (currentInputs[x] != cmdInput[x])
					{
						possibleSelectables[x].OnInteract();
						yield return new WaitForSeconds(0.1f);
					}
				}
			}
			yield return null;
			queryButton.OnInteract();
			yield return "strike";
			yield return "solve";
		}
		else if (Regex.IsMatch(cmd, @"^reset$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
		{
			yield return null;
			resetButton.OnInteract();
		}
		yield break;
	}

}
