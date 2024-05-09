using JetBrains.Annotations;
using UnityEngine;

namespace Tetris
{
    public static class RandomGenerator
    {
        public const int SequenceLength = (int)ShapeType.MaxValue - (int)ShapeType.MinValue + 1;

        /// <summary>
        /// Generates a new sequence of shapes. The result should be processed using <see cref="GetNextShape"/>.
        /// </summary>
        /// <returns></returns>
        public static ShapeType[] NewSequence(out int startIndex)
        {
            var shapes = new ShapeType[SequenceLength];
            startIndex = SequenceLength - 1;
            UpdateSequence(shapes);
            return shapes;
        }

        public static ShapeType GetNextShape([CanBeNull] ShapeType[] shapes, ref int currentIndex)
        {
            if (shapes == null)
            {
                Debug.LogError("GetNextShape: Shape buffer is null.");
                return ShapeType.Square;
            }

            if (currentIndex == -1)
            {
                UpdateSequence(shapes);
                currentIndex = SequenceLength - 1;
            }

            return shapes[currentIndex--];
        }

        /// <summary>
        /// Updates a sequence created with <see cref="NewSequence"/>. This generates a random permutation
        /// containing one of each block type.
        /// </summary>
        /// <param name="shapes">An array of shape types created with <see cref="NewSequence"/>.</param>
        private static void UpdateSequence(ShapeType[] shapes)
        {
            // First pass: Initialize the array to unique shapes in order
            for (var i = (int)ShapeType.MinValue; i <= (int)ShapeType.MaxValue; i++)
            {
                shapes[i] = (ShapeType)i;
            }

            // Second pass: Do a Fisher-Yates shuffle to create a random permutation of elements in-place
            for (var i = (int)ShapeType.MinValue; i <= (int)ShapeType.MaxValue - 2; i++)
            {
                var pivot = Random.Range(i, (int)ShapeType.MaxValue - i + 1);
                Swap(shapes, i, pivot);
            }
        }

        private static void Swap(ShapeType[] shapes, int i, int j)
        {
            var temp = shapes[i];
            shapes[i] = shapes[j];
            shapes[j] = temp;
        }
    }
}