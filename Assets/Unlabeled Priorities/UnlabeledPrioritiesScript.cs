using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UnlabeledPrioritiesScript : MonoBehaviour {

	public KMBombModule modSelf;
	public KMAudio mAudio;
	public KMRuleSeedable ruleSeedCore;
	public KMSelectable[] displaySelectables;
	public TextMesh[] displayedMeshes;
	
	private string[] shuffledQuotes;
	int[] idxSolutionQuotes = new int[4], idxCurrentQuotes = new int[4],
		correctButtonPressOrder = new int[4], currentButtonPressOrder = new int[4],
		currentCycleCnt = new int[4];

	int[][] possibleEachIdxQuotes = new int[4][];

	static int modIDCnt = 1;
	int modID;

	bool modSolved = false, interactable = false, RSBottomToTop = false, RSReverseRequiredPresses = false;

	private string[] allPossibleQuotes = {
		"I’m certain you press\nthis button first.",
		"ALWAYS press this\nbutton first.",
		"I’m pretty sure you\npress this button first.",
		"I am certain this\nis the first one.",
		"I am certain you\npress this one first.",
		"I’m certain you\npress this one first.",
		"No, press THIS one first.",
		"I’m pretty sure this\nis the first one.",
		"You must press\nthis button first.",
		"No really, this\nis the first one.",
		"Press this one.",
		"First button.",
		"I’m pretty sure this\nis the first button.",
		"Maybe you should\npress this button first.",
		"No no, press\nTHIS button first.",
		"I am pretty sure this\nis the first one.",
		"No, press THIS button first.",
		"I am pretty sure you\npress this button first.",
		"Press this button\nfirst, please?",
		"Press me first.",
		"Press this\none first.",
		"This is the\nfirst button.",
		"Press. This. Button. First.",
		"First one.",
		"I am certain you\npress this button first.",
		"THIS, is the first button.",
		"This one is\nthe first button.",
		"I am certain this\nis the first button.",
		"I am certain this\nis the first one.",
		"This really is\nthe first button.",
		"No no no, press\nTHIS button first.",
		"No no no, press\nTHIS one first.",
		"Don’t press this one.",
		"You should press\nthis button first.",
		"I’m certain this\nis the first one.",
		"No really, this\nis the first button.",
		"Press. This. One. First.",
		"Button numero uno.",
		"First, press this one.",
		"Press this button\nfirst, will ya?",
		"Number one.",
		"I am pretty sure this\nis the first button.",
		"Press this button first.",
		"Number 1.",
		"ALWAYS press\nthis one first.",
		"The first button\nis this one.",
		"Press this one\nfirst, will ya?",
		"I’m certain this\nis the first button.",
		"Please press this\nbutton first.",
		"First.",
		"#1.",
		"Will you press\nthis button first?",
		"First, press this button.",
		"Maybe you should\npress this one first.",
		"Press this one\nfirst, please?",
		"Do not press this one.",
		"THIS, is the first one.",
		"No no, press\nTHIS one first."
	};

	void HandleRuleSeed()
	{
		string[] currentQuotes = allPossibleQuotes.ToArray();
		if (ruleSeedCore != null)
		{
			var randomizer = ruleSeedCore.GetRNG();
			if (randomizer.Seed != 1)
			{
				randomizer.ShuffleFisherYates(currentQuotes);
				RSBottomToTop = randomizer.Next(0, 2) == 1;
				RSReverseRequiredPresses = randomizer.Next(0, 2) == 1;
			}
			QuickLog(string.Format("Ruleseed successfully generated with a seed of {0}.", randomizer.Seed));
		}
		else
        {
			QuickLog("Ruleseed handler does not exist. Using default instructions.");
        }
		shuffledQuotes = currentQuotes.ToArray();
		Debug.LogFormat("<Unlabeled Priorities #{0}> All phrases from highest to lowest:", modID);
		for (int x = 0; x < shuffledQuotes.Length; x++)
        {
			Debug.LogFormat("<Unlabeled Priorities #{0}> \"{1}\"", modID, shuffledQuotes[x].Replace("\n", " "));
        }
		Debug.LogFormat("<Unlabeled Priorities #{0}> Phrase priority when disarming: {1}", modID, !RSBottomToTop ? "high to low": "low to high");
		Debug.LogFormat("<Unlabeled Priorities #{0}> Button press priority when disarming: {1}", modID, RSReverseRequiredPresses ? "high to low" : "low to high");
	}
	IEnumerator HandleRevealAnim()
	{
		for (float y = 0; y < 1f; y += Time.deltaTime)
		{
			yield return null;
			for (int x = 0; x < displayedMeshes.Length; x++)
			{
				displayedMeshes[x].color = new Color(1, 1, 1, y);
			}
		}
		for (int x = 0; x < displayedMeshes.Length; x++)
		{
			displayedMeshes[x].color = Color.white;
		}
	}
	IEnumerator HandleStrikeAnim()
    {
		interactable = false;
		for (float y = 0; y < 1f; y += Time.deltaTime)
		{
			yield return null;
			for (int x = 0; x < displayedMeshes.Length; x++)
			{
				displayedMeshes[x].color = new Color(1, 0, 0, 1f - y);
			}
		}
		interactable = true;
		for (int x = 0; x < idxCurrentQuotes.Length; x++)
		{
			currentButtonPressOrder[x] = -1;
			idxCurrentQuotes[x] = -1;
			displayedMeshes[x].text = (RSReverseRequiredPresses ? (4 - correctButtonPressOrder.ElementAt(x)) : (correctButtonPressOrder.ElementAt(x) + 1)).ToString();
		}
		for (float y = 0; y < 1f; y += Time.deltaTime)
		{
			yield return null;
			for (int x = 0; x < displayedMeshes.Length; x++)
			{
				displayedMeshes[x].color = new Color(1, 1, 1, y);
			}
		}
		for (int x = 0; x < displayedMeshes.Length; x++)
		{
			displayedMeshes[x].color = Color.white;
		}
	}
	IEnumerator HandleDisarmAnim()
	{
		for (float y = 0; y < 1f; y += Time.deltaTime)
		{
			yield return null;
			for (int x = 0; x < displayedMeshes.Length; x++)
			{
				displayedMeshes[x].color = new Color(0, 1, 0, 1f - y);
			}
		}
		for (int x = 0; x < displayedMeshes.Length; x++)
		{
			displayedMeshes[x].color = Color.clear;
			displayedMeshes[x].text = "";
		}
	}
	// Use this for initialization
	void Start () {
		modID = modIDCnt++;
		modSelf.OnActivate += delegate
		{ GenerateSolution(); StartCoroutine(HandleRevealAnim()); };
		for (int x = 0; x < displaySelectables.Length; x++)
		{
			int y = x;
			displaySelectables[x].OnInteract += delegate {
				displaySelectables[y].AddInteractionPunch();
				mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, displaySelectables[y].transform);
				if (interactable && !modSolved)
                {
					HandleScreenPress(y);
                }
				return false;
			};
		}

        for (int x = 0; x < displayedMeshes.Length; x++)
        {
			displayedMeshes[x].text = "";
			displayedMeshes[x].color = Color.clear;
        }
		HandleRuleSeed();
	}
	void QuickLog(object stuff)
    {
		Debug.LogFormat("[Unlabeled Priorities #{0}] {1}", modID, stuff.ToString());
    }
    void HandleScreenPress(int idx)
	{
		if (idx < 0 || idx >= 4) return;
		for (int x = 0; x < 4; x++)
		{
			currentCycleCnt[idx] = (currentCycleCnt[idx] + 1) % 4;
			idxCurrentQuotes[idx] = possibleEachIdxQuotes[idx][currentCycleCnt[idx]];
			bool isDupe = false;
			for (int y = 0; y < 4 && !isDupe; y++)
			{
				if (y != idx && idxCurrentQuotes[y] == idxCurrentQuotes[idx]) isDupe = true;
			}
			if (!isDupe) break;
		}
		displayedMeshes[idx].text = shuffledQuotes[idxCurrentQuotes[idx]];

		if (currentButtonPressOrder.All(a => a == -1) || (currentButtonPressOrder[idx] != currentButtonPressOrder.Max() && currentButtonPressOrder[idx] == -1))
			currentButtonPressOrder[idx] = currentButtonPressOrder.Max() + 1;
		if (idxCurrentQuotes.All(a => a != -1))
        {
			QuickLog(string.Format("Attempting to submit the following phrases from top to bottom:"));
			for (int x = 0; x < 4; x++)
			{
                QuickLog(string.Format("{0}: \"{1}\"", x + 1, shuffledQuotes[idxCurrentQuotes[x]].Replace("\n", " ")));
			}
			QuickLog(string.Format("With the screen presses in this order from top to bottom: {0}", currentButtonPressOrder.Select(a => (a+1).ToString()).Join()));
			if (currentButtonPressOrder.SequenceEqual(correctButtonPressOrder) && idxSolutionQuotes.SequenceEqual(idxCurrentQuotes))
			{
				QuickLog(string.Format("That seems right. Module disarmed."));
				mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
				modSolved = true;
				modSelf.HandlePass();
				StartCoroutine(HandleDisarmAnim());
				interactable = false;
			}
			else
            {
				QuickLog(string.Format("That doesn't seems right. Strike incurred."));
				modSelf.HandleStrike();
				StartCoroutine(HandleStrikeAnim());
			}
		}
	}
	void GenerateSolution()
	{
		var idxAll = new int[shuffledQuotes.Length];
		for (int x = 0; x < idxAll.Length; x++)
		{
			idxAll[x] = x;
		}
		idxAll.Shuffle();


		for (int x = 0; x < idxCurrentQuotes.Length; x++)
        {
			idxCurrentQuotes[x] = -1;
			currentButtonPressOrder[x] = -1;
			idxSolutionQuotes[x] = idxAll[x];
		}
		for (int x = 0; x < possibleEachIdxQuotes.Length; x++)
        {
			possibleEachIdxQuotes[x] = idxSolutionQuotes.ToArray().Shuffle();
        }

		var orderedList = new[] { 0, 1, 2, 3 }.OrderBy(a => RSBottomToTop ? -idxSolutionQuotes[a] : idxSolutionQuotes[a]).ToArray();
		correctButtonPressOrder = orderedList;
		for (int x = 0; x < displayedMeshes.Length; x++)
		{
			displayedMeshes[x].text = (RSReverseRequiredPresses ? (4 - orderedList.ElementAt(x)) : (orderedList.ElementAt(x) + 1)).ToString();
		}
		QuickLog(string.Format("Have the following phrases from top to bottom on the displays:"));
		for (int x = 0; x < 4; x++)
		{
			QuickLog(string.Format("{0}: \"{1}\"", x + 1, shuffledQuotes[idxSolutionQuotes[x]].Replace("\n", " ")));
		}
		QuickLog(string.Format("With the screen presses in this order from top to bottom: {0}", correctButtonPressOrder.Select(a => (a + 1).ToString()).Join()));
		interactable = true;
	}

	// Update is called once per frame
	void Update () {

	}

	bool IsPartCorrect(int idxIgnore)
    {
		bool output = true;
		for (int x = 0; x < 4; x++)
			if (x != idxIgnore && idxSolutionQuotes[x] != idxCurrentQuotes[x]) output = false;
		return output;
    }

	IEnumerator TwitchHandleForcedSolve()
	{
		yield return null;
		var willReset = false;
		for (int x = 0; x < 4; x++)
		{
			if (currentButtonPressOrder[x] != -1 && correctButtonPressOrder[x] != currentButtonPressOrder[x])
				willReset = true;
		}
		if (willReset)
		{
			StartCoroutine(HandleStrikeAnim());
			while (!interactable)
				yield return true;
		}
		var orderPress = new[] { 0, 1, 2, 3 }.OrderBy(a => correctButtonPressOrder[a]).ToArray();
		while (!IsPartCorrect(orderPress.Last()))
        {
			for (int x = 0; x < orderPress.Length - 1; x++)
			{
				if (idxCurrentQuotes[orderPress[x]] != idxSolutionQuotes[orderPress[x]])
					for (var y = 0; y < 4; y++)
					{
						if (!interactable) yield break;
							displaySelectables[orderPress[x]].OnInteract();
						yield return null;
						if (idxCurrentQuotes[orderPress[x]] == idxSolutionQuotes[orderPress[x]]) break;
					}
			}
		}
		displaySelectables[orderPress.Last()].OnInteract();
		yield return null;
	}
}
