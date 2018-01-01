using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
This needs to be attached to the player camera, and is necessary for correct sky rendering
*/

public class CameraDepth : MonoBehaviour {

	public Material mat;

	// Use this for initialization
	void Start () {
		Camera camera = GetComponent<Camera>();
		camera.depthTextureMode = DepthTextureMode.Depth;
	}

	void Update() {
		if (DoomMapBuilder.skyMaterial != null) {
			DoomMapBuilder.skyMaterial.SetFloat("_CameraAngle", transform.eulerAngles.y);
		}
	}
}
