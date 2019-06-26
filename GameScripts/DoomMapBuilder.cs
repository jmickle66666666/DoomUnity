using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using WadTools;
using System.Text;

/*
Used to build a mesh from a level, and apply the correct textures and offsets etc.
*/

public class DoomMapBuilder {

	public MapData map;
	WadFile wad;
	MapInfo mapInfo;
	Transform container;
	Transform thingContainer;
	NodeTriangulation nodeTri;
	public DoomPlayer playerControl;
	DoomMeshGenerator meshGenerator;
	public GameObject level;
	GameObject player;

	public static Vector3 SCALE = new Vector3(0.015625f,0.01875f,0.015625f);  // 1/64

	public DoomMapBuilder(WadFile wad, MapData map, MapInfo mapInfo = null)
	{
		this.map = map;
		this.wad = wad;
		this.mapInfo = mapInfo;
		this.nodeTri = new NodeTriangulation(map);
		container = new GameObject("MAP").transform;
		Vector3 scale = SCALE;
        container.localScale = scale;
		thingContainer = new GameObject("Things").transform;
	}

	public int GetIndexOfThing(int thingType) {
		for (int i = map.things.Length - 1; i >= 0; i--) {
			if (map.things[i].type == thingType) {
				return i;
			}
		}
		return -1;
	}

	public void BuildMap() {
		for (int i = 0; i < container.childCount; i++) {
            GameObject.Destroy(container.GetChild(i).gameObject);
        }

        nodeTri.TraverseNodes();
		meshGenerator = new DoomMeshGenerator(wad, map, nodeTri);
		level = meshGenerator.BuildMesh();
        level.transform.SetParent(container, false); 
		MoveToLayer(level.transform, LayerMask.NameToLayer("Level"));
	}

	void MoveToLayer(Transform root, int layer) {
		if (root.gameObject.layer != LayerMask.NameToLayer("Trigger")) {
			root.gameObject.layer = layer;
			foreach(Transform child in root)
				MoveToLayer(child, layer);
		}
	}

	public void BuildPlayer(GameObject prefab)
	{
		player = GameObject.Instantiate(prefab);
		int playerIndex = 0;

		for (int i = map.things.Length - 1; i >= 0; i--) {
			if (map.things[i].type == 1) {
				playerIndex = i;
			}
		}
		Thing playerThing = map.things[playerIndex];

		player.transform.localPosition = ThingSpawnPosition(playerThing, true); 

		player.transform.localEulerAngles = new Vector3(
			0f,
			90f - playerThing.angle,
			0f
		);

		playerControl = player.GetComponent<DoomPlayer>();
		playerControl.doomMesh = meshGenerator;
		Debug.Log(playerControl);

		LevelEntity.playerEntity = playerControl.levelEntity;
		LevelEntity.playerTransform = playerControl.camera.transform;
		LevelEntity.player = player;
		LevelEntity.mainCamera = playerControl.camera;

		GameSetup.main.player = player;
	}

	Vector3 ThingSpawnPosition(Thing thing, bool scaled)
	{
		Vector3 position = new Vector3(
			thing.x * (scaled?SCALE.x:1f),
			nodeTri.SectorAtPosition(new Vector2(thing.x, thing.y)).floorHeight * (scaled?SCALE.y:1f),
			thing.y * (scaled?SCALE.z:1f)
		);
		return position;
	}

	public void BuildLevelEntities(bool spawnMonsters) {
		for (int i = 0; i < map.things.Length; i++) {
			if (!map.things[i].multiplayer) {
				MultigenObject mobj = wad.multigen.GetObjectByDoomedNum(map.things[i].type);
				if (mobj != null) {
					LevelEntity newObject = LevelEntity.SpawnEntity(
						ThingSpawnPosition(map.things[i], true),
						(float) map.things[i].angle,
						mobj,
						wad
					);

					if (!spawnMonsters && newObject.MF_COUNTKILL) {
						GameObject.Destroy(newObject.gameObject);
					} else {
						newObject.transform.parent = thingContainer;
					}

				}
			}
		}
	}

	public void Destroy()
	{
		GameObject.Destroy(container.gameObject);
		GameObject.Destroy(thingContainer.gameObject);
		GameObject.Destroy(player);
	}

}
