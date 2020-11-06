using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class PolyominoeDatabaseTests
    {
        // A Test behaves as an ordinary method
        [Test]
        public void PolyominoeDatabaseTestsSimplePasses()
        {
            // Use the Assert class to test conditions
        }

        [Test]
        public void TriangleCubeCoordsTest() {
            PolyominoeDatabase x;
            //SetMode(NeighborhoodType.TriangleNeumann);
            //for (int i = -100; i<100; i++)
            //for (int j = -100; j<100; j++) {
            //    (int, int, int) cc = triangle_storage_to_cube_coords(i, j);
            //    (int, int) sc = triangle_cube_to_storage_coords(cc);
            //    Debug.AssertTrue(sc == (i, j),
            //                     "Cube coord conversion is not invertable.");
            //}
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
