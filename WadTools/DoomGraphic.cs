using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/*
This handles processing for Doom Graphics, Doom Flats, the Palette and Colormap lumps from a wad.
*/

namespace WadTools {

	public class Palette {
		private byte[] data;
		public Palette(byte[] data) {
			this.data = data;
		}

		public Color GetColor(int index) {
			float r = data[(index * 3)] / 256f;
			float g = data[(index * 3)+1] / 256f;
			float b = data[(index * 3)+2] / 256f;
			return new Color(r,g,b);
		}

		public Texture2D GetLookupTexture() {
			Texture2D output = new Texture2D(256, 1, TextureFormat.RGBA32, false, true);
			for (int i = 0; i < 256; i++) {
				output.SetPixel(i, 0, GetColor(i));
			}
			output.Apply();
			output.wrapMode = TextureWrapMode.Clamp;
			output.filterMode = FilterMode.Point;
			//Debug.Log("This should be 1: "+output.mipmapCount);
			return output;
		}
	}

	public class Colormap {
		private byte[] data;
		public Colormap(byte[] data) {
			this.data = data;
		}

		public int Lookup(int index, int table) {
			return data[(table * 256) + index];
		}

		public Texture2D GetLookupTexture() {
			Texture2D output = new Texture2D(256,34, TextureFormat.RGBA32, false, true);
			for (int i = 0; i < 256; i++) {
				for (int j = 0; j < 34; j++) {
					output.SetPixel(i, j, new Color(Lookup(i, j) / 256f, 0f, 0f, 1f));
				}
			}
			output.Apply();
			output.wrapMode = TextureWrapMode.Clamp;
			output.filterMode = FilterMode.Point;
			return output;
		}

		public Texture2D GetPalettedLookup(Palette palette) {
			Texture2D output = new Texture2D(256, 34, TextureFormat.RGBA32, false, true);
			for (int i = 0; i < 256; i++) {
				for (int j = 0; j < 34; j++) {
					output.SetPixel(i, j, palette.GetColor(data[i + (j*256)]));
				}
			}
			output.Apply();
			output.wrapMode = TextureWrapMode.Clamp;
			output.filterMode = FilterMode.Point;
			return output;
		}
	}

	public class PatchTable {
		public List<string> patches;

		public PatchTable(byte[] lumpData) {
			patches = new List<string>();

			for (int i = 0; i < (int) BitConverter.ToUInt32(lumpData, 0); i++) {
				patches.Add(WadFile.GetString(lumpData, 4 + (i * 8)));
			}
		}
	}

	public class TextureTable {
		private Dictionary<string, DoomTexture> textures;
		private PatchTable patches;

		public int size {
			get {
				return textures.Count;
			}
		}

		public DoomTexture Get(string name, bool tryDefault = true) {
			if (textures.ContainsKey(name)) {
				return textures[name];
			} else {
				Debug.LogError("No such texture: "+name);
				if (tryDefault) {
					return Get("AASHITTY", false);
				} else {
					
				}
			}
			return null;
		}

		public TextureTable(byte[] lumpData = null, PatchTable patchTable = null) {
			textures = new Dictionary<string, DoomTexture>();
			if (lumpData != null) {
				if (patchTable == null) {
					Debug.LogError("Must include patchtable");
					return;
				}
				Add(lumpData, patchTable);
			}
		}

		public bool Contains(string name) {
			return textures.ContainsKey(name);
		}

		// Append a texture definition from wad
		public void Add(byte[] lumpData, PatchTable patchTable) {
			uint amt = BitConverter.ToUInt32(lumpData, 0);
			uint[] offsets = new uint[amt];
			int i;
			for (i = 0; i < amt; i++) {
				offsets[i] = BitConverter.ToUInt32(lumpData, 4 + (i * 4));
			}

			
			for (i = 0; i < amt; i++) {
				int offset = (int) offsets[i];

				uint patchCount = BitConverter.ToUInt16(lumpData, offset + 20);
				List<DoomPatch> patches = new List<DoomPatch>();
				for (int j = offset + 22; j < (offset+22) + (patchCount * 10); j+= 10) {
					DoomPatch np = new DoomPatch(
						(int) BitConverter.ToInt16(lumpData, j),
						(int) BitConverter.ToInt16(lumpData, j + 2),
						patchTable.patches[(int) BitConverter.ToUInt16(lumpData, j + 4)]
					);
					patches.Add(np);
				}

				DoomTexture newTex = new DoomTexture(
					WadFile.GetString(lumpData, offset),
					(int) BitConverter.ToUInt16(lumpData, offset + 12),
					(int) BitConverter.ToUInt16(lumpData, offset + 14),
					patches
				);

				if (textures.ContainsKey(newTex.name)) {
					textures[newTex.name] = newTex;
				} else {
					//Debug.Log(newTex.name);
					textures.Add(newTex.name, newTex);
				}
			}
		}

		// Merge in an existing texture definition
		public void Merge(TextureTable textureTable) {
			foreach (KeyValuePair<string, DoomTexture> entry in textureTable.textures) {
				if (textures.ContainsKey(entry.Key)) {
					textures[entry.Key] = entry.Value;
				} else {
					textures.Add(entry.Key, entry.Value);
				}
			}
		} 
	}

	public class DoomTexture {
		public string name;
		public int width;
		public int height;
		public List<DoomPatch> patches; 

		public DoomTexture(string name, int width, int height, List<DoomPatch> patches) {
			this.name = name;
			this.width = width;
			this.height = height;
			this.patches = patches;
		}
	}

	public class DoomPatch {
		public int originX;
		public int originY;
		public string patchName;

		public DoomPatch(int originX, int originY, string patchName) {
			this.originX = originX;
			this.originY = originY;
			this.patchName = patchName;
		}
	}

	public class DoomGraphic {

		public int width;
		public int height;
		public int offsetX;
		public int offsetY;
		private byte[] data;

		public DoomGraphic(byte[] data) {
			width = BitConverter.ToInt16(data, 0);
			height = BitConverter.ToInt16(data, 2);
			offsetX = BitConverter.ToInt16(data, 4);
			offsetY = BitConverter.ToInt16(data, 6);

			this.data = data;
		}

		public Texture2D ToTexture2D(Palette palette) {
			Texture2D output = new Texture2D(width, height);
			int i, j;

			for (i = 0; i < width; i++) {
				for (j=0; j < height; j++) {
					output.SetPixel(i, j, Color.clear);
				}
			}

			uint[] columns = new uint[width];

			for (i = 0; i < width; i++) {
				columns[i] = BitConverter.ToUInt32(data, 8 + (i * 4));
			}

			uint position = 0;
			int pixelCount = 0;
			
			for (i = 0; i < width; i++) {
	            
	            position = columns[i];
	            int rowStart = 0;
	            
	            while (rowStart != 255) {
	                
	                rowStart = data[position];
	                position += 1;
	                
	                if (rowStart == 255) break;
	                
	                pixelCount = data[position];
	                position += 2;
	                
	                for (j = 0; j < pixelCount; j++) {
	                	output.SetPixel(i, (rowStart+j) - height, palette.GetColor((int) data[position]));
	                    position += 1;
	                }
	                position += 1;
	            }
	        }

	        output.Apply();
	        output.wrapMode = TextureWrapMode.Repeat;
	        output.filterMode = FilterMode.Point;
	        return output;
		}

		public Texture2D ToRenderMap(bool inverseY = false) {
			Texture2D output = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
			int i, j;

			for (i = 0; i < width; i++) {
				for (j=0; j < height; j++) {
					output.SetPixel(i, j, Color.clear);
				}
			}

			uint[] columns = new uint[width];

			for (i = 0; i < width; i++) {
				columns[i] = BitConverter.ToUInt32(data, 8 + (i * 4));
			}

			uint position = 0;
			int pixelCount = 0;
			
			for (i = 0; i < width; i++) {
	            
	            position = columns[i];
	            int rowStart = 0;
	            
	            while (rowStart != 255) {
	                
	                rowStart = data[position];
	                position += 1;
	                
	                if (rowStart == 255) break;
	                
	                pixelCount = data[position];
	                position += 2;
	                
	                for (j = 0; j < pixelCount; j++) {
	                	int hPixel = (rowStart+j) - height;
	                	if (inverseY) {
	                		hPixel = height - hPixel - 1;
	                	}
	                	output.SetPixel(i, hPixel, new Color(data[position] / 256f, 0f, 0f, 1f));
	                	//output.SetPixel(i, (rowStart+j) - height, palette.GetColor((int) data[position]));
	                    position += 1;
	                }
	                position += 1;
	            }
	        }

	        output.Apply();
	        output.wrapMode = TextureWrapMode.Repeat;
	        //Debug.Log("This should be 1: "+output.mipmapCount);
	        output.filterMode = FilterMode.Point;
	        return output;
		}

		public Sprite ToSprite() {
			Texture2D texture = ToRenderMap(true);
			Sprite output = Sprite.Create(texture, new Rect(0,0,(float)width,(float)height), new Vector2((float)offsetX / (float)width, 1.0f - ((float)offsetY / (float)height)));
			return output;
		}

		// Static functions

		public static Dictionary<string, Sprite> spriteCache;
		public static Dictionary<string, Texture2D> patchCache;
		public static Dictionary<string, Texture2D> textureCache;

		public static Sprite BuildSprite(string name, WadFile wad) {
			if (spriteCache == null) spriteCache = new Dictionary<string, Sprite>();

			if (spriteCache.ContainsKey(name)) {
				return spriteCache[name];
			}

			Sprite output = new DoomGraphic(wad.GetLump(name.ToUpper())).ToSprite();
			spriteCache.Add(name, output);

			return output;
		}

		public static Texture2D BuildPatch(string name, WadFile wad, bool ignoreCache = false, bool trueColor = false) {

			if (!wad.Contains(name.ToUpper())) {
				return null;
			}

			if (patchCache == null) patchCache = new Dictionary<string, Texture2D>();

			if (!ignoreCache) {
				if (patchCache.ContainsKey(name)) {
					return patchCache[name];
				} 
			}

			Texture2D output = new DoomGraphic(wad.GetLump(name.ToUpper())).ToRenderMap();
			if (trueColor) output = new DoomGraphic(wad.GetLump(name.ToUpper())).ToTexture2D(new Palette(wad.GetLump("PLAYPAL")));
			
			if (!ignoreCache) {
				patchCache.Add(name, output);
			}
			
			return output;

		}

		public static Texture2D BuildPatch(DoomPatch patch, WadFile wad, bool trueColor = false) {
			return BuildPatch(patch.patchName, wad, false, trueColor);
		}

		public static Texture2D BuildTexture(string name, WadFile wad, TextureTable textures, bool trueColor = false) {
			if (textureCache == null) textureCache = new Dictionary<string, Texture2D>();

			if (textureCache.ContainsKey(name)) {
				return textureCache[name];
			}

			DoomTexture texture = textures.Get(name.ToUpper());
			

			Texture2D output = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false, true);
			for (int i = 0; i < texture.patches.Count; i++) {
				DoomPatch p = texture.patches[i];
				Texture2D patch2d = DoomGraphic.BuildPatch(p.patchName, wad, trueColor);

				if (patch2d == null) return null;

				int copyX = (p.originX < 0)?-p.originX:0;
				int copyY = (p.originY < 0)?-p.originY:0;

				int pasteX = (p.originX > 0)?p.originX:0;
				int pasteY = (p.originY > 0)?p.originY:0;

				int copyWidth = patch2d.width - copyX;
				if (copyWidth > output.width - pasteX) {
					copyWidth = output.width - pasteX;
				}

				int copyHeight = patch2d.height - copyY;
				if (copyHeight > output.height - pasteY) {
					copyHeight = output.height - pasteY;
				}

				for (int a = 0; a < copyWidth; a++) {
					for (int b = 0; b < copyHeight; b++) {
						Color col = patch2d.GetPixel(copyX + a, copyY + b);
						if (col.a != 0f) {
							output.SetPixel(pasteX+a, pasteY+b, col);
						}
					}
				}
				
			}

			output.Apply();
			output.wrapMode = TextureWrapMode.Repeat;
	        output.filterMode = FilterMode.Point;
			
			textureCache.Add(name, output);

			return output;
		}

	}

	public class DoomFlat {

		private byte[] data;

		public DoomFlat(byte[] lumpData) {
			this.data = lumpData;
		}

		public Texture2D ToTexture2D(Palette palette) {
			Texture2D output = new Texture2D(64,64);
			for (int i = 0; i < 64; i++) {
				for (int j = 0; j < 64; j++){
					output.SetPixel(i, j, palette.GetColor(data[(j*64)+i]));
				}
			}
			output.Apply();
			output.wrapMode = TextureWrapMode.Repeat;
	        output.filterMode = FilterMode.Point;
			return output;
		}

		public Texture2D ToRenderMap() {
			Texture2D output = new Texture2D(64,64, TextureFormat.RGBA32, false, true);
			for (int i = 0; i < 64; i++) {
				for (int j = 0; j < 64; j++){
					int index = data[(j*64)+i];
					output.SetPixel(i, j, new Color(index / 256f,0f, 0f, 1f));
				}
			}
			output.Apply();
			output.wrapMode = TextureWrapMode.Repeat;
	        output.filterMode = FilterMode.Point;
			return output;
		}
	}
}
