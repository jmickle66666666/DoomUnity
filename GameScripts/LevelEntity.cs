using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WadTools;
using System.Reflection;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider))]
public class LevelEntity : MonoBehaviour {

	// Object references
	BoxCollider boxCollider;
	SpriteRenderer spriteRenderer;
	Material spriteMaterial;
	MultigenState state;
	MultigenParser multigen;
	MultigenObject mobj;
	WadFile wad;

	// State control
	public string stateName;
	string nextState;
	public float direction;
	static float tick = (1.0f / 35.0f);
	bool timerActive = true;
	float stateTimer;

	// Monster attack behaviour
	public float reactionTime;
	public GameObject target;
	bool justAttacked = false;
	int moveTime = 0;
	float radius;
	float height;
	float speed;
	Vector3 move;

	// Sounds
	AudioClip seeSound;
	AudioClip attackSound;
	AudioClip activeSound;
	static AudioClip itemPickupSound;
	static AudioClip weaponPickupSound;

	// Static sprite cache
	string spriteName {
		get {
			return state.spriteName + state.spriteFrame;
		}
	}
	static Dictionary<string, Sprite[]> sprites;

	public void LoadMultigen(MultigenParser multigen, MultigenObject mobj, WadFile wad) {

		this.mobj = mobj;
		this.multigen = multigen;
		this.wad = wad;

		if (sprites == null) {
			sprites = new Dictionary<string, Sprite[]>();
		}

		if (itemPickupSound == null) {
			itemPickupSound = new DoomSound(wad.GetLump("DSITEMUP"), "DSITEMUP").ToAudioClip();
		}

		if (weaponPickupSound == null) {
			weaponPickupSound = new DoomSound(wad.GetLump("DSWPNUP"), "DSWPNUP").ToAudioClip();
		}

		boxCollider = GetComponent<BoxCollider>();
		boxCollider.isTrigger = !mobj.data["flags"].Contains("MF_SOLID");

		radius = ReadFracValue(mobj.data["radius"]);
		height = ReadFracValue(mobj.data["height"]);
		speed = float.Parse(mobj.data["speed"]) / 128f;
		move = new Vector3();

		boxCollider.size = new Vector3(radius, height, radius);

		reactionTime = float.Parse(mobj.data["reactiontime"]) * tick;

		spriteMaterial = new Material(Shader.Find("Doom/Texture"));
		Texture2D paletteLookup = new Palette(wad.GetLump("PLAYPAL")).GetLookupTexture();
		Texture2D colormapLookup = new Colormap(wad.GetLump("COLORMAP")).GetLookupTexture();
		spriteMaterial.SetTexture("_Palette", paletteLookup);
		spriteMaterial.SetTexture("_Colormap", colormapLookup);

		spriteRenderer = GetComponent<SpriteRenderer>();
		spriteRenderer.material = spriteMaterial;

		string seeSoundName = ParseSoundName(mobj.data["seesound"]);
		if (wad.Contains(seeSoundName)) {
			seeSound = new DoomSound(wad.GetLump(seeSoundName), seeSoundName).ToAudioClip();
		}

		string activeSoundName = ParseSoundName(mobj.data["activesound"]);
		if (wad.Contains(activeSoundName)) {
			activeSound = new DoomSound(wad.GetLump(activeSoundName), activeSoundName).ToAudioClip();
		}

		string attackSoundName = ParseSoundName(mobj.data["attacksound"]);
		if (wad.Contains(attackSoundName)) {
			attackSound = new DoomSound(wad.GetLump(attackSoundName), attackSoundName).ToAudioClip();
		}

		UpdateVerticalPosition();
		SetState(mobj.data["spawnstate"]);
	}

	string ParseSoundName(string value) {
		return value.Replace("sfx_","DS").ToUpper();
	}

	float ReadFracValue(string value) {
		float output;
		if (value.Contains("*")) {
			output = float.Parse(value.Substring(0, value.IndexOf("*")));
			output /= 64f;
		} else {
			output = float.Parse(value);
		}
		return output;
	}

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {

		//direction += Time.deltaTime * 100.0f;

		if (sprites[spriteName].Length > 1) {
			float cameraAngle = Mathf.Atan2(Camera.main.transform.position.z - transform.position.z, Camera.main.transform.position.x - transform.position.x) * Mathf.Rad2Deg;
			//cameraAngle = 0f;

			int angle = Mathf.RoundToInt(((cameraAngle - direction)+360.0f) / 45.0f);
			angle = (angle + 8) % 8;
			spriteRenderer.sprite = sprites[spriteName][angle];
		} else {
			spriteRenderer.sprite = sprites[spriteName][0];
		}

		transform.localEulerAngles = new Vector3(0f, Camera.main.transform.eulerAngles.y, 0f);
		
		if (timerActive) {
			stateTimer -= Time.deltaTime;
			if (stateTimer <= 0f) {
				ChangeState();
			}
		}

	}
	
	void LoadState() {
		if (state.name == "S_NULL") {
			GameObject.Destroy(gameObject);
			return;
		}
		
		if (!sprites.ContainsKey(spriteName)) {
			sprites.Add(spriteName, state.GetSprite(wad));
		}

		if (state.duration == "-1") timerActive = false;
		stateTimer += float.Parse(state.duration) * tick;
		nextState = state.nextState;
		MethodInfo method = this.GetType().GetMethod(state.action);
		if (method != null) {
			method.Invoke(this, new object[]{});
		}

		stateName = state.name;
	}

	void ChangeState() {
		state = multigen.states[nextState];
		
		LoadState();
	}

	void SetState(string newState) {
		state = multigen.states[newState];
		LoadState();
	}

	void SetNextState(string newState) {
		nextState = newState;
	}

	void LookForPlayers() {
		GameObject player = GameObject.Find("Player");

		target = player;
		return;

		float playerDistance = Vector3.Distance(transform.position, player.transform.position);

		RaycastHit raycastinfo;
		Ray checkRay = new Ray(transform.position, (player.transform.position - transform.position).normalized);

		if (!Physics.Raycast(checkRay, out raycastinfo, playerDistance * 0.95f)) {
			target = player;
		}
	}

	void NewChaseDir() {
		// TODO
		//boxCollider.Raycast
		moveTime = Random.Range(0,16);
		move.x = 0f;
		move.y = 0f;

		if (CheckMeleeRange()) {
			NewFleeDir();
			return;
		}

		if (target.transform.position.x - speed > transform.position.x) {
			move.x = 1f;
		} else if (target.transform.position.x + speed< transform.position.x) {
			move.x = -1f;
		} 
		
		if (target.transform.position.z + speed < transform.position.z) {
			move.z = -1f;
		} else if (target.transform.position.z - speed > transform.position.z) {
			move.z = 1f;
		}

		direction = Mathf.Atan2(move.z, move.x) * Mathf.Rad2Deg;
	}

	void NewFleeDir() {
		move.x = 0f;
		move.y = 0f;

		if (target.transform.position.x < transform.position.x) {
			move.x = 1f;
		} else if (target.transform.position.x > transform.position.x) {
			move.x = -1f;
		} 
		
		if (target.transform.position.z > transform.position.z) {
			move.z = -1f;
		} else if (target.transform.position.z < transform.position.z) {
			move.z = 1f;
		}

		direction = Mathf.Atan2(move.z, move.x) * Mathf.Rad2Deg;
	}

	bool CheckMeleeRange() {
		// TODO
		return Vector3.Distance(transform.position, target.transform.position) - radius < 1.0f;
	}

	bool CheckMissileRange() {
		// TODO
		return true;
	}

	void Move() {
		// First redo vertical height
		UpdateVerticalPosition();

		if (!Physics.Raycast(transform.position + new Vector3(0f, 0.375f, 0f), move, speed + radius)) {
			transform.Translate(move * speed, Space.World);
			//moveTime = Random.Range(0,255) & 15;
		}
	}

	void UpdateVerticalPosition() {
		RaycastHit hit;
		Ray ray = new Ray(transform.position + new Vector3(0f, 1f, 0f), Vector3.down);

		if (Physics.Raycast(ray, out hit, 1000f, LayerMask.GetMask("Level"))) {
			float yHeight = hit.point.y;
			transform.position = new Vector3(transform.position.x, yHeight, transform.position.z);
		}
	}

	void OnTriggerEnter(Collider collision) {
		if (mobj.data["flags"].Contains("MF_SPECIAL")) { // MF_SPECIAL identifies pickups
			if (collision.gameObject.name == "Player") {

				ItemInfo info = ItemData.Get(state.spriteName);
				DoomSound.PlaySoundAtPoint(wad, info.sound, transform.position);
				HUD.Message(Locale.Get(info.message));

				GameObject.Destroy(gameObject);
			}
		}
	}

	bool IsValid(string value) {
		return value != "0" && value != "S_NULL";
	}

	///// CODEPOINTERS =====================================

	public void A_Look() {
		Vector3 heading = Camera.main.transform.position - transform.position;
		float dot = Vector3.Dot(heading.normalized, Quaternion.AngleAxis(direction, Vector3.forward) * Vector3.right);

		float playerDistance = Vector3.Distance(transform.position, Camera.main.transform.position);

		// If player is in front of the object
		if (dot > 0) {
			RaycastHit raycastinfo;
			Ray checkRay = new Ray(transform.position, (Camera.main.transform.position - transform.position).normalized);

			if (!Physics.Raycast(checkRay, out raycastinfo, playerDistance)) {
				
				if (seeSound != null) {
					AudioSource.PlayClipAtPoint(seeSound, transform.position);
				}
				
				SetNextState(mobj.data["seestate"]);
			}
		}
	}

	// UNFINISHED

	public void A_Chase() {
		if (reactionTime > 0f) reactionTime -= Time.deltaTime;
		
		if (target == null) {
			
			LookForPlayers();

			if (target == null) {
				SetNextState(mobj.data["spawnstate"]);
			}

			return;
		}
		
		if (justAttacked) { 
			justAttacked = false;
			NewChaseDir();
			return;
		}
		
		if (IsValid(mobj.data["meleestate"]) && CheckMeleeRange()) {
			if (attackSound != null) {
				AudioSource.PlayClipAtPoint(attackSound, transform.position);
			}

			SetState(mobj.data["meleestate"]);
			return;
		}		
		
		if (IsValid(mobj.data["missilestate"])) {
			if (( /* difficulty is not nightmare, && */ moveTime > 0) || !CheckMissileRange()) {
				moveTime -= 1;
				Move();
				if (moveTime <= 0) {
					NewChaseDir();
				}

				if (Random.value < (3f/255f)) {
					AudioSource.PlayClipAtPoint(activeSound, transform.position);
				}
			} else {
				SetState(mobj.data["missilestate"]);
				justAttacked = true;
				return;
			}
		}
	}

	public void A_FaceTarget() {
		direction = Mathf.Atan2(target.transform.position.z - transform.position.z, target.transform.position.x - transform.position.x) * Mathf.Rad2Deg;
	}

	
}
