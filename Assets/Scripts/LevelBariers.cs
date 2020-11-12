using System.Collections;
using System.Collections.Generic;

static class LevelBariers {
    public static int get_total_score() {
        int total_score = 0;
        foreach (SaveLevel sl in CurrentSaveGame.save.save_levels.Values) {
            total_score += sl.max_cells;
        }
        return total_score;
    }

    public static int get_max_level() {
        return get_max_level(get_total_score());
    }

    public static int get_max_level(int total_score) {
        // unlock levels based on score
        if (total_score < 4) {
            return 0;
            // For TriangleNeumann - finish tutorial;
            // - SquareNeumann 4 (5 options)
        } else if (total_score < 10) {
            return 1;  // unlock TriangleNeumann
            // For Hexagon - finish 10=4+5(+1):
            // - SquareNeumann 4 (5 options)
            // - TriangleNeumann 5 (4 options)
            // and one of:
            // - SquareNeumann 5=+1 (12 options)
            // - TriangleNeumann 6=+1 (12 options)
        } else if (total_score < 16) {
            return 2;  // unlock Hexagon
            // For SquareMoore - finish 16=5+6+4(+1):
            // - SquareNeumann 5 (12 options)
            // - TriangleNeumann 6 (12 options)
            // - Hexagon 4 (7 options)
            // and one of:
            // - SquareNeumann 6=+1 (35 options)
            // - TriangleNeumann 7=+1 (24 options)
            // - Hexagon 5=+1 (22 options)
        } else if (total_score < 20) {  // 
            return 3;  // unlock SquareMoore
            // For TriangleMoore - finish 21=5+6+4+3(+3):
            // - SquareNeumann 5 (12 options)
            // - TriangleNeumann 6 (12 options)
            // - Hexagon 4 (7 options)
            // - SquareMoore 3 (5 options)
            // and two of (one extra compared to last):
            // - SquareNeumann 6=+1 (35 options)
            // - TriangleNeumann 7=+1 (24 options)
            // - Hexagon 5=+1 (22 options)
            // - SquareMoore 4=+1 (22 options)
        } else if (total_score < 25) {  // 
            return 4;  // unlock TriangleMoore
            // For TriangleMoore - finish 26=5+6+4+3+3(+5):
            // - SquareNeumann 5 (12 options)
            // - TriangleNeumann 6 (12 options)
            // - Hexagon 4 (7 options)
            // - SquareMoore 3 (5 options)
            // - TriangleMoore 3 (11 options)
            // and four out of (two extra compared to last):
            // - SquareNeumann 6=+1 (35 options)
            // - TriangleNeumann 7=+1 (24 options)
            // - Hexagon 5=+1 (22 options)
            // - SquareMoore 4=+1 (22 options)
            // - TriangleMoore 4=+1 (75 options)
        } else {
            return 5;  // unlock HexagonJump
        }
    }
}
