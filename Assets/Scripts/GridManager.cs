using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

using Shape = System.Collections.Generic.SortedSet<(int x, int y)>;

public class GridManager : MonoBehaviour, IClickableObject {
    GameObject cell_prefab;
    GameObject cell_prefab_small;

    public GameObject menu_button;

    public GameObject cell_prefab_square;
    public GameObject cell_prefab_square_small;
    public GameObject cell_prefab_hex;
    public GameObject cell_prefab_hex_small;
    public GameObject cell_prefab_triangle;
    public GameObject cell_prefab_triangle_flipped;
    public GameObject cell_prefab_triangle_small;
    public GameObject cell_prefab_triangle_small_flipped;

    public Dictionary<(int, int),GameObject> cells;
    public PolyominoeDatabase polyominoe_database;
    public Shape selected_cells;

    public List<AudioClip> cell_sounds;
    public List<AudioClip> cell_sounds_long;

    public int max_cell_count = 3;
    public DiscreteValueIndicator cell_count_indicator;
    public TMP_Text shape_progress_counter;
    public GameObject tutorial_three;
    public GameObject tutorial_can_you_make_size_four;
    public GameObject tutorial_see_shapes;
    public GameObject make_sure_to_deselect;
    public GameObject tutorial_done_text;
    public GameObject level_unlocked_message;
    bool tutorial_done = false;

    GridType grid_type = GridType.Hexagon;
    //NeighborhoodType neighborhood_type = NeighborhoodType.SquareNeumann;
    //NeighborhoodType neighborhood_type = NeighborhoodType.TriangleNeumann;
    NeighborhoodType neighborhood_type = NeighborhoodType.Hexagon;

    bool initialized_camera_size = false;

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
            // NOTE: does not round perfectly - only accurate near center.
            return ((int)Mathf.Round(2/3f * pos.x / hex_size),
                    (int)Mathf.Round((-1/3f * pos.x +
                                      Mathf.Sqrt(3f)/3f * pos.y) / hex_size));
          case GridType.Triangle:
            // NOTE: does not round perfectly - only accurate near center.
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

    public float GetTargetOrthographicSize(int cell_count=-1) {
        if (cell_count < 0) {
            cell_count = max_cell_count;
        }
        float current_height = Camera.main.orthographicSize * 2;
        float current_width = Camera.main.aspect * current_height;
        float min_size = cell_count + 2;
        if (neighborhood_type == NeighborhoodType.TriangleNeumann) {
            //min_size = (int)Mathf.Ceil(cell_count + 2);
        }
        if (neighborhood_type == NeighborhoodType.TriangleMoore) {
            min_size = 2*cell_count + 4;
        }
        if (neighborhood_type == NeighborhoodType.Hexagon) {
            //min_size = (int)Mathf.Ceil(1f*cell_count+2*1.2f);
        }
        if (neighborhood_type == NeighborhoodType.HexagonJump) {
            min_size = (int)Mathf.Ceil(2*cell_count+4f);
        }

        float target_orthographic_size;
        // we want width >= 
        // and height >= cell_count+2
        target_orthographic_size =
                Mathf.Max(1.25f*min_size / 2,  // height fits
                          min_size / (2 * Camera.main.aspect));  // width fits
        // width = 8 = aspect*height = aspect * orthographicSize * 2
        // orthographicSize = 8/(2 * aspect)

        return target_orthographic_size;
    }

    int invalid_click_count = 0;
    float tutorial_done_message_start_time = -1f;
    float tutorial_see_shapes_start_time = -1f;
    float tutorial_size_four_start_time = -1f;
    float level_unlocked_timer = -1f;

    public void Update() {
        if (Input.GetKeyUp(KeyCode.X)) {
            polyominoe_database.cheat_to_next_level();
        }

        if (Input.GetKeyUp(KeyCode.Escape) ||
            Input.GetKeyUp(KeyCode.Pause) ||
            Input.GetKeyUp(KeyCode.Backspace)) {
            SaveAndExit();
        }

        if (!tutorial_done) {
            if (max_cell_count == 3) {
                make_sure_to_deselect.SetActive(invalid_click_count >= 1);
            }
            if (max_cell_count == 4) {
                make_sure_to_deselect.SetActive(invalid_click_count >= 2);

                // TODO (not here?) add counter showing how many
                // shapes you collected/have to go
                if (polyominoe_database.get_num_shapes_found(size: 4) >= 3) {
                    if (tutorial_see_shapes_start_time < 0) {
                        tutorial_see_shapes_start_time = Time.time + 4f;
                    }
                    tutorial_see_shapes.SetActive(
                            Time.time < tutorial_see_shapes_start_time);

                    // stop showing the make-all-size-4-shapes message
                    tutorial_size_four_start_time = 0.1f;
                }

                if (tutorial_size_four_start_time < 0) {
                    tutorial_size_four_start_time = Time.time + 10f;
                    // 10 seconds is long, but it will be hidden once three
                    // shapes are created
                }
                tutorial_can_you_make_size_four.SetActive(
                            Time.time < tutorial_size_four_start_time);
            }
            if (max_cell_count == 5) {
                // show message for 7 seconds
                if (tutorial_done_message_start_time < 0) {
                    tutorial_done_message_start_time = Time.time + 7f;
                }
                if (Time.time < tutorial_done_message_start_time) {
                    tutorial_done_text.SetActive(true);
                    invalid_click_count = 0;
                    // avoid overlap with deselect message
                } else {
                    tutorial_done_text.SetActive(false);
                }
                        
            }
            menu_button.SetActive(max_cell_count > 4);
            tutorial_three.SetActive(max_cell_count < 4);
            // only allow going back to the menu after completing tutorial
        }

        level_unlocked_message.SetActive(tutorial_done &&
                                         level_unlocked_timer >= 0 &&
                                         Time.time < level_unlocked_timer);
        
        cell_count_indicator.max_value = max_cell_count;
        cell_count_indicator.value = selected_cells.Count;
    }

    public void FixedUpdate() {
        if (!initialized_camera_size) {
            Camera.main.orthographicSize = GetTargetOrthographicSize();
            initialized_camera_size = true;
        }
        Camera.main.orthographicSize = 0.98f * Camera.main.orthographicSize +
                                       0.02f * GetTargetOrthographicSize();
    }

    public void StoreSaveLevel() {
        SaveLevel sl = new SaveLevel();
        sl.hashes_of_shapes_found = polyominoe_database.GetFoundHashes();
        sl.neighborhood_type = neighborhood_type;
        sl.max_cells = polyominoe_database.biggest_complete_polyominoe_set();
        sl.score = (int)Mathf.Max(sl.max_cells, sl.score);
        CurrentSaveGame.save.save_levels[neighborhood_type] = sl;
        SaveGame.SaveToFile(CurrentSaveGame.save);
    }

    public void SaveAndExit() {
        StoreSaveLevel();
        SceneManager.LoadScene("Menu");
    }

    public void Start() {
        if (!CurrentSaveGame.save.initialized) {
            // Should only be possible when debugging!
            CurrentSaveGame.save.save_levels[neighborhood_type] = new SaveLevel();
            CurrentSaveGame.save.save_levels[neighborhood_type].neighborhood_type =
                    neighborhood_type;
            CurrentSaveGame.save.current_level = neighborhood_type;
        }

        Camera.main.orthographicSize = GetTargetOrthographicSize();
        
        LoadSaveLevel();

        UpdateShapesFoundCounter();

        // avoid showing tutorial again when returning to SquareNeumann,
        // and don't show it in other levels at all
        tutorial_done = neighborhood_type != NeighborhoodType.SquareNeumann ||
                        max_cell_count >= 5;

    }

    public void LoadSaveLevel() {
        SaveLevel save_level = CurrentSaveGame.save.save_levels[
                CurrentSaveGame.save.current_level];
        neighborhood_type = save_level.neighborhood_type;
        Debug.Assert(neighborhood_type == CurrentSaveGame.save.current_level);
        grid_type = PolyominoeDatabase.neighborhood_to_grid[neighborhood_type];

        polyominoe_database.SetMode(neighborhood_type);
        polyominoe_database.AddFoundHashes(save_level.hashes_of_shapes_found);

        SetMaxCellCount();

        InitializeGrid();

        selected_cells = new Shape();

        // TODO switch to other sound based on grid type
        switch (grid_type) {
          case GridType.Square:
            polyominoe_database.clip = cell_sounds[0];
            polyominoe_database.clip_long = cell_sounds_long[0];
          break;
          case GridType.Triangle:
            polyominoe_database.clip = cell_sounds[1];
            polyominoe_database.clip_long = cell_sounds_long[1];
          break;
          case GridType.Hexagon:
            polyominoe_database.clip = cell_sounds[2];
            polyominoe_database.clip_long = cell_sounds_long[2];
          break;
        }
    }

    public void SetMaxCellCount() {
        max_cell_count =
                polyominoe_database.biggest_complete_polyominoe_set() + 1;
        max_cell_count = (int)Mathf.Max(3, max_cell_count);

        if (max_cell_count > polyominoe_database.GetMaxCells()) {
            max_cell_count = polyominoe_database.GetMaxCells();
        }
    }

    public void InitializeGrid() {
        switch (grid_type) {
          case GridType.Square:
            cell_prefab = cell_prefab_square;
            cell_prefab_small = cell_prefab_square_small;
          break;
          case GridType.Triangle:
            cell_prefab = cell_prefab_triangle;
            cell_prefab_small = cell_prefab_triangle_small;
          break;
          case GridType.Hexagon:
            cell_prefab = cell_prefab_hex;
            cell_prefab_small = cell_prefab_hex_small;
          break;
        }

        int size = 30; // should be sufficient for all levels and aspect ratios
        float max_orthographic_size =
                GetTargetOrthographicSize(polyominoe_database.GetMaxCells());
        Debug.Log($"orth size max: {max_orthographic_size}");

        float max_world_height = 2 * max_orthographic_size;
        float max_world_width = max_world_height * Camera.main.aspect;

        Debug.Log($"max world size: {max_world_height}x{max_world_width}");

        float max_cell_y = max_world_height/2f + 2f;
        float max_cell_x = max_world_width/2f + 2f;
        
        cells = new Dictionary<(int, int),GameObject>();
        for (int i = -size; i<size; i++)
        for (int j = -size; j<size; j++) {
            // TODO? use a shape / draw_cells here
            Vector3 cell_pos = IndexToWorldCoord(i, j);
            if (cell_pos.x < -max_cell_x || cell_pos.x > max_cell_x ||
                cell_pos.y < -max_cell_y || cell_pos.y > max_cell_y) {
                continue;
            }
            if (grid_type == GridType.Triangle && (i % 2 + 2) % 2 == 1) {
                cells[(i,j)] = Instantiate(cell_prefab_triangle_flipped,
                                           cell_pos,
                                           Quaternion.identity);
            } else {
                cells[(i,j)] = Instantiate(cell_prefab, cell_pos,
                                           Quaternion.identity);
            }
            cells[(i,j)].GetComponent<CellState>().coordinate = (i, j);

            //////// Put coordinates in cells (for debugging, if needed)
            //string str = "";
            //str += $"({i},{j})";
            //(int x, int y, int z) p =
            //        PolyominoeDatabase.triangle_storage_to_cube_coords(i, j);
            //str += $"\n({p.x},{p.y},{p.z})";
            //str += $"\n({p.x*2+1},{p.y*2-1},{p.z*2-1})";
            //cells[(i,j)].GetComponent<CellState>().set_text(str);
            ////////
        }
    }
    
    public void RegisterClick() {
        int prior_total_score = LevelBariers.get_total_score();
        // store score before processing click, so we can see if it increased

        Vector3 world_pos =
                Camera.main.ScreenToWorldPoint(Input.mousePosition);

        RaycastHit2D hit = Physics2D.Raycast(
                new Vector2(world_pos.x, world_pos.y), Vector2.zero);
        if (hit.collider == null){
            return;
        }
        (int x, int y) cell_index = hit.collider.transform.parent.gameObject
                                       .GetComponent<CellState>().coordinate;


        // // The following works, but rounding can be off.
        //(int x, int y) cell_index = WorldCoordToIndex(world_pos);

        CellState toggled_cell =
                cells[(cell_index.x, cell_index.y)].GetComponent<CellState>();
        List<Shape> achieved_shapes = new List<Shape>();
        if (toggled_cell.is_selected()) {
            toggled_cell.set_selected(false);
            selected_cells.Remove(cell_index);
            achieved_shapes = polyominoe_database.query(selected_cells);

            invalid_click_count = 0;  // used for the tutorial
        } else {
            if (selected_cells.Count < max_cell_count) {
                toggled_cell.set_selected(true);
                selected_cells.Add(cell_index);
                achieved_shapes = polyominoe_database.query(selected_cells);
            } else {
                Camera.main.gameObject.GetComponent<RandomShake>()
                      .StartShake(duration: 0.5f, amplitude: 0.05f);
                cell_count_indicator.BoostVisibility();

                invalid_click_count++;  // used for the tutorial
            }
        }

        foreach (Shape achieved_shape in achieved_shapes) {
            GameObject parent = new GameObject();
            Rect bounds = get_bounds(achieved_shape);
            Vector2 center = bounds.center;
            parent.transform.position = center;
            foreach ((int x, int y) cell in achieved_shape) {
                GameObject duplicate = Instantiate(cells[(cell.x, cell.y)]);
                duplicate.transform.SetParent(parent.transform);
                //// TODO Do i really need to set order_in_layer?
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

            float height = Camera.main.orthographicSize*2;
            float width = height * Camera.main.aspect;

            Vector3 dist = 5*new Vector3(width + bounds.size[0] + 2,
                                         height*0.6f + bounds.size[1]/2f, 0);
            parent.GetComponent<ObjectLerper>().SetTargetPosition(dist);
        }

        SetMaxCellCount();
        int cells_left = selected_cells.Count - max_cell_count;

        if (achieved_shapes.Count > 0) {
            // we have new achievements, so save progress.
            StoreSaveLevel();
            UpdateShapesFoundCounter();

            int total_score = LevelBariers.get_total_score();
            if (total_score > prior_total_score) {
                // we just had a score increase
                if (LevelBariers.get_max_level(total_score) >
                    LevelBariers.get_max_level(total_score-1) ) {
                    // score increase caused unlock of new level!
                    level_unlocked_timer = Time.time + 7f;
                }
            }
        }
    }

    public void UpdateShapesFoundCounter() {
        // If level is maxed out, replace the counter
        Debug.Log($"max_cell_count={max_cell_count}");
        Debug.Log($"database_max={polyominoe_database.GetMaxCells()}");
        if (polyominoe_database.biggest_complete_polyominoe_set() ==
            polyominoe_database.GetMaxCells()) {
            shape_progress_counter.text = "DONE!";
            return;
        }

        int shapes_found =
                polyominoe_database.get_num_shapes_found(max_cell_count);
        int shapes_exist =
                polyominoe_database.get_num_shapes_exist(max_cell_count);
        if (shapes_exist >= 10) {
            shape_progress_counter.text = $"{shapes_found} / {shapes_exist}";
        } else {
            shape_progress_counter.text = "";
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
            // TODO do I really need to set order in layer here?
            //c.set_order_in_layer(order_in_layer);
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
            foreach ((int x, int y) p in shape) {
                // we rely on ordering of shape being constant
                if (p.x % 2 != 0) {
                    prefabs.Add(cell_prefab_triangle_small_flipped);
                } else {
                    prefabs.Add(cell_prefab_small);
                }
            }
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
        if (grid_type == GridType.Triangle) {
            block_size /= 1.9f;
        }
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
