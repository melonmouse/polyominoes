using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectLerper : MonoBehaviour {
    Vector3 target_position;
    Vector3 speed;

    RectTransform rect_transform;

    void Start() {
        rect_transform = GetComponent<RectTransform>();
    }

    public void SetTargetPosition(Vector3 v) {
        target_position = v;
    }

    void FixedUpdate() {
        Vector3 diff = (target_position - rect_transform.anchoredPosition3D);
        speed = 0.95f*speed + 0.05f*(diff/60f);
        speed.ClampMagnitude(100f/60f);
        rect_transform.anchoredPosition3D += speed;
    }
}
