using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;

/*
This needs to be attached to the player camera, and is necessary for correct sky rendering
*/

public class CameraDepth : MonoBehaviour {

	public Material mat;
	private bool automap = false;
	private Camera cam;
    private float automapZoom = 15f;

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
                transform.localPosition = new Vector3(0f, 100f, -80f);
				cam.orthographic = true;
                cam.orthographicSize = automapZoom;
				automap = true;
				HUD.ShowMapName();
			}
		}

        if (System.Math.Abs(Input.GetAxisRaw("Mouse ScrollWheel")) > 0.01f) {
            if (automap) {
                cam.orthographicSize -= Input.GetAxisRaw("Mouse ScrollWheel");
                automapZoom = cam.orthographicSize;
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
        }

        if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
        }

		if (Input.GetKeyDown(KeyCode.P)) {
			string datetime = DateTime.Now.ToString();
			datetime = datetime.Replace("/", "");
			datetime = datetime.Replace(" ", "");
			datetime = datetime.Replace(":", "");

			if (!Directory.Exists("Screenshots")) {
				Directory.CreateDirectory("Screenshots");
			}

			string pathname = "Screenshots/NaSTY_"+datetime+".png";

			ScreenCapture.CaptureScreenshot(pathname);

			StartCoroutine("DelayedScreenshotMessage");

		}
	}

	public IEnumerator DelayedScreenshotMessage() {
		yield return new WaitForSeconds(0.1f);
		HUD.Message("Screenshot captured");
	}
}
