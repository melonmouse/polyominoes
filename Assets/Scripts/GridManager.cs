using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Shape = System.Collections.Generic.SortedSet<(int x, int y)>;

public class GridManager : MonoBehaviour {
    GameObject cell_prefab;
    GameObject cell_prefab_small;

    public GameObject cell_prefab_square;
    public GameObject cell_prefab_square_small;
    public GameObject cell_prefab_hex;
    public GameObject cell_prefab_hex_small;
    public GameObject cell_prefab_triangle;
    public GameObject cell_prefab_triangle_small;

    public Dictionary<(int, int),GameObject> cells;
    public PolyominoeDatabase polyominoe_database;

    public Shape selected_cells;
    int max_cell_count = 3;
    GridType grid_type = GridType.Square;
    
    public enum GridType {
        Square,
        Hexagon,
        Triangle,
        // TODO nit deduplicate against version in PolyominoeDatabase
    }

    static float hex_size = 0.6f;
    Vector3 hex_x_base =
            new Vector3(3f/2f * hex_size, Mathf.Sqrt(3f)/2f * hex_size, 0);
    Vector3 hex_y_base =
            new Vector3(0, Mathf.Sqrt(3) * hex_size, 0);

    static float triangle_size = 1.8f;
    Vector3 triangle_x_base =
            new Vector3(1f/2f * triangle_size, 0, 0);
    Vector3 triangle_y_base =
            new Vector3(1f/2f * triangle_size,
                        Mathf.Sqrt(3f)/2f * triangle_size,
                        0);

    public (int x, int y) WorldCoordToIndex(Vector3 pos) {
        switch (grid_type) {
          case GridType.Square:
            return ((int)Mathf.Floor(pos.x), (int)Mathf.Floor(pos.y));
          case GridType.Hexagon:
            // TODO round in a way that is perfect
            return ((int)Mathf.Round(2/3f * pos.x / hex_size),
                    (int)Mathf.Round((-1/3f * pos.x +
                                      Mathf.Sqrt(3f)/3f * pos.y) / hex_size));
          case GridType.Triangle:
            // TODO round in a way that is perfect
            return ((int)Mathf.Round((pos.x - pos.y / Mathf.Sqrt(3f)) *
                                     2/triangle_size),
                    (int)Mathf.Round(pos.y / Mathf.Sqrt(3f) * 2/triangle_size));
            
        }
        Debug.Assert(false, "grid_type not set to supported value");
        return (0,0);
    }

    public Vector3 IndexToWorldCoord(float i, float j) {
        switch (grid_type) {
          case GridType.Square:
            return new Vector3(i, j, 0);
          case GridType.Hexagon:
            return i*hex_x_base + j*hex_y_base;
          case GridType.Triangle:
            return i*triangle_x_base + j*triangle_y_base;
        }
        Debug.Assert(false, "grid_type not set to supported value");
        return Vector3.zero;
    }
    
    public void Start() {
        //// Square mode
        //grid_type = GridType.Square;
        //grid_type = GridType.Hexagon;
        grid_type = GridType.Triangle;
        switch (grid_type) {
          case GridType.Square:
            polyominoe_database.SetMode(
                    PolyominoeDatabase.NeighborhoodType.SquareNeumann);
                    //PolyominoeDatabase.NeighborhoodType.SquareMoore);
            cell_prefab = cell_prefab_square;
            cell_prefab_small = cell_prefab_square_small;
          break;
          case GridType.Hexagon:
            polyominoe_database.SetMode(
                    PolyominoeDatabase.NeighborhoodType.Hexagon);
            cell_prefab = cell_prefab_hex;
            cell_prefab_small = cell_prefab_hex_small;
          break;
          case GridType.Triangle:
            polyominoe_database.SetMode(
                    PolyominoeDatabase.NeighborhoodType.TriangleNeumann);
                    //PolyominoeDatabase.NeighborhoodType.TriangleMoore);
            cell_prefab = cell_prefab_triangle;
            cell_prefab_small = cell_prefab_triangle_small;
          break;
        }
        
        selected_cells = new Shape();
        int size = (int)Mathf.Ceil(4*Camera.main.orthographicSize);
        cells = new Dictionary<(int, int),GameObject>();
        for (int i = -size; i<size; i++)
        for (int j = -size; j<size; j++) {
            // TODO use a shape / draw_cells here
            cells[(i,j)] = Instantiate(cell_prefab, IndexToWorldCoord(i, j),
                                       Quaternion.identity);

            ////////
            string str = "";
            str += $"({i},{j})";
            (int w, int y, int z) p =
                    PolyominoeDatabase.get_triangle_cube_coordinates(i, j);
            str += $"\n({p.w},{p.y},{p.z})";
            cells[(i,j)].GetComponent<CellState>().set_text(str);
            ////////
            
            if (grid_type == GridType.Triangle) {
                if ((i % 2 + 2) % 2 == 1) {
                    cells[(i,j)].transform.localRotation =
                            Quaternion.Euler(0, 0, 180f);
                    cells[(i,j)].GetComponent<CellState>().debug_text
                                .gameObject.transform.localRotation = 
                            Quaternion.Euler(0, 0, 180f);
                }
            }
        }
    }

    public void Update() {
        if (Input.GetMouseButtonDown(0)) {
            Vector3 world_pos =
                    Camera.main.ScreenToWorldPoint(Input.mousePosition);
            (int x, int y) cell_index = WorldCoordToIndex(world_pos);

            CellState toggled_cell =
                    cells[(cell_index.x, cell_index.y)].GetComponent<CellState>();
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
                    GameObject duplicate = Instantiate(cells[(cell.x, cell.y)]);
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

    public GameObject draw_cells(List<Vector3> normalized_positions,
                                 List<GameObject> prefabs,
                                 Rect bounds, float block_size,
                                 int order_in_layer=0) {
        // TODO generalize to allow different prefabs (for e.g. triangles)
        GameObject parent = new GameObject();
        parent.AddComponent<RectTransform>();
        Debug.Assert(prefabs.Count == 1 ||
                     normalized_positions.Count == prefabs.Count,
                     "Mismatch between prefab and position counts");
        for (int i = 0; i < normalized_positions.Count; i++) {
            Vector3 normalized_pos = normalized_positions[i];
            Vector3 position =
                    bounds.center.Pad() + normalized_pos * bounds.size.Min()/2;
            GameObject prefab = (prefabs.Count == 1 ? prefabs[0] : prefabs[i]);
            GameObject cell = Instantiate(prefab,
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
        List<GameObject> prefabs = new List<GameObject>();
        switch (grid_type) {
          case GridType.Square:
          case GridType.Hexagon:
            prefabs.Add(cell_prefab_small);
          break;
          case GridType.Triangle:
            GameObject flipped_prefab = Instantiate(cell_prefab_small);
            flipped_prefab.transform.localRotation =
                            Quaternion.Euler(0, 0, 180f);
            foreach ((int x, int y) p in shape) {
                // we rely on ordering of shape being constant
                if (p.x % 2 == 1) {
                    prefabs.Add(flipped_prefab);
                } else {
                    prefabs.Add(cell_prefab_small);
                }
            }
            Destroy(flipped_prefab);
          break;
        }
        return draw_cells(normalized_positions, prefabs, bounds, block_size, 
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
        List<GameObject> prefabs = new List<GameObject>();
        prefabs.Add(cell_prefab_small);
        GameObject badge_content = draw_cells(normalized_positions, prefabs,
                                              bounds, block_size,
                                              order_in_layer);

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
