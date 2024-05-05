using UdonSharp;
using UnityEngine;
using VRC.Udon;

namespace Tetris
{
    public class BlockFactory : UdonSharpBehaviour
    {
        [field: SerializeField]
        public BlockGroup PrototypeBlockGroup { get; set; }

        [field: SerializeField]
        public Block PrototypeBlock { get; set; }

        [field: SerializeField]
        public Transform BlockGroupParent { get; set; }

        public BlockGroup CreateBlockGroup()
        {
            var group = Instantiate(PrototypeBlockGroup.gameObject, BlockGroupParent, true);
            group.name = "BlockGroup";

            return (BlockGroup)group.GetComponent(typeof(UdonBehaviour));
        }

        public Block CreateBlock(BlockGroup group, int groupX, int groupY)
        {
            var blockObject = Instantiate(PrototypeBlock.gameObject, group.transform, true);
            blockObject.name = "Block";

            var block = (Block)blockObject.GetComponent(typeof(UdonBehaviour));
            block.Group = group;

            group[groupX, groupY] = block;

            return block;
        }

        private void CreateControlledBlock(BlockGroup group, int groupX, int groupY)
        {
            var block = CreateBlock(group, groupX, groupY);
            block.State = BlockState.Controlled;
        }

        public BlockGroup CreateControlledSquare()
        {
            var group = CreateBlockGroup();
            CreateControlledBlock(group, 0, 0);
            CreateControlledBlock(group, 0, 1);
            CreateControlledBlock(group, 1, 0);
            CreateControlledBlock(group, 1, 1);
            return group;
        }
    }
}