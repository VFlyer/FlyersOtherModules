using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class GameChangerScript : MonoBehaviour {
	public KMSelectable[] gridSelectables;
	public MeshRenderer[] gridRenderers, LEDRenderers, oldGridRenderers;
	public KMSelectable statusLight, submitButton, resetButton, clearButton;
	public KMBombModule modSelf;
	public KMAudio mAudio;
	public GameObject allOldGridRenderers;

	private static readonly int[] oldGCStates = { 42088, 22757, 61367, 49135, 55037, 63485, 62429, 47939, 41925, 1632 };

	List<bool[]> allExpectedStates;
	bool[] lastFinishedState, currentState;
	bool moduleSolved, interactable = false, flashingRed, solveOnNextIteration;
	static int modIDCnt = 1;
	int modID, iteractionCount = 0, statusLightClickCount;
	float cooldownThreshold = 0f;
	IEnumerator flashingAnim;

	// Use this for initialization
	void Start() {
		modID = modIDCnt++;
		currentState = new bool[16];
		for (var x = 0; x < gridSelectables.Length; x++)
		{
			var y = x;
			gridSelectables[x].OnInteract += delegate {
				mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, gridSelectables[y].transform);
				if (!moduleSolved && interactable)
				{
					currentState[y] = !currentState[y];
					UpdateVisuals();
				}
				return false;
			};
		}
		resetButton.OnInteract += delegate
		{
			resetButton.AddInteractionPunch();
			mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, resetButton.transform);
			if (!moduleSolved && interactable)
			{
				currentState = lastFinishedState.ToArray();
				UpdateVisuals();
			}
			return false;
		};
		clearButton.OnInteract += delegate
		{
			clearButton.AddInteractionPunch();
			mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, clearButton.transform);
			if (!moduleSolved && interactable)
			{
				for (var y = 0; y < currentState.Length; y++)
					currentState[y] = false;
				UpdateVisuals();
			}
			return false;
		};

		submitButton.OnInteract += delegate
		{
			submitButton.AddInteractionPunch();
			mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, submitButton.transform);
			if (!moduleSolved && interactable)
			{
				StartCoroutine(HandleCellFlashAnim());
				/*
				if (currentState.SequenceEqual(expectedState))
				{
					if (iteractionCount < 32)
					{
						GenerateExpectedState();
						if (expectedState.SequenceEqual(lastFinishedState))
						{
							QuickLog("Expected board state is exactly the previous board state. Disarming...");
							SolveModule();
						}
					}
					else
					{
						QuickLog("You overflowed the iteraction counter. Disarming...");
						SolveModule();
					}
				}
				else
				{
					QuickLog("You submitted an incorrect board state:");
					for (var x = 0; x < 4; x++)
					{
						QuickLog(currentState.Skip(x * 4).Take(4).Select(a => a ? "W" : "K").Join(""));
					}
					modSelf.HandleStrike();
					currentState = lastFinishedState.ToArray();
					UpdateVisuals();
					if (flashingAnim != null)
						StopCoroutine(flashingAnim);
					flashingAnim = HandleFlashingLEDAnim();
					StartCoroutine(flashingAnim);
				}
				*/
			}
			return false;
		};
		statusLight.OnInteract += delegate {
			if (!solveOnNextIteration && !moduleSolved && interactable)
            {
				if (cooldownThreshold <= 0f)
                {
					statusLightClickCount = 0;
                }
				cooldownThreshold = 1f;
				statusLightClickCount++;
				if (statusLightClickCount >= 5)
				{
					mAudio.PlaySoundAtTransform("StaticEnd", transform);
					solveOnNextIteration = true;
					interactable = false;
					StartCoroutine(HandleChangeStatusLightAnim());
					QuickLog("You are attempting to make the module solve on the next correct iteration...");
					if (iteractionCount <= 3)
					{
						QuickLog("...Before at least 3 iterations were correctly submitted. This is fine to get rid of Game Changer, but worth the strike?");
						modSelf.HandleStrike();
					}
				}
				else
                {
					mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, statusLight.transform);
				}
            }
			return false;
		};

		for (var x = 0; x < LEDRenderers.Length; x++)
		{
			LEDRenderers[x].material.color = Color.black;
		}
		modSelf.OnActivate += GenerateAllExpectedStates;
		statusLight.gameObject.SetActive(true);
		allOldGridRenderers.SetActive(false);
	}
	void QuickLog(string value, params object[] args)
	{
		Debug.LogFormat("[Game Changer #{0}] {1}", modID, string.Format(value, args));
	}

	void SolveModule()
    {
		moduleSolved = true;
		modSelf.HandlePass();
		for (var x = 0; x < LEDRenderers.Length; x++)
		{
			LEDRenderers[x].material.color = Color.black;
		}
		if (allOldGridRenderers.activeSelf)
		{
			StartCoroutine(HandleSolveAnimOldBoardOnly(iteractionCount));
		}
	}
	void GenerateAllExpectedStates()
    {
		lastFinishedState = new bool[18];
		for (var x = 0; x < lastFinishedState.Length; x++)
		{
			lastFinishedState[x] = Random.value < 0.5f;
		}
		currentState = lastFinishedState.ToArray();
		QuickLog("Initial Board State:");
		for (var x = 0; x < 6; x++)
		{
			QuickLog(lastFinishedState.Skip(x * 3).Take(3).Select(a => a ? "W" : "K").Join(""));
		}
        allExpectedStates = new List<bool[]>
        {
            lastFinishedState.ToArray()
        };
        var duplicateBoardFound = false;
        for (var iter = 0; iter < 8 && !duplicateBoardFound; iter++)
        {
			var lastState = allExpectedStates[iter].ToArray();
			var adjacentFromBlack = lastState.Take(9);
			var adjacentFromWhite = lastState.TakeLast(9).Reverse();
			QuickLog("White cell birth requires this many surrounding tiles: {0}",
				adjacentFromBlack.Any(a => a) ? Enumerable.Range(0, adjacentFromBlack.Count()).Where(a => adjacentFromBlack.ElementAt(a)).Join(", ") : "(empty)");
			QuickLog("White cell survival requires this many surrounding tiles: {0}",
				adjacentFromWhite.Any(a => a) ? Enumerable.Range(0, adjacentFromWhite.Count()).Where(a => adjacentFromWhite.ElementAt(a)).Join(", ") : "(empty)");
			var expectedState = new bool[18];
			for (var x = 0; x < lastState.Length; x++)
			{
				var whiteNeighborCount = 0;
				for (var deltaX = -1; deltaX <= 1; deltaX++)
				{
					for (var deltaY = -1; deltaY <= 1; deltaY++)
					{
						var curX = x % 3;
						var curY = x / 3;

						if ((deltaX != 0 || deltaY != 0) &&
							x % 3 + deltaX >= 0 && x % 3 + deltaX <= 2 &&
							x / 3 + deltaY >= 0 && x / 3 + deltaY <= 5)
						{
							whiteNeighborCount += lastState[x + deltaX + 3 * deltaY] ? 1 : 0;
						}
					}
				}
				//Debug.Log(whiteNeighborCount);
				expectedState[x] = lastState[x]
                    ? adjacentFromWhite.ElementAtOrDefault(whiteNeighborCount)
                    : adjacentFromBlack.ElementAtOrDefault(whiteNeighborCount);
            }
			QuickLog("Expected board state after {0} iteration{1}:", iter + 1, iter == 0 ? "" : "s");
			for (var x = 0; x < 6; x++)
			{
				QuickLog(expectedState.Skip(x * 3).Take(3).Select(a => a ? "W" : "K").Join(""));
			}
			if (allExpectedStates.Any(a => expectedState.SequenceEqual(a)))
			{
				duplicateBoardFound = true;
				QuickLog("At {0} iteration{1}, a duplicate board was found that is exactly the same state as the previous iterations. Stopping here.", iter + 1, iter == 0 ? "" : "s");
			}
			allExpectedStates.Add(expectedState.ToArray());
		}
		interactable = true;
		iteractionCount = 1;
		UpdateVisuals();
	}
	void UpdateIterationCounter()
    {
		iteractionCount++;
		lastFinishedState = allExpectedStates[iteractionCount - 1];
		UpdateVisuals();
		interactable = true;
	}
		/*
	void GenerateExpectedState()
	{
		if (!hasStarted)
		{
			hasStarted = true;
			lastFinishedState = new bool[18];
			for (var x = 0; x < lastFinishedState.Length; x++)
			{
				lastFinishedState[x] = Random.value < 0.5f;
			}
			expectedState = new bool[18];
			currentState = lastFinishedState.ToArray();
			QuickLog("Initial Board State:");
		}
		else
		{
			lastFinishedState = expectedState.ToArray();
			QuickLog("Board State after {0} correct iteration{1}:", iteractionCount, iteractionCount == 1 ? "" : "s");
		}

		for (var x = 0; x < 6; x++)
		{
			QuickLog(lastFinishedState.Skip(x * 3).Take(3).Select(a => a ? "W" : "K").Join(""));
		}

		var adjacentFromBlack = lastFinishedState.Take(9);
		var adjacentFromWhite = lastFinishedState.TakeLast(9).Reverse();

		QuickLog("White cell birth requires this many surrounding tiles: {0}",
			adjacentFromBlack.Any(a => a) ? Enumerable.Range(0, adjacentFromBlack.Count()).Where(a => adjacentFromBlack.ElementAt(a)).Join(", ") : "(empty)");
		QuickLog("White cell survival requires this many surrounding tiles: {0}",
			adjacentFromWhite.Any(a => a) ? Enumerable.Range(0, adjacentFromWhite.Count()).Where(a => adjacentFromWhite.ElementAt(a)).Join(", ") : "(empty)");
		for (var x = 0; x < lastFinishedState.Length; x++)
		{
			var whiteNeighborCount = 0;
			for (var deltaX = -1; deltaX <= 1; deltaX++)
			{
				for (var deltaY = -1; deltaY <= 1; deltaY++)
				{
					if ((deltaX != 0 || deltaY != 0) &&
						x % 3 + deltaX >= 0 && x % 3 + deltaX <= 2 &&
						x / 3 + deltaY >= 0 && x / 3 + deltaY <= 5)
					{
						whiteNeighborCount += lastFinishedState[x + deltaX + 3 * deltaY] ? 1 : 0;
					}
				}
			}
			//Debug.Log(whiteNeighborCount);
			if (lastFinishedState[x])
			{
				expectedState[x] = adjacentFromWhite.ElementAtOrDefault(whiteNeighborCount);
			}
			else
			{
				expectedState[x] = adjacentFromBlack.ElementAtOrDefault(whiteNeighborCount);
			}
		}
		iteractionCount++;
		QuickLog("Expected board state for {0} iteration{1}:", iteractionCount, iteractionCount == 1 ? "" : "s");
		for (var x = 0; x < 6; x++)
		{
			QuickLog(expectedState.Skip(x * 3).Take(3).Select(a => a ? "W" : "K").Join(""));
		}
		interactable = true;
		UpdateVisuals();
	}
		*/
	IEnumerator HandleSolveAnimOldBoardOnly(int startIdx = 0)
    {
		var curIt = startIdx;
		while (curIt < oldGCStates.Length)
		{
			for (var x = 0; x < oldGridRenderers.Length; x++)
			{
				oldGridRenderers[x].material.color = (oldGCStates[curIt] >> (oldGridRenderers.Length - 1 - x)) % 2 == 1 ? Color.green : Color.black;
			}
			yield return new WaitForSeconds(1f);
			curIt++;
		}
		for (var x = 0; x < oldGridRenderers.Length; x++)
		{
			oldGridRenderers[x].material.color = (oldGCStates.Last() >> (oldGridRenderers.Length - 1 - x)) % 2 == 1 ? Color.green : Color.black;
		}
	}
	IEnumerator HandleChangeStatusLightAnim()
    {
		for (float t = 1; t > 0; t -= Time.deltaTime * 2f)
		{
			statusLight.transform.localScale = Vector3.one * t;
			yield return null;
		}
		statusLight.transform.localScale = Vector3.zero;
		statusLight.gameObject.SetActive(false);
		allOldGridRenderers.SetActive(true);
		for (var x = 0; x < oldGridRenderers.Length; x++)
		{
			oldGridRenderers[x].material.color = (oldGCStates[iteractionCount] >> (oldGridRenderers.Length - 1 - x)) % 2 == 1 ? (iteractionCount <= 3 ? Color.red : Color.white) : Color.black;
		}
		for (float t = 0; t < 1f; t += Time.deltaTime * 2f)
		{
			for (var x = 0; x < oldGridRenderers.Length; x++)
			{
				oldGridRenderers[x].transform.localScale = Vector3.one * t;
			}
			yield return null;
		}
		for (var x = 0; x < oldGridRenderers.Length; x++)
		{
			oldGridRenderers[x].transform.localScale = Vector3.one;
		}
		interactable = true;
		for (var x = 0; x < oldGridRenderers.Length; x++)
		{
			oldGridRenderers[x].material.color = (oldGCStates[iteractionCount] >> (oldGridRenderers.Length - 1 - x)) % 2 == 1 ? Color.white : Color.black;
		}
	}
	IEnumerator HandleCellFlashAnim()
    {
		interactable = false;
		for (var x = 0; x < lastFinishedState.Length; x++)
		{
			gridRenderers[x].material.color = lastFinishedState[x] ? Color.white : Color.black;
		}
		yield return new WaitForSeconds(0.2f);
		for (var x = 0; x < currentState.Length; x++)
		{
			gridRenderers[x].material.color = currentState[x] ? Color.white : Color.black;
		}
		if (currentState.SequenceEqual(allExpectedStates[iteractionCount]))
		{
			UpdateIterationCounter();
			if (iteractionCount >= allExpectedStates.Count)
			{
				QuickLog("You submitted all {0} iteration{1} correctly. Disarming...", iteractionCount - 1, iteractionCount == 1 ? "" : "s");
				SolveModule();
				interactable = false;
			}
			else if (solveOnNextIteration)
            {
				QuickLog("You bailed after submitting {0} iteration{1} correctly. Disarming...", iteractionCount - 1, iteractionCount == 1 ? "" : "s");
				SolveModule();
				interactable = false;
			}
		}
		else
		{
			QuickLog("You submitted an incorrect board state at {0} iteration{1}:", iteractionCount, iteractionCount == 1 ? "" : "s");
			for (var x = 0; x < 6; x++)
			{
				QuickLog(currentState.Skip(x * 3).Take(3).Select(a => a ? "W" : "K").Join(""));
			}
			modSelf.HandleStrike();
			currentState = lastFinishedState.ToArray();
			
			if (flashingAnim != null)
				StopCoroutine(flashingAnim);
			flashingAnim = HandleFlashingLEDAnim();
			StartCoroutine(flashingAnim);
			yield return new WaitForSeconds(0.2f);
			UpdateVisuals();
			interactable = true;
		}
	}

	IEnumerator HandleFlashingLEDAnim()
    {
		flashingRed = true;
		for (var x = 0; x < LEDRenderers.Length; x++)
		{
			LEDRenderers[x].material.color = Color.red;
		}
		if (allOldGridRenderers.activeSelf)
			for (var x = 0; x < oldGridRenderers.Length; x++)
			{
				oldGridRenderers[x].material.color = (oldGCStates[iteractionCount] >> (oldGridRenderers.Length - 1 - x)) % 2 == 1 ? Color.red : Color.black;
			}
		yield return new WaitForSeconds(1f);
		var displayedIterCount = iteractionCount - 1;
		for (var x = 0; x < LEDRenderers.Length; x++)
		{
			var y = 1 << x;
			LEDRenderers[x].material.color = displayedIterCount / y % 2 == 1 ? Color.green : Color.black;
		}
		if (allOldGridRenderers.activeSelf)
			for (var x = 0; x < oldGridRenderers.Length; x++)
			{
				oldGridRenderers[x].material.color = (oldGCStates[iteractionCount] >> (oldGridRenderers.Length - 1 - x)) % 2 == 1 ? Color.white : Color.black;
			}
		flashingRed = false;
	}

	void UpdateVisuals()
    {
        for (var x = 0; x < currentState.Length; x++)
        {
			gridRenderers[x].material.color = currentState[x] ? Color.white : Color.black;
		}
		if (flashingRed) return;
		var displayedIterCount = iteractionCount - 1;
        for (var x = 0; x < LEDRenderers.Length; x++)
        {
			var y = 1 << x;
			LEDRenderers[x].material.color = displayedIterCount / y % 2 == 1 ? Color.green : Color.black;
        }
		if (allOldGridRenderers.activeSelf)
        {
            for (var x = 0; x < oldGridRenderers.Length; x++)
            {
				oldGridRenderers[x].material.color = ((iteractionCount < 0 ? oldGCStates.First() : iteractionCount >= oldGCStates.Length ? oldGCStates.Last() : oldGCStates[iteractionCount]) >> (oldGridRenderers.Length - 1 - x)) % 2 == 1 ? Color.white : Color.black;
            }
        }
    }
	IEnumerator TwitchHandleForcedSolve()
    {
		while (!moduleSolved)
        {
			while (!interactable)
				yield return true;
			var expectedState = allExpectedStates[iteractionCount];
			while (!currentState.SequenceEqual(expectedState))
            {
				for (var x = 0; x < expectedState.Length; x++)
                {
					if (currentState[x] != expectedState[x])
					{
						gridSelectables[x].OnInteract();
						yield return new WaitForSeconds(0.1f);
					}
                }
            }
			submitButton.OnInteract();
			do
				yield return true;
			while (!interactable);
			yield return null;
        }
    }
#pragma warning disable IDE0051 // Remove unused private members
	readonly string TwitchHelpMessage = "Toggle the following cell with \"!{0} [A-C][1-6]\" where columns are labeled A-C from left to right, rows are numbered 1-6 from top to bottom. " +
		"Multiple cells can be toggled in one command. " +
		"Submit the current board with \"!{0} submit\" or \"!{0} s\", reset the current board with \"!{0} reset\" or \"!{0} r\", or clear the current board with \"!{0} clear\" or \"!{0} c\". Press the status light once with \"!{0} sl\"" +
		"All of the mentioned possible commands can be chained into one command using spaces, E.G: \"!{0} c A1 B4 r C3 D2 s\". Commands may be interrupted upon trying to chain past the submit command if the submitted grid is incorrect at any point.";
#pragma warning restore IDE0051 // Remove unused private members
	IEnumerator ProcessTwitchCommand(string cmd)
	{
		if (Application.isEditor)
		{
			cmd = cmd.Trim();
		}
		if (!interactable || moduleSolved)
        {
			yield return "sendtochaterror The module is not accepting any commands at the moment. Wait a bit until the module is ready.";
			yield break;
        }
		var allPossibleCommands = cmd.Trim().Split();
		var allSelectablesToGo = new List<KMSelectable>();
		for (var x = 0; x < allPossibleCommands.Length; x++)
        {
			var curCmdPart = allPossibleCommands[x];
			Match coordMatch = Regex.Match(curCmdPart, @"^[a-z][0-9]$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
				submitMatch = Regex.Match(curCmdPart, @"^s(ubmit)?$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
				clearMatch = Regex.Match(curCmdPart, @"^c(lear)?$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
				resetMatch = Regex.Match(curCmdPart, @"^r(eset)?", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
				slMatch = Regex.Match(curCmdPart, @"^sl", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
			if (coordMatch.Success)
			{
				var curCoord = coordMatch.Value.ToLower().ToCharArray();
				var idxCol = "abc".IndexOf(curCoord.First());
				var idxRow = "123456".IndexOf(curCoord.Last());
				if (idxCol == -1 || idxRow == -1)
                {
					yield return string.Format("sendtochaterror Your command has been interrupted due to a bad coordinate: \"{0}\".",
						curCoord.Join(""));
					yield break;
				}
				allSelectablesToGo.Add(gridSelectables[idxCol + idxRow * 3]);
			}
			else if (submitMatch.Success)
			{
				allSelectablesToGo.Add(submitButton);
			}
			else if (clearMatch.Success)
			{
				allSelectablesToGo.Add(clearButton);
			}
			else if (resetMatch.Success)
            {
				allSelectablesToGo.Add(resetButton);
            }
			else if (slMatch.Success)
            {
				allSelectablesToGo.Add(statusLight);
			}
			else
            {
				yield return string.Format("sendtochaterror I do not know what \"{0}\" does. Command interrupted.", curCmdPart);
				yield break;
            }
        }
		for (var x = 0; x < allSelectablesToGo.Count; x++)
        {
			yield return null;
			var curSelectable = allSelectablesToGo[x];
			var willStrike = curSelectable == submitButton && !currentState.SequenceEqual(allExpectedStates[iteractionCount]);
			curSelectable.OnInteract();
			if (curSelectable == submitButton)
				yield return willStrike ? "strike" : "solve";
			if (willStrike)
				yield break;
			while (!interactable)
				yield return string.Format("trycancel Your command has been interrupted after {0} press{1} in the command specified.", x, x == 1 ? "" : "es");
			yield return new WaitForSeconds(0.1f);
        }
	}
}
