using System.Collections;
using System.Collections.Generic;
using System;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

/*
This handles processing UDMF map data from a wad.
*/

namespace WadTools {

	public class UDMFMapData : MapData {

		static string regexComments = @"\/\/(.*?)\r?\n|\/\*((.|\n)*?)\*\/";
		string[] tokens;
		int pos;


		// Unused, but necessary
		string udmfNamespace;

		public UDMFMapData(string textmap) {
			linedefs = new List<Linedef>();
			sidedefs = new List<Sidedef>();
			things = new List<Thing>();
			sectors = new List<Sector>();
			vertices = new List<Vertex>();
			ParseData(textmap);
		}

		public void ParseData(string data) {

			// Remove comments
			data = Regex.Replace(data, regexComments, "");

			// Replace ; with whitespace
			data = data.Replace(";", " ");

			// create list of tokens
			tokens = data.Split(new char[0], StringSplitOptions.RemoveEmptyEntries);

			pos = 0;

			if (tokens[pos] != "namespace") {
				Debug.LogError("UDMF Parse Error: namespace not found");
				return;
			}

			pos += 2;

			udmfNamespace = ParseString(tokens[pos]);

			pos += 1;

			while (pos < tokens.Length) {

				if (tokens[pos] == "linedef") ParseLinedef();
				else if (tokens[pos] == "thing") ParseThing();
				else if (tokens[pos] == "vertex") ParseVertex();
				else if (tokens[pos] == "sector") ParseSector();
				else if (tokens[pos] == "sidedef") ParseSidedef();
				else {
					Debug.LogError("UDMF Parse Error: Unexpected token "+tokens[pos]);
					return;
				}

			}

		}

		string ParseString(string token) {
			// TODO: Use a better regex to handle escaped characters
			return token.Replace("\"", "");
		}

		bool ParseBool(string token) {
			return token == "true";
		}

		Dictionary<string, string> ParseBlock() {
			Dictionary<string, string> output = new Dictionary<string, string>();
			pos += 2;
			while (tokens[pos] != "}") {
				output.Add(tokens[pos], tokens[pos+2]);
				pos += 3;
			}
			return output;
		}

		int BlockInt(Dictionary<string, string> block, string key, int def) {
			if (block.ContainsKey(key)) return int.Parse(block[key]);
			return def;
		}

		int BlockInt(Dictionary<string, string> block, string key) {
			return int.Parse(block[key]);
		}

		string BlockString(Dictionary<string, string> block, string key, string def) {
			if (block.ContainsKey(key)) return block[key];
			return def;
		}

		string BlockString(Dictionary<string, string> block, string key) {
			return block[key];
		}

		float BlockFloat(Dictionary<string, string> block, string key, float def) {
			if (block.ContainsKey(key)) return float.Parse(block[key]);
			return def;
		}

		float BlockFloat(Dictionary<string, string> block, string key) {
			return float.Parse(block[key]);
		}

		bool BlockBool(Dictionary<string, string> block, string key, bool def) {
			if (block.ContainsKey(key)) return ParseBool(block[key]);
			return def;
		}

		bool BlockBool(Dictionary<string, string> block, string key) {
			return ParseBool(block[key]);
		}

		void ParseLinedef() {

			Linedef nl = new Linedef();

			Dictionary<string, string> blockData = ParseBlock();

			nl.start = BlockInt(blockData, "v1");
			nl.end = BlockInt(blockData, "v2");
			nl.special = BlockInt(blockData, "special", 0);
			nl.tag = BlockInt(blockData, "id", -1);
			nl.front = BlockInt(blockData, "sidefront");
			nl.back = BlockInt(blockData, "sideback", -1);
			nl.impassable = BlockBool(blockData, "blocking", false);
			nl.blockMonster = BlockBool(blockData, "blockmonsters", false);
			nl.twoSided = BlockBool(blockData, "twosided", false);
			nl.upperUnpegged = BlockBool(blockData, "dontpegtop", false);
			nl.lowerUnpegged = BlockBool(blockData, "dontpegbottom", false);
			nl.secret = BlockBool(blockData, "secret", false);
			nl.blockSound = BlockBool(blockData, "blocksound", false);
			nl.alwaysShow = BlockBool(blockData, "mapped", false);
			nl.dontDraw = BlockBool(blockData, "dontdraw", false);

			linedefs.Add(nl);
			return;

		}

		void ParseSidedef() {
			
			Sidedef ns = new Sidedef();

			Dictionary<string, string> blockData = ParseBlock();

			ns.xOffset = BlockInt(blockData, "offsetx", 0);
			ns.yOffset = BlockInt(blockData, "offsety", 0);
			ns.upper = BlockString(blockData, "texturetop", "-");
			ns.lower = BlockString(blockData, "texturebottom", "-");
			ns.mid = BlockString(blockData, "texturemiddle", "-");
			ns.sector = BlockInt(blockData, "sector");

			sidedefs.Add(ns);
			return;

		}

		void ParseVertex() {

			Vertex nv = new Vertex();

			Dictionary<string, string> blockData = ParseBlock();

			nv.x = (int) BlockFloat(blockData, "x");
			nv.y = (int) BlockFloat(blockData, "y");

			vertices.Add(nv);
			return;

		}

		void ParseSector() {
			
			Sector ns = new Sector();

			Dictionary<string, string> blockData = ParseBlock();

			ns.floorHeight = BlockInt(blockData, "heightfloor", 0);
			ns.ceilingHeight = BlockInt(blockData, "heightceiling", 0);
			ns.floorTexture = BlockString(blockData, "texturefloor");
			ns.ceilingTexture = BlockString(blockData, "textureceiling");
			ns.lightLevel = BlockInt(blockData, "lightlevel");
			ns.type = BlockInt(blockData, "special", 0);
			ns.tag = BlockInt(blockData, "id", 0);

			sectors.Add(ns);
			return;

		}

		void ParseThing() {
			
			Thing nt = new Thing();

			Dictionary<string, string> blockData = ParseBlock();

			nt.x = BlockInt(blockData, "x");
			nt.y = BlockInt(blockData, "y");
			nt.angle = BlockInt(blockData, "angle", 0);
			nt.type = BlockInt(blockData, "type");
			nt.skill2 = BlockBool(blockData, "skill2", false);
			nt.skill3 = BlockBool(blockData, "skill3", false);
			nt.skill4 = BlockBool(blockData, "skill4", false);
			nt.ambush = BlockBool(blockData, "ambush", false);
			nt.multiplayer = !BlockBool(blockData, "single", false);

			things.Add(nt);
			return;

		}
		
	}
}
