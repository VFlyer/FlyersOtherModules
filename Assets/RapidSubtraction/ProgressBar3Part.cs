using System;
using UnityEngine;

public class ProgressBar3Part : MonoBehaviour {

	public MeshRenderer[] progress = new MeshRenderer[3];

	int curProgress = 0;
	int maxProgress = 3;

	// Use this for initialization
	void Start () {

	}

	public void Increment()
	{
		curProgress++;
		curProgress = Math.Min(curProgress, maxProgress);
	}
	public void Increment(int value)
	{
		curProgress += value;
		curProgress = Math.Min(curProgress, maxProgress);
	}
	public void ResetProgress()
	{
		curProgress = 0;
	}
	public int CurrentProgress()
	{
		return curProgress;
	}

	// Update is called once per frame
	void Update () {
		for (int x = 0; x < maxProgress; x++)
		{
			progress[x].enabled = curProgress >= x + 1;
		}
	}
}
