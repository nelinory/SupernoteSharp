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
- [X] `*.note` file created on Supernote A5X (firmware Chauvet 2.8.22)
- [ ] `*.mark` file created on Supernote A5X (firmware Chauvet 2.8.22)
- [X] `*.note` file created on Supernote A6X (firmware Chauvet 2.8.22)
- [ ] `*.mark` file created on Supernote A6X (firmware Chauvet 2.8.22) 

### Key Features - A5X/A6X models only
- [X] Export the Supernote file structure (metadata)
```C#
    using (FileStream fileStream = new FileStream(NOTE_FILE_PATH, FileMode.Open, FileAccess.Read))
    {
        Parser parser = new Parser();
        Metadata metadata = parser.ParseMetadata(_fileStream, Policy.Strict);
        string metadataJson = metadata.ToJson();
    }
```
- [X] Export individual pages/all pages to png file format
```C#
    using (FileStream fileStream = new FileStream(NOTE_FILE_PATH, FileMode.Open, FileAccess.Read))
    {
        Parser parser = new Parser();
        Notebook notebook = parser.LoadNotebook(_fileStream, Policy.Strict);
        ImageConverter converter = new Converter.ImageConverter(notebook, DefaultColorPalette.Grayscale);

        // convert a page
        Image page_0 = converter.Convert(0, VisibilityOverlay.Default);

        // convert all pages
        List<Image> images = converter.ConvertAll(VisibilityOverlay.Default);
    }
```
- [ ] Export individual pages/all pages to svg file format
- [ ] Export individual pages/all pages to pdf file format
- [ ] Export individual pages/all pages to vector pdf file format
- [ ] Export all text from realtime recognition note to text file format
- [ ] Export individual annotation/all annotations for a pdf file format

### Tested on
- Windows 10 version 22H2 (OS Build 19045.2846)
- Windows 11 version 22H2 (OS Build 22621.1413)