using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrefabGenerator : MonoBehaviour {
    public GameObject prefab;
    public float sec_per_prefab = 0.5f;
    public string sec_per_prefab_expression = "";

    float time_since_last_prefab = 0f;
    int iteration = 0;

    List<GameObject> prefab_pool;
    public int pool_size = 100;

    void UpdateSecPerPrefab() {
        if (sec_per_prefab_expression.Length > 0) {
            string expression = sec_per_prefab_expression;
            // set variables i (iteration) and x (previous value):
            expression = Evaluator.SetVar(expression, "i", iteration);
            expression = Evaluator.SetVar(expression, "x", sec_per_prefab);
            sec_per_prefab = (float)Evaluator.Eval(expression);
        }
    }

    void Start() {
        time_since_last_prefab = 0f;
        prefab_pool = new List<GameObject>();
        
        GameObject instance;
        for (int i = 0; i < pool_size; i++) {
            instance = Instantiate(prefab, gameObject.transform, false);
            instance.SetActive(false);
            prefab_pool.Add(instance);
        }
        UpdateSecPerPrefab();
    }

    void InstantiateObject() {
        foreach (GameObject obj in prefab_pool) {
            if (!obj.activeSelf) {
                obj.SetActive(true);
                obj.transform.position = gameObject.transform.position;
                obj.transform.localScale = Vector3.one;
                foreach (MonoBehaviour mb in
                         obj.GetComponentsInChildren<MonoBehaviour>()) {
                    Poolable p = mb.GetComponent<Poolable>();
                    if (p != null) {
                        p.Init();
                    }
                }
                return;
            }
        }
        Debug.Log("ERROR: pool capacity reached, could not initialize" +
                  $" [{prefab.name}].");
    }

    void Update() {
        time_since_last_prefab += Time.deltaTime;
        while (time_since_last_prefab >= sec_per_prefab) {
            time_since_last_prefab -= sec_per_prefab;
            UpdateSecPerPrefab();
            InstantiateObject();
            iteration ++;
        }
    }
}
