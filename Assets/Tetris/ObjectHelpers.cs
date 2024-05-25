using UnityEngine;

namespace Tetris
{
    public static class ObjectHelpers
    {
        public static void Destroy(Object obj)
        {
#if !COMPILER_UDONSHARP && UNITY_EDITOR
            Object.DestroyImmediate(obj);
#else
            Object.Destroy(obj);
#endif
        }
    }
}