using System.Collections;
using System.Collections.Generic;
using System;
using System.Text;

/*
This handles processing map data from a wad.
*/

namespace WadTools {

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

		public bool impassable { get { return (flags & 1) == 1; } }
		public bool blockMonster { get { return (flags & 2) == 2; } }
		public bool twoSided { get { return (flags & 4) == 4; } }
		public bool upperUnpegged { get { return (flags & 8) == 8; } }
		public bool lowerUnpegged { get { return (flags & 16) == 16; } }
		public bool secret { get { return (flags & 32) == 32; } }
		public bool blockSound { get { return (flags & 64) == 64; } }
		public bool alwaysShow { get { return (flags & 128) == 128; } }

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

		public bool skill2 { get { return (flags & 1) == 1; }}
		public bool skill3 { get { return (flags & 2) == 2; }}
		public bool skill4 { get { return (flags & 4) == 4; }}
		public bool ambush { get { return (flags & 8) == 8; }}
		public bool multiplayer { get { return (flags & 16) == 16; }}
	}

	public class MapData {

		public List<Vertex> vertices;
		public List<Linedef> linedefs;
		public List<Sector> sectors;
		public List<Sidedef> sidedefs;
		public List<Thing> things;

		public MapData(WadFile wad, string name) {
			int index = wad.GetIndex(name);
			LoadThings(wad.GetLump(index + 1));
			LoadLinedefs(wad.GetLump(index + 2));
			LoadSidedefs(wad.GetLump(index + 3));
			LoadVertices(wad.GetLump(index + 4));
			LoadSectors(wad.GetLump(index + 8));
		}

		private string FixString(string input) {
			return WadFile.FixString(input);
		}

		public void LoadVertices(byte[] data) {
			int size = 4;

			vertices = new List<Vertex>();

			for (int i = 0; i < data.Length; i+=size) {
				Vertex nv = new Vertex() {
					x = BitConverter.ToInt16(data, i),
					y = BitConverter.ToInt16(data, i + 2)
				};
				vertices.Add(nv);
			}
		}

		public void LoadLinedefs(byte[] data) {
			int size = 14;

			linedefs = new List<Linedef>();

			for (int i = 0; i < data.Length; i+=size) {
				Linedef nl = new Linedef() {
					start = BitConverter.ToInt16(data, i),
					end = BitConverter.ToInt16(data, i + 2),
					flags = BitConverter.ToInt16(data, i + 4),
					special = BitConverter.ToInt16(data, i + 6),
					tag = BitConverter.ToInt16(data, i + 8),
					front = BitConverter.ToInt16(data, i + 10),
					back = BitConverter.ToInt16(data, i + 12)
				};
				linedefs.Add(nl);
			}
		}

		public void LoadSectors(byte[] data) {
			int size = 26;

			sectors = new List<Sector>();

			for (int i = 0; i < data.Length; i+=size) {
				Sector ns = new Sector() {
					floorHeight = BitConverter.ToInt16(data, i),
					ceilingHeight = BitConverter.ToInt16(data, i + 2),
					floorTexture = FixString(new string(Encoding.ASCII.GetChars(data, i + 4, 8))),
					ceilingTexture = FixString(new string(Encoding.ASCII.GetChars(data, i + 12, 8))),
					lightLevel = BitConverter.ToInt16(data, i + 20),
					type = BitConverter.ToInt16(data, i + 22),
					tag = BitConverter.ToInt16(data, i + 24)
				};
				sectors.Add(ns);
			}
		}

		public void LoadSidedefs(byte[] data) {
			int size = 30;

			sidedefs = new List<Sidedef>();

			for (int i = 0; i < data.Length; i+=size) {
				Sidedef ns = new Sidedef() {
					xOffset = BitConverter.ToInt16(data, i),
					yOffset = BitConverter.ToInt16(data, i + 2),
					upper = FixString(new string(Encoding.ASCII.GetChars(data, i + 4, 8))),
					lower = FixString(new string(Encoding.ASCII.GetChars(data, i + 12, 8))),
					mid = FixString(new string(Encoding.ASCII.GetChars(data, i + 20, 8))),
					sector = BitConverter.ToInt16(data, i + 28),
				};
				sidedefs.Add(ns);
			}
		}

		public void LoadThings(byte[] data) {
			int size = 10;

			things = new List<Thing>();

			for (int i = 0; i < data.Length; i+=size) {
				Thing nt = new Thing() {
					x = BitConverter.ToInt16(data, i),
					y = BitConverter.ToInt16(data, i + 2),
					angle = BitConverter.ToInt16(data, i + 4),
					type = BitConverter.ToInt16(data, i + 6),
					flags = BitConverter.ToInt16(data, i + 8)
				};
				things.Add(nt);
			}
		}
		
	}
}
