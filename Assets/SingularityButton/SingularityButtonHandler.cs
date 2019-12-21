using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KModkit;
using System.Linq;

public class SingularityButtonHandler : MonoBehaviour {

	public KMSelectable disarmButton, buttonFront;
	public KMBossModule bossModule;
	public KMBombInfo bombInfo;
	public KMBombModule modSelf;

	private static bool isSolved;
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

	sealed class SingularityButtonInfo //Lock down infomation to a single bomb, hopefully.
	{
		public List<SingularityButtonHandler> singularButtons = new List<SingularityButtonHandler>();
		public List<int> inputs = new List<int>();
		public List<string> buttonColors = new List<string>();
		public List<int> buttonDigits = new List<int>();

	}

	void AddOthersModulesOntoList()
	{
		cautionaryModules.AddRange(bossModule.GetIgnoredModules("Singularity Button", new string[]
		{
			"Cookie Jars",
			"Divided Squares",
			"Forget Enigma",
			"Forget Everything",
			"Forget It Not",
			"Forget Me Not",
			"Forget Perspective",
			"Forget Them All",
			"Forget This",
			"Forget Us Not",
			"Hogwarts",
			"Organization",
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
		}// Add Free Parking, note that it doesn't detect if the value falls below 0 after modification.
		
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
		// The Stare, you can solve this without needing this to be at a multiple of 5 solves.
		// Black Hole, you can solve this without advantagous solves.
		// Boolean Wires, has a chance where 2 solve dependent conditions can NOT show up, no consistent way to detect if the given module is solve dependent or not.
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
		if (cautionaryModules.Contains("Free Parking") && (bombInfo.GetSolvableModuleNames().Where(a => a.Equals("Cheap Checkout") || a.Equals("Silly Slots") || a.Equals("The Jewel Vault")).Count() == bombInfo.GetSolvedModuleNames().Where(a => a.Equals("Cheap Checkout") || a.Equals("Silly Slots") || a.Equals("The Jewel Vault")).Count()))
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
			modSelf.HandlePass();
		};
		buttonFront.OnInteract += delegate
		{
			isPressedMain = true;
			return false;
		};
		buttonFront.OnInteractEnded += delegate
		{
			isPressedMain = false;
			
		};
		bombInfo.OnBombExploded += delegate {
			isSolved = false;
		};
		bombInfo.OnBombSolved += delegate {
			isSolved = false;
		};
	}

	// Update is called once per frame
	void Update () {
	}
}
