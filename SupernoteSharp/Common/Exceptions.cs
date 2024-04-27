using System;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("SupernoteSharpUnitTests")]
namespace SupernoteSharp.Common
{
    internal class ConverterException : Exception
    {
        protected internal ConverterException() { }
        protected internal ConverterException(string message) : base(message) { }
    }

    internal class UnsupportedFileFormatException : Exception
    {
        protected internal UnsupportedFileFormatException() { }
        protected internal UnsupportedFileFormatException(string message) : base(message) { }
    }

    internal class DecoderException : Exception
    {
        protected internal DecoderException() { }
        protected internal DecoderException(string message) : base(message) { }
    }

    internal class UnknownDecodeProtocolException : Exception
    {
        protected internal UnknownDecodeProtocolException() { }
        protected internal UnknownDecodeProtocolException(string message) : base(message) { }
    }
}
