using Codeuctivity.ImageSharpCompare;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SupernoteSharp.Business;
using SupernoteSharp.Common;
using SupernoteSharp.Entities;
using System.Collections.Generic;
using System.IO;
using System.Text;
using static SupernoteSharp.Business.Converter;

namespace SupernoteSharpUnitTests
{
    [TestClass]
    public class TestConverter: TestBase
    {
        [TestMethod]
        public void TestImageConvert_Note()
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
        public void TestImageConvert_Mark()
        {
            Parser parser = new Parser();
            Notebook notebook = parser.LoadNotebook(_A5X_TestNote_Pdf_Mark, Policy.Strict);

            ImageConverter converter = new Converter.ImageConverter(notebook, DefaultColorPalette.Grayscale);
            Image page_0 = converter.Convert(0, ImageConverter.BuildVisibilityOverlay());
            Image page_1 = converter.Convert(1, ImageConverter.BuildVisibilityOverlay());

            ImageSharpCompare.ImagesAreEqual(page_0.CloneAs<Rgba32>(),
                Image.Load<Rgba32>(Path.Combine(Path.Combine(_testDataLocation, "A5X_TestNote.pdf.mark_0.png")))).Should().BeTrue();
            ImageSharpCompare.ImagesAreEqual(page_1.CloneAs<Rgba32>(),
                Image.Load<Rgba32>(Path.Combine(Path.Combine(_testDataLocation, "A5X_TestNote.pdf.mark_1.png")))).Should().BeTrue();
        }

        [TestMethod]
        public void TestImageConvertAll_Note()
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
        public void TestImageConvertAll_Mark()
        {
            Parser parser = new Parser();
            Notebook notebook = parser.LoadNotebook(_A5X_TestNote_Pdf_Mark, Policy.Strict);

            ImageConverter converter = new Converter.ImageConverter(notebook, DefaultColorPalette.Grayscale);

            List<Image> allPages = converter.ConvertAll(ImageConverter.BuildVisibilityOverlay());

            ImageSharpCompare.ImagesAreEqual(allPages[0].CloneAs<Rgba32>(),
                Image.Load<Rgba32>(Path.Combine(Path.Combine(_testDataLocation, "A5X_TestNote.pdf.mark_0.png")))).Should().BeTrue();
            ImageSharpCompare.ImagesAreEqual(allPages[1].CloneAs<Rgba32>(),
                Image.Load<Rgba32>(Path.Combine(Path.Combine(_testDataLocation, "A5X_TestNote.pdf.mark_1.png")))).Should().BeTrue();
        }

        [TestMethod]
        public void TestPdfConvert_Note()
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
        public void TestPdfConvert_Mark()
        {
            Parser parser = new Parser();
            Notebook notebook = parser.LoadNotebook(_A5X_TestNote_Pdf_Mark, Policy.Strict);

            PdfConverter converter = new PdfConverter(notebook, DefaultColorPalette.Grayscale);
            byte[] page_0 = converter.Convert(0);
            byte[] page_1 = converter.Convert(1);

            Utilities.ByteArraysEqual(File.ReadAllBytes(Path.Combine(_testDataLocation, "A5X_TestNote.pdf.mark_0.pdf")), page_0).Should().BeTrue();
            Utilities.ByteArraysEqual(File.ReadAllBytes(Path.Combine(_testDataLocation, "A5X_TestNote.pdf.mark_1.pdf")), page_1).Should().BeTrue();
        }

        [TestMethod]
        public void TestPdfConvertAll_Note()
        {
            Parser parser = new Parser();
            Notebook notebook = parser.LoadNotebook(_A5X_TestNote, Policy.Strict);

            PdfConverter converter = new PdfConverter(notebook, DefaultColorPalette.Grayscale);
            byte[] allPages = converter.ConvertAll();

            Utilities.ByteArraysEqual(File.ReadAllBytes(Path.Combine(_testDataLocation, "A5X_TestNote.pdf")), allPages).Should().BeTrue();
        }

        [TestMethod]
        public void TestPdfConvertAll_Mark()
        {
            Parser parser = new Parser();
            Notebook notebook = parser.LoadNotebook(_A5X_TestNote_Pdf_Mark, Policy.Strict);

            PdfConverter converter = new PdfConverter(notebook, DefaultColorPalette.Grayscale);
            byte[] allPages = converter.ConvertAll();

            Utilities.ByteArraysEqual(File.ReadAllBytes(Path.Combine(_testDataLocation, "A5X_TestNote.pdf.mark.pdf")), allPages).Should().BeTrue();
        }

        [TestMethod]
        public void TestPdfConvertAll_Note_WithLinks()
        {
            Parser parser = new Parser();
            Notebook notebook = parser.LoadNotebook(_A5X_TestNote_Links, Policy.Strict);

            PdfConverter converter = new PdfConverter(notebook, DefaultColorPalette.Grayscale);
            byte[] allPages = converter.ConvertAll(enableLinks: true);

            Utilities.ByteArraysEqual(File.ReadAllBytes(Path.Combine(_testDataLocation, "A5X_TestNote_Links.pdf")), allPages).Should().BeTrue();
        }

        [TestMethod]
        public void TestPdfConvert_Note_Vectorization()
        {
            Parser parser = new Parser();
            Notebook notebook = parser.LoadNotebook(_A5X_TestNote_Vectorization, Policy.Strict);

            PdfConverter converter = new PdfConverter(notebook, DefaultColorPalette.Grayscale);
            byte[] page_0 = converter.Convert(0, vectorize: true);

            Utilities.ByteArraysEqual(File.ReadAllBytes(Path.Combine(_testDataLocation, "A5X_TestNote_Vectorization.pdf")), page_0).Should().BeTrue();
        }

        [TestMethod]
        public void TestPdfConvertAll_Mark_Vectorization()
        {
            Parser parser = new Parser();
            Notebook notebook = parser.LoadNotebook(_A5X_TestNote_Pdf_Mark, Policy.Strict);

            PdfConverter converter = new PdfConverter(notebook, DefaultColorPalette.Grayscale);
            byte[] allPages = converter.ConvertAll(vectorize: true);

            Utilities.ByteArraysEqual(File.ReadAllBytes(Path.Combine(_testDataLocation, "A5X_TestNote.pdf.mark_Vectorization.pdf")), allPages).Should().BeTrue();
        }

        [TestMethod]
        public void TestPdfConvert_Note_With_Pdf_Template_Vectorization()
        {
            Parser parser = new Parser();
            Notebook notebook = parser.LoadNotebook(_A5X_TestNote_With_Pdf_Template, Policy.Strict);

            PdfConverter converter = new PdfConverter(notebook, DefaultColorPalette.Grayscale);
            byte[] allPages = converter.ConvertAll(vectorize: true, enableLinks: true);

            Utilities.ByteArraysEqual(File.ReadAllBytes(Path.Combine(_testDataLocation, "A5X_TestNote_With_Pdf_Template.pdf")), allPages).Should().BeTrue();
        }

        [TestMethod]
        public void TestSvgConvert_Note()
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
        public void TestSvgConvert_Mark()
        {
            Parser parser = new Parser();
            Notebook notebook = parser.LoadNotebook(_A5X_TestNote_Pdf_Mark, Policy.Strict);

            SvgConverter converter = new SvgConverter(notebook, DefaultColorPalette.Grayscale);
            string page_0 = converter.Convert(0);
            string page_1 = converter.Convert(1);

            Utilities.ByteArraysEqual(File.ReadAllBytes(Path.Combine(_testDataLocation, "A5X_TestNote.pdf.mark_0.svg")), Encoding.ASCII.GetBytes(page_0));
            Utilities.ByteArraysEqual(File.ReadAllBytes(Path.Combine(_testDataLocation, "A5X_TestNote.pdf.mark_1.svg")), Encoding.ASCII.GetBytes(page_1));
        }

        [TestMethod]
        public void TestSvgConvertAll_Note()
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
        public void TestSvgConvertAll_Mark()
        {
            Parser parser = new Parser();
            Notebook notebook = parser.LoadNotebook(_A5X_TestNote_Pdf_Mark, Policy.Strict);

            SvgConverter converter = new SvgConverter(notebook, DefaultColorPalette.Grayscale);
            List<string> allPages = converter.ConvertAll();

            Utilities.ByteArraysEqual(File.ReadAllBytes(Path.Combine(_testDataLocation, "A5X_TestNote.pdf.mark_0.svg")), Encoding.ASCII.GetBytes(allPages[0]));
            Utilities.ByteArraysEqual(File.ReadAllBytes(Path.Combine(_testDataLocation, "A5X_TestNote.pdf.mark_1.svg")), Encoding.ASCII.GetBytes(allPages[1]));
        }

        [TestMethod]
        public void TestRealtimeConvert_Note()
        {
            Parser parser = new Parser();
            Notebook notebook = parser.LoadNotebook(_A5X_TestNote_Realtime, Policy.Strict);

            TextConverter converter = new TextConverter(notebook);
            string page_0 = converter.Convert(0);
            string page_1 = converter.Convert(1);

            string expected_0 = File.ReadAllText(Path.Combine(_testDataLocation, "A5X_TestNote_Realtime_0.txt"));
            string expected_1 = File.ReadAllText(Path.Combine(_testDataLocation, "A5X_TestNote_Realtime_1.txt"));

            page_0.Should().BeEquivalentTo(expected_0);
            page_1.Should().BeEquivalentTo(expected_1);
        }

        [TestMethod]
        public void TestPdfConvert_Note_2_11_26()
        {
            Parser parser = new Parser();
            Notebook notebook = parser.LoadNotebook(_A5X_TestNote_2_11_26, Policy.Strict);

            PdfConverter converter = new PdfConverter(notebook, DefaultColorPalette.Grayscale);
            byte[] allPages = converter.ConvertAll(vectorize: true, enableLinks: true);

            Utilities.ByteArraysEqual(File.ReadAllBytes(Path.Combine(_testDataLocation, "A5X_TestNote_2.11.26.pdf")), allPages).Should().BeTrue();
        }

        [TestMethod]
        public void TestPdfConvert_Note_2_15_29()
        {
            Parser parser = new Parser();
            Notebook notebook = parser.LoadNotebook(_A5X_TestNote_2_15_29, Policy.Strict);

            PdfConverter converter = new PdfConverter(notebook, DefaultColorPalette.Grayscale);
            byte[] page_0 = converter.Convert(0, vectorize: true);

            Utilities.ByteArraysEqual(File.ReadAllBytes(Path.Combine(_testDataLocation, "A5X_TestNote_2.15.29.pdf")), page_0).Should().BeTrue();
        }
    }
}