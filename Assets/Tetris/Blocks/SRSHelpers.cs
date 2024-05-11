using VRC.SDK3.Data;
using VRCExtensions;

namespace Tetris.Blocks
{
    public static class SRSHelpers
    {
        public static DataDictionary NewDataTable()
        {
            // https://tetris.wiki/Super_Rotation_System
            var offsets = new DataDictionary();
            var standardOffsets = NewStandardOffsets();
            offsets[ShapeType.S.GetToken()] = new DataToken(standardOffsets);
            offsets[ShapeType.Z.GetToken()] = new DataToken(standardOffsets);
            offsets[ShapeType.T.GetToken()] = new DataToken(standardOffsets);
            offsets[ShapeType.L.GetToken()] = new DataToken(standardOffsets);
            offsets[ShapeType.J.GetToken()] = new DataToken(standardOffsets);
            offsets[ShapeType.I.GetToken()] = new DataToken(NewIOffsets());
            offsets[ShapeType.O.GetToken()] = new DataToken(NewOOffsets());
            return offsets;
        }

        public static int[][] GetPossibleTranslations(DataDictionary dataTable, ShapeType shapeType,
            Orientation orientation, Rotation rotation)
        {
            var offsets = dataTable[shapeType.GetToken()].As<int[][][]>();
            var nextOrientation = orientation.Rotate(rotation);
            return Difference(offsets[(int)orientation], offsets[(int)nextOrientation]);
        }

        private static int[][] Difference(int[][] first, int[][] second)
        {
            var ret = new int[first.Length][];
            for (var i = 0; i < ret.Length; i++)
            {
                ret[i] = new[] { first[i][0] - second[i][0], first[i][1] - second[i][1] };
            }

            return ret;
        }

        private static int[][][] NewStandardOffsets()
        {
            return new[]
            {
                /* 0 */ new[] { new[] { 0, 0 }, new[] { 0, 0 }, new[] { 0, 0 }, new[] { 0, 0 }, new[] { 0, 0 } },
                /* R */ new[] { new[] { 0, 0 }, new[] { 1, 0 }, new[] { 1, -1 }, new[] { 0, 2 }, new[] { 1, 2 } },
                /* 2 */ new[] { new[] { 0, 0 }, new[] { 0, 0 }, new[] { 0, 0 }, new[] { 0, 0 }, new[] { 0, 0 } },
                /* L */ new[] { new[] { 0, 0 }, new[] { -1, 0 }, new[] { -1, -1 }, new[] { 0, 2 }, new[] { -1, 2 } },
            };
        }

        private static int[][][] NewIOffsets()
        {
            return new[]
            {
                /* 0 */ new[] { new[] { 0, 0 }, new[] { -1, 0 }, new[] { 2, 0 }, new[] { -1, 0 }, new[] { 2, 0 } },
                /* R */ new[] { new[] { -1, 0 }, new[] { 0, 0 }, new[] { 0, 0 }, new[] { 0, 1 }, new[] { 0, -2 } },
                /* 2 */ new[] { new[] { -1, 1 }, new[] { 1, 1 }, new[] { -2, 1 }, new[] { 1, 0 }, new[] { -2, 0 } },
                /* L */ new[] { new[] { 0, 1 }, new[] { 0, 1 }, new[] { 0, 1 }, new[] { 0, -1 }, new[] { 0, 2 } },
            };
        }

        private static int[][][] NewOOffsets()
        {
            return new[]
            {
                /* 0 */ new[] { new[] { 0, 0 } },
                /* R */ new[] { new[] { 0, -1 } },
                /* 2 */ new[] { new[] { -1, -1 } },
                /* L */ new[] { new[] { -1, 0 } },
            };
        }
    }
}