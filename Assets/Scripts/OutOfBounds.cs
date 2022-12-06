using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class OutOfBounds : MonoBehaviour
{
    [SerializeField] private GameObject gameOver = null;
    [SerializeField] private GameObject game1 = null;
    [SerializeField] private GameObject loading = null;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            game1.SetActive(false);
            loading.SetActive(false);
            gameOver.SetActive(true);
        }
    }
}
