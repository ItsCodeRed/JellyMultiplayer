using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    void FixedUpdate()
    {
        sbyte[] input = new sbyte[2];

        if (Input.GetKey(KeyCode.W))
        {
            input[1] += 1;
        }
        if (Input.GetKey(KeyCode.A))
        {
            input[0] -= 1;
        }
        if (Input.GetKey(KeyCode.S))
        {
            input[1] -= 1;
        }
        if (Input.GetKey(KeyCode.D))
        {
            input[0] += 1;
        }

        ClientSend.Input(input);
    }
}
