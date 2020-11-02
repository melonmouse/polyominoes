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
    public GridManager grid_manager;
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

    public void AchieveNewShape(Shape shape) {
        GameObject container = Instantiate(achievement_container_prefab,
                                           new Vector3(0, 0, 0),
                                           Quaternion.identity);
        container.transform.SetParent(canvas.transform, false);

        Rect bounds = new Rect(new Vector2(-65, -65), new Vector2(130, 130));
        GameObject drawing = grid_manager.draw_shape(shape, bounds, 1);
        drawing.transform.SetParent(container.transform, false);

        container.GetComponent<RectTransform>().localPosition += 
                new Vector3(1000, 0, 0);
        achievements_shown.Add(container);
        end_times.Add(Time.time + 4f); // show badges for 4 seconds.
    }
}
