using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
	public Dictionary<sbyte, GameObject> players = new Dictionary<sbyte, GameObject>();
	public List<GameObject> connectedPlayers = new List<GameObject>();

	public static GameManager instance;
	public GameObject localPlayerPrefab;
	public GameObject playerPrefab;
	public Transform spawn;

	public void Awake()
	{
		// Singleton implementation
		if (instance == null)
		{
			instance = this;
			DontDestroyOnLoad(this);
		}
		else
		{
			Debug.LogWarning($"Two gamemanager objects have been created. Destroying this ({name})!");
			Destroy(gameObject);
		}
	}

	public GameObject SpawnLocal()
	{
		GameObject player = Instantiate(localPlayerPrefab, spawn.position, spawn.rotation);
		player.GetComponentInChildren<TMP_Text>().text = Client.instance.username;
		players.Add(Client.instance.id, player);
		connectedPlayers.Add(player);
		return player;
	}

	public GameObject Spawn(sbyte id, string username)
	{
		GameObject player = Instantiate(playerPrefab, spawn.position, spawn.rotation);
		player.GetComponentInChildren<TMP_Text>().text = username;
		players.Add(id, player);
		connectedPlayers.Add(players[id]);
		return player;
	}

	public void Despawn(sbyte id)
	{
		connectedPlayers.Remove(players[id]);
		Destroy(players[id]);
		players.Remove(id);
	}
}
