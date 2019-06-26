using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory
{
    public int health;
    public int armor;

    public bool redKeyCard = false;   
    public bool blueKeyCard = false;   
    public bool yellowKeyCard = false;   
    public bool redSkullKey = false;   
    public bool blueSkullKey = false;   
    public bool yellowSkullKey = false; 

    public bool shotgun = false;
    public bool superShotgun = false;
    public bool chaingun = false;
    public bool rocketLauncher = false;
    public bool plasmaRifle = false;
    public bool BFG9000 = false;

    public int bullets;
    public int shells;
    public int rockets;
    public int cells; 

    // Reset all values that change between levels
    public void LevelReset()
    {
        redKeyCard = false;
        blueKeyCard = false;
        yellowKeyCard = false;
        redSkullKey = false;
        blueSkullKey = false;
        yellowSkullKey = false;
    }

    // Reset all values that change for a new game
    public void FullReset()
    {
        health = 100;
        armor = 0;

        bullets = 50;
        shells = 0;
        rockets = 0;
        cells = 0;

        redKeyCard = false;
        blueKeyCard = false;
        yellowKeyCard = false;
        redSkullKey = false;
        blueSkullKey = false;
        yellowSkullKey = false;

        shotgun = false;
        superShotgun = false;
        chaingun = false;
        rocketLauncher = false;
        plasmaRifle = false;
        BFG9000 = false;
    }
}
