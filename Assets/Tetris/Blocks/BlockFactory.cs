using UdonSharp;
using UnityEngine;
using VRC.Udon;

namespace Tetris.Blocks
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
        /// Creates a straight tetra as a block group.
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public BlockGroup CreateStraight(Color color)
        {
            var group = CreateBlockGroup();

            // Create the blocks within the group
            CreateBlock(group, -1, 0);
            CreateBlock(group, 0, 0);
            CreateBlock(group, 1, 0);
            CreateBlock(group, 2, 0);

            group.SetColor(color);

            return group;
        }

        /// <summary>
        /// Creates a T tetra as a block group.
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public BlockGroup CreateT(Color color)
        {
            var group = CreateBlockGroup();

            // Create the blocks within the group
            CreateBlock(group, -1, 0);
            CreateBlock(group, 0, 0);
            CreateBlock(group, 1, 0);
            CreateBlock(group, 0, -1);

            group.SetColor(color);

            return group;
        }

        /// <summary>
        /// Creates a square tetra as a block group.
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public BlockGroup CreateSquare(Color color)
        {
            var group = CreateBlockGroup();

            // Create the blocks within the group
            CreateBlock(group, 0, 0);
            CreateBlock(group, 0, 1);
            CreateBlock(group, 1, 0);
            CreateBlock(group, 1, 1);

            group.SetColor(color);

            return group;
        }

        /// <summary>
        /// Creates a left skew (S) tetra as a block group.
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public BlockGroup CreateLeftSkew(Color color)
        {
            var group = CreateBlockGroup();

            // Create the blocks within the group
            CreateBlock(group, -1, -1);
            CreateBlock(group, 0, -1);
            CreateBlock(group, 0, 0);
            CreateBlock(group, 1, 0);

            group.SetColor(color);

            return group;
        }

        /// <summary>
        /// Creates a right skew (Z) tetra as a block group.
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public BlockGroup CreateRightSkew(Color color)
        {
            var group = CreateBlockGroup();

            // Create the blocks within the group
            CreateBlock(group, -1, 0);
            CreateBlock(group, 0, 0);
            CreateBlock(group, 0, -1);
            CreateBlock(group, 1, -1);

            group.SetColor(color);

            return group;
        }

        /// <summary>
        /// Creates a left L (L) tetra as a block group.
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public BlockGroup CreateLeftL(Color color)
        {
            var group = CreateBlockGroup();

            // Create the blocks within the group
            CreateBlock(group, 0, 0);
            CreateBlock(group, 1, 0);
            CreateBlock(group, 0, 1);
            CreateBlock(group, 0, 2);

            group.SetColor(color);

            return group;
        }

        /// <summary>
        /// Creates a right L (J) tetra as a block group.
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public BlockGroup CreateRightL(Color color)
        {
            var group = CreateBlockGroup();

            // Create the blocks within the group
            CreateBlock(group, -1, 0);
            CreateBlock(group, 0, 0);
            CreateBlock(group, 0, 1);
            CreateBlock(group, 0, 2);

            group.SetColor(color);

            return group;
        }

        public BlockGroup CreateShape(ShapeType type, Color color)
        {
            switch (type)
            {
                case ShapeType.Square:
                    return CreateSquare(color);
                case ShapeType.Straight:
                    return CreateStraight(color);
                case ShapeType.LeftSkew:
                    return CreateLeftSkew(color);
                case ShapeType.RightSkew:
                    return CreateRightSkew(color);
                case ShapeType.T:
                    return CreateT(color);
                case ShapeType.LeftL:
                    return CreateLeftL(color);
                case ShapeType.RightL:
                    return CreateRightL(color);
                default:
                    Debug.LogError($"CreateShape: Unrecognized shape type provided: {type}");
                    return null;
            }
        }
    }
}