using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectLerper : MonoBehaviour {
    Vector3 target_position;
    Vector3 speed;

    RectTransform rect_transform;
    public bool rect_transform_mode = true;

    void Start() {
        if (rect_transform_mode) {
            rect_transform = GetComponent<RectTransform>();
        }
    }

    public void SetTargetPosition(Vector3 v) {
        target_position = v;
    }

    void FixedUpdate() {
        Vector3 diff;
        if (rect_transform_mode) {
            diff = (target_position - rect_transform.anchoredPosition3D);
        } else {
            diff = (target_position - gameObject.transform.position);
        }
        speed = 0.95f*speed + 0.05f*(diff/60f);
        speed.ClampMagnitude(100f/60f);
        if (rect_transform_mode) {
            rect_transform.anchoredPosition3D += speed;
        } else {
            gameObject.transform.position += speed;
        }
    }
}
