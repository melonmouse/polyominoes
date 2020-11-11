using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LevelSelecter : MonoBehaviour {

    public List<GameObject> level_icons;
    public List<NeighborhoodType> neigh_types;
    int current_selection = 0;
    int max_level = 0;
    public GameObject next_button;
    public GameObject prev_button;
    public GameObject click_to_start;

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

        // add satelites representing score
        int total_score = 0;
        for (int i = 0; i < level_icons.Count; i++) {
            int score = SaveGame.save_levels[neigh_types[i]].max_cells;
            level_icons[i].GetComponent<Satellite>().n_satelites = score;
            total_score += score;
            if (score > 1) {
                level_icons[i].GetComponent<Satellite>().initialize();
            }
        }
        Debug.Log($"total_score={total_score}");
        // unlock levels based on score
        if (total_score < 4) {
            max_level = 0;
            // For TriangleNeumann - finish tutorial;
            // - SquareNeumann 4 (5 options)
        } else if (total_score < 10) {
            max_level = 1;  // unlock TriangleNeumann
            // For Hexagon - finish 10=4+5(+1):
            // - SquareNeumann 4 (5 options)
            // - TriangleNeumann 5 (4 options)
            // and one of:
            // - SquareNeumann 5=+1 (12 options)
            // - TriangleNeumann 6=+1 (12 options)
        } else if (total_score < 16) {
            max_level = 2;  // unlock Hexagon
            // For SquareMoore - finish 16=5+6+4(+1):
            // - SquareNeumann 5 (12 options)
            // - TriangleNeumann 6 (12 options)
            // - Hexagon 4 (7 options)
            // and one of:
            // - SquareNeumann 6=+1 (35 options)
            // - TriangleNeumann 7=+1 (24 options)
            // - Hexagon 5=+1 (22 options)
        } else if (total_score < 21) {  // 
            max_level = 3;  // unlock SquareMoore
            // For TriangleMoore - finish 21=5+6+4+3(+3):
            // - SquareNeumann 5 (12 options)
            // - TriangleNeumann 6 (12 options)
            // - Hexagon 4 (7 options)
            // - SquareMoore 3 (5 options)
            // and three of (two extra compared to last):
            // - SquareNeumann 6=+1 (35 options)
            // - TriangleNeumann 7=+1 (24 options)
            // - Hexagon 5=+1 (22 options)
            // - SquareMoore 4=+1 (22 options)
        } else if (total_score < 26) {  // 
            max_level = 4;  // unlock TriangleMoore
            // For TriangleMoore - finish 26=5+6+4+3+3(+5):
            // - SquareNeumann 5 (12 options)
            // - TriangleNeumann 6 (12 options)
            // - Hexagon 4 (7 options)
            // - SquareMoore 3 (5 options)
            // - TriangleMoore 3 (11 options)
            // and all of (two extra compared to last):
            // - SquareNeumann 6=+1 (35 options)
            // - TriangleNeumann 7=+1 (24 options)
            // - Hexagon 5=+1 (22 options)
            // - SquareMoore 4=+1 (22 options)
            // - TriangleMoore 4=+1 (75 options)
        } else {
            max_level = 5;  // unlock HexagonJump
        }

        Debug.Log($"max_level={max_level}");
        UpdateNextPrevButtons();
    }

    public void SelectNext() {
        current_selection = (int)Mathf.Min(current_selection + 1, max_level);
        UpdateNextPrevButtons();
    }

    public void SelectPrev() {
        current_selection = (int)Mathf.Max(current_selection - 1, 0);
        UpdateNextPrevButtons();
    }

    public void UpdateNextPrevButtons() {
        prev_button.SetActive(current_selection > 0);
        if (max_level == 0) {
            next_button.SetActive(false);
            // Don't show the next button at first to avoid confusion
        } else {
            next_button.SetActive(current_selection < level_icons.Count-1);
            next_button.GetComponent<Button>().interactable =
                    (current_selection < max_level);
        }
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
        if (Input.GetKeyUp(KeyCode.A) || Input.GetKeyUp(KeyCode.LeftArrow)) {
            SelectPrev();
        }
        if (Input.GetKeyUp(KeyCode.D) || Input.GetKeyUp(KeyCode.RightArrow)) {
            SelectNext();
        }
        if (Input.GetKeyUp(KeyCode.Return) || Input.GetKeyUp(KeyCode.Space)) {
            StartGame();
        }

        if (max_level == 0 && Time.time > 6f) {
            click_to_start.SetActive(true);
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
