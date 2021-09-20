using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class JoinMenu : MonoBehaviour
{
	public GameObject defaultCam;
	public TMP_InputField usernameField;

	public void Join()
	{
		Client.instance.username = usernameField.text;
		Client.instance.Connect(Client.instance.ip);

		defaultCam.SetActive(false);
		gameObject.SetActive(false);
	}
}
