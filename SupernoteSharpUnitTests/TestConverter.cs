using Codeuctivity.ImageSharpCompare;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SupernoteSharp.Business;
using SupernoteSharp.Common;
using SupernoteSharp.Entities;
using System;
using System.IO;
using static SupernoteSharp.Business.Converter;

namespace SupernoteSharpUnitTests
{
    [TestClass]
    public class TestConverter
    {
        private FileStream _fileStream;

        [TestInitialize]
        public void Setup()
        {
            string testNotePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"TestData\A5X_TestNote.note");
            _fileStream = new FileStream(testNotePath, FileMode.Open, FileAccess.Read);
        }

        [TestCleanup]
        public void TearDown()
        {
            if (_fileStream != null)
                _fileStream.Close();
        }

        [TestMethod]
        public void TestImageConverter()
        {
            Parser parser = new Parser();
            Notebook notebook = parser.LoadNotebook(_fileStream, Policy.Strict);

            ImageConverter converter = new Converter.ImageConverter(notebook, DefaultColorPalette.Grayscale);
            Image page_0 = converter.Convert(0, VisibilityOverlay.Default);
            Image page_1 = converter.Convert(1, VisibilityOverlay.Default);
            Image page_2 = converter.Convert(2, VisibilityOverlay.Default);
            Image page_3 = converter.Convert(3, VisibilityOverlay.Default);

            ImageSharpCompare.ImagesAreEqual(page_0.CloneAs<Rgba32>(),
                Image.Load<Rgba32>(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"TestData\A5X_TestNote_0.png"))).Should().BeTrue();
            ImageSharpCompare.ImagesAreEqual(page_1.CloneAs<Rgba32>(),
                Image.Load<Rgba32>(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"TestData\A5X_TestNote_1.png"))).Should().BeTrue();
            ImageSharpCompare.ImagesAreEqual(page_2.CloneAs<Rgba32>(),
                Image.Load<Rgba32>(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"TestData\A5X_TestNote_2.png"))).Should().BeTrue();
            ImageSharpCompare.ImagesAreEqual(page_3.CloneAs<Rgba32>(),
                Image.Load<Rgba32>(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"TestData\A5X_TestNote_3.png"))).Should().BeTrue();
        }
    }
}