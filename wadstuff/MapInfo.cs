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
	public string baseMapinfo;

	public static Dictionary<string, MapInfo> Load(string data, WadFile wad) {
		MapInfoLump minfol = JsonUtility.FromJson<MapInfoLump>(data);

		if (minfol.baseMapinfo == null) {
			return ReadData(minfol);
		} else {
			Dictionary<string, MapInfo> baseMapinfo = ReadData(JsonUtility.FromJson<MapInfoLump>(wad.GetLumpAsText(minfol.baseMapinfo)));
			Dictionary<string, MapInfo> repMapinfo = ReadData(minfol);
			foreach (KeyValuePair<string, MapInfo> entry in repMapinfo) {
				if (baseMapinfo.ContainsKey(entry.Key)) {
					if (entry.Value.name != null) baseMapinfo[entry.Key].name = entry.Value.name;
					if (entry.Value.music != null) baseMapinfo[entry.Key].music = entry.Value.music;
					if (entry.Value.sky != null) baseMapinfo[entry.Key].sky = entry.Value.sky;
				} else {
					baseMapinfo.Add(entry.Key, entry.Value);
				}
			}
			return baseMapinfo;
		}
	}

	public static Dictionary<string, MapInfo> ReadData(MapInfoLump minfol) {
		Dictionary<string, MapInfo> output = new Dictionary<string, MapInfo>();
		for (int i = 0; i < minfol.mapinfo.Length; i++) {
			output.Add(minfol.mapinfo[i].lump, minfol.mapinfo[i]);
		}
		return output;
	}
}