using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkLoadAnimation : MonoBehaviour
{
    float speed = 3f;
    Vector3 targetPos;
    float timer;
    float waitTimer;
    // Start is called before the first frame update
    void Start()
    {
        waitTimer = Random.Range(0f, 3f);
        targetPos = transform.position;
        transform.position = new Vector3(transform.position.x, -VoxelData.ChunkHeight,transform.position.z);
    }

    // Update is called once per frame
    void Update()
    {
        if (timer < waitTimer) {
            timer += Time.deltaTime;
        } else
        {
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * speed);
            if ((targetPos.y - transform.position.y) < 0.05f) {
                transform.position = targetPos;
                Destroy(this);
            }
        }
    }
}
