using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelSelecter : MonoBehaviour {

    public List<GameObject> level_icons;
    public List<NeighborhoodType> neigh_types;
    int current_selection = 0;
    int max_level = 1;

    public void InitializeEmptySaveGame() {
        foreach (NeighborhoodType nt in neigh_types) {
            SaveLevel sl = new SaveLevel();
            sl.neighborhood_type = nt;
            SaveGame.save_levels[nt] = sl;
        }
        SaveGame.initialized = true;
    }

    void Start() {
        if (!SaveGame.initialized) {
            // TODO load from file instead if existing
            InitializeEmptySaveGame();
        }

        // revert menu to current level
        for (int i = 0; i < neigh_types.Count; i++) {
            if (SaveGame.current_level == neigh_types[i]) {
                current_selection = i;
            }
        }
    }

    public void SelectNext() {
        current_selection = (int)Mathf.Min(current_selection + 1, max_level);
    }

    public void SelectPrev() {
        current_selection = (int)Mathf.Max(current_selection - 1, 0);
    }

    public void StartGame() {
        NeighborhoodType nt = neigh_types[current_selection];
        SaveLevel sl;
        if (SaveGame.save_levels.ContainsKey(nt)) {
            sl = SaveGame.save_levels[nt];
        } else {
            // just load a fresh game with grid_type nt
            sl = new SaveLevel();
            sl.neighborhood_type = nt;
        }
        SaveGame.current_level = nt;
        SceneManager.LoadScene("Game");
    }

    void Update() {
        if (Input.GetKeyUp(KeyCode.A)) {
            SelectPrev();
        }
        if (Input.GetKeyUp(KeyCode.D)) {
            SelectNext();
        }
        if (Input.GetKeyUp(KeyCode.W)) {
            StartGame();
        }

        for (int i = 0; i < level_icons.Count; i++) {
            level_icons[i].SetActive(i == current_selection);
        }
    }

    // TODO load/save from file
    //public void LoadFromFile() {
    //    string path = Application.persistentDataPath + "/save.save";
    //    if (File.Exists(path)) {
    //        FileStream f = File.Open(path, FileMode.Open);
    //        BinaryFormatter bf = new BinaryFormatter();
    //        SaveGame sl = (SaveLevel)bf.Deserialize(f);
    //    } else {

    //    }
    //    LoadSaveLevel(SaveLevel save_level);
    //}

}
