﻿using VRC.SDK3.Data;

namespace Tetris.Blocks
{
    public static class ShapeTypeExtensions
    {
        public static DataToken GetToken(this ShapeType type)
        {
            return new DataToken((int)type);
        }
    }
}