using Bitpacking;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HuffmanAlgorithm
{
    public partial class HuffmanTree
    {
        private HuffmanNode _rootNode;
        
        public HuffmanTree() { }

        #region Tree generation

        /// <summary>
        /// Generate a tree from a frequency occurence of bytes.
        /// </summary>
        /// <param name="bytes">The bytes sequences to generate the tree.</param>
        private void GenerateTree(IEnumerable<KeyValuePair<byte, int>> sortedFrequencies)
        {
            // Generate the leaf nodes from the occurences of each bytes
            List<HuffmanNode> nodes = new List<HuffmanNode>();
            foreach (KeyValuePair<byte, int> item in sortedFrequencies)
            {
                nodes.Add(new HuffmanNode(item));
            }

            // Build the tree from the leaf nodes
            while (nodes.Count > 1)
            {
                HuffmanNode node = nodes[0];
                HuffmanNode node2 = nodes[1];
                nodes.RemoveRange(0, 2);

                nodes.Add(new HuffmanNode(node, node2));
            }
            _rootNode = nodes[0];
        }

        /// <summary>
        /// Returns for each byte the number of occurence.
        /// </summary>
        /// <param name="bytes">the bytes data to check.</param>
        /// <returns>An ordered enumerable containing KeyValuePair with byte code and number of occurences.</returns>
        private IEnumerable<KeyValuePair<byte, int>> GetFrequencies(byte[] bytes)
        {
            Dictionary<byte, int> sequenceFrequencies = new Dictionary<byte, int>();
            for (int i = 0; i < bytes.Length; i++)
            {
                if (sequenceFrequencies.TryGetValue(bytes[i], out int frequency))
                    sequenceFrequencies[bytes[i]] = ++frequency;
                else
                    sequenceFrequencies.Add(bytes[i], 1);
            }
            return from entry in sequenceFrequencies
                   orderby entry.Value ascending
                   select entry;
        }

        /// <summary>
        /// A recursive function that add in a dictionary the data to write (code for the sequence + number of bits + frequency of this sequence) for each sequence.
        /// </summary>
        /// <param name="dictionary">The dictionary into add data.</param>
        /// <param name="currentNode">The current node called (start from the root node and go recursively deeper into the child nodes).</param>
        /// <param name="code">The binary code of the current node.</param>
        /// <param name="length">The depth of the tree (needed to calculate the binary code).</param>
        private void FillCharEntriesDictionary(Dictionary<byte, CharEntry> dictionary, HuffmanNode currentNode, byte code = 0, int length = 0)
        {
            if (currentNode.IsLeaf)
                dictionary.Add(currentNode.Sequence, new CharEntry(code, length, currentNode.Frequency));
            else
            {
                length++;
                FillCharEntriesDictionary(dictionary, currentNode.LeftNode, code, length);
                FillCharEntriesDictionary(dictionary, currentNode.RightNode, (byte)(code + Math.Pow(2, length - 1)), length);
            }
        }

        #endregion

        #region Compression/Decompression

        public byte[] Compress(byte[] bytes)
        {
            IEnumerable<KeyValuePair<byte, int>> frequencies = GetFrequencies(bytes);
            GenerateTree(frequencies);

            // Build a character encoding table (char, CharEntry<encoding value, nb of bits of the encoded value, frequency>)
            Dictionary<byte, CharEntry> charTableEncoding = new Dictionary<byte, CharEntry>();
            FillCharEntriesDictionary(charTableEncoding, _rootNode);

            long nbBits = 10 * charTableEncoding.Count + 1 + 32; // + 32 because we stored the number of input bytes (characters) just after serializing the tree.
            foreach (CharEntry charEntry in charTableEncoding.Values)
            {
                nbBits += charEntry.NbBits * charEntry.Frequency;
            }

            int nbOfBytes = (int)Math.Ceiling(nbBits / (decimal)8);
            BitPacker bitpacker = new BitPacker(nbOfBytes);
            SerializeTree(bitpacker, _rootNode);

            // Write in the bitpacker the total number of char after the tree.
            bitpacker.WriteValue((uint)_rootNode.Frequency, 32);

            // Write encoded data in the bitpacker.
            for (int i = 0; i < bytes.Length; i++)
            {
                CharEntry charEntry = charTableEncoding[bytes[i]];
                bitpacker.WriteValue(charEntry.Code, charEntry.NbBits);
            }
            bitpacker.PushTempInBuffer();
            return bitpacker.GetByteBuffer();
        }

        public byte[] Decompress(byte[] compressedData)
        {
            BitPacker bitpacker = BitPacker.FromArray(compressedData);
            _rootNode = DeserializeTree(bitpacker);

            // Read in the bitpacker the total number of char stored after the tree.
            int length = (int)bitpacker.ReadValue(32);

            byte[] result = new byte[length];
            for (int i = 0; i < length; i++)
            {
                HuffmanNode currentNode = _rootNode;
                do
                {
                    if (bitpacker.ReadValue(1) == 0)
                    {
                        currentNode = currentNode.LeftNode;
                    }
                    else
                    {
                        currentNode = currentNode.RightNode;
                    }
                } while (currentNode.LeftNode != null && currentNode.RightNode != null);
                result[i] = currentNode.Sequence;
            }
            return result;
        }

        public void Decompress(byte[] compressedData, out Stream stream)
        {
            BitPacker bitpacker = BitPacker.FromArray(compressedData);
            _rootNode = DeserializeTree(bitpacker);

            // Read in the bitpacker the total number of char stored after the tree.
            int length = (int)bitpacker.ReadValue(32);

            stream = new MemoryStream(length);
            for (int i = 0; i < length; i++)
            {
                HuffmanNode currentNode = _rootNode;
                do
                {
                    if (bitpacker.ReadValue(1) == 0)
                    {
                        currentNode = currentNode.LeftNode;
                    }
                    else
                    {
                        currentNode = currentNode.RightNode;
                    }
                } while (currentNode.LeftNode != null && currentNode.RightNode != null);
                stream.WriteByte(currentNode.Sequence);
            }
            stream.Position -= length;
        }

        #endregion

        #region Serialize/Deserialize

        /// <summary>
        /// A recursive function that serialize each node.
        /// The pattern is :
        /// - 1 then the sequence for leaf nodes.
        /// - 0 then the serialize the left node, then the right node.
        /// </summary>
        /// <param name="bitpacker">The bitpacker where serialize the tree.</param>
        /// <param name="currentNode">The current node to serialize (starts from the root node and recursively call the child nodes).</param>
        private void SerializeTree(BitPacker bitpacker, HuffmanNode currentNode)
        {
            // If the node is a leaf, write 1 then the Sequence.
            if (currentNode.IsLeaf)
            {
                bitpacker.WriteValue(1, 1);
                bitpacker.WriteValue(currentNode.Sequence, 8);
            }
            else
            {
                // Otherwise, just write 0 then serialize the left node, then right node. 
                bitpacker.WriteValue(0, 1);
                SerializeTree(bitpacker, currentNode.LeftNode);
                SerializeTree(bitpacker, currentNode.RightNode);
            }
        }

        /// <summary>
        /// A recursive function that deserialize each node contained in the bitpacker.
        /// </summary>
        /// <param name="bitPacker">The bitpacker that contains the tree</param>
        /// <returns>Return the root node of the complete tree.</returns>
        private HuffmanNode DeserializeTree(BitPacker bitPacker)
        {
            // If bit is 1, this is a leaf
            if (bitPacker.ReadValue(1) == 1)
            {
                byte sequence = (byte)bitPacker.ReadValue(8);
                return new HuffmanNode(sequence, 0);
            }
            else
            {
                HuffmanNode leftNode = DeserializeTree(bitPacker);
                HuffmanNode rightNode = DeserializeTree(bitPacker);
                return new HuffmanNode(leftNode, rightNode);
            }
        } 

        #endregion
    }
}