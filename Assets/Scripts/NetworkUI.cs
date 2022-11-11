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

    [SerializeField] private Button toggleInterpolationButton;
    [SerializeField] private TMP_Text toggleInterpolationButtonText;
    DynamicInterpolatorFloat.InterpolationType interpolationType;

    private UnityTransport transport;

    void Start() {

        NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnect;

        transport = NetworkManager.Singleton.gameObject.GetComponent<UnityTransport>();
        
        SetToggleInterpolationActive(false);

        hostButton.onClick.AddListener(() => {
            NetworkManager.Singleton.StartHost();
            SetHostClientSelectionActive(false);
        });

        clientButton.onClick.AddListener(() => {
            transport.ConnectionData.Address = ipField?.text;
            Debug.Log("Connecting to " + transport.ConnectionData.Address);
            NetworkManager.Singleton.StartClient();
            SetHostClientSelectionActive(false);

            SetToggleInterpolationActive(true);
        });

        interpolationType = DynamicInterpolatorFloat.InterpolationType.LINEAR;

        toggleInterpolationButton.onClick.AddListener(() => {
            if (interpolationType == DynamicInterpolatorFloat.InterpolationType.CUBIC_SPLINE) {
                interpolationType = DynamicInterpolatorFloat.InterpolationType.LINEAR;
                toggleInterpolationButtonText.SetText("Interpolation: LINEAR");

            } else {
                interpolationType = DynamicInterpolatorFloat.InterpolationType.CUBIC_SPLINE;
                toggleInterpolationButtonText.SetText("Interpolation: CUBIC SPLINE");
            }

            // Set interpolation
            Object [] interps = GameObject.FindObjectsOfType(typeof(NetworkPositionTracker));
            foreach (NetworkPositionTracker interp in interps) {
                interp.interpolationType = interpolationType;
            }
        });

    }

    private void HandleClientDisconnect(ulong clientId) {
        if (clientId == NetworkManager.Singleton.LocalClientId) {
            SetHostClientSelectionActive(true);
            SetToggleInterpolationActive(false);
        }
    }

    private void SetHostClientSelectionActive(bool active) {
        hostButton.gameObject.SetActive(active);
        clientButton.gameObject.SetActive(active);
        ipField.gameObject.SetActive(active);
    }

    private void SetToggleInterpolationActive(bool active) {
        toggleInterpolationButton.gameObject.SetActive(active);
        toggleInterpolationButtonText.gameObject.SetActive(active);
    }
}
