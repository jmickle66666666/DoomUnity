
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MouseLook : MonoBehaviour
{

	// I copied this from my doom project's player controller, mouse/camera rotation is a p a i n
 
	public enum RotationAxes { MouseX = 1, MouseY = 2, None = 3 }
	public RotationAxes axes = RotationAxes.MouseX;
	public bool invertY = false;
	
	public float sensitivityX = 10F;
	public float sensitivityY = 9F;
 
	public float minimumY = -85F;
	public float maximumY = 85F;
 
	float rotationX = 0F;
	float rotationY = 0F;
 
	Quaternion originalRotation;
	
	void Start ()
	{			
		if (GetComponent<Rigidbody>())
		{
			GetComponent<Rigidbody>().freezeRotation = true;
		}
		
		originalRotation = transform.localRotation;
	}
 
	void Update ()
	{
		if (axes == RotationAxes.MouseX)
		{			
			rotationX += Input.GetAxis("Mouse X") * sensitivityX * Time.timeScale;
			Quaternion xQuaternion = Quaternion.AngleAxis (rotationX, Vector3.up);
			transform.localRotation = originalRotation * xQuaternion;			
		}
		else if (axes == RotationAxes.MouseY)
		{			
			float invertFlag = 1f;
 			if( invertY )
 			{
 				invertFlag = -1f;
 			}

			rotationY += Input.GetAxis("Mouse Y") * sensitivityY * invertFlag * Time.timeScale;
			
			rotationY = Mathf.Clamp(rotationY, minimumY, maximumY);
 
			Quaternion yQuaternion = Quaternion.AngleAxis (rotationY, Vector3.left);
			transform.localRotation = originalRotation * yQuaternion;
		}
	}
	
	public void SetSensitivity(float s)
	{
		sensitivityX = s;
		sensitivityY = s;
	}
}