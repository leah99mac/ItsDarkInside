using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ToMain : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject networkUI = null;
    [SerializeField] private GameObject mainMenu = null;

    float timer = 0.0f;
    float total = 5.0f;

    // Update is called once per frame
    void Update()
    {
        if (timer < total)
        {
            timer += Time.deltaTime;
        }
        else
        {
            networkUI.SetActive(true);
            mainMenu.SetActive(true);
        }
    }
}
