using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChildAlternator : MonoBehaviour {
    public float sec_per_child = 0.5f;
    public string sec_per_child_expression = "";

    List<GameObject> children;
    int current_index = 0;
    int iteration = 0;
    float time_since_switch;

    void Start() {
        children = new List<GameObject>();
        foreach (Transform child in transform) {
            children.Add(child.gameObject);
        }
        time_since_switch = 0f;
        UpdateVisibility();
        UpdateSecPerChild();
    }

    void UpdateVisibility() {
        for (int i = 0; i < children.Count; i++) {
            GameObject child = children[i];
            child.SetActive(i == current_index);
        }
    }

    void UpdateSecPerChild() {
        if (sec_per_child_expression.Length > 0) {
            string expression = sec_per_child_expression;
            // set variables i (iteration) and x (previous value):
            expression = Evaluator.SetVar(expression, "i", iteration);
            expression = Evaluator.SetVar(expression, "x", sec_per_child);
            sec_per_child = (float)Evaluator.Eval(expression);
        }
    }

    void Update() {
        time_since_switch += Time.deltaTime;
        if (time_since_switch >= sec_per_child) {
            time_since_switch -= sec_per_child;
            UpdateSecPerChild();
            current_index = (current_index + 1) % children.Count;
            UpdateVisibility();
            iteration ++;
        }
    }
}
