using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FaultySevenSegmentHandler : MonoBehaviour {


	public GameObject[] segmentObjects;
	public SevenSegmentDisplay[] segmentDisplays;
	public KMSelectable[] segmentSelectables;
	public KMSelectable needySelfSelectable;
	public KMNeedyModule needyModule;
	public KMAudio audioSelf;
	bool isActive = false;
	private List<Vector3> localPosSeg = new List<Vector3>();
	private List<Vector3> localRotSeg = new List<Vector3>();
	private int[] segmentIDs = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13 }; // Used for read-only assignment, generally used for importing
	private List<int> curSegmentPos = new List<int>();

	private int activationCount = 1;
	private static int modID = 1;
	private int curModID;
	// Use this for initialization
	void Start () {
		needyModule.OnNeedyActivation += delegate
		{
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
				needyModule.HandleStrike();
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
	int value = 0, cooldown = 20, delayFlicker = 0, checkCooldown = 0;
	//readonly string[] faultyDisplayLetters = new string[] { "a", "b", "c", "d", "e", "f", "h", "j", "l", "n", "o", "p", "r", "u", "y", "-" }; // Unused atm.
	void Update () {
		if (isActive)
		{
			if (cooldown > 0)
				cooldown--;
			else
			{
				value = (value + 1) % 100;
				cooldown = 25;
			}
			for (int x = 0; x < segmentDisplays.Length; x++)
			{
				segmentDisplays[x].SetCurrentValue(value.ToString("00")[x].ToString());
			}
			if (delayFlicker <= 0)// Add the faulty section that can make this module easier to manage.
			{
				for (int x = 0; x < segmentDisplays.Length; x++)
				{
					segmentDisplays[x].SetColors(new Color[] { Color.black, Color.white });
				}
				checkCooldown = Mathf.Max(checkCooldown - 1, 0);
				if (checkCooldown <= 0)
				{
					if (Random.Range(0, 5) == 0)
					{
						int idxFlicker = Random.Range(0, segmentDisplays.Length);
						segmentDisplays[idxFlicker].SetColors(new Color[] { Color.black, faultyColorList[Random.Range(0,faultyColorList.Length)] });
						delayFlicker = 35;
					}
					checkCooldown = 20;
				}
			}
			else
			{
				delayFlicker = Mathf.Max(delayFlicker - 1, 0);
			}
		}
		else
		{
			value = 0;
			checkCooldown = 20;
			delayFlicker = 35;
			cooldown = 25;
			for (int x = 0; x < segmentDisplays.Length; x++)
			{
				segmentDisplays[x].SetCurrentValue("");
			}
		}
	}
}
