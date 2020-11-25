using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DelayedDestroy : MonoBehaviour, Poolable {
    public float ttl = -1f;
    public bool destroy = true;  // otherwise just deactivate
    float end_time = 0;
    public bool init_on_start = false;
    bool initialized = false;

    public void Init() {
        initialized = true;
        Debug.Assert(ttl > 0, "Tried initializing DelayedDestroy with ttl<=0.");
        end_time = Time.time + ttl;
    }

    public void SetTTL(float ttl) {
        this.ttl = ttl;
        Init();
    }

    void Start() {
        if (init_on_start && ttl >= 0) {
            Init();
        }
    }

    void Update() {
        if (initialized && Time.time > end_time) {
            if (destroy) {
                Destroy(gameObject);
            } else {
                gameObject.SetActive(false);
            }
        }
    }
}
