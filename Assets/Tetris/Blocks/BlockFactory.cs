﻿using JetBrains.Annotations;
using Tetris.VRCExtensions;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;

namespace Tetris.Blocks
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class BlockFactory : UdonSharpBehaviour
    {
        private readonly DataList blockPool = new DataList();

        [field: SerializeField] public BlockGroup PrototypeBlockGroup { get; set; }
        [field: SerializeField] public Block PrototypeBlock { get; set; }
        [field: SerializeField] public Transform BlockGroupParent { get; set; }

        public int PoolSize => blockPool.Count;

        /// <summary>
        /// Creates a new block group by cloning the provided group prototype instance.
        /// </summary>
        /// <returns></returns>
        public BlockGroup CreateBlockGroup()
        {
            var group = Instantiate(PrototypeBlockGroup.gameObject, BlockGroupParent, true);
            group.name = "BlockGroup";

            // Put the cloned block group on top of its prototype, so we know we didn't spawn it somewhere
            // weird like a player spawn area. We'll move it somewhere meaningful later.
            group.transform.position = PrototypeBlockGroup.transform.position;

            var component = (UdonSharpBehaviour)group.GetComponent(typeof(UdonSharpBehaviour));
            return (BlockGroup)component;
        }

        /// <summary>
        /// Creates a new block by cloning the provided block prototype instance, and adds it
        /// to the provided block group.
        /// </summary>
        /// <param name="group">The group to add the block to.</param>
        /// <param name="localX">The block's x-position, local to the group.</param>
        /// <param name="localY">The block's y-position, local to the group.</param>
        /// <returns></returns>
        public Block CreateBlock(BlockGroup group, int localX, int localY)
        {
            if (!TryGetBlockFromPool(group, out var block))
            {
                block = CreateNewBlock(group);
            }

            // Put the cloned block on top of the group transform, then translate the block
            // where it needs to be relative to the group root.
            block.transform.SetLocalPositionAndRotation(new Vector3(localX, localY), Quaternion.identity);

            group[localX, localY] = block;

            return block;
        }

        private Block CreateNewBlock(BlockGroup group)
        {
            var blockObject = Instantiate(PrototypeBlock.gameObject, group.transform, true);
            blockObject.name = "Block";

            var component = (UdonSharpBehaviour)blockObject.GetComponent(typeof(UdonSharpBehaviour));
            return (Block)component;
        }

        private bool TryGetBlockFromPool(BlockGroup group, out Block block)
        {
            block = null;
            if (blockPool.TryGetValue(0, TokenType.Reference, out var blockToken))
            {
                blockPool.RemoveAt(0);
                block = blockToken.As<Block>();
                block.transform.SetParent(group.transform, true);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Creates an I tetronimo as a block group.
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public BlockGroup CreateI(Color color)
        {
            var group = CreateBlockGroup();

            // Create the blocks within the group
            CreateBlock(group, -1, 0);
            CreateBlock(group, 0, 0);
            CreateBlock(group, 1, 0);
            CreateBlock(group, 2, 0);

            group.SetColor(color);
            group.SetShapeType(ShapeType.I);

            return group;
        }

        /// <summary>
        /// Creates a T tetronimo as a block group.
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public BlockGroup CreateT(Color color)
        {
            var group = CreateBlockGroup();

            // Create the blocks within the group
            CreateBlock(group, -1, 0);
            CreateBlock(group, 0, 0);
            CreateBlock(group, 0, 1);
            CreateBlock(group, 1, 0);

            group.SetColor(color);
            group.SetShapeType(ShapeType.T);

            return group;
        }

        /// <summary>
        /// Creates an O tetronimo as a block group.
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public BlockGroup CreateO(Color color)
        {
            var group = CreateBlockGroup();

            // Create the blocks within the group
            CreateBlock(group, 0, 0);
            CreateBlock(group, 0, 1);
            CreateBlock(group, 1, 0);
            CreateBlock(group, 1, 1);

            group.SetColor(color);
            group.SetShapeType(ShapeType.O);

            return group;
        }

        /// <summary>
        /// Creates an S tetronimo as a block group.
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public BlockGroup CreateS(Color color)
        {
            var group = CreateBlockGroup();

            // Create the blocks within the group
            CreateBlock(group, -1, 0);
            CreateBlock(group, 0, 0);
            CreateBlock(group, 0, 1);
            CreateBlock(group, 1, 1);

            group.SetColor(color);
            group.SetShapeType(ShapeType.S);

            return group;
        }

        /// <summary>
        /// Creates a Z tetronimo as a block group.
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public BlockGroup CreateZ(Color color)
        {
            var group = CreateBlockGroup();

            // Create the blocks within the group
            CreateBlock(group, -1, 1);
            CreateBlock(group, 0, 1);
            CreateBlock(group, 0, 0);
            CreateBlock(group, 1, 0);

            group.SetColor(color);
            group.SetShapeType(ShapeType.Z);

            return group;
        }

        /// <summary>
        /// Creates an L tetronimo as a block group.
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public BlockGroup CreateL(Color color)
        {
            var group = CreateBlockGroup();

            // Create the blocks within the group
            CreateBlock(group, -1, 0);
            CreateBlock(group, 0, 0);
            CreateBlock(group, 1, 0);
            CreateBlock(group, 1, 1);

            group.SetColor(color);
            group.SetShapeType(ShapeType.L);

            return group;
        }

        /// <summary>
        /// Creates a J tetronimo as a block group.
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public BlockGroup CreateJ(Color color)
        {
            var group = CreateBlockGroup();

            // Create the blocks within the group
            CreateBlock(group, -1, 1);
            CreateBlock(group, -1, 0);
            CreateBlock(group, 0, 0);
            CreateBlock(group, 1, 0);

            group.SetColor(color);
            group.SetShapeType(ShapeType.J);

            return group;
        }

        public BlockGroup CreateShape(ShapeType type, Color color)
        {
            switch (type)
            {
                case ShapeType.O:
                    return CreateO(color);
                case ShapeType.I:
                    return CreateI(color);
                case ShapeType.S:
                    return CreateS(color);
                case ShapeType.Z:
                    return CreateZ(color);
                case ShapeType.T:
                    return CreateT(color);
                case ShapeType.L:
                    return CreateL(color);
                case ShapeType.J:
                    return CreateJ(color);
                default:
                    Debug.LogError($"CreateShape: Unrecognized shape type provided: {type}");
                    return CreateBlockGroup();
            }
        }

        public void ReturnBlock([CanBeNull] Block block)
        {
            if (block == null) return;
            block.DisableGhostMode();
            ReturnBlockCore(block);
        }

        /// <summary>
        /// Optimized form of <see cref="ReturnBlock"/>, used when a block is known not to be
        /// ghosted and not null.
        /// </summary>
        /// <param name="block">The block.</param>
        private void ReturnBlockCore([NotNull] Block block)
        {
            block.transform.SetParent(transform);
            block.transform.position = transform.position;
            blockPool.Add(block.Token);
        }

        public void ReturnBlockGroup([CanBeNull] BlockGroup group)
        {
            if (group == null) return;

            group.DisableGhostMode();
            foreach (var block in group.GetBlocks())
            {
                ReturnBlockCore(block);
            }

            ObjectHelpers.Destroy(group.gameObject);
        }
    }
}