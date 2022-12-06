using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class ToMain : MonoBehaviour
{

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
            NetworkManager.Singleton.Shutdown();
            SceneManager.LoadScene("Scene1");
        }
    }
}
