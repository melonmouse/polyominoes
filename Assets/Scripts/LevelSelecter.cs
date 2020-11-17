using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LevelSelecter : MonoBehaviour {

    public List<GameObject> level_icons;
    public List<NeighborhoodType> neigh_types;
    public List<AudioSource> songs;
    int current_selection = 0;
    int max_level = 0;
    public GameObject next_button;
    public GameObject prev_button;
    public GameObject click_to_start;

    float start_time;
    bool cooldown_finished = false;

    float song_time = 0f;

    public void InitializeEmptySaveGame() {
        CurrentSaveGame.save = new SaveGame();
        foreach (NeighborhoodType nt in neigh_types) {
            SaveLevel sl = new SaveLevel();
            sl.neighborhood_type = nt;
            CurrentSaveGame.save.save_levels[nt] = sl;
        }
        CurrentSaveGame.save.initialized = true;
    }

    void ChangeSong() {
        float wall_time_in_song = 0f;
        if (current_selection < 3) {
            // normal speed
            wall_time_in_song = song_time;
        } else {
            // double speed
            wall_time_in_song = song_time/2f;
        }

        for (int i = 0; i < songs.Count; i++) {
            if (i == current_selection) {
                // we assume the song just stops if wall_time_in_song > length
                songs[i].time = wall_time_in_song;
                songs[i].Play();
            } else {
                songs[i].Pause();
            }
        }
    }

    void UpdateSongTime() {
        if (current_selection < 3) {
            // normal speed (80bpm)
            song_time += Time.deltaTime;
        } else {
            // double speed (160bpm)
            song_time += 2*Time.deltaTime;
        }
    }

    void Start() {
        CurrentSaveGame.save = SaveGame.LoadFromFile();
        if (CurrentSaveGame.save == null || !CurrentSaveGame.save.initialized) {
            InitializeEmptySaveGame();
        }

        // revert menu to current level
        for (int i = 0; i < neigh_types.Count; i++) {
            if (CurrentSaveGame.save.current_level == neigh_types[i]) {
                current_selection = i;
            }
        }


        // add satelites representing score
        for (int i = 0; i < level_icons.Count; i++) {
            int score = CurrentSaveGame.save.save_levels[neigh_types[i]].score;
            level_icons[i].GetComponent<Satellite>().n_satelites = score;
            if (score > 1) {
                level_icons[i].GetComponent<Satellite>().initialize();
            }
        }
        max_level = LevelBariers.get_max_level();
        Debug.Log($"max_level={max_level}");
        start_time = Time.time;
        CurrentSelectionChanged();
    }

    public void CurrentSelectionChanged() {
        UpdateNextPrevButtons();
        ChangeSong();
        for (int i = 0; i < level_icons.Count; i++) {
            level_icons[i].SetActive(i == current_selection);
        }
    }

    public void SelectNext() {
        current_selection = (int)Mathf.Min(current_selection + 1, max_level);
        CurrentSelectionChanged();
    }

    public void SelectPrev() {
        current_selection = (int)Mathf.Max(current_selection - 1, 0);
        CurrentSelectionChanged();
    }

    public void UpdateNextPrevButtons() {
        prev_button.GetComponent<Button>().interactable = cooldown_finished;
        prev_button.SetActive(current_selection > 0);
        if (max_level == 0) {
            next_button.SetActive(false);
            // Don't show the next button at first to avoid confusion
        } else {
            next_button.SetActive(current_selection < level_icons.Count-1);
            next_button.GetComponent<Button>().interactable =
                    (current_selection < max_level) && cooldown_finished;
        }

    }

    public void StartGame() {
        NeighborhoodType nt = neigh_types[current_selection];
        SaveLevel sl;
        if (CurrentSaveGame.save.save_levels.ContainsKey(nt)) {
            sl = CurrentSaveGame.save.save_levels[nt];
        } else {
            // just load a fresh game with grid_type nt
            sl = new SaveLevel();
            sl.neighborhood_type = nt;
        }
        CurrentSaveGame.save.current_level = nt;
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

        if (!cooldown_finished) {
            if (Time.time > start_time + 0.5f) {
                cooldown_finished = true;
                UpdateNextPrevButtons();
            }
        }

        UpdateSongTime();
    }
}
