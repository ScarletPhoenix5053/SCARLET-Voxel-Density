using UnityEngine;
using System.Collections;

namespace SCARLET
{
    public static partial class ExtensionMethods
    {
        public static bool Contains(this byte a, byte b) => (a & b) == b;
        public static bool Contains(this short a, short b) => (a & b) == b;
    }
}
