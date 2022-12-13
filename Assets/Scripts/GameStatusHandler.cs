using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine.SceneManagement;
using UnityEngine;

public class GameStatusHandler : NetworkBehaviour
{
    [SerializeField] private GameObject gameWon = null;
    [SerializeField] private GameObject gameOver = null;
    [SerializeField] private GameObject loading = null;
    [SerializeField] private GameObject mainMenu = null;


    // Call this from the server when the game is lost
    [ClientRpc]
    public void GameOverClientRpc() {
        loading.SetActive(false);
        gameOver.SetActive(true);
        gameWon.SetActive(false);
        mainMenu.SetActive(false);
    }

    // Call this from the server when the game is won
    [ClientRpc]
    public void GameWonClientRpc() {
        loading.SetActive(false);
        gameOver.SetActive(false);
        gameWon.SetActive(true);
        mainMenu.SetActive(false);
    }

    // Call this from the server when a client should be loading
    [ClientRpc]
    public void GameLoadingClientRpc(ulong clientId) {
        if (NetworkManager.Singleton.LocalClientId == clientId) {
            loading.SetActive(true);
            mainMenu.SetActive(false);
        }
    }

    // Call this to remove all loading-type screens and reset everything
    public void Reset() {
        loading.SetActive(false);
        gameOver.SetActive(false);
        gameWon.SetActive(false);
        mainMenu.SetActive(true);
        
        NetworkManager.Singleton.Shutdown(true);
        SceneManager.LoadScene("Scene1", LoadSceneMode.Single);
    }

}
