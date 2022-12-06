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
        if (IsOwner)
        {
            if (collision.gameObject.CompareTag("Player"))
            {
                count++;
                game1.SetActive(false);
                loading.SetActive(true);
            }
        } else
        {
            if (collision.gameObject.CompareTag("Player"))
            {
                count++;
                if (count == 2)
                {
                    gameWon.SetActive(true);
                }
                else
                {
                    game1.SetActive(false);
                    loading.SetActive(true);
                }
            }
        }
    }
}
