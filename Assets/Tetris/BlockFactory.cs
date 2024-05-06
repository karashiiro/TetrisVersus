using UdonSharp;
using UnityEngine;
using VRC.Udon;

namespace Tetris
{
    public class BlockFactory : UdonSharpBehaviour
    {
        [field: SerializeField] public BlockGroup PrototypeBlockGroup { get; set; }

        [field: SerializeField] public Block PrototypeBlock { get; set; }

        [field: SerializeField] public Transform BlockGroupParent { get; set; }

        /// <summary>
        /// Creates a new block group by cloning the provided group prototype instance.
        /// </summary>
        /// <returns></returns>
        public BlockGroup CreateBlockGroup()
        {
            var group = Instantiate(PrototypeBlockGroup.gameObject, BlockGroupParent, true);
            group.name = "BlockGroup";

            return (BlockGroup)group.GetComponent(typeof(UdonBehaviour));
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
            var blockObject = Instantiate(PrototypeBlock.gameObject, group.transform, true);
            blockObject.name = "Block";

            var block = (Block)blockObject.GetComponent(typeof(UdonBehaviour));
            block.Group = group;

            group[localX, localY] = block;

            return block;
        }

        /// <summary>
        /// Creates a controlled square tetra as a block group.
        /// </summary>
        /// <returns></returns>
        public BlockGroup CreateControlledSquare()
        {
            var group = CreateBlockGroup();
            CreateControlledBlock(group, 0, 0);
            CreateControlledBlock(group, 0, 1);
            CreateControlledBlock(group, 1, 0);
            CreateControlledBlock(group, 1, 1);
            return group;
        }

        private void CreateControlledBlock(BlockGroup group, int localX, int localY)
        {
            var block = CreateBlock(group, localX, localY);
            block.State = BlockState.Controlled;
        }
    }
}