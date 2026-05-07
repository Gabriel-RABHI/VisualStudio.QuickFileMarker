using QuickFileMarker.Loader.Constants;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuickFileMarker.Loader.Contracts
{
    /// <summary>
    /// A Helper class that permit to access the Markers in a structured way.
    /// It manage the file access to prevent race conditions and stale reads.
    /// It listen the file system from the RootPathFilters root path.
    /// THe various published IMarkerGroup instance are stable.
    /// </summary>
    internal interface IFileMarkerLoader : IDisposable
    {
        /// <summary>
        /// The Loader instance paths to filter IFileMarker by FilePath property.
        /// </summary>
        string[] RootPathFilters { get; set; }

        /// <summary>
        /// There is two list of Markers : MarkerGroups and OtherGroups. They are filtered by Flag value.
        /// The default value is ["MARKER"].
        /// </summary>
        string[] MarkerFlags { get; set; }

        /// <summary>
        /// A Marker group is a small list of markers that are clustered by TimeStamp, filtered using the MarkerFlags array.
        /// To compute the clustering, an average delay is computed in between the Marker.
        /// </summary>
        IEnumerable<IMarkerGroup> MarkerGroups { get; }

        /// <summary>
        /// Groups that do not comply with MarkerFlags array.
        /// The "SHOW" Flag value will be here, by default.
        /// </summary>
        IEnumerable<IMarkerGroup> OtherGroups { get; }

        /// <summary>
        /// Register a IFileMarkerLoaderListener object instance using Weak Reference.
        /// </summary>
        /// <param name="listener">The IFileMarkerLoaderListener</param>
        void AddListener(IFileMarkerLoaderListener listener);
    }

    /// <summary>
    /// A client, observer contract.
    /// </summary>
    internal interface IFileMarkerLoaderListener
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
        object ClientToken { get; set; }

        T TokenAs<T>() => ClientToken != default ? (T)ClientToken : default;
    }

    internal interface IFileMarker
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
        /// Compute if the Marker is valid.
        /// </summary>
        public MarkerValidity Validity { get; }

        /// <summary>
        /// An object attached by the client code to manage private states.
        /// </summary>
        object ClientToken { get; set; }

        T TokenAs<T>() => ClientToken != default ? (T)ClientToken : default;
    }

    internal interface IFileSection
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
        /// The sellected text in the IDE editor.
        /// </summary>
        string SellectedText { get; }

        /// <summary>
        /// The sellected text, entire line.
        /// </summary>
        string SellectedTextLine { get; }

        /// <summary>
        /// The current carret line number.
        /// </summary>
        string CarretLine { get; }

        /// <summary>
        /// The char number in the carret line.
        /// </summary>
        string CharPositionInCarretLine { get; }

        /// <summary>
        /// Sellection state line, if there is a sellection
        /// </summary>
        int SellectionStartLine { get; }

        /// <summary>
        /// Sellection state line, if there is a sellection
        /// </summary>
        int SellectionEndLine { get; }

        /// <summary>
        /// Moment the marker had been created.
        /// </summary>
        DateTime TimeStamp { get; }

        /// <summary>
        /// Compute if the Marker is valid.
        /// </summary>
        MarkerValidity Validity { get; }

        /// <summary>
        /// An object attached by the client code to manage private states.
        /// </summary>
        object ClientToken { get; set; }

        T TokenAs<T>() => ClientToken != default ? (T)ClientToken : default;
    }
}
