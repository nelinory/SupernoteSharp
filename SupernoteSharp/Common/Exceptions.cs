using System;

namespace SupernoteSharp.Common
{
    internal class ParserException : Exception
    {
        protected internal ParserException() { }
        protected internal ParserException(string message) : base(message) { }
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

    //internal class ManipulatorException : Exception
    //{
    //    protected internal ManipulatorException() { }
    //    protected internal ManipulatorException(string message) : base(message) { }
    //}

    //internal class GeneratedFileValidationException : Exception
    //{
    //    protected internal GeneratedFileValidationException() { }
    //    protected internal GeneratedFileValidationException(string message) : base(message) { }
    //}
}
