using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BillboardSprite : MonoBehaviour {

	void Update () {
		transform.localEulerAngles = new Vector3(0f, Camera.main.transform.eulerAngles.y, 0f);
	}
}
