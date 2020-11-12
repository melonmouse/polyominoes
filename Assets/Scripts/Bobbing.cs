﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bobbing : MonoBehaviour {

    RectTransform rt = null;
    Vector3 base_pos;

    public float amplitude = 10f;
    public float frequency = 0.2f;  // bobs per second

    void Start() {
        rt = GetComponent<RectTransform>();
        base_pos = rt.anchoredPosition3D;
        update_position();
    }

    public void update_position() {
        float phase = Time.time * 2 * Mathf.PI * frequency;
        Vector3 bob_delta = new Vector3(0, amplitude*Mathf.Sin(phase), 0);
        rt.anchoredPosition3D = base_pos + bob_delta;
    }

    void Update() {
        update_position();
    }

    void OnEnable() {
        if (rt != null) {
            update_position();
        }
    }
}