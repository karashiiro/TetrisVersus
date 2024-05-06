using UdonSharp;
using UnityEngine;
using VRC.Udon;

namespace Tetris
{
    public class BlockFactory : UdonSharpBehaviour
    {
        private readonly int ShaderColorKey = Shader.PropertyToID("_Color");

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

            // Put the cloned block group on top of its prototype, so we know we didn't spawn it somewhere
            // weird like a player spawn area. We'll move it somewhere meaningful later.
            group.transform.position = PrototypeBlockGroup.transform.position;

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

            // Put the cloned block on top of the group transform, then translate the block
            // where it needs to be relative to the group root.
            block.transform.SetLocalPositionAndRotation(new Vector3(localX, localY), Quaternion.identity);

            group[localX, localY] = block;

            return block;
        }

        /// <summary>
        /// Creates a square tetra as a block group.
        /// </summary>
        /// <returns></returns>
        public BlockGroup CreateSquare(Color color)
        {
            var group = CreateBlockGroup();

            // Create the blocks within the group
            CreateBlock(group, 0, 0);
            CreateBlock(group, 0, 1);
            CreateBlock(group, 1, 0);
            CreateBlock(group, 1, 1);

            foreach (var block in group.GetBlocks())
            {
                var blockRenderer = block.GetComponent<Renderer>();
                if (blockRenderer == null) continue;
                blockRenderer.material.SetColor(ShaderColorKey, color);
            }

            return group;
        }
    }
}