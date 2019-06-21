using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoomPlayer : MonoBehaviour {

	public CharacterController characterController;
	public float moveSpeed;

	private Vector3 movement;
	public bool noClip;
	bool _locked = false;
	public bool locked { 
		set { 
			_locked = value;
			horzLook.enabled = value;
			vertLook.enabled = value;
		}
	}

	public MouseLook horzLook;
	public MouseLook vertLook;
	new public Camera camera;
	public LevelEntity levelEntity;

	// Use this for initialization
	void Start () {
		movement = new Vector3();
	}
	
	// Update is called once per frame
	void Update () {
		if (_locked) return;
		movement.Set(1,1,1);
		movement.Scale(
			Input.GetAxisRaw("Horizontal") * camera.transform.right + Input.GetAxisRaw("Vertical") * transform.forward
		);
		
		if (noClip) {
			transform.Translate(movement * moveSpeed * Time.deltaTime, Space.World);
		} else {
			characterController.Move(
				movement * moveSpeed * Time.deltaTime
			);
		}


	}
}
