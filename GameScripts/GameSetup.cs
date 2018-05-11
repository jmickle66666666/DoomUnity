using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using UnityMidi;
using AudioSynthesis.Midi;
using AudioSynthesis.Bank;
using WadTools;
#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class IwadInfo {
	public string name;
	public string md5;
	public string mapnameFormat;
	public string titleMusic;
	public string mapInfo;
	public string multigen;
}

[System.Serializable]
public class IwadData {
	public IwadInfo[] iwads;
}

public class CommandlineArguments {
	public List<string> pwads;
	public string iwad;
	public string warp;
	public string soundfont;
	public bool runTests;
	public bool midi;
    public bool nomonsters;

	public CommandlineArguments() {
		pwads = new List<string>();
		iwad = "";
		warp = "";
		soundfont = "";
		runTests = false;
		midi = false;
        nomonsters = false;
	}
}

/*

The "Master" controller. Does most things

This handles the following:
Loading the engine wad ("nasty.wad")
Loading the iwads
Creating the map and player
Cheats
Switching to new wads
Activating and using the menu
More stuff

*/

public class GameSetup : MonoBehaviour {

	public static GameSetup main;
	public GameObject playerPrefab;
	private GameObject player;
	private FirstPersonDrifter fpd;
	private string currentMap = "";
	private DoomMapBuilder mapBuilder;
	public static WadFile wad;
	private DoomMenu menu;
	private bool menuActive;
	private TitleSetup title;
	private enum MapFormat {
		MAP,
		EM
	}
	private MapFormat mapFormat;
	private WadFile engineWad;

	private bool iwadSelector = false;
	private List<IwadInfo> foundIwads;
	private List<string> iwadPaths;
	private MultigenParser multigen;

	private MidiPlayer midiPlayer;
	private Dictionary<string,MapInfo> mapinfo;
	public bool midiEnabled = false;
	[SerializeField] private string editorArgs;
	private bool buildingMap = false;

	private CommandlineArguments args;

	// Use this for initialization
	void Start () {
		main = this;
		Settings.Init();
		ParseArguments();

        Settings.Set("nomonsters", args.nomonsters ? "true" : "false");

		midiEnabled = args.midi;

		engineWad = new WadFile("nasty.wad");

		if (midiEnabled) {
			if (File.Exists(args.soundfont)) {
				midiPlayer = gameObject.AddComponent<MidiPlayer>();
				midiPlayer.LoadBank(new PatchBank(File.OpenRead(args.soundfont), "sf2"));
			} else {
				if (engineWad.Contains("GMBANK")) {
					midiPlayer = gameObject.AddComponent<MidiPlayer>();
					midiPlayer.LoadBank(new PatchBank(engineWad.GetLumpAsMemoryStream("GMBANK"), "bank"));
				} else {
					Debug.LogError("No soundfont found, disabling midi");
				}
			}
		}
		
		IwadData iwadData = JsonUtility.FromJson<IwadData>(engineWad.GetLumpAsText("IWADS"));

		cheatCodes = new List<string>() {
			"idclev",
			"idclip",
			"kill",
			"test"
		};

		SetupTitleCamera();

		string iwadDirectory = Settings.Get("iwads_path", "./");

		if (args.iwad == "") { // Run IWAD selection tool

			foundIwads = new List<IwadInfo>();
			iwadPaths = new List<string>();

			var fileInfo = new DirectoryInfo(iwadDirectory).GetFiles();
			foreach (var file in fileInfo) {
				string fileMd5 = WadFile.GetMD5(file.FullName);
				if (file.Extension.ToUpper() == ".WAD") {
					for (int i = 0; i < iwadData.iwads.Length; i++) {
						if (fileMd5 == iwadData.iwads[i].md5) {
							foundIwads.Add(iwadData.iwads[i]);
							iwadPaths.Add(file.FullName);
						}
					}
				}
			}

			if (foundIwads.Count == 0) {
				Debug.LogError("Cannot find any iwads!");
			}
			if (foundIwads.Count > 1) {
				iwadSelector = true;
			}

			if (foundIwads.Count == 1) {
				SetupWad(foundIwads[0], iwadPaths[0]);
			}

		} else {
			for (int i = 0; i < iwadData.iwads.Length; i++) {
				if (WadFile.GetMD5(args.iwad) == iwadData.iwads[i].md5) {
					SetupWad(iwadData.iwads[i], args.iwad);
					break;
				}
			}
		}
		
	}

	void SetupHUD() {
		HUD.wad = wad;
		GameObject HUDObject = new GameObject("HUD");
		HUDObject.AddComponent<HUD>();
	}

	void SetupTitleCamera() {
		GameObject titleCameraObject = new GameObject("TitleCamera");
		titleCameraObject.layer = 8;
		Camera titleCamera = titleCameraObject.AddComponent<Camera>();
		titleCamera.orthographic = true;
		titleCamera.orthographicSize = 1f;
		titleCamera.cullingMask = 256;
		GameObject titleQuad = new GameObject("TitleQuad");
		titleQuad.layer = 8;
		titleQuad.transform.parent = titleCameraObject.transform;
		title = titleQuad.AddComponent<TitleSetup>();
		title.Build(engineWad);
	}

	void SetupWad(IwadInfo info, string path) {
		
		if (info.mapInfo != null) {
			mapinfo = MapInfoLump.Load(engineWad.GetLumpAsText(info.mapInfo), engineWad);
		}

		wad = new WadFile(path);
		if (info.mapnameFormat == "MAP") mapFormat = MapFormat.MAP;
		if (info.mapnameFormat == "EM") mapFormat = MapFormat.EM;
		iwadSelector = false;

		for (int i = 0; i < args.pwads.Count; i++) {
			wad.Merge(args.pwads[i]);
		}

		if (info.multigen != null) {
			multigen = new MultigenParser(engineWad.GetLumpAsText(info.multigen));
		}

		StartGame(info);
	}

	void OnGUI() {
		float wscale = (float)Screen.width / 320.0f;
		float hscale = (float)Screen.height / 200.0f;
		if (iwadSelector) {

			if (GUI.Button(new Rect(260 * wscale, 110 * hscale, 40 * wscale, 20 * hscale), "Quit")) {
				#if UNITY_EDITOR
					EditorApplication.isPlaying = false;
				#endif
				Application.Quit();
			}

			for (int i = 0; i < foundIwads.Count; i++) {
				if (GUI.Button(new Rect(10 * wscale, (10 * hscale) + (i * 25), 200 * wscale, 20), foundIwads[i].name)) {
					SetupWad(foundIwads[i], iwadPaths[i]);
				}
			}
		}

		if (buildingMap) {
			GUI.Box(new Rect(0f, 198f * hscale, mapBuilder.amountLoaded * Screen.width, 200f*hscale), "");
		}
	}

	void ParseArguments() {
		string[] arguments = System.Environment.GetCommandLineArgs();
		#if UNITY_EDITOR
		arguments = editorArgs.Split(' ');
		#endif
		args = new CommandlineArguments();

        for (int i = 0; i < arguments.Length; i++) {

			if (arguments[i] == "-iwad") {
				args.iwad = arguments[i+1];
			}

			if (arguments[i] == "-file" || (i == 0 && arguments[0] != "" && arguments[0][0] != '-')) {
				int j = i + 1;
				while (j < arguments.Length && arguments[j][0] != '-') {
					args.pwads.Add(arguments[j]);
					j++;
				}
			}

			if (arguments[i] == "-warp") {
				args.warp = arguments[i+1];
			}

			if (arguments[i] == "-soundfont") {
				args.soundfont = arguments[i+1];
			}

			if (arguments[i] == "-test") {
				args.runTests = true;
			}

			if (arguments[i] == "-midi") {
				args.midi = true;
			}

            if (arguments[i] == "-nomonsters") {
                args.nomonsters = true;
            }
		}
	}

	void StartGame(IwadInfo info) {
		mapBuilder = new DoomMapBuilder();

		if (args.runTests) {
			Debug.Log("Running tests...");

			// Keep a separate map builder to avoid issues building maps afterwards
			DoomMapBuilder testMapBuilder = new DoomMapBuilder();

			foreach (KeyValuePair<string, MapInfo> entry in mapinfo) {
				int errors = testMapBuilder.TestMap(wad, entry.Key);
				if (errors > 0) {
					Debug.Log("Failed sectors in "+entry.Key+": "+errors);
				}
			}
		}

		if (args.warp == "") {
			title.Build(wad);
			PlayMidi(info.titleMusic);
		} else {
			title.DisableCamera();
			menuActive = false;
			BuildMap(args.warp);
		}
		SetupMenu();
		SetupHUD();
	}

	void PlayMidi(string name) {
		if (midiPlayer != null) {

			MidiFile midi = null;
			DataType musType = wad.DetectType(name);
			if (musType == DataType.MIDI) {
				midi = new MidiFile(wad.GetLump(name));
			} else if (musType == DataType.MUS) {
				midi = new MidiFile(new Mus2Mid(wad.GetLump(name)).MidiData());
			} else {
				Debug.LogError("Not a midi or mus which are the only thigns supported rn surprisingly enough");
			}

			if (midi != null) {
				midiPlayer.Stop();
				midiPlayer.LoadMidi(midi);
				midiPlayer.Play();
			}
		}
	}

	float time;

	void BuildMap(string mapname) {
		if (menuActive) {
			menu.Show(false, true);
			menuActive = false;
		}
		if (GameObject.Find(currentMap) != null) GameObject.Find(currentMap).name = "CLEAR";
		time = Time.realtimeSinceStartup;
		currentMap = mapname;
		if (mapinfo != null) {
			mapBuilder.SetMapInfo(mapinfo.ContainsKey(mapname) ? mapinfo[mapname] : null);
		}
		mapBuilder.doneBuilding = FinishMap;
		mapBuilder.BuildMap(wad, mapname);
		buildingMap = true;
	}

	void FinishMap() {
		Debug.Log("Map build time: "+(Time.realtimeSinceStartup-time));
		title.DisableCamera();
		CreatePlayer();
		if (midiEnabled) {
			PlayMidi(mapinfo[currentMap].music);
		}
		if (multigen != null && Settings.Get("nomonsters", "false") == "false") {
			mapBuilder.BuildLevelEntities(multigen);
		}
		buildingMap = false;
		GameObject.Destroy(GameObject.Find("CLEAR"));
		HUD.SetMapName(mapinfo[currentMap].name);
	}

	void CreatePlayer() {
		int playerIndex = mapBuilder.GetIndexOfThing(1);
		Thing playerThing = mapBuilder.map.things[playerIndex];
		float playerScale = 0.6f;

		if (player != null) {
			GameObject.Destroy(player);
		}

		player = Instantiate(playerPrefab);
		player.transform.localScale = new Vector3(playerScale,playerScale,playerScale);
		player.transform.position = new Vector3(playerThing.x * mapBuilder.SCALE, 
								 			    (mapBuilder.thingSectors[playerIndex].floorHeight + mapBuilder.PLAYER_HEIGHT) * mapBuilder.SCALE * 1.2f, 
								 			    playerThing.y * mapBuilder.SCALE);
		player.transform.localEulerAngles = new Vector3(0f, 90f - playerThing.angle, 0f);
		fpd = player.GetComponent<FirstPersonDrifter>();

		player.transform.Find("Main Camera").GetComponent<MouseLook>().enabled = (Settings.Get("MOUSE_VLOOK", "false") == "true");
	}
	
	private List<string> cheatCodes;
	private bool cheatLevelChange = false;
	private string levelChangeId = "";
	private string currentCheat = "";

	// Update is called once per frame
	void Update () {
		MenuUpdate();
		CheatUpdate();
	}

	string GetMapName(int index) {
		string output;
		
		if (mapFormat == MapFormat.MAP) {
			output = index.ToString();
			if (output.Length == 1) output = "0"+output;
			return "MAP"+output;
		}

		if (mapFormat == MapFormat.EM) {
			string indexS = index.ToString();
			if (indexS.Length == 1) indexS = "0"+indexS;
			output = "E";
			output += indexS[0];
			output += "M";
			output += indexS[1];
			return output;
		}
		return "";
	}

	void SetupMenu() {
		menu = new DoomMenu(wad);
		menu.onQuit = MenuQuit;
		menu.onPlay = MenuPlay;
	}

	void MenuUpdate() {

		if (Input.GetKeyDown(KeyCode.Escape)) {
			menuActive = !menuActive;
			SetPlayerActive(!menuActive);
			title.Darken(menuActive);
			menu.Show(menuActive);
		}

		if (menuActive) {
			menu.Update(Time.deltaTime);

			if (Input.GetKeyDown(KeyCode.DownArrow)) {
				menu.Down();
			}

			if (Input.GetKeyDown(KeyCode.UpArrow)) {
				menu.Up();
			}

			if (Input.GetKeyDown(KeyCode.Return)) {
				menu.Accept();
			}
		}
	}

	void MenuPlay() {
		BuildMap(GetMapName((mapFormat==MapFormat.MAP)?1:11));
	}

	void MenuQuit() {
		#if UNITY_EDITOR
			EditorApplication.isPlaying = false;
		#endif
		Application.Quit();
	}

	void SetPlayerActive(bool active) {
		if (player != null) {
			player.GetComponent<MouseLook>().enabled = active;
			fpd.enabled = active;
		}	
	}

	void CheatUpdate() {
		// Cheats here
		foreach(char c in Input.inputString) {
			if (cheatLevelChange != true) {
				bool found = false;
				foreach(string s in cheatCodes) {
					if (s[currentCheat.Length] == c) {
						found = true;
						break;
					}
				}
				if (found) {
					currentCheat += c;
				} else {
					//Debug.Log(currentCheat + c);
					currentCheat = "";
				}
			} else {
				if (!Char.IsDigit(c)) {
					cheatLevelChange = false;
					currentCheat = "";
					levelChangeId = "";
				} else {
					levelChangeId += c;
				}

				if (levelChangeId.Length == 2) {

					int levelChange = Int32.Parse(levelChangeId);

					currentCheat = "";
					cheatLevelChange = false;
					WarpMap(GetMapName(levelChange));
				}
			}
		}

		if (currentCheat == "kill") {
			GameObject.Destroy(GameObject.Find(currentMap));
			currentCheat = "";
		}

		if (currentCheat == "idclev") {
			cheatLevelChange = true;
			levelChangeId = "";
			currentCheat = "";
		}

		if (currentCheat == "test") {
			HUD.Message("Test");
			Debug.Log(currentCheat);
			currentCheat = "";
		}

		if (currentCheat == "idclip") {
			HUD.Message("no clip mode "+(fpd.noClip?"off":"on"));
			fpd.noClip = !fpd.noClip;
			currentCheat = "";
		}
	}

	public void WarpMap(string mapname) {
		mapname = mapname.ToUpper();
		if (mapBuilder.wad.Contains(mapname)) {
			HUD.Message("Warping to map: "+mapname);
			BuildMap(mapname);
		} else {
			HUD.Message("Couldn't find map "+mapname);
		}
	}
}
