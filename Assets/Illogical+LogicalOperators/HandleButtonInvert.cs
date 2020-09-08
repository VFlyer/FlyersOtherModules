using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandleButtonInvert : MonoBehaviour {

	public MeshRenderer buttonStatus;
	public KMSelectable selfSelectable;
	public Material[] buttonStates = new Material[2];

	public bool toggled = false;
	// Use this for initialization
	void Start () {
	}
	public virtual void ToggleState()
    {
		toggled = !toggled;
	}

	// Update is called once per frame
	void Update () {
		buttonStatus.material = buttonStates[toggled ? 1 : 0];
	}
}
