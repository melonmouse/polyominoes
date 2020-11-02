using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Shape = System.Collections.Generic.SortedSet<(int x, int y)>;

public class GridManager : MonoBehaviour {
    public GameObject cell_prefab;
    public GameObject[,] cells;
    public PolyominoeDatabase polyominoe_database;

    public Shape selected_cells;
    int max_cell_count = 10;
    
    public (int x, int y) WorldCoordToIndex(Vector3 pos) {
        return ((int)Mathf.Floor(pos.x), (int)Mathf.Floor(pos.y));
    }

    public Vector3 IndexToWorldCoord(int i, int j) {
        return new Vector3(i, j, 0);
    }
    
    public void Start() {
        selected_cells = new Shape();
        polyominoe_database = new PolyominoeDatabase();
        int size = (int)Mathf.Ceil(4*Camera.main.orthographicSize);
        cells = new GameObject[size, size];
        for (int i = 0; i<size; i++)
        for (int j = 0; j<size; j++) {
            cells[i,j] = Instantiate(cell_prefab, IndexToWorldCoord(i, j),
                                     Quaternion.identity);
        }
    }

    public void Update() {
        if (Input.GetMouseButtonDown(0)) {
            Vector3 world_pos =
                    Camera.main.ScreenToWorldPoint(Input.mousePosition);
            (int x, int y) cell_index = WorldCoordToIndex(world_pos);
            CellState toggled_cell =
                    cells[cell_index.x, cell_index.y].GetComponent<CellState>();
            if (toggled_cell.is_selected()) {
                toggled_cell.set_selected(false);
                selected_cells.Remove(cell_index);
                polyominoe_database.query(selected_cells);
            } else {
                if (selected_cells.Count < max_cell_count) {
                    toggled_cell.set_selected(true);
                    selected_cells.Add(cell_index);
                    polyominoe_database.query(selected_cells);
                } else {
                    // TODO show something to show selection failed.
                }
            }
        }
    }
}
