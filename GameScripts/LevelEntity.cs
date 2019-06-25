using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WadTools;
using System.Reflection;

[SelectionBase]
[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(BoxCollider))]
public class LevelEntity : MonoBehaviour {

	// Object references
	BoxCollider boxCollider;
	SpriteRenderer spriteRenderer;
	Transform spriteTransform;
	Material spriteMaterial;
	MultigenState state;
	MultigenObject mobj;
	WadFile wad;
	AudioSource audioSource;

	// State control
	public string stateName;
	string nextState;
	public float direction;
	static float tick = (1.0f / 35.0f);
	bool timerActive = true;
	float stateTimer;

	// Monster attack behaviour
	public float reactionTime;
	public LevelEntity target; 
	public static Transform targetTransform;
	bool justAttacked = false; // has this entity just attacked
	bool justHit = false; // has this entity just *been* attacked
	int moveTime = 0;
	public float radius;
	public float height;
	public float speed;
	Vector3 move;
	static float stepHeight = 0.375f;

	// flags
	bool MF_MISSILE = false;
	bool MF_SPECIAL = false;
	bool MF_SOLID = false;

	// Sounds
	AudioClip seeSound;
	AudioClip[] seeSounds;
	AudioClip attackSound;
	AudioClip activeSound;
	AudioClip deathSound;
	static AudioClip itemPickupSound;
	static AudioClip weaponPickupSound;

	// Player/sight information
	public static LevelEntity playerEntity; // DUMMY
	public static Transform playerTransform;
	public static GameObject player;
	public static Camera mainCamera;
	Vector3 sightPosition {
		get {
			return transform.position + (Vector3.up * height * 0.5f);
		}
	}
	bool firstTick = true; // To get rid of first-frame issues

	// Static sprite cache
	string spriteName {
		get {
			return state.spriteName + state.spriteFrame;
		}
	}
	static Dictionary<string, Sprite[]> sprites;

	public void LoadMultigen(MultigenObject mobj, WadFile wad) {
		this.mobj = mobj;
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

		MF_MISSILE = mobj.data["flags"].Contains("MF_MISSILE");
		MF_SPECIAL = mobj.data["flags"].Contains("MF_SPECIAL");
		MF_SOLID = mobj.data["flags"].Contains("MF_SOLID");

		boxCollider = GetComponent<BoxCollider>();
		boxCollider.isTrigger = !MF_SOLID;

		

		radius = ReadFracValue(mobj.data["radius"]);
		height = ReadFracValue(mobj.data["height"]);
		speed = ReadFracValue(mobj.data["speed"]);
		move = new Vector3();

		float vHeight = height;
		if (!MF_SPECIAL) {
			vHeight -= stepHeight * 0.5f;
		} else {
			vHeight *= 2f;
		}
		boxCollider.size = new Vector3(radius, vHeight, radius);
		boxCollider.center = new Vector3(0f, (vHeight * 0.5f), 0f);

		reactionTime = float.Parse(mobj.data["reactiontime"]) * tick;

		spriteMaterial = new Material(Shader.Find("Doom/Texture"));
		Texture2D paletteLookup = new Palette(wad.GetLump("PLAYPAL")).GetLookupTexture();
		Texture2D colormapLookup = new Colormap(wad.GetLump("COLORMAP")).GetLookupTexture();
		spriteMaterial.SetTexture("_Palette", paletteLookup);
		spriteMaterial.SetTexture("_Colormap", colormapLookup);

		spriteRenderer = spriteTransform.gameObject.AddComponent<SpriteRenderer>();
		spriteRenderer.material = spriteMaterial;

		switch (mobj.data["seesound"]) {
			case "sfx_posit1":
			case "sfx_posit2":
			case "sfx_posit3":
				seeSounds = new AudioClip[] {
					new DoomSound(wad.GetLump("DSPOSIT1"), "DSPOSIT1").ToAudioClip(),
					new DoomSound(wad.GetLump("DSPOSIT2"), "DSPOSIT2").ToAudioClip(),
					new DoomSound(wad.GetLump("DSPOSIT3"), "DSPOSIT3").ToAudioClip()
				};
				break;
			case "sfx_bgsit1":
			case "sfx_bgsit2":
				seeSounds = new AudioClip[] {
					new DoomSound(wad.GetLump("DSBGSIT1"), "DSBGSIT1").ToAudioClip(),
					new DoomSound(wad.GetLump("DSBGSIT2"), "DSBGSIT2").ToAudioClip()
				};
				break;
			default:
				string seeSoundName = ParseSoundName(mobj.data["seesound"]);

				if (wad.Contains(seeSoundName)) {
					seeSound = new DoomSound(wad.GetLump(seeSoundName), seeSoundName).ToAudioClip();
				}
				break;
		}

		

		string activeSoundName = ParseSoundName(mobj.data["activesound"]);
		if (wad.Contains(activeSoundName)) {
			activeSound = new DoomSound(wad.GetLump(activeSoundName), activeSoundName).ToAudioClip();
		}

		string attackSoundName = ParseSoundName(mobj.data["attacksound"]);
		if (wad.Contains(attackSoundName)) {
			attackSound = new DoomSound(wad.GetLump(attackSoundName), attackSoundName).ToAudioClip();
		}

		string deathSoundName = ParseSoundName(mobj.data["deathsound"]);
		if (wad.Contains(deathSoundName)) {
			deathSound = new DoomSound(wad.GetLump(deathSoundName), deathSoundName).ToAudioClip();
		}

		audioSource = GetComponent<AudioSource>();

		// UpdateVerticalPosition();
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
			output = float.Parse(value) / 128f; 
		}
		return output;
	}

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {

		if (this.mobj == null) return;

		//direction += Time.deltaTime * 100.0f;

		if (sprites[spriteName].Length > 1) {
			float cameraAngle = Mathf.Atan2(mainCamera.transform.position.z - transform.position.z, mainCamera.transform.position.x - transform.position.x) * Mathf.Rad2Deg;
			//cameraAngle = 0f;

			int angle = Mathf.RoundToInt(((cameraAngle - direction)+360.0f) / 45.0f);
			angle = (angle + 8) % 8;
			spriteRenderer.sprite = sprites[spriteName][angle];
		} else {
			spriteRenderer.sprite = sprites[spriteName][0];
		}

		spriteTransform.eulerAngles = new Vector3(0f, mainCamera.transform.eulerAngles.y, 0f);
		
		if (timerActive) {
			stateTimer -= Time.deltaTime;
			if (stateTimer <= 0f) {
				ChangeState();
			}
		}

		if (MF_MISSILE) {

			if (!Physics.Raycast(transform.position, move, (speed * Time.deltaTime * 35f) + radius)) {
				transform.Translate(move * speed * Time.deltaTime * 35f, Space.World);
			} else {
				move = Vector3.zero;
				SetNextState(mobj.data["deathstate"]);
				PlaySound(deathSound);
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
		if (state.action != "NULL") {
			MethodInfo method = this.GetType().GetMethod(state.action);
			if (method != null) {
				method.Invoke(this, new object[]{});
			} else {
				Debug.LogWarning($"No codepointer: {state.action}");
			}
		}

		stateName = state.name;
	}

	void ChangeState() {
		state = wad.multigen.states[nextState];
		
		LoadState();
	}

	void SetState(string newState) {
		state = wad.multigen.states[newState];
		LoadState();
	}

	void SetNextState(string newState) {
		nextState = newState;
	}

	void LookForPlayers() {
		// GameObject player = GameObject.Find("Player");

		target = playerEntity;
		targetTransform = playerTransform;
		return;

		// float playerDistance = Vector3.Distance(transform.position, player.transform.position);

		// RaycastHit raycastinfo;
		// Ray checkRay = new Ray(transform.position, (player.transform.position - transform.position).normalized);

		// if (!Physics.Raycast(checkRay, out raycastinfo, playerDistance * 0.95f)) {
		// 	target = player;
		// }
	}

	bool CheckSight() {
		Vector3 heading = targetTransform.position - sightPosition;
		Vector3 dir = new Vector3(Mathf.Cos(direction * Mathf.Deg2Rad), 0f, Mathf.Sin(direction * Mathf.Deg2Rad));
		float dot = Vector3.Dot(heading.normalized, dir);
		float playerDistance = Vector3.Distance(sightPosition, targetTransform.position);

		// If player is in front of the object
		if (dot > 0) {
			RaycastHit raycastinfo;
			Ray checkRay = new Ray(sightPosition, targetTransform.position - sightPosition);

			if (!Physics.Raycast(checkRay, out raycastinfo, playerDistance, LayerMask.GetMask("Level"))) {
				return true;
			}
		}

		return false;
	}

	void NewChaseDir() {
		// TODO: actually recreate from p_enemy.c
		moveTime = Random.Range(0,16);
		move.x = 0f;
		move.y = 0f;

		if (CheckMeleeRange()) {
			NewFleeDir();
			return;
		}

		if (targetTransform.position.x - speed > transform.position.x) {
			move.x = 1f;
		} else if (targetTransform.position.x + speed< transform.position.x) {
			move.x = -1f;
		} 
		
		if (targetTransform.position.z + speed < transform.position.z) {
			move.z = -1f;
		} else if (targetTransform.position.z - speed > transform.position.z) {
			move.z = 1f;
		}

		direction = Mathf.Atan2(move.z, move.x) * Mathf.Rad2Deg;
	}

	void NewFleeDir() {
		move.x = 0f;
		move.y = 0f;

		if (targetTransform.position.x < transform.position.x) {
			move.x = 1f;
		} else if (targetTransform.position.x > transform.position.x) {
			move.x = -1f;
		} 
		
		if (targetTransform.position.z > transform.position.z) {
			move.z = -1f;
		} else if (targetTransform.position.z < transform.position.z) {
			move.z = 1f;
		}

		direction = Mathf.Atan2(move.z, move.x) * Mathf.Rad2Deg;
	}

	bool CheckMeleeRange() {
		if (target == null) return false;

		float distance = Vector3.Distance(sightPosition, targetTransform.position);

		if (distance >= 0.6875f + target.radius)
		{
			return false;
		}

		if (!CheckSight()) {
			return false;
		}

		return true;
	}

	bool CheckMissileRange() {
		if (!CheckSight()) {
			return false;
		}

		if (justHit) {
			justHit = false;
			return true;
		}

		if (reactionTime > 0f) {
			return false;
		}

		float distance = Vector3.Distance(sightPosition, targetTransform.position) - 1f;

		if (!IsValid(mobj.data["meleestate"])) {
			// no melee attack, so fire more
			distance -= 2f;
		}

		if (mobj.name == "MT_VILE") {
			if (distance > 14f) {
				return false;
			}
		}

		if (mobj.name == "MT_UNDEAD") {
			if (distance < 3f) {
				return false;
			}
			distance /= 2f;
		}

		if (mobj.name == "MT_CYBORG" || 
			mobj.name == "MT_SPIDER" ||
			mobj.name == "MT_SKULL") 
		{
			distance /= 2f;
		}

		if (distance > 3f) {
			distance = 3f;
		}

		if (mobj.name == "MT_CYBORG" && distance > 2.5f) {
			distance = 2.5f;
		}

		if ((Random.value * 4f) < distance) {
			return false;
		}

		return true;
	}

	void SpawnMissile(Vector3 target, string thingType)
	{
		MultigenObject mobj = wad.multigen.objects[thingType];

		LevelEntity thing = SpawnEntity(
			transform.position + Vector3.up * 0.75f * height,
			0f,
			mobj,
			wad
		);

		if (thing.seeSound != null) {
			thing.PlaySound(thing.seeSound, false);
		}

		thing.target = this;
		float direction = Mathf.Atan2(target.z - transform.position.z, target.x - transform.position.x) * Mathf.Rad2Deg;
		
		// TODO: fuzzy player angle variant
		
		thing.direction = direction;
		thing.move = (target - thing.transform.position).normalized; 
	}

	public static LevelEntity SpawnEntity(Vector3 position, float direction, MultigenObject mobj, WadFile wad)
	{
		GameObject newObj = new GameObject(mobj.name);
		newObj.transform.localPosition = position;


		LevelEntity ent = newObj.AddComponent<LevelEntity>();

		ent.spriteTransform = new GameObject("Sprite").transform;
		ent.spriteTransform.SetParent(newObj.transform, false);
		ent.spriteTransform.localScale= new Vector3(1.6f,1.76f,1.6f);
		ent.LoadMultigen(mobj, wad);
		ent.direction = direction;

		return ent;
	}

	void Move() {
		RaycastHit forwardHit;
		bool moveForward = !Physics.BoxCast(
			transform.position + boxCollider.center, 
			boxCollider.size,
			move,
			out forwardHit,
			Quaternion.identity,
			speed * move.magnitude
		);

		bool moveStepped = !Physics.BoxCast(
			transform.position + boxCollider.center + Vector3.up * stepHeight, 
			boxCollider.size,
			move,
			Quaternion.identity,
			speed * move.magnitude
		);

		bool noLedge = Physics.Raycast((transform.position + move * speed) + Vector3.up * stepHeight * 0.1f, Vector3.down, stepHeight * 1.1f);

		if (moveForward) {
			if (noLedge) {
				transform.Translate(move * speed, Space.World);
			}
		} else {
			if (moveStepped) {
				transform.Translate(move * speed, Space.World);
			} else {
				transform.Translate(move * forwardHit.distance, Space.World);
			}
		}

		if (!MF_MISSILE) {
			UpdateVerticalPosition();
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
		if (MF_SPECIAL) { // MF_SPECIAL identifies pickups
			if (collision.gameObject == player) {

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

	void PlaySound(AudioClip clip, bool fullVolume = false)
	{
		audioSource.Stop();
		audioSource.spatialBlend = fullVolume?0f:1f;
		audioSource.clip = clip;
		audioSource.Play();
	}

	///// CODEPOINTERS =====================================

	public void A_Look() {
		if (firstTick) {
			firstTick = false;
			return;
		}

		// TODO: Wake up via sound here

		LookForPlayers();

		if (CheckSight()) {
			if (seeSound != null) {
				bool fullVolume = mobj.name == "MT_CYBORG" || mobj.name == "MT_SPIDER";
				PlaySound(seeSound, fullVolume);
			} else if (seeSounds != null) {
				PlaySound(seeSounds[Random.Range(0, seeSounds.Length)]);
			}
			
			SetNextState(mobj.data["seestate"]);
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
				PlaySound(attackSound);
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
					PlaySound(activeSound);
				}
			} else {
				SetState(mobj.data["missilestate"]);
				justAttacked = true;
				return;
			}
		}
	}

	public void A_FaceTarget() {
		direction = Mathf.Atan2(targetTransform.position.z - transform.position.z, targetTransform.position.x - transform.position.x) * Mathf.Rad2Deg;
	}

	public void A_PosAttack() {
		A_FaceTarget();

		if (attackSound != null) {
			PlaySound(attackSound);
		}
		
		// TODO: Shoot a bullet
	}

	// public void A_SPosAttack() {
	// 	A_PosAttack();
	// }

	public void A_TroopAttack() {
		if (target == null) return;
		int damage;

		A_FaceTarget();

		if (CheckMeleeRange()) {
			DoomSound.PlaySoundAtPoint(wad, "DSCLAW", transform.position);
			damage = (Random.Range(0,256) % 8 + 1) * 3;
			// TODO: Damage target
			return;
		}

		// Spawn missile
		Vector3 targetPosition = targetTransform.position;
		// targetPosition.y += height * 0.75f;
		SpawnMissile(targetPosition, "MT_TROOPSHOT");
	}

	
}
