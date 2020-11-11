using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomShake : MonoBehaviour {
    Vector3 base_pos;

    float start_time = -999f;
    float duration = 1f;
    float amplitude = 0f;

    void Start() {
        base_pos = gameObject.transform.position;
    }

    void Update() {
        float delta_time = Time.time - start_time;
        float time_fraction = delta_time / duration;
        if (time_fraction > 0 && time_fraction < 1) {
            Vector3 offset = new Vector3(
                    (2*Random.value-1f),
                    (2*Random.value-1f),
                    0);
            offset *= amplitude * (1-time_fraction) / offset.magnitude;
            gameObject.transform.position = base_pos + offset;
        }
    }

    public void StartShake(float duration, float amplitude) {
        this.duration = duration;
        start_time = Time.time;
        this.amplitude = amplitude;
    }
}
