using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WadTools;

public class HUDMessage {
	public GameObject obj;
	public float life;

	public void Kill() {
		GameObject.Destroy(obj);
	}
}

public class HUD : MonoBehaviour {

	Camera cam;
	GameObject cameraObject;
	static HUD main;
	static DoomText doomText;
	public static WadFile wad;
	static float messageLife = 3f;
	List<HUDMessage> messages;
	static Material messageMaterial;
	private AudioSource audioSource;
	private AudioClip soundMessage;

	// Use this for initialization
	void Start () {
		messages = new List<HUDMessage>();
		main = this;
		gameObject.layer = 9;
		SetupCamera();
		audioSource = gameObject.AddComponent<AudioSource>();
		audioSource.spatialBlend = 0.0f;
		soundMessage = new DoomSound(wad.GetLump("DSRADIO"), "HUD/Message").ToAudioClip();
	}

	void SetupCamera() {
		cameraObject = new GameObject();
		cameraObject.transform.parent = transform;
		cameraObject.transform.localPosition = new Vector3(0f, 0f, -1f);

		cam = cameraObject.AddComponent<Camera>();
		cam.orthographic = true;
		cam.orthographicSize = 1;
		cam.cullingMask = 512;
		cam.clearFlags = CameraClearFlags.Depth;
	}
	
	// Update is called once per frame
	void Update () {
		UpdateMessages();
	}

	void UpdateMessages() {
		for (int i = 0; i < messages.Count; i++) {
			messages[i].life -= Time.deltaTime;
			if (messages[i].life <= 0) {
				messages[i].Kill();
				messages.RemoveAt(i);
				i--;
			}
		}
	}

	void UpdateMessageList() {
		for (int i = 0; i < messages.Count; i++) {
			messages[i].obj.transform.localPosition = new Vector3(-((float)Screen.width/(float)Screen.height), 1f - (((messages.Count-1) - i) * 0.1f), 0f);
		}
	}

	public static void Message(string message) {
		if (doomText == null) doomText = new DoomText(wad);
		if (messageMaterial == null) {
			messageMaterial = new Material(Shader.Find("Doom/Unlit Texture"));
			messageMaterial.SetTexture("_Palette", new Palette(wad.GetLump("PLAYPAL")).GetLookupTexture());
			messageMaterial.SetTexture("_Colormap", new Colormap(wad.GetLump("COLORMAP")).GetLookupTexture());
		}

		Texture2D messageTexture = doomText.Write(message);
		HUDMessage hudMessage = new HUDMessage();
		hudMessage.life = messageLife;
		hudMessage.obj = new GameObject(message);
		hudMessage.obj.layer = 9;
		hudMessage.obj.transform.localScale = new Vector3(1f, -1f, 1f);
		hudMessage.obj.transform.parent = main.gameObject.transform;
		SpriteRenderer hs = hudMessage.obj.AddComponent<SpriteRenderer>();
		hs.material = messageMaterial;
		hs.sprite = Sprite.Create(messageTexture, new Rect(0f, 0f, messageTexture.width, -messageTexture.height), new Vector2(0f, 1f));
		main.messages.Add(hudMessage);
		main.UpdateMessageList();
		main.audioSource.PlayOneShot(main.soundMessage);
	}
}
