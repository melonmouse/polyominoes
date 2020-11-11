using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DiscreteValueIndicator : MonoBehaviour {
    public GameObject one_prefab;
    public GameObject zero_prefab;
    public Vector3 delta;
    public int max_value = 0;
    public int value = 0;
    List<GameObject> one_indicators = new List<GameObject>();
    List<GameObject> zero_indicators = new List<GameObject>();

    float opacity = 0.35f;
    float redness = 0f;
    float bonus_scale = 0f;

    void Start() {

    }

    public void ExpandWithPrefab(List<GameObject> l, GameObject prefab) {
        for (int i = 0; i < max_value; i++) {
            if (i >= l.Count) {
                GameObject new_one = Instantiate(prefab);
                new_one.transform.SetParent(gameObject.transform);
                l.Add(new_one);
            }
            RectTransform rt = l[i].GetComponent<RectTransform>();
            rt.anchoredPosition3D = delta * i;
            //Debug.Log(rt.localScale);
            rt.localScale = Vector3.one;
        }
        while (l.Count > max_value) {
            l.RemoveAt(l.Count - 1);
        }
    }

    public void BoostVisibility() {
        redness = 1.0f;
        opacity = 0.7f;
        bonus_scale = 0.3f;
    }


    void FixedUpdate() {
        if (zero_indicators.Count != max_value) {
            ExpandWithPrefab(zero_indicators, zero_prefab);
        }
        if (one_indicators.Count != max_value) {
            ExpandWithPrefab(one_indicators, one_prefab);
        }

        float target_opacity = (value == max_value ? 0.3f : 0.15f);
        opacity = 0.97f * opacity + 0.03f * target_opacity;
        redness *= 0.95f;
        bonus_scale *= 0.95f;
        for (int i = 0; i < max_value; i++) {
            one_indicators[i].SetActive(i < value);
            zero_indicators[i].SetActive(i >= value);

            Color c = one_indicators[i].GetComponent<Image>().color;
            c.a = opacity;
            //c.g = 1f - redness;  // enable for flashing red
            //c.b = 1f - redness;
            one_indicators[i].GetComponent<Image>().color = c;

            c = zero_indicators[i].GetComponent<Image>().color;
            c.a = opacity;
            //c.g = 1f - redness;  // enable for flashing red
            //c.b = 1f - redness;
            zero_indicators[i].GetComponent<Image>().color = c;

            one_indicators[i].GetComponent<RectTransform>().localScale =
                    (1.0f + bonus_scale) * Vector3.one;
        }
    }
}
