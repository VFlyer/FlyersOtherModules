using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SingularityButtonHandler : MonoBehaviour {

	public KMSelectable disarmButton, buttonFront;
	public KMBossModule bossModule;
	public KMBombInfo bombInfo;
	public KMBombModule modSelf;

	private static bool isSolved;
	private bool hasActivated = false;

	// Use this for initialization
	void Start () {
		disarmButton.OnInteract += delegate
		{
			if (isSolved)
			modSelf.HandlePass();
			return false;
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
