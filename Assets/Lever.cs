using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class Lever : NetworkBehaviour {
    
    // The tag of the lever, any door or component with this tag will be activated
    public string Tag;

    // The arm of the lever
    public GameObject arm;

    // The threshold of when the lever should "activate"
    public float Threshold;

    private float armLastPosition;

    private void Update() {

        float armPosition = 180f - arm.transform.eulerAngles.z;

        if ((armPosition > Threshold || armPosition < -Threshold) && (Mathf.Sign(armPosition) != Mathf.Sign(armLastPosition))) {

            // New lever position, activate everything here

            // DOORS
            Object [] doors = GameObject.FindObjectsOfType(typeof(Door));
            foreach (Door door in doors) {
                if (door.Tag == Tag) {
                    door.Closed = !door.Closed;
                }
            }

            armLastPosition = armPosition;
        }

    }

}

