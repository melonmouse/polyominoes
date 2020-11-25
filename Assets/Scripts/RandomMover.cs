using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomMover : MonoBehaviour, Poolable {
    public float friction_coefficient = 0.99f;
    public float angular_friction_coefficient = 0.99f;
    public Vector3 gravity = new Vector3(0, -1f, 0);
    public string initial_speed_expression;
    public string initial_angle_expression;
    public string initial_angular_momentum_expression;
    public string initial_localScale_expression;

    Vector3 speed;
    float angular_momentum;

    public bool init_on_start = false;

    public float GetValue(string expression) {
        string expr = expression;
        expr = Evaluator.SetVar(expression, "r", Random.value);
        return (float)Evaluator.Eval(expr);
    }

    public void Init() {
        float initial_speed = GetValue(initial_speed_expression);
        float initial_angle = GetValue(initial_angle_expression);
        angular_momentum = GetValue(initial_angular_momentum_expression);

        speed = Quaternion.Euler(0, 0, initial_angle) * new Vector3(0, 1f, 0) *
                initial_speed;

        gameObject.transform.localScale = gameObject.transform.localScale *
                GetValue(initial_localScale_expression);
    }

    void Start() {
        if (init_on_start) {
            Init();
        }
    }

    void Update() {
        speed += Time.deltaTime * gravity;
        speed *= friction_coefficient;
        gameObject.transform.position += speed * Time.deltaTime;

        angular_momentum *= angular_friction_coefficient;
        Vector3 euler_rot = gameObject.transform.rotation.eulerAngles;
        float angular_pos = euler_rot.z + angular_momentum * Time.deltaTime;
        gameObject.transform.rotation = Quaternion.Euler(0, 0, angular_pos);
    }
}
