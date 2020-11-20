using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using UnityEngine;

public static class CurrentSaveGame {
    static public SaveGame save = new SaveGame();
    static public string save_file_name = "save.save";
}

[System.Serializable]
public class SaveGame {
    public Dictionary<NeighborhoodType, SaveLevel> save_levels =
            new Dictionary<NeighborhoodType, SaveLevel>();

    public NeighborhoodType current_level;

    public bool initialized = false;

    static public SaveGame LoadFromFile() {
        string path = Path.Combine(Application.persistentDataPath,
                                   CurrentSaveGame.save_file_name);
        Debug.Log($"Loading from {path}");
        if (File.Exists(path)) {
            FileStream f = File.Open(path, FileMode.Open);
            BinaryFormatter bf = new BinaryFormatter();
            SaveGame sg = (SaveGame)bf.Deserialize(f);
            f.Close();
            return sg;
            // TODO consider how to handle errors
        } else {
            return null;
        }
    }

    static public void SaveToFile(SaveGame sg) {
        // NOTE: this is non-blocking
        string path = Path.Combine(Application.persistentDataPath,
                                   CurrentSaveGame.save_file_name);
        Debug.Log($"Saving to {path}");
        // Note; this overwrites the file at path.
        FileStream f = File.Create(path);
        BinaryFormatter bf = new BinaryFormatter();
        bf.Serialize(f, sg);
        f.Close();
        // TODO consider how to handle errors
    }
}
