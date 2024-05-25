using NUnit.Framework;
using Tetris.Blocks;
using Tetris.PlayArea;
using UdonSharpEditor;
using UnityEngine;

namespace Tests.Tetris
{
    public static class Helpers
    {
        public static void AssertEqual(Block block1, Block block2)
        {
            Assert.AreEqual(block1.State, block2.State);
            Assert.AreEqual(block1.ShapeType, block2.ShapeType);
        }

        public static GameObject CreateObject()
        {
            return UnityEditor.ObjectFactory.CreatePrimitive(PrimitiveType.Cube);
        }
        
        public static Block CreateBlock()
        {
            var gameObject = CreateObject();
            return gameObject.AddUdonSharpComponent<Block>();
        }

        public static BlockGroup CreateBlockGroup()
        {
            var gameObject = CreateObject();
            return gameObject.AddUdonSharpComponent<BlockGroup>();
        }

        public static Hold CreateHold()
        {
            var gameObject = CreateObject();
            return gameObject.AddUdonSharpComponent<Hold>();
        }

        public static BlockFactory CreateBlockFactory()
        {
            var parentObject = CreateObject();
            var gameObject = CreateObject();
            var blockFactory = gameObject.AddUdonSharpComponent<BlockFactory>();
            blockFactory.PrototypeBlockGroup = CreateBlockGroup();
            blockFactory.PrototypeBlock = CreateBlock();
            blockFactory.BlockGroupParent = parentObject.transform;
            return blockFactory;
        }
    }
}