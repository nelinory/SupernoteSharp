using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace SupernoteSharpUnitTests
{
    public class TestBase
    {
        internal static FileStream _A5X_TestNote;
        internal static FileStream _A5X_TestNote_Links;
        internal static FileStream _A5X_TestNote_Realtime;
        internal static FileStream _A5X_TestNote_Vectorization;
        internal static FileStream _A5X_TestNote_Pdf_Mark;
        internal static FileStream _A5X_TestNote_With_Pdf_Template;
        internal static FileStream _A5X_TestNote_2_11_26;
        internal static string _testDataLocation = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData");

        [TestInitialize]
        public void Setup()
        {
            _A5X_TestNote = new FileStream(Path.Combine(_testDataLocation, "A5X_TestNote.note"), FileMode.Open, FileAccess.Read);
            _A5X_TestNote_Links = new FileStream(Path.Combine(_testDataLocation, "A5X_TestNote_Links.note"), FileMode.Open, FileAccess.Read);
            _A5X_TestNote_Realtime = new FileStream(Path.Combine(_testDataLocation, "A5X_TestNote_Realtime.note"), FileMode.Open, FileAccess.Read);
            _A5X_TestNote_Vectorization = new FileStream(Path.Combine(_testDataLocation, "A5X_TestNote_Vectorization.note"), FileMode.Open, FileAccess.Read);
            _A5X_TestNote_Pdf_Mark = new FileStream(Path.Combine(_testDataLocation, "A5X_TestNote.pdf.mark"), FileMode.Open, FileAccess.Read);
            _A5X_TestNote_With_Pdf_Template = new FileStream(Path.Combine(_testDataLocation, "A5X_TestNote_With_Pdf_Template.note"), FileMode.Open, FileAccess.Read);
            _A5X_TestNote_2_11_26 = new FileStream(Path.Combine(_testDataLocation, "A5X_TestNote_2.11.26.note"), FileMode.Open, FileAccess.Read);
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

            if (_A5X_TestNote_Pdf_Mark != null)
                _A5X_TestNote_Pdf_Mark.Close();

            if (_A5X_TestNote_With_Pdf_Template != null)
                _A5X_TestNote_With_Pdf_Template.Close();

            if (_A5X_TestNote_2_11_26 != null)
                _A5X_TestNote_2_11_26.Close();
        }
    }
}
