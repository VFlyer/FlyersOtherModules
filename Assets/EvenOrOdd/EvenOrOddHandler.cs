using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EvenOrOddHandler : MonoBehaviour {

	public KMSelectable evenSelectable, oddSelectable, displaySelectable;
	public KMBombModule modSelf;
	public KMAudio mAudio;
	public TextMesh digitMesh, timerMesh;

	private float timeLeft = 30f;
	private int displayNo, totalOverall, correctPresses, requiredPresses;
	bool isRedText, isActive, interactable = true;

	private static int modCounter = 1;
	int modIDLog;
	IEnumerator countDownCoroutine;

	List<int> allDigits = new List<int>();

	// Use this for initialization
	void Awake()
	{

	}

	void Start () {
		modIDLog = modCounter++;
		evenSelectable.OnInteract += delegate {
			mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, evenSelectable.transform);
			evenSelectable.AddInteractionPunch();
			StartCoroutine(HandleButtonEvenAnim());
			if (isActive && interactable)
			{
				ProcessInput(true);
			}
			return false;
		};
		oddSelectable.OnInteract += delegate {
			mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, oddSelectable.transform);
			oddSelectable.AddInteractionPunch();
			StartCoroutine(HandleButtonOddAnim());
			if (isActive && interactable)
			{
				ProcessInput(false);
			}
			return false;
		};
		displaySelectable.OnInteract += delegate {
			mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonRelease, displaySelectable.transform);
			displaySelectable.AddInteractionPunch();
			if (!isActive && interactable)
			{
				StartCountDown();
			}
			return false;
		};
		LogModule("Tap the display to start the module.");
		timerMesh.text = "";
		digitMesh.text = "";
	}

	void LogModule(string toLog)
    {
		Debug.LogFormat("[Even Or Odd #{0}]: {1}", modIDLog, toLog);
    }

	void ProcessInput(bool pressedEven)
    {
		if (totalOverall % 2 == 0 == !isRedText == pressedEven)
        {
			correctPresses++;
			if (correctPresses >= requiredPresses)
            {
				LogModule(string.Format("Enough correct presses have been made to disarm the module."));
				StopCoroutine(countDownCoroutine);
				mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
				modSelf.HandlePass();
				interactable = false;
				StartCoroutine(HandleSolveAnim());
            }
			else
            {
				StartCoroutine(HandleFlickerChangeAnim());
				interactable = false;
				if (TwitchPlaysActive)
					timeLeft = 30;
            }
        }
		else
        {
			LogModule(string.Format("Strike! The {0} button was pressed for the display {1} in {3} (press no. {2})", pressedEven ? "even" : "odd",
				displayNo, correctPresses + 1, isRedText ? "red" : "green"));
			LogModule(string.Format("For reference, all digits shown before this strike were {0}", allDigits.Join(", ")));
			modSelf.HandleStrike();
			ResetModule();
			LogModule("Tap the display to restart the module.");
		}
    }

	void GenerateValue()
    {
		displayNo = Random.Range(0, 10);
		isRedText = Random.value < 0.5f;
		digitMesh.text = displayNo.ToString();
		digitMesh.color = isRedText ? Color.red : Color.green;
		allDigits.Add(displayNo);
		LogModule(string.Format("The displayed number is now {0} in {1}.", displayNo, isRedText ? "red" : "green"));
		totalOverall += displayNo;
		totalOverall %= 10;
		if (correctPresses > 0)
			LogModule(string.Format("The total value is now {0}.", totalOverall));
		LogModule(string.Format("Therefore you should press the {0} button.", (totalOverall % 2 == 0 == !isRedText) ? "even" : "odd"));
	}

	IEnumerator HandleButtonEvenAnim()
    {
		for (int x = 3; x >= 0; x--)
		{
			evenSelectable.gameObject.transform.localPosition -= Vector3.up * .001f;
			yield return null;
		}
        for (int x = 0; x <= 3; x++)
		{
			evenSelectable.gameObject.transform.localPosition += Vector3.up * .001f;
			yield return null;
		}
	}
	IEnumerator HandleButtonOddAnim()
	{
		for (int x = 3; x >= 0; x--)
		{
			oddSelectable.gameObject.transform.localPosition -= Vector3.up * .001f;
			yield return null;
		}
		for (int x = 0; x <= 3; x++)
		{
			oddSelectable.gameObject.transform.localPosition += Vector3.up * .001f;
			yield return null;
		}
	}
	void StartCountDown()
    {
		interactable = false;
		requiredPresses = Random.Range(8, 13);
		countDownCoroutine = CountdownTimeLeft();
		mAudio.PlaySoundAtTransform("startUp", transform);
		StartCoroutine(countDownCoroutine);
	}
	
	void ResetModule()
    {
		totalOverall = 0;
		isActive = false;
		correctPresses = 0;
		timeLeft = 30;
		StopCoroutine(countDownCoroutine);
		timerMesh.text = "";
		digitMesh.text = "";
		allDigits.Clear();
	}
	IEnumerator HandleFlickerChangeAnim()
    {
		digitMesh.text = "";
		yield return new WaitForSeconds(0.05f);
		GenerateValue();
		if (!TwitchPlaysActive)
			timeLeft += 0.05f;
		interactable = true;
    }

	IEnumerator HandleSolveAnim()
    {
		
		for (int x = 0; x <= 60; x++)
		{
			float randomTime = Random.Range(0f, 30f);

			timerMesh.text = randomTime < 9.9f ? randomTime.ToString("0.0") : randomTime.ToString("00");
			digitMesh.text = Random.Range(0, 10).ToString();
			yield return new WaitForFixedUpdate();
			yield return new WaitForFixedUpdate();
			timerMesh.color = Color.white * (60 - x) / 60f;
			digitMesh.color = (isRedText ? Color.red : Color.green) * (60 - x) / 60f;
		}
		timerMesh.text = "";
		digitMesh.text = "/";
		for (float x = 0; x <= 1f; x += Time.deltaTime / 1.5f)
		{
			yield return null;
			digitMesh.color = Color.white * (1f - Mathf.Abs(2 * x - 1));
		}
		digitMesh.color = Color.clear;
	}
	IEnumerator CountdownTimeLeft()
    {
		for (int x = 0; x < timeLeft; x++)
        {
			timerMesh.text = x < 10 ? x.ToString("0.0") : x.ToString("00");
			yield return new WaitForSeconds(1 / 30f);
        }
		isActive = true;
		interactable = true;
		GenerateValue();
		while (timeLeft > 0f)
		{
			yield return null;
			timeLeft -= Time.deltaTime;
			timerMesh.text = timeLeft < 9.9f ? timeLeft.ToString("0.0") : timeLeft.ToString("00");
		}
		isActive = false;
		LogModule("You've ran out of time! Strike!");
		LogModule(string.Format("For reference, all digits shown before this strike were {0}", allDigits.Join(", ")));
		modSelf.HandleStrike();
		ResetModule();
		LogModule("Tap the display to restart the module.");
	}

	// Update is called once per frame
	void Update () {

	}

	// TP Support Begins Here

	IEnumerator TwitchHandleForcedSolve()
    {
		while (!interactable)
			yield return true;
		if (!isActive)
		{
			yield return null;
			displaySelectable.OnInteract();
			yield return new WaitWhile(delegate { return !isActive; });
		}
		while (correctPresses < requiredPresses)
		{
			yield return new WaitUntil(delegate { return interactable; });
			if (totalOverall % 2 == 0 == !isRedText)
			{
				yield return null;
				evenSelectable.OnInteract();
			}
			else
			{
				yield return null;
				oddSelectable.OnInteract();
			}
		}
		yield return true;
    }

#pragma warning disable IDE0051 // Remove unused private members
#pragma warning disable IDE0044 // Add readonly modifier
	readonly string TwitchHelpMessage = "Press the display with \"!{0} display\", \"!{0} even/odd\" or \"!{0} e/o\" to press the even or odd buttons respectively. On Twitch Plays, the timer resets to 30 seconds for every correct press.";
    bool TwitchPlaysActive;
#pragma warning restore IDE0044 // Add readonly modifier
#pragma warning restore IDE0051 // Remove unused private members
    IEnumerator ProcessTwitchCommand(string command)
	{
		if (command.EqualsIgnoreCase("display"))
        {
			yield return null;
			displaySelectable.OnInteract();
			yield return "strike";
        }
		else if (command.EqualsIgnoreCase("even") || command.EqualsIgnoreCase("e"))
		{
			yield return null;
			evenSelectable.OnInteract();
			yield return "strike";
		}
		else if (command.EqualsIgnoreCase("odd") || command.EqualsIgnoreCase("o"))
		{
			yield return null;
			oddSelectable.OnInteract();
			yield return "strike";
		}
		else
			yield return string.Format("sendtochaterror I do not know of a command \"{0}\" on the module.",command);
		yield break;
	}
}
