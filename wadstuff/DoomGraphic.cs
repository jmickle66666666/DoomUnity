using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

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

	public DoomTexture Get(string name) {
		if (textures.ContainsKey(name)) {
			return textures[name];
		} else {
			Debug.LogError("No such texture: "+name);
		}
		return null;
	}

	public TextureTable(byte[] lumpData) {
		uint amt = BitConverter.ToUInt32(lumpData, 0);
		uint[] offsets = new uint[amt];
		int i;
		for (i = 0; i < amt; i++) {
			offsets[i] = BitConverter.ToUInt32(lumpData, 4 + (i * 4));
		}

		textures = new Dictionary<string, DoomTexture>();
		for (i = 0; i < amt; i++) {
			int offset = (int) offsets[i];

			uint patchCount = BitConverter.ToUInt16(lumpData, offset + 20);
			List<Patch> patches = new List<Patch>();
			for (int j = offset + 22; j < (offset+22) + (patchCount * 10); j+= 10) {
				Patch np = new Patch(
					(int) BitConverter.ToInt16(lumpData, j),
					(int) BitConverter.ToInt16(lumpData, j + 2),
					(int) BitConverter.ToUInt16(lumpData, j + 4)
				);
				patches.Add(np);
			}

			DoomTexture newTex = new DoomTexture(
				WadFile.GetString(lumpData, offset),
				(int) BitConverter.ToUInt16(lumpData, offset + 12),
				(int) BitConverter.ToUInt16(lumpData, offset + 14),
				patches
			);

			textures.Add(newTex.name, newTex);
		}

	}
}

public class DoomTexture {
	public string name;
	public int width;
	public int height;
	public List<Patch> patches; 

	public DoomTexture(string name, int width, int height, List<Patch> patches) {
		this.name = name;
		this.width = width;
		this.height = height;
		this.patches = patches;
	}
}

public class Patch {
	public int originX;
	public int originY;
	public int patchIndex;

	public Patch(int originX, int originY, int patchIndex) {
		this.originX = originX;
		this.originY = originY;
		this.patchIndex = patchIndex;
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

	// Static functions

	public static Dictionary<string, Texture2D> patchCache;
	public static Dictionary<string, Texture2D> textureCache;

	public static Texture2D BuildPatch(string name, WadFile wad) {
		if (patchCache == null) patchCache = new Dictionary<string, Texture2D>();

		if (patchCache.ContainsKey(name)) {
			return patchCache[name];
		} 
		Texture2D output = new DoomGraphic(wad.GetLump(name.ToUpper())).ToTexture2D(new Palette(wad.GetLump("PLAYPAL")));
		
		patchCache.Add(name, output);

		return output;

	}

	public static Texture2D BuildPatch(int index, PatchTable pnames, WadFile wad) {
		return BuildPatch(pnames.patches[index], wad);
	}

	public static Texture2D BuildPatch(int index, WadFile wad) {
		PatchTable pnames = new PatchTable(wad.GetLump("PNAMES"));
		return BuildPatch(pnames.patches[index], wad);
	}

	public static Texture2D BuildTexture(string name, WadFile wad) {
		if (textureCache == null) textureCache = new Dictionary<string, Texture2D>();

		if (textureCache.ContainsKey(name)) {
			return textureCache[name];
		}

		TextureTable textures = new TextureTable(wad.GetLump("TEXTURE1"));
		PatchTable pnames = new PatchTable(wad.GetLump("PNAMES"));

		DoomTexture texture = textures.Get(name.ToUpper());
		

		Texture2D output = new Texture2D(texture.width, texture.height);
		for (int i = 0; i < texture.patches.Count; i++) {
			Patch p = texture.patches[i];
			Texture2D patch2d = DoomGraphic.BuildPatch(p.patchIndex, pnames, wad);

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
		return output;
	}

}