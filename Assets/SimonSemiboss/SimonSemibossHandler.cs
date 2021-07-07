using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using uernd = UnityEngine.Random;
using System;

public class SimonSemibossHandler : MonoBehaviour {

	public KMSelectable[] possibleButtons = new KMSelectable[8];
	public MeshRenderer[] buttonRenderers;
	public Light[] buttonLights;
	public KMBombInfo bombInfo;
	public KMColorblindMode colorblindMode;
	public KMBossModule bossModuleHandler;
	public KMBombModule moduleSelf;
	public KMAudio mAudio;
	public Material[] selectionMats;
	public MeshRenderer faceRender;
	public Texture[] faces = new Texture[3];

	string[] ignoredModuleNames;

	Color[] colorList = {
		Color.red,
		Color.yellow,
		Color.green,
		Color.cyan,
		Color.blue,
		Color.magenta,
		Color.white,
		Color.black
	};
	string[] debugColorString = { "Red", "Yellow", "Green", "Cyan", "Blue", "Magenta", "White", "Black" };
	int[] idxColorList = { 0, 1, 2, 3, 4, 5, 6, 7 };

	List<int> possiblePressIdx = new List<int>();

	bool isSolved, mashToSolve, hasStarted, isPaniking, alterDefaultHandling, hasStruck;
	float mashCooldown = 0f;
	static int modID = 1;
	int curmodID, solveCountActivation = 0, curPressIdx = 0, curSolveCount, unignoredModuleCount, maxFlashesAllowed;
	IEnumerator[] buttonFlashSet;
	IEnumerator flashingSequence;
	private FlyersOtherSettings selfSettings = new FlyersOtherSettings();
	// Use this for initialization
	void Awake()
	{
		try
		{
			ModConfig<FlyersOtherSettings> universalConfig = new ModConfig<FlyersOtherSettings>("FlyersOtherSettings");
			selfSettings = universalConfig.Settings;
			universalConfig.Settings = selfSettings;
			maxFlashesAllowed = selfSettings.SimonSemibossMaxFlashes;
		}
		catch
		{
			Debug.LogFormat("<SimonSettings> Settings do not work as intended! using default settings.");
			maxFlashesAllowed = 40;
		}
		Debug.LogFormat("<SimonSettings> Max flashes allowed: {0}", maxFlashesAllowed < 10 ? "unlimited" : maxFlashesAllowed.ToString());
	}
	IEnumerator FlashButton(int idx)
	{
		if (idx < 0 || idx >= Mathf.Min(buttonRenderers.Length, buttonLights.Length, possibleButtons.Length)) yield break;
		buttonLights[idx].enabled = true;
		buttonRenderers[idx].material = selectionMats[1];
		buttonRenderers[idx].material.color = colorList[idxColorList[idx]];
		yield return new WaitForSeconds(0.4f);
		buttonLights[idx].enabled = false;
		buttonRenderers[idx].material = selectionMats[0];
		buttonRenderers[idx].material.color = colorList[idxColorList[idx]] * 0.5f + Color.gray * 0.5f;
	}
	IEnumerator FlashSequence(int repeatCount = 1, int startIdx = 0)
	{
		for (int x = 0; x < repeatCount; x++)
		{
			yield return new WaitForSeconds(1f);
			for (int i = startIdx; i < possiblePressIdx.Count; i++)
			{
				int oneIdx = possiblePressIdx[i];
				yield return FlashButton(oneIdx);
				yield return new WaitForSeconds(0.5f);
			}
		}
	}
	IEnumerator FlashSequenceQuickly(int repeatCount = 2, int startIdx = 0)
	{
		for (int x = 0; x < repeatCount; x++)
		{
			yield return new WaitForSeconds(3f);
			for (int i = startIdx; i < possiblePressIdx.Count; i++)
			{
				int idx = possiblePressIdx[i];
				buttonLights[idx].enabled = true;
				buttonRenderers[idx].material = selectionMats[1];
				buttonRenderers[idx].material.color = colorList[idxColorList[idx]];
				yield return new WaitForSeconds(0.25f);
				buttonLights[idx].enabled = false;
				buttonRenderers[idx].material = selectionMats[0];
				buttonRenderers[idx].material.color = colorList[idxColorList[idx]] * 0.5f + Color.gray * 0.5f;
				yield return new WaitForSeconds(0.25f);
			}
		}
	}
	// Sections of the code used to handle Mystery Module and Organization in case the module is unable to grab data from the repo
	IEnumerator CheckOrganMysterySolveOrder(KMBombModule mModule = null)
	{
		while (mModule != null)
		{
			yield return null;
			var organCoreScript = mModule.GetComponent("OrganizationScript");
			if (organCoreScript != null)
			{
				var ignoreList = organCoreScript.GetValue<string[]>("ignoredModules");
				if (ignoreList != null && !ignoreList.Contains(moduleSelf.ModuleDisplayName))
				{
					var solveOrder = organCoreScript.GetValue<List<string>>("order");
					if (solveOrder != null)
					{
						if (solveOrder.FirstOrDefault() == moduleSelf.ModuleDisplayName)
						{
							GenerateFlashes();
							yield break;
						}
					}
				}
				continue;
			}
			else
			{
				var mysteryModCoreScript = mModule.gameObject.GetComponent("MysteryModuleScript");
				if (mysteryModCoreScript != null)
				{
					var listKeyModules = mysteryModCoreScript.GetValue<List<KMBombModule>>("keyModules");
					if (listKeyModules != null)
					{
						if (listKeyModules.Contains(moduleSelf))
						{
							alterDefaultHandling = true;
							if (listKeyModules.FirstOrDefault().Equals(moduleSelf))
							{
								GenerateFlashes();
								yield break;
							}
						}
					}
				}
				else
				{
					yield break;
				}
			}
		}
	}

	IEnumerator HandleOrganMysteryModuleCore()
	{
		/* Summary:
		 * This entire set of methods is designed to handle Organization and Mystery Module in a case where these modules aren't handled by default.
		 * 
		 */
		if (Application.isEditor)
			yield break;
		var mBomb = GetComponentInParent<KMBomb>();
		if (mBomb != null)
		{
			yield return null;
			var organizationModules = mBomb.GetComponentsInChildren<KMBombModule>().Where(a => a.ModuleType == "organizationModule");
			if (organizationModules != null && organizationModules.Any())
			{// Check Organization's handling for this module.
				foreach (var oneOrganizationModule in organizationModules)
				{
					var organCoreScript = oneOrganizationModule.GetComponent("OrganizationScript");
					if (organCoreScript != null)
					{
						var ignoreList = organCoreScript.GetValue<string[]>("ignoredModules");
						if (ignoreList != null && !ignoreList.Contains(moduleSelf.ModuleDisplayName))
						{
							alterDefaultHandling = true;
							StartCoroutine(CheckOrganMysterySolveOrder(oneOrganizationModule));
						}
					}
				}
			}
			var mysteryModules = mBomb.GetComponentsInChildren<KMBombModule>().Where(a => a.ModuleType == "mysterymodule");
			if (mysteryModules != null && mysteryModules.Any())
			{// Alter this module's handling for Mystery Module.
				foreach (var aMysteryModule in mysteryModules)
				{
					var mysteryModCoreScript = aMysteryModule.gameObject.GetComponent("MysteryModuleScript");
					if (mysteryModCoreScript != null)
					{
						var selectedHiddenModule = mysteryModCoreScript.GetValue<KMBombModule>("mystifiedModule");
						if (selectedHiddenModule != null && selectedHiddenModule.Equals(moduleSelf)) // If the mystified module is this module
						{
							alterDefaultHandling = true;
							aMysteryModule.OnPass += delegate
							{
								GenerateFlashes();
								return false;
							};
						}
						else
						{
							StartCoroutine(CheckOrganMysterySolveOrder(aMysteryModule));
						}
					}
				}
			}
		}
	}
	// End Section
	void Start() {

		curmodID = modID++;
		ignoredModuleNames = bossModuleHandler.GetIgnoredModules("Simon", new string[] {
		"+",
		"14",
		"42",
		"501",
		"Access Codes",
		"Amnesia",
		"A>N<D",
		"Bamboozling Time Keeper",
		"Black Arrows",
		"Brainf---",
		"Busy Beaver",
		"Button Messer",
		"Cookie Jars",
		"Cube Synchronization",
		"Divided Squares",
		"Don't Touch Anything",
		"Encrypted Hangman",
		"Encryption Bingo",
		"Floor Lights",
		"Forget Any Color",
		"Forget Enigma",
		"Forget Everything",
		"Forget Infinity",
		"Forget It Not",
		"Forget Me Later",
		"Forget Me Not",
		"Forget Perspective",
		"Forget The Colors",
		"Forget Them All",
		"Forget This",
		"Forget Us Not",
		"Four-Card Monte",
		"The Heart",
		"Hogwarts",
		"Iconic",
		"Keypad Directionality",
		"The Klaxon",
		"Kugelblitz",
		"Multitask",
		"Mystery Module",
		"OmegaForget",
		"OmegaDestroyer",
		"Organization",
		"Purgatory",
		"RPS Judging",
		"Security Council",
		"Shoddy Chess",
		"Simon",
		"Simon Forgets",
		"Simon's Stages",
		"Souvenir",
		"SuperBoss",
		"The Swan",
		"Tallordered Keys",
		"The Time Keeper",
		"Timing is Everything",
		"The Troll",
		"Turn The Key",
		"The Twin",
		"Übermodule",
		"Ultimate Custom Night",
		"The Very Annoying Button",
		"Whiteout",
		});
		Debug.LogFormat("<Simon #{0}> Ignored Modules: {1}", curmodID, ignoredModuleNames.Join(", "));

		if (!Application.isEditor)
		{
			unignoredModuleCount = bombInfo.GetSolvableModuleNames().Count(a => !ignoredModuleNames.Contains(a));
			Debug.LogFormat("[Simon #{0}]: Detected this many unignored modules on the bomb: {1}", curmodID, unignoredModuleCount);
		}

		idxColorList.Shuffle();
		Debug.LogFormat("[Simon #{0}]: Button Colors in reading order: {1}", curmodID, idxColorList.Select(a => debugColorString[a]).Join(", "));
		float lossyScaleSelf = moduleSelf.transform.lossyScale.x;
		for (int x = 0; x < buttonRenderers.Length; x++)
		{
			buttonRenderers[x].material.color = colorList[idxColorList[x]] * 0.5f + Color.gray * 0.5f;
		}
		for (int x = 0; x < buttonLights.Length; x++)
		{
			//buttonLights[x].color = colorList[idxColorList[x]];
			buttonLights[x].range *= lossyScaleSelf;
			buttonLights[x].enabled = false;
		}

		moduleSelf.OnActivate += delegate {
			if (Application.isEditor)
			{
				unignoredModuleCount = bombInfo.GetSolvableModuleNames().Count(a => !ignoredModuleNames.Contains(a));
				Debug.LogFormat("[Simon #{0}]: Detected this many unignored modules on the bomb: {1}", curmodID, unignoredModuleCount);
			}
			StartCoroutine(HandleOrganMysteryModuleCore());
			// Set a random number of solves required to make Simon panic.
			if (unignoredModuleCount > 0)
				solveCountActivation = uernd.Range(1, unignoredModuleCount * 3 / 4 + 1);
			hasStarted = true;
		};
		buttonFlashSet = new IEnumerator[possibleButtons.Length];
		for (var x = 0; x < possibleButtons.Length; x++)
		{
			var y = x;
			possibleButtons[x].OnInteract += delegate
			{
				possibleButtons[y].AddInteractionPunch();
				mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonRelease, possibleButtons[y].transform);
				if (!isSolved)
					HandleFlashingPress(y);
				return false;
			};
		}

	}
	void HandleFlashingPress(int idx = 0)
	{
		if (buttonFlashSet[idx] != null)
			StopCoroutine(buttonFlashSet[idx]);
		buttonFlashSet[idx] = FlashButton(idx);
		StartCoroutine(buttonFlashSet[idx]);

		if (mashToSolve)
		{
			if (mashCooldown <= 0f) curPressIdx = 0;
			curPressIdx++;
			mashCooldown = 5f;
			if (curPressIdx >= 20)
			{
				SolveModule();
			}
		}
		else
		{
			if (isPaniking)
			{
				if (possiblePressIdx[curPressIdx] == idx)
				{
					curPressIdx++;
					if (curPressIdx >= possiblePressIdx.Count)
					{
						SolveModule();
					}
				}
				else
				{
					Debug.LogFormat("[Simon #{0}]: The defuser pressed {1} which is wrong for position {2} for the input sequence!", curmodID, debugColorString[idxColorList[idx]], curPressIdx + 1);
					hasStruck = true;
					moduleSelf.HandleStrike();
					flashingSequence = FlashSequence(1, curPressIdx);
					StartCoroutine(flashingSequence);
				}
			}
		}
	}
	void SolveModule()
	{
		if (flashingSequence != null)
			StopCoroutine(flashingSequence);
		isSolved = true;
		Debug.LogFormat("[Simon #{0}]: Simon is now happy. He gave you a green light.", curmodID);
		moduleSelf.HandlePass();
		var matFace = faceRender.material;
		if (matFace.HasProperty("_MainTex"))
			matFace.SetTexture("_MainTex", faces[2]);

	}
	void GenerateFlashes()
	{
		int solveCountUponGeneration = bombInfo.GetSolvedModuleIDs().Count();
		Debug.LogFormat("[Simon #{0}]: Simon has started paniking at {1} solve(s).", curmodID, solveCountUponGeneration);
		if (solveCountUponGeneration > 0)
		{
			for (int x = 0; x < solveCountUponGeneration * 2 && (possiblePressIdx.Count() < maxFlashesAllowed || maxFlashesAllowed < 10); x++)
			{
				possiblePressIdx.Add(uernd.Range(0, 8));
			}
			Debug.LogFormat("[Simon #{0}]: And is flashing the following colors: {1}", curmodID, possiblePressIdx.Select(a => debugColorString[idxColorList[a]]).Join(", "));

			isPaniking = true;
			//solveCountActivation = bombInfo.GetSolvedModuleNames().Count(a => !ignoredModuleNames.Contains(a));
			flashingSequence = FlashSequenceQuickly(3);
			StartCoroutine(flashingSequence);
		}
		else
        {
			mashToSolve = true;
			isPaniking = true;
			Debug.LogFormat("[Simon #{0}]: Simon has started paniking! But there are no flashes! You should just mash the buttons until it solves.", curmodID);
		}
		StartCoroutine(PanicAnim());
	}

	IEnumerator PanicAnim()
	{
		while (!hasStarted)
			yield return null;
		var matFace = faceRender.material;
		if (matFace.HasProperty("_MainTex"))
			matFace.SetTexture("_MainTex", faces[1]);
		float lastZPos = faceRender.gameObject.transform.localPosition.z;
		while (!isSolved)
		{
			faceRender.gameObject.transform.localPosition = new Vector3(uernd.Range(-.001f, .001f), uernd.Range(-.001f, .001f), lastZPos);
			yield return new WaitForSeconds(0.1f);
		}
		faceRender.gameObject.transform.localPosition = new Vector3(0, 0, lastZPos);
		yield return null;
	}
	// Update is called once per frame
	void Update() {
		if (mashToSolve && mashCooldown > 0)
		{
			mashCooldown -= Time.deltaTime;
		}

		if (hasStarted && !isSolved)
		{
			var nonIgnoredSolves = bombInfo.GetSolvedModuleNames().Count(a => !ignoredModuleNames.Contains(a));
			if (!alterDefaultHandling)
			{

				if (isPaniking)
				{
					if (nonIgnoredSolves > solveCountActivation)
					{
						solveCountActivation = nonIgnoredSolves;
						Debug.LogFormat("[Simon #{0}]: Simon got angry for being ignored. He put an \"X\" on the timer.", curmodID);
						moduleSelf.HandleStrike();
					}
				}
				else
				{
					if (curSolveCount != nonIgnoredSolves)
					{
						curSolveCount = nonIgnoredSolves;
					}
					if (curSolveCount * 4 >= unignoredModuleCount * 3 || curSolveCount >= solveCountActivation)
					{
						GenerateFlashes();
					}
				}
			}
		}
	}
	IEnumerator MashButtons()
    {
		while (!isSolved)
		{
			yield return null;
			possibleButtons.PickRandom().OnInteract();
			yield return new WaitForSeconds(0.1f);
		}
		yield break;
    }
	void TwitchHandleForcedSolve()
	{
		mashToSolve = true;
		alterDefaultHandling = true;
		StartCoroutine(MashButtons());
	}

#pragma warning disable IDE0051 // Remove unused private members
    readonly string TwitchHelpMessage = "To press the buttons in reading order use: \"!{0} press 12345678\" Numbers may be spaced out in the command; \"press\" is optional. Mash the buttons with \"!{0} mash\".";
#pragma warning restore IDE0051 // Remove unused private members

    IEnumerator ProcessTwitchCommand(string cmd)
    {
		if (cmd.EqualsIgnoreCase("mash"))
        {
			if (!mashToSolve)
            {
				yield return string.Format("antitroll Mashing seems pretty useless for this I don't trust it.");
			}
			var pressesDone = 0;
			hasStruck = false;
			while (pressesDone < 50 && !isSolved && !hasStruck)
            {
				yield return null;
				yield return "trycancel The mashing has been canceled due to a request.";
				possibleButtons.PickRandom().OnInteract();
				pressesDone++;
				yield return new WaitForSeconds(0.1f);
            }
        }

		if (cmd.ToLowerInvariant().StartsWith("press "))
			cmd = cmd.Substring(5).Trim();
		string validSets = "12345678";
		List<KMSelectable> allPresses = new List<KMSelectable>();
		foreach (string cmdSet in cmd.Split())
        {
			foreach (char aLetter in cmdSet)
            {
				if (validSets.Contains(aLetter))
                {
					allPresses.Add(possibleButtons[validSets.IndexOf(aLetter)]);
                }
				else
                {
					yield return string.Format("sendtochaterror This character is not valid for this command: {0}", aLetter);
					yield break;
                }
            }
        }
		if (allPresses.Any())
        {
			yield return null;
			hasStruck = false;
            for (int x = 0; x < allPresses.Count && !hasStruck; x++)
            {
				allPresses[x].OnInteract();
				yield return new WaitForSeconds(0.1f);
            }
        }
		yield break;
    }

}
