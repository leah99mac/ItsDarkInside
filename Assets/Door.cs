using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class Door : NetworkBehaviour
{

    // Tag that connects this door to a lever or other activatable object
    public string Tag = "";

    // If the door is closed
    public bool Closed = true;

    SpriteRenderer sr;
    Collider2D c;
    ShadowCaster2D sc;

    // Start is called before the first frame update
    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        c = GetComponent<Collider2D>();
        sc = GetComponent<ShadowCaster2D>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Closed) {
            c.enabled = true;
            sr.color = Color.white;
            if (sc) sc.enabled = true;
        } else {
            c.enabled = false;
            sr.color = new Color(1, 1, 1, 0.5f);
            if (sc) sc.enabled = false;
        }
    }
}
