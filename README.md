# SupernoteSharp
![GitHub release](https://img.shields.io/github/release/nelinory/SupernoteSharp)
![GitHub issues](https://img.shields.io/github/issues/nelinory/SupernoteSharp)
![Github license](https://img.shields.io/github/license/nelinory/SupernoteSharp)

SupernoteSharp is an unofficial library for Supernote paper-like tablet by Ratta (https://supernote.com). It allows exporting Supernote's `*.note` & `*.mark` file formats.

> SupernoteSharp is multi-platform library build with .NET 6.

### Credits
This project is heavily inspired by https://github.com/jya-dev/supernote-tool.

### Supported file formats
- [ ] `*.note` file created on Supernote A5
- [X] `*.note` file created on Supernote A5X/A6X (firmware Chauvet 2.8.22)
- [ ] `*.mark` file created on Supernote A5X/A6X (firmware Chauvet 2.8.22)

### Key Features - A5X/A6X models only
- [X] Export the Supernote file structure (metadata)
```C#
    using (FileStream fileStream = new FileStream(NOTE_FILE_PATH, FileMode.Open, FileAccess.Read))
    {
        Parser parser = new Parser();
        Metadata metadata = parser.ParseMetadata(fileStream, Policy.Strict);

        // metadata
        string metadataJson = metadata.ToJson();
    }
```
- [X] Export individual pages/all pages to png file format
```C#
    using (FileStream fileStream = new FileStream(NOTE_FILE_PATH, FileMode.Open, FileAccess.Read))
    {
        Parser parser = new Parser();
        Notebook notebook = parser.LoadNotebook(fileStream, Policy.Strict);
        ImageConverter converter = new Converter.ImageConverter(notebook, DefaultColorPalette.Grayscale);

        // convert a page to PNG
        Image page_0 = converter.Convert(0, VisibilityOverlay.Default);
        // save the result
        page_0.SaveAsPng(PNG_FILE_LOCATION);

        // convert all pages to PNG
        List<Image> images = converter.ConvertAll(VisibilityOverlay.Default);
        // save the result
        ...
    }
```
- [X] Export individual pages/all pages to pdf file format
```C#
    using (FileStream fileStream = new FileStream(NOTE_FILE_PATH, FileMode.Open, FileAccess.Read))
    {
        Parser parser = new Parser();
        Notebook notebook = parser.LoadNotebook(fileStream, Policy.Strict);
        PdfConverter converter = new PdfConverter(notebook, DefaultColorPalette.Grayscale);

        // convert a page to PDF
        byte[] page_0 = converter.Convert(0);
        // save the result
        File.WriteAllBytes(PDF_FILE_LOCATION, page_0);

        // convert all pages to PDF
        byte[] allPages = converter.ConvertAll();
        // save the result
        ...

        // convert all pages to PDF and build all links
        byte[] allPages = converter.ConvertAll(enableLinks: true);
        // save the result
        ...
    }
```
- [ ] Export individual pages/all pages to svg file format
- [ ] Export individual pages/all pages to vector pdf file format
- [ ] Export all text from realtime recognition note to text file format
- [ ] Export individual annotation/all annotations for a pdf file format

### Tested on
- Windows 10 version 22H2 (OS Build 19045.2846)
- Windows 11 version 22H2 (OS Build 22621.1413)
 
### Used Nuget Packages
- SupernoteSharp
    - SixLabors.ImageSharp: https://github.com/SixLabors/ImageSharp
    - VectSharp: https://github.com/arklumpus/VectSharp
    - VectSharp.PDF: https://github.com/arklumpus/VectSharp
- SupernoteSharpUnitTests
    - SixLabors.ImageSharp: https://github.com/SixLabors/ImageSharp
    - Codeuctivity.ImageSharpCompare: https://github.com/Codeuctivity/ImageSharp.Compare
    - coverlet.collector: https://github.com/coverlet-coverage/coverlet
    - FluentAssertions: https://fluentassertions.com
    - Microsoft.NET.Test.Sdk: https://github.com/microsoft/vstest
    - MSTest.TestAdapter: https://github.com/microsoft/testfx
    - MSTest.TestFramework: https://github.com/microsoft/testfx
