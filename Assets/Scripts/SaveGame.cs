using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public static class SaveGame {
    static public Dictionary<NeighborhoodType, SaveLevel> save_levels =
            new Dictionary<NeighborhoodType, SaveLevel>();

    static public NeighborhoodType current_level;
    static public bool initialized = false;
}
