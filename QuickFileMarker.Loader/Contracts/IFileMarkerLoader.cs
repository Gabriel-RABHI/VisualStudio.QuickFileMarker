using QuickFileMarker.Loader.Constants;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuickFileMarker.Loader.Contracts
{
    /// <summary>
    /// A Helper class that permits access to the Markers in a structured way.
    /// It manages the file access to prevent race conditions and stale reads.
    /// It listens to the file system from the root temporary path.
    /// The various published IMarkerGroup instances are stable.
    /// </summary>
    public interface IFileMarkerLoader : IDisposable
    {
        /// <summary>
        /// The Loader instance paths used to filter IFileMarker by FilePath property.
        /// </summary>
        string[] RootPathFilters { get; set; }

        /// <summary>
        /// There are two lists of Markers: MarkerGroups and OtherGroups. They are filtered by Flag value.
        /// The default value is ["MARKER"].
        /// </summary>
        string[] MarkerFlags { get; set; }

        /// <summary>
        /// A Marker group is a small list of markers that are clustered by TimeStamp, filtered using the MarkerFlags array.
        /// To compute the clustering, an average delay is computed between the Markers.
        /// </summary>
        IEnumerable<IFileMarkerGroup> MarkerGroups { get; }

        /// <summary>
        /// Groups that do not comply with the MarkerFlags array.
        /// The "SHOW" Flag value will be here, by default.
        /// </summary>
        IEnumerable<IFileMarkerGroup> OtherGroups { get; }

        /// <summary>
        /// Registers an IFileMarkerLoaderListener object instance using a Weak Reference.
        /// </summary>
        /// <param name="listener">The IFileMarkerLoaderListener</param>
        void AddListener(IFileMarkerLoaderListener listener);
    }

    /// <summary>
    /// A client, observer contract.
    /// </summary>
    public interface IFileMarkerLoaderListener
    {
        void MarkerAddedOrUpdated(IFileMarkerGroup group);

        void ValidityChanged(IFileMarkerGroup group);
    }

    public interface IFileMarkerGroup
    {
        IEnumerable<IFileMarker> Markers { get; }

        /// <summary>
        /// An object attached by the client code to manage private states.
        /// </summary>
        object? ClientToken { get; set; }

        T TokenAs<T>() => ClientToken != default ? (T)ClientToken : default;
    }

    public interface IFileMarker
    {
        IFileMarkerGroup Parent { get; }

        /// <summary>
        /// Absolute path of the marked file.
        /// </summary>
        string FilePath { get; }

        /// <summary>
        /// If multiple consecutive Marker files refer to the same file, sections are added to the same IFileMarker instance.
        /// </summary>
        IEnumerable<IFileSection> Sections { get; }

        /// <summary>
        /// Computes if the Marker is valid.
        /// </summary>
        MarkerValidity Validity { get; }

        /// <summary>
        /// An object attached by the client code to manage private states.
        /// </summary>
        object? ClientToken { get; set; }

        T TokenAs<T>() => ClientToken != default ? (T)ClientToken : default;
    }

    public interface IFileSection
    {
        IFileMarker Parent { get; }

        /// <summary>
        /// The flag from the menu configuration flag
        /// </summary>
        string Flag { get; }

        /// <summary>
        /// Identifier extracted from the file name.
        /// </summary>
        int Identifier { get; }

        /// <summary>
        /// The selected text in the IDE editor.
        /// </summary>
        string SellectedText { get; }

        /// <summary>
        /// The selected text, entire line.
        /// </summary>
        string SellectedTextLine { get; }

        /// <summary>
        /// The current caret line number.
        /// </summary>
        string CarretLine { get; }

        /// <summary>
        /// The char number in the caret line.
        /// </summary>
        string CharPositionInCarretLine { get; }

        /// <summary>
        /// Selection start line, if there is a selection.
        /// </summary>
        int SellectionStartLine { get; }

        /// <summary>
        /// Selection end line, if there is a selection.
        /// </summary>
        int SellectionEndLine { get; }

        /// <summary>
        /// Moment the marker had been created.
        /// </summary>
        DateTime TimeStamp { get; }

        /// <summary>
        /// Computes if the Marker is valid.
        /// </summary>
        MarkerValidity Validity { get; }

        /// <summary>
        /// An object attached by the client code to manage private states.
        /// </summary>
        object? ClientToken { get; set; }

        T TokenAs<T>() => ClientToken != default ? (T)ClientToken : default;
    }
}
