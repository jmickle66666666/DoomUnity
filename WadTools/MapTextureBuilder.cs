using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WadTools {
    public class MapTextureBuilder
    {
        public static Texture2D paletteLookup;
        public static Texture2D colormapLookup;

        static void Init(WadFile wad) {
            paletteLookup = new Palette(wad.GetLump("PLAYPAL")).GetLookupTexture();
		    colormapLookup = new Colormap(wad.GetLump("COLORMAP")).GetLookupTexture();
        } 

        public static string[] FindMapTextures(MapData map) {
            List<string> output = new List<string>();

            for (int i = 0; i < map.sidedefs.Length; i++) {
                if (!output.Contains(map.sidedefs[i].lower)) { output.Add(map.sidedefs[i].lower); }
                if (!output.Contains(map.sidedefs[i].mid)) { output.Add(map.sidedefs[i].mid); }
                if (!output.Contains(map.sidedefs[i].upper)) { output.Add(map.sidedefs[i].upper); }
            }

            return output.ToArray();
        }

        public static string[] FindMapFlats(MapData map) {
            List<string> output = new List<string>();

            for (int i = 0; i < map.sectors.Length; i++) {
                if (!output.Contains(map.sectors[i].floorTexture)) { output.Add(map.sectors[i].floorTexture); }
                if (!output.Contains(map.sectors[i].ceilingTexture)) { output.Add(map.sectors[i].ceilingTexture); }
            }

            return output.ToArray();
        }

        public static Dictionary<string, Material> BuildTextureMaterials(WadFile wad, string[] textures) {
            if (paletteLookup == null) {
                Init(wad);
            }

            Dictionary<string, Material> output = new Dictionary<string, Material>();
            for (int i = 0; i < textures.Length; i++) {

                if (textures[i] != "-" && wad.textureTable.Contains(textures[i].ToUpper()) && !output.ContainsKey(textures[i].ToUpper())) { 
                    output.Add(textures[i].ToUpper(), BuildTextureMaterial(wad, textures[i]));
                }

            }
            return output;
        }

        public static Material BuildTextureMaterial(WadFile wad, string textureName) {
            if (paletteLookup == null) {
                Init(wad);
            }

            if (wad.textureTable.Contains(textureName.ToUpper())) { 
                DoomTexture texture = wad.textureTable.Get(textureName.ToUpper());
                Material material = new Material(Shader.Find("Doom/Texture"));
                material.SetTexture("_MainTex", DoomGraphic.BuildTexture(textureName.ToUpper(), wad));
                material.SetTexture("_Palette", paletteLookup);
                material.SetTexture("_Colormap", colormapLookup);
                material.enableInstancing = true;
                return material;
            }
            return null;
        }

        public static Dictionary<string, Material> BuildFlatMaterials(WadFile wad, string[] flats) {
            if (paletteLookup == null) {
                Init(wad);
            }

            Dictionary<string, Material> output = new Dictionary<string, Material>();
            for (int i = 0; i < flats.Length; i++) {

                if (wad.Contains(flats[i])) {
                    DoomFlat flat = new DoomFlat(wad.GetLump(flats[i]));
                    Material material = new Material(Shader.Find("Doom/Flat"));
                    material.SetTexture("_MainTex", flat.ToRenderMap());
                    material.SetTexture("_Palette", paletteLookup);
                    material.SetTexture("_Colormap", colormapLookup);
                    material.enableInstancing = true;
                    output.Add(flats[i].ToUpper(), material);
                }

            }
            return output;
        }


    }
}
