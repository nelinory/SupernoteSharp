namespace SupernoteSharp.Common
{
    /// <summary>
    /// Policy:
    /// - Strict: raise exception for unknown signature (default)
    /// - Loose: try to parse for unknown signature
    /// </summary>
    public enum Policy { Strict, Loose }

    public enum VisibilityOverlay : byte { Default, Visible, Invisible }

    public enum LinkDirection { Out = 0, In = 1 }

    public enum LinkType { Page = 0, File = 1, Web = 4 }

    public enum StyleUsageType { Default = 0, Image = 1, Pdf = 2 }
}
