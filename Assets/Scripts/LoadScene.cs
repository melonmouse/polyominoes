using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadScene : MonoBehaviour {
    public string scene_name;
    void Start() {
        SceneManager.LoadSceneAsync(scene_name, LoadSceneMode.Single);
    }
}
