using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;

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

public enum GridType {
    Square,
    Hexagon,
    Triangle,
}

public enum NeighborhoodType {
    SquareNeumann,
    SquareMoore,
    Hexagon,
    HexagonJump,
    TriangleNeumann,
    TriangleMoore,
}

public class PolyominoeDatabase {
    protected int max_cells;
    protected int n_rotations;
    protected Dictionary<ulong, Shape>[] polyominoes_found;
    protected Dictionary<ulong, Shape>[] polyominoes_all;

    public static Dictionary<NeighborhoodType, GridType> neighborhood_to_grid =
            new Dictionary<NeighborhoodType, GridType>{
                {NeighborhoodType.SquareNeumann, GridType.Square},
                {NeighborhoodType.SquareMoore, GridType.Square},
                {NeighborhoodType.Hexagon, GridType.Hexagon},
                {NeighborhoodType.HexagonJump, GridType.Hexagon},
                {NeighborhoodType.TriangleNeumann, GridType.Triangle},
                {NeighborhoodType.TriangleMoore, GridType.Triangle},
            };

    public ulong ShapeHash(Shape s) {

        unchecked {
            ulong hash = 1UL;
            //Debug.Assert(max_cells < 11, "Need to choose higher primes!");
            foreach ((int x, int y) p in s) {
                //hash += (ulong)(p.x + 17*p.y);
                //hash *= 23UL;  // some number higher than max_cells, coprime
                Debug.Assert(p.x > 0);
                Debug.Assert(p.y > 0);
                hash = 6364136223846793005UL * (hash + (ulong)p.x) + 1442695040888963407UL;
                hash = 6364136223846793005UL * (hash + (ulong)p.y) + 1442695040888963407UL;
                // with 2^63-1 (with factors 7, 73, 127, 337, and two big ones).
                // If this is a good hash function int64, collisions would appear
                // only after generating ~2**32 polyominoes. Tests with 32-bit hash
                // showed collisions from earlier than 2**16 onwards, suggesting the
                // hash function is not optimal.
                // The counts up to n=12 are correct, so OK in practice.
            }
            return hash;
        }
    }

    public void generate_polyominoes() {
        for (int n_squares = 2; n_squares <= max_cells; n_squares++) {
            foreach (Shape small_shape in polyominoes_all[n_squares-1].Values) {
                foreach ((int x, int y) cell in small_shape) {
                    foreach ((int x, int y) new_cell in GetNeighbors(cell)) {
                        if (small_shape.Contains(new_cell)) {
                            continue;
                        }
                        Shape inc_shape = new Shape(small_shape);
                        inc_shape.Add(new_cell);
                        inc_shape = get_canonical(inc_shape);
                        ulong hash = ShapeHash(inc_shape);
                        polyominoes_all[n_squares][hash] = inc_shape; 
                                // no-op if duplicate
                    }
                }
            }
        }
    }

    public void initialize_polyominoes() {  // !!!!
        polyominoes_found = new Dictionary<ulong, Shape>[max_cells+1];
        for (int n_squares = 1; n_squares <= max_cells; n_squares++) {
            polyominoes_found[n_squares] = new Dictionary<ulong, Shape>();
        }

        polyominoes_all = new Dictionary<ulong, Shape>[max_cells+1];
        for (int n_squares = 1; n_squares <= max_cells; n_squares++) {
            polyominoes_all[n_squares] = new Dictionary<ulong, Shape>();
        }

        Shape trivial = new Shape();
        trivial.Add((0, 0));
        trivial = get_canonical(trivial);
        // always skip the trivial badge
        polyominoes_all[1][ShapeHash(trivial)] = trivial;
        polyominoes_found[1][ShapeHash(trivial)] = trivial;

        if (neighborhood_type == NeighborhoodType.SquareNeumann) {
            // first level - skip size 2 badge
            Shape easy = new Shape();
            easy.Add((0, 0));
            easy.Add((1, 0));
            easy = get_canonical(easy);
            polyominoes_all[2][ShapeHash(easy)] = easy;
            polyominoes_found[2][ShapeHash(easy)] = easy;
        }
        
        generate_polyominoes();

        for (int n_squares = 2; n_squares <= max_cells; n_squares++) {
            Console.WriteLine($"{n_squares} = {polyominoes_all[n_squares].Count}");
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

    public Shape get_canonical(Shape cells, bool debug=false) {
        Shape canonical_shape = null;
        ulong best_hash = System.UInt64.MaxValue;
        for (int rotation = 0; rotation < n_rotations; rotation++) {
            for (int mirror = 0; mirror < 2; mirror++) {
                Shape alt_cells =
                        normalize(rigid_transform(cells, rotation, mirror == 1));
                ulong hash = ShapeHash(alt_cells);
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
            dx = new List<int>{ 2, 1, 0,-1,-2,-2,-2,-1, 0, 1, 2, 2};
            dy = new List<int>{ 0, 1, 2, 2, 2, 1, 0,-1,-2,-2,-2,-1};
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
            // ACCORDING TO ABE:
            // 1, 1, 2, 5, 12, 35, 108, 369, 1285, 4655
          break;
          case NeighborhoodType.SquareMoore:
            max_cells = 6; // the level finishes after this (524)
            n_rotations = 4;
            grid_type = GridType.Square;
            // 1, 2, 5, 22, 94, 524, 3031, 18770, 118133, 758381, 4915652
            //     brz,slv,gold,plat
            // ACCORDING TO ME:
            // 1, 2, 5, 22, 94, 524, 3031, 
          break;
          case NeighborhoodType.Hexagon:
            max_cells = 7; // the level finishes after this (333)
            n_rotations = 6;
            grid_type = GridType.Hexagon;
            // 1, 1, 3, 7, 22, 82, 333, 1448, 6572, 30490, 143552, 683101
            //        brz,slv,gld,plat
            // ACCORDING TO ME:
            // 1, 1, 3, 7, 22, 82, 333, 1448, 6572
            // ACCORDING TO RAGNAR:
            // 1, 1, 3, 7, 22, 82, 333, 1448, 6572, 30490, 143552, 683101
          break;
          case NeighborhoodType.HexagonJump:
            max_cells = 8; // the level finishes after this (675)
            n_rotations = 6;
            grid_type = GridType.Hexagon;
            // ACCORDING TO ME:
            // 1, 2, 9, 70, 675, 7869, 94911, 1181821  (NOT ON OEIS)
            //     brz,slv,gld,plat
            // ACCORDING TO RAGNAR:                  
            // 1, 2, 9, 70, 675, 7869, 94911, 1181821
          break;
          case NeighborhoodType.TriangleNeumann:
            max_cells = 14; // the level finishes after this (448)
            n_rotations = 6;
            grid_type = GridType.Triangle;
            // 1, 1, 1, 3, 4, 12, 24, 66, 160, 448, 1186, 3334, 9235, 26166, 73983
            //               brz,slv, gld,    plat
            // ACCORDING TO ME
            // 1, 1, 1, 3, 4, 12, 24, 66, 160, 448, 1186, 3334, 9235, 26166
          break;
          case NeighborhoodType.TriangleMoore:
            max_cells = 9; // the level finishes after this (528)
            n_rotations = 6;
            grid_type = GridType.Triangle;
            // ACCORDING TO ME
            // 1, 3, 11, 75, 528, 4584, 40609, 373981, 3493723 (NOT ON OEIS)
            //      slv,gld,plat
            // ACCORDING TO RAGNAR:                  
            // 1, 3, 11, 75, 528, 4584, 40609, 373981, 3493723
          break;
        }
        // initialize right after
        initialize_polyominoes();
    }

    static public void print_shape(Shape s) {
        foreach ((int x, int y) p in s) {
            Console.WriteLine($"({p.x}, {p.y})");
        }
    }

    public void AddFoundHashes(List<List<ulong> > hashes_of_shapes_found) {
        for (int size = 1; size < polyominoes_found.Length; size++) {
            foreach (ulong hash in hashes_of_shapes_found[size]) {
                Shape s = polyominoes_all[size][hash];
                polyominoes_found[size][hash] = s;
            }
        }
    }

    public List<List<ulong> > GetFoundHashes() {
        List<List<ulong> > hashes_of_shapes_found = new List<List<ulong> >();
        for (int size = 0; size < polyominoes_found.Length; size++) {
            hashes_of_shapes_found.Add(new List<ulong>());
        }
        for (int size = 1; size < polyominoes_found.Length; size++) {
            foreach (ulong hash in polyominoes_found[size].Keys) {
                hashes_of_shapes_found[size].Add(hash);
            }
        }

        return hashes_of_shapes_found;
    }

    static void Main() {
        PolyominoeDatabase pd = new PolyominoeDatabase();
        //pd.SetMode(NeighborhoodType.TriangleNeumann);
        pd.SetMode(NeighborhoodType.TriangleMoore);
        //pd.SetMode(NeighborhoodType.HexagonJump);
    }

//public enum NeighborhoodType {
//    SquareNeumann,
//    SquareMoore,
//    Hexagon,
//    HexagonJump,
//    TriangleNeumann,
//    TriangleMoore,
//}

}
