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

public class PolyominoeDatabase : MonoBehaviour {
    public AchievementManager achievement_manager;
    public GridManager grid_manager;

    public GameObject text_badge_prefab;

    int max_squares = 8;
    Dictionary<long, Shape>[] polyominoes_found;
    Dictionary<long, Shape>[] polyominoes_all;

    bool enable_partitions = false;
    Dictionary<long, Freqs>[] partitions_found;
    Dictionary<long, Freqs>[] partitions_all;

    void Start() {
        
    }

    public int smallest_incomplete_polyominoe_set() {
        // skip 1 and 2
        for (int i = 3; i <= max_squares; i++) {
            if (polyominoes_all[i].Count != polyominoes_found[i].Count) {
                return i;
            }
        }
        return max_squares+1; // player finished the game
    }

    public long ShapeHash(Shape s) {
        long hash = 0;
        foreach ((int x, int y) in s) {
            hash += (x + max_squares*y);
            hash *= 13;  // some number higher than max_squares, coprime
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
            hash *= 13;  // some number higher than max_squares, coprime
            // with 2^63-1 (with factors 7, 73, 127, 337, and two big ones).
        }
        return hash;
    }

    public void initialize_polymonioes() {
        polyominoes_found = new Dictionary<long, Shape>[max_squares+1];
        polyominoes_all = new Dictionary<long, Shape>[max_squares+1];
        for (int n_squares = 1; n_squares <= max_squares; n_squares++) {
            polyominoes_found[n_squares] = new Dictionary<long, Shape>();
            polyominoes_all[n_squares] = new Dictionary<long, Shape>();
        }
        Shape trivial = new Shape();
        trivial.Add((0, 0));
        trivial = get_canonical(trivial);
        polyominoes_all[1][ShapeHash(trivial)] = trivial;
        polyominoes_found[1][ShapeHash(trivial)] = trivial;
        for (int n_squares = 2; n_squares <= max_squares; n_squares++) {
            foreach (Shape small_shape in polyominoes_all[n_squares-1].Values) {
                foreach ((int x, int y) cell in small_shape) {
                    for (int i = 0; i < dx.Count; i++) {
                        (int x, int y) new_cell = (cell.x + dx[i],
                                                   cell.y + dy[i]);
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
        partitions_found = new Dictionary<long, Freqs>[max_squares+1];
        partitions_all = new Dictionary<long, Freqs>[max_squares+1];
        for (int n_squares = 1; n_squares <= max_squares; n_squares++) {
            partitions_found[n_squares] = new Dictionary<long, Freqs>();
            partitions_all[n_squares] = new Dictionary<long, Freqs>();
        }
        Freqs trivial = new Freqs();
        trivial.Add(1);
        make_canonical(trivial);
        partitions_all[1][FreqsHash(trivial)] = trivial;
        partitions_found[1][FreqsHash(trivial)] = trivial;
        for (int n_squares = 2; n_squares <= max_squares; n_squares++) {
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

    public Shape rigid_transform(Shape cells, int rotation, bool mirror) {
        // Returns rotated/mirrored version of cells.
        Shape result = new Shape();
        foreach ( (int x, int y) cell in cells ) {
            (int x, int y) new_cell = cell;
            if (mirror) {
                new_cell.x *= -1;
            }
            for (int rot = 0; rot < rotation; rot++) {
                // rotate clockwize once
                // (0, 1) -> (1, 0) -> (0, -1) -> (-1, 0) ->
                int tmp = new_cell.y;
                new_cell.y = new_cell.x;
                new_cell.x = -tmp;
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

        Shape normalized_cells = new Shape();
        foreach ( (int x, int y) cell in cells ) {
            normalized_cells.Add((cell.x - min_x, cell.y - min_y));
        }
        return normalized_cells;
    }

    public void make_canonical(Freqs f) {
        f.Sort();
    }

    public Shape get_canonical(Shape cells) {
        Shape canonical_shape = null;
        long best_hash = System.Int64.MaxValue;
        for (int rotation = 0; rotation < 4; rotation++) {
            for (int mirror = 0; mirror < 2; mirror++) {
                Shape alt_cells =
                        normalize(rigid_transform(cells, rotation, mirror == 1));
                long hash = ShapeHash(alt_cells);
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
    }

    List<int> dx;
    List<int> dy;
    public void SetMode(NeighborhoodType t) {
        switch (t) {
          case NeighborhoodType.SquareNeumann:
            dx = new List<int>{  1, -1,  0,  0};
            dy = new List<int>{  0,  0,  1, -1};
            max_squares = 8; // the level finishes after this
          break;
          case NeighborhoodType.SquareMoore:
            dx = new List<int>{  1, -1,  0,  0,  1,  1, -1, -1};
            dy = new List<int>{  0,  0,  1, -1,  1, -1,  1, -1};
            max_squares = 6; // the level finishes after this
          break;
        }
        // initialize right after
        initialize_polymonioes();
        if (enable_partitions) {
            initialize_partitions();
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

                for (int i = 0; i < dx.Count; i++) {
                    neighbors.Add((current_cell.x + dx[i],
                                   current_cell.y + dy[i]));
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

    void print_shape(Shape s) {
        foreach ((int x, int y) p in s) {
            Debug.Log($"({p.x}, {p.y})");
        }
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

