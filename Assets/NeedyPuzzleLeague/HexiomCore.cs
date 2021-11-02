using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using uernd = UnityEngine.Random;
/* 
 * That one flash game in the past? He brought it here.
 * Original credit goes to Matthew Stradwick for creating the flash version of this game.
 */
public class HexiomCore : MonoBehaviour {

	public KMSelectable selfSelectable, resetSelectable, replaySelectable, generateNewSelectable;
	public KMSelectable[] allSelectables;
	public Transform entireGrid;
	public Color[] indicatorColors;
	public KMBombModule modSelf;
	public KMAudio mAudio;
	public KMColorblindMode colorblindHandler;
	public KMBombInfo bombInfo;
	int idxHovering = -1, idxStartHold = -1;
	bool isHolding = false, interactable = false, colorblindDetected, canForceUpdateTiles = false, doNotLogBoard, focused;
	int[] idxArray = new int[0], lastIdxArray = new int[0];
	
	bool[] isLocked = new bool[0];
	bool[] isStandardTile = new bool[0];
	int[] correctAdjacentTiles = new int[0];

	public Mesh[] modifiedMeshes; 
	public HexiomTile[] activeTiles;
	public HexiomTile ancilleryTile;
	public MeshFilter[] insetObjects;
	Dictionary<int, int[]> adjacentNodeMappings;
	IEnumerator animatorHandler;

	static int modCounter = 1;
	int modId;

	void FillGrid()
    {
		adjacentNodeMappings = new Dictionary<int, int[]>
		{
			{ 0, new[] { 1, 5, 6 } },
			{ 1, new[] { 0, 2, 6, 7 } },
			{ 2, new[] { 1, 7, 3 } },
			{ 3, new[] { 2, 4, 7, 8 } },
			{ 4, new[] { 3, 8, 9 } },

			{ 5, new[] { 0, 6, 10, 11 } },
			{ 6, new[] { 0, 1, 5, 7, 11, 12 } },
			{ 7, new[] { 1, 2, 3, 6, 8, 12 } },
			{ 8, new[] { 3, 4, 7, 9, 12, 13 } },
			{ 9, new[] { 4, 8, 13, 14 } },

			{ 10, new[] { 5, 11, 15 } },
			{ 11, new[] { 5, 6, 10, 12, 15, 16 } },
			{ 12, new[] { 6, 7, 8, 11, 13, 16 } },
			{ 13, new[] { 8, 9, 12, 14, 16, 17 } },
			{ 14, new[] { 9, 13, 17 } },

			{ 15, new[] { 10, 11, 16, 18 } },
			{ 16, new[] { 11, 12, 13, 15, 17, 18 } },
			{ 17, new[] { 13, 14, 16, 18 } },

			{ 18, new[] { 15, 16, 17 } },
		};
    }
	void Awake()
    {
        try
        {
			colorblindDetected = colorblindHandler.ColorblindModeActive;
        }
		catch
        {
			colorblindDetected = false;
        }
    }

	// Use this for initialization
	void Start () {
		modId = modCounter++;
		GenerateBoard();
		// Assign all selectables onto this module.
        for (int x = 0; x < allSelectables.Length; x++)
        {
			int y = x;
			allSelectables[x].OnInteract += delegate {
				if (interactable)
				{
					if (!isHolding && isStandardTile[idxArray[y]] && !isLocked[idxArray[y]])
					{
						isHolding = true;
						idxStartHold = y;
						idxHovering = y;
						ancilleryTile.bodyRenderer.enabled = true;
						ancilleryTile.textDisplay.text = activeTiles[y].textDisplay.text;
						ancilleryTile.transform.localPosition = activeTiles[y].transform.localPosition + Vector3.up * .01f;
						activeTiles[y].textDisplay.text = "";
						activeTiles[y].bodyRenderer.enabled = false;
						UpdateAncilleryTile();
						mAudio.PlaySoundAtTransform("6_Click_Trimmed", transform);
						//mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonRelease, allSelectables[y].transform);
					}
				}
				return false;
			};
			allSelectables[x].OnInteractEnded += delegate {
				if (interactable)
				{
					if (idxHovering != -1)
					{
						if (!isLocked[idxArray[idxHovering]])
						{
							SwapPair(idxHovering);
							mAudio.PlaySoundAtTransform("8_Drop_Trimmed", transform);
							//mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, allSelectables[y].transform);
							activeTiles[idxHovering].bodyRenderer.material.color = ancilleryTile.bodyRenderer.material.color;
							
						}
						else
						{
							//mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.TitleMenuPressed, allSelectables[y].transform);
							mAudio.PlaySoundAtTransform("10_Babow_Trimmed", transform);
						}

						UpdateSelfAndAdjacentTiles(idxHovering);
						UpdateSelfAndAdjacentTiles(idxStartHold);

						idxHovering = -1;
						idxStartHold = -1;


					}
					if (animatorHandler != null)
						StopCoroutine(animatorHandler);
					ancilleryTile.bodyRenderer.enabled = false;
					ancilleryTile.textDisplay.text = "";
					CheckCorrect();
					allSelectables[y].AddInteractionPunch(0.25f);
					isHolding = false;
				}
			};
			allSelectables[x].OnHighlight += delegate {
				if (idxHovering != -1 && isHolding && interactable)
				{
					idxHovering = y;
					if (animatorHandler != null)
						StopCoroutine(animatorHandler);
					animatorHandler = AnimateTileMovement(idxHovering);
					StartCoroutine(animatorHandler);
					UpdateAncilleryTile();
				}
			};
			/*
			allSelectables[x].OnHighlightEnded += delegate {
				if (idxHovering == y && isHolding)
					idxHovering = -1;
			};*/
		}
		resetSelectable.OnInteract += delegate {
			if (interactable)
			{
				if (!idxArray.Select(a => correctAdjacentTiles[a]).SequenceEqual(lastIdxArray.Select(b => correctAdjacentTiles[b])) && !doNotLogBoard)
                {
					Debug.LogFormat("[Hexiom #{0}] Non initial board before reset:", modId);
					LogCurrentGrid();
                }
				idxArray = lastIdxArray.ToArray();
				mAudio.PlaySoundAtTransform("7_TooMany_Trimmed", transform);
				canForceUpdateTiles = true;
				UpdateGrid();
				canForceUpdateTiles = false;
			}
			resetSelectable.AddInteractionPunch(0.25f);
			//mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonRelease, resetSelectable.transform);
			mAudio.PlaySoundAtTransform("4_ButtonClick_Trimmed", transform);
			return false;
		};
		bombInfo.OnBombExploded += delegate {
			if (IsBoardSolved() || doNotLogBoard) return;
			Debug.LogFormat("[Hexiom #{0}] Unsolved board upon detonation:", modId);
			LogCurrentGrid();
		};
		generateNewSelectable.OnInteract += delegate {

			return false;
		};

		StartCoroutine(HandleRevealAnim());
	}
	bool IsBoardSolved()
	{
		bool output = true;
		for (int x = 0; x < activeTiles.Length; x++)
		{
			int idxCorrect = idxArray[x];
			if (isStandardTile[idxCorrect])
			{
				int curCountAdjacent = 0;
				int correctCountAdjacent = correctAdjacentTiles[idxCorrect];
				if (adjacentNodeMappings.ContainsKey(x))
				{
					foreach (int y in adjacentNodeMappings[x])
					{
						curCountAdjacent += isStandardTile[idxArray[y]] ? 1 : 0;
					}

				}
				if (curCountAdjacent != correctCountAdjacent)
					output = false;
			}
		}
		return output;
	}
	void GenerateBoard()
    {
		if (adjacentNodeMappings == null || !adjacentNodeMappings.Any())
			FillGrid();
		idxArray = new int[allSelectables.Length];
		isStandardTile = new bool[allSelectables.Length];
		isLocked = new bool[allSelectables.Length];
		correctAdjacentTiles = new int[allSelectables.Length];
		do
		{
			for (int y = 0; y < idxArray.Length; y++)
			{
				idxArray[y] = y;
				var valueFromRandomizer = uernd.value;
				if (valueFromRandomizer < 0.7f)
				{
					isStandardTile[y] = true;
					isLocked[y] = valueFromRandomizer < 0.1f;
				}
				else
				{
					isStandardTile[y] = false;
					isLocked[y] = valueFromRandomizer > 0.9f;
				}
			}
			for (int y = 0; y < correctAdjacentTiles.Length; y++)
			{
				int value = -1;
				if (isStandardTile[y])
				{
					value = 0;
					if (adjacentNodeMappings.ContainsKey(y))
						foreach (int z in adjacentNodeMappings[y])
							value += isStandardTile[z] ? 1 : 0;
				}
				correctAdjacentTiles[y] = value;
			}
			// Shuffle only the unlocked tiles.
			int[] modifiedShuffledObjects = idxArray.Where(a => !isLocked[a]).ToArray().Shuffle();
			var pointerModified = 0;
			for (int x = 0; x < idxArray.Length; x++)
			{
				if (!isLocked[x])
				{
					idxArray[x] = modifiedShuffledObjects[pointerModified];
					pointerModified++;
				}
			}
		}
		while (IsBoardSolved());
		lastIdxArray = idxArray.ToArray();
		if (!doNotLogBoard)
		{
			Debug.LogFormat("[Hexiom #{0}] One possible solution:", modId);
			LogPossibleSolution();
			Debug.LogFormat("[Hexiom #{0}] Initial state:", modId);
			LogCurrentGrid();
		}
		for (int x = 0; x < insetObjects.Length; x++)
		{
			if (!isStandardTile[x])
				insetObjects[x].mesh = modifiedMeshes[isLocked[x] ? 1 : 0];
			var renderer = insetObjects[x].gameObject.GetComponent<MeshRenderer>();
			if (renderer != null)
				if (!isLocked[x] || isStandardTile[x])
					renderer.material.color *= 0.7f;
		}
	}

	void LogCurrentGrid()
	{
		Debug.LogFormat("[Hexiom #{0}] ----------", modId);
		int[][] idxLoggings = {
			new[] { -1, -1, 2, -1, -1 },
			new[] { -1, 1, -1, 3, -1 },
			new[] { 0, -1, 7, -1, 4 },
			new[] { -1, 6, -1, 8, -1 },
			new[] { 5, -1, 12, -1, 9 },
			new[] { -1, 11, -1, 13, -1 },
			new[] { 10, -1, 16, -1, 14 },
			new[] { -1, 15, -1, 17, -1 },
			new[] { -1, -1, 18, -1, -1 },
		};
		for (int x = 0; x < idxLoggings.Length; x++)
        {
			Debug.LogFormat("[Hexiom #{0}] {1}", modId, idxLoggings[x].Select(a => (a >= 0 && correctAdjacentTiles[idxArray[a]] != -1 ? correctAdjacentTiles[idxArray[a]].ToString() : " ") + (a >= 0 && isLocked[idxArray[a]] ? "*" : " ")).Join(""));
		}
		Debug.LogFormat("[Hexiom #{0}] ----------", modId);
	}
	void LogPossibleSolution()
	{
		Debug.LogFormat("[Hexiom #{0}] ----------", modId);
		int[][] idxLoggings = {
			new[] { -1, -1, 2, -1, -1 },
			new[] { -1, 1, -1, 3, -1 },
			new[] { 0, -1, 7, -1, 4 },
			new[] { -1, 6, -1, 8, -1 },
			new[] { 5, -1, 12, -1, 9 },
			new[] { -1, 11, -1, 13, -1 },
			new[] { 10, -1, 16, -1, 14 },
			new[] { -1, 15, -1, 17, -1 },
			new[] { -1, -1, 18, -1, -1 },
		};
		for (int x = 0; x < idxLoggings.Length; x++)
		{
			Debug.LogFormat("[Hexiom #{0}] {1}", modId, idxLoggings[x].Select(a => (a >= 0 && correctAdjacentTiles[a] != -1 ? correctAdjacentTiles[a].ToString() : " ") + (a >= 0 && isLocked[a] ? "*" : " ")).Join(""));
		}
		Debug.LogFormat("[Hexiom #{0}] ----------", modId);
	}
	void UpdateAncilleryTile()
    {
		int idxCorrect = idxArray[idxStartHold];
		int curCountAdjacent = 0;
		int correctCountAdjacent = correctAdjacentTiles[idxCorrect];
		if (adjacentNodeMappings.ContainsKey(idxHovering))
		{
			var selectedAdjacentTiles = adjacentNodeMappings[idxHovering];
			foreach (int z in selectedAdjacentTiles.Where(a => idxHovering == idxStartHold || idxStartHold != a))
			{
				curCountAdjacent += isStandardTile[idxArray[z]] ? 1 : 0;
			}
			if (selectedAdjacentTiles.Contains(idxStartHold) && isStandardTile[idxArray[idxStartHold]] && isStandardTile[idxArray[idxHovering]])
				curCountAdjacent++;
		}
		if (!isLocked[idxHovering])
		{
			if (curCountAdjacent > correctCountAdjacent)
				ancilleryTile.bodyRenderer.material.color = indicatorColors[2];
			else if (curCountAdjacent < correctCountAdjacent)
				ancilleryTile.bodyRenderer.material.color = indicatorColors[0];
			else
				ancilleryTile.bodyRenderer.material.color = indicatorColors[1];
		}
		else
			ancilleryTile.bodyRenderer.material.color = Color.blue;
	}

    void SwapPair(int idxToSwap = 0)
    {
        if (idxToSwap < 0 || idxStartHold < 0) return;
        var temp = idxArray[idxToSwap];
        idxArray[idxToSwap] = idxArray[idxStartHold];
        idxArray[idxStartHold] = temp;
    }
	static int soundCount = 0;
	IEnumerator HandleStartSoundPlay()
    {
		if (soundCount < 1)
        {
			mAudio.PlaySoundAtTransform("2_Grow_Trimmed", transform);
			soundCount++;
		}
		yield return null;
		soundCount = 0;
    }
	IEnumerator HandleRevealAnim()
    {
		yield return HandleStartSoundPlay();
		UpdateGrid();
		for (float x = 0; x < 1f; x += Time.deltaTime * 5)
		{
			yield return null;
			entireGrid.localScale = Vector3.one * x;
		}
		entireGrid.localScale = Vector3.one;
		interactable = true;
	}
	IEnumerator HandleDisappearAnim()
	{
		canForceUpdateTiles = true;
		UpdateGrid();
		selfSelectable.Children = new KMSelectable[] { resetSelectable };
		selfSelectable.UpdateChildren();
		yield return new WaitForSeconds(1f);
		mAudio.PlaySoundAtTransform("1_Shrink_Trimmed", transform);
		for (float x = 0; x < 1f; x += Time.deltaTime * 5)
		{
			yield return null;
			entireGrid.localScale = Vector3.one * (1f - x);
		}
		entireGrid.localScale = Vector3.zero;
		entireGrid.gameObject.SetActive(false);
		yield break;
	}
	IEnumerator AnimateTileMovement(int idx)
    {
		if (idx < 0 || idx >= activeTiles.Length) yield break;
		Vector3 lastPos = ancilleryTile.transform.localPosition,
			targetPos = activeTiles[idx].transform.localPosition;
        for (float x = 0; x < 1f; x += Time.deltaTime * 10)
        {
			ancilleryTile.transform.localPosition = lastPos * (1 - x) + targetPos * x + Vector3.up * .01f;
			yield return null;
		}
		ancilleryTile.transform.localPosition = targetPos + Vector3.up * .01f;
	}
	void UpdateSelfAndAdjacentTiles(int idx)
	{
		List<int> allTilesToCheck = new List<int>() { idx };
		if (adjacentNodeMappings.ContainsKey(idx))
			allTilesToCheck.AddRange(adjacentNodeMappings[idx]);


		foreach (int x in allTilesToCheck)
		{
			int idxCorrect = idxArray[x];
			if (isStandardTile[idxCorrect])
			{
				activeTiles[x].bodyRenderer.enabled = true;
				int curCountAdjacent = 0;
				int correctCountAdjacent = correctAdjacentTiles[idxCorrect];
				if (adjacentNodeMappings.ContainsKey(x))
				{
					foreach (int y in adjacentNodeMappings[x])
					{
						curCountAdjacent += isStandardTile[idxArray[y]] ? 1 : 0;
					}

				}
				if (curCountAdjacent > correctCountAdjacent)
					activeTiles[x].SoftChangeColor(indicatorColors[2]);
				else if (curCountAdjacent < correctCountAdjacent)
					activeTiles[x].SoftChangeColor(indicatorColors[0]);
				else
					activeTiles[x].SoftChangeColor(indicatorColors[1]);
				activeTiles[x].textDisplay.text = correctCountAdjacent.ToString() + (colorblindDetected ? curCountAdjacent > correctCountAdjacent ? ">" : curCountAdjacent < correctCountAdjacent ? "<" : "=" : "");
			}
			else
			{
				activeTiles[x].bodyRenderer.enabled = false;
				activeTiles[x].textDisplay.text = "";
				/*
				if (isLocked[idxCorrect])
				{

				}
				*/
			}
		}
	}
	void UpdateGrid()
    {
		for (int x = 0; x < activeTiles.Length; x++)
        {
            int idxCorrect = idxArray[x];
            if (isStandardTile[idxCorrect])
            {
                activeTiles[x].bodyRenderer.enabled = true;
                int curCountAdjacent = 0;
				int correctCountAdjacent = correctAdjacentTiles[idxCorrect];
				if (adjacentNodeMappings.ContainsKey(x))
                {
                    foreach (int y in adjacentNodeMappings[x])
                    {
                        curCountAdjacent += isStandardTile[idxArray[y]] ? 1 : 0;
                    }

                }
				if (canForceUpdateTiles)
					if (curCountAdjacent > correctCountAdjacent)
						activeTiles[x].ChangeColor(indicatorColors[2]);
					else if (curCountAdjacent < correctCountAdjacent)
						activeTiles[x].ChangeColor(indicatorColors[0]);
					else
						activeTiles[x].ChangeColor(indicatorColors[1]);
				else
					if (curCountAdjacent > correctCountAdjacent)
					activeTiles[x].SoftChangeColor(indicatorColors[2]);
				else if (curCountAdjacent < correctCountAdjacent)
					activeTiles[x].SoftChangeColor(indicatorColors[0]);
				else
					activeTiles[x].SoftChangeColor(indicatorColors[1]);
				activeTiles[x].textDisplay.text = correctCountAdjacent.ToString() + (colorblindDetected ? curCountAdjacent > correctCountAdjacent ? ">" : curCountAdjacent < correctCountAdjacent ? "<" : "=" : "");
				activeTiles[x].lockedRenderer.enabled = isLocked[idxCorrect];
			}
            else
            {
                activeTiles[x].bodyRenderer.enabled = false;
                activeTiles[x].textDisplay.text = "";
				activeTiles[x].lockedRenderer.enabled = false;
			}
        }
    }
	void CheckCorrect()
    {
		if (IsBoardSolved())
		{
			Debug.LogFormat("[Hexiom #{0}] Module disarmed with the following board:", modId);
			LogCurrentGrid();
			//mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
			mAudio.PlaySoundAtTransform("7_TooMany_Trimmed", transform);
			modSelf.HandlePass();
			StartCoroutine(HandleDisappearAnim());
			interactable = false;
		}
	}

	IEnumerator TwitchHandleForcedSolve()
    {
		yield return null;
		for (int x = 0; !IsBoardSolved(); x = (x + 1) % idxArray.Length)
        {
			if (isLocked[x] || !isStandardTile[idxArray[x]] || idxArray[x] == x) continue;
			allSelectables[x].OnInteract();
			yield return null;
			allSelectables[idxArray[x]].OnHighlight();
			yield return new WaitForSeconds(0.1f);
			allSelectables[x].OnInteractEnded();
			yield return null;
		}
    }

#pragma warning disable IDE0051 // Remove unused private members
    private readonly string TwitchHelpMessage = "To swap the given pairs, I.E A1 with B1, C1 with D1: \"!{0} swap A1 B1;C1 D1\" Columns are labeled A-E from left to right on the hexagonal grid; rows are labeled 1-5 from the top-most hexagon in that column. Use \";\" to chain swaps. Commands may be voided if the numbered tiles are attempted to be placed in an invalid position, the command detects a pair of empty tiles, or when the module is solved.\n Toggle colorblind mode with \"!{0} colorblind\"; reset the board with \"!{0} reset\"; focably update tiles with \"!{0} update\"";
#pragma warning restore IDE0051 // Remove unused private members
	IEnumerator ProcessTwitchCommand(string cmd)
	{
		if (!interactable)
        {
			yield return "sendtochaterror This module is refusing inputs at the moment.";
			yield break;
		}
		if (Regex.IsMatch(cmd, @"^colou?rblind$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
			yield return null;
			colorblindDetected = !colorblindDetected;
			UpdateGrid();
			yield break;
        }
		else if (Regex.IsMatch(cmd, @"^(update|refresh)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
		{
			yield return null;
			canForceUpdateTiles = true;
			UpdateGrid();
			canForceUpdateTiles = false;
			yield break;
		}
		else if (Regex.IsMatch(cmd, @"^reset$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
		{
			yield return null;
			resetSelectable.OnInteract();
			yield break;
		}
		if (Regex.IsMatch(cmd, @"^swap\s", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
			cmd = cmd.Substring(4).Trim();
		var coordinatePairs = cmd.Split(';').Select(a => a.Trim()).ToArray();

		List<KMSelectable> startPairs = new List<KMSelectable>();
		List<KMSelectable> swappingPairs = new List<KMSelectable>();
		Dictionary<char, KMSelectable[]> coordinateValues = new Dictionary<char, KMSelectable[]>()
		{
			{ 'A', new KMSelectable[] { allSelectables.ElementAtOrDefault(0), allSelectables.ElementAtOrDefault(5), allSelectables.ElementAtOrDefault(10) } },
			{ 'B', new KMSelectable[] { allSelectables.ElementAtOrDefault(1), allSelectables.ElementAtOrDefault(6), allSelectables.ElementAtOrDefault(11), allSelectables.ElementAtOrDefault(15) } },
			{ 'C', new KMSelectable[] { allSelectables.ElementAtOrDefault(2), allSelectables.ElementAtOrDefault(7), allSelectables.ElementAtOrDefault(12), allSelectables.ElementAtOrDefault(16), allSelectables.ElementAtOrDefault(18) } },
			{ 'D', new KMSelectable[] { allSelectables.ElementAtOrDefault(3), allSelectables.ElementAtOrDefault(8), allSelectables.ElementAtOrDefault(13), allSelectables.ElementAtOrDefault(17) } },
			{ 'E', new KMSelectable[] { allSelectables.ElementAtOrDefault(4), allSelectables.ElementAtOrDefault(9), allSelectables.ElementAtOrDefault(14) } },
		};
		foreach (string aPair in coordinatePairs)
		{
			var coordinates = aPair.Split();
			if (coordinates.Length != 2)
			{
				yield return string.Format("sendtochaterror \"{0}\" does not have exactly a pair of coordinates that may or may not be coordinates.", aPair);
				yield break;
			}
			for (int x = 0; x < coordinates.Length; x++)
			{
				var valueChecked = Regex.Match(coordinates[x], @"^[ABCDE][12345]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
				if (!valueChecked.Success)
				{
					yield return string.Format("sendtochaterror \"{0}\" is not a valid coordinate for this module.", coordinates[x]);
					yield break;
				}
				var value = valueChecked.Value.ToUpperInvariant();
				if (coordinateValues.ContainsKey(value[0]))
				{
					var selectedItem = coordinateValues[value[0]].ElementAtOrDefault(value[1] - '1');
					if (selectedItem == null)
					{
						yield return string.Format("sendtochaterror \"{0}\" is not a valid coordinate for this module.", value);
						yield break;
					}
					switch (x)
					{
						case 0:
							startPairs.Add(selectedItem);
							break;
						default:
							swappingPairs.Add(selectedItem);
							break;
					}
				}
				else
                {
					yield return string.Format("sendtochaterror \"{0}\" is not a valid coordinate for this module.", value);
					yield break;
				}
            }

        }
		for (int x = 0; x < Mathf.Min(startPairs.Count, swappingPairs.Count) && interactable; x++)
		{
			int valueA = System.Array.IndexOf(allSelectables, startPairs[x]);
			int valueB = System.Array.IndexOf(allSelectables, swappingPairs[x]);
			
			if (isStandardTile[idxArray[valueA]])
			{
				yield return null;
				yield return startPairs[x];
				yield return new WaitForSeconds(0.1f);
				swappingPairs[x].OnHighlight();
				yield return new WaitForSeconds(0.1f);
				yield return startPairs[x];
				yield return new WaitForSeconds(0.1f);
			}
			else if (isStandardTile[idxArray[valueB]])
			{
				yield return null;
				yield return swappingPairs[x];
				yield return new WaitForSeconds(0.1f);
				startPairs[x].OnHighlight();
				yield return new WaitForSeconds(0.1f);
				yield return swappingPairs[x];
				yield return new WaitForSeconds(0.1f);
			}
			else
            {
				yield return "sendtochat {0}, your swap has been canceled after " + (x + 1) + " swaps due to a pair of tiles being empty, namely \"" + coordinatePairs[x].Select(a => char.IsWhiteSpace(a) ? ',' : a).Join("") + "\"";
				yield break;
			}
			if (isLocked[valueA] || isLocked[valueB])
			{
				yield return "sendtochat {0}, your swap has been canceled after " + (x + 1) + " swaps due to a tile within that pair being locked, namely \"" + coordinatePairs[x].Select(a => char.IsWhiteSpace(a) ? ',' : a).Join("") + "\"";
				yield break;
			}
		}

		yield break;
    }

}