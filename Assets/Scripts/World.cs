using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class World : MonoBehaviour
{
    public Settings settings;
    [Header("World Generation Values")]
    public BiomeAttributes[] biomes;

    [Range(0f, 1f)]
    public float globalLightLevel;
    public Color day;
    public Color night;

    public Transform player;
    public Player _player;
    public Quaternion playerRotation = default;
    public Vector3 spawnPosition = new Vector3(VoxelData.WorldCentre, VoxelData.ChunkHeight - 50f, VoxelData.WorldCentre);
    public Material material;
    public Material transparentMaterial;
    public Material waterMaterial;
    public BlockType[] blockTypes;

    Chunk[,] chunks = new Chunk[VoxelData.WorldSizeInChunks, VoxelData.WorldSizeInChunks];

    List<ChunkCoord> activeChunks = new List<ChunkCoord>();
    public ChunkCoord playerChunkCoord;
    ChunkCoord playerLastChunkCoord;

    public List<Chunk> chunksToUpdate = new List<Chunk>();
    public Queue<Chunk> chunksToDraw = new Queue<Chunk>();

    bool applyingModifications = false;
    Queue<Queue<VoxelMod>> modifications = new Queue<Queue<VoxelMod>>();

    private bool _inUI = false;
    public Clouds clouds;

    public GameObject debugScreen;

    public GameObject creativeInventoryWindow;

    public Text statusText;
    private bool saved;
    public GameObject cursorSlot;

    Thread ChunkUpdateThread;
    public object ChunkUpdateThreadLock = new Object();
    public object ChunkListThreadLock = new object();

    private static World _instance;
    public static World Instance { get { return _instance; } }

    public WorldData worldData;
    public string appPath;

    private void Awake()
    {
        // If the instance value is not null and not *this*, we've somehow ended up with more than one World component.
        // Since another one has already been assigned, delete this one.
        if (_instance != null && _instance != this)
            Destroy(this.gameObject);
        // Else set this to the instance.
        else
            _instance = this;

        appPath = Application.persistentDataPath;

        _player = player.GetComponent<Player>();
    }
    private void Start()
    {
        worldData = SaveSystem.LoadWorld("Testing", out spawnPosition, out playerRotation, out saved);
        Debug.Log("World is generated with the seed: " + VoxelData.seed);
        statusText.color = new Color(statusText.color.r, statusText.color.g, statusText.color.b, 0);

        string JsonImport = File.ReadAllText(Application.dataPath + "/settings.cfg");
        settings = JsonUtility.FromJson<Settings>(JsonImport);

        Random.InitState(VoxelData.seed);


        Shader.SetGlobalFloat("minGlobalLightLevel", VoxelData.minLightLevel);
        Shader.SetGlobalFloat("maxGlobalLightLevel", VoxelData.maxLightLevel);

        LoadWorld();

        SetGlobalLightValue();
        player.position = spawnPosition;
        player.rotation = playerRotation;
        CheckViewDistance();
        playerLastChunkCoord = GetChunkCoordFromVector3(player.position);

        if (settings.enableThreading)
        {
            ChunkUpdateThread = new Thread(new ThreadStart(ThreadedUpdate));
            ChunkUpdateThread.Start();
        }
    }

    public void SetGlobalLightValue()
    {

        Shader.SetGlobalFloat("GlobalLightLevel", globalLightLevel);
        Camera.main.backgroundColor = Color.Lerp(night, day, globalLightLevel);

    }

    private void Update()
    {

        playerChunkCoord = GetChunkCoordFromVector3(player.position);

        // Only update the chunks if the player has moved from the chunk they were previously on.
        if (!playerChunkCoord.Equals(playerLastChunkCoord))
            CheckViewDistance();

        if (chunksToDraw.Count > 0)
        {
            chunksToDraw.Dequeue().CreateMesh();
        }

        if (!settings.enableThreading)
        {
            if (!applyingModifications)
                ApplyModifications();

            if (chunksToUpdate.Count > 0)
                UpdateChunks();
        }

        if (Input.GetKeyDown(KeyCode.F3))
            debugScreen.SetActive(!debugScreen.activeSelf);

        if (Input.GetKeyDown(KeyCode.F1))
        {
            SetTextAndFadeOut("Currently saving.");
            SaveSystem.SaveWorld(worldData, spawnPosition, playerRotation, out saved);
            if (saved) SetTextAndFadeOut("Finished saving.");
        }


        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SetTextAndFadeOut("Saving before quitting.");
            SaveSystem.SaveWorld(worldData, spawnPosition, playerRotation, out saved);
            if (saved)
                Application.Quit();
        }

    }


    void LoadWorld()
    {
        for (int x = (VoxelData.WorldSizeInChunks / 2) - settings.loadDistance; x < (VoxelData.WorldSizeInChunks / 2) + settings.loadDistance; x++)
        {
            for (int z = (VoxelData.WorldSizeInChunks / 2) - settings.loadDistance; z < (VoxelData.WorldSizeInChunks / 2) + settings.loadDistance; z++)
            {
                worldData.LoadChunk(new Vector2Int(x, z));
            }
        }
    }

    public void AddChunkToUpdate(Chunk chunk)
    {
        AddChunkToUpdate(chunk, false);
    }

    public void AddChunkToUpdate(Chunk chunk, bool insert)
    {
        // Lock list to ensure only one thing is using the list at a time
        lock (ChunkUpdateThreadLock)
        {
            // Make sure update list doesn't already contain chunk
            if (!chunksToUpdate.Contains(chunk))
            {
                if (insert)
                {
                    chunksToUpdate.Insert(0, chunk);
                }
                else
                {
                    chunksToUpdate.Add(chunk);
                }
            }
        }
    }

    void UpdateChunks()
    {
        lock (ChunkUpdateThreadLock)
        {
            chunksToUpdate[0].UpdateChunk();
            if (!activeChunks.Contains(chunksToUpdate[0].coord))
                activeChunks.Add(chunksToUpdate[0].coord);
            chunksToUpdate.RemoveAt(0);
        }
    }

    void ThreadedUpdate()
    {
        while (true)
        {
            if (!applyingModifications)
                ApplyModifications();

            if (chunksToUpdate.Count > 0)
                UpdateChunks();
        }
    }

    private void OnDisable()
    {
        if (settings.enableThreading)
            ChunkUpdateThread.Abort();
    }

    void ApplyModifications()
    {
        applyingModifications = true;
        while (modifications.Count > 0)
        {
            Queue<VoxelMod> queue = modifications.Dequeue();

            while (queue.Count > 0)
            {
                VoxelMod v = queue.Dequeue();

                worldData.SetVoxel(v.position, v.id, 1);
            }
        }
        applyingModifications = false;
    }

    ChunkCoord GetChunkCoordFromVector3(Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x / VoxelData.ChunkWidth);
        int z = Mathf.FloorToInt(pos.z / VoxelData.ChunkWidth);
        return new ChunkCoord(x, z);
    }
    public Chunk GetChunkFromVector3(Vector3 pos)
    {

        int x = Mathf.FloorToInt(pos.x / VoxelData.ChunkWidth);
        int z = Mathf.FloorToInt(pos.z / VoxelData.ChunkWidth);
        return chunks[x, z];

    }

    void CheckViewDistance()
    {
        clouds.UpdateClouds();

        ChunkCoord coord = GetChunkCoordFromVector3(player.position);
        playerLastChunkCoord = playerChunkCoord;

        List<ChunkCoord> prevActiveChunks = new List<ChunkCoord>(activeChunks);
        activeChunks.Clear();

        for (int x = coord.x - settings.viewDistance; x < coord.x + settings.viewDistance; x++)
        {
            for (int z = coord.z - settings.viewDistance; z < coord.z + settings.viewDistance; z++)
            {
                ChunkCoord thisChunkCoord = new ChunkCoord(x, z);
                if (IsChunkInWorld(thisChunkCoord))
                {
                    if (chunks[x, z] == null)
                    {
                        chunks[x, z] = new Chunk(thisChunkCoord);
                    }
                    chunks[x, z].isActive = true;
                    activeChunks.Add(thisChunkCoord);
                }
                for (int i = 0; i < prevActiveChunks.Count; i++)
                {
                    if (prevActiveChunks[i].Equals(thisChunkCoord))
                        prevActiveChunks.RemoveAt(i);
                }
            }
            foreach (ChunkCoord c in prevActiveChunks)
                chunks[c.x, c.z].isActive = false;
        }
    }
    public bool CheckForVoxel(Vector3 pos)
    {

        VoxelState voxel = worldData.GetVoxel(pos);

        if (voxel.properties.isSolid)
            return true;
        else
            return false;

    }

    public VoxelState GetVoxelState(Vector3Int pos)
    {
        return worldData.GetVoxel(pos);
    }

    public bool inUI
    {
        get { return _inUI; }
        set
        {
            _inUI = value;
            if (_inUI)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                creativeInventoryWindow.SetActive(true);
                cursorSlot.SetActive(true);
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                creativeInventoryWindow.SetActive(false);
                cursorSlot.SetActive(false);
            }
        }
    }

    public byte GetVoxel(Vector3 pos)
    {
        int yPos = Mathf.FloorToInt(pos.y);
        /*immutable pass*/

        // if outside world, return air
        if (!IsVoxelInWorld(pos))
            return 0;
        // if bottom block of chunk, return bedrock
        if (yPos == 0) return 1;

        /*biome selection pass*/
        int solidGroundHeight = 42;
        float sumOfHeights = 0f;
        int count = 0;
        float strongestWeight = 0f;
        int strongestBiomeIndex = 0;

        for (int i = 0; i < biomes.Length; i++)
        {

            float weight = Noise.Get2DPerlin(new Vector2(pos.x, pos.z), biomes[i].offset, biomes[i].scale);

            // Keep track of which weight is strongest.
            if (weight > strongestWeight)
            {

                strongestWeight = weight;
                strongestBiomeIndex = i;

            }

            // Get the height of the terrain (for the current biome) and multiply it by its weight.
            float height = biomes[i].terrainHeight * Noise.Get2DPerlin(new Vector2(pos.x, pos.z), 0, biomes[i].terrainScale) * weight;

            // If the height value is greater 0 add it to the sum of heights.
            if (height > 0)
            {

                sumOfHeights += height;
                count++;

            }

        }

        // Set biome to the one with the strongest weight.
        BiomeAttributes biome = biomes[strongestBiomeIndex];

        // Get the average of the heights.
        sumOfHeights /= count;

        int terrainHeight = Mathf.FloorToInt(sumOfHeights + solidGroundHeight);


        //BiomeAttributes biome = biomes[index];


        /*basic terrain pass*/
        byte voxelValue = 0;

        if (yPos == terrainHeight)
            voxelValue = biome.surfaceBlock;
        else if (yPos < terrainHeight && yPos > terrainHeight - 4)
            voxelValue = biome.subSurfaceBlock;
        else if (yPos > terrainHeight)
        {
            if (yPos < 51)
            {
                return 14;
            }
            else
            {
                return 0;
            }
        }
        else
            voxelValue = 2;
        /*second pass*/
        if (voxelValue == 2)
        {
            foreach (Lode lode in biome.lodes)
            {
                if (yPos > lode.minHeight && yPos < lode.maxHeight)
                    if (Noise.Get3DPerlin(pos, lode.noiseOffset, lode.scale, lode.threshold))
                        voxelValue = lode.blockID;
            }
        }

        /*tree pass*/
        if (yPos == terrainHeight && biome.placeMajorFlora)
        {
            if (Noise.Get2DPerlin(new Vector2(pos.x, pos.z), 0, biome.majorFloraZoneScale) > biome.majorFloraZoneThreshold)
            {
                if (Noise.Get2DPerlin(new Vector2(pos.x, pos.z), 0, biome.majorFloraPlacementScale) > biome.majorFloraPlacementThreshold)
                {
                    modifications.Enqueue(Structure.GenerateMajorFlora(biome.majorFloraIndex, pos, biome.minHeight, biome.maxHeight));
                }
            }
        }
        return voxelValue;
    }



    bool IsChunkInWorld(ChunkCoord coord)
    {
        if (coord.x > 0 && coord.x < VoxelData.WorldSizeInChunks - 1 && coord.z > 0 && coord.z < VoxelData.WorldSizeInChunks - 1)
            return true;
        else
            return false;
    }
    bool IsVoxelInWorld(Vector3 pos)
    {
        if (pos.x >= 0 && pos.x < VoxelData.WorldSizeInVoxels && pos.y >= 0 && pos.y < VoxelData.ChunkHeight && pos.z >= 0 && pos.z < VoxelData.WorldSizeInVoxels)
            return true;
        else
            return false;

    }

    // Function to set the text and start fading out
    public void SetTextAndFadeOut(string message, float fadeDuration = 2f)
    {
        statusText.text = message;
        statusText.color = new Color(statusText.color.r, statusText.color.g, statusText.color.b, 1);  // Ensure the alpha is fully visible
        StartCoroutine(FadeOutText(fadeDuration));
    }

    // Coroutine to handle the fade-out process
    private IEnumerator FadeOutText(float duration)
    {
        Color originalColor = statusText.color;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / duration);  // Gradually reduce alpha
            statusText.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            yield return null;
        }

        // Ensure the text is fully invisible at the end
        statusText.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0);
    }
}

[System.Serializable]
public class BlockType
{
    public string blockName;
    public bool isSolid;
    public VoxelMeshData meshData;
    public bool renderNeighborFaces;
    public bool isWater;
    public byte opacity;
    public Sprite icon;

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
                return frontFaceTexture;
            case 2:
                return topFaceTexture;
            case 3:
                return bottomFaceTexture;
            case 4:
                return leftFaceTexture;
            case 5:
                return rightFaceTexture;
            default:
                Debug.Log("Error in getting Texture ID, out of index");
                return 0;
        }
    }
}


public class VoxelMod
{
    public Vector3 position;
    public byte id;

    public VoxelMod()
    {
        position = new Vector3();
        id = 0;
    }

    public VoxelMod(Vector3 _position, byte _id)
    {
        position = _position;
        id = _id;
    }

}

[System.Serializable]
public class Settings
{

    [Header("Game Data")]
    public string version = "0.0.01";

    [Header("Performance")]
    public int loadDistance = 16;
    public int viewDistance = 8;
    public bool enableThreading = true;
    public CloudStyle clouds = CloudStyle.Fast;
    public bool enableAnimatedChunks = false;

    [Header("Controls")]
    [Range(0.1f, 10f)]
    public float mouseSensitivity = 5.0f;

    [Header("Game Details")]
    public bool isCreativeMode = false;
    //public int difficulty;


}