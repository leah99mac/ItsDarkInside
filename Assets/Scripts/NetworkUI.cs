using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class NetworkUI : MonoBehaviour
{

    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;


    void Start() {

        NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnect;
        
        hostButton.onClick.AddListener(() => {
            NetworkManager.Singleton.StartHost();
            gameObject.SetActive(false);
        });

        clientButton.onClick.AddListener(() => {
            NetworkManager.Singleton.StartClient();
            gameObject.SetActive(false);
        });

    }

    private void HandleClientDisconnect(ulong clientId) {
        if (clientId == NetworkManager.Singleton.LocalClientId) {
            gameObject.SetActive(true);
        }
    }
}
