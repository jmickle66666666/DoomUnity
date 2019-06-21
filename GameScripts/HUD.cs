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
	List<GameObject> consoleMessages;
	static Material messageMaterial;
	//private AudioSource audioSource;
	//private AudioClip soundMessage;
	GameObject consoleObject;
	// int maxConsoleMessages = 20;

	Vector3 consoleOpenHeight = new Vector3(0f, -0.7f, 0f);
	Vector3 consoleClosedHeight = new Vector3(0f, 0.1f, 0f);
	public bool consoleOpen = false;

	string consoleInput;
	SpriteRenderer consoleInputSR;

	GameObject mapNameObj;
	SpriteRenderer mapNameSR;
	static string mapname;

	// Use this for initialization
	void Start () {
		messages = new List<HUDMessage>();
		consoleMessages = new List<GameObject>();
		consoleObject = new GameObject("console");
		main = this;
		gameObject.layer = LayerMask.NameToLayer("HUD");
		SetupCamera();
		SetupMaterials();
		//audioSource = gameObject.AddComponent<AudioSource>();
		//audioSource.spatialBlend = 0.0f;
		// try {
		// soundMessage = new DoomSound(wad.GetLump("DSRADIO"), "HUD/Message").ToAudioClip();
		// } catch {}
		SpriteRenderer sr = consoleObject.AddComponent<SpriteRenderer>();
		

		if (wad.DetectType("TITLEPIC") == DataType.PNG) {
			Texture2D image = new Texture2D(2,2);
			ImageConversion.LoadImage(image, wad.GetLump("TITLEPIC"));
			sr.sprite = Sprite.Create(image, new Rect(0f, 0f, image.width, image.height), new Vector2(0.5f, -0.45f));
			sr.flipY = false;
			consoleObject.transform.localScale = new Vector3(320f / image.width, 200f / image.height);
			//sr.color = new Color(0.1f, 0.1f, 0.1f, 1.0f);
		} else {
			sr.material = messageMaterial;
			sr.sprite = Sprite.Create(new DoomGraphic(wad.GetLump("TITLEPIC")).ToRenderMap(), new Rect(0f, 0f, 320, -200), new Vector2(0.5f, -0.45f));
			sr.flipY = true;
			sr.material.SetFloat("_Brightness", 0.3f);
			
		}
		
		consoleObject.layer = LayerMask.NameToLayer("HUD");

		GameObject consoleInputObject = new GameObject("Console Input");
		consoleInputSR = consoleInputObject.AddComponent<SpriteRenderer>();
		consoleInputSR.material = messageMaterial;
		consoleInputObject.transform.parent = consoleObject.transform;
		consoleInputObject.transform.localPosition = new Vector3(-((float)Screen.width/(float)Screen.height), 1f, -0.1f);
		consoleInputObject.transform.localScale = new Vector3(1f, -1f, 1f);
		consoleInputObject.layer = LayerMask.NameToLayer("HUD");

		mapNameObj = new GameObject("Map name");
		mapNameSR = mapNameObj.AddComponent<SpriteRenderer>();
		mapNameSR.material = messageMaterial;
		mapNameObj.transform.parent = transform;
		mapNameObj.layer = LayerMask.NameToLayer("HUD");
		mapNameObj.transform.localPosition = new Vector3(-((float)Screen.width/(float)Screen.height), -((float)Screen.width/(float)Screen.height) + 0.5f, 0.1f);
		mapNameObj.transform.localScale = new Vector3(1f, -1f, 1f);
		if (mapname != null) UpdateMapName();
		HideMapName();

		// GameObject statusBar = new GameObject("StatusBar");
		// statusBar.layer = 9;
		// statusBar.transform.parent = transform;
		// StatusBar sbar = statusBar.AddComponent<StatusBar>();
		// sbar.Build(wad);

	}

	void SetupCamera() {
		cameraObject = new GameObject();
		cameraObject.transform.parent = transform;
		cameraObject.transform.localPosition = new Vector3(0f, 0f, -1f);

		cam = cameraObject.AddComponent<Camera>();
		cam.orthographic = true;
		cam.orthographicSize = 1;
		cam.cullingMask = LayerMask.GetMask("HUD");
		cam.clearFlags = CameraClearFlags.Depth;
	}
	
	// Update is called once per frame
	void Update () {
		UpdateMessages();

		if (Input.GetKeyDown(KeyCode.BackQuote)) {
			consoleOpen = !consoleOpen;
		} else {
			if (consoleOpen) {
				if (Input.GetKeyDown(KeyCode.Return)) {
					// Do something with the input
					ConsoleLog(consoleInput);
					ParseConsoleCommand(consoleInput);
					ClearConsoleInput();
				} else if (Input.GetKeyDown(KeyCode.Backspace)) {
					consoleInput = consoleInput.Substring(0, consoleInput.Length-1);
					SetInputSprite();
				} else if (Input.GetKeyDown(KeyCode.Tab)) {
					List<string> ac = Settings.Autocomplete(consoleInput);
					if (ac.Count == 1) {
						consoleInput = ac[0];
					} else {
						for (int i = 0; i < ac.Count; i++) {
							ConsoleLog(ac[i]);
						}
					}
					
				} else {
					consoleInput += Input.inputString;
					if (Input.inputString != "") {
						SetInputSprite();
					}
				}	
			} 
		}

		consoleObject.transform.position = Vector3.Lerp(consoleObject.transform.position, consoleOpen?consoleOpenHeight:consoleClosedHeight, Time.deltaTime * 10f);
		
	}

	string[] commands = new string[] {
		"map"
	};

	bool IsCommand(string command) {
		for (int i = 0; i < commands.Length; i++) {
			if (commands[i] == command) return true;
		}
		return false;
	}

	void ParseConsoleCommand(string command) {
		string[] splitCommand = command.Split(' ');

		if (IsCommand(splitCommand[0])) {
			if (splitCommand[0] == "map") {
				if (splitCommand.Length == 1) ConsoleLog("Usage: map [mapname]");
				if (splitCommand.Length == 2) {
					GameSetup.main.WarpMap(splitCommand[1]);
					consoleOpen = false;
				}
			}
		} else {

			if (splitCommand.Length == 1) ConsoleLog(Settings.Get(command));
			if (splitCommand.Length == 2) {
				ConsoleLog(Settings.Set(splitCommand[0], splitCommand[1], false));
			}

		}
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

	void SetInputSprite() {
		Texture2D tex2d = doomText.Write(consoleInput);
		consoleInputSR.sprite = Sprite.Create(tex2d, new Rect(0f, 0f, tex2d.width, -tex2d.height), new Vector2(0f, 1f));
	}

	void ClearConsoleInput() {
		consoleInput = "";
		SetInputSprite();
	}

	void UpdateMessageList() {
		for (int i = 0; i < messages.Count; i++) {
			messages[i].obj.transform.localPosition = new Vector3(-((float)Screen.width/(float)Screen.height), 1f - (((messages.Count-1) - i) * 0.1f), 0f);
		}
	}

	void UpdateConsoleMessages() {
		for (int i = 0; i < consoleMessages.Count; i++) {
			consoleMessages[i].transform.localPosition = new Vector3(-((float)Screen.width/(float)Screen.height), 1f + ((messages.Count + i + 1) * 0.1f), -0.1f);
		}
	}

	static void SetupMaterials() {
		if (doomText == null) doomText = new DoomText(wad);
		if (messageMaterial == null) {
			messageMaterial = new Material(Shader.Find("Doom/Unlit Texture"));
			messageMaterial.SetTexture("_Palette", new Palette(wad.GetLump("PLAYPAL")).GetLookupTexture());
			messageMaterial.SetTexture("_Colormap", new Colormap(wad.GetLump("COLORMAP")).GetLookupTexture());
		}
	}

	public static void Message(string message) {
		if (doomText == null) SetupMaterials();

		Texture2D messageTexture = doomText.Write(message);
		HUDMessage hudMessage = new HUDMessage();
		hudMessage.life = messageLife;
		hudMessage.obj = new GameObject(message);
		hudMessage.obj.layer = LayerMask.NameToLayer("HUD");
		hudMessage.obj.transform.localScale = new Vector3(1f, -1f, 1f);
		hudMessage.obj.transform.parent = main.gameObject.transform;
		SpriteRenderer hs = hudMessage.obj.AddComponent<SpriteRenderer>();
		hs.material = messageMaterial;
		hs.sprite = Sprite.Create(messageTexture, new Rect(0f, 0f, messageTexture.width, -messageTexture.height), new Vector2(0f, 1f));
		main.messages.Add(hudMessage);
		main.UpdateMessageList();
		//if (main.soundMessage != null) main.audioSource.PlayOneShot(main.soundMessage);

		// All hud messages go to the console too
		ConsoleLog(messageTexture, message);
	}

	public static void ConsoleLog(string message) {
		if (doomText == null) SetupMaterials();

		Texture2D messageTexture = doomText.Write(message);
		ConsoleLog(messageTexture, message);
	}

	private static void ConsoleLog(Texture2D messageTexture, string message) {
		GameObject messageObject = new GameObject(message);
		messageObject.layer = LayerMask.NameToLayer("HUD");
		messageObject.transform.localScale = new Vector3(1f, -1f, 1f);
		messageObject.transform.parent = main.consoleObject.transform;
		main.consoleMessages.Insert(0, messageObject);
		SpriteRenderer sr = messageObject.AddComponent<SpriteRenderer>();
		sr.material = messageMaterial;
		sr.sprite = Sprite.Create(messageTexture, new Rect(0f, 0f, messageTexture.width, -messageTexture.height), new Vector2(0f, 1f));
		main.UpdateConsoleMessages();
	}

	public void UpdateMapName() {
		Texture2D messageTexture = doomText.Write(mapname);
		mapNameSR.sprite = Sprite.Create(messageTexture, new Rect(0f, 0f, messageTexture.width, -messageTexture.height), new Vector2(0f, 1f));
	}

	public static void SetMapName(string mapname) {
		HUD.mapname = mapname;
		if (main != null) {
			main.UpdateMapName();
		}
	}

	public static void HideMapName() {
		main.mapNameSR.enabled = false;
	}

	public static void ShowMapName() {
		main.mapNameSR.enabled = true;
	}
}
