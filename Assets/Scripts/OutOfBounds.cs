using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class OutOfBounds : MonoBehaviour
{
    private GameStatusHandler gameStatusHandler;

    private void Start() {
        gameStatusHandler = (GameStatusHandler)FindObjectOfType(typeof(GameStatusHandler));
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            gameStatusHandler.GameOverClientRpc();
        }
    }
}
