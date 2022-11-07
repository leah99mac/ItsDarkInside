using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Light2D))]
public class LightBall : MonoBehaviour
{

    public float decayRate = 0.01f;


    private Rigidbody2D rb;
    private Light2D l;

    private float intensity;

    private void Start() {
        rb = GetComponent<Rigidbody2D>();
        l = GetComponent<Light2D>();
        intensity = l.intensity;
    }

    void Update()
    {
        
        // Destroy when faded out
        if (intensity <= 0f) {
            Destroy(gameObject);
        }

        intensity -= decayRate;

        l.intensity = intensity;
    }
}
