using Newtonsoft.Json.Serialization;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SocialPlatforms;

public class SevenHandler : MonoBehaviour {

	public KMBombModule modSelf;
	public GameObject entireModule;
	public KMBombInfo info;
	public MeshRenderer[] segments, colorTriangles, colorTrianglesHL;
	public MeshRenderer LEDMesh;
	public KMSelectable[] segmentSelectables, colorTriangleSelectables;
	public KMSelectable LED, stageDisplay;
	private SevenSegmentCodings segmentCodings;
	public TextMesh stageIndc;
	public Material[] matSwitch = new Material[2];

	int[] finalValues = new int[3];

	List<int[]> displayedValues = new List<int[]>();
	List<int> idxOperations = new List<int>();

	readonly int[] segmentLogging = { 0, 5, 1, 6, 4, 2, 3 };

	int curSelectedColor = 0;
	int[] segmentsColored = new int[7], segmentsSolution = new int[7];


	static int modID;
	int curModID;

	// Detection and Logging
	bool zenDetected, timeDetected, hasStarted, isSubmitting;
	int curIdx = 0, curStrikeCount, localStrikes = 0;

	// Use this for initialization
	void Awake()
	{
		segmentCodings = ScriptableObject.CreateInstance<SevenSegmentCodings>();
		curModID = ++modID;

		for (int x = 0; x < segmentSelectables.Length; x++)
		{
			int y = x;
			segmentSelectables[x].OnInteract += delegate {
				if (isSubmitting)
				{
					segmentsColored[y] = curSelectedColor;
					UpdateSegments(false);
				}
				return false;
			};
		}

		for (int x = 0; x < colorTriangleSelectables.Length; x++)
		{
			int y = x;
			colorTriangleSelectables[x].OnInteract += delegate {
				if (isSubmitting)
				{
					curSelectedColor = y;
					for (int idx = 0; idx < colorTrianglesHL.Length; idx++)
					{
						colorTrianglesHL[idx].enabled = y == idx;
					}
				}
				return false;
			};
		}

		LED.OnInteract += delegate {
			if (hasStarted)
			{
				if (!isSubmitting)
				{
					curIdx++;
					if (curIdx >= displayedValues.Count)
						curIdx = 0;
					DisplayGivenValue(displayedValues[curIdx]);
				}
				else
				{
					curStrikeCount = info.GetStrikes();
					if (zenDetected || (timeDetected && localStrikes >= 2) || (!zenDetected && !timeDetected && curStrikeCount > 1))
					{
						isSubmitting = false;
						for (int x = 0; x < colorTriangles.Length; x++)
							colorTriangles[x].material.color = Color.black;
						DisplayGivenValue(displayedValues[curIdx]);
					}
				}
			}
			return false;
		};

		stageDisplay.OnInteract += delegate {
			if (hasStarted)
			{
				if (!isSubmitting)
				{
					isSubmitting = true;
					for (int x = 0; x < segments.Length; x++)
						segments[x].material.color = Color.black;
					LEDMesh.material.color = Color.black;
					stageIndc.text = "5UB";
					for (int x = 0; x < colorTriangles.Length; x++)
						colorTriangles[x].material.color = new Color(x % 2, x / 2 % 2, x / 4 % 2);
					colorTrianglesHL[0].enabled = true;
				}
			}
			return false;
		};
		for (int x = 0; x < segments.Length; x++)
			segments[x].material = matSwitch[0];
		for (int x = 0; x < colorTriangles.Length; x++)
			colorTriangles[x].material = matSwitch[0];
		LEDMesh.material = matSwitch[0];
		stageIndc.text = "";
	}
	void Start () {
		modSelf.OnActivate += delegate {
			for (int x = 0; x < segments.Length; x++)
				segments[x].material = matSwitch[1];
			for (int x = 0; x < colorTriangles.Length; x++)
				colorTriangles[x].material = matSwitch[0];
			LEDMesh.material = matSwitch[1];
			int modCount = info.GetSolvableModuleNames().Count;
			int stagesToGenerate = Mathf.Min(modCount, 7);
			Debug.LogFormat("[7 #{0}]: Modules detected: {1}", curModID, modCount);
			GenerateStages(stagesToGenerate);
			zenDetected = ZenModeActive;
			timeDetected = TimeModeActive;
			DisplayGivenValue(displayedValues[curIdx]);
			hasStarted = true;
		};
	}

	void GenerateStages(int extStageCount)
	{
		Debug.LogFormat("[7 #{0}]: Extra stages to generate: {1}", curModID, extStageCount);
		for (int x = 0; x < finalValues.Length; x++)
			finalValues[x] = Random.Range(-9, 10);
		Debug.LogFormat("[7 #{0}]: Values are logged in RGB format ( R, G, B )", curModID, finalValues.Join(", "));
		Debug.LogFormat("[7 #{0}]: Initial Values: ( {1} )", curModID, finalValues.Join(", "));
		displayedValues.Add(finalValues.ToArray()); // Add the initial stage.
		idxOperations.Add(-1);
		for (int x = 0; x < extStageCount; x++)
		{
			int[] modifedNumbers = { Random.Range(-9, 10), Random.Range(-9, 10), Random.Range(-9, 10) };
			int operationModifer = Random.Range(0, 3);
			Debug.LogFormat("[7 #{0}]: Stage {1}: LED: {3}, Values: ( {2} )", curModID, x + 1, modifedNumbers.Join(", "), new string[] { "R", "G", "B" }[operationModifer]);
			switch (operationModifer)
			{
				case 0:
					for (int p = 0; p < finalValues.Length; p++)
					{
						finalValues[p] += modifedNumbers[p];
					}
					goto default;
				case 1:
					for (int p = 0; p < finalValues.Length; p++)
					{
						finalValues[p] -= modifedNumbers[p];
					}
					goto default;
				case 2:
					for (int p = 0; p < finalValues.Length; p++)
					{
						finalValues[p] *= modifedNumbers[p];
					}
					goto default;
				default:
					for (int p = 0; p < finalValues.Length; p++)
					{
						finalValues[p] %= 10;
					}
					Debug.LogFormat("[7 #{0}]: Stage {1}: Result after modification: ( {2} )", curModID, x + 1, finalValues.Join(", "));
					break;
			}
			displayedValues.Add(modifedNumbers);
			idxOperations.Add(operationModifer);
		}
	}

	void CalculateSolution()
	{

	}

	void DisplayGivenValue(int[] curVal)
	{
		if (curVal.Length == 3)
		{
			for (int x = 0; x < segmentLogging.Length; x++)
			{
				int segIdx = segmentLogging[x];
				bool[] displayCnl = { false, false, false };
				
				for (int y = 0; y < displayCnl.Length; y++)
				{
					int grabbedValue = curVal[y];
					bool invert = curVal[y] < 0;
					string absVal = Mathf.Abs(curVal[y]).ToString();
					int valIdx = segmentCodings.possibleValues.IndexOf(absVal[0]);
					if (valIdx != -1)
					{
						displayCnl[y] = invert != segmentCodings.segmentStates[valIdx,segIdx];
					}
				}
				segments[x].material.color = new Color(displayCnl[0] ? 1: 0, displayCnl[1] ? 1 : 0, displayCnl[2] ? 1 : 0);
			}
			stageIndc.text = curIdx.ToString();
			Color[] cPallete = { Color.red, Color.green, Color.blue };

			LEDMesh.material.color = idxOperations[curIdx] < 0 || idxOperations[curIdx] >= 3 ? Color.black : cPallete[idxOperations[curIdx]] ;
		}
	}
	void UpdateSegments(bool canValidCheck)
	{
		for (int x = 0; x < segments.Length; x++)
		{
			if (canValidCheck)
				segments[x].material.color = segmentsColored[x] == segmentsSolution[x] ? Color.green : Color.red;
			else
				segments[x].material.color = new Color(segmentsColored[x] % 2, segmentsColored[x] / 2 % 2, segmentsColored[x] / 4 % 2);
		}
	}
	// Update is called once per frame
	void Update () {

	}

	bool TimeModeActive;
	bool ZenModeActive;

}
