using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Satellite : MonoBehaviour {
    public GameObject satelite_prefab;
    public float radius;
    public float rotation_speed_deg_per_sec;
    public int n_satelites;
    public Vector3 offset = Vector3.zero;

    public bool auto_init = true;

    GameObject container;

    void Start() {
        if (auto_init) {
            initialize();
        }
    }
    
    public void initialize() {
        // not sure this works when reinitializing (needs cleanup)
        container = new GameObject();
        container.transform.SetParent(gameObject.transform);
        container.AddComponent<RectTransform>();
        container.GetComponent<RectTransform>()
                 .anchoredPosition3D = offset;
        container.GetComponent<RectTransform>()
                 .localScale = Vector3.one;

        for (int i = 0; i < n_satelites; i++) {
            float rad = 2 * Mathf.PI * i/n_satelites;
            Vector3 pos =
                    radius * new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0);
            GameObject sat =
                    Instantiate(satelite_prefab,
                                Vector3.zero,
                                Quaternion.identity);
            sat.transform.SetParent(container.transform);
            sat.GetComponent<RectTransform>()
               .localScale = Vector3.one;
            sat.GetComponent<RectTransform>()
               .anchoredPosition3D = pos;
            sat.AddComponent<RTRotator>();
            sat.GetComponent<RTRotator>()
               .SetRotationSpeed(rotation_speed_deg_per_sec);
        }
        container.AddComponent<RTRotator>();
        container.GetComponent<RTRotator>()
                 .SetRotationSpeed(-rotation_speed_deg_per_sec);
    }
}
