using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using rnd = UnityEngine.Random;
using KeepCoding;
public class LabeledPrioritiesPlusScript : MonoBehaviour {

	public KMBombModule modSelf;
	public KMBombInfo bombInfo;
	public KMAudio mAudio;
	public KMRuleSeedable ruleSeedCore;
	public KMSelectable[] displaySelectables;
	public TextMesh[] displayedMeshes;
	public Transform backingAll, screensAll;

	private string[] shuffledQuotes;
	int[] idxSolutionQuotes, idxCurrentQuotes = new int[4],
		currentCycleCnt, selectedDynamicScores, relabeledStageOrder,
		authorDynamicScoring = new[] { 0, 2, 6, 7 };

	int[][] possibleEachIdxQuotes = new int[4][], mislabeledPrioritiesGrid = new int[][] {
        new[] {0, 1, 2, 3},
        new[] {3, 2, 1, 0},
        new[] {1, 0, 3, 2},
        new[] {2, 3, 0, 1},
		};
	List<int> correctButtonPressOrder = new List<int>(), currentButtonPressOrder = new List<int>(), selectedVariantIdxes = new List<int>();

	static int modIDCnt = 1;
	int modID, idxVariantGenerated, dynamicScoreToGive;

	bool modSolved = false, TPFlipPressOrder = false, interactable = false, RSReversePriorityLabeled = false, RSReversePriorityUnlabeled = false;
	public bool softResetLabeledUnlabeled = false;
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
	FlyersOtherSettings universalSettings = new FlyersOtherSettings();
	// Mission overrides.
	static int[] variantsCountAll;
	static bool overrideSuccessful;

	void Awake()
    {
		try
		{
			ModConfig<FlyersOtherSettings> universalConfig = new ModConfig<FlyersOtherSettings>("FlyersOtherSettings");
			universalSettings = universalConfig.Settings;
			universalConfig.Settings = universalSettings;

            selectedDynamicScores = universalSettings.UseAuthorSuggestedDynamicScoring ? authorDynamicScoring
			: new[]
			{ universalSettings.LPPLabeledPrioritiesTPScore, universalSettings.LPPUnlabeledPrioritiesTPScore,
				universalSettings.LPPRelabeledPrioritiesTPScore, universalSettings.LPPMislabeledPrioritiesTPScore };
			if (universalSettings.LPPEnableLabeledPriorities)
				selectedVariantIdxes.Add(0);
			if (universalSettings.LPPEnableUnlabeledPriorities)
				selectedVariantIdxes.Add(1);
			if (universalSettings.LPPEnableRelabeledPriorities)
				selectedVariantIdxes.Add(2);
			
			if (universalSettings.LPPEnableMislabeledPriorities)
				selectedVariantIdxes.Add(3);
			
		}
		catch
		{
			Debug.LogFormat("<Labeled Priorities Plus> Settings do not work as intended! Using default settings instead.");
			selectedVariantIdxes.AddRange(new[] { 0, 1, 2, 3 });
			selectedDynamicScores = authorDynamicScoring;
			
		}
		Debug.LogFormat("<Labeled Priorities Plus> Rollable Variants: {0}", selectedVariantIdxes.Select(a => new[] { "Labeled", "Unlabeled", "Relabeled", "Mislabeled" }.ElementAtOrDefault(a)).Join(", "));
		if (variantsCountAll != null)
			variantsCountAll = null;
	}
	void HandleRuleSeed()
	{
		string[] currentQuotes = allPossibleQuotes.ToArray();
		if (ruleSeedCore != null)
		{
			var randomizer = ruleSeedCore.GetRNG();
			randomizer.ShuffleFisherYates(currentQuotes);
			RSReversePriorityLabeled = randomizer.Next(0, 2) != 1;
			RSReversePriorityUnlabeled = randomizer.Next(0, 2) != 1;
			QuickLog(string.Format("Ruleseed successfully generated with a seed of {0}.", randomizer.Seed));
			if (randomizer.Seed == 1)
				relabeledStageOrder = new[] { 0, 1, 2, 3, -1 };
			else
            {
				relabeledStageOrder = new[] { 0, 2, 2, 2, -1 };
				// Do stuff with Relabeled Priorities' stage ordering for the first 3 non-initial stages. TBD.
            }
		}
		else
        {
			QuickLog("Ruleseed handler does not exist. Using default instructions.");
			var randomizer = new MonoRandom(1);
			randomizer.ShuffleFisherYates(currentQuotes);
			RSReversePriorityLabeled = randomizer.Next(0, 2) != 1;
			RSReversePriorityUnlabeled = randomizer.Next(0, 2) != 1;
			relabeledStageOrder = new[] { 0, 1, 2, 3, -1 };
		}
		shuffledQuotes = currentQuotes.ToArray();
		Debug.LogFormat("<Labeled Priorities Plus #{0}> All phrases from top to bottom:", modID);
		for (int x = 0; x < shuffledQuotes.Length; x++)
        {
			Debug.LogFormat("<Labeled Priorities Plus #{0}> {2}: \"{1}\"", modID, shuffledQuotes[x].Replace("\n", " "), x + 1);
        }
		Debug.LogFormat("<Labeled Priorities Plus #{0}> Phrase priority when disarming Labeled Priorities: {1}", modID, RSReversePriorityLabeled ? "low to high" : "high to low");
		Debug.LogFormat("<Labeled Priorities Plus #{0}> Phrase priority when disarming Unlabeled Priorities: {1}", modID, RSReversePriorityUnlabeled ? "low to high" : "high to low");
	}
	void QuickLog(object stuff)
	{
		Debug.LogFormat("[Labeled Priorities Plus #{0}] {1}", modID, stuff.ToString());
	}
	IEnumerator HandleRevealAnim(bool keepColors = false)
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
	IEnumerator HandleRevealAnimReverse(bool keepColors = false)
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
	// Mission Overrides begin here.
	void TryOverrideMission()
    {
		try
        {
			var missionID = Application.isEditor ? "freeplay" : Game.Mission.ID;
			switch (missionID)
            {
				case "freeplay":
					Debug.LogFormat("<Labeled Priorities Plus #{0}> MISSION DETECTED AS FREEPLAY. NOT OVERRIDING SETTINGS.", modID);
					return;
				case "mod_missionpack_VFlyer_mission23rdProblem":
					selectedVariantIdxes.Clear();
                    selectedVariantIdxes.AddRange(Enumerable.Range(0, 2));
					Debug.LogFormat("<Labeled Priorities Plus #{0}> DETECTED MISSION BY ID \"Flyer's 23rd Problem\", OVERRIDING VARIANTS.", modID);
					return;
				case "mod_missionpack_VFlyer_mission47thWrathFlyer":
					if (variantsCountAll == null)
						variantsCountAll = new int[] { 1, 1, 1, 1 };
					selectedVariantIdxes.Clear();
					selectedVariantIdxes.AddRange(Enumerable.Range(0, 4).Where(a => variantsCountAll[a] > 0));
					overrideSuccessful = true;
					Debug.LogFormat("<Labeled Priorities Plus #{0}> DETECTED MISSION BY ID \"Flyer's 47th Wrath\", OVERRIDING VARIANTS.", modID);
					return;
			}
			var desc = Game.Mission.Description ?? "";
			Match regexMatchCountVariants = Regex.Match(desc, @"\[LPPOverride\]\s([0-9]+,\s?){3}[0-9]+"),
				regexMatchRandomizeCertainVariants = Regex.Match(desc, @"\[LPPOverride\]\s((Re|Un|Mis)?-,\s?)*(Re|Un|Mis)?-");
			if (regexMatchRandomizeCertainVariants.Success)
            {
				var matchedValue = regexMatchRandomizeCertainVariants.Value;
				var sectionMatchVariantsOnly = Regex.Match(matchedValue, @"((Re|Un|Mis)?-,\s?)*(Re|Un|Mis)?-");
				selectedVariantIdxes.Clear();
				foreach (string section in sectionMatchVariantsOnly.Value.Split( new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
					switch (section)
                    {
						case "-":
							selectedVariantIdxes.Add(0);
							break;
						case "Un-":
							selectedVariantIdxes.Add(1);
							break;
						case "Re-":
							selectedVariantIdxes.Add(2);
							break;
						case "Mis-":
							selectedVariantIdxes.Add(3);
							break;
                    }
                }
				Debug.LogFormat("<Labeled Priorities Plus #{0}> MISSION OVERRIDE BY DESCRIPTION. LOCKING TO THESE VARIANTS: {1}", modID,
					Enumerable.Range(0, 4).Where(a => selectedVariantIdxes.Contains(a)).Select(a => new[] { "Labeled", "Unlabeled", "Relabeled", "Mislabeled" }[a] + "(Weight = " + selectedVariantIdxes.Count(b => b == a) + ")").Join(", "));
			}
			else if (regexMatchCountVariants.Success)
            {
				if (variantsCountAll == null)
                {
					variantsCountAll = new int[4];
					var matchedValue = regexMatchCountVariants.Value;
					var portionMatched = Regex.Match(matchedValue, @"([0-9]+,\s?){3}[0-9]+");
					var resultingMatch = portionMatched.Value.Where(a => !char.IsWhiteSpace(a)).Join("");
                    string[] array = resultingMatch.Split(',');
                    for (int i = 0; i < array.Length; i++)
                    {
                        string aValue = array[i];
						if (!int.TryParse(aValue, out variantsCountAll[i]))
                        {
							Debug.LogFormat("<Labeled Priorities Plus #{0}> VALUE DETECTED AS INPROCESSABLE. CANCELING OVERRIDE.", modID, variantsCountAll.Join(", "));
							return;
						}
                    }
					overrideSuccessful = true;
					Debug.LogFormat("<Labeled Priorities Plus #{0}> MISSION OVERRIDE BY DESCRIPTION. MAX OF EACH OF THESE VARIANTS: {1}", modID, variantsCountAll.Join(", "));
				}
				else
					Debug.LogFormat("<Labeled Priorities Plus #{0}> MISSION OVERRIDE BY DESCRIPTION. USING EXISTING ARRAY.", modID);
				if (overrideSuccessful)
				{
					selectedVariantIdxes.Clear();
					selectedVariantIdxes.AddRange(Enumerable.Range(0, 4).Where(a => variantsCountAll[a] > 0));
				}
				else
                {
					Debug.LogFormat("<Labeled Priorities Plus #{0}> INSTANCE MARKED NOT SUCCESSFUL. CANCELING.", modID, variantsCountAll.Join(", "));
				}
			}
			Debug.LogFormat("<Labeled Priorities Plus #{0}> UNABLE TO OVERRIDE BY MISSION ID AND DESCRIPTION.", modID);
		}
		catch (Exception error)
        {
			Debug.LogErrorFormat("<Labeled Priorities Plus #{0}> EXCEPTION OCCURED. USING SETTINGS INSTEAD. PLEASE SEEK OUT THE CREATOR ON HOW TO FIX THIS.", modID);
			Debug.LogException(error);
			selectedVariantIdxes.Clear();
			if (universalSettings.LPPEnableLabeledPriorities)
				selectedVariantIdxes.Add(0);
			if (universalSettings.LPPEnableUnlabeledPriorities)
				selectedVariantIdxes.Add(1);
			if (universalSettings.LPPEnableRelabeledPriorities)
				selectedVariantIdxes.Add(2);
			if (universalSettings.LPPEnableMislabeledPriorities)
				selectedVariantIdxes.Add(3);
		}
    }
	// Use this for initialization

	void Start () {
		modID = modIDCnt++;
        for (int x = 0; x < displayedMeshes.Length; x++)
        {
			displayedMeshes[x].text = "";
			displayedMeshes[x].color = Color.clear;
        }
        HandleRuleSeed();
		TryOverrideMission();
		if (!selectedVariantIdxes.Any())
			selectedVariantIdxes.Add(rnd.Range(0, 4));
		idxVariantGenerated = selectedVariantIdxes.PickRandom();
		dynamicScoreToGive = selectedDynamicScores[idxVariantGenerated];
		switch (idxVariantGenerated)
        {
			case 0:
				QuickLog(string.Format("Labeled Priorities has been rolled for this module."));
				PrepLabeledPriorities();
				break;
			case 1:
				QuickLog(string.Format("Unlabeled Priorities has been rolled for this module."));
				PrepUnlabeledPriorities();
				break;
			case 2:
				QuickLog(string.Format("Relabeled Priorities has been rolled for this module."));
				PrepRelabeledPriorities();
				break;
			case 3:
				QuickLog(string.Format("Mislabeled Priorities has been rolled for this module."));
				PrepMislabeledPriorities();
				break;
			default:
				QuickLog(string.Format("Unlabeled Priorities has been rolled for this module."));
				PrepUnlabeledPriorities();
				break;

        }

	}
	// Labeled Priorities
	void PrepLabeledPriorities()
    {
		modSelf.OnActivate += delegate
		{ GenerateSolutionLabeled(); StartCoroutine(HandleRevealAnimReverse()); };
		for (int x = 0; x < displaySelectables.Length; x++)
		{
			int y = x;
			displaySelectables[x].OnInteract += delegate {
				displaySelectables[y].AddInteractionPunch(0.2f);
				mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, displaySelectables[y].transform);
				if (interactable && !modSolved)
				{
					HandleScreenPressLabeled(y);
				}
				return false;
			};
			displayedMeshes[x].transform.localEulerAngles = (Vector3.up * 180) + (Vector3.right * 90);
		}
		backingAll.localEulerAngles = Vector3.up * 180;
		screensAll.localEulerAngles = Vector3.up * 180;
		if (variantsCountAll != null && overrideSuccessful)
			variantsCountAll[0]--;
	}
	void GenerateSolutionLabeled()
    {
		var idxAll = Enumerable.Range(0,shuffledQuotes.Length).ToArray().Shuffle();
        for (int x = 0; x < possibleEachIdxQuotes.Length; x++)
        {
			var anItem = new[] { idxAll[x] };
			possibleEachIdxQuotes[x] = anItem;
			displayedMeshes[x].text = shuffledQuotes[anItem.Single()];
		}
        var orderedList = Enumerable.Range(0, 4).OrderBy(a => RSReversePriorityLabeled ? -possibleEachIdxQuotes[a].Single() : possibleEachIdxQuotes[a].Single()).ToArray();
		correctButtonPressOrder.Clear();
		correctButtonPressOrder.AddRange(orderedList.Take(3));
		QuickLog(string.Format("The screens show the following from top to bottom:"));
		for (int x = 3; x >= 0; x--)
		{
			QuickLog(string.Format("{0}: \"{1}\"", 4 - x, shuffledQuotes[possibleEachIdxQuotes[x].Single()].Replace("\n", " ")));
		}
		QuickLog(string.Format("Have the screen presses in this order where 1 is the top-most screen: {0}", correctButtonPressOrder.Select(a => (4 - a).ToString()).Join()));
		interactable = true;
	}
	void HandleScreenPressLabeled(int idx)
	{
		if (idx < 0 || idx >= 4) return;
		if (!currentButtonPressOrder.Contains(idx))
		{
			currentButtonPressOrder.Add(idx);
			displayedMeshes[idx].text = (currentButtonPressOrder.IndexOf(idx) + 1).ToString();
		}
		if (currentButtonPressOrder.Count >= correctButtonPressOrder.Count)
		{
			QuickLog(string.Format("Attempting to submit the screen presses in this order from top to bottom: {0}", currentButtonPressOrder.Select(a => (4 - a).ToString()).Join()));
			if (currentButtonPressOrder.SequenceEqual(correctButtonPressOrder))
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
				StartCoroutine(HandleStrikeAnimLabeled());
			}
		}
	}
	IEnumerator HandleStrikeAnimLabeled()
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
		currentButtonPressOrder.Clear();
		if (softResetLabeledUnlabeled)
			for (int x = 0; x < possibleEachIdxQuotes.Length; x++)
			{
				displayedMeshes[x].text = displayedMeshes[x].text = shuffledQuotes[possibleEachIdxQuotes[x].Single()];
			}
		else
			GenerateSolutionLabeled();
		yield return HandleRevealAnimReverse();
	}
	// Unlabeled Priorities
	void PrepUnlabeledPriorities()
    {
		modSelf.OnActivate += delegate
		{ GenerateSolutionUnlabeled(); StartCoroutine(HandleRevealAnim()); };
		for (int x = 0; x < displaySelectables.Length; x++)
		{
			int y = x;
			displaySelectables[x].OnInteract += delegate {
				displaySelectables[y].AddInteractionPunch(0.2f);
				mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, displaySelectables[y].transform);
				if (interactable && !modSolved)
				{
					HandleScreenPressUnlabeled(y);
				}
				return false;
			};
		}
		TPFlipPressOrder = true;
		if (variantsCountAll != null && overrideSuccessful)
			variantsCountAll[1]--;
	}
    void HandleScreenPressUnlabeled(int idx)
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
        if (idxCurrentQuotes.All(a => a != -1))
        {
            QuickLog(string.Format("Attempting to submit the following phrases from top to bottom:"));
            for (int x = 0; x < 4; x++)
            {
                QuickLog(string.Format("{0}: \"{1}\"", x + 1, shuffledQuotes[idxCurrentQuotes[x]].Replace("\n", " ")));
            }
            if (idxSolutionQuotes.SequenceEqual(idxCurrentQuotes))
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
                StartCoroutine(HandleStrikeAnimUnlabeled());
            }
        }
    }
	IEnumerator HandleStrikeAnimUnlabeled()
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
		if (softResetLabeledUnlabeled)
			for (int x = 0; x < idxCurrentQuotes.Length; x++)
			{
				idxCurrentQuotes[x] = -1;
				displayedMeshes[x].text = (correctButtonPressOrder.ElementAt(x) + 1).ToString();
			}
		else
			GenerateSolutionUnlabeled();
		yield return HandleRevealAnim();
	}
	void GenerateSolutionUnlabeled()
	{
		var idxAll = new int[shuffledQuotes.Length];
		for (int x = 0; x < idxAll.Length; x++)
		{
			idxAll[x] = x;
		}
		idxAll.Shuffle();

		idxSolutionQuotes = new int[4];
		currentCycleCnt = new int[4];
		for (int x = 0; x < idxCurrentQuotes.Length; x++)
        {
			idxCurrentQuotes[x] = -1;
			idxSolutionQuotes[x] = idxAll[x];
		}
		for (int x = 0; x < possibleEachIdxQuotes.Length; x++)
        {
			possibleEachIdxQuotes[x] = idxSolutionQuotes.ToArray().Shuffle();
        }

        var priorityOrder = Enumerable.Range(0, 4).OrderBy(a => RSReversePriorityUnlabeled ? -idxSolutionQuotes[a] : idxSolutionQuotes[a]);
		var shownPriority = Enumerable.Range(0, 4).OrderBy(a => priorityOrder.ElementAt(a));
		for (int x = 0; x < displayedMeshes.Length; x++)
		{
			displayedMeshes[x].text = (shownPriority.ElementAt(x) + 1).ToString();
		}
		QuickLog(string.Format("With the screen showing the digits from top to bottom: {0}", shownPriority.Select(a => (a + 1).ToString()).Join()));
		QuickLog(string.Format("Have the following phrases from top to bottom on the displays:"));
		for (int x = 0; x < 4; x++)
		{
			QuickLog(string.Format("{0}: \"{1}\"", x + 1, shuffledQuotes[idxSolutionQuotes[x]].Replace("\n", " ")));
		}
		interactable = true;
	}
	// Relabeled Priorities
	List<int> rememberedIdxPhrases = new List<int>(), rememberedIdxPositions = new List<int>();
	int curStageCnt = 0;
	void PrepRelabeledPriorities()
    {
		modSelf.OnActivate += delegate { GenerateSolutionRelabeled(); StartCoroutine(HandleRevealAnimReverse()); };

		for (int x = 0; x < displaySelectables.Length; x++)
		{
			int y = x;
			displaySelectables[x].OnInteract += delegate {
				displaySelectables[y].AddInteractionPunch(0.2f);
				mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, displaySelectables[y].transform);
				if (interactable && !modSolved)
				{
					HandleScreenPressRelabeled(y);
				}
				return false;
			};
			displayedMeshes[x].transform.localEulerAngles = (Vector3.up * 180) + (Vector3.right * 90);
		}

		backingAll.localEulerAngles = Vector3.up * 90;
		screensAll.localEulerAngles = Vector3.up * 180;
		if (variantsCountAll != null && overrideSuccessful)
			variantsCountAll[2]--;
	}
	void GenerateSolutionRelabeled()
	{
		var idxAll = new int[shuffledQuotes.Length];
		for (int x = 0; x < idxAll.Length; x++)
		{
			idxAll[x] = x;
		}
		QuickLog("");
		if (curStageCnt != 0)
			QuickLog(string.Format("Stage {0}:", curStageCnt));
		else
			QuickLog("Initial Stage:");
		var unpressedIdxes = Enumerable.Range(0, 4).Where(a => !rememberedIdxPositions.Contains(a));
		switch (relabeledStageOrder.ElementAtOrDefault(curStageCnt))
        {
			case -1: // The final stage
				{
					int[] allRememberedIdxPhrases = rememberedIdxPhrases.ToArray().Shuffle();
					for (int x = 0; x < idxCurrentQuotes.Length; x++)
					{
						idxCurrentQuotes[x] = allRememberedIdxPhrases[x];
						displayedMeshes[x].text = shuffledQuotes[allRememberedIdxPhrases[x]];
					}
					QuickLog(string.Format("The following phrases are now shown from top to bottom:"));
					for (int x = 3; x >= 0; x--)
					{
						QuickLog(string.Format("{0}: \"{1}\"", 4 - x, shuffledQuotes[idxCurrentQuotes[x]].Replace("\n", " ")));
					}
					correctButtonPressOrder.Clear();
					var combinedIdxes = Enumerable.Range(0, 4).Except(rememberedIdxPositions).Union(rememberedIdxPositions);
					QuickLog(string.Format("Your remembered phrases from the previous stages:"));
					for (var x = 0; x < rememberedIdxPhrases.Count; x++)
					{
						QuickLog(string.Format("{0}: \"{1}\"", x + 1, shuffledQuotes[rememberedIdxPhrases[x]].Replace("\n", " ")));
					}
					QuickLog(string.Format("Your remembered positions from top to bottom, including unassigned: {0}", combinedIdxes.Select(a => 4 - a).Join()));
					var assignedOrderIdx = new int[4];
					for (var x = 0; x < assignedOrderIdx.Length; x++)
					{
						assignedOrderIdx[Array.IndexOf(idxCurrentQuotes, rememberedIdxPhrases[x])] = combinedIdxes.ElementAt(x);
					}
					//Debug.Log(assignedOrderIdx.Join());
					//Debug.Log(Enumerable.Range(0, 4).OrderBy(a => assignedOrderIdx.ElementAtOrDefault(a)).Join());
					correctButtonPressOrder.AddRange(Enumerable.Range(0, 4).OrderBy(a => assignedOrderIdx[a]).Reverse());
					//Debug.Log(Enumerable.Range(0, 4).OrderBy(a => combinedIdxes.ElementAt(a)).Join());
					QuickLog(string.Format("Press the screens from top to bottom in this order from left to right: {0}", correctButtonPressOrder.Any() ? correctButtonPressOrder.Select(a => 4 - a).Join() : "?"));
				}
				break;
			case 0: // Initial stage
                {
					rememberedIdxPositions.Clear();
					rememberedIdxPhrases.Clear();
					var randomlySelectedIdx = idxAll.PickRandom();
					for (int x = 0; x < idxCurrentQuotes.Length; x++)
					{
						idxCurrentQuotes[x] = randomlySelectedIdx;
						displayedMeshes[x].text = shuffledQuotes[randomlySelectedIdx];
					}
					QuickLog(string.Format("Press any screen to start disarming. Remember the following phrase, \"{0}\"", shuffledQuotes[randomlySelectedIdx].Replace("\n", " ")));
				}
				break;
			case 1:
                {
					var lastRememberedIdx = rememberedIdxPhrases.Last();

					var otherIdxPhrases = idxAll.Where(a => !rememberedIdxPhrases.Contains(a));
					var shuffledArray = otherIdxPhrases.ToArray().Shuffle();
                    for (int x = 0; x < idxCurrentQuotes.Length; x++)
                    {
                        idxCurrentQuotes[x] = shuffledArray[x];
                        displayedMeshes[x].text = shuffledQuotes[shuffledArray[x]];
                    }
					QuickLog(string.Format("The following phrases are now shown from top to bottom:"));
					for (int x = 3; x >= 0; x--)
					{
						QuickLog(string.Format("{0}: \"{1}\"", 4 - x, shuffledQuotes[idxCurrentQuotes[x]].Replace("\n", " ")));
					}
					int[] arrayDistance = new int[4];
                    for (int x = 0; x < idxCurrentQuotes.Length; x++)
                    {
						arrayDistance[x] = Mathf.Abs(idxCurrentQuotes[x] - lastRememberedIdx);
                    }
					Debug.LogFormat("<Labeled Priorities Plus #{0}> Distances for this stage: {1}", modID, arrayDistance.Reverse().Join(", "));
					correctButtonPressOrder.Clear();
					correctButtonPressOrder.AddRange(unpressedIdxes.Where(a => arrayDistance[a] <= unpressedIdxes.Select(x => arrayDistance[x]).Min()));
					QuickLog(string.Format("Valid screens to press in this stage where 1 is the top-most screen: {0}", correctButtonPressOrder.Select(a => 4 - a).Join()));
				}
				break;
			case 2:
                {
					var lastRememberedIdx = rememberedIdxPhrases.Last();

					var otherIdxPhrases = idxAll.Where(a => !rememberedIdxPhrases.Contains(a));
					var shuffledArray = otherIdxPhrases.ToArray().Shuffle();
					for (int x = 0; x < idxCurrentQuotes.Length; x++)
					{
						idxCurrentQuotes[x] = shuffledArray[x];
						displayedMeshes[x].text = shuffledQuotes[shuffledArray[x]];
					}
					QuickLog(string.Format("The following phrases are now shown from top to bottom:"));
					for (int x = 3; x >= 0; x--)
					{
						QuickLog(string.Format("{0}: \"{1}\"", 4 - x, shuffledQuotes[idxCurrentQuotes[x]].Replace("\n", " ")));
					}
					int[] arrayDistance = new int[4];
                    for (int x = 0; x < idxCurrentQuotes.Length; x++)
                    {
						arrayDistance[x] = Mathf.Abs(idxCurrentQuotes[x] - lastRememberedIdx);
                    }
					Debug.LogFormat("<Labeled Priorities Plus #{0}> Distances for this stage: {1}", modID, arrayDistance.Reverse().Join(", "));
					correctButtonPressOrder.Clear();
					correctButtonPressOrder.AddRange(unpressedIdxes.Where(a => arrayDistance[a] >= unpressedIdxes.Select(x => arrayDistance[x]).Max()));
					QuickLog(string.Format("Valid screens to press in this stage where 1 is the top-most screen: {0}", correctButtonPressOrder.Select(a => 4 - a).Join()));
				}
				break;
			case 3:
				{
					var lastRememberedIdx = rememberedIdxPhrases.Last();

					var otherIdxPhrases = idxAll.Where(a => !rememberedIdxPhrases.Contains(a));
					var shuffledArray = otherIdxPhrases.ToArray().Shuffle();
					for (int x = 0; x < idxCurrentQuotes.Length; x++)
					{
						idxCurrentQuotes[x] = shuffledArray[x];
						displayedMeshes[x].text = shuffledQuotes[shuffledArray[x]];
					}
					QuickLog(string.Format("The following phrases are now shown from top to bottom:"));
					for (int x = 3; x >= 0; x--)
					{
						QuickLog(string.Format("{0}: \"{1}\"", 4 - x, shuffledQuotes[idxCurrentQuotes[x]].Replace("\n", " ")));
					}
					correctButtonPressOrder.Clear(); // Obtain the first one from top to bottom, starting at the current idx and wrapping around if necessary.
					for (var x = 0; x < 60 && !correctButtonPressOrder.Any(); x++)
                    {
						var idxReferenced = (lastRememberedIdx + x) % 60;
						if (idxCurrentQuotes.Contains(idxReferenced))
                        {
							for (int y = 0; y < idxCurrentQuotes.Length; y++)
								if (idxCurrentQuotes[y] == idxReferenced && !rememberedIdxPositions.Contains(y))
									correctButtonPressOrder.Add(y);
                        }							
                    }
					QuickLog(string.Format("First valid screen to press in this stage where 1 is the top-most screen: {0}", correctButtonPressOrder.Select(a => 4 - a).Join()));
				}
				break;
			default:
				break;
        }
		QuickLog("");
		interactable = true;
	}
	void HandleScreenPressRelabeled(int idx)
	{
		if (idx < 0 || idx >= 4) return;
		bool isAllCorrect = true;
		switch (relabeledStageOrder.ElementAtOrDefault(curStageCnt))
        {
			case -1:
				{
					if (!currentButtonPressOrder.Contains(idx))
					{
						currentButtonPressOrder.Add(idx);
						displayedMeshes[idx].text = currentButtonPressOrder.Count.ToString();
					}
					if (currentButtonPressOrder.Count < correctButtonPressOrder.Count) return;
					QuickLog(string.Format("Sequence of presses for stage {1}: {0}", currentButtonPressOrder.Select(a => 4 - a).Join(), curStageCnt));
					isAllCorrect = currentButtonPressOrder.SequenceEqual(correctButtonPressOrder);
					currentButtonPressOrder.Clear();
					break;
				}
			case 1:
			case 2:
			case 3:
				{
					QuickLog(string.Format("The defuser pressed screen #{0} from the top for stage {1}", 3 - idx + 1, curStageCnt));
					isAllCorrect = correctButtonPressOrder.Contains(idx);
					break;
				}
		}
		if (isAllCorrect)
		{
			interactable = false;
			if (curStageCnt != 0)
				QuickLog(string.Format("That seems right."));
			if (curStageCnt >= 4)
			{
				QuickLog(string.Format("You cleared enough stages. Module disarmed."));
				mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
				modSolved = true;
				modSelf.HandlePass();
				StartCoroutine(HandleDisarmAnim());
			}
			else
            {
				if (curStageCnt != 0)
				{
					rememberedIdxPositions.Add(idx);
					QuickLog(string.Format("Remembering the following position from the top: {0}", 3 - idx + 1));
				}
				rememberedIdxPhrases.Add(idxCurrentQuotes[idx]);
				QuickLog(string.Format("Remembering the following phrase from {1}: \"{0}\"", shuffledQuotes[idxCurrentQuotes[idx]].Replace("\n", " "), curStageCnt == 0 ? "initial" : ("stage " + curStageCnt)));
				curStageCnt++;
				StartCoroutine(HandleAnimRelabeled());
			}
		}
		else
		{
			QuickLog(string.Format("That doesn't seems right. Strike incurred. And resetting..."));
			modSelf.HandleStrike();
			StartCoroutine(HandleAnimRelabeled(true));
		}
		
	}
	IEnumerator HandleAnimRelabeled(bool hasStruck = false)
	{
		interactable = false;
		for (float y = 0; y < 1f; y += Time.deltaTime)
		{
			yield return null;
			for (int x = 0; x < displayedMeshes.Length; x++)
			{
				displayedMeshes[x].color = hasStruck ? new Color(1, 0, 0, 1f - y) :
					x + 1 < curStageCnt ? new Color(0, 1, 0, 1f - y) : new Color(1, 1, 1, 1f - y);
			}
		}
		interactable = true;
		if (hasStruck)
			curStageCnt = 0;
		GenerateSolutionRelabeled();
		yield return HandleRevealAnimReverse(!hasStruck);
	}
	// Mislabeled Priorities
	void PrepMislabeledPriorities()
    {
		modSelf.OnActivate += delegate { GenerateSoultionMislabeled(); StartCoroutine(HandleRevealAnim()); };
		for (int x = 0; x < displaySelectables.Length; x++)
		{
			int y = x;
			displaySelectables[x].OnInteract += delegate {
				displaySelectables[y].AddInteractionPunch(0.2f);
				mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, displaySelectables[y].transform);
				if (interactable && !modSolved)
				{
					HandleScreenPressMislabeled(y);
				}
				return false;
			};
		}
		backingAll.localEulerAngles = Vector3.down * 90;
		TPFlipPressOrder = true;
		if (variantsCountAll != null && overrideSuccessful)
			variantsCountAll[3]--;
	}
    void GenerateSoultionMislabeled()
    {
		var idxAll = Enumerable.Range(0, shuffledQuotes.Length).ToArray().Shuffle();
		currentButtonPressOrder.Clear();
		switch (curStageCnt)
		{
			case 0:
				QuickLog("Initial Stage:");
				currentCycleCnt = Enumerable.Range(0, 4).ToArray().Shuffle();
				var selectedScnIdxForbid = rnd.Range(0, 4);
				var selected3RandomIdxes = idxAll.Take(3);
				var curIdxSelectedRandomIdx = 0;
				var stringCode = currentCycleCnt.Select(a => a + 1).Join("");
				for (var x = 0; x < idxCurrentQuotes.Length; x++)
                {
					if (selectedScnIdxForbid != x)
					{
						var selectedIdx = selected3RandomIdxes.ElementAt(curIdxSelectedRandomIdx);
						idxCurrentQuotes[x] = selectedIdx;
						displayedMeshes[x].text = shuffledQuotes[selectedIdx];
						curIdxSelectedRandomIdx++;
					}
					else
                    {
						idxCurrentQuotes[x] = -1;
						displayedMeshes[x].text = stringCode;
					}
				}
				QuickLog("The following are now shown from top to bottom:");
				for (int x = 0; x < 4; x++)
				{
					QuickLog(string.Format("{0}: \"{1}\"", x + 1, x == selectedScnIdxForbid ? stringCode : shuffledQuotes[idxCurrentQuotes[x]].Replace("\n", " ")));
				}
				QuickLog("Press the phrases in any order to disarm the module. DO NOT press the 4-digit code. And make sure to remember the initial displays and the order they were pressed in.");
				break;
			case 1:
			case 2:
			case 3:
				{
					QuickLog(string.Format("Stage {0}:", curStageCnt));
					correctButtonPressOrder.Clear();
					var filteredUnrememberedIdxesAll = idxAll.Where(a => !rememberedIdxPhrases.Contains(a));
					var selected4RandomIdxes = filteredUnrememberedIdxesAll.Take(4);
					var selectedIdxRule = mislabeledPrioritiesGrid[currentCycleCnt[curStageCnt - 1]][rememberedIdxPositions[curStageCnt - 1]];
					QuickLog(string.Format("Remembered position pressed {0}: {1}", new[] { "1st", "2nd", "3rd" }[curStageCnt - 1], rememberedIdxPositions[curStageCnt - 1] + 1));
					QuickLog(string.Format("Remembered phrase pressed {0}: \"{1}\"", new[] { "1st", "2nd", "3rd" }[curStageCnt - 1], shuffledQuotes[rememberedIdxPhrases[curStageCnt - 1]].Replace("\n", " ")));
					QuickLog(string.Format("Intersecting the {0} digit of the 4-digit code ({2}) and the {0} initial position pressed results in this letter: {1}", new[] { "1st", "2nd", "3rd" }[curStageCnt - 1], "ABCD"[selectedIdxRule], currentCycleCnt[curStageCnt - 1]));
					for (var x = 0; x < idxCurrentQuotes.Length; x++)
					{
						var selectedIdx = filteredUnrememberedIdxesAll.ElementAt(x);
						idxCurrentQuotes[x] = selectedIdx;
					}
					switch (selectedIdxRule)
                    {
						case 0:
						case 1:
							for (var i = 0; i < shuffledQuotes.Length && correctButtonPressOrder.Count < 4; i++)
                            {
								var curIdxPhraseScan = (rememberedIdxPhrases[curStageCnt - 1] + i) % shuffledQuotes.Length;
								if (idxCurrentQuotes.Contains(curIdxPhraseScan))
									correctButtonPressOrder.AddRange(Enumerable.Range(0, 4).Where(a => idxCurrentQuotes[a] == curIdxPhraseScan));
                            }
							if (selectedIdxRule == 1) correctButtonPressOrder.Reverse();
							break;
						case 2:
						case 3:
							var distancesAll = idxCurrentQuotes.Select(a => Math.Abs(a - rememberedIdxPhrases[curStageCnt - 1]));
							while (distancesAll.Distinct().Count() < 4)
							{
								idxAll.Shuffle();
								filteredUnrememberedIdxesAll = idxAll.Where(a => !rememberedIdxPhrases.Contains(a));
								selected4RandomIdxes = filteredUnrememberedIdxesAll.Take(4);
								for (var x = 0; x < idxCurrentQuotes.Length; x++)
								{
									var selectedIdx = filteredUnrememberedIdxesAll.ElementAt(x);
									idxCurrentQuotes[x] = selectedIdx;
								}
								distancesAll = idxCurrentQuotes.Select(a => Math.Abs(a - rememberedIdxPhrases[curStageCnt - 1]));
							}
							correctButtonPressOrder.AddRange(Enumerable.Range(0, 4).OrderBy(a => distancesAll.ElementAt(a)));
							if (selectedIdxRule == 3) correctButtonPressOrder.Reverse();
							break;
						default:
							break;
                    }
					correctButtonPressOrder.RemoveAt(3);
					for (var x = 0; x < idxCurrentQuotes.Length; x++)
					{
						displayedMeshes[x].text = shuffledQuotes[idxCurrentQuotes[x]];
					}
					QuickLog("The following are now shown from top to bottom:");
					for (int x = 0; x < 4; x++)
					{
						QuickLog(string.Format("{0}: \"{1}\"", x + 1, shuffledQuotes[idxCurrentQuotes[x]].Replace("\n", " ")));
					}
					QuickLog(string.Format("Expected sequence of screens to press: {0}", correctButtonPressOrder.Select(a => a + 1).Join()));
				}
				break;
			case 4:
                {
					QuickLog(string.Format("Stage {0}:", curStageCnt));
					var finalNumber = currentCycleCnt.Last();
					var shuffledValues = Enumerable.Range(0, 4).ToArray().Shuffle();
					correctButtonPressOrder.Clear();
					switch (finalNumber)
                    {
						case 3:
							correctButtonPressOrder.AddRange(Enumerable.Range(0, 4).Where(a => !rememberedIdxPositions.Contains(shuffledValues[a])));
							break;
						case 2:
							var largestDigitPos = Enumerable.Range(0, 4).Where(a => shuffledValues[a] == shuffledValues.Max()).Single();
							correctButtonPressOrder.AddRange(Enumerable.Range(0, 4).Where(a => shuffledValues[a] == largestDigitPos));
							break;
						case 1:
							var smallestDigitPos = Enumerable.Range(0, 4).Where(a => shuffledValues[a] == shuffledValues.Min()).Single();
							correctButtonPressOrder.AddRange(Enumerable.Range(0, 4).Where(a => shuffledValues[a] == smallestDigitPos));
							break;
						case 0:
							while (Enumerable.Range(0, 4).Count(a => a == shuffledValues[a]) != 1)
								shuffledValues.Shuffle();
							correctButtonPressOrder.AddRange(Enumerable.Range(0, 4).Where(a => a == shuffledValues[a]));
							break;
                    }
					QuickLog("The following are now shown from top to bottom:");
					for (int x = 0; x < 4; x++)
					{
						displayedMeshes[x].text = (shuffledValues[x] + 1).ToString();
						QuickLog(string.Format("{0}: \"{1}\"", x + 1, shuffledValues[x] + 1));
					}
					QuickLog(string.Format("Remaining Digit: {0}", finalNumber + 1));
					QuickLog(string.Format("Press the screen in this position to complete the stage: {0}", correctButtonPressOrder.Select(a => a + 1).Join()));
				}
				break;
        }
		interactable = true;
    }
	void HandleScreenPressMislabeled(int idx)
	{
		if (idx < 0 || idx >= 4) return;
		bool isAllCorrect = true;
		switch (curStageCnt)
		{
			case 4:
				{
					QuickLog(string.Format("The defuser pressed screen #{0} from the top for stage {1}", idx + 1, curStageCnt));
					isAllCorrect = correctButtonPressOrder.Contains(idx);
					break;
				}
			case 0:
                {
					if (!currentButtonPressOrder.Contains(idx))
					{
						currentButtonPressOrder.Add(idx);
						displayedMeshes[idx].text = currentButtonPressOrder.Count.ToString();
					}
					if (idxCurrentQuotes[idx] == -1) {
						isAllCorrect = false;
						QuickLog(string.Format("The defuser pressed screen #{0} from the top for the initial stage, which contained the 4-digit code.", idx + 1, curStageCnt));
						break;
					}
					else if (currentButtonPressOrder.Count != 3) return;
					
					break;
                }
			case 1:
			case 2:
			case 3:
				{
					if (!currentButtonPressOrder.Contains(idx))
					{
						currentButtonPressOrder.Add(idx);
						displayedMeshes[idx].text = currentButtonPressOrder.Count.ToString();
					}
					if (currentButtonPressOrder.Count < correctButtonPressOrder.Count) return;
					QuickLog(string.Format("Sequence of presses for stage {1}: {0}", currentButtonPressOrder.Select(a => a + 1).Join(), curStageCnt));
					isAllCorrect = correctButtonPressOrder.SequenceEqual(currentButtonPressOrder);
					break;
				}
		}
		if (isAllCorrect)
		{
			interactable = false;
			if (curStageCnt == 0)
			{
				QuickLog(string.Format("Storing the following phrases pressed in this order:"));
				for (int x = 0; x < currentButtonPressOrder.Count; x++)
				{
					QuickLog(string.Format("{0}: \"{1}\"", currentButtonPressOrder[x] + 1, shuffledQuotes[idxCurrentQuotes[currentButtonPressOrder[x]]].Replace("\n", " ")));
				}
				rememberedIdxPositions.AddRange(currentButtonPressOrder);
				rememberedIdxPhrases.AddRange(currentButtonPressOrder.Select(a => idxCurrentQuotes[a]));
				QuickLog(string.Format("Storing the 4-digit code: {0}", currentCycleCnt.Select(a => a + 1).Join()));
			}
			else
            {
				QuickLog("That seems correct.");
			}
			if (curStageCnt >= 4)
			{
				QuickLog(string.Format("You cleared enough stages. Module disarmed."));
				mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
				modSolved = true;
				modSelf.HandlePass();
				StartCoroutine(HandleDisarmAnim());
			}
			else
			{
				curStageCnt++;
				StartCoroutine(HandleAnimMislabeled());
			}
		}
		else
		{
			QuickLog(string.Format("That doesn't seems right. Strike incurred. And resetting..."));
			modSelf.HandleStrike();
			StartCoroutine(HandleAnimMislabeled(true));
			rememberedIdxPhrases.Clear();
			rememberedIdxPositions.Clear();
		}

	}
	IEnumerator HandleAnimMislabeled(bool hasStruck = false)
	{
		interactable = false;
		for (float y = 0; y < 1f; y += Time.deltaTime)
		{
			yield return null;
			for (int x = 0; x < displayedMeshes.Length; x++)
			{
				displayedMeshes[x].color = hasStruck ? new Color(1, 0, 0, 1f - y) :
					4 - x < curStageCnt ? new Color(0, 1, 0, 1f - y) : new Color(1, 1, 1, 1f - y);
			}
		}
		interactable = true;
		if (hasStruck)
			curStageCnt = 0;
		GenerateSoultionMislabeled();
		yield return HandleRevealAnim(!hasStruck);
	}
	// TP Handler Begins Here
	IEnumerator TwitchHandleForcedSolve()
    {
		while (!interactable) yield return true;
		switch (idxVariantGenerated)
        {
			case 0:
				currentButtonPressOrder.Clear();
                for (var x = 0; x < 4; x++)
                {
					displayedMeshes[x].text = shuffledQuotes[possibleEachIdxQuotes[x].Single()];
                }
				for (var x = 0; x < correctButtonPressOrder.Count; x++)
				{
					displaySelectables[correctButtonPressOrder[x]].OnInteract();
					yield return new WaitForSeconds(0.1f);
				}
				break;
			case 1:
				var curIdx = Array.IndexOf(idxSolutionQuotes, idxSolutionQuotes.Min());
				for (var x = 0; x < 4; x++)
					idxCurrentQuotes[x] = -1;

				while (!idxCurrentQuotes.SequenceEqual(idxSolutionQuotes))
                {
					yield return null;
					for (var x = 0; x < 4 && idxCurrentQuotes[curIdx] != idxSolutionQuotes[curIdx]; x++)
					{
						displaySelectables[curIdx].OnInteract();
						yield return new WaitForSeconds(0.1f);
					}
					curIdx = (curIdx + 1) % 4;
                }
				break;
			case 2:
				while (!modSolved)
                {
					while (!interactable) yield return true;
					if (relabeledStageOrder[curStageCnt] != -1)
                    {
						if (!correctButtonPressOrder.Any())
						{
							displaySelectables.PickRandom().OnInteract();
						}
						else
							displaySelectables[correctButtonPressOrder.PickRandom()].OnInteract();
                    }
					else
                    {
						currentButtonPressOrder.Clear();
						for (var x = 0; x < correctButtonPressOrder.Count; x++)
                        {
							displaySelectables[correctButtonPressOrder[x]].OnInteract();
							yield return new WaitForSeconds(0.1f);
						}
                    }
					yield return null;
                }
				break;
			case 3:
                {
					while (!modSolved)
					{
						while (!interactable) yield return true;
						switch (curStageCnt)
						{
							case 0:
								var selectedRandomOrderPress = Enumerable.Range(0, 4).Where(a => idxCurrentQuotes[a] != -1).ToArray().Shuffle();
								for (int i = 0; i < selectedRandomOrderPress.Length && curStageCnt == 0; i++)
								{
									int idxValid = selectedRandomOrderPress[i];
									displaySelectables[idxValid].OnInteract();
									yield return new WaitForSeconds(0.1f);
								}
								break;
							case 1:
							case 2:
							case 3:
								currentButtonPressOrder.Clear();
								for (var x = 0; x < 4; x++)
								{
									displayedMeshes[x].text = shuffledQuotes[idxCurrentQuotes[x]];
								}
								for (var x = 0; x < correctButtonPressOrder.Count; x++)
								{
									displaySelectables[correctButtonPressOrder[x]].OnInteract();
									yield return new WaitForSeconds(0.1f);
								}
								break;
							case 4:
								{
									displaySelectables[correctButtonPressOrder.PickRandom()].OnInteract();
								}
								break;
						}	
					}
				}
				break;
			default:
				yield return null;
				modSelf.HandlePass();
				interactable = false;
				StartCoroutine(HandleDisarmAnim());
				break;
        }
    }

#pragma warning disable IDE0051 // Remove unused private members
    readonly string TwitchHelpMessage = "Press a given button with \"!{0} press ### # # #\" where 1 is the top-most button in that module. Append \"slow\" or \"veryslow\" onto the command to make the presses go slower or \"instant\" to make the button presses instant.";
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
								screenPresses.Add(displaySelectables[TPFlipPressOrder ? 0 : 3]);
								break;
							case '2':
								screenPresses.Add(displaySelectables[TPFlipPressOrder ? 1 : 2]);
								break;
							case '3':
								screenPresses.Add(displaySelectables[TPFlipPressOrder ? 2 : 1]);
								break;
							case '4':
								screenPresses.Add(displaySelectables[TPFlipPressOrder ? 3 : 0]);
								break;
							default:
								yield return string.Format("sendtochaterror The given character \"{0}\" does not correspond to a pressable button on the module.",aNum);
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
				if (modSolved)
				{
					yield return string.Format("awardpoints {0}", dynamicScoreToGive);
					yield break;
				}
				else if (!interactable)
				{
					//Debug.LogFormat("<Labeled Priorities Plus #{0}> TP Debug: Interrupting TP command due to specific modifier after {1} press(es).", modID, x + 1);
					yield break;
				}
				else if (requireDelay)
					yield return string.Format("trywaitcancel {0} Your button press has been canceled after {1} press{2}.", delayAmount, x + 1, x == 0 ? "" : "es");
            }
        }

		yield break;
    }

}
