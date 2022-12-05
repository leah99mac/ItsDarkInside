using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine.SceneManagement;

public class HandleDisconnect : MonoBehaviour
{

    private void Start() {
        NetworkManager.Singleton.OnClientDisconnectCallback += CloseServerAndLoadMainMenu;
    }

    private void CloseServerAndLoadMainMenu(ulong clientId) {
        NetworkManager.Singleton.Shutdown();
        SceneManager.LoadScene("MainMenu");
    }
}
