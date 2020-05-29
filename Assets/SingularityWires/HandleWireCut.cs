using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandleWireCut : MonoBehaviour {

	public KMSelectable wireSelectable;
	public MeshRenderer wireUncutMesh, wireCutMesh;
	public GameObject wireUncut,wireCut;


	// Use this for initialization
	void Awake () {

		wireSelectable.OnInteract += delegate {

			wireCut.SetActive(true);
			wireUncut.SetActive(false);

			return false;
		};
	}

	public void UpdateMaterials(Material given)
	{
		wireUncutMesh.material = given;
		wireCutMesh.material = given;
	}

	// Update is called once per frame
	void Update () {

	}
}
