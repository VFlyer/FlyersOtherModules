using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using uernd = UnityEngine.Random;

public class MastermindRestrictedCore : MonoBehaviour {

	public KMSelectable resetButton, queryButton;
	public KMSelectable[] possibleSelectables;
	public MeshRenderer[] selectableRenderer, correctRenderer;
	public GameObject correctButtonsAll;
	public KMBombModule modSelf;
	public KMAudio audioKM;
	public KMColorblindMode colorblindMode;
	public TextMesh correctBothDisplay, correctColorDisplay, queryLeftDisplay;
	public TextMesh[] currentColorblindText, correctColorblindText;

	public Color[] colorList;
	public char[] colorblindLetters;
	public bool[] invertColorblindLetter;
	protected List<int[]> allQueries = new List<int[]>();
	protected List<int> queryCorrectColorAndPos = new List<int>(),
		queryCorrectColorNotPos = new List<int>();

	protected int[] currentInputs, correctInputs;
	protected int queriesLeft, maxPossible;
	private static int modCounter = 1;
	protected int loggingID;
	protected bool interactable = true, colorblindDetected;
	// Use this for initialization
	void Awake()
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
	void Start () {
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
        }
	}
	protected virtual void QuickLog(string toLog)
    {
		Debug.LogFormat("[Mastermind Restricted #{0}]: {1}", loggingID, toLog);
	}
	protected virtual void QueryModule()
    {
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
			
			correctBothDisplay.text = queryCorrectColorAndPos[idx].ToString();
			correctColorDisplay.text = queryCorrectColorNotPos[idx].ToString();
			queryLeftDisplay.text = queriesLeft.ToString();
		}
		else
        {
			queriesLeft--;
			// Process correct inputs.
			int correctColors = 0, correctPosandColors = 0;
            for (int x = 0; x < maxPossible; x++) // Start by filtering out each color separately to determine the states of each
            {
				var filteredCorrectInputs = correctInputs.Select(a => a == x ? a : -1);
				var filteredCurrentInputs = currentInputs.Select(a => a == x ? a : -1);
                var filteredWrongIdxes = Enumerable.Range(0, selectableRenderer.Length).Where(a => filteredCorrectInputs.ElementAt(a) == -1);
				int correctInOnePos = 0;
				int correctColorOnly = 0;
				correctInOnePos = Enumerable.Range(0, selectableRenderer.Length)
					.Count(y => filteredCurrentInputs.ElementAt(y) != -1 && filteredCorrectInputs.ElementAt(y) != -1
						&& filteredCorrectInputs.ElementAt(y) == filteredCurrentInputs.ElementAt(y));
				// Count the number of correct positions for that correct color.
				// If both are not -1 and they are equal in value, add 1 for that occurance.
				// Compacted from a for loop
				correctColorOnly = Mathf.Min(filteredWrongIdxes.Count(a => filteredCurrentInputs.ElementAt(a) == x),
					filteredCorrectInputs.Count(a => a != -1) - correctInOnePos);
				// Count the number of correct colors in the wrong positions.
				// This is done by counting the number of colors in their wrong positions, and then capping it based on how many correct colors there should be, minus how many that are actually in the correct positions.
				correctColors += correctColorOnly;
				correctPosandColors += correctInOnePos;
				//Debug.LogFormat("{0},{1}", correctInOnePos, correctColorOnly);
            }


			// Display the result of this query
			correctBothDisplay.text = correctPosandColors.ToString();
			correctColorDisplay.text = correctColors.ToString();
			queryLeftDisplay.text = queriesLeft.ToString();
			allQueries.Add(currentInputs.ToArray());
			queryCorrectColorAndPos.Add(correctPosandColors);
			queryCorrectColorNotPos.Add(correctColors);

			QuickLog(string.Format("Query: [{0}]. Result: {1} correct color(s) in correct position, {2} correct color(s) not in correct position.",
				currentInputs.Select(a => colorblindLetters[a]).Join(), correctPosandColors, correctColors));

			if (currentInputs.SequenceEqual(correctInputs))
            {
				QuickLog(string.Format("FOUR HITS! Module disarmed."));
				StartCoroutine(RevealCorrectAnim());
				audioKM.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
				modSelf.HandlePass();
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
	protected virtual void UpdateCurrentDisplay()
    {
		for (int x = 0; x < selectableRenderer.Length; x++)
        {
			selectableRenderer[x].material.color = colorList[currentInputs[x]];
			if (colorblindDetected)
			{
				currentColorblindText[x].text = colorblindLetters[currentInputs[x]].ToString();
				currentColorblindText[x].color = invertColorblindLetter[currentInputs[x]] ? Color.white : Color.black;
			}
			else
            {
				currentColorblindText[x].text = "";
            }
        }
    }

	protected virtual void ResetModule()
    {
		queryCorrectColorAndPos.Clear();
		queryCorrectColorNotPos.Clear();
		allQueries.Clear();
		currentInputs = new int[selectableRenderer.Length];
		correctInputs = new int[selectableRenderer.Length];
		queriesLeft = 12;
		maxPossible = new[] { colorList.Length, colorblindLetters.Length, invertColorblindLetter.Length }.Min();
		for (int x = 0; x < correctInputs.Length; x++)
        {
			correctInputs[x] = uernd.Range(0, maxPossible);
        }
		QuickLog(string.Format("The correct answer is now [{0}] Get this within 12 distinct queries to disarm the module.", correctInputs.Select(a => colorblindLetters[a]).Join()));
		UpdateCurrentDisplay();
		correctBothDisplay.text = "";
		correctColorDisplay.text = "";
		queryLeftDisplay.text = "";
	}
	protected IEnumerator RevealCorrectAnim()
    {
		Vector3 endPos = new Vector3(0, 0, -0.025f), startPos = new Vector3(0, 0, -0.045f);
		for (int x = 0; x < correctRenderer.Length; x++)
		{
			correctRenderer[x].material.color = colorList[correctInputs[x]];
			if (colorblindDetected)
			{
				correctColorblindText[x].text = colorblindLetters[correctInputs[x]].ToString();
				correctColorblindText[x].color = invertColorblindLetter[correctInputs[x]] ? Color.white : Color.black;
			}
			else
			{
				currentColorblindText[x].text = "";
			}
		}

		for (float x = 0; x <= 1f; x = Mathf.Min(Time.deltaTime + x, 1))
		{
			correctButtonsAll.transform.localPosition = endPos * x + startPos * (1 - x);
			if (x >= 1f) break;
			yield return new WaitForEndOfFrame();
		}
	}
	protected IEnumerator HandleQueryExhaustAnim()
    {
		yield return RevealCorrectAnim();
		yield return new WaitForSeconds(2f);
		Vector3 endPos = new Vector3(0, 0, -0.025f), startPos = new Vector3(0, 0, -0.045f);
		for (float x = 0; x <= 1f; x = Mathf.Min(Time.deltaTime + x, 1))
		{
			correctButtonsAll.transform.localPosition = startPos * x + endPos * (1 - x);
			if (x >= 1f) break;
			yield return null;
		}
		modSelf.HandleStrike();
		ResetModule();
		interactable = true;
	}
	// TP Section Begins Here

	protected virtual IEnumerator TwitchHandleForcedSolve()
    {
		QuickLog("Force solve requested viva TP Handler");
		while (!currentInputs.SequenceEqual(correctInputs))
        {
            for (int x = 0; x < correctInputs.Length; x++)
            {
				while (correctInputs[x] != currentInputs[x])
                {
					possibleSelectables[x].OnInteract();
					yield return null;
                }
            }
        }
		queryButton.OnInteract();
		yield return true;
    }
#pragma warning disable IDE0051 // Remove unused private members
	readonly string TwitchHelpMessage = "Query the current state with \"!{0} query\", or specific colors with \"!{0} query W W W W\"; Set the colors instead of trying to query it with \"!{0} set W W W W\" Available colors are white, magenta, yellow, green, red, blue. Reset the module with \"!{0} reset\". Toggle colorblind mode with \"!{0} colorblind/colourblind\".";
#pragma warning restore IDE0051 // Remove unused private members
	Dictionary<int, string[]> intereptedValues = new Dictionary<int, string[]> {
		{ 0, new string[] { "white", "w", } },
		{ 1, new string[] { "magenta", "m", } },
		{ 2, new string[] { "yellow", "y", } },
		{ 3, new string[] { "green", "g", } },
		{ 4, new string[] { "red", "r", } },
		{ 5, new string[] { "blue", "b", } },
	};
	protected virtual IEnumerator ProcessTwitchCommand(string cmd)
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
			UpdateCurrentDisplay();
        }
		else if (Regex.IsMatch(cmd, @"^set\s*", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
		{
			string modifiedCommand = cmd.Substring(3).Trim().ToLower();
			string[] splittedCommands = modifiedCommand.Split();
			if (splittedCommands.Length > 0 && !string.IsNullOrEmpty(modifiedCommand))
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
						yield return string.Format("sendtochaterror I do not know of a color \"{0}\" on the module. Valid colors are white, magenta, yellow, green, red, blue.", splittedCommands[x]);
						yield break;
					}
				}
				if (cmdInput.Count != 4)
				{
					yield return string.Format("sendtochaterror You provided {0} color(s) for this module when I expected exactly 4.", cmdInput.Count);
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
			if (splittedCommands.Length > 0 && !string.IsNullOrEmpty(modifiedCommand))
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
						yield return string.Format("sendtochaterror I do not know of a color \"{0}\" on the module. Valid colors are white, magenta, yellow, green, red, blue.", splittedCommands[x]);
						yield break;
					}
				}
				if (cmdInput.Count != 4)
                {
					yield return string.Format("sendtochaterror You provided {0} color(s) for this module when I expected exactly 4.", cmdInput.Count);
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
        }
		else if (Regex.IsMatch(cmd, @"^reset$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
			yield return null;
			resetButton.OnInteract();
        }
		yield break;
    }

}
