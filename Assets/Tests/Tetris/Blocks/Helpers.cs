using NUnit.Framework;
using Tetris.Blocks;
using UdonSharpEditor;
using UnityEngine;

namespace Tests.Tetris.Blocks
{
    public static class Helpers
    {
        public static void AssertEqual(Block block1, Block block2)
        {
            Assert.AreEqual(block1.State, block2.State);
            Assert.AreEqual(block1.ShapeType, block2.ShapeType);
        }
        
        public static Block CreateBlock()
        {
            var gameObject = UnityEditor.ObjectFactory.CreatePrimitive(PrimitiveType.Cube);
            return gameObject.AddUdonSharpComponent<Block>();
        }

        public static BlockGroup CreateBlockGroup()
        {
            var gameObject = UnityEditor.ObjectFactory.CreatePrimitive(PrimitiveType.Cube);
            return gameObject.AddUdonSharpComponent<BlockGroup>();
        }

        public static BlockFactory CreateBlockFactory()
        {
            var parentObject = UnityEditor.ObjectFactory.CreatePrimitive(PrimitiveType.Cube);
            var gameObject = UnityEditor.ObjectFactory.CreatePrimitive(PrimitiveType.Cube);
            var blockFactory = gameObject.AddUdonSharpComponent<BlockFactory>();
            blockFactory.PrototypeBlockGroup = CreateBlockGroup();
            blockFactory.PrototypeBlock = CreateBlock();
            blockFactory.BlockGroupParent = parentObject.transform;
            return blockFactory;
        }
    }
}