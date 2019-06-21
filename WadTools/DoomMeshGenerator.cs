using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WadTools {

    enum SideOrientation
    {
        Upper,
        Lower,
        Middle
    }

    public class DoomMeshGenerator
    {

        WadFile wad;
        MapData map;
        Dictionary<string, Material> wallMaterials;
        Dictionary<string, Material> flatMaterials;
        Material skyMaterial;
        NodeTriangulation nodeTri;

        public DoomMeshGenerator(WadFile wad, MapData map, NodeTriangulation nodeTri) 
        {
            this.wad = wad;
            this.map = map;
            this.nodeTri = nodeTri;
        }

        // public GameObject BuildCollisionMesh()
        // {
        //     GameObject output = new GameObject("Collision");

        //     return output;
        // }

        public GameObject BuildMesh()
        {
            // SETUP
            flatMaterials = MapTextureBuilder.BuildFlatMaterials(wad, MapTextureBuilder.FindMapFlats(map));
            wallMaterials = MapTextureBuilder.BuildTextureMaterials(wad, MapTextureBuilder.FindMapTextures(map));

            DoomTexture texture = wad.textureTable.Get("SKY1");
            skyMaterial = new Material(Shader.Find("Doom/DoomSky"));
            skyMaterial.SetTexture("_MainTex", DoomGraphic.BuildTexture("SKY1", wad));
            skyMaterial.SetTexture("_Palette", MapTextureBuilder.paletteLookup);
            skyMaterial.SetTexture("_Colormap", MapTextureBuilder.colormapLookup);
            skyMaterial.enableInstancing = true;

            wallMaterials.Add("_SKY", MapTextureBuilder.BuildTextureMaterial(wad, "SKY1"));
            GameObject output = new GameObject("MAP");

            // TRIANGULATE
            

            // SECTORS
            for (int i = 0; i < nodeTri.subsectorHulls.Count; i++) {
                if (nodeTri.subsectorHulls[i].hull.Length > 2) {
                    Sector sector = map.sectors[nodeTri.subsectorHulls[i].sector];
                    
                    SubsectorFloorObject(
                        MeshFromConvexHull(nodeTri.subsectorHulls[i].hull, sector.floorHeight, false),
                        sector,
                        sector.floorTexture == "F_SKY1"?skyMaterial:flatMaterials[sector.floorTexture]
                    ).transform.SetParent(output.transform, false);

                    SubsectorCeilingObject(
                        MeshFromConvexHull(nodeTri.subsectorHulls[i].hull, sector.ceilingHeight, true),
                        sector,
                        sector.ceilingTexture == "F_SKY1"?skyMaterial:flatMaterials[sector.ceilingTexture]
                    ).transform.SetParent(output.transform, false);

                }
            }

            // WALLS

            for (int i = 0; i < map.linedefs.Length; i++) {
                var quads = BuildLine(i);
                for (int j = 0; j < quads.Length; j++) {
                    quads[j].transform.SetParent(output.transform, false);
                }
            }

            return output;        
        }

        Mesh MeshFromConvexHull(Vector2[] hull, float height, bool reversed) 
        {
            Mesh mesh = new Mesh();

            Vector3[] vertices = new Vector3[hull.Length];
            int[] triangles = new int[(hull.Length-2)*3];

            for (int i = 0; i < hull.Length; i++) {
                vertices[i] = new Vector3(hull[i].x, height, hull[i].y);

                if (i < hull.Length - 2) {
                    triangles[(i * 3) + 0] = 0;
                    triangles[(i * 3) + 1] = i + 1;
                    triangles[(i * 3) + 2] = i + 2;
                } 
            }

            if (reversed) {
                System.Array.Reverse(triangles);
            }

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            return mesh;
        }

        GameObject SubsectorFloorObject(Mesh mesh, Sector sector, Material floor) 
        {
            GameObject subsector = new GameObject("subsector");
            subsector.AddComponent<MeshCollider>().sharedMesh = mesh;
            subsector.AddComponent<MeshFilter>().mesh = mesh;
            MeshRenderer mr = subsector.AddComponent<MeshRenderer>();
            mr.material = floor;
            mr.material.SetFloat("_Brightness", sector.lightLevel/256f);
            return subsector;
        }

        GameObject SubsectorCeilingObject(Mesh mesh, Sector sector, Material ceiling) 
        {
            GameObject subsector = new GameObject("subsector");
            subsector.AddComponent<MeshCollider>().sharedMesh = mesh;
            subsector.AddComponent<MeshFilter>().mesh = mesh;
            MeshRenderer mr = subsector.AddComponent<MeshRenderer>();
            mr.material = ceiling;
            mr.material.SetFloat("_Brightness", sector.lightLevel/256f);
            return subsector;
        }

        GameObject[] BuildLine(int lineIndex) {
            List<GameObject> output = new List<GameObject>();

            Linedef line = map.linedefs[lineIndex];
            Sidedef front = map.sidedefs[line.front];
            Sidedef back = null;

            if (line.back != 0xFFFF && line.back != -1) {
                back = map.sidedefs[line.back];
            }

            float floorHeight = map.sectors[front.sector].floorHeight;
            float ceilingHeight = map.sectors[front.sector].ceilingHeight;

            Vector2Int start = new Vector2Int(
                map.vertices[line.start].x, 
                map.vertices[line.start].y
            );

            Vector2Int end = new Vector2Int(
                map.vertices[line.end].x, 
                map.vertices[line.end].y
            );

            float length = Vector2Int.Distance(start,end);

            Vector2Int frontHeight = new Vector2Int(
                map.sectors[front.sector].floorHeight,
                map.sectors[front.sector].ceilingHeight
            );

            Vector2Int backHeight = new Vector2Int();
            if (back != null) {
                backHeight.x = map.sectors[back.sector].floorHeight;
                backHeight.y = map.sectors[back.sector].ceilingHeight;
            }


            if (back == null) {
                // 1-sided line

                Vector2Int topHeight = new Vector2Int(
                    frontHeight.y,
                    frontHeight.y
                );
                Vector2Int bottomHeight = new Vector2Int(
                    frontHeight.x,
                    frontHeight.x
                );

                if (frontHeight.y != frontHeight.x) {
                    Mesh mesh = BuildQuad(
                        start, end, frontHeight, CalculateUVs(length, bottomHeight, topHeight, line, true, SideOrientation.Middle)
                    );
                    output.Add(BuildGameObject(mesh, map.sectors[front.sector], GetWallMaterial(front.mid), true));
                }
                
            } else {
                // 2-sided line
                Mesh mesh;
                Material material;

                Vector2Int topHeight = new Vector2Int(
                    Mathf.Min(backHeight.y, frontHeight.y),
                    Mathf.Max(backHeight.y, frontHeight.y)
                );
                Vector2Int bottomHeight = new Vector2Int(
                    Mathf.Min(backHeight.x, frontHeight.x),
                    Mathf.Max(backHeight.x, frontHeight.x)
                );

                // Front Mid
                if (front.mid != "-") {
                    Vector2Int height = new Vector2Int(
                        bottomHeight.y,
                        topHeight.x
                    );
                    mesh = BuildQuad(start, end, height, CalculateUVs(length, bottomHeight, topHeight, line, true, SideOrientation.Middle));
                    output.Add(BuildGameObject(mesh, map.sectors[front.sector], material = GetWallMaterial(front.mid), line.impassable));
                }

                if (frontHeight.y > backHeight.y) {
                    // Front Upper
                    mesh = BuildQuad(start, end, topHeight, CalculateUVs(length, bottomHeight, topHeight, line, true, SideOrientation.Upper));
                    if (IsSky(line, true, SideOrientation.Upper)) {
                        material = skyMaterial;
                    } else {
                        material = GetWallMaterial(front.upper);
                    }
                    output.Add(BuildGameObject(mesh, map.sectors[front.sector], material, true));
                }

                if (frontHeight.x < backHeight.x) {
                    // Front Lower
                    mesh = BuildQuad(start, end, bottomHeight, CalculateUVs(length, bottomHeight, topHeight, line, true, SideOrientation.Lower));
                    if (IsSky(line, true, SideOrientation.Lower)) {
                        material = skyMaterial;
                    } else {
                        material = GetWallMaterial(front.lower);
                    }
                    
                    output.Add(BuildGameObject(mesh, map.sectors[front.sector], material, true));
                }
                

                // Back Mid
                if (back.mid != "-") {
                    Vector2Int height = new Vector2Int(
                        bottomHeight.y,
                        topHeight.x
                    );
                    mesh = BuildQuad(end, start, height, CalculateUVs(length, bottomHeight, topHeight, line, false, SideOrientation.Middle));
                    output.Add(BuildGameObject(mesh, map.sectors[back.sector], GetWallMaterial(back.mid), line.impassable));
                }

                if (frontHeight.y < backHeight.y) {
                    // Back Upper
                    mesh = BuildQuad(end, start, topHeight, CalculateUVs(length, bottomHeight, topHeight, line, false, SideOrientation.Upper));
                    if (IsSky(line, false, SideOrientation.Upper)) {
                        material = skyMaterial;
                    } else {
                        material = GetWallMaterial(back.upper);
                    }
                    output.Add(BuildGameObject(mesh, map.sectors[back.sector], material, true));
                }

                if (frontHeight.x > backHeight.x) {
                    // Back Lower
                    mesh = BuildQuad(end, start, bottomHeight, CalculateUVs(length, bottomHeight, topHeight, line, false, SideOrientation.Lower));
                    if (IsSky(line, false, SideOrientation.Lower)) {
                        material = skyMaterial;
                    } else {
                        material = GetWallMaterial(back.lower);
                    }
                    output.Add(BuildGameObject(mesh, map.sectors[back.sector], material, true));
                }
                
            }

            return output.ToArray();
        }

        GameObject BuildGameObject(Mesh mesh, Sector sector, Material material, bool impassable) 
        {
            GameObject quad = new GameObject("line");
            if (impassable) quad.AddComponent<MeshCollider>().sharedMesh = mesh;
            quad.AddComponent<MeshFilter>().mesh = mesh;
            MeshRenderer mr = quad.AddComponent<MeshRenderer>();
            mr.material = material;
            mr.material.SetFloat("_Brightness", sector.lightLevel/256f);
            return quad;
        }

        Material GetWallMaterial(string texture) 
        {
            if (!wallMaterials.ContainsKey(texture.ToUpper())) {
                if (texture == "-") {
                    Debug.LogWarning($"Visible side with texture -");
                } else {
                    Debug.LogError($"Can't find texture: {texture.ToUpper()}");
                }
            } else {
                return wallMaterials[texture.ToUpper()];
            }
            return null;
        }

        bool IsSky(Linedef line, bool front, SideOrientation orientation)
        {
            if (orientation == SideOrientation.Middle) return false; 
            Sector backSector = map.sectors[map.sidedefs[front?line.back:line.front].sector];
            switch (orientation) {
                case SideOrientation.Upper:
                    if (backSector.ceilingTexture == "F_SKY1") return true;
                    break;
                case SideOrientation.Lower:
                    if (backSector.floorTexture == "F_SKY1") return true;
                    break;
            }
            return false;
        }

        Mesh BuildQuad(Vector2Int start, Vector2Int end, Vector2Int height, Vector2[] uv)
        {
            Vector3[] vertices = new Vector3[] {
                new Vector3(start.x, height.x, start.y),
                new Vector3(start.x, height.y, start.y),
                new Vector3(end.x, height.y, end.y),
                new Vector3(end.x, height.x, end.y)
            };
            int[] triangles = new int[] {
                0, 1, 2, 0, 2, 3
            };
            Mesh mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uv;
            mesh.RecalculateNormals();
            return mesh;
        }

        // TODO?
        // Mesh mergeMeshes(Mesh[] meshes) {
        //     Mesh output = new Mesh();
        //     List<Vector3> vertices = new List<Vector3>();
        //     List<
        //     int vertexCount = 0;
        //     for (int i = 0; i < meshes.Length; i++) {

        //         for (int j = 0; j < meshes[i].triangles.Length; j++) {
        //             meshes[i].triangles[j] += vertexCount;
        //         }
        //         vertexCount += meshes[i].vertices.Length;
        //     }
        // }

        Vector2[] CalculateUVs(float length, Vector2 bottomHeight, Vector2 topHeight, Linedef line, bool front, SideOrientation orientation)
        {
            Sidedef side = null;
            side = map.sidedefs[front?line.front:line.back];
            float height;

            string texture;
            switch (orientation) {
                case SideOrientation.Upper: 
                    texture = side.upper; 
                    height = topHeight.y - topHeight.x;
                    break;
                case SideOrientation.Middle: 
                    texture = side.mid; 
                    height = topHeight.x - bottomHeight.y;
                    break;
                case SideOrientation.Lower: 
                    texture = side.lower; 
                    height = bottomHeight.y - bottomHeight.x;
                    break;
                default: 
                    texture = "-";
                    height = 1f; 
                    break;
            }

            Vector2 size = Vector2.one;
            Vector2 offset = Vector2.zero;
            
            if (wad.textureTable.Contains(texture.ToUpper())) {
                var textureInfo = wad.textureTable.Get(texture.ToUpper());
                Vector2 textureSize = new Vector2(textureInfo.width, textureInfo.height);
                size.x = length / textureSize.x;
                size.y = height / textureSize.y;

                offset.x = (float)side.xOffset / textureSize.x;
                offset.y = (float)side.yOffset / textureSize.y;

                if (orientation == SideOrientation.Middle) {
                    if (line.lowerUnpegged && !line.upperUnpegged) {
                        // Draw from bottom
                        offset.y += size.y;
                    } else {

                    }
                }

                if (orientation == SideOrientation.Upper) {
                    if (!line.upperUnpegged) {
                        // draw from height ceiling
                        offset.y -= size.y;
                    }
                }

                if (orientation == SideOrientation.Lower) {
                    if (!line.lowerUnpegged) {
                        // draw from higher ceiling
                        offset.y -= topHeight.y - bottomHeight.y;
                    }
                }
            }

            size += offset;

            return new Vector2[] {
                new Vector2(offset.x,size.y),
                new Vector2(offset.x,offset.y),
                new Vector2(size.x,offset.y),
                new Vector2(size.x,size.y)
            };
        }
    }
}

