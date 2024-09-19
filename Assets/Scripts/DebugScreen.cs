using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class DebugScreen : MonoBehaviour
{
    World world;
    Text text;
    float frameRate;
    float timer;
    int halfWorldSizeInVoxels;
    int halfWorldSizeInChunks;
    void Start()
    {
        world = GameObject.Find("World").GetComponent<World>();
        text = GetComponent<Text>();
        halfWorldSizeInVoxels = VoxelData.WorldSizeInVoxels / 2;
        halfWorldSizeInChunks = VoxelData.WorldSizeInChunks / 2;
    }

    void Update()
    {
        string debugText = "Block by Block\n";
        debugText += frameRate + " FPS\n\n";
        debugText += "XYZ: " + (Mathf.FloorToInt(world.player.transform.position.x) - halfWorldSizeInVoxels) + " / " + Mathf.FloorToInt(world.player.transform.position.y) + " / " + (Mathf.FloorToInt(world.player.transform.position.z) - halfWorldSizeInVoxels);
        debugText += "\nChunk: " + (world.playerChunkCoord.x - halfWorldSizeInChunks) + " / " + (world.playerChunkCoord.z - halfWorldSizeInChunks);

        // Clip 27
        // -----------
        // string direction = "";
        // switch(world._player.orientation) {
        //     case 0:
        //         direction = "South";
        //         break;
        //     case 5: 
        //         direction = "East";
        //         break;
        //     case 1:
        //         direction = "North";
        //         break;
        //     default:
        //         direction = "West";
        //         break;
        // }
        // debugText += "\n";
        // debugText += "Direction Facing " + direction;
        // ------------

        text.text = debugText;

        if (timer > 1f)
        {
            frameRate = (int)(1f / Time.unscaledDeltaTime);
            timer = 0;
        }
        else
            timer += Time.deltaTime;

    }
}
