using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Memoryception: MonoBehaviour {

	public KMBombModule modSelf;
	public MiniMemoryBase[] miniMemories;
	public TextMesh bigMemoryLabel;

	static int modIDCnt;
	int curModID;

	private int goalMemoryIdxLarge, curStageIdxLarge, curDisplayBig;

	private int[] labelsAll, curStageIdxMini, goalMemoryIdxMini, curDisplaysMini;

	private List<int> rememberedMiniMemoryInteractionPositions;
	private List<List<int>> rememberedMiniMemoryDisplays;
	int HandleLabelRuleIdxMini(int idx)
    {
		switch (idx)
        {

        }
		return -1;
    }

	// Use this for initialization
	void Start () {
		curModID = ++modIDCnt;
		labelsAll = new int[9];
		curStageIdxMini = new int[3];
		goalMemoryIdxMini = new int[3];

	}


	void QuickLog(string value, params object[] otherStuff)
    {
		Debug.LogFormat("[Memoryception #{0}]: {1}", curModID, string.Format(value, otherStuff));
    }
}
