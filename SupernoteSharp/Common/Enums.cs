namespace SupernoteSharp.Common
{
    /// <summary>
    /// Policy:
    /// - Strict: raise exception for unknown signature (default)
    /// - Loose: try to parse for unknown signature
    /// </summary>
    public enum Policy { Strict, Loose }

    public enum VisibilityOverlay : byte { Default, Visible, Invisible }
}
