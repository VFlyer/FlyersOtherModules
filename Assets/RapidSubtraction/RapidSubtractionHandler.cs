using KModkit;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public class RapidSubtractionHandler : MonoBehaviour {
	public KMSelectable[] buttonDigitsSelectables = new KMSelectable[10];
	public KMSelectable clrButtonSelectable;
	public KMSelectable subButtonSelectable;
	public KMNeedyModule needyHandler;
	public MeshRenderer[] progressRenderers;
	public KMBombInfo bombInfo;
	public KMAudio audioHandler;

	public TextMesh displayText, inputText;

	static int modID = 1;
	int curModID;
	int[] streakValues = new int[4];

	int currentValue, valueToSubtract, digitsHidden = 0, curStreak = 0;
	bool isActivated, forceDisable;

	float[] streakNeedyActivateDelayMin = { 45f, 75f, 105f, 135f };
	float[] streakNeedyActivateDelayMax = { 60f, 90f, 150f, 240f };

	string[] debugPositionalNums = { "1st", "2nd", "3rd", "4th", "5th", "6th", "7th", "8th", "9th", "10th", "11th", "12th", "13th", "14th", "15th", "16th", "17th", "18th", "19th" };
	int debugValInput = 0;
	IEnumerator revealAnim;
	// Use this for initialization
	void Start()
	{
		curModID = modID++;

		for (int x = 0; x < 10; x++)
		{
			int y = x;
			buttonDigitsSelectables[x].OnInteract += delegate
			{
				StartCoroutine(HandleButtonAnim(buttonDigitsSelectables[y].gameObject));
				audioHandler.PlaySoundAtTransform("keyStroke", buttonDigitsSelectables[y].transform);
				if (inputText.text.Length < 6 && isActivated)
					inputText.text += y;
				return false;
			};

		}
		clrButtonSelectable.OnInteract += delegate
		{
			StartCoroutine(HandleButtonAnim(clrButtonSelectable.gameObject));
			audioHandler.PlaySoundAtTransform("keyStroke", clrButtonSelectable.transform);
			if (isActivated)
			{
				if (inputText.text.Length > 0)
					inputText.text = "";
				else
				{
					float timeLeft = needyHandler.GetNeedyTimeRemaining();
					if (timeLeft - 5 > 0.5f && digitsHidden > 0)
					{
						needyHandler.SetNeedyTimeRemaining(timeLeft - 5f);
						revealAnim = HandleRerevealDelay(digitsHidden * 1);
						StartCoroutine(revealAnim);
					}
				}

			}
			return false;
			
		};
		subButtonSelectable.OnInteract += delegate
		{
			StartCoroutine(HandleButtonAnim(subButtonSelectable.gameObject));
			audioHandler.PlaySoundAtTransform("keyStroke", subButtonSelectable.transform);
			if (isActivated)
			{
				int givenValue = 0;
				if (inputText.text.Length > 0 && int.TryParse(inputText.text, out givenValue))
				{
					int goalValue = currentValue - valueToSubtract;
					if (givenValue == goalValue)
					{
						
						currentValue = givenValue;
						debugValInput++;
						if (currentValue < 10)
						{
							audioHandler.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
							IncreaseStreak();
						}
						else
						{
							audioHandler.PlaySoundAtTransform("correct", transform);
							digitsHidden = Mathf.Min(digitsHidden + 1, 2);
							needyHandler.SetNeedyTimeRemaining(needyHandler.GetNeedyTimeRemaining() + (TwitchPlaysActive ? 8f : 5f));
						}
					}
					else
					{
						QuickLog(string.Format("Strike! {0} was incorrectly entered when {1} was expected for the {2} input.", inputText.text, goalValue,debugPositionalNums[debugValInput]));
						ResetStreak();
						needyHandler.HandlePass();
					}
					inputText.text = "";
				}
			}
			
			return false;
		};
		needyHandler.OnActivate += delegate {
			PrepValues();
		};
		needyHandler.OnNeedyActivation += delegate
		{
			if (forceDisable)
			{
				needyHandler.HandlePass();
				return;
			}
			digitsHidden = 0;
			valueToSubtract = curStreak > 3 ? streakValues.Last() : streakValues[curStreak];
			currentValue = Random.Range(85, 100);
			QuickLog(string.Format("Starting Value: {0}", currentValue));
			List<int> debugValues = new List<int>();
			for (int logValue = currentValue - valueToSubtract; logValue > 0; logValue -= valueToSubtract)
			{
				debugValues.Add(logValue);
				if (logValue < 10) break;
			}
			QuickLog(string.Format("Answers to submit at {1} streak: [ {0} ]", debugValues.Join(", "), curStreak >= 3 ? "3+" : curStreak.ToString()));
			isActivated = true;
			if (TwitchPlaysActive)
				needyHandler.SetNeedyTimeRemaining(35f);
		};
		needyHandler.OnTimerExpired += delegate
		{
			QuickLog("Letting the needy timer run out was not a good idea after all.");
			ResetStreak();
		};
		needyHandler.OnNeedyDeactivation += delegate
		{
			isActivated = false;
			StopAllCoroutines();
		};
		inputText.text = "";
		for (var x = 0; x < progressRenderers.Length; x++)
		{
			progressRenderers[x].material.color = Color.green;
			progressRenderers[x].enabled = false;
		}
	}

	IEnumerator HandleButtonAnim(GameObject gameObject)
	{
		for (int x = 0; x < 5; x++)
		{
			gameObject.transform.localPosition -= new Vector3(0, 0, 0.001f);
			yield return new WaitForSeconds(0.01f);
		}
		for (int x = 0; x < 5; x++)
		{
			gameObject.transform.localPosition += new Vector3(0, 0, 0.001f);
			yield return new WaitForSeconds(0.01f);
		}
		yield return null;
	}

	void ModifyDelay()
	{
		int streakCount = Mathf.Min(curStreak, 3);
		needyHandler.SetResetDelayTime(streakNeedyActivateDelayMin[streakCount], streakNeedyActivateDelayMax[streakCount]);
	}
	void PrepValues()
	{
		char[] vowelList = { 'A', 'E', 'I', 'O', 'U' };
		int indicatorCount = bombInfo.GetIndicators().Count();
		int batteryCount = bombInfo.GetBatteryCount();
		bool isSerialPresent = bombInfo.IsPortPresent(Port.Serial);
		bool isVowelPresent = bombInfo.GetSerialNumberLetters().Where(a => vowelList.Contains(a)).Any();
		for (var x = 0; x < 4; x++)
		{
			switch (x)
			{
				case 0:
					streakValues[x] = batteryCount == 3 ? 5 : indicatorCount > 3 ? 6 : isVowelPresent ? 7 : isSerialPresent ? 8 : 9;
					break;
				case 1:
					streakValues[x] = batteryCount < 3 ? 6 : indicatorCount == 3 ? 7 : !isVowelPresent ? 8 : 9;
					break;
				case 2:
					streakValues[x] = batteryCount > 3 ? 7 : indicatorCount < 3 ? 8 : 9;
					break;
				case 3:
					streakValues[x] = 9;
					break;
			}
		}
		QuickLog(string.Format("Values to subtract by for [ 0, 1, 2, 3+ ] streak: [ {0} ]", streakValues.Join(", ")));
	}
	void UpdateStreak()
    {
		for (var x = 0; x < progressRenderers.Length; x++)
        {
			progressRenderers[x].enabled = x < curStreak;
        }
    }


	IEnumerator HandleRerevealDelay(int lastHiddenDigitCount)
	{
		digitsHidden = 0;
		yield return new WaitForSeconds(2f);
		digitsHidden = lastHiddenDigitCount;
	}

	void QuickLog(string debugLog)
	{
		Debug.LogFormat("[Rapid Subtraction #{0}] {1}", curModID, debugLog);
	}
	void IncreaseStreak()
	{
		curStreak++;
		isActivated = false;
		debugValInput = 0;
		ModifyDelay();
		UpdateStreak();
		needyHandler.HandlePass();
	}
	void ResetStreak()
	{
		needyHandler.HandleStrike();
		curStreak = 0;
		isActivated = false;
		debugValInput = 0;
		if (revealAnim != null)
			StopCoroutine(revealAnim);
		ModifyDelay();
		StartCoroutine(FlashStreak());
	}

	IEnumerator FlashStreak()
    {
		for (var y = 0; y < 5; y++)
		{
			for (var x = 0; x < progressRenderers.Length; x++)
			{
				progressRenderers[x].material.color = Color.red;
			}
			yield return new WaitForSeconds(0.1f);
			for (var x = 0; x < progressRenderers.Length; x++)
			{
				progressRenderers[x].material.color = Color.black;
			}
			yield return new WaitForSeconds(0.1f);
		}
		for (var x = 0; x < progressRenderers.Length; x++)
		{
			progressRenderers[x].material.color = Color.green;
			progressRenderers[x].enabled = false;
		}
	}

	// Update is called once per frame
	float curDelay = 0f;
	void Update () {
		if (curDelay > 0.03f)
		{
			displayText.text = isActivated ?
					(digitsHidden > 1 ? Random.Range(0, 10).ToString() : (currentValue / 10).ToString()) + (digitsHidden > 0 ? Random.Range(0, 10).ToString()
					: (currentValue % 10).ToString()) : "";
			curDelay = 0f;
		}
		else
			curDelay += Time.deltaTime;
	}
	// TP Handling
#pragma warning disable IDE0051 // Remove unused private members
	readonly string TwitchHelpMessage = "Submit an answer with \"!{0} submit ##\" Multiple answers can be submitted in one command. To check on the current value again for a short bit: \"!{0} reshow\" For Twitch Plays, the needy timer will start at 35 seconds and answering correctly will add 8 seconds rather than 5.";
	bool TwitchPlaysActive;
#pragma warning restore IDE0051 // Remove unused private members



	void TwitchHandleForcedSolve()
	{
		QuickLog("Forcably disabling the needy viva TP Handler.");
		needyHandler.SetResetDelayTime(float.MaxValue, float.MaxValue);
		needyHandler.HandlePass();
		needyHandler.OnNeedyDeactivation();
		forceDisable = true;
	}

	IEnumerator ProcessTwitchCommand(string cmd)
	{
		if (!isActivated)
		{
			yield return "sendtochaterror This needy is not activated yet. Wait for a moment until the needy is activated.";
			yield break;
		}
		cmd = cmd.ToLower();
		if (cmd.RegexMatch(@"^(reshow)$"))
		{
			if (inputText.text.Any())
			{
				yield return null;
				clrButtonSelectable.OnInteract();
			}
			yield return null;
			clrButtonSelectable.OnInteract();
		}
		if (cmd.RegexMatch(@"^(answer|submit)(\s\d{1,2})+$"))
		{
			string[] cmdParts = cmd.Split();
			if (inputText.text.Any())
			{
				yield return null;
				clrButtonSelectable.OnInteract();
			}
			for (int x = 1; x < cmdParts.Length; x++)
			{
				if (!isActivated)
				{
					yield return "sendtochat {0}, Rapid Subtraction #{1} has already deactivated itself.";
					yield break;
				}
				for (int y = 0; y < cmdParts[x].Length; y++)
				{
					yield return null;
					buttonDigitsSelectables[int.Parse(cmdParts[x][y].ToString())].OnInteract();
					yield return new WaitForSeconds(0.05f);
				}
				yield return "multiple strikes";
				if (int.Parse(cmdParts[x]) != currentValue - valueToSubtract && cmdParts.Length > 2)
					yield return string.Format("strikemessage incorrectly inputting {0} after {1} input(s) in the TP command provided!", cmdParts[x], x);
				subButtonSelectable.OnInteract();
				yield return "end multiple strikes";
				yield return new WaitForSeconds(0.05f);
			}
		}
		yield break;
	}
}
