using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Mazeception : MonoBehaviour {

	public KMBombModule modSelf;
	public KMAudio mAudio;
	public KMSelectable[] arrowSelectables;
	public GameObject[] goalMarkers, curPosMarkers;

	int[] goalPositions, currentPositions, mazeIdxAll;
	Dictionary<int, Vector3> idxPositions = new Dictionary<int, Vector3>()
	{
		{ 0, Vector3.left + Vector3.forward },
		{ 1, Vector3.forward },
		{ 2, Vector3.right + Vector3.forward },
		{ 3, Vector3.left },
		{ 4, Vector3.zero },
		{ 5, Vector3.right },
		{ 6, Vector3.left + Vector3.back },
		{ 7, Vector3.back },
		{ 8, Vector3.right + Vector3.back },
	};
	Dictionary<int, Dictionary<int, int[]>> mazeLayouts = new Dictionary<int, Dictionary<int, int[]>>()
	{
		{ -1, new Dictionary<int, int[]>()
			{
				// Formatted as { int nodeIdx , int[] {up, right, down, left} }
                { 0, new[] { 6, 1, 3, 2 } },
				{ 1, new[] { 7, 2, 4, 0 } },
				{ 2, new[] { 8, 0, 5, 1 } },
				{ 3, new[] { 0, 4, 6, 5 } },
				{ 4, new[] { 1, 5, 7, 3 } },
				{ 5, new[] { 2, 3, 8, 4 } },
				{ 6, new[] { 3, 7, 0, 8 } },
				{ 7, new[] { 4, 8, 1, 6 } },
				{ 8, new[] { 5, 6, 2, 7 } },
			}
		},
		{ 0, new Dictionary<int, int[]>()
			{
				// Formatted as { int nodeIdx , int[] {up, right, down, left} }
                { 0, new[] { -1, 1, 3, -1 } },
				{ 1, new[] { -1, 2, 4, 0 } },
				{ 2, new[] { -1, -1, 5, 1 } },
				{ 3, new[] { 0, 4, 6, -1 } },
				{ 4, new[] { 1, 5, 7, 3 } },
				{ 5, new[] { 2, -1, 8, 4 } },
				{ 6, new[] { 3, 7, -1, -1 } },
				{ 7, new[] { 4, 8, -1, 6 } },
				{ 8, new[] { 5, -1, -1, 7 } },
			}
		},
	};
	bool modSolved = false;
	// Use this for initialization
	void Start () {
		PrepMazes();
		for (var x = 0; x < arrowSelectables.Length; x++)
        {
			int y = x;
			arrowSelectables[x].OnInteract += delegate {
				HandleDirectionPress(y);
				return false;
			};
        }
	}
	void HandleDirectionPress(int dirIdx)
    {
		if (dirIdx < 0 || dirIdx >= 4 || modSolved) return;
		arrowSelectables[dirIdx].AddInteractionPunch();
		mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, arrowSelectables[dirIdx].transform);
		bool validMove = false;
		for (var x = 0; x < currentPositions.Length; x++)
        {
			var nextPos = -1;
			if (mazeLayouts.ContainsKey(mazeIdxAll[x]) && mazeLayouts[mazeIdxAll[x]].ContainsKey(currentPositions[x]))
            {
				var markedPos = mazeLayouts[mazeIdxAll[x]][currentPositions[x]];
				nextPos = dirIdx < 0 || dirIdx >= markedPos.Length ? -1 : markedPos[dirIdx];
			}

			if (nextPos != -1)
            {
				validMove = true;
				currentPositions[x] = nextPos;
            }				
        }
		if (!validMove)
        {
			modSelf.HandleStrike();
        }
		else if (currentPositions.SequenceEqual(goalPositions))
        {
			modSolved = true;
			modSelf.HandlePass();
        }
		UpdateMazes();
    }


	void PrepMazes()
    {
		goalPositions = new int[3];
		currentPositions = new int[3];
		mazeIdxAll = Enumerable.Repeat(0, 3).ToArray();
		var selectedRandomValue = Random.Range(0, 9);

		for (var x = 0; x < goalPositions.Length; x++)
        {
			goalPositions[x] = selectedRandomValue;
        }
		for (var x = 0; x < currentPositions.Length; x++)
        {
			currentPositions[x] = Random.Range(0, 9);
        }
		UpdateMazes();
	}
	void UpdateMazes()
    {
		for (var x = 0; x < curPosMarkers.Length; x++)
		{
			curPosMarkers[x].transform.localPosition = idxPositions.ContainsKey(currentPositions[x]) ? idxPositions[currentPositions[x]] : Vector3.zero;
		}
		for (var x = 0; x < goalMarkers.Length; x++)
		{
			goalMarkers[x].transform.localPosition = idxPositions.ContainsKey(goalPositions[x]) ? idxPositions[goalPositions[x]] : Vector3.zero;
			goalMarkers[x].SetActive(goalPositions[x] != currentPositions[x]);
		}
	}
	// Update is called once per frame
	void Update () {
        for (var x = 0; x < goalMarkers.Length; x++)
        {
			if (goalMarkers[x].activeInHierarchy)
				goalMarkers[x].transform.Rotate(Vector3.forward * Time.deltaTime * 90);
        }
	}
}
