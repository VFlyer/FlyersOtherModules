using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class EncryptedMemoryScript : MonoBehaviour {

	public KMBombModule modself;
	public KMSelectable[] buttonsSelectable;
	public TextMesh[] displayMeshes;



	static int modIDCnt;
	int moduleID;

	List<string> encryptedRules;
	int[] digitDisplays, colorDisplayIdxes, colorBtnIdxes;

	void GenerateStage(int stageIdx = 0)
    {
		var digitDisplayShuffleOrder = Enumerable.Range(0, 3 + stageIdx).ToArray().Shuffle();

        for (var x = 0; x < digitDisplayShuffleOrder.Length; x++)
        {
			var startingString = x == 0 ? "?" : x + 1 >= digitDisplayShuffleOrder.Length ? "/" : "/?";
        }

	}

	// Use this for initialization
	void Start () {
		colorBtnIdxes = Enumerable.Range(0, 3).ToArray().Shuffle();
	}

}
