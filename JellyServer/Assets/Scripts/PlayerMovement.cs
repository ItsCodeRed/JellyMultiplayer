using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
	public sbyte id;
	public float speed = 10;

	public void HandleInput(Vector2 input)
	{
		transform.position += (Vector3)input.normalized * speed * Time.fixedDeltaTime;
	}

	private void FixedUpdate()
	{
		ServerSend.Position(transform.position, id);
	}
}
