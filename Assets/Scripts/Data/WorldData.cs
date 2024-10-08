using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[HideInInspector]
[System.Serializable]
public class WorldData
{
    public string worldName = "Prototype";
    public int seed;

    // New fields to store player position as separate float values
    public float playerPosX;
    public float playerPosY;
    public float playerPosZ;
    public float playerRotX;
    public float playerRotY;
    public float playerRotZ;
    public float playerRotW;

    [System.NonSerialized]
    public Dictionary<Vector2Int, ChunkData> chunks = new Dictionary<Vector2Int, ChunkData>();

    [System.NonSerialized]
    public List<ChunkData> modifiedChunks = new List<ChunkData>();

    public WorldData(string _worldName, int _seed)
    {
        worldName = _worldName;
        seed = _seed;
        playerPosX = 0f;  // Default position X
        playerPosY = 10f; // Default position Y
        playerPosZ = 0f;  // Default position Z
        playerRotX = 0f;  // Default rotation X
        playerRotY = 0f;  // Default rotation Y
        playerRotZ = 0f;  // Default rotation Z
        playerRotW = 1f;  // Default rotation W (identity)
    }

    public WorldData(WorldData wD)
    {
        worldName = wD.worldName;
        seed = wD.seed;
        playerPosX = wD.playerPosX;
        playerPosY = wD.playerPosY;
        playerPosZ = wD.playerPosZ;
        playerRotX = wD.playerRotX;
        playerRotY = wD.playerRotY;
        playerRotZ = wD.playerRotZ;
        playerRotW = wD.playerRotW;
    }

    public void AddToModifiedChunkList(ChunkData chunk)
    {
        // Only add to list if ChunkData is not already in the list.
        if (!modifiedChunks.Contains(chunk))
            modifiedChunks.Add(chunk);
    }

    public ChunkData RequestChunk(Vector2Int coord, bool create)
    {
        ChunkData c;

        lock (World.Instance.ChunkListThreadLock)
        {
            if (chunks.ContainsKey(coord))
                c = chunks[coord];
            else if (!create)
                c = null;
            else
            {
                LoadChunk(coord);
                c = chunks[coord];
            }
        }

        return c;
    }

    public void LoadChunk(Vector2Int coord)
    {
        if (chunks.ContainsKey(coord))
            return;
        ChunkData chunk = SaveSystem.LoadChunk(worldName, coord);
        if (chunk != null)
        {
            chunks.Add(coord, chunk);
            return;
        }

        chunks.Add(coord, new ChunkData(coord));
        chunks[coord].Populate();
    }

    bool IsVoxelInWorld(Vector3 pos)
    {
        if (pos.x >= 0 && pos.x < VoxelData.WorldSizeInVoxels && pos.y >= 0 && pos.y < VoxelData.ChunkHeight && pos.z >= 0 && pos.z < VoxelData.WorldSizeInVoxels)
        {
            return true;
        }
        else return false;
    }

    public void SetVoxel(Vector3 pos, byte value, int direction)
    {
        // If the voxel is outside of the world we don't need to do anything with it.
        if (!IsVoxelInWorld(pos))
            return;

        // Find out the ChunkCoord value of our voxel's chunk.
        int x = Mathf.FloorToInt(pos.x / VoxelData.ChunkWidth);
        int z = Mathf.FloorToInt(pos.z / VoxelData.ChunkWidth);

        // Then reverse that to get the position of the chunk.
        x *= VoxelData.ChunkWidth;
        z *= VoxelData.ChunkWidth;

        // Check if the chunk exists. If not, create it.
        ChunkData chunk = RequestChunk(new Vector2Int(x, z), true);

        // Then create a Vector3Int with the position of our voxel *within* the chunk.
        Vector3Int voxel = new Vector3Int((int)(pos.x - x), (int)pos.y, (int)(pos.z - z));

        // Then set the voxel in our chunk.
        chunk.ModifyVoxel(voxel, value, direction);

        //AddToModifiedChunkList(chunk);
    }

    public VoxelState GetVoxel(Vector3 pos)
    {
        // If the voxel is outside of the world we don't need to do anything with it.
        if (!IsVoxelInWorld(pos))
            return null;

        // Find out the ChunkCoord value of our voxel's chunk.
        int x = Mathf.FloorToInt(pos.x / VoxelData.ChunkWidth);
        int z = Mathf.FloorToInt(pos.z / VoxelData.ChunkWidth);

        // Then reverse that to get the position of the chunk.
        x *= VoxelData.ChunkWidth;
        z *= VoxelData.ChunkWidth;

        // Check if the chunk exists. If not, create it.
        ChunkData chunk = RequestChunk(new Vector2Int(x, z), false);

        if (chunk == null)
        {
            return null;
        }

        // Then create a Vector3Int with the position of our voxel *within* the chunk.
        Vector3Int voxel = new Vector3Int((int)(pos.x - x), (int)pos.y, (int)(pos.z - z));

        // Then set the voxel in our chunk.
        return chunk.map[voxel.x, voxel.y, voxel.z];
    }
}
