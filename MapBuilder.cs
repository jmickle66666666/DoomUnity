using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
//using Linedef;

public class MapBuilder : MonoBehaviour {

	public List<string> textures;
	private MapData map;
	private WadFile wad;
	private GameObject levelObject;
	private TextureTable textureTable; 

	// Use this for initialization
	void Start () {
		string path = "DOOM2.WAD";
		string mapname = "MAP01";
		wad = new WadFile(path);
		map = new MapData(wad, mapname);
		textureTable = new TextureTable(wad.GetLump("TEXTURE1"));

		levelObject = new GameObject(mapname);

		for (int i = 0; i < map.linedefs.Count; i++) {
			BuildLine(i);
		}

		for (int i = 0; i < map.sectors.Count; i++) {
			Debug.Log(i);
			BuildSector(i);	
		}

		// BuildSector(18);

		float scale = 1f/64f;
		levelObject.transform.localScale = new Vector3(scale,scale,scale);

		GameObject player = new GameObject("Player");
		Thing playerdat = map.things[0];
		player.transform.position = new Vector3(playerdat.x * (1f/64f), (56f/64f), playerdat.y * (1f/64f));
		player.transform.localEulerAngles = new Vector3(0, playerdat.angle, 0);
	}

	DoomTexture GetInfo(string name) {
		return textureTable.Get(name.ToUpper());
	}

	void BuildSector(int index) {
		SectorTriangulation st = new SectorTriangulation(map);
		List<SectorPolygon> polygons = st.Triangulate(index);

		int floorHeight = map.sectors[index].floorHeight;
		int ceilingHeight = map.sectors[index].ceilingHeight;

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
			Material mat = new Material(Shader.Find("Unlit/Texture"));
			mat.SetTexture("_MainTex", GetFlat(map.sectors[index].floorTexture));
			newObj.AddComponent<MeshRenderer>().material = mat;
			newObj.AddComponent<MeshFilter>().mesh = mesh;
			newObj.transform.parent = levelObject.transform;

			mesh = new Mesh();
			Array.Reverse(tris);
			//Array.Reverse(uvs);
			for (int j = 0; j < vertices.Length; j++) {
				vertices[j].y = ceilingHeight;
			}
			mesh.vertices = vertices;
			mesh.triangles = tris;
			mesh.uv = uvs;

			newObj = new GameObject();
			mat = new Material(Shader.Find("Unlit/Texture"));
			mat.SetTexture("_MainTex", GetFlat(map.sectors[index].ceilingTexture));
			newObj.AddComponent<MeshRenderer>().material = mat;
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

		if (backSector == null) {
			int z1 = frontSector.floorHeight;
			int z2 = frontSector.ceilingHeight;

			if (line.lowerUnpegged) frontOffset.y += GetInfo(frontSide.mid).height - (z2-z1);

			BuildQuad(new Vector3(x1, z1, y1), new Vector3(x2,z2,y2), frontSide.mid, frontOffset);
		} else {

			int midBottom;
			if (frontSector.floorHeight < backSector.floorHeight) {
				if (line.lowerUnpegged) frontOffset.y += GetInfo(frontSide.upper).height + (backSector.ceilingHeight-frontSector.ceilingHeight);
				BuildQuad(new Vector3(x1, frontSector.floorHeight, y1), new Vector3(x2, backSector.floorHeight, y2), frontSide.lower, frontOffset);
				midBottom = backSector.floorHeight;
			} else {
				BuildQuad(new Vector3(x2, backSector.floorHeight, y2), new Vector3(x1, frontSector.floorHeight, y1), backSide.lower, backOffset);
				midBottom = frontSector.floorHeight;
			}

			int midTop;
			if (frontSector.ceilingHeight < backSector.ceilingHeight) {
				if (backSide.upper != "-") {
					if (!line.upperUnpegged) backOffset.y += GetInfo(backSide.upper).height + (frontSector.ceilingHeight-backSector.ceilingHeight);
					BuildQuad(new Vector3(x1, frontSector.ceilingHeight, y1), new Vector3(x2, backSector.ceilingHeight, y2), backSide.upper, backOffset);
				}
				midTop = frontSector.ceilingHeight;
			} else {
				if (frontSide.upper != "-") {
					if (!line.upperUnpegged) frontOffset.y += GetInfo(frontSide.upper).height + (backSector.ceilingHeight-frontSector.ceilingHeight);
					BuildQuad(new Vector3(x1, backSector.ceilingHeight, y1), new Vector3(x2, frontSector.ceilingHeight, y2), frontSide.upper, frontOffset);
				}
				midTop = backSector.ceilingHeight;
			}

			BuildQuad(new Vector3(x1, midBottom, y1), new Vector3(x2,midTop,y2), frontSide.mid, frontOffset);
			BuildQuad(new Vector3(x2, midBottom, y2), new Vector3(x1,midTop,y1), backSide.mid, backOffset);
		}

		GetTextures(index);
	}

	private Dictionary<string, Material> materialCache = new Dictionary<string, Material>();

	void BuildQuad(Vector3 v1, Vector3 v2, string texture, Vector2 uvOffset = new Vector2()) {

		if (texture == "-") return;

		Mesh mesh = new Mesh();
		Vector3[] vertices = new Vector3[4];
		vertices[0] = new Vector3(v1.x, v1.y, v1.z);
		vertices[1] = new Vector3(v2.x, v1.y, v2.z);
		vertices[2] = new Vector3(v1.x, v2.y, v1.z);
		vertices[3] = new Vector3(v2.x, v2.y, v2.z);
		int[] tri = new int[6] {0, 2, 1, 2, 3, 1};

		Texture2D tex = GetTexture(texture);

		uvOffset.x /= tex.width;
		uvOffset.y /= tex.height;

		float length = Vector3.Distance(v1, new Vector3(v2.x,v1.y,v2.z));
		float height = (v2.y - v1.y);

		Vector2[] uvs = new Vector2[4] {
			new Vector2(0f, height / tex.height) + uvOffset,
			new Vector2(length / tex.width , height / tex.height) + uvOffset,
			new Vector2(0f, 0f) + uvOffset,
			new Vector2(length / tex.width, 0f) + uvOffset
		};

		Material material;

		if (materialCache.ContainsKey(texture)) {
			material = materialCache[texture];
		} else {
			material = new Material(Shader.Find("Unlit/Transparent Cutout"));
			material.SetTexture("_MainTex", tex);
			material.SetFloat("_Cutoff", 1f);

			materialCache.Add(texture, material);
		}

		mesh.vertices = vertices;
		mesh.triangles = tri;
		mesh.uv = uvs;
		GameObject newObj = new GameObject();
		newObj.AddComponent<MeshRenderer>().material = material;
		newObj.AddComponent<MeshFilter>().mesh = mesh;
		newObj.transform.parent = levelObject.transform;
	}

	private Dictionary<string, Texture2D> flatCache = new Dictionary<string, Texture2D>();

	Texture2D GetFlat(string name) {
		name = name.ToUpper();

		if (flatCache == null) flatCache = new Dictionary<string, Texture2D>();

		if (flatCache.ContainsKey(name)) return flatCache[name];

		DoomFlat flat = new DoomFlat(wad.GetLump(name));
		Texture2D output = flat.ToTexture2D(new Palette(wad.GetLump("PLAYPAL")));
		flatCache.Add(name, output);
		return output;
	}


	Texture2D GetTexture(string name) {
		return DoomGraphic.BuildTexture(name, wad);
	}

	void GetTextures(int lineIndex) {
		Linedef line = map.linedefs[lineIndex];
		Sidedef front = map.sidedefs[line.front];

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
