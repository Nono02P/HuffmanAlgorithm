using System.Collections.Generic;

namespace HuffmanAlgorithm
{
    public partial class HuffmanTree
    {
        private class HuffmanNode
        {
            public readonly HuffmanNode LeftNode;
            public readonly HuffmanNode RightNode;
            public readonly byte Sequence;
            public readonly int Frequency;

            public bool IsLeaf => LeftNode == null && RightNode == null;

            public HuffmanNode(KeyValuePair<byte, int> keyValue) : this(keyValue.Key, keyValue.Value) { }
            public HuffmanNode(byte sequence, int frequency)
            {
                Sequence = sequence;
                Frequency = frequency;
            }

            public HuffmanNode(HuffmanNode node1, HuffmanNode node2)
            {
                if (node1.Frequency <= node2.Frequency)
                {
                    LeftNode = node1;
                    RightNode = node2;
                }
                else
                {
                    LeftNode = node2;
                    RightNode = node1;
                }
                Frequency = LeftNode.Frequency + RightNode.Frequency;
            }
        }
    }
}