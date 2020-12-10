using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public float moveSpeed = 2f;
    float hMove;
    float vMove;
    Vector3 motion;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        hMove = Input.GetAxis("Horizontal");
        vMove = Input.GetAxis("Vertical");

        motion = new Vector3(hMove, 0f, vMove).normalized * Time.deltaTime * moveSpeed;

        transform.position = transform.position + motion;
    }
}
