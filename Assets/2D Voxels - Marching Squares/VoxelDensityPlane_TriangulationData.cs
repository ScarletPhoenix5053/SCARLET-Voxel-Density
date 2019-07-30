using UnityEngine;
using System.Collections;

public static class TriangulationData2D
{
    public static byte[] IntersectionPoints = new byte[16]
    {
         0b_0000,    // 0
         0b_0011,    // 1
         0b_0101,    // 2
         0b_0110,    // 3
         0b_1010,    // 4
         0b_1001,    // 5
         0b_1111,    // 6
         0b_1100,    // 7
         0b_1100,    // 8
         0b_1111,    // 9
         0b_1001,    // 10
         0b_1010,    // 11
         0b_0110,    // 12
         0b_0101,    // 13
         0b_0011,    // 14
         0b_0000     // 15
    };
    public static byte[][] TriangleFormations = new byte[16][]
    {
        new byte[] {},                      // 0
        new byte[] {0,2,1},                 // 1
        new byte[] {0,1,2},                 // 2
        new byte[] {0,2,1,1,2,3},           // 3 
        new byte[] {0,2,1},                 // 4
        new byte[] {0,1,2,1,3,2},           // 5
        new byte[] {0,2,4, 1,5,3},          // 6
        new byte[] {0,3,1,0,4,3,0,2,4},     // 7
        new byte[] {0,1,2},                 // 8
        new byte[] {0,3,2,1,4,5},           // 9
        new byte[] {0,2,1,1,2,3},           // 10
        new byte[] {1,0,3,1,3,4,1,4,2},     // 11
        new byte[] {0,3,2,1,3,0},           // 12
        new byte[] {1,3,0,1,4,3,1,2,4},     // 13
        new byte[] {2,0,3,2,3,4,2,4,1},     // 14
        new byte[] {0,2,1,3,1,2}            // 15
    };
}