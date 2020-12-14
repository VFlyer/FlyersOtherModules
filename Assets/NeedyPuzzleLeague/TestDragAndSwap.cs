using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestDragAndSwap : MonoBehaviour {

	public KMSelectable[] allSelectables;
	public MeshRenderer[] baseObjects;
	public MeshRenderer selectionObject;
	public Color[] testColors;
	public KMBombModule modSelf;
	public KMAudio mAudio;
	int idxHovering = -1, idxStartHold = -1;
	bool isHolding = false;
	int[] idxArray = new int[0];

	// Use this for initialization
	void Start () {
		idxArray = new int[allSelectables.Length];
        for (int y = 0; y < idxArray.Length; y++)
        {
			idxArray[y] = y;
        }
		idxArray.Shuffle();
        for (int x = 0; x < allSelectables.Length; x++)
        {
			int y = x;
			allSelectables[x].OnInteract += delegate {
				if (idxHovering == -1)
					idxHovering = y;
				isHolding = true;
				idxStartHold = y;
				mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonRelease, allSelectables[y].transform);
				return false;
			};
			allSelectables[x].OnInteractEnded += delegate {
				if (idxHovering != -1)
				{
					SwapPair(idxHovering);
					idxHovering = -1;
					idxStartHold = -1;
					CheckOrder();
				}
				mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, allSelectables[y].transform);
				isHolding = false;
			};
			allSelectables[x].OnHighlight += delegate {
				if (idxHovering != -1 && isHolding)
					idxHovering = y;
			};
		}
	}
    void SwapPair(int idxToSwap = 0)
    {
        if (idxToSwap < 0 || idxStartHold < 0) return;
        var temp = idxArray[idxToSwap];
        idxArray[idxToSwap] = idxArray[idxStartHold];
        idxArray[idxStartHold] = temp;
    }
	void CheckOrder()
    {
		bool isAllCorrect = true;
		for (int y = 0; y < idxArray.Length; y++)
		{
			isAllCorrect &= idxArray[y] == y;
		}
		if (isAllCorrect)
			modSelf.HandlePass();
	}
	
	// Update is called once per frame
	void Update () {
        for (int x = 0; x < baseObjects.Length; x++)
        {
			baseObjects[x].material.color = testColors[idxArray[x]];
			baseObjects[x].enabled = idxStartHold != x;
        }
		if (idxStartHold != -1)
		{
			selectionObject.enabled = true;
			if (idxHovering != -1)
				selectionObject.gameObject.transform.localPosition = baseObjects[idxHovering].gameObject.transform.localPosition + Vector3.up * .017f;
			selectionObject.material.color = testColors[idxArray[idxStartHold]];
		}
		else
			selectionObject.enabled = false;
	}
}
