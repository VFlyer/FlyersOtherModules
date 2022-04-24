using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class Memoryception: MonoBehaviour {

	public KMBombModule modSelf;
	public MiniMemoryBase[] miniMemories;
	public TextMesh bigMemoryLabel;

	static int modIDCnt;
	int curModID;

	private int goalMemoryIdxLarge, curStageIdxLarge, curDisplayBig;

	private int[] curStageIdxMini, goalMemoryIdxMini, curDisplaysMini;

	private List<int> rememberedMiniMemoryInteractionPositions;
	private List<List<int>> rememberedInitialMiniMemoryDisplays;
	private List<List<int[]>> rememberedMiniMemoryLabels;
	bool interactable = false;
	int GetCorrectIdxMini(int idxRule, int miniMemoryIdx, params int[] metadata)
    {
		switch (idxRule)
        {
			case 0: // Press the button on the X. 0 = left, 1 = middle, 2 = right
				return metadata[0];
			case 1: // Press the button that had a label of X.
				return Array.IndexOf(value: metadata[0], array: rememberedMiniMemoryLabels[miniMemoryIdx].Last());
			case 2: // Press the button that had a label of X on stage Y.
				return Array.IndexOf(value: metadata[0], array: rememberedMiniMemoryLabels[miniMemoryIdx][metadata[1]]);
		}
		return -1;
    }
	int GetCorrectIdxLarge(int idxRule, params int[] metadata)
    {
		switch (idxRule)
        {
			case 0: // Solve the Mini Memory on the X. 0 = left, 1 = middle, 2 = right
				return metadata[0];
			case 1: // Solve the Mini Memory that has the initial display of X.
				return rememberedInitialMiniMemoryDisplays.Last().IndexOf(metadata[0]);
			case 2: // Solve the Mini Memory that has the initial display of X on stage Y.
				return rememberedInitialMiniMemoryDisplays[metadata[1]].IndexOf(metadata[0]);
        }
		return -1;
    }

	// Use this for initialization
	void Start () {
		curModID = ++modIDCnt;
		curStageIdxMini = new int[3];
		goalMemoryIdxMini = new int[3];
		rememberedInitialMiniMemoryDisplays = new List<List<int>>();
		rememberedMiniMemoryInteractionPositions = new List<int>();
		rememberedMiniMemoryLabels = new List<List<int[]>>();

	}
	void RestartToBeginning()
    {

    }

	void QuickLog(string value, params object[] otherStuff)
    {
		Debug.LogFormat("[Memoryception #{0}]: {1}", curModID, string.Format(value, otherStuff));
    }
}
