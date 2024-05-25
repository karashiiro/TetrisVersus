﻿using VRC.SDK3.Data;

namespace Tetris.VRCExtensions
{
    public static class DataTokenExtensions
    {
        public static T As<T>(this DataToken token)
        {
            return (T)token.Reference;
        }
    }
}