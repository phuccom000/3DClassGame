using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BiomeAttributes", menuName = "BlockByBlock/Biome Attribute")]
public class BiomeAttributes : ScriptableObject
{

    public string biomeName;

    public int solidGroundHeight;
    public int terrainHeight;
    public float terrainScale;

    public Lode[] lodes;

}

[System.Serializable]
public class Lode //ore detail
{

    public string nodeName;
    public byte blockID;
    public int minHeight; //height at where ores can spawn
    public int maxHeight;
    public float scale; //bigger the scale smaller the sample size
    public float threshold;
    public float noiseOffset;


}
