using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CanvasFader : MonoBehaviour {
    public string alpha_expression;
    Image image;
    float start_time = 0f;

    public float GetAlpha() {
        string expr = alpha_expression;
        expr = Evaluator.SetVar(expr, "t", Time.time - start_time);
        return (float)Evaluator.Eval(expr);
    }

    void Start() {
        image = gameObject.GetComponent<Image>();
        UpdateAlpha();
        start_time = Time.time;
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
