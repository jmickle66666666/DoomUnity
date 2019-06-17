using System.Collections;
using System.Collections.Generic;
using System;
using System.Text;

/*
This handles processing map data from a wad.
*/

namespace WadTools {

	public class DoomMapData : MapData {

		public DoomMapData(WadFile wad, string name) {

			bounds = new NodeBounds();

			this.format = MapFormat.Doom;
			int index = wad.GetIndex(name);
			LoadThings(wad.GetLump(index + 1));
			LoadLinedefs(wad.GetLump(index + 2));
			LoadSidedefs(wad.GetLump(index + 3));
			LoadVertices(wad.GetLump(index + 4));
			LoadSegs(wad.GetLump(index + 5));
			LoadSubsectors(wad.GetLump(index + 6));
			LoadNodes(wad.GetLump(index + 7));
			LoadSectors(wad.GetLump(index + 8));
		}

		private string FixString(string input) {
			return WadFile.FixString(input);
		}

		public void LoadVertices(byte[] data) {
			int size = 4;

			vertices = new Vertex[data.Length / size];

			for (int i = 0; i < data.Length; i+=size) {
				Vertex nv = new Vertex() {
					x = BitConverter.ToInt16(data, i),
					y = BitConverter.ToInt16(data, i + 2)
				};
				vertices[i / size] = nv;

				if (i == 0) {
					bounds.top = nv.y;
					bounds.bottom = nv.y;
					bounds.left = nv.x;
					bounds.right = nv.x;
				} else {
					if (nv.y > bounds.top) bounds.top = nv.y;
					if (nv.y < bounds.bottom) bounds.bottom = nv.y;
					if (nv.x > bounds.right) bounds.right = nv.x;
					if (nv.x < bounds.left) bounds.left = nv.x;
				}
			}
		}

		public void LoadLinedefs(byte[] data) {
			int size = 14;

			linedefs = new Linedef[data.Length / size];

			for (int i = 0; i < data.Length; i+=size) {

				Linedef nl = new Linedef() {
					start = (int) BitConverter.ToUInt16(data, i),
					end = (int) BitConverter.ToUInt16(data, i + 2),
					special = (int) BitConverter.ToUInt16(data, i + 6),
					tag = (int) BitConverter.ToUInt16(data, i + 8),
					front = (int) BitConverter.ToUInt16(data, i + 10),
					back = (int) BitConverter.ToUInt16(data, i + 12)
				};

				int flags = (int) BitConverter.ToUInt16(data, i + 4);
				nl.impassable = (flags & 1) == 1;
				nl.blockMonster = (flags & 2) == 2;
				nl.twoSided = (flags & 4) == 4;
				nl.upperUnpegged = (flags & 8) == 8;
				nl.lowerUnpegged = (flags & 16) == 16;
				nl.secret = (flags & 32) == 32;
				nl.blockSound = (flags & 64) == 64;
				nl.alwaysShow = (flags & 128) == 128;

				linedefs[i / size] = nl;
			}
		}

		public void LoadSectors(byte[] data) {
			int size = 26;

			sectors = new Sector[data.Length / size];

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
				sectors[i / size] = ns;
			}
		}

		public void LoadSidedefs(byte[] data) {
			int size = 30;

			sidedefs = new Sidedef[data.Length / size];

			for (int i = 0; i < data.Length; i+=size) {
				Sidedef ns = new Sidedef() {
					xOffset = BitConverter.ToInt16(data, i),
					yOffset = BitConverter.ToInt16(data, i + 2),
					upper = FixString(new string(Encoding.ASCII.GetChars(data, i + 4, 8))),
					lower = FixString(new string(Encoding.ASCII.GetChars(data, i + 12, 8))),
					mid = FixString(new string(Encoding.ASCII.GetChars(data, i + 20, 8))),
					sector = BitConverter.ToInt16(data, i + 28),
				};
				sidedefs[i / size] = ns;
			}
		}

		public void LoadThings(byte[] data) {
			int size = 10;

			things = new Thing[data.Length / size];

			for (int i = 0; i < data.Length; i+=size) {
				Thing nt = new Thing() {
					x = BitConverter.ToInt16(data, i),
					y = BitConverter.ToInt16(data, i + 2),
					angle = BitConverter.ToInt16(data, i + 4),
					type = BitConverter.ToInt16(data, i + 6)
				};

				int flags = BitConverter.ToInt16(data, i + 8);
				nt.skill2 = (flags & 1) == 1;
				nt.skill3 = (flags & 2) == 2;
				nt.skill4 = (flags & 4) == 4;
				nt.ambush = (flags & 8) == 8;
				nt.multiplayer = (flags & 16) == 16;
				things[i / size] = nt;
			}
		}

		public void LoadSubsectors(byte[] data) {
			int size = 4;

			subsectors = new Subsector[data.Length / size];

			for (int i = 0; i < data.Length; i+=size) {
				Subsector ns = new Subsector() {
					segCount = BitConverter.ToInt16(data, i),
					firstSeg = BitConverter.ToInt16(data, i + 2)
				};
				subsectors[i / size] = ns;
			}
		}

		public void LoadSegs(byte[] data) {
			int size = 12;

			segs = new Seg[data.Length / size];

			for (int i = 0; i < data.Length; i+=size) {
				Seg ns = new Seg() {
					startIndex = BitConverter.ToInt16(data, i),
					endIndex = BitConverter.ToInt16(data, i + 2),
					angle = BitConverter.ToInt16(data, i + 4),
					linedefIndex = BitConverter.ToInt16(data, i + 6),
					direction = BitConverter.ToInt16(data, i + 8) == 1,
					offset = BitConverter.ToInt16(data, i + 10)
				};
				segs[i / size] = ns;
			}
		}

		public void LoadNodes(byte[] data) {
			int size = 28;

			nodes = new Node[data.Length / size];

			for (int i = 0; i < data.Length; i+=size) {
				Node nn = new Node() {
					x = BitConverter.ToInt16(data, i),
					y = BitConverter.ToInt16(data, i + 2),
					dx = BitConverter.ToInt16(data, i + 4),
					dy = BitConverter.ToInt16(data, i + 6),
					rightBounds = new NodeBounds() {
						top = BitConverter.ToInt16(data, i + 8),
						bottom = BitConverter.ToInt16(data, i + 10),
						left = BitConverter.ToInt16(data, i + 12),
						right = BitConverter.ToInt16(data, i + 14)
					},
					leftBounds = new NodeBounds() {
						top = BitConverter.ToInt16(data, i + 16),
						bottom = BitConverter.ToInt16(data, i + 18),
						left = BitConverter.ToInt16(data, i + 20),
						right = BitConverter.ToInt16(data, i + 22)
					},
					rightChild = BitConverter.ToInt16(data, i + 24),
					leftChild = BitConverter.ToInt16(data, i + 26)
				};
				nodes[i / size] = nn;
			}
		}
		
	}
}
