using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CellState : MonoBehaviour {
    bool selected;
    public GameObject selected_sprite;
    public GameObject deselected_sprite;

    public bool is_selected() {
        return selected;
    }
    public void set_selected(bool v) {
        if (v != selected) {
            selected = v;
            selected_sprite.SetActive(v);
            deselected_sprite.SetActive(!v);
        }
    }

    void Start() {
        selected = false;
    }
}
