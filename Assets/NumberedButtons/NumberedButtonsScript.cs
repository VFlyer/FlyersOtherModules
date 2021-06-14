using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using uernd = UnityEngine.Random;

public class NumberedButtonsScript : MonoBehaviour {

	public SampleNumberedKey[] sampleNumberedKeys;
	public KMRuleSeedable ruleSeedCore;
	public KMBombModule modSelf;
	public KMAudio audioSelf;
	public KMBombInfo mBombInfo;
	public GameObject panelAnim, allKeys, sticker, textSolve;
	bool moduleSolved, interactable, hasStruck, isSolving;
	int[] buttonNums = new int[16];
	bool[] correctPresses = new bool[16];
	private List<string> ExpectedButtons;
	List<int> pressedCorrect;
	int[][] selectedRuleseedCorrectNumbers = new int[16][];

	private static int modCnt = 1;
	int modID, reattemptCount;

	void HandleRuleSeedGenerator()
    {
		int[][] RS1CorrectButtonNumbers = new int[][] {
            new int[] { 1, 6, 14, 17, 23, 31, 32, 39, 45, 58, 61, 66 },
            new int[] { 7, 11, 21, 25, 36, 41, 59, 60, 76, 77, 78, 82 },
			new int[] { 2, 4, 9, 15, 19, 24, 28, 35, 42, 56, 63, 69 },
			new int[] { 13, 16, 18, 22, 27, 30, 33, 37, 51, 55, 62, 74 },

			new int[] { 3, 8, 12, 19, 25, 32, 47, 49, 63, 74, 82, 85 },
			new int[] { 5, 13, 21, 32, 41, 42, 59, 64, 72, 76, 84, 85 },
			new int[] { 6, 15, 16, 23, 34, 37, 41, 63, 71, 73, 91, 99 },
			new int[] { 8, 9, 14, 17, 21, 25, 43, 55, 75, 81, 83, 88 },

			new int[] { 10, 24, 36, 48, 53, 61, 64, 77, 80, 87, 92, 95 },
			new int[] { 4, 8, 15, 25, 26, 31, 44, 68, 73, 81, 95, 97 },
			new int[] { 7, 17, 35, 42, 54, 67, 69, 72, 86, 92, 96, 100 },
			new int[] { 5, 14, 22, 26, 34, 48, 55, 57, 86, 87, 94, 96 },

			new int[] { 18, 23, 32, 47, 56, 63, 79, 80, 91, 94, 97, 100 },
			new int[] { 9, 11, 17, 25, 29, 37, 48, 50, 63, 71, 77, 80 },
			new int[] { 1, 7, 12, 18, 21, 25, 32, 45, 57, 62, 81, 96 },
			new int[] { 4, 12, 25, 37, 42, 59, 61, 71, 84, 88, 97, 98 },

		};

		if (ruleSeedCore == null)
        {
			Debug.LogWarningFormat("[Numbered Buttons #{0}]: Rule Seed Handler does not exist! Using default numbers instead.", modID);
			selectedRuleseedCorrectNumbers = RS1CorrectButtonNumbers;
			Debug.LogFormat("<Numbered Buttons #{0}>: All Correct Buttons' Numbers (in reading order on the module):", modID);
			for (var x = 0; x < selectedRuleseedCorrectNumbers.Length; x++)
				Debug.LogFormat("<Numbered Buttons #{0}>: {1}{2}: {3}", modID, "ABCD"[x % 4], "1234"[x / 4], selectedRuleseedCorrectNumbers[x].Join());
			return;
        }
		var RSRandomizer = ruleSeedCore.GetRNG();
		if (RSRandomizer.Seed == 1)
		{
			selectedRuleseedCorrectNumbers = RS1CorrectButtonNumbers;
		}
		else
        {
			int[] numbersTo100 = Enumerable.Range(1, 100).ToArray();

			for (int x = 0; x < 16; x++)
			{
				RSRandomizer.ShuffleFisherYates(numbersTo100);
				selectedRuleseedCorrectNumbers[x] = numbersTo100.Take(12).OrderBy(a => a).ToArray();
			}
        }
		if (RSRandomizer.Seed == 2041162502)
        {
			Debug.LogFormat("[Numbered Buttons #{0}]: Eltrick. Listen to me.", modID);
			Debug.LogFormat("[Numbered Buttons #{0}]: You've made a lot of great modules in the past.", modID);
			Debug.LogFormat("[Numbered Buttons #{0}]: They don't suck even if they look bland.", modID);
			Debug.LogFormat("[Numbered Buttons #{0}]: I know that you don't like the looks of the modules you made.", modID);
			Debug.LogFormat("[Numbered Buttons #{0}]: It's tough. You should not have let one person get into your mind.", modID);
			Debug.LogFormat("[Numbered Buttons #{0}]: You can't delete the past as much as you want anymore. More problems start arising otherwise.", modID);
			Debug.LogFormat("[Numbered Buttons #{0}]: You left a great future for yourself. Just do not let that ruin your perception.", modID);
			Debug.LogFormat("[Numbered Buttons #{0}]: People made ugly things before. But it's how it shapes us. For the future. And the dreams for others to come.", modID);
			Debug.LogFormat("[Numbered Buttons #{0}]: Just... Take care of yourself. You've done well to help the community.", modID);
		}
		Debug.LogFormat("[Numbered Buttons #{0}]: Rule Seed successfully generated instructions with Rule Seed {1}.", modID, RSRandomizer.Seed);
		Debug.LogFormat("<Numbered Buttons #{0}>: All Correct Buttons' Numbers (in reading order on the module):", modID);
		for (var x = 0; x < selectedRuleseedCorrectNumbers.Length; x++)
			Debug.LogFormat("<Numbered Buttons #{0}>: {1}{2}: {3}", modID, "ABCD"[x % 4], "1234"[x / 4], selectedRuleseedCorrectNumbers[x].Join());

	}
	void QuickLog(string toLog)
    {
		Debug.LogFormat("[Numbered Buttons #{0}]: {1}", modID, toLog);
    }
	// Use this for initialization
	void Start () {
		modID = modCnt++;
		HandleRuleSeedGenerator();
		QuickLog(string.Format("Initial Activation: "));
		GenerateCorrectButtons();
		for (int x = 0; x < sampleNumberedKeys.Length; x++)
        {
			int y = x;
			sampleNumberedKeys[x].selfSelectable.OnInteract += delegate {
				sampleNumberedKeys[y].selfSelectable.AddInteractionPunch();
				audioSelf.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, sampleNumberedKeys[y].transform);
				if (!moduleSolved && !sampleNumberedKeys[y].pressed && interactable)
                {
					ProcessButtonInput(y);
                }
				return false;
			};
        }
		RenderButtons();
		StartCoroutine(AnimateOpeningAnim());

		sticker.SetActive(uernd.value < 0.01f);
	}
	void RenderButtons()
    {
		for (int x = 0; x < buttonNums.Length; x++)
		{
			sampleNumberedKeys[x].textMesh.text = buttonNums[x].ToString();
		}
	}

	void ProcessButtonInput(int idx)
    {
		if (idx < 0 || idx >= buttonNums.Length) return;
		var oneNumberedKey = sampleNumberedKeys[idx];

		oneNumberedKey.pressed = true;
        var correctIdxs = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 }.Where(a => correctPresses[a]);
		if (correctIdxs.Contains(idx))
		{
			QuickLog(string.Format("Correctly pressed the button in {0}{1}.", "ABCD"[idx % 4], "1234"[idx / 4]));

			oneNumberedKey.ledRenderer.material = oneNumberedKey.ledMats[2];
			pressedCorrect.Add(idx);
			if (pressedCorrect.ToArray().OrderBy(a => a).SequenceEqual(correctIdxs.OrderBy(a => a)))
			{
				isSolving = true;
				QuickLog(string.Format("All buttons correctly pressed. Module disarmed."));
				StartCoroutine(HandleSolveAnim());
			}
		}
		else if (correctIdxs.Any())
        {
			hasStruck = true;
			QuickLog(string.Format("Strike! Incorrectly pressed the button in {0}{1}.", "ABCD"[idx % 4], "1234"[idx / 4]));
			oneNumberedKey.ledRenderer.material = oneNumberedKey.ledMats[1];
			modSelf.HandleStrike();
			reattemptCount++;
			StartCoroutine(HandleRestartAnim());
			QuickLog(string.Format("Restart #{0}: ", reattemptCount));
			GenerateCorrectButtons();
		}
		else
        {
			isSolving = true;
			QuickLog("Failsafe triggered! Solving module.");
			StartCoroutine(HandleSolveAnim());
		}
	}
	IEnumerator HandleRestartAnim()
    {
		interactable = false;
		yield return new WaitForSeconds(0.5f);
		textSolve.SetActive(false);
		yield return AnimateClosingAnim();
		RenderButtons();
		for (int x = 0; x < sampleNumberedKeys.Length; x++)
		{
			if (sampleNumberedKeys[x].pressed)
			{
				sampleNumberedKeys[x].pressed = false;
				sampleNumberedKeys[x].ledRenderer.material = sampleNumberedKeys[x].ledMats[0];
			}
		}
		yield return AnimateOpeningAnim();
		interactable = true;
    }

	IEnumerator HandleSolveAnim()
    {
		interactable = false;
		for (int x = 0; x < sampleNumberedKeys.Length; x++)
		{
			if (!sampleNumberedKeys[x].pressed)
			{
				yield return new WaitForSeconds(0.05f);
				sampleNumberedKeys[x].pressed = true;
				audioSelf.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, sampleNumberedKeys[x].transform);
				sampleNumberedKeys[x].ledRenderer.material = sampleNumberedKeys[x].ledMats[2];
			}
		}
		yield return new WaitForSeconds(0.5f);
		textSolve.SetActive(uernd.value < 0.01f);
		yield return AnimateClosingAnim();
		moduleSolved = true;
		audioSelf.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
		modSelf.HandlePass();
	}

	IEnumerator AnimateClosingAnim()
    {
		audioSelf.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.WireSequenceMechanism, transform);
		for (float x = 0; x <= 1f; x = Mathf.Min( x + 2 * Time.deltaTime , 1f))
        {
			yield return null;
			allKeys.transform.localPosition = new Vector3(-.0005f, -.01f * x, -.0005f);
			if (x >= 1f) break;
        }
		for (float x = 0; x <= 1f; x = Mathf.Min(x + 2 * Time.deltaTime, 1f))
		{
			yield return null;
			panelAnim.transform.localPosition = new Vector3(0, 0, 5 * (1f - x));
			panelAnim.transform.localScale = new Vector3(1, 1, x);
			if (x >= 1f) break;
		}
		for (int x = 0; x < sampleNumberedKeys.Length; x++)
		{
			sampleNumberedKeys[x].ledRenderer.material = sampleNumberedKeys[x].ledMats[0];
			sampleNumberedKeys[x].textMesh.text = "";
		}
		yield return null;
	}
	IEnumerator AnimateOpeningAnim()
	{
		
		audioSelf.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.WireSequenceMechanism, transform);
		for (float x = 0; x <= 1f; x = Mathf.Min(x + 2 * Time.deltaTime, 1f))
		{
			yield return null;
			panelAnim.transform.localPosition = new Vector3(0, 0, 5 * x);
			panelAnim.transform.localScale = new Vector3(1, 1, 1 -x);
			if (x >= 1f) break;
		}
		for (float x = 0; x <= 1f; x = Mathf.Min(x + 2 * Time.deltaTime, 1f))
		{
			yield return null;
			allKeys.transform.localPosition = new Vector3(-.0005f, -.01f * (1f - x), -.0005f);
			if (x >= 1f) break;
		}
		yield return null;
		interactable = true;
	}
	void GenerateCorrectButtons()
    {
		int attemptsDone = 0;
		pressedCorrect = new List<int>();
		do
		{
			attemptsDone++;
			for (int x = 0; x < buttonNums.Length; x++)
			{
				buttonNums[x] = uernd.Range(1, 101);
				correctPresses[x] = selectedRuleseedCorrectNumbers[x].Contains(buttonNums[x]);
			}
		}
		while (correctPresses.ToList().TrueForAll(a => !a));
		QuickLog(string.Format("Sucessfully generated at least 1 correct button after {0} attempt(s)", attemptsDone));
		QuickLog(string.Format("Numbers on the module:"));
		var debugNums = new List<int>();
		for (int x = 0; x < buttonNums.Length; x++)
        {
			debugNums.Add(buttonNums[x]);
			if (debugNums.Count >= 4)
            {
				QuickLog(string.Format("{0}", debugNums.Join()));
				debugNums.Clear();
			}
        }
		QuickLog(string.Format("Correct buttons to press:"));
		var debugStates = new List<bool>();
		for (int x = 0; x < buttonNums.Length; x++)
		{
			debugStates.Add(correctPresses[x]);
			if (debugStates.Count >= 4)
			{
				QuickLog(string.Format("{0}", debugStates.Select(a => a ? "!" : "X").Join()));
				debugStates.Clear();
			}
		}
        ExpectedButtons = Enumerable.Range(0, 16).Where(a => correctPresses[a]).Select(a => buttonNums[a].ToString()).Distinct().ToList();
	}

	public readonly string TwitchHelpMessage = "Press a button with “!{0} A1 B2 C3 D4...”. Columns are labeled A-D from left to right, rows are labeled 1-4 from top to bottom. Commands may be voided if the module strikes or enters a solve state. \"press\" is optional.";
	readonly string RowIDX = "abcd";
	readonly string ColIDX = "1234";

	IEnumerator TwitchHandleForcedSolve()
    {
		yield return null;
		while (!interactable)
			yield return true;
		for (int x = 0; x < correctPresses.Length; x++)
        {
			if (correctPresses[x])
            {
				sampleNumberedKeys[x].selfSelectable.OnInteract();
				yield return null;
			}				
        }
    }

	IEnumerator ProcessTwitchCommand(string command)
	{
		if (moduleSolved || isSolving)
		{
			yield return "sendtochaterror Are you trying to interact with the module when it is already solved? You might want to think again.";
			yield break;
		}
		string proCmd = command.ToLower().Trim();
		while (!interactable)
		{
			yield return "trycancel";
		}
		if (proCmd.StartsWith("press "))
		{
			proCmd = proCmd.Substring(5).Trim();
		}
		List<int> cordList = new List<int>();
		foreach (string cord in proCmd.Split(' '))
		{
			if (cord.Length == 2 && cord.RegexMatch(@"^[a-d][1-4]$"))
			{
				char[] cordchrs = cord.ToCharArray();
				cordList.Add(RowIDX.IndexOf(cordchrs[0]) + ColIDX.IndexOf(cordchrs[1]) * 4);
			}
			else
			{
				yield return "sendtochaterror I'm sorry but what coordinate is \"" + cord + "\" supposed to be?";
				yield break;
			}
		}
		hasStruck = false;
		for (int x = 0; x < cordList.Count; x++)
		{
			yield return null;
			do
			{
				yield return "trycancel The command has been canceled after " + x + " presses.";
			}
			while (!interactable);
			if (!(isSolving || hasStruck))
			{
				yield return null;
				sampleNumberedKeys[cordList[x]].selfSelectable.OnInteract();
				if (isSolving)
					yield return "solve";
				yield return new WaitForSeconds(0.1f);
			}
			else
			{
				yield return "sendtochat Sorry, the rest of the command has been voided after " + x.ToString() + " presses due to a sudden change in the module.";
				yield break;
			}
		}
		yield return null;
	}
}
