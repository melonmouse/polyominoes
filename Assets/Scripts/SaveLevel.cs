using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SaveLevel {
    public SaveLevel() {
        for (int i = 0; i < 100; i++) {
            hashes_of_shapes_found.Add(new List<long>());
        }
    }

    public NeighborhoodType neighborhood_type;
    public List<List<long> > hashes_of_shapes_found = new List<List<long> >();
    public int max_cells = 0;  // keep track for score
}
