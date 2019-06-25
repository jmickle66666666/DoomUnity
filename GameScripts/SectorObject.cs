using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WadTools;

[SelectionBase]
public class SectorObject : MonoBehaviour
{
    public int[] lines;
    public int sector;

    public int initialCeilingPosition;
    public int initialFloorPosition;

    public float targetFloorHeight;
    public float targetCeilingHeight;
    public bool moving;
    public float moveSpeed = 0.025f;

    public Transform floor;
    public Transform ceiling;

    float scale = 64f/1.2f;



    public DoomMeshGenerator meshGenerator;

    void Start()
    {
        
    }

    void FixedUpdate()
    {
        if (moving) {

            if (Mathf.Abs(targetFloorHeight - floor.position.y) > moveSpeed) {
                floor.Translate(
                    0f,
                    Mathf.Sign(targetFloorHeight - floor.position.y) * moveSpeed,
                    0f
                );
            } else {
                floor.position = new Vector3(floor.position.x, targetFloorHeight, floor.position.z);

                if (Mathf.Abs(targetCeilingHeight - ceiling.position.y) > moveSpeed) {
                    ceiling.Translate(
                        0f,
                        Mathf.Sign(targetCeilingHeight - ceiling.position.y) * moveSpeed,
                        0f
                    );
                } else {
                    ceiling.position = new Vector3(ceiling.position.x, targetCeilingHeight, ceiling.position.z);
                    moving = false;
                }
            }
            
            meshGenerator.RebuildSector(
                sector, 
                PositionToSectorHeight(floor.position.y, initialFloorPosition),
                PositionToSectorHeight(ceiling.position.y, initialCeilingPosition)
            );
        } else {
            if (sector == 6) {
                if (Input.GetKeyDown(KeyCode.G)) {
                    moving = true;
                    targetCeilingHeight = -2f;
                }
            }
        }
    }

    int PositionToSectorHeight(float height, int initial)
    {
        int offset = Mathf.RoundToInt(height * scale);
        return initial + offset;
    }

    float SectorHeightToPosition(int height, int initial)
    {

        return (float)(height-initial) / scale;
    }
}
