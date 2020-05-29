using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class SevenSegmentCodings {
	public string possibleValues;
	public bool[,] segmentStates;

	public SevenSegmentCodings()
	{
		possibleValues = "0123456789abcdefhjlnopruy-"; // The list of possible values that can be made in the coding strand.
		segmentStates = new bool[,] {// Order for the segments: T, TR, BR, B, BL, TL, M; Respect possible values
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
	}
	public SevenSegmentCodings(string readValues,bool[,] segmentRenders)
	{
		possibleValues = readValues;
		segmentStates = segmentRenders;
	}
	
	void Awake()
	{

	}
	
}
