using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Light2D))]
[RequireComponent(typeof(SpriteRenderer))]

public class Torch : MonoBehaviour
{

    // Tag of torch, will be toggled by lever if tag matches
    public string Tag;

    // Rate at which the torch flickers
    public float flickerPeriod = 1.0f;

    // If the torch has been disabled
    public bool lit = true;

    // Sprites for animation
    public Sprite leftSprite;
    public Sprite rightSprite;
    public Sprite offSprite;

    Light2D l2d;
    SpriteRenderer sr;
    float litIntensity;
    float flickerTime = 0.0f;

    void Start() {
        l2d = GetComponent<Light2D>();
        sr = GetComponent<SpriteRenderer>();
        litIntensity = l2d.intensity;
    }

    void Update() {

        flickerTime = (flickerTime + Time.deltaTime) % flickerPeriod;

        // On or Off
        if (lit) {
            l2d.intensity = litIntensity;

            // Flicker
            if (flickerTime < flickerPeriod / 2f) {
                sr.sprite = leftSprite;
            } else {
                sr.sprite = rightSprite;
            }

        } else {
            l2d.intensity = 0f;

            sr.sprite = offSprite;
        }

    }
}
