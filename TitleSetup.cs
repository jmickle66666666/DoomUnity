using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This is attached to a quad, placed in front of a camera on a new layer.
// It just shows the TITLEPIC graphic on the quad, used for main menu backgrounds before a level is loaded.

public class TitleSetup : MonoBehaviour {

	private MeshRenderer mr;

	// Use this for initialization
	void Start () {

	}

	public void Build(WadFile wad) {
		mr = gameObject.GetComponent<MeshRenderer>();
		mr.material = new Material(Shader.Find("Doom/Unlit Texture"));
		mr.material.SetTexture("_Palette", new Palette(wad.GetLump("PLAYPAL")).GetLookupTexture());
		mr.material.SetTexture("_Colormap", new Colormap(wad.GetLump("COLORMAP")).GetLookupTexture());
		mr.material.SetTexture("_MainTex", DoomGraphic.BuildPatch("TITLEPIC", wad));
	}

	public void Darken(bool dark) {
		mr.material.SetFloat("_Brightness", dark?0.5f:1f);
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
