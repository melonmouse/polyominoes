using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

using Shape = System.Collections.Generic.SortedSet<(int x, int y)>;

// MYBRARY
public static class MyExtensions {
    public static T Sample<T>(this IList<T> l) {
        return l[(int)(l.Count * Random.value)];
    }

    public static T Sample<T, TKey>(this IDictionary<TKey, T> d) {
        List<T> values = Enumerable.ToList(d.Values);
        return values.Sample();
    }
}

namespace Tests
{
    public class PolyominoeDatabaseTests : PolyominoeDatabase
    {
        public Shape get_random_shape(int size) {
            return base.polyominoes_all[size].Sample();
        }

        [Test]
        public void TriangleCubeCoordsTest() {
            SetMode(NeighborhoodType.TriangleNeumann);
            for (int i = -100; i<100; i++)
            for (int j = -100; j<100; j++) {
                (int, int, int) cc = triangle_storage_to_cube_coords(i, j);
                (int, int) sc = triangle_cube_to_storage_coords(cc);
                Assert.True(sc == (i, j),
                            "Cube coord conversion is not invertable.");
            }
        }

        [Test]
        public void TriangleRotationTest() {
            SetMode(NeighborhoodType.TriangleNeumann);

            for (int test_i = 0; test_i < 100; test_i ++) {
                Shape s = get_random_shape(1 + (test_i%(base.max_cells-1)));

                List<Shape> rotations = new List<Shape>();

                for (int i = 0; i<=2*base.n_rotations; i++) {
                    rotations.Add(rigid_transform(s, i, false));
                }
                Assert.True(s.SetEquals(rotations[0]));
                Assert.True(s.SetEquals(rotations[base.n_rotations]));
                for (int i = 0; i < base.n_rotations; i++) {
                    Assert.True(rotations[i].SetEquals(rotations[i + base.n_rotations]));
                }
            }
        }

        [Test]
        public void TriangleMirrorTest() {
            SetMode(NeighborhoodType.TriangleNeumann);

            for (int test_i = 0; test_i < 100; test_i ++) {
                Shape s = get_random_shape(1 + (test_i%(base.max_cells-1)));
                Shape mirror_s = rigid_transform(s, 0, true);
                Shape mirror_mirror_s = rigid_transform(mirror_s, 0, true);
                Assert.True(s.SetEquals(mirror_mirror_s));
            }
        }

        [Test]
        public void TriangleGetCanonicalTinyTest() {
            SetMode(NeighborhoodType.TriangleNeumann);

            foreach (Shape s in base.polyominoes_all[4].Values) {
                Shape canonical_s = get_canonical(s);
                for (int rotation = 0; rotation < base.n_rotations; rotation++)
                for (int mirror = 0; mirror < 2; mirror++) {
                    Shape transform_s =
                            rigid_transform(s, rotation, mirror == 1);
                    Shape canonical_transform_s = get_canonical(transform_s);
                    if (!canonical_transform_s.SetEquals(canonical_s)) {
                        Debug.Log("Original:");
                        print_shape(s);
                        Debug.Log("Original canonical:");
                        print_shape(canonical_s);
                        Debug.Log($"Transform ({rotation},{mirror}):");
                        print_shape(transform_s);
                        Debug.Log($"Transform canonical ({rotation},{mirror}):");
                        print_shape(canonical_transform_s);
                        Debug.Log("!!!! CANONICAL OF TRANSFORM");
                        get_canonical(transform_s, true);
                        Debug.Log("!!!! CANONICAL OF ORIGINAL");
                        get_canonical(s, true);
                    }
                    Assert.True(canonical_transform_s.SetEquals(canonical_s));
                }
            }
        }

        [Test]
        public void TriangleGetCanonicalTest() {
            SetMode(NeighborhoodType.TriangleNeumann);

            for (int test_i = 0; test_i < 100; test_i ++) {
                Shape s = get_random_shape(1 + (test_i%(base.max_cells-1)));
                Shape canonical_s = get_canonical(s);
                for (int rotation = 0; rotation < base.n_rotations; rotation++)
                for (int mirror = 0; mirror < 2; mirror++) {
                    Shape transform_s =
                            rigid_transform(s, rotation, mirror == 1);
                    Shape canonical_transform_s = get_canonical(transform_s);
                    Assert.True(canonical_transform_s.SetEquals(canonical_s));
                }
            }
        }






        // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
        // `yield return null;` to skip a frame.
        [UnityTest]
        public IEnumerator PolyominoeDatabaseTestsWithEnumeratorPasses()
        {
            // Use the Assert class to test conditions.
            // Use yield to skip a frame.
            yield return null;
        }
    }
}
