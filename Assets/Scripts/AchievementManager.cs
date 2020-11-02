﻿using System.IO;
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
}

public class AchievementManager : MonoBehaviour {
    public GridManager grid_manager;
    public GameObject achievement_container_prefab;
    public GameObject canvas;

    public static Vector3 Times(Vector3 v, Vector3 rhs) {
        return new Vector3(v.x * rhs.x, v.y * rhs.y, v.z * rhs.z);
    }

    public void AchieveNewShape(Shape shape) {
        GameObject container = Instantiate(achievement_container_prefab,
                                           new Vector3(0, 0, 0),
                                           Quaternion.identity);
        container.transform.SetParent(canvas.transform, false);

        Rect bounds = new Rect(new Vector2(-30, -30), new Vector2(60, 60));
        GameObject drawing = grid_manager.draw_shape(shape, bounds, 1);
        drawing.transform.SetParent(container.transform, false);
    }
}
