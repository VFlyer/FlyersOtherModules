using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SevenSegmentDisplay : MonoBehaviour {

	public MeshRenderer[] segments = new MeshRenderer[7];
	public Color[] segmentColors = new Color[2];
	private SevenSegmentCodings segmentHandling;

	public string currentValue;
	// Use this for initialization
	void Awake()
	{
		segmentHandling = new SevenSegmentCodings();
	}
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
	public void SetColors(Color[] newSegmentColors)
	{
		segmentColors = newSegmentColors;
	}
	// Update is called once per frame
	void Update()
	{
		var indexCurrentLetter = segmentHandling.possibleValues.IndexOf(currentValue);
		if (indexCurrentLetter == -1 || currentValue.Length != 1)
		{
			for (int x = 0; x < segments.Length; x++)
				segments[x].material.color = segmentColors[0];
		}
		else
		{
			for (int x = 0; x < segments.Length; x++)
				segments[x].material.color = segmentHandling.segmentStates[indexCurrentLetter,x] ? segmentColors[1] : segmentColors[0];
		}
	}
}
