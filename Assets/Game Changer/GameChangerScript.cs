using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class GameChangerScript : MonoBehaviour {
	public KMSelectable[] gridSelectables;
	public MeshRenderer[] gridRenderers, LEDRenderers;
	public KMSelectable statusLight, submitButton, resetButton;
	public KMBombModule modSelf;
	public KMAudio mAudio;

	bool[] lastFinishedState, expectedState, currentState;
	bool hasStarted = false, moduleSolved, interactable = false;
	static int modIDCnt = 1;
	int modID, iteractionCount = 0;
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
		for (var x = 0; x < LEDRenderers.Length; x++)
		{
			LEDRenderers[x].material.color = Color.black;
		}
		modSelf.OnActivate += GenerateExpectedState;
	}
	void QuickLog(string value)
	{
		Debug.LogFormat("[Game Changer #{0}] {1}", modID, value);
	}

	void SolveModule()
    {
		moduleSolved = true;
		modSelf.HandlePass();
		for (var x = 0; x < LEDRenderers.Length; x++)
		{
			LEDRenderers[x].material.color = Color.green;
		}
	}


	void GenerateExpectedState()
	{
		if (!hasStarted)
		{
			hasStarted = true;
			lastFinishedState = new bool[16];
			for (var x = 0; x < lastFinishedState.Length; x++)
			{
				lastFinishedState[x] = Random.value < 0.5f;
			}
			expectedState = new bool[16];
			currentState = lastFinishedState.ToArray();
			QuickLog("Initial Board State:");
		}
		else
		{
			lastFinishedState = expectedState.ToArray();
			QuickLog(string.Format("Board State after {0} correct iteration{1}:", iteractionCount, iteractionCount == 1 ? "" : "s"));
		}

		for (var x = 0; x < 4; x++)
		{
			QuickLog(lastFinishedState.Skip(x * 4).Take(4).Select(a => a ? "W" : "K").Join(""));
		}

		var adjacentFromBlack = Enumerable.Repeat(false, 1).Concat(lastFinishedState.Take(8));
		var adjacentFromWhite = Enumerable.Repeat(false, 1).Concat(lastFinishedState.TakeLast(8));

		QuickLog(string.Format("White cell birth requires this many surrounding tiles: {0}",
			adjacentFromBlack.Any(a => a) ? Enumerable.Range(0, adjacentFromBlack.Count()).Where(a => adjacentFromBlack.ElementAt(a)).Join(", ") : "(empty)"));
		QuickLog(string.Format("White cell survival requires this many surrounding tiles: {0}",
			adjacentFromWhite.Any(a => a) ? Enumerable.Range(0, adjacentFromWhite.Count()).Where(a => adjacentFromWhite.ElementAt(a)).Join(", ") : "(empty)"));
		for (var x = 0; x < lastFinishedState.Length; x++)
		{
			var whiteNeighborCount = 0;
			for (var deltaX = -1; deltaX <= 1; deltaX++)
			{
				for (var deltaY = -1; deltaY <= 1; deltaY++)
				{
					if ((deltaX != 0 || deltaY != 0) &&
						x % 4 + deltaX >= 0 && x % 4 + deltaX <= 3 &&
						x / 4 + deltaY >= 0 && x / 4 + deltaY <= 3)
					{
						whiteNeighborCount += lastFinishedState[x + deltaX + 4 * deltaY] ? 1 : 0;
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
		QuickLog(string.Format("Expected board state for {0} iteration{1}:", iteractionCount, iteractionCount == 1 ? "" : "s"));
		for (var x = 0; x < 4; x++)
		{
			QuickLog(expectedState.Skip(x * 4).Take(4).Select(a => a ? "W" : "K").Join(""));
		}
		interactable = true;
		UpdateVisuals();
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
		int displayedIterCount;
		for (var z = 0; z < 5; z++)
        {
			displayedIterCount = iteractionCount - 1;
			for (var x = 0; x < LEDRenderers.Length; x++)
			{
				var y = 1;
				for (var u = 0; u < x; u++)
				{
					y *= 2;
				}
				LEDRenderers[x].material.color = displayedIterCount / y % 2 == 0 ? Color.black : Color.white;
			}
			yield return new WaitForSeconds(0.1f);
			displayedIterCount = iteractionCount - 1;
			for (var x = 0; x < LEDRenderers.Length; x++)
			{
				var y = 1;
				for (var u = 0; u < x; u++)
				{
					y *= 2;
				}
				LEDRenderers[x].material.color = displayedIterCount / y % 2 == 0 ? Color.red : Color.white;
			}
			yield return new WaitForSeconds(0.1f);
        }
		displayedIterCount = iteractionCount - 1;
		for (var x = 0; x < LEDRenderers.Length; x++)
		{
			var y = 1;
			for (var u = 0; u < x; u++)
			{
				y *= 2;
			}
			LEDRenderers[x].material.color = displayedIterCount / y % 2 == 1 ? Color.white : Color.black;
		}
	}

	void UpdateVisuals()
    {
        for (var x = 0; x < currentState.Length; x++)
        {
			gridRenderers[x].material.color = currentState[x] ? Color.white : Color.black;
		}
		var displayedIterCount = iteractionCount - 1;
        for (var x = 0; x < LEDRenderers.Length; x++)
        {
			var y = 1;
			for (var u = 0; u < x; u++)
            {
				y *= 2;
            }
			LEDRenderers[x].material.color = displayedIterCount / y % 2 == 1 ? Color.white : Color.black;
        }

    }
	IEnumerator TwitchHandleForcedSolve()
    {
		while (!moduleSolved)
        {
			while (!interactable)
				yield return true;
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
	readonly string TwitchHelpMessage = "Toggle the following cell with \"!{0} [A-D][1-4]\" where columns are labeled A-D from left to right, rows are numbered 1-4 from top to bottom." +
		"Multiple cells can be toggled in one command." +
		"Submit the current board with \"!{0} submit\" or \"!{0} s\", reset the current board with \"!{0} reset\" or \"!{0} r\", or clear the current board with \"!{0} clear\" or \"!{0} c\"." +
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
		for (var x = 0; x < allPossibleCommands.Length; x++)
        {
			var curCmdPart = allPossibleCommands[x];
			Match coordMatch = Regex.Match(curCmdPart, @"^[a-z][0-9]$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
				submitMatch = Regex.Match(curCmdPart, @"^s(ubmit)?$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
				clearMatch = Regex.Match(curCmdPart, @"^c(lear)?$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
				resetMatch = Regex.Match(curCmdPart, @"^r(eset)?$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
			if (coordMatch.Success)
			{
				var curCoord = coordMatch.Value.ToLower().ToCharArray();
				var idxCol = "abcd".IndexOf(curCoord.First());
				var idxRow = "1234".IndexOf(curCoord.Last());
				if (idxCol == -1 || idxRow == -1)
                {
					yield return string.Format("sendtochaterror Your command has been interrupted after {0} press{1} due to a bad coordinate: \"{2}\".",
						x, x == 1 ? "" : "es", curCoord.Join(""));
					yield break;
				}
				yield return null;
				gridSelectables[idxCol + idxRow * 4].OnInteract();
				yield return new WaitForSeconds(0.1f);
			}
			else if (submitMatch.Success)
			{
				yield return null;
				var willStrike = !currentState.SequenceEqual(expectedState);
				yield return willStrike ? "strike" : "solve";
                submitButton.OnInteract();
                if (willStrike || x + 1 >= allPossibleCommands.Length)
					yield break;
				else
				{
					while (!interactable)
						yield return string.Format("trycancel Your command has been interrupted after {0} press{1} in the command specified.", x, x == 1 ? "" : "es");
				}
				yield return new WaitForSeconds(0.1f);
			}
			else if (clearMatch.Success)
			{
				yield return null;
				var curStateToPress = currentState.ToArray();
				for (var y = 0; y < curStateToPress.Length; y++)
				{
					if (curStateToPress[y])
						gridSelectables[y].OnInteract();
				}
				yield return new WaitForSeconds(0.1f);
			}
			else if (resetMatch.Success)
            {
				yield return null;
				resetButton.OnInteract();
				yield return new WaitForSeconds(0.1f);
            }
			else
            {
				yield return string.Format("sendtochaterror I do not know what \"{0}\" does.",curCmdPart);
				yield break;
            }
        }
	}
}
