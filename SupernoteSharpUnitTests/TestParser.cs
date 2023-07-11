using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SupernoteSharp.Business;
using SupernoteSharp.Common;
using SupernoteSharp.Entities;
using System;
using System.IO;
using System.Text.Json;

namespace SupernoteSharpUnitTests
{
    [TestClass]
    public class TestParser
    {
        private static FileStream _A5X_TestNote;
        private static FileStream _A5X_TestNote_Links;
        private static FileStream _A5X_TestNote_Pdf_Mark;
        private static string _testDataLocation = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData");

        [TestInitialize]
        public void Setup()
        {
            _A5X_TestNote = new FileStream(Path.Combine(_testDataLocation, "A5X_TestNote.note"), FileMode.Open, FileAccess.Read);
            _A5X_TestNote_Links = new FileStream(Path.Combine(_testDataLocation, "A5X_TestNote_Links.note"), FileMode.Open, FileAccess.Read);
            _A5X_TestNote_Pdf_Mark = new FileStream(Path.Combine(_testDataLocation, "A5X_TestNote.pdf.mark"), FileMode.Open, FileAccess.Read);
        }

        [TestCleanup]
        public void TearDown()
        {
            if (_A5X_TestNote != null)
                _A5X_TestNote.Close();

            if (_A5X_TestNote_Links != null)
                _A5X_TestNote_Links.Close();

            if (_A5X_TestNote_Pdf_Mark != null)
                _A5X_TestNote_Pdf_Mark.Close();
        }

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
        }

        [TestMethod]
        public void TestParseMetadata_Mark()
        {
            Parser parser = new Parser();

            // generate metadata from a mark test file
            Metadata actual = parser.ParseMetadata(_A5X_TestNote_Pdf_Mark, Policy.Strict);
            string expectedContent = File.ReadAllText(Path.Combine(_testDataLocation, "A5X_TestNote.pdf.mark.json"));
            Metadata expected = JsonSerializer.Deserialize<Metadata>(expectedContent);

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
            notebook.Links.Count.Should().Be(7); // test note have 7 links: 6 internal + 1 external
            notebook.TotalPages.Should().Be(4); // test note have 4 pages
            notebook.Pages.Count.Should().Be(4); // test note have 4 pages
            notebook.Pages[2].LayerOrder.Count.Should().Be(5); // test note page 3 have total of 5 layers
            notebook.Pages[2].LayerOrder[1].Should().Be("LAYER2");
            notebook.Pages[2].LayerOrder[3].Should().Be("MAINLAYER");
            notebook.Pages[2].Protocol.Should().Be("RATTA_RLE");
            notebook.Pages[3].Style.Should().StartWith("user_"); // test note page 4 have custom templates
            notebook.FileId.Should().Be("F20230426085756711303PkPWUQZNRUPC");
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
            notebook.TotalPages.Should().Be(2); // test mark have 2 pages
            notebook.Pages.Count.Should().Be(2); // test mark have 2 pages
            notebook.Pages[0].LayerOrder.Count.Should().Be(1); // test mark page 0 have total of 1 layers
            notebook.Pages[0].LayerOrder[0].Should().Be("MAINLAYER");
            notebook.Pages[0].Protocol.Should().Be("RATTA_RLE");
            notebook.Pages[0].Style.Should().StartWith("none"); // test mark page have no custom templates
            notebook.FileId.Should().Be("F20230615115246511903rxKCoF4YD7zG");
            notebook.IsRealtimeRecognition.Should().BeFalse(); // test mark does not have realtime recognition enabled
        }
    }
}