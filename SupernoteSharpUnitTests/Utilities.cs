using System;

namespace SupernoteSharpUnitTests
{
    internal static class Utilities
    {
        // Credit: https://stackoverflow.com/questions/43289/comparing-two-byte-arrays-in-net (Joe Amenta)
        internal static bool ByteArraysEqual(ReadOnlySpan<byte> array_1, ReadOnlySpan<byte> array_2)
        {
            return array_1.SequenceEqual(array_2);
        }
    }
}
