namespace QuickFileMarker.Loader.Constants
{
    public enum MarkerValidity
    {
        /// <summary>
        /// The marker is still valid.
        /// </summary>
        Valid,
        /// <summary>
        /// The file no longer exists, or have been moved.
        /// </summary>
        MissingFile,
        /// <summary>
        /// The selected text no longer exists in the file.
        /// </summary>
        SellectedTextMissing,
        /// <summary>
        /// The complete line text no longer exists.
        /// </summary>
        SellectedTextLineMissing,
        /// <summary>
        /// Line number or carret position are out of the file content.
        /// </summary>
        RangeOverflow
    }
}
