using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoomPlayer : MonoBehaviour {

	public CharacterController characterController;
	public float moveSpeed;

	private Vector3 movement;
	private Vector3 momentum;
	public float friction = 0.7f;
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
	bool onGround = false;
	public float gravity = 1f;

	// Use this for initialization
	void Start () {
		movement = new Vector3();
		momentum = new Vector3();
	}
	
	// Update is called once per frame
	void Update () {
		if (_locked) return;
		
		
		if (noClip) {

			movement.Set(1,1,1);
			movement.Scale(
				Input.GetAxisRaw("Horizontal") * camera.transform.right + Input.GetAxisRaw("Vertical") * camera.transform.forward
			);

			transform.Translate(movement * moveSpeed * Time.deltaTime, Space.World);

		} else {

			movement.Set(1,1,1);
			movement.Scale(
				Input.GetAxisRaw("Horizontal") * camera.transform.right + Input.GetAxisRaw("Vertical") * transform.forward
			);

			if (!onGround) {
				momentum.y -= gravity * Time.deltaTime;
			}

			movement += momentum;

			var coll = characterController.Move(
				movement * moveSpeed * Time.deltaTime
			);
			onGround = ((coll & CollisionFlags.Below) != 0);
			if (onGround) {
				momentum.y = 0f;
			}
		}
	}

	void FixedUpdate()
	{
		momentum = movement * friction;
	}
}
