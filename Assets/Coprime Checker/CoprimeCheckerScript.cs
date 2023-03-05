using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class CoprimeCheckerScript : MonoBehaviour {

	public KMSelectable coprimeButton, notCoprimeButton;
	public KMBombModule modSelf;
	public KMAudio mAudio;
	public Material[] ledMats;
	public MeshRenderer[] ledRenderers;
	public TextMesh displayText;

	static int modIDCnt = 1;
	int curModID;

	int stagesCompleted = 0;
	bool expectedCoprime = false, modSolved = false;
	private int[] primeNumbers = { 2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37, 41, 43, 47, 53, 59, 61, 67, 71, 73, 79, 83, 89, 97, 101, 103, 107, 109, 113, 127, 131, 137, 139, 149, 151, 157, 163, 167, 173, 179, 181, 191, 193, 197, 199, 211, 223, 227, 229, 233, 239, 241, 251, 257, 263, 269, 271, 277, 281, 283, 293, 307, 311, 313, 317, 331, 337, 347, 349, 353, 359, 367, 373, 379, 383, 389, 397, 401, 409, 419, 421, 431, 433, 439, 443, 449, 457, 461, 463, 467, 479, 487, 491, 499, 503, 509, 521, 523, 541, 547, 557, 563, 569, 571, 577, 587, 593, 599, 601, 607, 613, 617, 619, 631, 641, 643, 647, 653, 659, 661, 673, 677, 683, 691, 701, 709, 719, 727, 733, 739, 743, 751, 757, 761, 769, 773, 787, 797, 809, 811, 821, 823, 827, 829, 839, 853, 857, 859, 863, 877, 881, 883, 887, 907, 911, 919, 929, 937, 941, 947, 953, 967, 971, 977, 983, 991, 997 };

	// Use this for initialization
	void Start () {
		curModID = modIDCnt++;
		coprimeButton.OnInteract += delegate { IsCorrect(true); return false; };
		notCoprimeButton.OnInteract += delegate { IsCorrect(false); return false; };
		GenerateStage();
	}
	void IsCorrect(bool buttonPressed)
    {
		if (buttonPressed)
        {
			coprimeButton.AddInteractionPunch();
			StartCoroutine(AnimatePressAnim(coprimeButton.transform, Vector3.down * 0.002f));
			mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, coprimeButton.transform);
        }
		else
        {
			notCoprimeButton.AddInteractionPunch();
			StartCoroutine(AnimatePressAnim(notCoprimeButton.transform, Vector3.down * 0.002f));
			mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, notCoprimeButton.transform);
		}

		if (modSolved) return;
		if (expectedCoprime ^ buttonPressed)
        {
            QuickLog("You pressed the wrong button! ({0} when expecting {1}) Generating a new answer.", buttonPressed ? "coprime" : "not coprime", expectedCoprime ? "coprime" : "not coprime");
			modSelf.HandleStrike();
			GenerateStage();
			StartCoroutine(FlashIncorrect());
        }
		else
        {
			stagesCompleted++;
			QuickLog("You pressed the right button. Correct presses made: {0}", stagesCompleted);
			for (var x = 0; x < Mathf.Min( ledRenderers.Length, stagesCompleted); x++)
			{
				ledRenderers[x].material = ledMats[2];
			}
			if (stagesCompleted >= 3)
            {
				QuickLog("You made enough correct presses. Module disarmed.");
				modSolved = true;
				modSelf.HandlePass();
				displayText.text = "";
            }
			else
            {
				GenerateStage();
			}
        }
    }

	IEnumerator FlashIncorrect()
    {
		for (var t = 0; t < 5; t++)
		{
			for (var x = stagesCompleted; x < ledRenderers.Length; x++)
			{
				ledRenderers[x].material = ledMats[1];
			}
			yield return new WaitForSeconds(0.1f);
			for (var x = stagesCompleted; x < ledRenderers.Length; x++)
			{
				ledRenderers[x].material = ledMats[0];
			}
			yield return new WaitForSeconds(0.1f);
		}
    }

	IEnumerator AnimatePressAnim(Transform curPos, Vector3 value, int frameCount = 5)
    {
		for (var t = 0; t < frameCount; t++)
		{
			curPos.Translate(value);
			yield return null;
		}
		for (var t = 0; t < frameCount; t++)
		{
			curPos.Translate(value * -1);
			yield return null;
		}
	}

	void QuickLog(string value, params object[] args)
	{
		Debug.LogFormat("[Coprime Checker #{0}] {1}", curModID, string.Format(value, args));
	}
	void GenerateStage()
    {
		int attemptsMade = 0, maxAttempts = 64;
		expectedCoprime = Random.value < 0.5f;
        bool isSuccessful = false;
        int givenNumA, givenNumB;
		do
		{
			givenNumA = Random.Range(2, 1000);
			do
				givenNumB = Random.Range(2, 1000);
			while (givenNumA == givenNumB);
			attemptsMade++;
			if (expectedCoprime == IsCoprime(givenNumA, givenNumB))
            {
				isSuccessful = true;
				break;
            }
		}
		while (attemptsMade < maxAttempts);
		if (isSuccessful)
			QuickLog("Generated a matching answer after {0} attempt(s)", attemptsMade);
		else
        {
			QuickLog("Unable to generate a matching answer after {0} attempt(s). Enforcing answer.", attemptsMade);
			expectedCoprime = IsCoprime(givenNumA, givenNumB);
		}
		displayText.text = givenNumA.ToString() + "\n" + givenNumB.ToString();
		QuickLog("The 2 numbers now shown are {0} and {1}", givenNumA, givenNumB);
		QuickLog("The greatest common multiple of {0} and {1} is {2}", givenNumA, givenNumB, ObtainGCM(givenNumA, givenNumB));
		QuickLog("These numbers are {0}coprime.", expectedCoprime ? "" : "not ");
	}
	// Old checking section to obtain prime factors of a certain number.
	/*
	List<int> ObtainPrimeFactors(int aNum)
    {
		var curNumA = aNum;
		List<int> primeFactors = new List<int>();
		while (curNumA > 1)
		{
			if (primeNumbers.Contains(curNumA))
			{
				primeFactors.Add(curNumA);
				break;
			}
			var foundPrime = false;
			var primeNumbersLessThanX = primeNumbers.Where(a => a < curNumA);
			foreach (int aPrime in primeNumbersLessThanX)
			{
				if (curNumA % aPrime == 0)
				{
					curNumA /= aPrime;
					primeFactors.Add(aPrime);
					foundPrime = true;
					break;
				}
			}
			if (!foundPrime)
			{
				primeFactors.Add(curNumA);
				break;
			}
		}
		return primeFactors;
	}
	*/
	int ObtainGCM(int numA, params int[] numsB)
    {
		var output = numA;

		for (var x = 0; x < numsB.Length; x++)
		{
			var pairValues = new List<int>() { output, numsB[x] };
			while (pairValues.Min() != 0)
            {
				if (pairValues[0] < pairValues[1])
					pairValues[1] %= pairValues[0];
				else
					pairValues[0] %= pairValues[1];
			}
			if (pairValues[0] == 0)
				output = pairValues[1];
			else if (pairValues[1] == 0)
				output = pairValues[0];
		}

		return output;
    }
	bool IsCoprime(int numA, params int[] numsB)
    {
		// Old checking section to determine coprime status.
		/*
		var distPrimeFactors = ObtainPrimeFactors(numA).Distinct();
        for (var x = 0; x < numsB.Length; x++)
        {
			var curDistPrimeFactors = ObtainPrimeFactors(numsB[x]).Distinct();
			distPrimeFactors = distPrimeFactors.Intersect(curDistPrimeFactors);
        }
		return !distPrimeFactors.Any();
		*/
		return ObtainGCM(numA, numsB) == 1;

    }
#pragma warning disable IDE0051 // Remove unused private members
    readonly string TwitchHelpMessage = "Press the button labeled \"Coprime\" with \"!{0} coprime\". Press the button labeled \"Not Coprime\" with \"!{0} notcoprime\".";
#pragma warning restore IDE0051 // Remove unused private members
	IEnumerator TwitchHandleForcedSolve()
	{
		while (stagesCompleted < 3)
		{
			(expectedCoprime ? coprimeButton : notCoprimeButton).OnInteract();
			yield return new WaitForSeconds(0.1f);
		}
	}
    IEnumerable<KMSelectable> ProcessTwitchCommand(string cmd)
    {
		if (cmd.EqualsIgnoreCase("coprime"))
		{
			return new[] { coprimeButton };
		}
		else if (cmd.EqualsIgnoreCase("notcoprime"))
		{
			return new[] { notCoprimeButton };
		}
		return null;
	}
}
