using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class CruelModuloScript : MonoBehaviour {

	public KMSelectable[] digitSelectables;
	public KMSelectable submitButton, clearButton;
	public TextMesh startingDisplay, divisorDisplay, exponentDisplay, resultDisplay;
	public KMBombModule modSelf;
	public KMAudio mAudio;

	static int modIDCnt = 1;
	int modID, correctValue, fixedExponent;
	bool interactable = false, lockExponent = false, moduleSolved = false;

	// Use this for initialization
	void Start () {
		modID = modIDCnt++;
		resultDisplay.text = "";
		exponentDisplay.text = "";
		divisorDisplay.text = "";
		startingDisplay.text = "";
		GenerateExpression();

        for (var x = 0; x < digitSelectables.Length; x++)
        {
			int y = x;
			digitSelectables[x].OnInteract += delegate {
				digitSelectables[y].AddInteractionPunch();
				mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, digitSelectables[y].transform);
				if (interactable && resultDisplay.text.Length < 2)
                {
					resultDisplay.text += y.ToString();
                }

				return false;
			};

        }

		submitButton.OnInteract += delegate {
			submitButton.AddInteractionPunch();
			mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, submitButton.transform);
			if (interactable)
			{
				int submittedValue;
				if (int.TryParse(resultDisplay.text, out submittedValue))
				{
					
					if (submittedValue == correctValue)
                    {
						QuickLog(string.Format("You submitted the correct value. Module disarmed.", correctValue));
						interactable = false;
						moduleSolved = true;
						modSelf.HandlePass();
                    }
                    else
                    {
						QuickLog(string.Format("You submitted {0}, which is incorrect! Starting over...", submittedValue));
						modSelf.HandleStrike();
                        resultDisplay.text = "";
                        GenerateExpression();
                    }
                }
			}
			return false;
		};

		clearButton.OnInteract += delegate {
			clearButton.AddInteractionPunch();
			mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, clearButton.transform);
			if (interactable)
            {
				resultDisplay.text = "";
            }
			return false;
		};

	}

	void GenerateExpression()
    {
		var startingValue = Random.Range(50, 1000);
		var divisorValue = Random.Range(3, 51);
		if (!lockExponent)
		{
			fixedExponent = Random.Range(10, 31);
			lockExponent = true;
		}

		QuickLog(string.Format("The starting value is {0}.", startingValue));
		QuickLog(string.Format("The divisor is {0}.", divisorValue));
		QuickLog(string.Format("The exponent is {0}.", fixedExponent));
		divisorDisplay.text = divisorValue.ToString();
		startingDisplay.text = startingValue.ToString();
		exponentDisplay.text = fixedExponent.ToString();

		var listModValues = new List<int>();
		var factoredValue = startingValue % divisorValue;
		listModValues.Add(factoredValue);
		for (var y = 1; y <= Mathf.Min(divisorValue, fixedExponent); y++)
		{
			var lastModuloValue = listModValues.Last();
			var calculatedNextModuloValue = lastModuloValue * factoredValue % divisorValue;
			if (listModValues.Contains(calculatedNextModuloValue)) break;
			listModValues.Add(calculatedNextModuloValue);
		}
		QuickLog(string.Format("Using the alternative method, the pattern of values before repetition and before the exponent, starting at {1}%{2} are {0}", listModValues.Join(), startingValue, divisorValue));
		correctValue = listModValues.Min() == 0 ? 0 : listModValues.ElementAt((fixedExponent + listModValues.Count - 1) % listModValues.Count);

		QuickLog(string.Format("The value you should submit is {0}. (From the expression: {1} ^ {2} % {3})", correctValue, startingValue, fixedExponent, divisorValue));
		interactable = true;

	}

	void QuickLog(string value)
    {
		Debug.LogFormat("[Cruel Modulo #{0}] {1}", modID, value);
    }
	// TP Handler Begins Here
	IEnumerator TwitchHandleForcedSolve()
    {
		while (!moduleSolved)
		{
			while (!interactable)
				yield return true;
			int curValue;
			if (int.TryParse(resultDisplay.text, out curValue))
			{
				if (curValue != correctValue)
				{
					clearButton.OnInteract();
					yield return new WaitForSeconds(0.1f);
				}
			}
			var correctValueIdxes = correctValue.ToString().ToCharArray().Select(a => "0123456789".IndexOf(a));
			for (var x = 0; x < correctValueIdxes.Count(); x++)
            {
				digitSelectables[correctValueIdxes.ElementAt(x)].OnInteract();
				yield return new WaitForSeconds(0.1f);
			}
			submitButton.OnInteract();
		}
    }
#pragma warning disable IDE0051 // Remove unused private members
	readonly string TwitchHelpMessage = "Submit the following number with \"!{0} submit #\" or \"!{0} ##\"";
#pragma warning restore IDE0051 // Remove unused private members
	IEnumerator ProcessTwitchCommand(string cmd)
    {
		if (Application.isEditor)
        {
			cmd = cmd.Trim();
        }
		Match cmdNumber = Regex.Match(cmd, @"^(submit\s)?\d+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		if (cmdNumber.Success)
		{
			var correctValueIdxes = cmdNumber.Value.Replace("submit","").Trim().ToCharArray().Select(a => "0123456789".IndexOf(a));
			if (correctValueIdxes.Count() > 2)
            {
				yield return "sendtochaterror You are trying to submit too many digits on the module. The module only accepts 1-2 digit answers.";
				yield break;
            }
			yield return null;
			if (resultDisplay.text.Any())
			{
				clearButton.OnInteract();
				yield return new WaitForSeconds(0.1f);
			}
			for (var x = 0; x < correctValueIdxes.Count(); x++)
			{
				digitSelectables[correctValueIdxes.ElementAt(x)].OnInteract();
				yield return new WaitForSeconds(0.1f);
			}
			submitButton.OnInteract();
		}
    }

}
