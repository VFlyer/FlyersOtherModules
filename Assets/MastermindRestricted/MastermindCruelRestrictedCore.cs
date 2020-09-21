using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using UnityEngine;

public class MastermindCruelRestrictedCore : MastermindRestrictedCore {

	private static int modCounter = 1;
	
	// Use this for initialization
	void Start () {
		loggingID = modCounter++;
		ResetModule();
		resetButton.OnInteract += delegate {
			resetButton.AddInteractionPunch();
			audioKM.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, resetButton.transform);
			if (interactable)
				ResetModule();
			return false;
		};
		queryButton.OnInteract += delegate {
			queryButton.AddInteractionPunch();
			audioKM.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, queryButton.transform);
			if (interactable)
				QueryModule();
			return false;
		};
		for (int x = 0; x < possibleSelectables.Length; x++)
		{
			int y = x;
			possibleSelectables[x].OnInteract += delegate {
				audioKM.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, queryButton.transform);
				if (interactable)
				{
					currentInputs[y] = (currentInputs[y] + 1) % maxPossible;
					UpdateCurrentDisplay();
				}
				return false;
			};
		}
		if (!colorblindDetected)
		{
			foreach (TextMesh aMesh in correctColorblindText)
			{
				aMesh.text = "";
			}
		}
	}

	protected override void QuickLog(string toLog)
    {
		Debug.LogFormat("[Mastermind Cruel Restricted #{0}]: {1}", loggingID, toLog);
	}

    // Update is called once per frame
    void Update () {

	}
}
