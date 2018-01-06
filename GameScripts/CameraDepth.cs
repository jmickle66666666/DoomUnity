using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
This needs to be attached to the player camera, and is necessary for correct sky rendering
*/

public class CameraDepth : MonoBehaviour {

	public Material mat;
	private bool automap = false;
	private Camera cam;

	// Use this for initialization
	void Start () {
		cam = GetComponent<Camera>();
	}

	void Update() {
		if (DoomMapBuilder.skyMaterial != null) {
			DoomMapBuilder.skyMaterial.SetFloat("_CameraAngle", transform.eulerAngles.y);
		}

		if (Input.GetKeyDown(KeyCode.Tab)) {
			if (automap) {
				transform.localEulerAngles = new Vector3(0f, 0f, 0f);
				transform.localPosition = new Vector3(0f, 0.5f, 0f);
				cam.orthographic = false;
				automap = false;
				HUD.HideMapName();
			} else {
				transform.localEulerAngles = new Vector3(45f, 0f, 0f);
				transform.localPosition = new Vector3(0f, 20f, -20f);
				cam.orthographic = true;
				cam.orthographicSize = 15;
				automap = true;
				HUD.ShowMapName();
			}
		}
	}
}
