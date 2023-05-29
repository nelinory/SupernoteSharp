namespace SupernoteSharp.Common
{
    internal static class Constants
    {
        // general Supernote constraints
        internal const int PAGE_HEIGHT = 1872;
        internal const int PAGE_WIDTH = 1404;
        internal const int ADDRESS_SIZE = 4;
        internal const int LENGTH_FIELD_SIZE = 4;

        // max layers currently allowed in Supernote X series
        internal const int MAX_LAYERS = 5;

        // realtime recognition state
        internal const int RECOGNSTATUS_NONE = 0;
        internal const int RECOGNSTATUS_DONE = 1;
        internal const int RECOGNSTATUS_RUNNING = 2;

        // note document key sections
        internal const string KEY_SIGNATURE = "__signature__";
        internal const string KEY_HEADER = "__header__";
        internal const string KEY_FOOTER = "__footer__";
        internal const string KEY_PAGES = "__pages__";
        internal const string KEY_LAYERS = "__layers__";
        internal const string KEY_KEYWORDS = "__keywords__";
        internal const string KEY_TITLES = "__titles__";
        internal const string KEY_LINKS = "__links__";

        // color mode
        internal const string MODE_GRAYSCALE = "grayscale";
        internal const string MODE_RGB = "rgb";

        // preset grayscale colors
        internal const byte GRAYSCALE_BLACK = 0x00;
        internal const byte GRAYSCALE_DARK_GRAY = 0x9d;
        internal const byte GRAYSCALE_GRAY = 0xc9;
        internal const byte GRAYSCALE_WHITE = 0xfe;
        internal const byte GRAYSCALE_TRANSPARENT = 0xff;

        // preset RGB colors
        internal const int RGB_BLACK = 0x000000;
        internal const int RGB_DARK_GRAY = 0x9d9d9d;
        internal const int RGB_GRAY = 0xc9c9c9;
        internal const int RGB_WHITE = 0xfefefe;
        internal const int RGB_TRANSPARENT = 0xffffff;
    }
}