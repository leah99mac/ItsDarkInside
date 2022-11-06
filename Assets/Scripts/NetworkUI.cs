using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using TMPro;

public class NetworkUI : MonoBehaviour
{

    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;
    [SerializeField] private TMP_InputField ipField;

    private UnityTransport transport;

    void Start() {

        NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnect;

        transport = NetworkManager.Singleton.gameObject.GetComponent<UnityTransport>();
        
        hostButton.onClick.AddListener(() => {
            NetworkManager.Singleton.StartHost();
            gameObject.SetActive(false);
        });

        clientButton.onClick.AddListener(() => {
            transport.ConnectionData.Address = ipField?.text;
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
