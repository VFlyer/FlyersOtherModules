using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class LabeledPrioritiesPlusBase : MonoBehaviour {

	public KMBombModule modSelf;
	public KMBombInfo bombInfo;
	public KMAudio mAudio;
	public KMRuleSeedable ruleSeedCore;
	public KMSelectable[] displaySelectables;
	public TextMesh[] displayedMeshes;
	public Transform backingAll, screensAll;

	private readonly string[] allPossibleQuotes = {
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
		"Press this one first.",
		"This is the first button.",
		"Press. This. Button. First.",
		"First one.",
		"I am certain you\npress this button first.",
		"THIS, is the first button.",
		"This one is the first button.",
		"I am certain this\nis the first button.",
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
		"No no, press\nTHIS one first.",
		"Will you press\nthis one first?",
		"Press this one\nfirst, will you?",
		"Press this button\nfirst, will you?",
	};
	string[] shuffledQuotes;

	protected int modID;
	protected bool modSolved = false, interactable;
	protected virtual void QuickLog(string toLog, params object[] args)
	{
		Debug.LogFormat("[{0} #{1}] {2}", modSelf.ModuleDisplayName, modID, string.Format(toLog, args));
	}
	protected virtual void QuickLogDebug(string toLog, params object[] args)
	{
		Debug.LogFormat("<{0} #{1}> {2}", modSelf.ModuleDisplayName, modID, string.Format(toLog, args));
	}
	protected virtual void HandleRuleSeed()
    {
		string[] currentQuotes = allPossibleQuotes.ToArray();
		if (ruleSeedCore != null)
		{
			var randomizer = ruleSeedCore.GetRNG();
			randomizer.ShuffleFisherYates(currentQuotes);
			QuickLog("Successfully used ruleseed {0} to shuffle phrases.", randomizer.Seed);
		}
		else
		{
			QuickLog("Ruleseed handler does not exist. Using rule seed 1 to shuffle phrases.");
			var randomizer = new MonoRandom(1);
			randomizer.ShuffleFisherYates(currentQuotes);
		}
		shuffledQuotes = currentQuotes.ToArray();
		QuickLogDebug("<Labeled Priorities Plus #{0}> All phrases from top to bottom:", modID);
	}

	protected virtual void Start()
    {
		PrepModule();
        for (var x = 0; x < displaySelectables.Length; x++)
		{
			var y = x;
			displaySelectables[x].OnInteract += () => {
				HandlePress(y);
				return false;
			};
        }
		modSelf.OnActivate += () => { StartCoroutine(HandleRevealAnim()); };
    }
	protected virtual void PrepModule()
    {

    }
	protected virtual void HandlePress(int idx)
    {
		if (!interactable || modSolved) return;
    }

	protected IEnumerator HandleRevealAnim(bool keepColors = false)
	{
		for (int x = 0; x < displayedMeshes.Length; x++)
		{
			for (float y = 0; y < 1f; y += Time.deltaTime * 5)
			{
				yield return null;
				displayedMeshes[x].color = keepColors ? new Color(displayedMeshes[x].color.r, displayedMeshes[x].color.g, displayedMeshes[x].color.b, y) :
					 new Color(1, 1, 1, y);
			}
			displayedMeshes[x].color = keepColors ? new Color(displayedMeshes[x].color.r, displayedMeshes[x].color.g, displayedMeshes[x].color.b, 1) :
					 new Color(1, 1, 1, 1);
		}
	}
	protected IEnumerator HandleRevealAnimReverse(bool keepColors = false)
	{
		for (int x = displayedMeshes.Length - 1; x >= 0; x--)
		{
			for (float y = 0; y < 1f; y += Time.deltaTime * 5)
			{
				yield return null;
				displayedMeshes[x].color = keepColors ? new Color(displayedMeshes[x].color.r, displayedMeshes[x].color.g, displayedMeshes[x].color.b, y) :
					 new Color(1, 1, 1, y);
			}
			displayedMeshes[x].color = keepColors ? new Color(displayedMeshes[x].color.r, displayedMeshes[x].color.g, displayedMeshes[x].color.b, 1) :
					 new Color(1, 1, 1, 1);
		}
	}

#pragma warning disable IDE0051 // Remove unused private members
	readonly string TwitchHelpMessage = "Press a given button with \"!{0} press ### # # #\" where 1 is the top-most button in that module. Append \"slow\" or \"veryslow\" onto the command to make the presses go slower or \"instant\" to make the button presses instant.";
#pragma warning restore IDE0051 // Remove unused private members
	protected IEnumerator ProcessTwitchCommand(string cmd)
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
		Match pressCmd = Regex.Match(cmd, @"^press(\s\d+)+(\s(slow(er)?|veryslow|instant))?", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		if (pressCmd.Success)
		{
			string[] pressStr = pressCmd.Value.Split();
			bool requireDelay = true;
			float delayAmount = 0.1f;
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
								screenPresses.Add(displaySelectables[0]);
								break;
							case '2':
								screenPresses.Add(displaySelectables[1]);
								break;
							case '3':
								screenPresses.Add(displaySelectables[2]);
								break;
							case '4':
								screenPresses.Add(displaySelectables[3]);
								break;
							default:
								yield return string.Format("sendtochaterror The given character \"{0}\" does not correspond to a pressable button on the module.", aNum);
								yield break;
						}
					}
				}
			}
			var lastString = pressStr.LastOrDefault();
			switch ((lastString ?? "").ToLowerInvariant())
			{
				case "slow":
					delayAmount = 1f;
					break;
				case "slower":
					delayAmount = 2f;
					break;
				case "veryslow":
					delayAmount = 3f;
					break;
				case "instant":
					requireDelay = false;
					break;
			}
			for (var x = 0; x < screenPresses.Count && interactable && !modSolved; x++)
			{
				yield return null;
				screenPresses[x].OnInteract();
				if (modSolved || !interactable)
					yield break;
				else if (requireDelay)
					yield return string.Format("trywaitcancel {0} Your button press has been canceled after {1} press{2}.", delayAmount, x + 1, x == 0 ? "" : "es");
			}
		}

		yield break;
	}

}
