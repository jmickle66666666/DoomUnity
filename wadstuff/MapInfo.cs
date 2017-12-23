using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MapInfo {
	public string lump;
	public string name;
	public string music;
	public string sky;
}


[System.Serializable]
public class MapInfoLump {
	public MapInfo[] mapinfo; 

	public static Dictionary<string, MapInfo> Load(string data) {
		MapInfoLump minfol = JsonUtility.FromJson<MapInfoLump>(data);

		Dictionary<string, MapInfo> output = new Dictionary<string, MapInfo>();
		for (int i = 0; i < minfol.mapinfo.Length; i++) {
			output.Add(minfol.mapinfo[i].lump, minfol.mapinfo[i]);
		}
		return output;
	}
}