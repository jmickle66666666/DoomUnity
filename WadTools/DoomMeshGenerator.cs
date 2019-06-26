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
        public MapData map;
        Dictionary<string, Material> wallMaterials;
        Dictionary<string, Material> flatMaterials;
        Material skyMaterial;
        public NodeTriangulation nodeTri;

        Transform lines;
        Transform sectors;
        Transform triggers;
        public SectorObject[] sectorObjects;

        List<LinedefTrigger> triggerList;

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
            
            lines = new GameObject("Lines").transform;
            sectors = new GameObject("Sectors").transform;
            triggers = new GameObject("Triggers").transform;

            lines.parent = output.transform;
            sectors.parent = output.transform;
            triggers.parent = output.transform;

            sectorObjects = new SectorObject[map.sectors.Length];

            for (int i = 0; i < map.sectors.Length; i++) {
                GameObject gameObject = new GameObject($"Sector {i}");
                gameObject.transform.parent = sectors;
                SectorObject sectorObject = gameObject.AddComponent<SectorObject>();
                sectorObject.sector = i;
                sectorObject.lines = map.GetLinesOfSector(i);
                sectorObjects[i] = sectorObject;
                sectorObject.floor = new GameObject("Floor").transform;
                sectorObject.ceiling = new GameObject("Ceiling").transform;
                sectorObject.floor.parent = sectorObject.transform;
                sectorObject.ceiling.parent = sectorObject.transform;
                sectorObject.initialFloorPosition = map.sectors[i].floorHeight;
                sectorObject.initialCeilingPosition = map.sectors[i].ceilingHeight;

                sectorObject.meshGenerator = this;
            }

            // SECTORS
            for (int i = 0; i < nodeTri.subsectorHulls.Count; i++) {
                if (nodeTri.subsectorHulls[i].hull.Length > 2) {
                    Sector sector = map.sectors[nodeTri.subsectorHulls[i].sector];
                    Transform sectorTransform = sectors.GetChild(nodeTri.subsectorHulls[i].sector);
                    
                    SubsectorFloorObject(
                        MeshFromConvexHull(nodeTri.subsectorHulls[i].hull, sector.floorHeight, false),
                        sector,
                        sector.floorTexture == "F_SKY1"?skyMaterial:flatMaterials[sector.floorTexture]
                    ).transform.SetParent(sectorObjects[nodeTri.subsectorHulls[i].sector].floor, false);

                    SubsectorCeilingObject(
                        MeshFromConvexHull(nodeTri.subsectorHulls[i].hull, sector.ceilingHeight, true),
                        sector,
                        sector.ceilingTexture == "F_SKY1"?skyMaterial:flatMaterials[sector.ceilingTexture]
                    ).transform.SetParent(sectorObjects[nodeTri.subsectorHulls[i].sector].ceiling, false);

                }
            }

            // WALLS

            triggerList = new List<LinedefTrigger>();

            for (int i = 0; i < map.linedefs.Length; i++) {
                var line = new GameObject($"Line {i}");
                var quads = BuildLine(i);
                for (int j = 0; j < quads.Length; j++) {
                    quads[j].transform.SetParent(line.transform, false);
                }
                line.transform.parent = lines;
                
                if (map.linedefs[i].special != 0) {
                    var triggerObject = new GameObject($"Trigger {i}");
                    var trigger = triggerObject.AddComponent<LinedefTrigger>();
                    triggerObject.layer = LayerMask.NameToLayer("Trigger");
                    triggerObject.transform.parent = triggers;

                    trigger.linedefIndex = i;
                    trigger.sectorTag = map.linedefs[i].tag;
                    trigger.specialType = map.linedefs[i].special;
                    trigger.doomMesh = this;

                    trigger.Init();

                    if (trigger.triggerType == TriggerType.Use || trigger.triggerType == TriggerType.Shoot) {
                        Mesh mesh = BuildTriggerMesh(i);
                        triggerObject.AddComponent<MeshFilter>().sharedMesh = mesh;

                        var collider = triggerObject.AddComponent<MeshCollider>();
                        collider.sharedMesh = mesh;
                        collider.convex = true;
                        collider.isTrigger = true;
                    } else { 
                        triggerList.Add(trigger);
                    }
                }
            }

            return output;        
        }

        public void CheckTriggers(Vector3 start, Vector3 end)
        {
            Vector2 A = new Vector2(
                start.x / DoomMapBuilder.SCALE.x,
                start.z / DoomMapBuilder.SCALE.z
            );
            Vector2 B = new Vector2(
                end.x / DoomMapBuilder.SCALE.x,
                end.z / DoomMapBuilder.SCALE.z
            );
            for (int i = 0; i < triggerList.Count; i++) 
            {
                if (triggerList[i] != null && triggerList[i].triggerType == TriggerType.Walk) {
                    Linedef line = map.linedefs[triggerList[i].linedefIndex];
                    Vector2 C = new Vector2(
                        map.vertices[line.start].x,
                        map.vertices[line.start].y
                    );
                    Vector2 D = new Vector2(
                        map.vertices[line.end].x,
                        map.vertices[line.end].y
                    );
                    if (LinesIntersect(A, B, C, D)) {
                        triggerList[i].Trigger();
                    }
                }
            }
        }

        private static bool LinesIntersect(Vector2 A, Vector2 B, Vector2 C, Vector2 D) {

			return (CCW(A,C,D) != CCW(B,C,D)) && (CCW(A,B,C) != CCW(A,B,D));
        }

        private static bool CCW(Vector2 A, Vector2 B, Vector2 C) {
			return ((C.y-A.y) * (B.x-A.x) > (B.y-A.y) * (C.x-A.x));
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

        public void RebuildSector(int sectorIndex, int floorHeight, int ceilingHeight)
        {
            Sector sector = map.sectors[sectorIndex];
            sector.floorHeight = floorHeight;
            sector.ceilingHeight = ceilingHeight;
            SectorObject so = sectorObjects[sectorIndex];
            Transform sectorTransform = sectors.GetChild(sectorIndex);
            ClearSectorLines(sectorIndex);

            for (int i = 0; i < so.lines.Length; i++) {
                var quads = BuildLine(so.lines[i]);
                for (int j = 0; j < quads.Length; j++) {
                    quads[j].transform.SetParent(lines.GetChild(so.lines[i]), false);
                }
            }
        }

        void ClearLine(int line) {
            Transform lineTransform = lines.GetChild(line);
            ClearTransform(lineTransform);
        }

        void ClearSectorLines(int sector) {
            Transform sectorTransform = sectors.GetChild(sector);
            SectorObject so = sectorObjects[sector];
            for (int i = 0; i < so.lines.Length; i++) {
                ClearLine(so.lines[i]);
            }
        }

        void ClearTransform(Transform transform) {
            for (int i = 0; i < transform.childCount; i++) {
                GameObject.Destroy(transform.GetChild(i).gameObject);
            }
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
                    // Debug.LogWarning($"Visible side with texture -");
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

        Mesh BuildTriggerMesh(int lineIndex)
        {
            int x1 = map.vertices[map.linedefs[lineIndex].start].x;
            int y1 = map.vertices[map.linedefs[lineIndex].start].y;
            int x2 = map.vertices[map.linedefs[lineIndex].end].x;
            int y2 = map.vertices[map.linedefs[lineIndex].end].y;
            Vector3[] vertices = new Vector3[] {
                new Vector3(x1, map.lowestHeight, y1),
                new Vector3(x1, map.tallestHeight, y1),
                new Vector3(x2, map.tallestHeight, y2),
                new Vector3(x2, map.lowestHeight, y2)
            };
            int[] triangles = new int[] {
                0, 1, 2, 0, 2, 3
            };
            Mesh mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            return mesh;
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
                        offset.y -= size.y;
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
                        offset.y += topHeight.y - bottomHeight.y;
                    } else {
                        // offset.y -= size.y;
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

