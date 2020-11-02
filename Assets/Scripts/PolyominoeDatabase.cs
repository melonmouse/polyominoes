using System.Collections;
using System.Collections.Generic;
using UnityEngine;


using Shape = System.Collections.Generic.SortedSet<(int x, int y)>;

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

    int max_squares = 8;
    Dictionary<long, Shape>[] polyominoes_found;
    Dictionary<long, Shape>[] polyominoes_all;

    void Start() {
        initialize();
    }

    public long ShapeHash(Shape s) {
        long hash = 0;
        foreach ((int x, int y) in s) {
            hash += (x + max_squares*y);
            hash *= 13;  // some prime number higher than max_squares
            // If this is a good hash function int64, collisions would appear
            // only after generating ~2**32 polyominoes. Tests with 32-bit hash
            // showed collisions from earlier than 2**16 onwards, suggesting the
            // hash function is not optimal.
            // The counts up to n=12 are correct, so OK in practice.
        }
        return hash;
    }

    public void initialize() {
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
        for (int n_squares = 2; n_squares <= max_squares; n_squares++) {
            foreach (Shape small_shape in polyominoes_all[n_squares-1].Values) {
                foreach ((int x, int y) cell in small_shape) {
                    for (int i = 0; i < 4; i++) {
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

    List<int> dx = new List<int>{ 1, -1,  0,  0};
    List<int> dy = new List<int>{ 0,  0,  1, -1};

    public void query(Shape cells) {
        Shape visited = new Shape();
        SortedSet<int> component_sizes = new SortedSet<int>();
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

                for (int i = 0; i < 4; i++) {
                    neighbors.Add((current_cell.x + dx[i],
                                   current_cell.y + dy[i]));
                }
            }
            if (component.Count == 0) {
                continue;
            }
            component_sizes.Add(component.Count);
            query_connected(component);
        }
        // TODO add achievements for component_sizes.
    }

    void print_shape(Shape s) {
        foreach ((int x, int y) p in s) {
            Debug.Log($"({p.x}, {p.y})");
        }
    }

    public void query_connected(Shape cells) {
        Shape polyominoe = get_canonical(cells);
        long hash = ShapeHash(polyominoe);
        if (!polyominoes_found[polyominoe.Count].ContainsKey(hash)) {
            //achievement_manager TODO
            achievement_manager.AchieveNewShape(polyominoe);
            Debug.Log($"Found new polyominoe of size {polyominoe.Count}!");
            polyominoes_found[polyominoe.Count][hash] = polyominoe;
            if (polyominoes_found[polyominoe.Count].Count == 
                polyominoes_all[polyominoe.Count].Count) {
                Debug.Log($"Found all polyominoes of size {polyominoe.Count}!");
            }
            //achievement_manager TODO
        }
    }

}

