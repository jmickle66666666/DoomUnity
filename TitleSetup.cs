using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This is attached to a quad, placed in front of a camera on a new layer.
// It just shows the TITLEPIC graphic on the quad, used for main menu backgrounds before a level is loaded.

public class TitleSetup : MonoBehaviour {

	private MeshRenderer mr;

	// Use this for initialization
	void Start () {
		mr = gameObject.AddComponent<MeshRenderer>();
		MeshFilter mf = gameObject.AddComponent<MeshFilter>();
		gameObject.AddComponent<AudioListener>();
		Mesh mesh = new Mesh();
		Vector2[] uvs = new Vector2[4] {
			new Vector2(0f, 0f),
			new Vector2(1f, 0f),
			new Vector2(0f, 1f),
			new Vector2(1f, 1f)
		};
		Vector3[] vertices = new Vector3[4] {
			new Vector3(-0.5f, -0.5f, 0f),
			new Vector3(0.5f, -0.5f, 0f),
			new Vector3(-0.5f, 0.5f, 0f),
			new Vector3(0.5f, 0.5f, 0f)
		};
		int[] triangles = new int[6] {0,2,1,2,3,1};
		
		mesh.vertices = vertices;
		mesh.triangles = triangles;
		mesh.uv = uvs;
		mf.mesh = mesh;
	}

	public void Build(WadFile wad) {
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
