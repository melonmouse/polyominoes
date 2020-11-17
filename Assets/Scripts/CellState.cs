using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Assertions;

public class CellState : MonoBehaviour {
    bool selected;
    bool image_mode;
    public GameObject selected_obj;
    public GameObject deselected_obj;
    public TMP_Text debug_text;

    public (int, int) coordinate;

    public bool is_selected() {
        return selected;
    }

    public void update_gameobject() {
        selected_obj.SetActive(selected);
        deselected_obj.SetActive(!selected);
    }

    public void set_selected(bool s) {
        if (s != selected) {
            selected = s;
            update_gameobject();
        }
    }

    public void set_image_mode(bool m) {
        if (m != image_mode) {
            image_mode = m;
            update_gameobject();
        }
    }

    public void set_text(string s) {
        debug_text.enabled = true;
        debug_text.text = s;
        debug_text.gameObject.transform.rotation = Quaternion.identity;
    }

    void Start() {
        selected = false;
        image_mode = false;
        debug_text.enabled = false;
    }

    public void set_order_in_layer(int order) {
        Assert.IsFalse(image_mode, "No sorting order in image mode.");
        selected_obj.GetComponent<Renderer>().sortingOrder = order;
        deselected_obj.GetComponent<Renderer>().sortingOrder = order;
    }

    public Rect get_rect() {
        Assert.IsTrue(image_mode, "No bounding rect in sprite mode.");
        if (selected) {
            return selected_obj.GetComponent<RectTransform>().rect;
        } else {
            return deselected_obj.GetComponent<RectTransform>().rect;
        }
    }

    public RectTransform get_rect_transform() {
        Assert.IsTrue(image_mode, "No bounding rect in sprite mode.");
        if (selected) {
            return selected_obj.GetComponent<RectTransform>();
        } else {
            return deselected_obj.GetComponent<RectTransform>();
        }
    }
}
