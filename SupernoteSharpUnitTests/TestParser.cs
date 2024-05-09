using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SupernoteSharp.Business;
using SupernoteSharp.Common;
using SupernoteSharp.Entities;
using System.IO;
using System.Text.Json;

namespace SupernoteSharpUnitTests
{
    [TestClass]
    public class TestParser: TestBase
    {
        [TestMethod]
        public void TestParseMetadata_Note()
        {
            Parser parser = new Parser();

            // generate metadata from a note test file
            Metadata actual = parser.ParseMetadata(_A5X_TestNote, Policy.Strict);
            string expectedContent = File.ReadAllText(Path.Combine(_testDataLocation, "A5X_TestNote.json"));
            Metadata expected = JsonSerializer.Deserialize<Metadata>(expectedContent);

            actual.ToJson().Should().BeEquivalentTo(expected.ToJson());

            // generate metadata from a note test file with link
            actual = parser.ParseMetadata(_A5X_TestNote_Links, Policy.Strict);
            expectedContent = File.ReadAllText(Path.Combine(_testDataLocation, "A5X_TestNote_Links.json"));
            expected = JsonSerializer.Deserialize<Metadata>(expectedContent);

            actual.ToJson().Should().BeEquivalentTo(expected.ToJson());

            // generate metadata from a realtime note test file
            actual = parser.ParseMetadata(_A5X_TestNote_Realtime, Policy.Strict);
            expectedContent = File.ReadAllText(Path.Combine(_testDataLocation, "A5X_TestNote_Realtime.json"));
            expected = JsonSerializer.Deserialize<Metadata>(expectedContent);

            actual.ToJson().Should().BeEquivalentTo(expected.ToJson());

            // generate metadata from a vectorization note test file
            actual = parser.ParseMetadata(_A5X_TestNote_Vectorization, Policy.Strict);
            expectedContent = File.ReadAllText(Path.Combine(_testDataLocation, "A5X_TestNote_Vectorization.json"));
            expected = JsonSerializer.Deserialize<Metadata>(expectedContent);

            actual.ToJson().Should().BeEquivalentTo(expected.ToJson());

            // generate metadata from a mark test file
            actual = parser.ParseMetadata(_A5X_TestNote_Pdf_Mark, Policy.Strict);
            expectedContent = File.ReadAllText(Path.Combine(_testDataLocation, "A5X_TestNote.pdf.mark.json"));
            expected = JsonSerializer.Deserialize<Metadata>(expectedContent);

            actual.ToJson().Should().BeEquivalentTo(expected.ToJson());

            // generate metadata from a note test file with pdf template
            actual = parser.ParseMetadata(_A5X_TestNote_With_Pdf_Template, Policy.Strict);
            expectedContent = File.ReadAllText(Path.Combine(_testDataLocation, "A5X_TestNote_With_Pdf_Template.json"));
            expected = JsonSerializer.Deserialize<Metadata>(expectedContent);

            actual.ToJson().Should().BeEquivalentTo(expected.ToJson());

            // generate metadata from a note test file for 2_11_26 firmware
            actual = parser.ParseMetadata(_A5X_TestNote_2_11_26, Policy.Strict);
            expectedContent = File.ReadAllText(Path.Combine(_testDataLocation, "A5X_TestNote_2.11.26.json"));
            expected = JsonSerializer.Deserialize<Metadata>(expectedContent);

            actual.ToJson().Should().BeEquivalentTo(expected.ToJson());
        }

        [TestMethod]
        public void TestLoadNotebook_Note()
        {
            Parser parser = new Parser();
            Notebook notebook = parser.LoadNotebook(_A5X_TestNote, Policy.Strict);

            notebook.Metadata.Should().NotBeNull();
            notebook.FileType.Should().Be("NOTE");
            notebook.Signature.Should().BeEquivalentTo("noteSN_FILE_VER_20220013");
            notebook.Cover.Content.Should().NotBeNull();
            notebook.Titles.Count.Should().Be(2); // test note have 2 titles
            notebook.Keywords.Count.Should().Be(2); // test note have 2 keywords
            notebook.Links.Count.Should().Be(4); // test note have 4 links: 3 internal + 1 external
            notebook.TotalPages.Should().Be(4); // test note have 4 pages
            notebook.Pages.Count.Should().Be(4); // test note have 4 pages
            notebook.Pages[2].LayerOrder.Count.Should().Be(5); // test note page 3 have total of 5 layers
            notebook.Pages[2].LayerOrder[1].Should().Be("LAYER2");
            notebook.Pages[2].LayerOrder[3].Should().Be("MAINLAYER");
            notebook.Pages[2].Protocol.Should().Be("RATTA_RLE");
            notebook.Pages[3].Style.Should().StartWith("user_"); // test note page 4 have custom templates
            notebook.PdfStyle.Should().Be("none");
            notebook.PdfStyleMd5.Should().Be("0");
            notebook.StyleUsageType.Should().Be(StyleUsageType.Default);
            notebook.FileId.Should().Be("F20230426085756711303PkPWUQZNRUPC");
            notebook.IsRealtimeRecognition.Should().BeFalse(); // test note does not have realtime recognition enabled
        }

        [TestMethod]
        public void TestLoadNotebook_Note_Links()
        {
            Parser parser = new Parser();
            Notebook notebook = parser.LoadNotebook(_A5X_TestNote_Links, Policy.Strict);

            notebook.Metadata.Should().NotBeNull();
            notebook.FileType.Should().Be("NOTE");
            notebook.Signature.Should().BeEquivalentTo("noteSN_FILE_VER_20220013");
            notebook.Cover.Content.Should().BeNull();
            notebook.Titles.Count.Should().Be(0);
            notebook.Keywords.Count.Should().Be(0);
            notebook.Links.Count.Should().Be(4); // test note have 4 links: 4 internal
            notebook.TotalPages.Should().Be(2); // test note have 2 pages
            notebook.Pages.Count.Should().Be(2); // test note have 2 pages
            notebook.PdfStyle.Should().Be("none");
            notebook.PdfStyleMd5.Should().Be("0");
            notebook.StyleUsageType.Should().Be(StyleUsageType.Default);
            notebook.FileId.Should().Be("F20230606174214710369I9D1tLkndv5d");
            notebook.IsRealtimeRecognition.Should().BeFalse(); // test note does not have realtime recognition enabled
        }

        [TestMethod]
        public void TestLoadNotebook_Note_Realtime()
        {
            Parser parser = new Parser();
            Notebook notebook = parser.LoadNotebook(_A5X_TestNote_Realtime, Policy.Strict);

            notebook.Metadata.Should().NotBeNull();
            notebook.FileType.Should().Be("NOTE");
            notebook.Signature.Should().BeEquivalentTo("noteSN_FILE_VER_20220013");
            notebook.Cover.Content.Should().BeNull();
            notebook.Titles.Count.Should().Be(0);
            notebook.Keywords.Count.Should().Be(0);
            notebook.Links.Count.Should().Be(0);
            notebook.TotalPages.Should().Be(2); // test note have 2 pages
            notebook.Pages.Count.Should().Be(2); // test note have 2 pages
            notebook.PdfStyle.Should().Be("none");
            notebook.PdfStyleMd5.Should().Be("0");
            notebook.StyleUsageType.Should().Be(StyleUsageType.Default);
            notebook.FileId.Should().Be("F20230623105319782029GcclbjlFqsoq");
            notebook.IsRealtimeRecognition.Should().BeTrue(); // test note does have realtime recognition enabled
        }

        [TestMethod]
        public void TestLoadNotebook_Note_Vectorization()
        {
            Parser parser = new Parser();
            Notebook notebook = parser.LoadNotebook(_A5X_TestNote_Vectorization, Policy.Strict);

            notebook.Metadata.Should().NotBeNull();
            notebook.FileType.Should().Be("NOTE");
            notebook.Signature.Should().BeEquivalentTo("noteSN_FILE_VER_20220013");
            notebook.Cover.Content.Should().BeNull();
            notebook.Titles.Count.Should().Be(0);
            notebook.Keywords.Count.Should().Be(0);
            notebook.Links.Count.Should().Be(0);
            notebook.TotalPages.Should().Be(1); // test note have 1 page
            notebook.Pages.Count.Should().Be(1); // test note have 1 page
            notebook.PdfStyle.Should().Be("none");
            notebook.PdfStyleMd5.Should().Be("0");
            notebook.StyleUsageType.Should().Be(StyleUsageType.Default);
            notebook.FileId.Should().Be("F20230617090423828272YLyqIslswPlI");
            notebook.IsRealtimeRecognition.Should().BeFalse(); // test note does not have realtime recognition enabled
        }

        [TestMethod]
        public void TestLoadNotebook_Mark()
        {
            Parser parser = new Parser();
            Notebook notebook = parser.LoadNotebook(_A5X_TestNote_Pdf_Mark, Policy.Strict);

            notebook.Metadata.Should().NotBeNull();
            notebook.FileType.Should().Be("MARK");
            notebook.Signature.Should().BeEquivalentTo("markSN_FILE_VER_20220013");
            notebook.Cover.Content.Should().BeNull();
            notebook.TotalPages.Should().Be(2); // test note have 2 pages
            notebook.Pages.Count.Should().Be(2); // test note have 2 pages
            notebook.Pages[0].LayerOrder.Count.Should().Be(1); // test note page 0 have total of 1 layers
            notebook.Pages[0].LayerOrder[0].Should().Be("MAINLAYER");
            notebook.Pages[0].Protocol.Should().Be("RATTA_RLE");
            notebook.Pages[0].Style.Should().StartWith("none"); // test note page have no custom templates
            notebook.PdfStyle.Should().Be("none");
            notebook.PdfStyleMd5.Should().Be("0");
            notebook.StyleUsageType.Should().Be(StyleUsageType.Default);
            notebook.FileId.Should().Be("F20230615115246511903rxKCoF4YD7zG");
            notebook.IsRealtimeRecognition.Should().BeFalse(); // test note does not have realtime recognition enabled
        }

        [TestMethod]
        public void TestLoadNotebook_Pdf_Template()
        {
            Parser parser = new Parser();
            Notebook notebook = parser.LoadNotebook(_A5X_TestNote_With_Pdf_Template, Policy.Strict);

            notebook.Metadata.Should().NotBeNull();
            notebook.FileType.Should().Be("NOTE");
            notebook.Signature.Should().BeEquivalentTo("noteSN_FILE_VER_20220013");
            notebook.Cover.Content.Should().BeNull();
            notebook.Links.Count.Should().Be(0);
            notebook.TemplateLinks.Count.Should().Be(4); // test note pdf template have 4 links: 3 internal and 1 external
            notebook.TotalPages.Should().Be(3); // test note have 3 pages
            notebook.Pages.Count.Should().Be(3); // test note have 3 pages
            notebook.PdfStyle.Should().Be("user_pdf_Pdf_Template_3");
            notebook.PdfStyleMd5.Should().Be("eddc1d3fb9837d1b8812ef3eb77dc5e1_9954");
            notebook.StyleUsageType.Should().Be(StyleUsageType.Pdf);
            notebook.FileId.Should().Be("F20231219133335147638a6XV72A419r2");
            notebook.IsRealtimeRecognition.Should().BeFalse(); // test note does not have realtime recognition enabled
        }

        //[TestMethod]
        //public void TestLoadNotebook_2_11_26_Strict()
        //{
        //    Parser parser = new Parser();
        //    _ = parser.Invoking(y => y.LoadNotebook(_A5X_TestNote_2_11_26, Policy.Strict)).Should().Throw<UnsupportedFileFormatException>()
        //            .WithMessage("Unsupported file format. Signature found: noteSN_FILE_VER_20230014");
        //}

        [TestMethod]
        public void TestLoadNotebook_2_11_26()
        {
            Parser parser = new Parser();
            Notebook notebook = parser.LoadNotebook(_A5X_TestNote_2_11_26, Policy.Strict);

            notebook.Metadata.Should().NotBeNull();
            notebook.FileType.Should().Be("NOTE");
            notebook.Signature.Should().BeEquivalentTo("noteSN_FILE_VER_20230014");
            notebook.Cover.Content.Should().BeNull();
            notebook.Links.Count.Should().Be(3);
            notebook.TemplateLinks.Count.Should().Be(0);
            notebook.TotalPages.Should().Be(3); // test note have 3 pages
            notebook.Pages.Count.Should().Be(3); // test note have 3 pages
            notebook.PdfStyle.Should().Be("none");
            notebook.PdfStyleMd5.Should().Be("0");
            notebook.StyleUsageType.Should().Be(StyleUsageType.Image);
            notebook.FileId.Should().Be("F20240221111319916429VSQ21RFBB6aU");
            notebook.IsRealtimeRecognition.Should().BeFalse(); // test note does not have realtime recognition enabled
        }
    }
}