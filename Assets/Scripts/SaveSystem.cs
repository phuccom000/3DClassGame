using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;

public static class SaveSystem
{
    public static void SaveWorld(WorldData world, Vector3 playerPosition, Quaternion playerRotation, out bool success)
    {
        success = false; // Default to false in case something goes wrong.

        // Set our save location and make sure we have a saves folder ready to go.
        string savePath = World.Instance.appPath + "/saves/" + world.worldName + "/";

        // If not, create it.
        if (!Directory.Exists(savePath))
            Directory.CreateDirectory(savePath);

        Debug.Log("Saving " + world.worldName);

        // Save player position and rotation
        world.playerPosX = playerPosition.x;
        world.playerPosY = playerPosition.y;
        world.playerPosZ = playerPosition.z;

        world.playerRotX = playerRotation.x;
        world.playerRotY = playerRotation.y;
        world.playerRotZ = playerRotation.z;
        world.playerRotW = playerRotation.w;

        try
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(savePath + "world.world", FileMode.Create);

            formatter.Serialize(stream, world);
            stream.Close();
        }
        catch (IOException e)
        {
            Debug.LogError("Error saving world data: " + e.Message);
            success = false;
            return;
        }
        bool temp = false;
        // Save chunks in a separate thread
        Thread thread = new Thread(() =>
        {
            bool chunksSaved = SaveChunks(world);
            if (chunksSaved)
            {
                Debug.Log("World saved successfully.");
                temp = true; // Mark success only if both world and chunks are saved.
            }
            else
            {
                Debug.LogError("Failed to save chunks.");
                temp = false;
            }
        });
        success = temp;
        thread.Start();
    }

    public static bool SaveChunks(WorldData world)
    {
        // Copy modified chunks into a new list and clear the old one to prevent
        // chunks being added to list while it is saving.
        List<ChunkData> chunks = new List<ChunkData>(world.modifiedChunks);
        world.modifiedChunks.Clear();

        int count = 0;

        try
        {
            // Loop through each chunk and save it.
            foreach (ChunkData chunk in chunks)
            {
                SaveSystem.SaveChunk(chunk, world.worldName);
                count++;
            }
            Debug.Log(count + " chunks saved.");
            return true; // Chunks saved successfully
        }
        catch (IOException e)
        {
            Debug.LogError("Error saving chunks: " + e.Message);
            return false; // Failed to save chunks
        }
    }

    public static WorldData LoadWorld(string worldName, out Vector3 playerPosition, out Quaternion playerRotation, out bool success, int seed = 0)
    {
        success = false; // Initialize as false.

        // Get the path to our world saves.
        string loadPath = World.Instance.appPath + "/saves/" + worldName + "/";

        // Check if a save exists for the name we were passed.
        if (File.Exists(loadPath + "world.world"))
        {
            Debug.Log(worldName + " found. Loading from save.");

            // If it does, load that file, deserialize it, and put it in a WorldData class for return.
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(loadPath + "world.world", FileMode.Open);

            // And then return the world.
            WorldData world = formatter.Deserialize(stream) as WorldData;
            stream.Close();

            // Set the player's position and rotation from the save data
            playerPosition = new Vector3(world.playerPosX, world.playerPosY, world.playerPosZ);
            playerRotation = new Quaternion(world.playerRotX, world.playerRotY, world.playerRotZ, world.playerRotW);

            success = true; // Loading succeeded
            return new WorldData(world);
        }
        else
        {
            Debug.Log(worldName + " not found. Creating new world.");
            seed = VoxelData.seed;
            WorldData world = new WorldData(worldName, seed);
            // Set default position and rotation
            playerPosition = new Vector3(VoxelData.WorldCentre, VoxelData.ChunkHeight - 50f, VoxelData.WorldCentre);            // Default position
            playerRotation = Quaternion.identity;    // Default rotation

            // Save the new world with the player's default position and rotation
            SaveWorld(world, playerPosition, playerRotation, out success);

            return world;
        }
    }

    public static void SaveChunk(ChunkData chunk, string worldName)
    {
        string chunkName = chunk.position.x + "-" + chunk.position.y;

        // Set our save location and make sure we have a saves folder ready to go.
        string savePath = World.Instance.appPath + "/saves/" + worldName + "/chunks/";

        // If not, create it.
        if (!Directory.Exists(savePath))
            Directory.CreateDirectory(savePath);

        BinaryFormatter formatter = new BinaryFormatter();
        FileStream stream = new FileStream(savePath + chunkName + ".chunk", FileMode.Create);

        formatter.Serialize(stream, chunk);
        stream.Close();
    }

    public static ChunkData LoadChunk(string worldName, Vector2Int position)
    {
        string chunkName = position.x + "-" + position.y;

        // Get the path to our world saves.
        string loadPath = World.Instance.appPath + "/saves/" + worldName + "/chunks/" + chunkName + ".chunk";

        // Check if a save exists for the name we were passed.
        if (File.Exists(loadPath))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(loadPath, FileMode.Open);

            ChunkData chunkData = formatter.Deserialize(stream) as ChunkData;
            stream.Close();

            return chunkData;
        }

        // If we didn't find the chunk in our folder, return null and our WorldData script
        // will make a new one.
        return null;
    }
}
