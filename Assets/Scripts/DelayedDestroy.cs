using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DelayedDestroy : MonoBehaviour {
    float ttl;
    float end_time = 0;

    public void SetTTL(float ttl) {
        this.ttl = ttl;
        end_time = Time.time + ttl;
    }

    void Update() {
        if (Time.time > end_time) {
            Destroy(gameObject);
        }
    }
}
