using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;
using System;
using System.IO;

public class Portal : NetworkBehaviour
{
    [SerializeField] private GameObject game1 = null;
    [SerializeField] private GameObject gameWon = null;
    [SerializeField] private GameObject loading = null;

    int count = 0;

    private void OnCollisionEnter2D (Collision2D collision)
    {
        count++;
        if (collision.gameObject.GetComponent<PlayerController>().IsOwner)
        {
            if (collision.gameObject.CompareTag("Player"))
            {
                loading.SetActive(true);
                Destroy(collision.gameObject);
            }
        }
        if (count == 2)
        {
            loading.SetActive(false);
            game1.SetActive(false);
            gameWon.SetActive(true);
        }
    }
}
