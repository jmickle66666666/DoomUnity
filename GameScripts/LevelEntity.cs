using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WadTools;

[RequireComponent(typeof(SpriteRenderer))]
public class LevelEntity : MonoBehaviour {

	SpriteRenderer spriteRenderer;
	Material spriteMaterial;
	MultigenState state;
	MultigenParser multigen;
	WadFile wad;
	public float direction;
	static float tick = (1.0f / 35.0f);
	string spriteName {
		get {
			return state.spriteName + state.spriteFrame;
		}
	}

	float stateTimer;

	Dictionary<string, Sprite[]> sprites;

	public void LoadMultigen(MultigenParser multigen, MultigenObject mobj, WadFile wad) {
		this.multigen = multigen;
		sprites = new Dictionary<string, Sprite[]>();
		this.wad = wad;

		spriteMaterial = new Material(Shader.Find("Doom/Texture"));
		Texture2D paletteLookup = new Palette(wad.GetLump("PLAYPAL")).GetLookupTexture();
		Texture2D colormapLookup = new Colormap(wad.GetLump("COLORMAP")).GetLookupTexture();
		spriteMaterial.SetTexture("_Palette", paletteLookup);
		spriteMaterial.SetTexture("_Colormap", colormapLookup);

		spriteRenderer = GetComponent<SpriteRenderer>();
		spriteRenderer.material = spriteMaterial;

		state = multigen.states[mobj.data["spawnstate"]];
		LoadState();
	}

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {

		direction += Time.deltaTime * 100.0f;

		if (sprites[spriteName].Length > 1) {
			float cameraAngle = Mathf.Atan2(Camera.main.transform.position.y - transform.position.y, Camera.main.transform.position.x - transform.position.x) * Mathf.Rad2Deg;
			cameraAngle = 0f;

			int angle = Mathf.FloorToInt(((cameraAngle + direction)+360.0f) / 45.0f) % 8;
			spriteRenderer.sprite = sprites[spriteName][angle];
		} else {
			spriteRenderer.sprite = sprites[spriteName][0];
		}

		transform.localEulerAngles = new Vector3(0f, Camera.main.transform.eulerAngles.y, 0f);
		
		stateTimer -= Time.deltaTime;
		if (stateTimer <= 0f) {
			ChangeState();
		}
	}
	
	void LoadState() {
		if (!sprites.ContainsKey(spriteName)) {
			sprites.Add(spriteName, state.GetSprite(wad));
		}

		if (state.name == "S_NULL") {
			GameObject.Destroy(gameObject);
		}

		stateTimer += float.Parse(state.duration) * tick;
	}

	void ChangeState() {
		state = multigen.states[state.nextState];
		
		LoadState();
	}
}
