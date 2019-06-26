using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WadTools;
using System;

public enum TriggerType
{
    Use,
    Walk,
    Shoot,
    None
}

public class LinedefTrigger : MonoBehaviour
{
    public int linedefIndex;
    public int sectorTag;
    public int specialType;
    public DoomMeshGenerator doomMesh;
    Action timerDone;
    float timer;
    public bool repeatable;
    MapData map;
    public TriggerType triggerType;

    // Just until all actions are implemented
    bool didAction = false;

    static float platSlow = 1f/64f;
    static float doorSlow = 2f/64f;
    static float platFast = 4f/64f;
    static float doorFast = 8f/64f;

    static int[] repeatableActions = {
        1, 26, 27, 28, 42, 43, 45, 46, 60, 61, 62, 63, 64, 65, 66, 67, 68, 69, 70, 72, 73, 74, 75, 76, 77, 78, 79, 80, 81, 82, 83, 84, 86, 87, 88, 89, 90, 91, 92, 93, 94, 95, 96, 97, 98, 99, 105, 106, 107, 114, 115, 116, 117, 120, 123, 126, 128, 129, 132, 134, 136, 138, 139
    };

    static int[] useActions = {
        1, 7, 9, 11, 14, 15, 18, 20, 21, 23, 26, 27, 28, 29, 31, 32, 33, 34, 41, 42, 43, 45, 49, 50, 51, 55, 60, 61, 62, 63, 64, 65, 66, 67, 68, 69, 70, 71, 78, 99, 101, 102, 103, 111, 112, 113, 114, 115, 116, 117, 118, 122, 123, 127, 131, 132, 133, 134, 135, 136, 137, 138, 139, 140, 158, 159, 160, 161, 162, 163, 164, 165, 166, 167, 168, 169, 170, 171, 172, 173, 174, 175, 176, 177, 178, 179, 180, 181, 182, 183, 184, 185, 186, 187, 188, 189, 190, 191, 192, 193, 194, 195, 196, 203, 204, 205, 206, 209, 210, 211, 221, 222, 229, 230, 233, 234, 237, 238, 241, 258, 259, 276, 277, 349, 434, 435
    };

    static int[] walkActions = {
        2, 3, 4, 5, 6, 8, 10, 12, 13, 16, 17, 19, 22, 25, 30, 35, 36, 37, 38, 39, 40, 44, 52, 53, 54, 56, 57, 58, 59, 72, 73, 74, 75, 76, 77, 79, 80, 81, 82, 83, 84, 86, 87, 88, 89, 90, 91, 92, 93, 94, 95, 96, 97, 98, 100, 104, 105, 106, 107, 108, 109, 110, 119, 120, 121, 124, 125, 126, 128, 129, 130, 141, 142, 143, 144, 145, 146, 147, 148, 149, 150, 151, 152, 153, 154, 155, 156, 157, 199, 200, 201, 202, 207, 208, 212, 219, 220, 227, 228, 231, 232, 235, 236, 239, 240, 243, 244, 256, 257, 262, 263, 264, 265, 266, 267, 268, 269, 270, 273, 274, 275, 338, 339, 348, 436, 437
    };

    public void Init()
    {
        repeatable = System.Array.IndexOf(repeatableActions, specialType) >= 0;

        if (System.Array.IndexOf(useActions, specialType) >= 0) {
            triggerType = TriggerType.Use;
        } else if (System.Array.IndexOf(walkActions, specialType) >= 0) {
            triggerType = TriggerType.Walk;
        }

        map = doomMesh.map;
    }

    void Update()
    {
        if (timerDone != null) {
            if (timer > 0f) {
                timer -= Time.deltaTime;
            } else {
                timerDone();
                timerDone = null;
                if (!repeatable) Destroy(gameObject);
            }
        }
    }
    
    public void Trigger()
    {
        if (timerDone != null) return;
        didAction = false;

        if (specialType == 1) Door(doorSlow, 4f);
        if (specialType == 2) Door(doorSlow);
        if (specialType == 11) LevelExit();
        if (specialType == 20) Floor(map.NextHighestFloor, platSlow);
        if (specialType == 26 && GameSetup.main.playerInventory.blueKeyCard) Door(doorSlow, 4f);
        if (specialType == 27 && GameSetup.main.playerInventory.yellowKeyCard) Door(doorSlow, 4f);
        if (specialType == 28 && GameSetup.main.playerInventory.redKeyCard) Door(doorSlow, 4f);
        if (specialType == 31) Door(doorSlow);
        if (specialType == 32 && GameSetup.main.playerInventory.blueKeyCard) Door(doorSlow);
        if (specialType == 33 && GameSetup.main.playerInventory.redKeyCard) Door(doorSlow);
        if (specialType == 34 && GameSetup.main.playerInventory.yellowKeyCard) Door(doorSlow);
        if (specialType == 61) Door(doorSlow);
        if (specialType == 62) Floor(map.LowestAdjecentFloor, platFast, 3f);
        if (specialType == 97) Teleport();
        if (specialType == 99 && GameSetup.main.playerInventory.blueKeyCard) Door(doorFast);
        if (specialType == 102) Floor(map.HighestAdjecentFloor, platSlow);
        if (specialType == 103) Door(doorSlow);
        if (specialType == 109) Door(doorFast);
        if (specialType == 114) Door(doorFast, 4f);
        if (specialType == 117) Door(doorFast, 4f);
        if (specialType == 120) Floor(map.LowestAdjecentFloor, platFast, 3f);
        if (specialType == 121) Floor(map.LowestAdjecentFloor, platFast, 3f);
        if (specialType == 122) Floor(map.LowestAdjecentFloor, platFast, 3f);
        if (specialType == 123) Floor(map.LowestAdjecentFloor, platFast, 3f);
        if (specialType == 133 && GameSetup.main.playerInventory.blueKeyCard) Door(doorFast);
        if (specialType == 134 && GameSetup.main.playerInventory.redKeyCard) Door(doorFast);
        if (specialType == 135 && GameSetup.main.playerInventory.redKeyCard) Door(doorFast);
        if (specialType == 136 && GameSetup.main.playerInventory.yellowKeyCard) Door(doorFast);
        if (specialType == 137 && GameSetup.main.playerInventory.yellowKeyCard) Door(doorFast);


        if (!didAction) {
            Debug.Log($"Linedef action not implemented: {specialType}!");
        }

        if (!repeatable && timerDone == null) Destroy(gameObject);
    }

    // Linedef actions
    void Door(float speed, float wait = -1f) {
        int[] sectorIndex;
        if (sectorTag == 0) {
            sectorIndex = new int[1]{map.GetLineDoorSector(linedefIndex)};
        } else {
            sectorIndex = map.GetSectorsWithTag(sectorTag);
        } 
        for (int i = 0; i < sectorIndex.Length; i++) {
            int openHeight = map.LowestAdjacentCeiling(sectorIndex[i]) - 4;
            doomMesh.sectorObjects[sectorIndex[i]].SetCeilingHeight(openHeight);
            doomMesh.sectorObjects[sectorIndex[i]].moveSpeed = speed;
        }

        if (wait > 0f) {
            timerDone = () => {
                for (int i = 0; i < sectorIndex.Length; i++) {
                    doomMesh.sectorObjects[sectorIndex[i]].SetCeilingHeight(map.sectors[sectorIndex[i]].floorHeight);
                }
            };
            timer = wait;
        }

        didAction = true;
    }

    void Floor(Func<int, int> targetHeight, float speed, float wait = -1f) { Plat(true, targetHeight, speed, wait); }
    void Ceiling(Func<int, int> targetHeight, float speed, float wait = -1f) { Plat(false, targetHeight, speed, wait); }

    void Plat(bool floor, Func<int, int> targetHeight, float speed, float wait = -1f) {
        var sectorIndex = map.GetSectorsWithTag(sectorTag);
        for (int i = 0; i < sectorIndex.Length; i++) {
            doomMesh.sectorObjects[sectorIndex[i]].moveSpeed = speed;
            if (floor) {
                doomMesh.sectorObjects[sectorIndex[i]].SetFloorHeight(targetHeight(sectorIndex[i]));
            } else {
                doomMesh.sectorObjects[sectorIndex[i]].SetCeilingHeight(targetHeight(sectorIndex[i]));
            }
        }

        if (wait > 0f) {
            timerDone = () => {
                for (int i = 0; i < sectorIndex.Length; i++) {
                    if (floor) {
                        doomMesh.sectorObjects[sectorIndex[i]].ResetFloorHeight();
                    } else {
                        doomMesh.sectorObjects[sectorIndex[i]].ResetCeilingHeight();
                    }
                }
            };
            timer = wait;
        }

        didAction = true;
    }

    void LevelExit()
    {
        GameSetup.main.NextMap();
        didAction = true;
    }

    void Teleport()
    {
        for (int i = 0; i < map.things.Length; i++) {
            if (map.things[i].type == 14) {
                int thingSector = doomMesh.nodeTri.ThingSector(map.things[i]);
                var sectors = map.GetSectorsWithTag(sectorTag);
                for (int j = 0; j < sectors.Length; j++) {
                    if (thingSector == sectors[j]) {
                        Vector3 position = new Vector3(
                            map.things[i].x * DoomMapBuilder.SCALE.x,
                            map.sectors[thingSector].floorHeight * DoomMapBuilder.SCALE.y,
                            map.things[i].y * DoomMapBuilder.SCALE.z
                        );
                        GameSetup.main.player.transform.position = position;

                        GameSetup.main.player.transform.localEulerAngles = new Vector3(
                            0f,
                            90f - map.things[i].angle,
                            0f
                        );

                        didAction = true;
                        return;
                    }
                }
            }
        }
        
    }
}