using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KModkit;
using System.Linq;

public class SingularityButtonHandler : MonoBehaviour {

	public GameObject disarmButtonObject, buttonFrontObject, entireModule,animatedPortion;
	public TextMesh textDisplay;
	public KMSelectable disarmButton, buttonFront;
	public KMBossModule bossModule;
	public KMBombInfo bombInfo;
	public KMBombModule modSelf;
	public KMAudio audioSelf;
	public KMGameCommands gameCommands;

	private bool isSolved = false;
	private bool hasDisarmed = false;
	private bool hasActivated = false;

	private bool isPressedDisarm = false;
	private bool isPressedMain = false;

	public List<string> cautionaryModules = new List<string>();

	private static int modID = 1;
	private int curmodID;

	public readonly List<string> otherSolveDependModNames = new List<string>() {
		"Blind Maze",
		"Burglar Alarm",
		"Colour Code",
		"Combination Lock",
		"Langton's Ant",
		"The Number",
		"Planets",
		"The Plunger Button",
		"Press X",
		"Scripting",
		"Shapes and Bombs"
	};// Nonboss modules that are 100% dependent on solves. These CANNOT have an override for the solve condition or show up by chance. The Number will remove itself after 8 or more solves.

	protected sealed class SingularityButtonInfo //Lock down infomation to a single bomb, hopefully.
	{
		public List<SingularityButtonHandler> singularButtons = new List<SingularityButtonHandler>();
		public List<int> inputs = new List<int>();
		public List<Color> buttonColors = new List<Color>();
		public List<string> buttonLabels = new List<string>();
		public List<int> buttonDigits = new List<int>();
		public bool canDisarm = false;
		public string serialNum;
		public void DisarmAll()
		{
			canDisarm = true;
		}
		public bool canModuleDisarm()
		{
			return canDisarm;
		}
		public void CauseStrike(int idx)
		{
			if (idx >= 0 && idx < singularButtons.Count)
			{
				singularButtons[idx].modSelf.HandleStrike();
			}
		}
		public int CountSingularityButtons()
		{
			return singularButtons.Count();
		}
		public int getIndexOfButton(SingularityButtonHandler handler)
		{
			return singularButtons.IndexOf(handler);
		}
	}
	private static readonly Dictionary<KMBomb, SingularityButtonInfo> groupedSingularityButtons = new Dictionary<KMBomb, SingularityButtonInfo>();
	private SingularityButtonInfo singularityButtonInfo;
	void AddOthersModulesOntoList()
	{
		cautionaryModules.AddRange(bossModule.GetIgnoredModules("Singularity Button", new string[]
		{
			"14",
			"Cookie Jars",
			"Divided Squares",
			"Encryption Bingo",
			"Forget Enigma",
			"Forget Everything",
			"Forget It Not",
			"Forget Infinity",
			"Forget Me Not",
			"Forget Perspective",
			"Forget Them All",
			"Forget This",
			"Forget Us Not",
			"Hogwarts",
			"Organization",
			"Simon Forgets",
			"Simon's Stages",
			"Tallordered Keys",
			"The Troll",
			"Übermodule",
			"Ultimate Custom Night"
		}));// Add boss modules onto this list that rely on checking stages or do not handle syncronized solves intentionally. Uses KMBossModule to grab latest list of boss modules
		cautionaryModules.AddRange(otherSolveDependModNames); // Add other solve dependent modules on the bomb.
		// BEGIN CHANCE SOLVES
		if (!(bombInfo.IsIndicatorPresent(Indicator.BOB) &&
			bombInfo.GetBatteryCount() == 5 &&
			bombInfo.GetBatteryHolderCount() == 3))// "If there are five batteries in three holders and at least one BOB indicator..."
		{
			cautionaryModules.Add("Big Circle");
			Debug.LogFormat("[Singularity Button #{0}]: Big Circle has no override active.", curmodID);
		}// Add Big Circle
		if (!"AEIOU".Contains(bombInfo.GetSerialNumber()) &&
			bombInfo.GetBatteryCount() <= 3 &&
			!bombInfo.IsPortPresent(Port.Serial))// Cases 1, 2, and 3 are NOT active.
		{
			cautionaryModules.Add("Modern Cipher");
			Debug.LogFormat("[Singularity Button #{0}]: Modern Cipher has no override active.", curmodID);
		}// Add Modern Cipher
		if (!(bombInfo.IsIndicatorOn(Indicator.BOB) &&
			bombInfo.GetBatteryCount() == 4 &&
			bombInfo.GetBatteryHolderCount() == 2))// "If there are exactly 4 batteries in 2 holders and a there is a lit BOB indicator..."
		{
			cautionaryModules.Add("Laundry");
			Debug.LogFormat("[Singularity Button #{0}]: Laundry has no override active.", curmodID);
		}// Add Laundry
		if (!(bombInfo.GetBatteryCount() == 2 &&
			bombInfo.GetBatteryHolderCount() == 1 &&
			bombInfo.IsIndicatorOn(Indicator.FRK) &&
			bombInfo.GetPortCount(Port.Parallel) == 0 &&
			bombInfo.GetPortCount(Port.Serial) == 0))//"If the bomb has exactly two batteries in one holder, a lit FRK indicator and no Serial or Parallel ports..."
		{
			cautionaryModules.Add("Heraldry");
			Debug.LogFormat("[Singularity Button #{0}]: Heraldry has no override active.", curmodID);
		}// Add Heraldry
		if (!(bombInfo.IsIndicatorPresent(Indicator.BOB) ||
			(bombInfo.GetSolvableModuleNames().Where(a => "Double".ContainsIgnoreCase(a)).ToList().Count >= 3) ||
			(bombInfo.GetSolvableModuleNames().Where(a => "Burglar Alarm".ContainsIgnoreCase(a)).ToList().Count >= 1 && bombInfo.GetSolvableModuleNames().Where(a => "Safety Safe".ContainsIgnoreCase(a)).ToList().Count >= 1) ||
			(bombInfo.GetSolvableModuleNames().Where(a => "The Jewel Vault".ContainsIgnoreCase(a)).ToList().Count >= 1 && bombInfo.GetSolvableModuleNames().Where(a => "Safety Safe".ContainsIgnoreCase(a)).ToList().Count >= 1) ||
			(bombInfo.GetSolvableModuleNames().Where(a => "Burglar Alarm".ContainsIgnoreCase(a)).ToList().Count >= 1 && bombInfo.GetSolvableModuleNames().Where(a => "The Jewel Vault".ContainsIgnoreCase(a)).ToList().Count >= 1)
			))// "If there is a lit or unlit BOB indicator..."
		{// "If you have two or more (not including repeats) out of Burglar Alarm, Safety Safe and The Jewel Vault, or if the Bomb contains three or more modules with the word 'Double' in their names..."
			cautionaryModules.Add("Free Parking");
			Debug.LogFormat("[Singularity Button #{0}]: Free Parking has no override active, ensure you check Free Parking's manual for other cases!", curmodID);
		}// Add Free Parking, note that it doesn't detect if the value falls below 0 after base/current modification.
		
		int litIndcnt = 0;
		int offIndcnt = 0;
		foreach (string litind in bombInfo.GetOnIndicators())
		{
			litIndcnt++;
		}
		foreach (string litind in bombInfo.GetOffIndicators())
		{
			offIndcnt++;
		}
		int lettersInRelation = 0;
		foreach (char letter in bombInfo.GetSerialNumber().ToCharArray())
		{
			if ("UNRELATED".Contains(letter))
			{
				lettersInRelation++;
			}
		}
		if (litIndcnt < 3 && offIndcnt < 3 && !(bombInfo.IsIndicatorOff(Indicator.BOB) && lettersInRelation >= 2))
		{
			cautionaryModules.Add("Unrelated Anagrams");
			Debug.LogFormat("[Singularity Button #{0}]: Unrelated Anagrams has no override active.",curmodID);
		}// Add Unrelated Anagrams, will remove itself after 9 or more solves have passed.

		// List of other chance solve dependent modules not shown in code:
		// Instructions, "Solved Modules" can NOT show up, no consistent way to detect if the given module is solve dependent or not.
		// Cruel Piano Keys, symbols for the solve dependent condition can NOT show up, no consistent way to detect if the given module is solve dependent or not.
		// Cruel Game of Life, has 2 overrides but green can NOT show up, no consistent way to detect if the given module is solve dependent or not.
		// The Hexabutton, no consistent way to detect if the given module is solve dependent or not.
		// Jack-O'-Lantern, no consistent way to detect if the given module is solve dependent or not.
		// Morse-A-Maze, no consistent way to detect if the given module is solve dependent or not.
		// Seven Wires, chance to get specific instances NOT the 6,12,18,24,... one, no consistent way to detect if the given module is solve dependent or not.
		// Boolean Wires, has a chance where 2 solve dependent conditions can NOT show up, no consistent way to detect if the given module is solve dependent or not.
		// Dr Doctor, For the override, (3B 3H, LIT FRK, UNLIT TRN,Forget Me Not, LIT FRQ) and then NO Fever Symptom, has a chance to NOT show up. No consistent way to detect if the given module is solve dependent or not.
		// Double Expert, a couple rules rely on solves but no consistent way to detect if the given module is solve dependent or not.
		// The Stare, you can solve this without needing this to be at a multiple of 5 solves.
		// Black Hole, you can solve this without advantagous solves.

		// END CHANCE SOLVES
	}

	void UpdateCautionaryList()
	{
		if (cautionaryModules.Contains("The Number") && bombInfo.GetSolvedModuleNames().Count >= 8)
		{
			cautionaryModules.Remove("The Number");
			Debug.LogFormat("[Singularity Button #{0}]: The bomb has exceeded a certain number of solves. The Number is no longer detected!", curmodID);
		}// Remove The Number after 8 or more solves.
		if (cautionaryModules.Contains("Unrelated Anagrams") && bombInfo.GetSolvedModuleNames().Count >= 9)
		{
			cautionaryModules.Remove("Unrelated Anagrams");
			Debug.LogFormat("[Singularity Button #{0}]: The bomb has exceeded a certain number of solves. Unrelated Anagrams is no longer detected!", curmodID);
		}// Remove Unrelated Anagrams after 9 or more solves.
		int cheapCheckoutCount = bombInfo.GetSolvableModuleNames().Where(a => a.Equals("Cheap Checkout")).Count();
		int slotsCount = bombInfo.GetSolvableModuleNames().Where(a => a.Equals("Silly Slots")).Count();
		int jewelVaultCount = bombInfo.GetSolvableModuleNames().Where(a => a.Equals("The Jewel Vault")).Count();
		if (cautionaryModules.Contains("Free Parking") &&
			cheapCheckoutCount > 0 && bombInfo.GetSolvedModuleNames().Where(a => a.Equals("Cheap Checkout")).Count() >= cheapCheckoutCount &&
			slotsCount > 0 && bombInfo.GetSolvedModuleNames().Where(a => a.Equals("Silly Slots")).Count() >= slotsCount &&
			jewelVaultCount > 0 && bombInfo.GetSolvedModuleNames().Where(a => a.Equals("The Jewel Vault")).Count() >= jewelVaultCount)
		{
			cautionaryModules.Remove("Free Parking");
			Debug.LogFormat("[Singularity Button #{0}]: Free Parking has an override active! This module is no longer detected!", curmodID);
		}// Remove Free Parking if all Cheap Checkout's, Silly Slots', and Jewel Vault's are solved.
	}
	void Awake()
	{
		curmodID = modID++;
	}
	// Use this for initialization
	void Start () {
		disarmButton.OnInteract += delegate {
			isPressedDisarm = true;
			return false;
		};
		disarmButton.OnInteractEnded += delegate
		{
			isPressedDisarm = false;
			if (isSolved)
			{
				modSelf.HandlePass();
				hasDisarmed = true;
			}
		};
		buttonFront.OnInteract += delegate
		{
			if (!isSolved && hasActivated)
			{
				
			}
			isPressedMain = true;
			return false;
		};
		buttonFront.OnInteractEnded += delegate
		{
			if (!isSolved && hasActivated)
			{

			}
			isPressedMain = false;
		};
		modSelf.OnActivate += delegate
		{
			// Setup Global Interaction
			KMBomb bombAlone = entireModule.GetComponentInParent<KMBomb>(); // Get the bomb that the module is attached on. Required for intergration due to modified value.

			if (!groupedSingularityButtons.ContainsKey(bombAlone))
				groupedSingularityButtons[bombAlone] = new SingularityButtonInfo();
			singularityButtonInfo = groupedSingularityButtons[bombAlone];
			singularityButtonInfo.singularButtons.Add(this);

			// Start Main Handling
			AddOthersModulesOntoList();
			StartCoroutine(HandleGlobalModule());
			hasActivated = true;
		};
	}
	IEnumerator HandleGlobalModule()
	{
		while (!singularityButtonInfo.canModuleDisarm())
		{
			UpdateCautionaryList();
			yield return new WaitForSeconds(0);
		}
		isSolved = true;
		if (!bombInfo.GetSolvableModuleNames().Where(a => cautionaryModules.Contains(a)).Any() || singularityButtonInfo.CountSingularityButtons() == 1) // Does the bomb contain any cautionary modules or is there 1 present on this bomb?
		{
			hasDisarmed = true;
			modSelf.HandlePass();
		}
		else
			Debug.LogFormat("[Singularity Button #{0}]: At least one cautionary module is present on the bomb. You must instead press the manual disarm button to disarm this module!", curmodID);
		yield return null;
	}
	// Update is called once per frame
	int frameMain = 45;
	int frameDisarm = 45;
	public int frameSwitch = 0;
	int animLength = 30;
	void Update () {
		if (!hasActivated) return;
		if (!isPressedMain)
		{
			frameMain = Mathf.Min(frameMain + 1, 45);
		}
		else
			frameMain = Mathf.Max(frameMain - 1, 30);
		if (!isPressedDisarm)
		{
			frameDisarm = Mathf.Min(frameDisarm + 1, 45);
		}
		else
			frameDisarm = Mathf.Max(frameDisarm - 1, 40);
		if (isSolved&&!hasDisarmed)
		{
			frameSwitch = Mathf.Min(frameSwitch + 1, animLength);
		}
		else
		{
			frameSwitch = Mathf.Max(frameSwitch - 1, 0);
		}
		buttonFrontObject.transform.localPosition = new Vector3(0, 0.03f * (frameMain / 45f), 0);
		disarmButtonObject.transform.localPosition = new Vector3(0, -0.019f * (frameDisarm / 45f), 0);
		animatedPortion.transform.localEulerAngles = new Vector3(0, 0, 180f * (frameSwitch / (float)animLength));
	}

	IEnumerator HandleForcedSolve()
	{
		while (frameSwitch < animLength)
			yield return new WaitForSeconds(0);
		disarmButton.OnInteract();
		yield return new WaitForSeconds(0.1f);
		disarmButton.OnInteractEnded();
	}

	void TwitchHandleForcedSolve()
	{
		singularityButtonInfo.DisarmAll(); // Call the protected method
		Debug.LogFormat("[Singularity Button #{0}]: A force solve has been issued viva TP Handler. ALL Singularity Buttons will be set to a solve state because of it.", curmodID);
		StartCoroutine(HandleForcedSolve());
	}
	IEnumerator ProcessTwitchCommand(string command)
	{
		yield return null;
	}
}
