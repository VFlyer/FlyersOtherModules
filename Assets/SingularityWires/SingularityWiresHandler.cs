using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SingularityWiresHandler : MonoBehaviour {
	sealed class SingularityWireInfo : MonoBehaviour
	{
		public List<SingularityWiresHandler> singularityWires = new List<SingularityWiresHandler>();
		private KMBombInfo curBombInfo;
		private IEnumerable currentlyRunning;
		public bool canDisarm = false;


		public void AssignBombInfo(KMBombInfo bombInfo)
		{
			if (curBombInfo != null) return;
			curBombInfo = bombInfo;
			bombInfo.OnBombExploded += delegate
			{
				StopAllCoroutines();
			};
		}
		public int getSingularityWireCount()
		{
			return singularityWires.Count;
		}
		public IEnumerator StartBootUpSequence()
		{
			yield return null;
		}

	}

	public KMSelectable[] wires = new KMSelectable[2];
	public KMSelectable disarmButton;
	public KMBombInfo bombInfo;
	public GameObject entireModule, animatedPortion;
	public KMColorblindMode colorblindMode;
	private bool hasDisarmed, colorblindDetected;

	private static readonly Dictionary<KMBomb, SingularityWireInfo> groupedSingularityWires = new Dictionary<KMBomb, SingularityWireInfo>();
	SingularityWireInfo wireInfo;

	// Use this for initialization
	void Start () {

		KMBomb bombAlone = entireModule.GetComponentInParent<KMBomb>(); // Get the bomb that the module is attached on. Required for intergration due to modified value.
		//Required for Multiple Bombs stable interaction in case of identical bomb seeds.

		if (!groupedSingularityWires.ContainsKey(bombAlone))
			groupedSingularityWires[bombAlone] = new SingularityWireInfo();
		wireInfo = groupedSingularityWires[bombAlone];
		wireInfo.singularityWires.Add(this);
		colorblindDetected = colorblindMode.ColorblindModeActive;
		StartCoroutine(HandleGlobalModule());

	}
	IEnumerator HandleGlobalModule()
	{
		StartCoroutine(wireInfo.StartBootUpSequence());
		int lastSolveCount = 0;
		while (!wireInfo.canDisarm)
		{
			var curSolveCount = bombInfo.GetSolvedModuleNames().Count;
			if (lastSolveCount != curSolveCount)
			{
				lastSolveCount = curSolveCount;
			}
			yield return new WaitForSeconds(0);
		}
	}
	// Update is called once per frame
	void Update () {

	}
}
