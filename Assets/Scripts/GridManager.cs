using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Shape = System.Collections.Generic.SortedSet<(int x, int y)>;

public class GridManager : MonoBehaviour {
    public GameObject cell_prefab;
    public GameObject cell_prefab_small;
    public GameObject[,] cells;
    public PolyominoeDatabase polyominoe_database;

    public Shape selected_cells;
    int max_cell_count = 3;
    
    public (int x, int y) WorldCoordToIndex(Vector3 pos) {
        return ((int)Mathf.Floor(pos.x), (int)Mathf.Floor(pos.y));
    }

    public Vector3 IndexToWorldCoord(float i, float j) {
        return new Vector3(i, j, 0);
    }
    
    public void Start() {
        polyominoe_database.SetMode(
                //PolyominoeDatabase.NeighborhoodType.SquareNeumann);
                PolyominoeDatabase.NeighborhoodType.SquareMoore);
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
            List<Shape> achieved_shapes = new List<Shape>();
            if (toggled_cell.is_selected()) {
                toggled_cell.set_selected(false);
                selected_cells.Remove(cell_index);
                achieved_shapes = polyominoe_database.query(selected_cells);
            } else {
                if (selected_cells.Count < max_cell_count) {
                    toggled_cell.set_selected(true);
                    selected_cells.Add(cell_index);
                    achieved_shapes = polyominoe_database.query(selected_cells);
                } else {
                    // TODO show something to show selection failed.
                }
            }

            foreach (Shape achieved_shape in achieved_shapes) {
                GameObject parent = new GameObject();
                Vector2 center = get_bounds(achieved_shape).center;
                parent.transform.position = center;
                foreach ((int x, int y) cell in achieved_shape) {
                    GameObject duplicate = Instantiate(cells[cell.x, cell.y]);
                    duplicate.transform.SetParent(parent.transform);
                    duplicate.GetComponent<CellState>().set_order_in_layer(2);
                    //// choose if achieved shapes get removed or not!
                    //// I think its better to not remove them.
                    //cells[cell.x, cell.y].GetComponent<CellState>()
                    //                     .set_selected(false);
                    //selected_cells.Remove(cell);
                }
                parent.AddComponent<TRotator>();
                parent.AddComponent<ObjectLerper>();
                parent.GetComponent<ObjectLerper>().rect_transform_mode = false;
                parent.GetComponent<ObjectLerper>().SetTargetPosition(
                        new Vector3(center.x + 30, center.y/2, 0));
            }

            max_cell_count =
                    polyominoe_database.smallest_incomplete_polyominoe_set();
        }
    }

    // MYBRARY
    public Rect RectUnion(in Rect lhs, in Rect rhs) {
        Rect result = new Rect();
        result.min = Vector2.Min(lhs.min, rhs.min);
        result.max = Vector2.Max(lhs.max, rhs.max);
        return result;
    }

    public GameObject draw_squares(List<Vector3> normalized_positions,
                                   Rect bounds, float block_size,
                                   int order_in_layer=0) {
        GameObject parent = new GameObject();
        parent.AddComponent<RectTransform>();
        foreach (Vector3 normalized_pos in normalized_positions) {
            Vector3 position =
                    bounds.center.Pad() + normalized_pos * bounds.size.Min()/2;
            GameObject cell = Instantiate(cell_prefab_small,
                                          position,
                                          Quaternion.identity);
            cell.transform.SetParent(parent.transform);

            CellState c = cell.GetComponent<CellState>();
            c.set_selected(true);
            c.set_image_mode(true);
            c.set_order_in_layer(order_in_layer);
            c.get_rect_transform().sizeDelta =
                    new Vector2(block_size, block_size);
        }
        return parent;
    }

    
    public Rect get_bounds(Shape shape) {
        Debug.Assert(shape.Count > 0, "cannot get bounds of empty shape");
        Rect shape_bounds = new Rect();
        shape_bounds.min = new Vector2(shape.First().x, shape.First().y);
        shape_bounds.max = shape_bounds.min;
        
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
        return shape_bounds;
    }


    public GameObject draw_shape(Shape shape, Rect bounds,
                                 int order_in_layer=0) {
        Rect shape_bounds = get_bounds(shape);
        float block_size = bounds.size.Min()/shape_bounds.size.Max();
        List<Vector3> normalized_positions = new List<Vector3>();
        foreach ((int x, int y) p in shape) {
            Vector3 normalized_pos =
                (IndexToWorldCoord(p.x, p.y) - shape_bounds.center.Pad()) /
                (shape_bounds.size.Max()/2);
                // normalized_pos is in the [-1, 1]x[-1, 1] square
            normalized_positions.Add(normalized_pos);
        }
        return draw_squares(normalized_positions, bounds, block_size, 
                            order_in_layer);
    }

    public GameObject draw_circ(int count, Rect bounds, int order_in_layer=0) {
        List<Vector3> normalized_positions = new List<Vector3>();
        float offset_rad = 0;
        if (count % 2 == 1) {
            offset_rad = 2 * Mathf.PI * 1 / (4*count);
        }
        for (int block_i = 0; block_i < count; block_i += 1) {
            float rad = 2 * Mathf.PI * block_i/count + offset_rad;
            normalized_positions.Add(
                    new Vector3(0.7f*Mathf.Cos(rad), 0.7f*Mathf.Sin(rad), 0));
                    // deduplicate part from draw_shape TODO
        }
        float block_size =
                Mathf.Min(0.9f*Mathf.Sqrt(2)/2*(Mathf.PI * 0.7f * bounds.size.Max())/count,
                          bounds.size.Max()/2);
        GameObject badge_content =  draw_squares(normalized_positions, bounds, 
                                                 block_size, order_in_layer);

        float rot_speed = 360/2;
        foreach (RectTransform rt in
                 badge_content.GetComponentsInChildren<RectTransform>()) {
            // make all RectTransforms rotate (including parent recttransform)
            rt.gameObject.AddComponent<RTRotator>();
            rt.gameObject.GetComponent<RTRotator>().SetRotationSpeed(rot_speed);
        }
        // reverse direction of parent rotation so children have global rot 0
        badge_content.GetComponent<RTRotator>().SetRotationSpeed(-rot_speed);
        return badge_content;
    }
}
