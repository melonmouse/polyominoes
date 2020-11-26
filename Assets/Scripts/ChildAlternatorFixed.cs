using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChildAlternatorFixed : MonoBehaviour {
    public float sec_per_child = 0.5f;

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
        sec_per_child = Mathf.Max(0.3f / (iteration+1f), 1f/60f);
        if (iteration < 1) {
            sec_per_child += 1f;
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
