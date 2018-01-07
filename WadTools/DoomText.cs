using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WadTools;

/*

For generating text with fonts loaded from doom.

*/

public class DoomText {

	private WadFile wad;
	private string lumpIdent = "STCFN0";

	public DoomText(WadFile wad) {
		this.wad = wad;
	}

	public Texture2D Write(string text) {
		text = text.ToUpper();
		int textLength = text.Length;
		Texture2D[] chars = new Texture2D[textLength];
		int[] spacing = new int[textLength];
		int currentSpacing = 0;
		for (int i = 0; i < textLength; i++) {
			chars[i] = GetChar(text[i]);
			spacing[i] = currentSpacing;
			if (chars[i] != null) {
				currentSpacing += chars[i].width;
			} else {
				currentSpacing += 6;
			}
		}

		Texture2D output = new Texture2D(currentSpacing, 8, TextureFormat.RGBA32, false, true);

		for (int i = 0; i < textLength; i++) {
			if (chars[i] != null) {
				output.SetPixels32(spacing[i], 8 - chars[i].height, chars[i].width, chars[i].height, chars[i].GetPixels32());
			}
		}
		output.Apply();

		output.filterMode = FilterMode.Point;
		return output;
	}

	private Dictionary<char, Texture2D> textCache;

	private Texture2D GetChar(char c) {
		if (textCache == null) textCache = new Dictionary<char, Texture2D>();

		if (textCache.ContainsKey(c)) return textCache[c];

		string lumpName = lumpIdent + (int)char.ToUpper(c);
		if (wad.Contains(lumpName)) {
			Texture2D output = new DoomGraphic(wad.GetLump(lumpName)).ToRenderMap();
			textCache.Add(c, output);
			return output;
		}
		return null;
	}
	
}
