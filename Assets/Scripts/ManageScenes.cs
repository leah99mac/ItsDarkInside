using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ManageScenes : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject mainMenu = null;
    [SerializeField] private GameObject game1 = null;
    [SerializeField] private GameObject gameOver = null;
    [SerializeField] private GameObject gameWon = null;
    [SerializeField] private GameObject loading = null;

    // Start is called before the first frame update
    void Start()
    {
        mainMenu.SetActive(true);
        game1.SetActive(false);
        gameOver.SetActive(false);
        gameWon.SetActive(false);
        loading.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
