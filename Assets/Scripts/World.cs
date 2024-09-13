using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class World : MonoBehaviour
{
    public Material material;
    public BlockType[] blockTypes;

    Chunk[,] chunks = new Chunk[VoxelData.WorldSizeInChunk, VoxelData.WorldSizeInChunk];

    private void Start()
    {
        GenerateWorld();
    }

    void GenerateWorld()
    {
        for (int x = 0; x < VoxelData.WorldSizeInChunk; ++x)
        {
            for (int z = 0; z < VoxelData.WorldSizeInChunk; ++z)
            {
                chunks[x, z] = new Chunk(this, new ChunkCoord(x, z));
            }
        }
    }

}

[System.Serializable]
public class BlockType
{
    public string blockName;
    public bool isSolid;

    [Header("Texture Values")]
    public int backFaceTexture;
    public int frontFaceTexture;
    public int topFaceTexture;
    public int bottomFaceTexture;
    public int leftFaceTexture;
    public int rightFaceTexture;

    public int GetTextureID(int faceIndex)
    {
        switch (faceIndex)
        {
            case 0:
                return backFaceTexture;
            case 1:
                return backFaceTexture;
            case 2:
                return backFaceTexture;
            case 3:
                return backFaceTexture;
            case 4:
                return backFaceTexture;
            case 5:
                return backFaceTexture;
            default:
                Debug.Log("Error in getting Texture ID, out of index");
                return 0;
        }
    }
}