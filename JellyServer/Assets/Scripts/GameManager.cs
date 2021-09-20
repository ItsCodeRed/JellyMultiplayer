using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
	public static GameManager instance;
	public GameObject playerPrefab;
	public Transform spawn;

	public void Awake()
	{
		// Singleton implementation
		if (instance == null)
		{
			instance = this;
		}
		else
		{
			Debug.LogWarning($"Two gamemanager objects have been created. Destroying this ({name})!");
			Destroy(gameObject);
		}
	}

	public GameObject Spawn(sbyte id)
	{
		GameObject player = Instantiate(playerPrefab, spawn.position, spawn.rotation);
		player.GetComponent<PlayerMovement>().id = id;
		return player;
	}

	public void Despawn(GameObject player)
	{
		Destroy(player);
	}
}
