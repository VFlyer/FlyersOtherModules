using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SevenSegmentDisplay : MonoBehaviour {

	public MeshRenderer[] segments = new MeshRenderer[7];
	public Color[] segmentColors = new Color[2];
	private readonly string possibleValues = "0123456789abcdefhjlnopruy-";
	private readonly bool[,] segmentStates = new bool[,] {// Order for the segments: T, TR, BR, B, BL, TL, M; Respect possible values
		{ true, true, true, true, true, true, false },		// 0
		{ false, true, true, false, false, false, false },	// 1
		{ true, true, false, true, true, false, true },		// 2
		{ true, true, true, true, false, false, true },		// 3
		{ false, true, true, false, false, true, true },	// 4
		{ true, false, true, true, false, true, true },		// 5
		{ true, false, true, true, true, true, true },		// 6
		{ true, true, true, false, false, false, false },	// 7
		{ true, true, true, true, true, true, true },		// 8
		{ true, true, true, true, false, true, true },		// 9
		{ true, true, true, false, true, true, true },		// a, uppercase to distinguish
		{ false, false, true, true, true, true, true },		// b
		{ true, false, false, true, true, true, false },	// c
		{ false, true, true, true, true, false, true },		// d, lowercase to distinguish
		{ true, false, false, true, true, true, true },		// e
		{ true, false, false, false, true, true, true },	// f
		{ false, true, true, false, true, true, true },		// h
		{ false, true, true, true, true, false, false },	// j
		{ false, false, false, true, true, true, false },	// l
		{ false, false, true, false, true, false, true },	// n
		{ false, false, true, true, true, false, true },	// o, lowercase to distinguish
		{ true, true, false, false, true, true, true },     // p
		{ false, false, false, false, true, false, true },	// r, lowercase due to limited segments
		{ false, true, true, true, true, true, false },		// u
		{ false, true, true, true, false, true, true },		// y, lowercase due to limited segments
		{ false, false, false, false, false, false, true }	// -
	};
	public string currentValue;
	// Use this for initialization
	void Start () {
		currentValue = "";
	}
	public void SetCurrentValue(string value)
	{
		currentValue = value;
	}
	public string GetCurrentValue()
	{
		return currentValue;
	}
	// Update is called once per frame
	void Update()
	{
		var indexCurrentLetter = possibleValues.IndexOf(currentValue);
		if (indexCurrentLetter == -1 || currentValue.Length != 1)
		{
			for (int x = 0; x < segments.Length; x++)
				segments[x].material.color = segmentColors[0];
		}
		else
		{
			for (int x = 0; x < segments.Length; x++)
				segments[x].material.color = segmentStates[indexCurrentLetter,x] ? segmentColors[1] : segmentColors[0];
		}
	}
}
