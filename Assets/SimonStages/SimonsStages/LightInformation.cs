using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightInformation : MonoBehaviour
{
		public Light ledGlow;
		public Renderer greyBase;
		public Renderer colorBase;
		public TextMesh lightText;
		public String colorName;
		public int colorIndex;
		public int soundIndex;
		public KMSelectable connectedButton;
		public AudioClip connectedSound;
}
