using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;
using KModkit;
public class FaultySevenSegmentHandler : MonoBehaviour {


	public GameObject[] segmentObjects;
	public SevenSegmentDisplay[] segmentDisplays;
	public KMSelectable[] segmentSelectables;
	public KMSelectable needySelfSelectable;
	public KMNeedyModule needyModule;
	public KMAudio audioSelf;
	bool isActive = false, TPDetected, forceDisable = false;
	private List<Vector3> localPosSeg = new List<Vector3>();
	private List<Vector3> localRotSeg = new List<Vector3>();
	private int[] segmentIDs = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13 }; // Used for read-only assignment, generally used for importing
	private List<int> curSegmentPos = new List<int>();

	private int activationCount = 1;
	private static int modID = 1;
	private int curModID;
	// Use this for initialization
	void Start() {
		needyModule.OnNeedyActivation += delegate
		{
			if (forceDisable)
			{
				needyModule.HandlePass();
				return;
			}
			isActive = true;
			List<int> tempSegmentsID = segmentIDs.ToList();
			curSegmentPos.Clear();
			while (tempSegmentsID.Count > 0)
			{
				int valueToAdd = tempSegmentsID[Random.Range(0, tempSegmentsID.Count)];
				curSegmentPos.Add(valueToAdd);
				tempSegmentsID.Remove(valueToAdd);
			}
			UpdateSegments();
			Debug.LogFormat("[Faulty Seven Segment Display #{0}]: The set of the seven segments scrambled for {1} needy activation(s) are:", curModID, activationCount);
			LogSegments(curSegmentPos.ToArray());
			TPDetected = TwitchPlaysActive;
			if (TPDetected)
			{
				needyModule.SetNeedyTimeRemaining(needyModule.GetNeedyTimeRemaining() * 2);
			}
		};
		needyModule.OnNeedyDeactivation += delegate
		{
			isActive = false;
		};
		needyModule.OnTimerExpired += delegate
		{
			isActive = false;
			Debug.LogFormat("[Faulty Seven Segment Display #{0}]: The current set of the seven segments when the time ran out for {1} needy activation(s):", curModID, activationCount++);
			LogSegments(curSegmentPos.ToArray());
			if (!curSegmentPos.SequenceEqual(segmentIDs.ToList()))
			{
				needyModule.HandleStrike();
			}
			else
            {
				needyModule.SetResetDelayTime(60f, 160f);
            }
		};
		foreach (GameObject objCom in segmentObjects)
		{
			localPosSeg.Add(objCom.transform.localPosition);
			localRotSeg.Add(objCom.transform.localEulerAngles);
		}
		for (int x = 0; x < segmentSelectables.Length; x++)
		{
			int temp = x;
			segmentSelectables[x].OnInteract += delegate
			{
				segmentSelectables[temp].AddInteractionPunch();
				audioSelf.PlaySoundAtTransform("tick", transform);
				if (curSegmentPos.Count > 0)
				{
					bool isSelected = false;
					int idxSelected = -1;
					for (int u = 0; u < segmentSelectables.Length; u++)
					{
						var selectable = segmentSelectables[u];
						if (selectable.Highlight.GetComponent<MeshRenderer>().enabled)
						{
							isSelected = true;
							idxSelected = u;
						}
					}
					if (!isSelected)
						segmentSelectables[temp].Highlight.GetComponent<MeshRenderer>().enabled = true;
					else
					{
						SwapSegments(temp, idxSelected);
						foreach (KMSelectable selectable in segmentSelectables)
						{
							selectable.Highlight.GetComponent<MeshRenderer>().enabled = false;
						}
					}
				}
				return false;
			};
		}

		curModID = modID++;
		Debug.LogFormat("[Faulty Seven Segment Display #{0}]: The correct set of the seven segments are logged as the following:", curModID);
		LogSegments();
	}
	void SwapSegments(int a, int b)
	{
		if (a < 0 || a >= segmentIDs.Length || b < 0 || b >= segmentIDs.Length) return;
		var temp = curSegmentPos[a];
		curSegmentPos[a] = curSegmentPos[b];
		curSegmentPos[b] = temp;
		UpdateSegments();
	}

	readonly int?[,] loggingOrderIdx = new int?[,] {// For logging the given position of the seven segments.
		{ null, 0, null, null, 7, null },
		{ 5, null, 1, 12, null, 8 },
		{ null, 6, null, null, 13, null },
		{ 4, null, 2, 11, null, 9 },
		{ null, 3, null, null, 10, null },
	};

	void LogSegments(int[] logSegmentIDs)
	{
		if (logSegmentIDs.Length == 14)
		{

			for (int x = 0; x < loggingOrderIdx.GetLength(0); x++)
			{
				string log1LineOutput = "";
				for (int y = 0; y < loggingOrderIdx.GetLength(1); y++)
				{
					if (y == loggingOrderIdx.GetLength(1) / 2) log1LineOutput += " ";
					if (loggingOrderIdx[x, y] == null)
						log1LineOutput += "  ";
					else
						log1LineOutput += logSegmentIDs[(int)loggingOrderIdx[x, y]].ToString("00");
				}
				Debug.LogFormat(log1LineOutput);
			}
		}
	}
	void LogSegments()
	{
		for (int x = 0; x < loggingOrderIdx.GetLength(0); x++)
		{
			string log1LineOutput = "";
			for (int y = 0; y < loggingOrderIdx.GetLength(1); y++)
			{
				if (y == loggingOrderIdx.GetLength(1) / 2) log1LineOutput += " ";
				if (loggingOrderIdx[x, y] == null)
					log1LineOutput += "  ";
				else
					log1LineOutput += ((int)loggingOrderIdx[x, y]).ToString("00");
			}
			Debug.LogFormat(log1LineOutput);
		}
	}
	void UpdateSegments()
	{
		for (int x = 0; x < segmentObjects.Length; x++)
		{
			segmentObjects[x].transform.localPosition = localPosSeg[curSegmentPos[x]];
			segmentObjects[x].transform.localEulerAngles = localRotSeg[curSegmentPos[x]];
		}
	}
	// Update is called once per frame
	readonly Color[] faultyColorList = new Color[] { Color.magenta, Color.red, Color.blue, Color.grey, Color.yellow, Color.cyan, Color.green };
	int value = 0;
	float cooldownTime = 0.33f, timeFlicker = 0.55f, checkCooldown = 0.2f;
	//readonly string[] faultyDisplayLetters = new string[] { "a", "b", "c", "d", "e", "f", "h", "j", "l", "n", "o", "p", "r", "u", "y", "-" }; // Unused atm.
	void Update() {
		if (isActive)
		{
			if (cooldownTime > 0)
                cooldownTime -= Time.deltaTime;
			else
			{
				value = (value + 1) % 100;
				cooldownTime = .33f;
			}
			for (int x = 0; x < segmentDisplays.Length; x++)
			{
				segmentDisplays[x].SetCurrentValue(value.ToString("00")[x].ToString());
			}
			if (timeFlicker <= 0)// Add the faulty section that can make this module easier to manage.
			{
				for (int x = 0; x < segmentDisplays.Length; x++)
				{
					segmentDisplays[x].SetColors(new Color[] { Color.black, Color.white });
				}
				checkCooldown -= Time.deltaTime;
				if (checkCooldown <= 0)
				{
					if (Random.Range(0, 5) == 0)
					{
						int idxFlicker = Random.Range(0, segmentDisplays.Length);
						segmentDisplays[idxFlicker].SetColors(new Color[] { Color.black, faultyColorList[Random.Range(0, faultyColorList.Length)] });
						timeFlicker = 0.55f;
					}
					checkCooldown = .2f;
				}
			}
			else
			{
				timeFlicker -= Time.deltaTime;
			}
		}
		else
		{
			value = 0;
			checkCooldown = .2f;
			timeFlicker = 0.55f;
			cooldownTime = .33f;
			for (int x = 0; x < segmentDisplays.Length; x++)
			{
				segmentDisplays[x].SetCurrentValue("");
			}
		}
	}

	void TwitchHandleForcedSolve()
	{
		Debug.LogFormat("[Faulty Seven Segment Display #{0}]: Forcably disabling the needy viva TP handler.", curModID);
		needyModule.SetResetDelayTime(float.MaxValue, float.MaxValue);
		needyModule.HandlePass();
		forceDisable = true;
		isActive = false;
	}

	bool TwitchPlaysActive;
	string TwitchHelpMessage = "Swap the following segments with \"!{0} swap a# b#\". Multiple pairs of segments can be swapped viva \";\" I.E \"!{0} swap a1 b2; a3 b4;...\".\nSegments are labeled 1-7 in reading order, \"a\" being the left 7 segment display, \"b\" being the right 7 segment display.";
	IEnumerator ProcessTwitchCommand(string command)
	{
		string commandLower = command.ToLower();
		if (commandLower.StartsWith("swap "))
			commandLower = commandLower.Substring(5);
		string[] selectedParts = commandLower.Split(';');
		int[] TPSegmentIDsL = { 0, 5, 1, 6, 4, 2, 3 };
		int[] TPSegmentIDsR = { 7, 12, 8, 13, 11, 9, 10 };
		List<KMSelectable> selectablesTPCMD = new List<KMSelectable>();
		if (curSegmentPos.Count != 14)
		{
			yield return "sendtochaterror The needy has not been activated for the first time yet. Wait for a bit until the needy is active.";
			yield break;
		}
		foreach (string part in selectedParts)
		{
			string intereptedPart = part.Trim();
			if (intereptedPart.RegexMatch(@"^[ab]\d\s[ab]\d$"))
			{
				string[] intereptedSections = intereptedPart.Split(' ');
				foreach (string section in intereptedSections)
				{
					string intereptedLetter = section.Substring(0, 1);
					string intereptedNum = section.Substring(1);
					int givenNum;
					if (int.TryParse(intereptedNum, out givenNum) && givenNum > 0 && givenNum <= 7)
					{
						if (intereptedLetter.Equals("a"))
							selectablesTPCMD.Add(segmentSelectables[curSegmentPos.IndexOf(TPSegmentIDsL[givenNum - 1])]);
						else if (intereptedLetter.Equals("b"))
							selectablesTPCMD.Add(segmentSelectables[curSegmentPos.IndexOf(TPSegmentIDsR[givenNum - 1])]);
						else
						{
							yield return "sendtochaterror Did you try to get this message?";
							yield break;
						}
					}
					else
					{
						yield return "sendtochaterror Sorry but segment \"" + intereptedNum + "\" for the given segment is not accessible.";
						yield break;
					}
				}
			}
			else
			{
				yield return "sendtochaterror Sorry but what command is \"" + part + "\" supposed to be?";
				yield break;
			}
		}
		if (selectablesTPCMD.Count > 0)
		{
			foreach (KMSelectable selectable in selectablesTPCMD)
			{
				yield return null;
				selectable.OnInteract();
				yield return new WaitForSeconds(0.2f);
			}
		}
		yield break;
	}

}
