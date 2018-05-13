using System.Collections;
using System.Collections.Generic;
using System;
using System.Text;

/*
Base class for map data.
*/

namespace WadTools {

	public enum MapFormat {
		Doom,
		Hexen,
		UDMF
	}

	public class Vertex {
		public int x;
		public int y;
	}

	public class Sidedef {
		public int xOffset;
		public int yOffset;
		public string upper;
		public string lower;
		public string mid;
		public int sector;
	}

	public class Linedef {
		public int start;
		public int end;
		public int flags;
		public int special;
		public int tag;
		public int front;
		public int back;

		public bool impassable;
		public bool blockMonster;
		public bool twoSided;
		public bool upperUnpegged;
		public bool lowerUnpegged;
		public bool secret;
		public bool blockSound;
		public bool alwaysShow;
		public bool dontDraw;
	}

	public class Sector {
		public int floorHeight;
		public int ceilingHeight;
		public string floorTexture;
		public string ceilingTexture;
		public int lightLevel;
		public int type;
		public int tag;
	}

	public class Thing {
		public int x;
		public int y;
		public int angle;
		public int type;
		public int flags;

		public bool skill2;
		public bool skill3;
		public bool skill4;
		public bool ambush;
		public bool multiplayer;
	}

	public class MapData {

		public Vertex[] vertices;
		public Linedef[] linedefs;
		public Sector[] sectors;
		public Sidedef[] sidedefs;
		public Thing[] things;
		public MapFormat format;

		public MapData() {

		}	

		public static MapData Load(WadFile wad, string mapname) {
			byte[] maplump = wad.GetLump(mapname);
			MapData map = null;

			// Detect map type and treat accordingly
			// First see if the lump is a wad. 
			if (maplump.Length != 0) {
				if (new string(Encoding.ASCII.GetChars(maplump, 0, 4)) == "PWAD") {
					// Ok! we have a wad representing a map, so we need to dive into it.
					WadFile mapWad = new WadFile(maplump);
					if (mapWad.directory[1].name == "THINGS") { // not a udmf, either Doom or Hexen
						if (mapWad.Contains("BEHAVIOR")) {
							// Hexen
							throw new Exception("Unsupported map format: Hexen");
						} else {
							map = new DoomMapData(mapWad, mapWad.directory[0].name);
						}
					} else if (mapWad.directory[1].name == "TEXTMAP") {
						map = new UDMFMapData(mapWad.GetLumpAsText("TEXTMAP"));
					} else {
						throw new Exception("Unknown map format");
					}
				}
			} else {
				int mapIndex = wad.GetIndex(mapname);
				if (wad.directory[mapIndex+1].name == "THINGS") {
					if (wad.directory.Count > mapIndex+11 && wad.directory[mapIndex+11].name == "BEHAVIOR") {
						throw new Exception("Unsupported map format: Hexen");
					} else {
						map = new DoomMapData(wad, mapname);
					}
				} else if (wad.directory[mapIndex+1].name == "TEXTMAP") {
					map = new UDMFMapData(wad.GetLumpAsText(mapIndex+1));
				} else {
					throw new Exception("Unknown map format");
				}
			}
			if (map == null) {
				throw new Exception("Error loading map: "+mapname);
			} else {
				return map;
			}
		}
	}
}
