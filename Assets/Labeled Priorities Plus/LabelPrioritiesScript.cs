using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class LabelPrioritiesScript : MonoBehaviour {

	public KMBombModule modSelf;
	public KMSelectable[] phrasesSelectable;
	public KMAudio mAudio;
	public KMRuleSeedable ruleSeed;
	public TextMesh[] phraseDisplays;

	string[] possibleQuotes = { "Press this button first.",
		"ALWAYS press this button first.",
		"You must press this button first.",
		"You should press this button first.",
		"Press. This. Button. First.",
		"Press this button first, please?",
		"This is the first button.",
		"Number 1.",
		"No really, this is the first one.",
		"THIS, is the first button.",
		"First, press this button.",
		"Maybe you should press this button first.",
		"No no no, press THIS button first.",
		"Please press this button first.",
		"Press me first.",
		"I’m pretty sure this is the first button.",
		"First one.",
		"No, press THIS one first.",
		"The first button is this one.",
		"I’m certain this is the first button.",
		"#1.",
		"First.",
		"Press this button first, will ya?",
		"This really is the first button.",
		"No no, press THIS button first.",
		"Button numero uno.",
		"I’m certain you press this button first.",
		"Press this one first.",
		"This one is the first button.",
		"Maybe you should press this one first.",
		"Press. This. One. First.",
		"No really, this is the first button.",
		"Number one.",
		"THIS, is the first one.",
		"First button.",
		"No no no, press THIS one first.",
		"No, press THIS button first.",
		"ALWAYS press this one first.",
		"Press this one first, please?",
		"I’m pretty sure you press this button first.",
		"Press this one first, will ya?",
		"I’m certain you press this one first.",
		"I’m certain this is the first one.",
		"No no, press THIS one first.",
		"I’m pretty sure this is the first one.",
		"First, press this one.",
		"Don’t press this one.",
		"Press this one."
	};

	List<int> correctInputs, currentInputs;
	bool moduleSolved, interactable;
	static int modIDCnt;
	int modID;
	int[] displayPhraseIdxes;
	int[] ruleSeedIDxBase;
	void HandleRuleSeed()
    {
		var baseList = Enumerable.Range(0, possibleQuotes.Length);
		if (ruleSeed == null)
        {
			ruleSeedIDxBase = baseList.ToArray();
			Debug.LogFormat("[Label Priorities #{0}] Rule seed handler does not exist. Generating default seed...", modID);
		}
		else
        {
			var curRandomizer = ruleSeed.GetRNG();
			ruleSeedIDxBase = curRandomizer.Seed == 1 ? baseList.ToArray() : curRandomizer.ShuffleFisherYates(baseList.ToArray());
			Debug.LogFormat("[Label Priorities #{0}] Rule seed generated with a seed of {1}", modID, curRandomizer.Seed);
		}
		Debug.LogFormat("<Label Priorities #{0}> Arranged phrases from top to bottom:", modID);
		for (int x = 0; x < ruleSeedIDxBase.Length; x++)
        {
			Debug.LogFormat("<Label Priorities #{0}> {1}: {2}", modID, x + 1, possibleQuotes[ruleSeedIDxBase[x]]);
		}
    }

	// Use this for initialization
	void Start () {
		modID = ++modIDCnt;
		correctInputs = new List<int>();
		currentInputs = new List<int>();
		for (var x = 0; x < phrasesSelectable.Length; x++)
		{
			var y = x;
			phrasesSelectable[x].OnInteract += delegate {
				if (interactable && !moduleSolved)
                {
					ProcessInput(y);
                }
				return false;
			};
		}
		HandleRuleSeed();
		modSelf.OnActivate += CalculateSolution;
		for (var x = 0; x < phraseDisplays.Length; x++)
		{
			phraseDisplays[x].text = "";
		}
	}
	void ProcessInput(int curIdx)
    {
		mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, phrasesSelectable[curIdx].transform);
		phrasesSelectable[curIdx].AddInteractionPunch(0.2f);
		if (!currentInputs.Contains(curIdx))
        {
			phraseDisplays[curIdx].color = Color.yellow;
			currentInputs.Add(curIdx);
			if (currentInputs.Count >= correctInputs.Count)
			{
				Debug.LogFormat("[Label Priorities #{0}] The following button presses from top to bottom were made, where 1 is the top button: {1}", modID, currentInputs.Select(a => a + 1).Join());
				if (currentInputs.SequenceEqual(correctInputs) || correctInputs.Count <= 0)
                {
					Debug.LogFormat("[Label Priorities #{0}] That is correct. Module disarmed.", modID);
					moduleSolved = true;
					mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, phrasesSelectable[curIdx].transform);
					modSelf.HandlePass();
					StartCoroutine(AnimateSolveAnim());
				}
				else
                {
					Debug.LogFormat("[Label Priorities #{0}] That is incorrect. Starting over...", modID);
					interactable = false;
					modSelf.HandleStrike();
					StartCoroutine(AnimateRestartAnim());
                }
            }
		}
    }
	IEnumerator AnimateSolveAnim()
	{
		yield return null;
		var textsToDisplay = new[] { "THAT IS", "CORRECT", "MODULE", "DISARMED" };
		for (var x = 0; x < phraseDisplays.Length; x++)
        {
			yield return new WaitForSeconds(0.2f);
			phraseDisplays[x].color = Color.green;
			phraseDisplays[x].text = textsToDisplay[x];
        }
	}
	IEnumerator AnimateRestartAnim()
    {
		yield return null;
		var textsToDisplay = new[] { "THAT IS", "INCORRECT", "STRIKE", "START OVER" };
		for (var x = 0; x < phraseDisplays.Length; x++)
		{
			yield return new WaitForSeconds(0.2f);
			phraseDisplays[x].color = Color.red;
			phraseDisplays[x].text = textsToDisplay[x];
		}
		yield return new WaitForSeconds(0.5f);
		CalculateSolution();
	}
    void CalculateSolution()
    {
        correctInputs.Clear();
        currentInputs.Clear();
        displayPhraseIdxes = Enumerable.Range(0, ruleSeedIDxBase.Length).ToArray().Shuffle().Take(4).ToArray();
        for (var x = 0; x < phraseDisplays.Length; x++)
        {
            phraseDisplays[x].text = possibleQuotes[ruleSeedIDxBase[displayPhraseIdxes[x]]];
            phraseDisplays[x].color = Color.white;
        }
		correctInputs.AddRange(Enumerable.Range(0, 4).OrderBy(a => Array.IndexOf(ruleSeedIDxBase, displayPhraseIdxes[a])).Take(3));

        Debug.LogFormat("[Label Priorities #{0}] The buttons are now showing the following phrases from top to bottom:", modID);
        for (var x = 0; x < displayPhraseIdxes.Length; x++)
        {
            Debug.LogFormat("[Label Priorities #{0}] {1}: {2}", modID, x + 1, possibleQuotes.ElementAt(displayPhraseIdxes[x]));
        }
        Debug.LogFormat("[Label Priorities #{0}] Have the following button presses from top to bottom where 1 is the top button: {1}", modID, correctInputs.Select(a => a + 1).Join());
        interactable = true;
    }

	IEnumerator TwitchHandleForcedSolve()
	{
		yield return null;
		currentInputs.Clear();
		foreach (var anIdx in correctInputs)
		{
			phrasesSelectable[anIdx].OnInteract();
			yield return new WaitForSeconds(0.1f);
		}
	}
#pragma warning disable IDE0051 // Remove unused private members
	readonly string TwitchHelpMessage = "Press a given button with \"!{0} press ### # # #\" where 1 is the top-most button in that module.";
#pragma warning restore IDE0051 // Remove unused private members
	IEnumerator ProcessTwitchCommand(string cmd)
    {
		if (Application.isEditor)
		{
			// Trim the command to simulate an actual Twitch command being processed.
			// Note that Twitch removes leading and trailing spaces in chat messages.
			cmd = cmd.Trim();
		}
		if (!interactable)
		{
			yield return "sendtochat This module (#{1}) is not interactable right now, {0}.";
			yield break;
		}
		Match pressCmd = Regex.Match(cmd, @"^press(\s\d+)+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		if (pressCmd.Success)
		{
			string[] pressStr = pressCmd.Value.Split();
			List<KMSelectable> screenPresses = new List<KMSelectable>();
			for (int x = 1; x < pressStr.Length; x++)
			{
				var curStr = pressStr[x];
				if (Regex.IsMatch(curStr, @"^\d+$"))
				{
					foreach (char aNum in curStr)
					{
						switch (aNum)
						{
							case '1':
								screenPresses.Add(phrasesSelectable[0]);
								break;
							case '2':
								screenPresses.Add(phrasesSelectable[1]);
								break;
							case '3':
								screenPresses.Add(phrasesSelectable[2]);
								break;
							case '4':
								screenPresses.Add(phrasesSelectable[3]);
								break;
							default:
								yield return string.Format("sendtochaterror The given character \"{0}\" does not correspond to a pressable button on the module.", aNum);
								yield break;
						}
					}
				}
			}
			for (var x = 0; x < screenPresses.Count && interactable && !moduleSolved; x++)
			{
				yield return null;
				screenPresses[x].OnInteract();
				if (moduleSolved || !interactable) yield break;
				yield return new WaitForSeconds(0.1f);
			}
		}
	}
}
