using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Light2D))]
[RequireComponent(typeof(SpriteRenderer))]

public class Torch : MonoBehaviour
{
    // Rate at which the torch flickers
    public float flickerPeriod = 1.0f;

    // If the torch has been disabled
    public bool lit = true;

    // Sprites for animation
    public Sprite leftSprite;
    public Sprite rightSprite;
    public Sprite offSprite;

    Light2D light;
    SpriteRenderer sr;
    float litIntensity;
    float flickerTime = 0.0f;

    void Start() {
        light = GetComponent<Light2D>();
        sr = GetComponent<SpriteRenderer>();
        litIntensity = light.intensity;
    }

    void Update() {

        flickerTime = (flickerTime + Time.deltaTime) % flickerPeriod;

        // On or Off
        if (lit) {
            light.intensity = litIntensity;

            // Flicker
            if (flickerTime < flickerPeriod / 2f) {
                sr.sprite = leftSprite;
            } else {
                sr.sprite = rightSprite;
            }

        } else {
            light.intensity = 0f;

            sr.sprite = offSprite;
        }

    }
}
