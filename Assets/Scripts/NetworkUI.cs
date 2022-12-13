using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using TMPro;

public class NetworkUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject mainMenu = null;
    [SerializeField] private GameObject game1 = null;

    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;
    [SerializeField] private TMP_InputField ipField;

    [SerializeField] private Button toggleInterpolationButton;
    [SerializeField] private TMP_Text toggleInterpolationButtonText;
    bool interpolate;

    [SerializeField] private Button toggleInterpolationTypeButton;
    [SerializeField] private TMP_Text toggleInterpolationTypeButtonText;
    DynamicInterpolatorFloat.InterpolationType interpolationType;

    private UnityTransport transport;

    void Start() {

        NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnect;

        transport = NetworkManager.Singleton.gameObject.GetComponent<UnityTransport>();
        
        SetToggleInterpolationActive(false);

        hostButton.onClick.AddListener(() => {
            NetworkManager.Singleton.StartHost();
            mainMenu.SetActive(false);
            game1.SetActive(true);
            SetHostClientSelectionActive(false);
        });

        clientButton.onClick.AddListener(() => {
            transport.ConnectionData.Address = ipField?.text;
            Debug.Log("Connecting to " + transport.ConnectionData.Address);
            NetworkManager.Singleton.StartClient();
            mainMenu.SetActive(false);
            game1.SetActive(true);
            SetHostClientSelectionActive(false);
            SetToggleInterpolationActive(true);
        });

        interpolationType = DynamicInterpolatorFloat.InterpolationType.LINEAR;

        toggleInterpolationTypeButton.onClick.AddListener(() => {
            if (interpolationType == DynamicInterpolatorFloat.InterpolationType.CUBIC_SPLINE) {
                interpolationType = DynamicInterpolatorFloat.InterpolationType.LINEAR;
                toggleInterpolationTypeButtonText.SetText("Interpolation: LINEAR");
            } else {
                interpolationType = DynamicInterpolatorFloat.InterpolationType.CUBIC_SPLINE;
                toggleInterpolationTypeButtonText.SetText("Interpolation: CUBIC SPLINE");
            }

            // Set interpolation
            Object [] interps = GameObject.FindObjectsOfType(typeof(NetworkPositionTracker));
            foreach (NetworkPositionTracker interp in interps) {
                interp.interpolationType = interpolationType;
            }
        });

        toggleInterpolationButton.onClick.AddListener(() => {
            if (interpolate) {
                toggleInterpolationButtonText.SetText("Interpolation: OFF");
                interpolate = false;
            } else {
                toggleInterpolationButtonText.SetText("Interpolation: ON");
                interpolate = true;
            }

            // Set interpolation
            Object [] interps = GameObject.FindObjectsOfType(typeof(NetworkPositionTracker));
            foreach (NetworkPositionTracker interp in interps) {
                interp.interpolate = interpolate;
            }
        });

    }

    private void HandleClientDisconnect(ulong clientId) {
        ResetButtons();
        gameObject.AddComponent<ToMain>();
    }

    private void SetHostClientSelectionActive(bool active) {
        hostButton.gameObject.SetActive(active);
        clientButton.gameObject.SetActive(active);
        ipField.gameObject.SetActive(active);
    }

    private void SetToggleInterpolationActive(bool active) {
        toggleInterpolationButton.gameObject.SetActive(active);
        toggleInterpolationTypeButton.gameObject.SetActive(active);
    }

    public void ResetButtons()
    {
        SetHostClientSelectionActive(true);
        SetToggleInterpolationActive(false);
    }
}
