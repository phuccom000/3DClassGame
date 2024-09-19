using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[HideInInspector]
[System.Serializable]
public class WorldData
{
    public string worldName = "Prototype";
    public int seed;

    public Dictionary<Vector2Int, ChunkData> chunks = new Dictionary<Vector2Int, ChunkData>();
    public ChunkData RequestChunk(Vector2Int coord)
    {
        if (chunks.ContainsKey(coord))
        {
            return chunks[coord];
        } else
        {
            return null;
        }
    }
}