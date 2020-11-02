using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Shape = System.Collections.Generic.SortedSet<(int x, int y)>;

public class GridManager : MonoBehaviour {
    public GameObject cell_prefab;
    public GameObject[,] cells;
    public PolyominoeDatabase polyominoe_database;

    public Shape selected_cells;
    int max_cell_count = 8;
    
    public (int x, int y) WorldCoordToIndex(Vector3 pos) {
        return ((int)Mathf.Floor(pos.x), (int)Mathf.Floor(pos.y));
    }

    public Vector3 IndexToWorldCoord(float i, float j) {
        return new Vector3(i, j, 0);
    }
    
    public void Start() {
        selected_cells = new Shape();
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

    // MYBRARY
    public Rect RectUnion(in Rect lhs, in Rect rhs) {
        Rect result = new Rect();
        result.min = Vector2.Min(lhs.min, rhs.min);
        result.max = Vector2.Max(lhs.max, rhs.max);
        return result;
    }

    public GameObject draw_shape(Shape shape, Rect bounds,
                                 int order_in_layer=0) {
        GameObject parent = new GameObject();
        Rect shape_bounds = new Rect();
        foreach ((int x, int y) p in shape) {
            shape_bounds.min =
                    Vector2.Min(shape_bounds.min,
                                IndexToWorldCoord(p.x-0.5f, p.y-0.5f));
            shape_bounds.max =
                    Vector2.Max(shape_bounds.max,
                                IndexToWorldCoord(p.x+0.5f, p.y+0.5f));
            // TODO might need different strategy when generalizing to
            // more grid types.
        }
        foreach ((int x, int y) p in shape) {
            Vector3 normalized_pos =
                (IndexToWorldCoord(p.x, p.y) - shape_bounds.center.Pad()) /
                (shape_bounds.size.Max()/2);
                // normalized_pos is in the [-1, 1]x[-1, 1] square
            Vector3 position =
                    bounds.center.Pad() + normalized_pos * bounds.size.Min()/2;
            float effective_size = bounds.size.Min()/shape_bounds.size.Max();
            GameObject cell = Instantiate(cell_prefab,
                                          position,
                                          Quaternion.identity);
            cell.transform.SetParent(parent.transform);

            CellState c = cell.GetComponent<CellState>();
            c.set_selected(true);
            c.set_image_mode(true);
            c.set_order_in_layer(order_in_layer);
            c.get_rect_transform().sizeDelta =
                    new Vector2(effective_size, effective_size);
        }
        return parent;
    }
}
