using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class CellState : MonoBehaviour {
    bool selected;
    bool image_mode;
    public GameObject selected_sprite;
    public GameObject deselected_sprite;
    public GameObject selected_image;
    public GameObject deselected_image;

    public bool is_selected() {
        return selected;
    }

    public void update_gameobject() {
        selected_sprite.SetActive(selected && (!image_mode));
        deselected_sprite.SetActive((!selected) && (!image_mode));
        selected_image.SetActive(selected && image_mode);
        deselected_image.SetActive((!selected) && image_mode);
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

    void Start() {
        selected = false;
        image_mode = false;
    }

    public void set_order_in_layer(int order) {
        selected_sprite.GetComponent<Renderer>().sortingOrder = order;
        deselected_sprite.GetComponent<Renderer>().sortingOrder = order;
    }

    public Rect get_rect() {
        Assert.IsTrue(image_mode, "No bounding rect in sprite mode.");
        if (selected) {
            return selected_image.GetComponent<RectTransform>().rect;
        } else {
            return deselected_image.GetComponent<RectTransform>().rect;
        }
    }

    public RectTransform get_rect_transform() {
        Assert.IsTrue(image_mode, "No bounding rect in sprite mode.");
        if (selected) {
            return selected_image.GetComponent<RectTransform>();
        } else {
            return deselected_image.GetComponent<RectTransform>();
        }
    }
}
