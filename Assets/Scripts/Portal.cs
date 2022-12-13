using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;
using System;
using System.IO;

public class Portal : NetworkBehaviour
{
    private GameStatusHandler gameStatusHandler;
    private void Start() {
        gameStatusHandler = (GameStatusHandler)FindObjectOfType(typeof(GameStatusHandler));
    }

    int count = 0;

    private void OnCollisionEnter2D (Collision2D collision)
    {
        // Only matters if object is player
        if (collision.gameObject.CompareTag("Player")) {
            if (NetworkManager.Singleton.IsHost)
            {
                count++;
                if (count == 2) 
                {
                    // Both ghosts have entered portal, game is won
                    gameStatusHandler.GameWonClientRpc();
                } else {
                    // One ghost has entered portal, load on that instance of game
                    gameStatusHandler.GameLoadingClientRpc(collision.gameObject.GetComponent<PlayerController>().clientId.Value);
                }

                // Destroy ghost that entered portal
                Destroy(collision.gameObject);
            }
        }
    }
}
