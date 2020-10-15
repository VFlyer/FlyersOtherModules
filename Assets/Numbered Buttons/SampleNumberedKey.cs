using UnityEngine;

public class SampleNumberedKey : MonoBehaviour {

	public TextMesh textMesh;
	public KMSelectable selfSelectable;
	public MeshRenderer ledRenderer;
	public Material[] ledMats;
	public bool pressed;

	float animVal = 1f;
	Vector3 startPos;
	// Use this for initialization
	void Start () {
		startPos = gameObject.transform.localPosition;
	}
	
	// Update is called once per frame
	void Update () {
		animVal = pressed ? Mathf.Max(0.5f, animVal - 4 * Time.deltaTime) : Mathf.Min(1 , animVal + 4 * Time.deltaTime);

		gameObject.transform.localPosition = new Vector3(startPos.x, startPos.y * animVal, startPos.z);
	}
}
