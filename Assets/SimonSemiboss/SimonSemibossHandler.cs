using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Reflection;
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

	bool isSolved = false, mashToSolve = false;

	static int modID = 1;
	int curmodID;
	// Use this for initialization
	void Awake()
	{
		curmodID = modID++;
		ignoredModuleNames = bossModuleHandler.GetIgnoredModules("Simon", new string[] {
		"14",
		"Bamboozling Time Keeper",
		"Cookie Jars",
		"The Digits",
		"Divided Squares",
		"Forget Enigma",
		"Forget Everything",
		"Forget Infinity",
		"Forget It Not",
		"Forget Me Later",
		"Forget Me Not",
		"Forget The Colors",
		"Forget Them All",
		"Forget This",
		"Forget Us Not",
		"The Heart",
		"Hogwarts",
		"Iconic",
		"Multitask",
		"Mystery Module",
		"Organization",
		"Purgatory",
		"Random Access Memory",
		"RPS Judging",
		"Simon",
		"Simon Forgets",
		"Simon's Stages",
		"Souvenir",
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
		});
		Debug.LogFormat("<Simon #{0}> Ignored Modules: {1}", curmodID, ignoredModuleNames.Join(","));
	}
	bool IsOrganMysteryPresentAndNotIgnoring()
	{
		List<string> allModules = bombInfo.GetSolvableModuleNames();
		if (allModules.Contains("Organization") || allModules.Contains("Mystery Module"))
		{

		}
		return false;
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
		}
	}

	void GenerateFlashes()
	{

	}

	IEnumerator PanicAnim()
	{
		float lastZPos = faceRender.gameObject.transform.localPosition.z;
		while (!isSolved)
		{
			faceRender.gameObject.transform.localPosition = new Vector3(UnityEngine.Random.Range(-.001f, .001f), UnityEngine.Random.Range(-.001f, .001f), lastZPos);
			yield return new WaitForSeconds(0.1f);
		}
		yield return null;
	}

	// Update is called once per frame
	void Update () {

	}
}
