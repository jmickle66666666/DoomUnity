using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WadTools;

public class StatusBar : MonoBehaviour {

	WadFile wad;
	GameObject statusBarObj;
	GameObject weaponSlotsObj;
	GameObject healthNumObj;
	GameObject armorNumObj;
	GameObject faceObj;
	Material spriteMaterial;
	float aspectRatio;

	Dictionary<string, Sprite> faceSprites;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	public void Build(WadFile wad) {
		this.wad = wad;

		aspectRatio = (float)Screen.width/(float)Screen.height;

		SetupMaterial();
		BuildStatusBar();

		if (aspectRatio < 1.6f) transform.localScale = new Vector3(aspectRatio / 1.6f,aspectRatio / 1.6f,0f);
	}

	void SetupMaterial() {
		spriteMaterial = new Material(Shader.Find("Doom/Unlit Texture"));
		spriteMaterial.SetTexture("_Palette", new Palette(wad.GetLump("PLAYPAL")).GetLookupTexture());
		spriteMaterial.SetTexture("_Colormap", new Colormap(wad.GetLump("COLORMAP")).GetLookupTexture());
	}

	void BuildStatusBar() {
		Texture2D stbarTexture = new DoomGraphic(wad.GetLump("STBAR")).ToRenderMap(true);
		Sprite stbarSprite = Sprite.Create(stbarTexture, new Rect(0,0,(float)stbarTexture.width,(float)stbarTexture.height), new Vector2(0.5f, 0f));
		statusBarObj = new GameObject("STBAR");
		statusBarObj.transform.parent = transform;
		statusBarObj.layer = 9;
		SpriteRenderer sbsr = statusBarObj.AddComponent<SpriteRenderer>();
		sbsr.material = spriteMaterial;
		sbsr.sprite = stbarSprite;
		statusBarObj.transform.localPosition = new Vector3(0f, -1f, 0f);

		Texture2D starmsTexture = new DoomGraphic(wad.GetLump("STARMS")).ToRenderMap(true);
		Sprite starmsSprite = Sprite.Create(starmsTexture, new Rect(0,0,(float)starmsTexture.width,(float)starmsTexture.height), new Vector2(0.5f, 0f));
		weaponSlotsObj = new GameObject("STARMS");
		weaponSlotsObj.transform.parent = transform;
		weaponSlotsObj.layer = 9;
		SpriteRenderer sasr = weaponSlotsObj.AddComponent<SpriteRenderer>();
		sasr.material = spriteMaterial;
		sasr.sprite = starmsSprite;
		weaponSlotsObj.transform.localPosition = new Vector3(-0.36f, -1f, -0.1f);

		faceSprites = new Dictionary<string, Sprite>();
	}

	string GetFaceSprite(int healthState, int facing, bool pain, bool happy, bool angry) {
		string output = "STF";

		if (angry) {
			output += "KILL";
			output += healthState.ToString();
			return output;
		}

		if (happy) {
			output += "EVL";
			output += healthState.ToString();
			return output;
		}

		if (pain) {
			if (facing == 0) output += "TL";
			if (facing == 2) output += "TR";

			if (facing == 1) {
				output += "OUCH";
				output += healthState.ToString();
			} else {
				output += healthState.ToString();
				output += "0";
			}

			return output;
		}

		return output + "ST" + healthState.ToString() + facing.ToString();
	}
}
