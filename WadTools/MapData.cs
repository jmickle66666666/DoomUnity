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

	public struct Subsector {
		public int segCount;
		public int firstSeg;
	}

	public struct Seg {
		public int startIndex;
		public int endIndex;
		public int angle;
		public int linedefIndex;
		public bool direction;
		public int offset;
	}

	public struct NodeBounds {
		public int top;
		public int bottom;
		public int left;
		public int right;
	}

	public struct Node {
		public int x;
		public int y;
		public int dx;
		public int dy;
		public NodeBounds rightBounds;
		public NodeBounds leftBounds;
		public int rightChild;
		public int leftChild;
	}

	public class MapData {

		public Vertex[] vertices;
		public Linedef[] linedefs;
		public Sector[] sectors;
		public Sidedef[] sidedefs;
		public Thing[] things;
		public Node[] nodes;
		public Subsector[] subsectors;
		public Seg[] segs;
		public MapFormat format;
		public NodeBounds bounds;
		public int lowestHeight;
		public int tallestHeight;

		public MapData() {

		}	

		public int[] GetLinesOfSector(int sector) {
			List<int> output = new List<int>();

			for (int i = 0; i < linedefs.Length; i++) {
				if (sidedefs[linedefs[i].front].sector == sector) {
					output.Add(i);
				} else {
					if (linedefs[i].back != 0xFFFF && linedefs[i].back != -1) {
						if (sidedefs[linedefs[i].back].sector == sector) {
							output.Add(i);
						}
					}
				}
			}

			return output.ToArray();
		}

		// Doors open the sector on the reverse side
		public int GetLineDoorSector(int line) {
			return sidedefs[linedefs[line].back].sector;
		}

		public int[] GetSectorsWithTag(int tag) {
			List<int> output = new List<int>();
			for (int i = 0; i < sectors.Length; i++) {
				if (sectors[i].tag == tag) output.Add(i);
			}
			return output.ToArray();
		}

		public int LowestAdjacentCeiling(int sector) {
			Sector[] sectors = AdjacentSectors(sector);
			int output = sectors[0].ceilingHeight;
			for (int i = 0; i < sectors.Length; i++) {
				if (sectors[i].ceilingHeight < output) output = sectors[i].ceilingHeight;
			}
			return output;
		}

		public int HighestAdjecentFloor(int sector) {
			Sector[] sectors = AdjacentSectors(sector);
			int output = sectors[0].floorHeight;
			for (int i = 0; i < sectors.Length; i++) {
				if (sectors[i].floorHeight > output) output = sectors[i].floorHeight;
			}
			return output;
		}

		public int LowestAdjecentFloor(int sector) {
			Sector[] sectors = AdjacentSectors(sector);
			int output = sectors[0].floorHeight;
			for (int i = 0; i < sectors.Length; i++) {
				if (sectors[i].floorHeight < output) output = sectors[i].floorHeight;
			}
			return output;
		}

		public Sector[] AdjacentSectors(int sector) {
			List<Sector> output = new List<Sector>();

			int[] sectorLines = GetLinesOfSector(sector); 

			for (int i = 0; i < sectorLines.Length; i++) {
				int sectorIndex = sidedefs[linedefs[sectorLines[i]].front].sector;
				if (sectorIndex != sector) {
					Sector front = sectors[sectorIndex];
					output.Add(front);
				}
				if (linedefs[sectorLines[i]].back != 0xFFFF && linedefs[sectorLines[i]].back != -1) {
					int backSectorIndex = sidedefs[linedefs[sectorLines[i]].back].sector;
					if (backSectorIndex != sector) {
						Sector back = sectors[sectorIndex];
						output.Add(back);
					}
				}
			}

			return output.ToArray();
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
