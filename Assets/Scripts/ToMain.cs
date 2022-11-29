using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class ToMain : MonoBehaviour
{
    float timer = 0.0f;
    float total = 5.0f;


    // Start is called before the first frame update
    void Start()
    {
        

    }


    // Update is called once per frame
    void Update()
    {
        if (timer < total)
        {
            timer += Time.deltaTime;
        }
        else
        {
            SceneManager.LoadScene("MainMenu");
        }
    }
}
