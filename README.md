# SupernoteSharp
![GitHub release](https://img.shields.io/github/release/nelinory/SupernoteSharp)
![Nuget](https://img.shields.io/nuget/dt/SupernoteSharp?label=nuget%20downloads)
![GitHub issues](https://img.shields.io/github/issues/nelinory/SupernoteSharp)
![Github license](https://img.shields.io/github/license/nelinory/SupernoteSharp)

SupernoteSharp is an unofficial library for Supernote paper-like tablet by Ratta (https://supernote.com). It allows exporting Supernote's `*.note` & `*.mark` file formats.

> SupernoteSharp is multi-platform library build with .NET 6.

### Credits
This project is heavily inspired by https://github.com/jya-dev/supernote-tool.

### Supported file formats
- `*.note` file created on Supernote A5X/A6X (firmware Chauvet 2.15.29)
- `*.mark` pdf annotations created on Supernote A5X/A6X (firmware Chauvet 2.15.29)

### Key Features - A5X/A6X models only
- Export `*.note`/`*.mark` file structure (metadata)
```C#
    using (FileStream fileStream = new FileStream(NOTE_FILE_PATH, FileMode.Open, FileAccess.Read))
    {
        Parser parser = new Parser();
        Metadata metadata = parser.ParseMetadata(fileStream, Policy.Strict);

        // metadata
        string metadataJson = metadata.ToJson();
    }
```
- Export `*.note`/`*.mark` single/all pages to png file format
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
        List<Image> allPages = converter.ConvertAll(VisibilityOverlay.Default);
        // save the result
        ...
    }
```
- Export `*.note`/`*.mark` single/all pages to pdf file format
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

        // convert all pages to PDF and build all links
        // only *.note files supports links
        byte[] allPages = converter.ConvertAll(enableLinks: true);
        // save the result
        ...
    }
```
- Export `*.note`/`*.mark` single/all pages to svg file format
```C#
    using (FileStream fileStream = new FileStream(NOTE_FILE_PATH, FileMode.Open, FileAccess.Read))
    {
        Parser parser = new Parser();
        Notebook notebook = parser.LoadNotebook(fileStream, Policy.Strict);
        SvgConverter converter = new Converter.SvgConverter(notebook, DefaultColorPalette.Grayscale);

        // convert a page to SVG
        string page_0 = converter.Convert(0);
        // save the result
        File.WriteAllText(SVG_FILE_LOCATION, page_0);

        // convert all pages to SVG
        List<string> allPages = converter.ConvertAll();
        // save the result
    }
``` 
- Export `*.note`/`*.mark` single/all pages to vector pdf file format
```C#
    using (FileStream fileStream = new FileStream(NOTE_FILE_PATH, FileMode.Open, FileAccess.Read))
    {
        Parser parser = new Parser();
        Notebook notebook = parser.LoadNotebook(fileStream, Policy.Strict);
        PdfConverter converter = new PdfConverter(notebook, DefaultColorPalette.Grayscale);

        // convert a page to vector PDF
        byte[] page_0 = converter.Convert(0, vectorize: true);
        // save the result
        File.WriteAllBytes(PDF_FILE_LOCATION, page_0);

        // convert all pages to vector PDF and build all links
        // only *.note files supports links
        byte[] allPages = converter.ConvertAll(vectorize: true, enableLinks: true);
        // save the result
        ...
    }
``` 
- Export all text from realtime recognition `*.note` to text file format
```C#
    using (FileStream fileStream = new FileStream(NOTE_FILE_PATH, FileMode.Open, FileAccess.Read))
    {
        Parser parser = new Parser();
        Notebook notebook = parser.LoadNotebook(fileStream, Policy.Strict);
        TextConverter converter = new TextConverter(notebook, DefaultColorPalette.Grayscale);

        // export the realtime text from a page
        string page_0 = converter.Convert(0);
        // save the result
        File.WriteAllText(TXT_FILE_LOCATION, page_0);
    }
```

### Tested on
- Windows 10 version 22H2 (OS Build 19045.2846)
- Windows 11 version 22H2 (OS Build 22621.1413)
 
### Used Nuget Packages
- SupernoteSharp
    - SixLabors.ImageSharp: https://github.com/SixLabors/ImageSharp
    - VectSharp: https://github.com/arklumpus/VectSharp
    - VectSharp.PDF: https://github.com/arklumpus/VectSharp
    - VectSharp.SVG: https://github.com/arklumpus/VectSharp
    - CsPotrace: https://www.drawing3d.de/Downloads.aspx (Vectorization)
- SupernoteSharpUnitTests
    - SixLabors.ImageSharp: https://github.com/SixLabors/ImageSharp
    - Codeuctivity.ImageSharpCompare: https://github.com/Codeuctivity/ImageSharp.Compare
    - coverlet.collector: https://github.com/coverlet-coverage/coverlet
    - FluentAssertions: https://fluentassertions.com
    - Microsoft.NET.Test.Sdk: https://github.com/microsoft/vstest
    - MSTest.TestAdapter: https://github.com/microsoft/testfx
    - MSTest.TestFramework: https://github.com/microsoft/testfx
