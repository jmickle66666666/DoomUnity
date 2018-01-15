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
					start = (int) BitConverter.ToUInt16(data, i),
					end = (int) BitConverter.ToUInt16(data, i + 2),
					flags = (int) BitConverter.ToUInt16(data, i + 4),
					special = (int) BitConverter.ToUInt16(data, i + 6),
					tag = (int) BitConverter.ToUInt16(data, i + 8),
					front = (int) BitConverter.ToUInt16(data, i + 10),
					back = (int) BitConverter.ToUInt16(data, i + 12)
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
