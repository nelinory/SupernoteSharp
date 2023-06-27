using Codeuctivity.ImageSharpCompare;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SupernoteSharp.Business;
using SupernoteSharp.Common;
using SupernoteSharp.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using static SupernoteSharp.Business.Converter;

namespace SupernoteSharpUnitTests
{
    [TestClass]
    public class TestConverter
    {
        private static FileStream _A5X_TestNote;
        private static FileStream _A5X_TestNote_Links;
        private static FileStream _A5X_TestNote_Realtime;
        private static FileStream _A5X_TestNote_Vectorization;
        private static string _testDataLocation = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData");

        [TestInitialize]
        public void Setup()
        {
            _A5X_TestNote = new FileStream(Path.Combine(_testDataLocation, "A5X_TestNote.note"), FileMode.Open, FileAccess.Read);
            _A5X_TestNote_Links = new FileStream(Path.Combine(_testDataLocation, "A5X_TestNote_Links.note"), FileMode.Open, FileAccess.Read);
            _A5X_TestNote_Realtime = new FileStream(Path.Combine(_testDataLocation, "A5X_TestNote_Realtime.note"), FileMode.Open, FileAccess.Read);
            _A5X_TestNote_Vectorization = new FileStream(Path.Combine(_testDataLocation, "A5X_TestNote_Vectorization.note"), FileMode.Open, FileAccess.Read);
        }

        [TestCleanup]
        public void TearDown()
        {
            if (_A5X_TestNote != null)
                _A5X_TestNote.Close();

            if (_A5X_TestNote_Links != null)
                _A5X_TestNote_Links.Close();

            if (_A5X_TestNote_Realtime != null)
                _A5X_TestNote_Realtime.Close();

            if (_A5X_TestNote_Vectorization != null)
                _A5X_TestNote_Vectorization.Close();
        }

        [TestMethod]
        public void TestImageConvert()
        {
            Parser parser = new Parser();
            Notebook notebook = parser.LoadNotebook(_A5X_TestNote, Policy.Strict);

            ImageConverter converter = new Converter.ImageConverter(notebook, DefaultColorPalette.Grayscale);
            Image page_0 = converter.Convert(0, ImageConverter.BuildVisibilityOverlay());
            Image page_1 = converter.Convert(1, ImageConverter.BuildVisibilityOverlay());
            Image page_2 = converter.Convert(2, ImageConverter.BuildVisibilityOverlay());
            Image page_3 = converter.Convert(3, ImageConverter.BuildVisibilityOverlay());

            ImageSharpCompare.ImagesAreEqual(page_0.CloneAs<Rgba32>(),
                Image.Load<Rgba32>(Path.Combine(Path.Combine(_testDataLocation, "A5X_TestNote_0.png")))).Should().BeTrue();
            ImageSharpCompare.ImagesAreEqual(page_1.CloneAs<Rgba32>(),
                Image.Load<Rgba32>(Path.Combine(Path.Combine(_testDataLocation, "A5X_TestNote_1.png")))).Should().BeTrue();
            ImageSharpCompare.ImagesAreEqual(page_2.CloneAs<Rgba32>(),
                Image.Load<Rgba32>(Path.Combine(Path.Combine(_testDataLocation, "A5X_TestNote_2.png")))).Should().BeTrue();
            ImageSharpCompare.ImagesAreEqual(page_3.CloneAs<Rgba32>(),
                Image.Load<Rgba32>(Path.Combine(Path.Combine(_testDataLocation, "A5X_TestNote_3.png")))).Should().BeTrue();
        }

        [TestMethod]
        public void TestImageConvertAll()
        {
            Parser parser = new Parser();
            Notebook notebook = parser.LoadNotebook(_A5X_TestNote, Policy.Strict);

            ImageConverter converter = new Converter.ImageConverter(notebook, DefaultColorPalette.Grayscale);

            List<Image> allPages = converter.ConvertAll(ImageConverter.BuildVisibilityOverlay());

            ImageSharpCompare.ImagesAreEqual(allPages[0].CloneAs<Rgba32>(),
                Image.Load<Rgba32>(Path.Combine(Path.Combine(_testDataLocation, "A5X_TestNote_0.png")))).Should().BeTrue();
            ImageSharpCompare.ImagesAreEqual(allPages[1].CloneAs<Rgba32>(),
                Image.Load<Rgba32>(Path.Combine(Path.Combine(_testDataLocation, "A5X_TestNote_1.png")))).Should().BeTrue();
            ImageSharpCompare.ImagesAreEqual(allPages[2].CloneAs<Rgba32>(),
                Image.Load<Rgba32>(Path.Combine(Path.Combine(_testDataLocation, "A5X_TestNote_2.png")))).Should().BeTrue();
            ImageSharpCompare.ImagesAreEqual(allPages[3].CloneAs<Rgba32>(),
                Image.Load<Rgba32>(Path.Combine(Path.Combine(_testDataLocation, "A5X_TestNote_3.png")))).Should().BeTrue();
        }

        [TestMethod]
        public void TestPdfConvert()
        {
            Parser parser = new Parser();
            Notebook notebook = parser.LoadNotebook(_A5X_TestNote, Policy.Strict);

            PdfConverter converter = new PdfConverter(notebook, DefaultColorPalette.Grayscale);
            byte[] page_0 = converter.Convert(0);
            byte[] page_1 = converter.Convert(1);
            byte[] page_2 = converter.Convert(2);
            byte[] page_3 = converter.Convert(3);

            Utilities.ByteArraysEqual(File.ReadAllBytes(Path.Combine(_testDataLocation, "A5X_TestNote_0.pdf")), page_0).Should().BeTrue();
            Utilities.ByteArraysEqual(File.ReadAllBytes(Path.Combine(_testDataLocation, "A5X_TestNote_1.pdf")), page_1).Should().BeTrue();
            Utilities.ByteArraysEqual(File.ReadAllBytes(Path.Combine(_testDataLocation, "A5X_TestNote_2.pdf")), page_2).Should().BeTrue();
            Utilities.ByteArraysEqual(File.ReadAllBytes(Path.Combine(_testDataLocation, "A5X_TestNote_3.pdf")), page_3).Should().BeTrue();
        }

        [TestMethod]
        public void TestPdfConvertAll()
        {
            Parser parser = new Parser();
            Notebook notebook = parser.LoadNotebook(_A5X_TestNote, Policy.Strict);

            PdfConverter converter = new PdfConverter(notebook, DefaultColorPalette.Grayscale);
            byte[] allPages = converter.ConvertAll();

            Utilities.ByteArraysEqual(File.ReadAllBytes(Path.Combine(_testDataLocation, "A5X_TestNote.pdf")), allPages).Should().BeTrue();
        }

        [TestMethod]
        public void TestPdfConvertAll_WithLinks()
        {
            Parser parser = new Parser();
            Notebook notebook = parser.LoadNotebook(_A5X_TestNote_Links, Policy.Strict);

            PdfConverter converter = new PdfConverter(notebook, DefaultColorPalette.Grayscale);
            byte[] allPages = converter.ConvertAll(enableLinks: true);

            Utilities.ByteArraysEqual(File.ReadAllBytes(Path.Combine(_testDataLocation, "A5X_TestNote_Links.pdf")), allPages).Should().BeTrue();
        }

        [TestMethod]
        public void TestPdfConvert_Vectorization()
        {
            Parser parser = new Parser();
            Notebook notebook = parser.LoadNotebook(_A5X_TestNote_Vectorization, Policy.Strict);

            PdfConverter converter = new PdfConverter(notebook, DefaultColorPalette.Grayscale);
            byte[] page_0 = converter.Convert(0, vectorize: true);

            Utilities.ByteArraysEqual(File.ReadAllBytes(Path.Combine(_testDataLocation, "A5X_TestNote_Vectorization.pdf")), page_0).Should().BeTrue();
        }

        [TestMethod]
        public void TestSvgConvert()
        {
            Parser parser = new Parser();
            Notebook notebook = parser.LoadNotebook(_A5X_TestNote, Policy.Strict);

            SvgConverter converter = new SvgConverter(notebook, DefaultColorPalette.Grayscale);
            string page_0 = converter.Convert(0);
            string page_1 = converter.Convert(1);
            string page_2 = converter.Convert(2);
            string page_3 = converter.Convert(3);

            Utilities.ByteArraysEqual(File.ReadAllBytes(Path.Combine(_testDataLocation, "A5X_TestNote_0.svg")), Encoding.ASCII.GetBytes(page_0));
            Utilities.ByteArraysEqual(File.ReadAllBytes(Path.Combine(_testDataLocation, "A5X_TestNote_1.svg")), Encoding.ASCII.GetBytes(page_1));
            Utilities.ByteArraysEqual(File.ReadAllBytes(Path.Combine(_testDataLocation, "A5X_TestNote_2.svg")), Encoding.ASCII.GetBytes(page_2));
            Utilities.ByteArraysEqual(File.ReadAllBytes(Path.Combine(_testDataLocation, "A5X_TestNote_3.svg")), Encoding.ASCII.GetBytes(page_3));
        }

        [TestMethod]
        public void TestSvgConvertAll()
        {
            Parser parser = new Parser();
            Notebook notebook = parser.LoadNotebook(_A5X_TestNote, Policy.Strict);

            SvgConverter converter = new SvgConverter(notebook, DefaultColorPalette.Grayscale);
            List<string> allPages = converter.ConvertAll();

            Utilities.ByteArraysEqual(File.ReadAllBytes(Path.Combine(_testDataLocation, "A5X_TestNote_0.svg")), Encoding.ASCII.GetBytes(allPages[0]));
            Utilities.ByteArraysEqual(File.ReadAllBytes(Path.Combine(_testDataLocation, "A5X_TestNote_1.svg")), Encoding.ASCII.GetBytes(allPages[1]));
            Utilities.ByteArraysEqual(File.ReadAllBytes(Path.Combine(_testDataLocation, "A5X_TestNote_2.svg")), Encoding.ASCII.GetBytes(allPages[2]));
            Utilities.ByteArraysEqual(File.ReadAllBytes(Path.Combine(_testDataLocation, "A5X_TestNote_3.svg")), Encoding.ASCII.GetBytes(allPages[3]));
        }

        [TestMethod]
        public void TestTextConvert()
        {
            Parser parser = new Parser();
            Notebook notebook = parser.LoadNotebook(_A5X_TestNote_Realtime, Policy.Strict);

            TextConverter converter = new TextConverter(notebook, DefaultColorPalette.Grayscale);
            string page_0 = converter.Convert(0);
        }
    }
}