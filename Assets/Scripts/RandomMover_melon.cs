using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomMover_melon : MonoBehaviour, Poolable {
    public float friction_coefficient = 0.998f;
    public float angular_friction_coefficient = 0.998f;
    public Vector3 gravity = new Vector3(0, -10f, 0);

    Vector3 speed;
    float angular_momentum;

    public bool init_on_start = false;

    public void Init() {
        float initial_speed = 5f + Random.value * 4f;
        float initial_angle = (Random.value < 0.5f ? 1 : -1)*80f +
                (Random.value - 0.5f) * 50f;
        angular_momentum = (Random.value-0.5f)*2*360*2;

        speed = Quaternion.Euler(0, 0, initial_angle) * new Vector3(0, 1f, 0) *
                initial_speed;

        float r = Random.value;
        gameObject.transform.localScale = gameObject.transform.localScale *
               (0.5f+0.3f*r*r);
    }

    void Start() {
        if (init_on_start) {
            Init();
        }
    }

    void FixedUpdate() {
        speed += Time.fixedDeltaTime * gravity;
        //speed *= Mathf.Pow(friction_coefficient, Time.fixedDeltaTime*60f);
        gameObject.transform.position += speed * Time.fixedDeltaTime;

        //angular_momentum *= Mathf.Pow(angular_friction_coefficient,
        //                              Time.fixedDeltaTime*60f);
        Vector3 euler_rot = gameObject.transform.rotation.eulerAngles;
        float angular_pos = euler_rot.z + angular_momentum * Time.fixedDeltaTime;
        gameObject.transform.rotation = Quaternion.Euler(0, 0, angular_pos);
    }
}
