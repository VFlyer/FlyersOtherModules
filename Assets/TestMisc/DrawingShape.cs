using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DrawingShape : MonoBehaviour {

	public int idx = -1;
	public LineRenderer lineRenderer;
	public KMSelectable selfSelectable;
	public int[] sideCounts;
	// Use this for initialization
	void Start () {

		selfSelectable.OnInteract += delegate {
			idx = (idx + 1) % sideCounts.Length;
			UpdateRenderer(sideCounts.ElementAtOrDefault(idx));
			return false;
		};
	}

	void UpdateRenderer(int vertices = 0)
    {
		lineRenderer.positionCount = vertices;
		lineRenderer.loop = vertices > 2;
        lineRenderer.SetPositions(
			Enumerable.Range(0, vertices).Select(
				x => (Vector3.right * Mathf.Sin(Mathf.PI * 2 * x / lineRenderer.positionCount)) + (Vector3.up * Mathf.Cos(Mathf.PI * 2 * x / lineRenderer.positionCount))).ToArray());
		
	}

	// Update is called once per frame
	void Update () {
		if (lineRenderer != null)
			lineRenderer.transform.Rotate(Vector3.forward);
	}
}
