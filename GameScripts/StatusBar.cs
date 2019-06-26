using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WadTools;

public class StatusBar : MonoBehaviour {

	WadFile wad;
	Material spriteMaterial;
	float aspectRatio;

	GameObject redKey;
	GameObject blueKey;
	GameObject yellowKey;

	// Use this for initialization
	void Start () {
		
	}
	
	void Update () {
		if (redKey.activeSelf != GameSetup.main.playerInventory.redKeyCard) redKey.SetActive(GameSetup.main.playerInventory.redKeyCard);
		if (blueKey.activeSelf != GameSetup.main.playerInventory.blueKeyCard) blueKey.SetActive(GameSetup.main.playerInventory.blueKeyCard);
		if (yellowKey.activeSelf != GameSetup.main.playerInventory.yellowKeyCard) yellowKey.SetActive(GameSetup.main.playerInventory.yellowKeyCard);
	}

	public void Build(WadFile wad) {
		this.wad = wad;

		aspectRatio = (float)Screen.width/(float)Screen.height;

		SetupMaterial();
		BuildStatusBar();

		transform.localScale = new Vector3(aspectRatio / 1.6f,aspectRatio / 1.6f,1f);
	}

	void SetupMaterial() {
		spriteMaterial = new Material(Shader.Find("Doom/Unlit Texture"));
		spriteMaterial.SetTexture("_Palette", new Palette(wad.GetLump("PLAYPAL")).GetLookupTexture());
		spriteMaterial.SetTexture("_Colormap", new Colormap(wad.GetLump("COLORMAP")).GetLookupTexture());
	}

	void BuildStatusBar() {
		BuildSprite("STBAR", 0, 0);
		BuildSprite("STARMS", 104, 0);
		blueKey = BuildSprite("STKEYS0", 239, 3);
		yellowKey = BuildSprite("STKEYS1", 239, 13);
		redKey = BuildSprite("STKEYS2", 239, 23);
	}

	GameObject BuildSprite(string graphic, float x, float y)
	{
		float offset = 1.6f / aspectRatio;
		Vector3 position = new Vector3(
			((x-160f)/160f) * 1.6f,
			(y/100f)-offset,
			graphic=="STBAR"?0f:-0.1f
		);
		Texture2D texture = new DoomGraphic(wad.GetLump(graphic)).ToRenderMap(true);
		Sprite sprite = Sprite.Create(texture, new Rect(0,0,(float)texture.width,(float)texture.height), new Vector2(0f, 0f));
		var newObject = new GameObject(graphic);
		newObject.transform.parent = transform;
		newObject.layer = LayerMask.NameToLayer("HUD");
		SpriteRenderer sbsr = newObject.AddComponent<SpriteRenderer>();
		sbsr.material = spriteMaterial;
		sbsr.sprite = sprite;
		newObject.transform.localPosition = position;
		return newObject;
	}
}
