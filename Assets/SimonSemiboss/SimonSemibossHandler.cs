using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using uernd = UnityEngine.Random;
using System;

public class SimonSemibossHandler : MonoBehaviour {

	public KMSelectable[] possibleButtons = new KMSelectable[8];
	public KMBombInfo bombInfo;
	public KMColorblindMode colorblindMode;
	public KMBossModule bossModuleHandler;
	public KMBombModule moduleSelf;

	public MeshRenderer faceRender;
	public Texture[] faces = new Texture[3];

	string[] ignoredModuleNames;

	List<int> possiblePressIdx = new List<int>();

	bool isSolved, mashToSolve, hasStarted, isPaniking, alterDefaultHandling;

	static int modID = 1;
	int curmodID, solveCountActivation;
	// Use this for initialization
	void Awake()
	{
		curmodID = modID++;
		ignoredModuleNames = bossModuleHandler.GetIgnoredModules("Simon", new string[] {
		"14",
		"42",
		"501",
		"Amnesia",
		"A>N<D",
		"Bamboozling Time Keeper",
		"Brainf---",
		"Busy Beaver",
		"Button Messer",
		"Cookie Jars",
		//"The Digits",
		"Divided Squares",
		"Don't Touch Anything",
		"Encrypted Hangman",
		"Encryption Bingo",
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
		"Kugelblitz",
		"Multitask",
		"Mystery Module",
		"OmegaForget",
		"Organization",
		"Purgatory",
		"Random Access Memory",
		"RPS Judging",
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
		Debug.LogFormat("<Simon #{0}> Ignored Modules: {1}", curmodID, ignoredModuleNames.Join(","));
	}
	IEnumerator HandleOrganMysteryModuleCore()
	{
		var mBomb = GetComponentInParent<KMBomb>();
		while (mBomb != null)
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
						if (selectedHiddenModule != null && selectedHiddenModule.Equals(moduleSelf))
                        {
							alterDefaultHandling = true;
							aMysteryModule.OnPass += delegate {
								GenerateFlashes();
								return false;
							};
                        }
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
                }
			}
		}
	}
	void Start () {
		int unignoredModuleCount = bombInfo.GetSolvableModuleNames().Where(a => !ignoredModuleNames.Contains(a)).Count();
		Debug.LogFormat("[Simon #{0}]: Detected this many unignored modules on the bomb: {1}", curmodID, unignoredModuleCount);
		if (unignoredModuleCount / 5 > 0)
		{
			Debug.LogFormat("[Simon #{0}]: Simon will start paniking at this many solves: {1}.", curmodID);
		}
		else
		{
			Debug.LogFormat("[Simon #{0}]: Simon will start paniking as soon as the lights turn on.", curmodID);
			mashToSolve = true;
			isPaniking = true;
			StartCoroutine(PanicAnim());
		}
		moduleSelf.OnActivate += delegate {
			hasStarted = true;
		};
	}

	void GenerateFlashes()
	{
		int solveCountUponGeneration = bombInfo.GetSolvedModuleIDs().Count();
		Debug.LogFormat("[Simon #{0}]: Simon has started paniking at {1} solve(s).", curmodID, solveCountUponGeneration);
		var breakLoop = false;
		for (int x = 0; x < solveCountUponGeneration && !breakLoop; x++)
        {
			for (int y = 0; y < 3; y++)
            {
				possiblePressIdx.Add(uernd.Range(0, 8));
				if (possiblePressIdx.Count >= 40)
				{
					breakLoop = true;
					break;
				};
            }
        }
		isPaniking = true;
		solveCountActivation = bombInfo.GetSolvableModuleNames().Count(a => !ignoredModuleNames.Contains(a));
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
		yield return null;
	}

	// Update is called once per frame
	void Update () {
		if (hasStarted && !isSolved)
		{
			var nonIgnoredSolves = bombInfo.GetSolvedModuleNames().Count(a => !ignoredModuleNames.Contains(a));
			if (isPaniking)
			{
				if (nonIgnoredSolves > solveCountActivation)
				{
					solveCountActivation = nonIgnoredSolves;
					Debug.LogFormat("[Simon #{0}]: Simon got angry for being ignored. He put an \"X\" on the timer.", curmodID);
					moduleSelf.HandleStrike();
				}
            }
			else if (!alterDefaultHandling)
            {

            }
		}
	}
}
