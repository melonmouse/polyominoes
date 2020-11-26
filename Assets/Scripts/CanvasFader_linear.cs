using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CanvasFader_linear : MonoBehaviour {
    Image image;
    float start_time = 0f;

    public bool fade_in;
    public float slope;
    public float offset;

    public float GetAlpha() {
        if (fade_in) {
            return Mathf.Clamp(slope * (offset - (Time.time - start_time)), 0, 1);
        } else {
            return Mathf.Clamp(slope * ((Time.time - start_time) - offset), 0, 1);
        }
    }

    void Start() {
        image = gameObject.GetComponent<Image>();
        start_time = Time.time;
        UpdateAlpha();
    }

    public void UpdateAlpha() {
        Color c = image.color;
        c.a = GetAlpha();
        image.color = c;
    }

    void Update() {
        UpdateAlpha();
    }
}
