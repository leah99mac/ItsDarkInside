using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lever : MonoBehaviour
{
    // Off = right, On = left TODO do we want to change this
    public enum LeverState {ON, OFF};

    // The current state of the lever
    public LeverState state = LeverState.OFF;
    // The id of this lever, used to distinguish between objects that are manipulated by multiple levers
    public int id;
    // The object to manipulate when the lever state changes
    public GameObject[] objects;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        // TODO improve sprite transition later, for now just switch to left or right based on state
        transform.localScale = new Vector3(((state == LeverState.ON)? -1 : 1) * Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
    }

    void Activate() 
    {
        // Activate objects
        foreach (GameObject obj in objects) 
        {
            // TODO add activatable monobehaviors to check for here
        }

        // Switch states
        state = (state == LeverState.ON)? LeverState.OFF : LeverState.ON;
    }
}
