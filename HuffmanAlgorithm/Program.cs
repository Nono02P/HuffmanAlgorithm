using System;
using System.IO;
using System.Text;

namespace HuffmanAlgorithm
{
    class Program
    {
        static void Main(string[] args)
        {
            #region Generate random string

            Random rnd = new Random();
            byte[] genBytes = new byte[ushort.MaxValue * 32];
            rnd.NextBytes(genBytes);

            string stringInput = Convert.ToBase64String(genBytes);
            genBytes = null;

            byte[] bytes = Encoding.UTF8.GetBytes(stringInput);
            stringInput = null;
            
            #endregion

            Console.WriteLine($"Original message size in bytes {bytes.Length}");

            #region Compression side

            HuffmanTree tree = new HuffmanTree();
            byte[] result = tree.Compress(bytes);
            Console.WriteLine($"Compressed message size in bytes {result.Length}");
            bytes = null;

            #endregion

            #region Decompression side

            #region return a byte[]

            HuffmanTree tree2 = new HuffmanTree();
            byte[] data = tree2.Decompress(result);
            Console.WriteLine($"Decompressed message size in bytes {data.Length}");

            #endregion

            #region write directly in stream

            HuffmanTree tree3 = new HuffmanTree();
            Stream streamData;
            tree3.Decompress(result, out streamData);
            Console.WriteLine($"Decompressed message size in bytes {streamData.Length}");
            
            #endregion

            #endregion

            Console.ReadKey();
        }
    }
}