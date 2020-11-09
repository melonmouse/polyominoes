using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Assertions;
using TMPro;

using Shape = System.Collections.Generic.SortedSet<(int x, int y)>;
using Freqs = System.Collections.Generic.List<int>;

// Alternative grids --
// Square (4 or 8 neigh, knight neigh), Hex (6 neigh), Triangle (3 or 12 neigh)
// Penrose:
// - http://www.math.utah.edu/~treiberg/PenroseSlides.pdf
// - http://www.scollins.net/_personal/scollins/penrose/
// - each of the rhombusses has 10 different orientations (36deg)
// - all sides are same length
// - big rhombus: 72 + 108 deg (2+3), small rhombus: 36 + 144 (1+4)
// 3d wieringa roof:
// - not so accurate but CC-BY: https://sketchfab.com/3d-models/wieringa-roof-7ddc52decc914ed1bfa0f43abfd8e39e

public enum GridType {
    Square,
    Hexagon,
    Triangle,
}

public class PolyominoeDatabase : MonoBehaviour, IClickableObject {
    public AchievementManager achievement_manager;
    public GridManager grid_manager;

    public GameObject text_badge_prefab;
    public GameObject badge_drawer;
    public GameObject badge_drawer_container;

    protected int max_cells;
    protected int n_rotations;
    protected Dictionary<long, Shape>[] polyominoes_found;
    protected Dictionary<long, Shape>[] polyominoes_all;

    bool enable_partitions = false;
    Dictionary<long, Freqs>[] partitions_found;
    Dictionary<long, Freqs>[] partitions_all;

    void Start() {
        
    }

    public void RegisterClick() {
        bool was_active = badge_drawer_container.activeSelf;
        badge_drawer_container.SetActive(!was_active);
        if (!was_active) {
            RenderAllBadges();
        }
    }

    public int smallest_incomplete_polyominoe_set() {
        // skip 1 and 2
        for (int i = 3; i <= max_cells; i++) {
            if (polyominoes_all[i].Count != polyominoes_found[i].Count) {
                return i;
            }
        }
        return max_cells+1; // player finished the game
    }

    public long ShapeHash(Shape s) {
        long hash = 0;
        Debug.Assert(max_cells < 11, "Need to choose higher primes!");
        foreach ((int x, int y) in s) {
            hash += (x + 11*y);
            hash *= 13;  // some number higher than max_cells, coprime
            // with 2^63-1 (with factors 7, 73, 127, 337, and two big ones).
            // If this is a good hash function int64, collisions would appear
            // only after generating ~2**32 polyominoes. Tests with 32-bit hash
            // showed collisions from earlier than 2**16 onwards, suggesting the
            // hash function is not optimal.
            // The counts up to n=12 are correct, so OK in practice.
        }
        return hash;
    }

    public long FreqsHash(Freqs f) {
        long hash = 0;
        foreach (int fi in f) {
            hash += fi;
            hash *= 13;  // some number higher than max_cells, coprime
            // with 2^63-1 (with factors 7, 73, 127, 337, and two big ones).
        }
        return hash;
    }

    public void initialize_polymonioes() {
        polyominoes_found = new Dictionary<long, Shape>[max_cells+1];
        polyominoes_all = new Dictionary<long, Shape>[max_cells+1];
        for (int n_squares = 1; n_squares <= max_cells; n_squares++) {
            polyominoes_found[n_squares] = new Dictionary<long, Shape>();
            polyominoes_all[n_squares] = new Dictionary<long, Shape>();
        }
        Shape trivial = new Shape();
        trivial.Add((0, 0));
        trivial = get_canonical(trivial);
        polyominoes_all[1][ShapeHash(trivial)] = trivial;
        polyominoes_found[1][ShapeHash(trivial)] = trivial;
        for (int n_squares = 2; n_squares <= max_cells; n_squares++) {
            foreach (Shape small_shape in polyominoes_all[n_squares-1].Values) {
                foreach ((int x, int y) cell in small_shape) {
                    foreach ((int x, int y) new_cell in GetNeighbors(cell)) {
                        if (small_shape.Contains(new_cell)) {
                            continue;
                        }
                        Shape inc_shape = new Shape(small_shape); // should be deep copy
                        inc_shape.Add(new_cell);
                        inc_shape = get_canonical(inc_shape);
                        long hash = ShapeHash(inc_shape);
                        polyominoes_all[n_squares][hash] = inc_shape;  // no-op if duplicate
                    }
                }
            }
        }
    }

    public void initialize_partitions() {
        partitions_found = new Dictionary<long, Freqs>[max_cells+1];
        partitions_all = new Dictionary<long, Freqs>[max_cells+1];
        for (int n_squares = 1; n_squares <= max_cells; n_squares++) {
            partitions_found[n_squares] = new Dictionary<long, Freqs>();
            partitions_all[n_squares] = new Dictionary<long, Freqs>();
        }
        Freqs trivial = new Freqs();
        trivial.Add(1);
        make_canonical(trivial);
        partitions_all[1][FreqsHash(trivial)] = trivial;
        partitions_found[1][FreqsHash(trivial)] = trivial;
        for (int n_squares = 2; n_squares <= max_cells; n_squares++) {
            foreach (Freqs small_freqs in partitions_all[n_squares-1].Values) {
                for (int i = 0; i <= small_freqs.Count; i++) {
                    Freqs inc_freqs = new Freqs();
                    int j = 0;
                    foreach (int fi in small_freqs) {
                        inc_freqs.Add(fi + (i == j ? 1 : 0));
                        j++;
                    }
                    if (i == small_freqs.Count) {
                        inc_freqs.Add(1);
                    }
                    make_canonical(inc_freqs);
                    long hash = FreqsHash(inc_freqs);
                    partitions_all[n_squares][hash] = inc_freqs;  // no-op if duplicate
                }
            }
        }
    }

    static public (int x, int y, int z)
            triangle_storage_to_cube_coords(int i, int j) {
        // NOTE: the input x and y have are different axes than output x and y.
        int z = -j;
        int x = -i / 2 + (i < 0 ? (i % 2 + 2) % 2 : 0); 
        // NOTE: x is simply -x_/2 with integer division towards -inf.
        int y = (i % 2 + 2) % 2 - z - x;
        return (x, y, z);
    }

    static public (int i, int j)
        triangle_cube_to_storage_coords((int x, int y, int z) c) {
        return (-2*c.x + (c.x+c.y+c.z), -c.z);
        // note that the orientation of the triangle is constant,
        // so x_%2 = (x+y+z) must be constant.

        //cube.x + cube.y + cube.z ==
        //                         (new_cell.x % 2 + 2)%2 == i mod 2
    }

    static public (int x, int y, int z)
        rot_triangle_cube_once((int x, int y, int z) cube) {
        // Shift to lower right corner of the cube.
        // NOTE to keep stuff integer, we double the coordinates,
        // rather than just adding 0.5, -0.5, -0.5.
        (int x, int y, int z) cube_corner =
                (2*cube.x+1, 2*cube.y-1, 2*cube.z-1);
        // rotate w -> y -> z ->
        (int x, int y, int z) cube_corner_rot =
                (-cube_corner.y, -cube_corner.z, -cube_corner.x);
        // Shift back.
        Debug.Assert(cube_corner_rot.x % 2 != 0, "Should be odd");
        Debug.Assert(cube_corner_rot.y % 2 != 0, "Should be odd");
        Debug.Assert(cube_corner_rot.z % 2 != 0, "Should be odd");
        (int x, int y, int z) cube_rot =
                ((cube_corner_rot.x-1)/2, (cube_corner_rot.y+1)/2,
                 (cube_corner_rot.z+1)/2);
        return cube_rot;
    }

    public Shape rigid_transform(Shape cells, int rotation, bool mirror) {
        // Returns rotated/mirrored version of cells.
        Shape result = new Shape();
        foreach ( (int x, int y) cell in cells ) {
            (int x, int y) new_cell = cell;
            if (mirror) {
                switch (grid_type) {
                  case GridType.Square:
                    new_cell.x *= -1;
                  break;
                  case GridType.Triangle:
                    (int x, int y, int z) cube =
                            triangle_storage_to_cube_coords(new_cell.x,
                                                            new_cell.y);
                    (int x_, int y_) rot_storage =
                            triangle_cube_to_storage_coords(
                                    (cube.y, cube.x, cube.z));
                     new_cell.x = rot_storage.x_;
                     new_cell.y = rot_storage.y_;
                  break;
                  case GridType.Hexagon:
                    int x = new_cell.x;
                    int y = new_cell.y;
                    new_cell.x = y;
                    new_cell.y = x;
                  break;
                }
            }
            for (int rot = 0; rot < rotation; rot++) {
                // rotate clockwize once
                switch (grid_type) {
                  case GridType.Square:
                // (0, 1) -> (1, 0) -> (0, -1) -> (-1, 0) ->
                    int tmp = new_cell.y;
                    new_cell.y = new_cell.x;
                    new_cell.x = -tmp;
                  break;
                  case GridType.Hexagon:
                // (0, 1) -> (1, 0) -> (1, -1) -> (0, -1) -> (-1, 0) -> (-1, 1) ->
                  {
                    int z = -(new_cell.x + new_cell.y);
                    new_cell.y = -new_cell.x;
                    new_cell.x = -z;
                  }
                  break;
                  case GridType.Triangle:
                  {
                    // see triangle_grids_wtf.jpg
                    (int x, int y, int z) cube =
                            triangle_storage_to_cube_coords(new_cell.x,
                                                            new_cell.y);
                    Debug.Assert(cube.x + cube.y + cube.z ==
                                 (new_cell.x % 2 + 2)%2,
                                 "I don't understand triangles.");
                    (int x, int y, int z) rot_cube =
                                    rot_triangle_cube_once(cube);
                    (int x_, int y_) rot_storage =
                            triangle_cube_to_storage_coords(rot_cube);
                    new_cell.x = rot_storage.x_;
                    new_cell.y = rot_storage.y_;
                    // I think i need 3 more rotations (the flipped ones)
                  }
                  break;
                }
            }
            result.Add(new_cell);
        }
        return result;
    }

    public Shape normalize(Shape cells) {
        // Translates so the minimum coordinates are (0, 0).
        // The result is a canonical fixed polyominoe.
        int min_x = System.Int32.MaxValue;
        int min_y = System.Int32.MaxValue;
        foreach ( (int x, int y) cell in cells ) {
            if (cell.x < min_x) {
                min_x = cell.x;
            }
            if (cell.y < min_y) {
                min_y = cell.y;
            }
        }

        if (grid_type == GridType.Triangle) {
            // On the triangle grid, we can only translate (in x) over even
            // distances without changing the shape.
            min_x -= (min_x % 2 + 2) % 2;
            Debug.Assert(min_x % 2 == 0, "Cannot shift over odd x!");
        }

        Shape normalized_cells = new Shape();
        foreach ( (int x, int y) cell in cells ) {
            normalized_cells.Add((cell.x - min_x, cell.y - min_y));
        }
        return normalized_cells;
    }

    public void make_canonical(Freqs f) {
        f.Sort();
    }

    public Shape get_canonical(Shape cells, bool debug=false) {
        Shape canonical_shape = null;
        long best_hash = System.Int64.MaxValue;
        for (int rotation = 0; rotation < n_rotations; rotation++) {
            for (int mirror = 0; mirror < 2; mirror++) {
                Shape alt_cells =
                        normalize(rigid_transform(cells, rotation, mirror == 1));
                long hash = ShapeHash(alt_cells);
                if (debug) {
                    Debug.Log($"Transform ({rotation},{mirror}) = {hash}");
                    print_shape(alt_cells);
                }
                if (hash < best_hash) {
                    best_hash = hash;
                    canonical_shape = alt_cells;
                }
            }
        }
        return canonical_shape;
        // the first (according to some ordering) is canonical
        //return configurations.Min;
    }

    public enum NeighborhoodType {
        SquareNeumann,
        SquareMoore,
        Hexagon,
        HexagonJump,
        TriangleNeumann,
        TriangleMoore,
    }

    GridType grid_type;
    NeighborhoodType neighborhood_type;

    public List<(int x, int y)> GetNeighbors((int x, int y) cell) {
        List<int> dx = null;
        List<int> dy = null;

        switch (neighborhood_type) {
          case NeighborhoodType.SquareNeumann:
            dx = new List<int>{  1, -1,  0,  0};
            dy = new List<int>{  0,  0,  1, -1};
          break;
          case NeighborhoodType.SquareMoore:
            dx = new List<int>{  1, -1,  0,  0,  1,  1, -1, -1};
            dy = new List<int>{  0,  0,  1, -1,  1, -1,  1, -1};
          break;
          case NeighborhoodType.Hexagon:
            dx = new List<int>{  1, -1,  0,  0,  1, -1};
            dy = new List<int>{  0,  0,  1, -1, -1,  1};
          break;
          case NeighborhoodType.HexagonJump:
            dx = new List<int>{  2, -2,  0,  0,  2, -2, 1,-1,-2,-1, 1, 2};
            dy = new List<int>{  0,  0,  2, -2, -2,  2, 1, 2, 1,-1,-2,-1};
          break;
          case NeighborhoodType.TriangleNeumann:
            if (cell.x%2 == 0) {  // if x is even, the triangle points up
                dx = new List<int>{  1, -1,  1};
                dy = new List<int>{  0,  0, -1};
            } else {  // if x is odd, the triangle points down
                dx = new List<int>{  1, -1, -1};
                dy = new List<int>{  0,  0,  1};
            }
          break;
          case NeighborhoodType.TriangleMoore:
            if (cell.x%2 == 0) {  // if x is even, the triangle points up
                dx = new List<int>{  1, -1,  1,  2,  3,  2,  0, -1, -2, -2, -1,  0};
                dy = new List<int>{  0,  0, -1, -1, -1,  0,  1,  1,  1,  0, -1, -1};
            } else {  // if x is odd, the triangle points down
                dx = new List<int>{  1, -1, -1,  0,  1,  2,  2,  1,  0, -2, -3, -2};
                dy = new List<int>{  0,  0,  1, -1, -1, -1,  0,  1,  1,  1,  1,  0};
            }
          break;
          default:
            Debug.Assert(false, "NeighborhoodType not set to valid value.");
          break;
        }

        List<(int x, int y)> neighbors = new List<(int x, int y)>();
        for (int i = 0; i < dx.Count; i++) {
            neighbors.Add((cell.x + dx[i], cell.y + dy[i]));
        }
        return neighbors;
    }

    public void SetMode(NeighborhoodType t) {
        neighborhood_type = t;
        switch (t) {
          // TODO: we need to zoom out for the final levels
          // maybe allow pinch-zoom?
          case NeighborhoodType.SquareNeumann:
            max_cells = 8; // the level finishes after this (369)
            n_rotations = 4;
            grid_type = GridType.Square;
            // 1, 1, 2, 5, 12, 35, 108, 369, 1285, 4655, 17073, 63600, 238591
            //         OK,brz,slv,gold,plat
          break;
          case NeighborhoodType.SquareMoore:
            max_cells = 6; // the level finishes after this (524)
            n_rotations = 4;
            grid_type = GridType.Square;
            // 1, 2, 5, 22, 94, 524, 3031, 18770, 118133, 758381, 4915652
            //     brz,slv,gold,plat
          break;
          case NeighborhoodType.Hexagon:
            max_cells = 7; // the level finishes after this (333)
            n_rotations = 6;
            grid_type = GridType.Hexagon;
            // 1, 1, 3, 7, 22, 82, 333, 1448, 6572, 30490, 143552, 683101
            //        brz,slv,gld,plat
          break;
          case NeighborhoodType.HexagonJump:
            max_cells = 5; // the level finishes after this (675)
            n_rotations = 6;
            grid_type = GridType.Hexagon;
            // 1, 1, 1, 2, 9, 70, 675, 7863, 94721  (NOT ON OEIS)
            //        brz,slv,gld,plat
          break;
          case NeighborhoodType.TriangleNeumann:
            max_cells = 9; // the level finishes after this (448)
            n_rotations = 6;
            grid_type = GridType.Triangle;
            // 1, 1, 3, 4, 12, 24, 66, 160, 448, 1186, 3334, 9235, 26166, 73983
            //            brz,slv, gld,    plat
          break;
          case NeighborhoodType.TriangleMoore:
            max_cells = 5; // the level finishes after this (528)
            n_rotations = 6;
            grid_type = GridType.Triangle;
            // 1, 3, 11, 75, 528, 4573, 40497, 372453  (NOT ON OEIS)
            //      slv,gld,plat
          break;
        }
        // initialize right after
        initialize_polymonioes();
        if (enable_partitions) {
            initialize_partitions();
        }

        for (int i = 1; i<= max_cells; i++) {
            Debug.Log($"{i}: {polyominoes_all[i].Count}");
        }
    }

    public List<Shape> query(Shape cells) {
        // returns all components that had a match
        List<Shape> result = new List<Shape>();
        Shape visited = new Shape();
        Freqs component_sizes = new Freqs();
        foreach ((int x, int y) root in cells) {
            Shape component = new Shape();
            Shape neighbors = new Shape();
            neighbors.Add(root);

            while (neighbors.Count > 0) {
                (int x, int y) current_cell = neighbors.Min;
                neighbors.Remove(current_cell);
                if (visited.Contains(current_cell) ||
                    !cells.Contains(current_cell)) {
                    continue;
                }
                visited.Add(current_cell);
                component.Add(current_cell);

                foreach ((int x, int y) current_neighbor in
                         GetNeighbors(current_cell)) {
                    neighbors.Add(current_neighbor);
                }
            }
            if (component.Count == 0) {
                continue;
            }
            component_sizes.Add(component.Count);
            if (query_connected(component)) {
                // this component was new
                result.Add(component);
            }
        }
        if (enable_partitions) {
            query_freqs(component_sizes);
        }
        return result;
    }

    static public void print_shape(Shape s) {
        foreach ((int x, int y) p in s) {
            Debug.Log($"({p.x}, {p.y})");
        }
    }

    public void DestroyAllChildren(GameObject o) {
        for (int i = 0; i < o.transform.childCount; i++) {
            Destroy(o.transform.GetChild(i).gameObject);
        }
    }

    public void RenderAllBadges() {
        float y_offset = 140;
        int max_size = smallest_incomplete_polyominoe_set();

        DestroyAllChildren(badge_drawer);

        for (int size = 1; size <= max_size; size++) {
            // TODO show header for this size
            int badge_count = 0;

            int max_badge_y = 0;
            foreach (var item in polyominoes_all[size]) {
                int badge_x = badge_count % 4;
                int badge_y = badge_count / 4;
                GameObject badge_content;
                if (polyominoes_found[size].ContainsKey(item.Key)) {
                    Rect bounds = new Rect(
                            new Vector2(-65, -65), new Vector2(130, 130));
                    badge_content = grid_manager.draw_shape(item.Value,
                                                            bounds, 1);
                } else {
                    badge_content = Instantiate(text_badge_prefab, Vector3.zero,
                                                Quaternion.identity);
                    badge_content.GetComponent<TMP_Text>().text = "?";
                }
                GameObject badge = achievement_manager.GetBadge(badge_content);
                badge.GetComponent<ObjectLerper>().enabled = false;
                badge.transform.SetParent(badge_drawer.transform, false);
                RectTransform rt = badge.GetComponent<RectTransform>();
                rt.anchorMax = new Vector2(0, 1);
                rt.anchorMin = new Vector2(0, 1);
                rt.anchoredPosition3D = new Vector3(
                        (badge_x+0.5f)*200, -badge_y*200 - y_offset, 0);
                badge_count++;
                max_badge_y = badge_y;
            }
            y_offset += (max_badge_y+1.2f)*200;
        }
        badge_drawer.GetComponent<RectTransform>().sizeDelta =
                new Vector2(0, y_offset + 100);
        // dynamically scale the scroll area to size of content
    }

    public void AchieveNewShape(Shape shape) {
        Rect bounds = new Rect(new Vector2(-65, -65), new Vector2(130, 130));
        GameObject drawing = grid_manager.draw_shape(shape, bounds, 1);
        achievement_manager.AddAchievement(drawing, duration: 4f);
    }

    public void AchieveAllShape(int count) {
        Rect bounds = new Rect(new Vector2(-65, -65), new Vector2(130, 130));
        GameObject drawing = grid_manager.draw_circ(count, bounds, 1);
        achievement_manager.AddAchievement(drawing, 8f);
    }

    public void AchieveNewPartition(Freqs freqs) {
        GameObject text_badge = Instantiate(text_badge_prefab, Vector3.zero,
                                            Quaternion.identity);
        string str = "<nobr>";
        for (int i = freqs.Count-1; i >= 0; i--) {
            str += $"{freqs[i]}";
            if (i > 0) {
                str += "+";
            }
        }
        str += "</nobr>";
        text_badge.GetComponent<TMP_Text>().text = str;
        achievement_manager.AddAchievement(text_badge, 4f);
    }

    public void AchieveAllPartition(int count) {
        GameObject text_badge = Instantiate(text_badge_prefab, Vector3.zero,
                                            Quaternion.identity);
        text_badge.GetComponent<TMP_Text>().text = $"{count}*";
        achievement_manager.AddAchievement(text_badge, 6f);
    }

    public bool query_connected(Shape cells) {
        Shape polyominoe = get_canonical(cells);
        long hash = ShapeHash(polyominoe);
        if (!polyominoes_found[polyominoe.Count].ContainsKey(hash)) {
            Assert.IsTrue(polyominoes_all[polyominoe.Count].ContainsKey(hash),
                          "Found polyominoe that is not in database!");
            AchieveNewShape(polyominoe);
            polyominoes_found[polyominoe.Count][hash] = polyominoe;
            if (polyominoes_found[polyominoe.Count].Count == 
                polyominoes_all[polyominoe.Count].Count) {
                AchieveAllShape(polyominoe.Count);
            }
            return true;
        }
        return false;
    }

    public bool query_freqs(Freqs freqs) {
        make_canonical(freqs);
        long hash = FreqsHash(freqs);
        int sum = 0;
        foreach (int fi in freqs) {
            sum += fi;
        }
        if (sum > 0 && !partitions_found[sum].ContainsKey(hash)) {
            Assert.IsTrue(partitions_all[sum].ContainsKey(hash),
                          "Found partition that is not in database!");
            AchieveNewPartition(freqs);
            partitions_found[sum][hash] = freqs;
            if (partitions_found[sum].Count == 
                partitions_all[sum].Count) {
                AchieveAllPartition(sum);
            }
            return true;
        }
        return false;
    }


}

