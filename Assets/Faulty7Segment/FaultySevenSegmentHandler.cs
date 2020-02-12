using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FaultySevenSegmentHandler : MonoBehaviour {


	public GameObject[] segmentObjects;
	public SevenSegmentDisplay[] segmentDisplays;
	public KMNeedyModule needyModule;
	bool isActive = false;
	int value = 0, cooldown = 20;
	private List<Vector3> localPosSeg1 = new List<Vector3>();
	private int[] segmentIDs = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13 };
	private List<int> curSegmentPos;
	// Use this for initialization
	void Start () {
		needyModule.OnNeedyActivation += delegate
		{
			isActive = true;
			curSegmentPos = new List<int>();
			while (curSegmentPos.Count < 14)
			{
				int valueToAdd = segmentIDs[Random.Range(0, segmentIDs.Length)];
				if (!curSegmentPos.Contains(valueToAdd))
					curSegmentPos.Add(valueToAdd);
			}
		};
		needyModule.OnNeedyDeactivation += delegate
		{
			isActive = false;
		};
		needyModule.OnTimerExpired += delegate
		{
			isActive = false;
		};
		foreach (GameObject objCom in segmentObjects)
		{
			localPosSeg1.Add(objCom.transform.localPosition);
		}
	}
	void UpdateSegments()
	{

	}
	// Update is called once per frame
	void Update () {
		if (isActive)
		{
			if (cooldown > 0)
				cooldown--;
			else
			{
				value = (value + 1) % 100;
				cooldown = 20;
			}
			for (int x = 0; x < segmentDisplays.Length; x++)
			{
				segmentDisplays[x].SetCurrentValue(value.ToString("00")[x].ToString());
			}
		}
		else
		{
			value = 0;
			cooldown = 20;
			for (int x = 0; x < segmentDisplays.Length; x++)
			{
				segmentDisplays[x].SetCurrentValue("");
			}
		}
	}
}
