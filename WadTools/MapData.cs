using System.Collections;
using System.Collections.Generic;
using System;
using System.Text;

/*
Base class for map data.
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

		public bool impassable;
		public bool blockMonster;
		public bool twoSided;
		public bool upperUnpegged;
		public bool lowerUnpegged;
		public bool secret;
		public bool blockSound;
		public bool alwaysShow;

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

		public List<Vertex> vertices;
		public List<Linedef> linedefs;
		public List<Sector> sectors;
		public List<Sidedef> sidedefs;
		public List<Thing> things;

		public MapData() {

		}
		
	}
}
