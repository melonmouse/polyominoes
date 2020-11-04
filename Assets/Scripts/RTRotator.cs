using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RTRotator : MonoBehaviour {
    RectTransform rt;
    public float rotation = 0f;
    public float rotation_speed_deg_per_second = 360 / 2;
    // default: one rot per 2 sec.

    void Start() {
        rt = GetComponent<RectTransform>();
    }

    public void SetRotationSpeed(float deg_per_second) {
        rotation_speed_deg_per_second = deg_per_second;
    }

    void Update() {
        rt.localRotation = Quaternion.Euler(0, 0, rotation);
        rotation += Time.deltaTime * rotation_speed_deg_per_second;
    }
}
