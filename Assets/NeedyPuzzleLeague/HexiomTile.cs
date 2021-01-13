using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexiomTile : MonoBehaviour {

	public MeshRenderer bodyRenderer, lockedRenderer;
	public TextMesh textDisplay;
	public float speed = 4f;
	IEnumerator animatorHandler;
	Color lastColor;
	// Use this for initialization
	void Start () {
		lastColor = bodyRenderer.material.color;
	}
	public void SoftChangeColor(Color newColor)
    {
		if (lastColor == newColor) return;
		if (animatorHandler != null)
			StopCoroutine(animatorHandler);
		lastColor = newColor;
		animatorHandler = AnimateBodyRenderer(newColor);
		StartCoroutine(animatorHandler);
	}
	public void ChangeColor(Color newColor)
    {
		if (animatorHandler != null)
			StopCoroutine(animatorHandler);
		lastColor = newColor;
		animatorHandler = AnimateBodyRenderer(newColor);
		StartCoroutine(animatorHandler);
    }
	public IEnumerator AnimateBodyRenderer(Color newColor)
    {
		yield return null;
		Color lastColor = bodyRenderer.material.color;
        for (float x = 0; x <= 1f; x += Time.deltaTime * speed)
        {
			yield return null;
			bodyRenderer.material.color = lastColor * (1f - x) + Color.white * x;
        }
		for (float x = 0; x <= 1f; x += Time.deltaTime * speed)
		{
			yield return null;
			bodyRenderer.material.color = newColor * x + Color.white * (1f - x);
		}
		bodyRenderer.material.color = newColor;
	}

	// Update is called once per frame
	void Update () {
		
	}
}
