using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class NineMisalignedLightsCore : MonoBehaviour {

	public KMBombModule modSelf;
	public KMAudio mAudio;
	public GroupedTileRendererSet[] groupedTiles;
	public KMSelectable[] allSelectables;

	int[] lightStates;
	int curStageIdx = -1;
	bool isAnimating = true;
	Dictionary<int, List<int>> interactionModifiers = new Dictionary<int, List<int>>();
	// Use this for initialization
	void Start () {
		for (int x = 0; x < allSelectables.Length; x++)
        {
			int y = x;
			allSelectables[x].OnInteract += delegate {
				if (!isAnimating)
                switch (curStageIdx)
                {
					case 0:
					case 1:
						HandleInteractionStage1(y);
						goto default;
					default:
						mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonRelease, allSelectables[y].transform);
						mAudio.PlaySoundAtTransform("tick", allSelectables[y].transform);
						break;
                }
				return false;
			};
        }
		modSelf.OnActivate += delegate
		{
			GenerateStage1Puzzle();
		};
	}
	
	void GenerateStage1Puzzle()
    {
		lightStates = new int[9];
		for (var x = 0; x < 9; x++)
		{
			List<int> shuffledValues = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 }.Shuffle().Where(a => x != a).Take(Random.Range(0, 5)).ToList();
			interactionModifiers.Add(x, shuffledValues);
		}
		var buttonPressesSuggested = Random.Range(5, 50);
		do
			for (var x = 0; x < buttonPressesSuggested; x++)
			{
				HandleInteractionStage1(Random.Range(0, 9));
			}
		while (lightStates.All(a => a == 0));
		StartCoroutine(AnimateActivationAnim());
		curStageIdx = 0;
    }
	void GeneratePreviousStage1Puzzle()
	{
		lightStates = Enumerable.Repeat(3, 9).ToArray();
		var buttonPressesSuggested = Random.Range(5, 50);
		do
			for (var x = 0; x < buttonPressesSuggested; x++)
			{
				HandleInteractionStage1(Random.Range(0, 9));
			}
		while (lightStates.All(a => a == 3));
		curStageIdx = 1;
	}
	void UpdateIndividualLight(MeshRenderer curRenderer, Color newColor)
    {
		curRenderer.material.color = newColor;
    }

	void UpdateStage1Lights()
    {
		var rotationIDxes = new[] {
			new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 },
            new[] { 6, 3, 0, 7, 4, 1, 8, 5, 2 },
			new[] { 8, 7, 6, 5, 4, 3, 2, 1, 0 },
			new[] { 2, 5, 8, 1, 4, 7, 0, 3, 6 },
		};
        for (var x = 0; x < groupedTiles.Length; x++)
        {
			var curRenderers = groupedTiles[x].indicatorRenderers;
			var curRotSet = rotationIDxes.ElementAtOrDefault(lightStates[x]);
            for (int i = 0; i < curRotSet.Length; i++)
			{
                int value = curRotSet[i];
                curRenderers[i].material.color = value == 1 ?
					interactionModifiers[x].Contains(value) ? Color.green : Color.red :
					interactionModifiers[x].Contains(value) ? Color.white : Color.black;
			}
			groupedTiles[x].baseRenderer.material.color = x == 1 ?
				(lightStates[x] == (curStageIdx == 0 ? 0 : 3)) ? Color.green : Color.red :
				(lightStates[x] == (curStageIdx == 0 ? 0 : 3)) ? Color.white : Color.black;
        }
    }
	IEnumerator AnimateActivationAnim()
	{
		var allColorsToUpdate = new List<Color>();
		var allMeshRendersToUpdate = new List<MeshRenderer>();

		yield return null;
		var rotationIDxes = new[] {
			new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 },
			new[] { 6, 3, 0, 7, 4, 1, 8, 5, 2 },
			new[] { 8, 7, 6, 5, 4, 3, 2, 1, 0 },
			new[] { 2, 5, 8, 1, 4, 7, 0, 3, 6 },
		};
		for (var x = 0; x < groupedTiles.Length; x++)
		{
			var curRenderers = groupedTiles[x].indicatorRenderers;
			var curRotSet = rotationIDxes.ElementAtOrDefault(lightStates[x]);
			for (int i = 0; i < curRotSet.Length; i++)
			{
				int value = curRotSet[i];
				allMeshRendersToUpdate.Add(curRenderers[i]);
				allColorsToUpdate.Add(value == 1 ?
					interactionModifiers[x].Contains(value) ? Color.green : Color.red :
					interactionModifiers[x].Contains(value) ? Color.white : Color.black);
			}
			allMeshRendersToUpdate.Add(groupedTiles[x].baseRenderer);
			allColorsToUpdate.Add(x == 1 ?
				lightStates[x] == (curStageIdx == 0 ? 0 : 3) ? Color.green : Color.red :
				lightStates[x] == (curStageIdx == 0 ? 0 : 3) ? Color.white : Color.black);
		}
		var shuffledSet = Enumerable.Range(0, allMeshRendersToUpdate.Count).ToArray().Shuffle();
		yield return new WaitForSeconds(0.2f);
		for (var x = 0; x < shuffledSet.Length; x++)
		{
			yield return null;
			UpdateIndividualLight(allMeshRendersToUpdate[shuffledSet[x]], allColorsToUpdate[shuffledSet[x]]);
		}
		isAnimating = false;
	}
	IEnumerator AnimateCorrectAnim()
    {
		for (var y = 0; y < 6; y++)
		{
			yield return new WaitForSeconds(0.5f);
			for (var x = 0; x < groupedTiles.Length; x++)
			{
				var curColor = groupedTiles[x].baseRenderer.material.color;
				groupedTiles[x].baseRenderer.material.color =
					curColor == Color.green ? Color.red :
					curColor == Color.red ? Color.green :
					curColor == Color.white ? Color.black :
					curColor == Color.black ? Color.white :
					Color.white;
			}
		}
		GeneratePreviousStage1Puzzle();
		yield return AnimateActivationAnim();
		isAnimating = false;
	}
	void HandleInteractionStage1(int idx)
    {
		if (idx >= 0 && idx < lightStates.Length)
        {
			allSelectables[idx].AddInteractionPunch(0.1f);
			lightStates[idx] = (lightStates[idx] + (curStageIdx == 0 ? 1 : 3)) % 4;
			if (interactionModifiers.ContainsKey(idx))
            {
				var groupedToggles = interactionModifiers[idx];
				for (var x = 0; x < groupedToggles.Count; x++)
					lightStates[groupedToggles[x]] = (lightStates[groupedToggles[x]] + (curStageIdx == 0 ? 1 : 3)) % 4;
			}
		}
		if (!isAnimating)
		{
			UpdateStage1Lights();
		}
		switch (curStageIdx)
        {
			case 0:
				if (lightStates.All(a => a == 0))
				{
					isAnimating = true;
					StartCoroutine(AnimateCorrectAnim());
				}
				break;
			case 1:
				if (lightStates.All(a => a == 3))
				{
					isAnimating = true;
				}
				break;
		}
    }

	// Update is called once per frame
	void Update () {
		
	}
}
