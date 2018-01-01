using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using WadTools;

/*
Used to build a mesh from a level, and apply the correct textures and offsets etc.
*/

public class DoomMapBuilder {

	public List<string> textures;
	public MapData map;
	public  WadFile wad;
	private  GameObject levelObject;
	private  TextureTable textureTable; 

	public  float SCALE = 1f/64f;
	public  float PLAYER_HEIGHT = 56f;

	public  Dictionary<int, Sector> thingSectors;
	private  List<int> unclaimedThings;

	private Texture2D paletteLookup;
	private Texture2D colormapLookup;
	private Material doomMaterial;
	public static Material skyMaterial;

	private string skyName;

	private SectorTriangulation st;

	public  int GetIndexOfThing(int thingType) {
		for (int i = map.things.Count - 1; i >= 0; i--) {
			if (map.things[i].type == thingType) {
				return i;
			}
		}
		return -1;
	}

	public void SetMapInfo(MapInfo mapInfo)
	{
		if(mapInfo != null) skyName = mapInfo.sky;
		else skyName = "SKY1";
	}

	public void BuildMap(WadFile wad, string mapname) {
		this.wad = wad;
		textureTable = new TextureTable(wad.GetLump("TEXTURE1"));
		if (wad.Contains("TEXTURE2")) textureTable.Add(wad.GetLump("TEXTURE2"));
		paletteLookup = new Palette(wad.GetLump("PLAYPAL")).GetLookupTexture();
		colormapLookup = new Colormap(wad.GetLump("COLORMAP")).GetLookupTexture();
		doomMaterial = new Material(Shader.Find("Doom/Texture"));
		doomMaterial.SetTexture("_Palette", paletteLookup);
		doomMaterial.SetTexture("_Colormap", colormapLookup);

		skyMaterial = new Material(Shader.Find("Doom/Sky"));
		skyMaterial.SetTexture("_Palette", paletteLookup);
		skyMaterial.SetTexture("_RenderMap", GetTexture(skyName));

		st = new SectorTriangulation(map);

		map = new MapData(wad, mapname);
		levelObject = new GameObject(mapname);

		unclaimedThings = new List<int>();
		for (int i = 0; i < map.things.Count; i++) {
			unclaimedThings.Add(i);
		}
		thingSectors = new Dictionary<int, Sector>();

		for (int i = 0; i < map.linedefs.Count; i++) {
			//Debug.Log(i);
			BuildLine(i);
		}


		for (int i = 0; i < map.sectors.Count; i++) {
			//Debug.Log(i);
			BuildSector(i);	
		}

		// BuildLine(335);

		// BuildSector(0);

		// List of all things that haven't been placed in a sector
		if (unclaimedThings.Count > 0) {
			for (int i = 0; i < unclaimedThings.Count; i++) {
				Debug.LogWarning("Unclaimed Thing: "+unclaimedThings[i]);
			}
		}

		levelObject.transform.localScale = new Vector3(SCALE,SCALE * 1.2f,SCALE);
	}

	public int TestMap(WadFile wad, string mapname) {
		map = new MapData(wad, mapname);
		st = new SectorTriangulation(map);
		int failedSectors = 0;
		for (int i = 0; i < map.sectors.Count; i++) {
			List<SectorPolygon> polygons = null;
			try {
				polygons = st.Triangulate(i);
			} catch  {
				//Debug.Log("Exception found in "+mapname+" sector "+i);
			}
			if (polygons == null) failedSectors += 1;
		}
		return failedSectors;
	}

	public void BuildMap(string wadpath, string mapname) {
		wad = new WadFile(wadpath);
		BuildMap(wad, mapname);
	}

	public void BuildMap(string mapname) {
		BuildMap(wad, mapname);
	}

	 DoomTexture GetInfo(string name) {
		return textureTable.Get(name.ToUpper());
	}

	 void BuildSector(int index) {
	 	st = new SectorTriangulation(map);
		List<SectorPolygon> polygons = st.Triangulate(index);

		if (polygons == null) return;

		for (int i = 0; i < polygons.Count; i++) {
			for (int t = 0; t < unclaimedThings.Count; t++) {
				if (polygons[i].ThingInside(map.things[unclaimedThings[t]])) {
					thingSectors.Add(unclaimedThings[t], map.sectors[index]);
					unclaimedThings.RemoveAt(t);
					t-=1;
				}
			}
		}

		int floorHeight = map.sectors[index].floorHeight;
		int ceilingHeight = map.sectors[index].ceilingHeight;

		float brightness = map.sectors[index].lightLevel /256f;
		

		for (int i = 0; i < polygons.Count; i++) {

			Mesh mesh = new Mesh();
			Vector3[] vertices = polygons[i].PointsToVector3(floorHeight).ToArray();
			int[] tris = polygons[i].triangles.ToArray();
			Vector2[] uvs = polygons[i].points.ToArray();

			for (int j = 0; j < uvs.Length; j++) {
				uvs[j] /= 64f;
			}

			mesh.vertices = vertices;
			mesh.triangles = tris;
			mesh.uv = uvs;

			GameObject newObj = new GameObject();
			MeshRenderer mr = newObj.AddComponent<MeshRenderer>();

			newObj.AddComponent<MeshCollider>().sharedMesh = mesh;
			if (map.sectors[index].floorTexture != "F_SKY1") {
				mr.material = doomMaterial;
				mr.material.SetTexture("_MainTex", GetFlat(map.sectors[index].floorTexture));
				mr.material.SetFloat("_Brightness", brightness);
			} else {
				mr.material = skyMaterial;
			}
			newObj.AddComponent<MeshFilter>().mesh = mesh;
			newObj.transform.parent = levelObject.transform;

			mesh = new Mesh();
			Array.Reverse(tris);
			for (int j = 0; j < vertices.Length; j++) {
				vertices[j].y = ceilingHeight;
			}
			mesh.vertices = vertices;
			mesh.triangles = tris;
			mesh.uv = uvs;

			newObj = new GameObject();
			newObj.AddComponent<MeshCollider>().sharedMesh = mesh;
			mr = newObj.AddComponent<MeshRenderer>();
			if (map.sectors[index].ceilingTexture != "F_SKY1") {
				mr.material = doomMaterial;
				mr.material.SetTexture("_MainTex", GetFlat(map.sectors[index].ceilingTexture));
				mr.material.SetFloat("_Brightness", brightness);
			} else {
				mr.material = skyMaterial;
			}
			newObj.AddComponent<MeshFilter>().mesh = mesh;
			newObj.transform.parent = levelObject.transform;

		}

		
	}

	 void BuildLine(int index) {
		Linedef line = map.linedefs[index];
		Sidedef frontSide = map.sidedefs[line.front];
		Sidedef backSide = line.back!=-1?map.sidedefs[line.back]:null;
		Sector frontSector = map.sectors[frontSide.sector];
		Sector backSector = backSide!=null?map.sectors[backSide.sector]:null;

		int x1 = map.vertices[line.start].x;
		int y1 = map.vertices[line.start].y;
		int x2 = map.vertices[line.end].x;
		int y2 = map.vertices[line.end].y;

		Vector2 frontOffset = new Vector2(frontSide.xOffset, frontSide.yOffset);
		Vector2 backOffset = new Vector2(frontSide.xOffset, frontSide.yOffset);

		bool upperSky = false;
		bool lowerSky = false;

		if (backSide != null) {
			backOffset = new Vector2(backSide.xOffset, backSide.yOffset);
			upperSky = (frontSector.ceilingTexture == "F_SKY1" && backSector.ceilingTexture == "F_SKY1");
			lowerSky = (frontSector.floorTexture == "F_SKY1" && backSector.floorTexture == "F_SKY1");
		}

		float frontBrightness = map.sectors[frontSide.sector].lightLevel /256f;
		float backBrightness = 1f;
		if (backSide != null) {
			backBrightness = map.sectors[backSide.sector].lightLevel /256f;
		}
		

		if (backSector == null) {
			int z1 = frontSector.floorHeight;
			int z2 = frontSector.ceilingHeight;

			if (line.lowerUnpegged) frontOffset.y += GetInfo(frontSide.mid).height - (z2-z1);
			// Just Midtex
			BuildQuad(new Vector3(x1, z1, y1), new Vector3(x2,z2,y2), frontSide.mid, frontOffset, frontBrightness);
		} else {
			// Lower texture
			float midBottom;
			Vector2 offset = new Vector2();
			if (frontSector.floorHeight < backSector.floorHeight) {
				// Front Side
				offset.Set(frontOffset.x, frontOffset.y);
				if (frontSide.lower != "-" || lowerSky) {
					// PEGGED: top of texture matches highest floor height
					// UNPEGGED: top of texture matches hightest CEILING
					if (line.lowerUnpegged && !lowerSky) { 
						// difference between top of floor and top of heighest ceiling
						// ceiling height - floor height
						float diff = Mathf.Max(frontSector.ceilingHeight, backSector.ceilingHeight) - backSector.floorHeight;
						offset.y -= GetInfo(frontSide.lower).height - diff;
					}
					BuildQuad(new Vector3(x1,frontSector.floorHeight, y1), new Vector3(x2, backSector.floorHeight, y2), frontSide.lower, offset, frontBrightness,lowerSky);
				}
				midBottom = backSector.floorHeight;
			} else {
				// Back Side
				offset.Set(backOffset.x, backOffset.y);
				if (backSide.lower != "-" || lowerSky) {
					// PEGGED: top of texture matches highest floor height
					// UNPEGGED: top of texture matches hightest CEILING
					if (line.lowerUnpegged && !lowerSky) { 
						float diff = Mathf.Max(frontSector.ceilingHeight, backSector.ceilingHeight) - frontSector.floorHeight;
						offset.y -= GetInfo(backSide.lower).height - diff;
					}
					BuildQuad(new Vector3(x2, backSector.floorHeight, y2), new Vector3(x1, frontSector.floorHeight, y1), backSide.lower, offset, backBrightness,lowerSky);
				}
				midBottom = frontSector.floorHeight;
			}

			// Upper texture
			float midTop;
			if (frontSector.ceilingHeight < backSector.ceilingHeight) {
				// Back Side
				offset.Set(backOffset.x, backOffset.y);
				if (backSide.upper != "-" || upperSky) { 
					// PEGGED: bottom of texture matches lower ceiling height
					// UNPEGGED: top of texture is the upper ceiling height
					if (!line.upperUnpegged && !upperSky) {
						offset.y += GetInfo(backSide.upper).height - (backSector.ceilingHeight-frontSector.ceilingHeight);
					}

					BuildQuad(new Vector3(x2, frontSector.ceilingHeight, y2), new Vector3(x1, backSector.ceilingHeight, y1), backSide.upper, offset, backBrightness,upperSky);
				}
				midTop = frontSector.ceilingHeight;
			} else {
				// Front Side
				offset.Set(frontOffset.x, frontOffset.y);
				if (frontSide.upper != "-" || upperSky) {
					// PEGGED: bottom of texture matches lower ceiling height
					// UNPEGGED: top of texture is the upper ceiling height
					if (!line.upperUnpegged && !upperSky) {
						offset.y += GetInfo(frontSide.upper).height - (frontSector.ceilingHeight-backSector.ceilingHeight);
					}
					BuildQuad(new Vector3(x1, backSector.ceilingHeight, y1), new Vector3(x2, frontSector.ceilingHeight, y2), frontSide.upper, offset, frontBrightness,upperSky);
				}
				midTop = backSector.ceilingHeight;
			}

			// Mid textures
			if (frontSide.mid != "-") {
				offset.Set(frontOffset.x, frontOffset.y);
				float fmidTop;
				float fmidBottom;
				if (line.lowerUnpegged) {
					fmidTop = (midBottom + GetInfo(frontSide.mid).height) + frontOffset.y;
					fmidBottom = Mathf.Max(midBottom, midBottom + offset.y);
				} else {
					fmidTop = Mathf.Min(midTop, offset.y + midTop);
					fmidBottom = Mathf.Max(midBottom, midTop + offset.y - GetInfo(frontSide.mid).height);
				}

				BuildQuad(new Vector3(x1, fmidBottom, y1), new Vector3(x2,fmidTop,y2), frontSide.mid, offset, frontBrightness);
			}
			if (backSide.mid != "-") {
				offset.Set(backOffset.x, backOffset.y);
				float bmidTop;
				float bmidBottom;
				if (line.lowerUnpegged) {
					bmidTop = (midBottom + GetInfo(backSide.mid).height) + backOffset.y;
					bmidBottom = Mathf.Max(midBottom, midBottom + offset.y);
				} else {
					bmidTop = Mathf.Min(midTop, offset.y + midTop);
					bmidBottom = Mathf.Max(midBottom, midTop + offset.y - GetInfo(backSide.mid).height);
				}
				BuildQuad(new Vector3(x2, bmidBottom, y2), new Vector3(x1,bmidTop,y1), backSide.mid, offset, backBrightness);
			}
		}

		GetTextures(index);
	}

	private  Dictionary<string, Material> materialCache;

	 void BuildQuad(Vector3 v1, Vector3 v2, string texture, Vector2 uvOffset, float light, bool sky = false) {
	 	// This is where we discard parts of a line that have no height or texture

	 	if (v1.y == v2.y) {
	 		return;
	 	}

	 	// It doesn't matter that it doesn't have a texture if it is flagged as a sky quad.
		if (texture == "-" && !sky) {
			return;
		}

		if (v1.x == v2.x) {
			light += 1f/16f;
		}

		if (v1.z == v2.z) {
			light -= 1f/16f;
		}

		Mesh mesh = new Mesh();
		Vector3[] vertices = new Vector3[4];
		vertices[0] = new Vector3(v1.x, v1.y, v1.z);
		vertices[1] = new Vector3(v2.x, v1.y, v2.z);
		vertices[2] = new Vector3(v1.x, v2.y, v1.z);
		vertices[3] = new Vector3(v2.x, v2.y, v2.z);
		int[] tri = new int[6] {0, 2, 1, 2, 3, 1};

		Texture2D tex;
		if (!sky) {
			tex = GetTexture(texture);
			uvOffset.x /= tex.width;
			uvOffset.y /= tex.height;
		} else {
			tex = GetTexture(skyName);
		}

		float length = Vector3.Distance(v1, new Vector3(v2.x,v1.y,v2.z));
		float height = (v2.y - v1.y);

		Vector2[] uvs = new Vector2[4] {
			new Vector2(0f, height / tex.height) + uvOffset,
			new Vector2(length / tex.width , height / tex.height) + uvOffset,
			new Vector2(0f, 0f) + uvOffset,
			new Vector2(length / tex.width, 0f) + uvOffset
		};

		mesh.vertices = vertices;
		mesh.triangles = tri;
		mesh.uv = uvs;
		GameObject newObj = new GameObject();
		MeshRenderer mr = newObj.AddComponent<MeshRenderer>();
		newObj.AddComponent<MeshCollider>().sharedMesh = mesh;
		if (sky) {
			mr.material = skyMaterial;
		} else {
			mr.material = doomMaterial;
			mr.material.SetTexture("_MainTex", tex);
			mr.material.SetFloat("_Brightness", light);
		}
		newObj.AddComponent<MeshFilter>().mesh = mesh;
		newObj.transform.parent = levelObject.transform;
	}

	private  Dictionary<string, Texture2D> flatCache;

	 Texture2D GetFlat(string name) {
		name = name.ToUpper();

		if (flatCache == null) flatCache = new Dictionary<string, Texture2D>();

		if (flatCache.ContainsKey(name)) return flatCache[name];

		DoomFlat flat = new DoomFlat(wad.GetLump(name));
		Texture2D output = flat.ToRenderMap();
		flatCache.Add(name, output);
		return output;
	}


	 Texture2D GetTexture(string name) {
		return DoomGraphic.BuildTexture(name, wad, textureTable);
	}

	 void GetTextures(int lineIndex) {
		Linedef line = map.linedefs[lineIndex];
		Sidedef front = map.sidedefs[line.front];

		if (textures == null) textures = new List<string>();

		if (!textures.Contains(front.lower)) textures.Add(front.lower);
		if (!textures.Contains(front.mid)) textures.Add(front.mid);
		if (!textures.Contains(front.upper)) textures.Add(front.upper);

		if (line.back != -1) {
			Sidedef back = map.sidedefs[line.back];
			if (!textures.Contains(back.lower)) textures.Add(back.lower);
			if (!textures.Contains(back.mid)) textures.Add(back.mid);
			if (!textures.Contains(back.upper)) textures.Add(back.upper);
		}
	}

}
