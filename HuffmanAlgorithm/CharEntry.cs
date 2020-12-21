namespace HuffmanAlgorithm
{
    public partial class HuffmanTree
    {
        private struct CharEntry
        {
            public byte Code;
            public int NbBits;
            public int Frequency;

            public CharEntry(byte code, int nbBits, int frequency)
            {
                Code = code;
                NbBits = nbBits;
                Frequency = frequency;
            }

            public override string ToString()
            {
                return $"Code = {Code}, NbBits = {NbBits}, Frequency = {Frequency}";
            }
        }
    }
}