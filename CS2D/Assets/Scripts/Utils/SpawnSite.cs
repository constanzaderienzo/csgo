using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnSite
{

    public float minX, maxX, minZ, maxZ;

    public SpawnSite(float minX, float maxX, float minZ, float maxZ)
    {
        this.minX = minX;
        this.maxX = maxX;
        this.minZ = minZ;
        this.maxZ = maxZ;
    }
}
