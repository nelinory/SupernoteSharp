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
        private static FileStream _fileStream;
        private static string _testDataLocation = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData");

        [TestInitialize]
        public void Setup()
        {
            _fileStream = new FileStream(Path.Combine(_testDataLocation, "A5X_TestNote.note"), FileMode.Open, FileAccess.Read);
        }

        [TestCleanup]
        public void TearDown()
        {
            if (_fileStream != null)
                _fileStream.Close();
        }

        [TestMethod]
        public void TestParseMetadata()
        {
            Parser parser = new Parser();

            // generate metadata from a note test file
            Metadata actual = parser.ParseMetadata(_fileStream, Policy.Strict);

            // load metadata from a json test file
            string expectedContent = File.ReadAllText(Path.Combine(_testDataLocation, "A5X_TestNote.json"));
            Metadata expected = JsonSerializer.Deserialize<Metadata>(expectedContent);

            actual.ToJson().Should().BeEquivalentTo(expected.ToJson()); // compare actual document with the sample document
        }

        [TestMethod]
        public void TestLoadNotebook()
        {
            Parser parser = new Parser();
            Notebook notebook = parser.LoadNotebook(_fileStream, Policy.Strict);

            notebook.Metadata.Should().NotBeNull();
            notebook.Signature.Should().BeEquivalentTo("noteSN_FILE_VER_20220013");
            notebook.Cover.Content.Should().NotBeNull();
            notebook.Titles.Count.Should().Be(2); // test document have 2 titles
            notebook.Keywords.Count.Should().Be(2); // test document have 2 keywords
            notebook.Links.Count.Should().Be(7); // test document have 7 links: 6 internal + 1 external
            notebook.TotalPages.Should().Be(4); // test document have 4 pages
            notebook.Pages.Count.Should().Be(4); // test document have 4 pages
            notebook.Pages[2].LayerOrder.Count.Should().Be(5); // test document page 3 have total of 5 layers
            notebook.Pages[2].LayerOrder[1].Should().Be("LAYER2");
            notebook.Pages[2].LayerOrder[3].Should().Be("MAINLAYER");
            notebook.Pages[2].Protocol.Should().Be("RATTA_RLE");
            notebook.Pages[3].Style.StartsWith("user_"); // test document page 4 have custom template
            notebook.FileId.Should().Be("F20230426085756711303PkPWUQZNRUPC");
            notebook.IsRealtimeRecognition.Should().BeFalse(); // test document does not have realtime recognition enabled
        }
    }
}