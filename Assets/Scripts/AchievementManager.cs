using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Shape = System.Collections.Generic.SortedSet<(int x, int y)>;


// MYBRARY
public static class MyExtensions {
    public static float Max(this Vector2 v) {
        return v.x > v.y ? v.x : v.y;
    }

    public static float Min(this Vector2 v) {
        return v.x < v.y ? v.x : v.y;
    }

    public static Vector2 XY(this Vector3 v) {
        return new Vector2(v.x, v.y);
    }

    public static Vector3 Pad(this Vector2 v) {
        return new Vector3(v.x, v.y, 0);
    }

    public static Vector3 GetClampMagnitude(this Vector3 v, float maxLength) {
        return Vector3.ClampMagnitude(v, maxLength);
    }

    public static void ClampMagnitude(this Vector3 v, float maxLength) {
        v = Vector3.ClampMagnitude(v, maxLength);
    }
}

public class AchievementManager : MonoBehaviour {
    public GameObject achievement_container_prefab;
    public GameObject canvas;

    List<GameObject> achievements_shown;
    List<float> end_times;

    void Start() {
        achievements_shown = new List<GameObject>();
        end_times = new List<float>();
    }

    void Update() {
        int outdated_index = -1;
        for (int i = 0; i < achievements_shown.Count; i++) {
            if (Time.time > end_times[i]) {
                outdated_index = i;
            }
        }
        for (int i = 0; i < achievements_shown.Count; i++) {
            Vector3 target_pos;
            if (i <= outdated_index) {
                target_pos = new Vector3(-1000, 0, 0);
            } else {
                target_pos =
                        new Vector3((i - outdated_index - 1) * 200, 0, 0);
            }
            achievements_shown[i].GetComponent<ObjectLerper>()
                                 .SetTargetPosition(target_pos);
        }

        while (achievements_shown.Count > 0 &&
               achievements_shown[0].GetComponent<RectTransform>()
                                    .anchoredPosition3D.x < -999) {
            // fell off screen
            Destroy(achievements_shown[0]);
            achievements_shown.RemoveAt(0);
            end_times.RemoveAt(0);
        }
    }

    public static Vector3 Times(Vector3 v, Vector3 rhs) {
        return new Vector3(v.x * rhs.x, v.y * rhs.y, v.z * rhs.z);
    }

    public GameObject GetBadge(GameObject badge_content) {
        GameObject container = Instantiate(achievement_container_prefab,
                                           new Vector3(0, 0, 0),
                                           Quaternion.identity);
        badge_content.transform.SetParent(container.transform, false);
        return container;
    }

    public void AddAchievement(GameObject badge_content, float duration=4f) {
        GameObject badge = GetBadge(badge_content);
        badge.transform.SetParent(canvas.transform, false);
        badge.GetComponent<RectTransform>().localPosition += 
                new Vector3(1000, 0, 0);
        achievements_shown.Add(badge);
        end_times.Add(Time.time + duration); // show badges for 4 seconds.
    }

    public void PlayNote(float time, float pitch, AudioClip clip) {
        Debug.Assert(clip != null, "Trying to play null sound.");
        GameObject container = new GameObject();
        DelayedDestroy dd = container.AddComponent<DelayedDestroy>();
        dd.SetTTL(6f); // this caps the duration to 6 seconds = 12 blocks

        AudioSource audio_source = container.AddComponent<AudioSource>();
        // TODO remove that component later
        audio_source.clip = clip;
        audio_source.pitch = pitch;
        audio_source.playOnAwake = false;
        audio_source.PlayScheduled(AudioSettings.dspTime + time);
    }

    public void PlayShape(List<(float x, int y)> shape, AudioClip clip) {
        Debug.Log("playing shape!");
        foreach ((float x, int y) p in shape) {
            //            // 0 (-0)
            // C -> E: +4 // 1 (-1), 6 (-2)
            // E -> A: +5 // 2 (-1), 7 (-2)
            // A -> D: +5 // 3 (-1), 8 (-2)
            // D -> G: +5 // 4 (-1), 9 (-2)
            // G -> C: +5 // 5 (-1), 10 (-2)
            Debug.Assert(p.y >= 0, "shapes go up!");
            int shift_size = p.y + (int)Mathf.Round(p.x)
                             - (int)Mathf.Round(shape.Count/2);
            int num_key_shift = shift_size * 5 - (int)Mathf.Ceil(shift_size/5f);
            Debug.Log($"x: {p.x}, height: {p.y}, keys: {num_key_shift}");

            float single_key_pitch_shift = Mathf.Pow(2, 1/12f);
            float total_pitch_shift = Mathf.Pow(single_key_pitch_shift,
                                                num_key_shift);

            float sec_per_beat = 60f/80f;  // sec_per_min / beat_per_min
            PlayNote(p.x * sec_per_beat / 4f, total_pitch_shift, clip);
            // pitch = 1 corresponds to no change
            // pitch = 2**(1/12) is up one key (?)
            // pitch = 2**(k/12) is up k keys (?)
            // pitch = 2 is up one octave (?)
        }
    }
}
