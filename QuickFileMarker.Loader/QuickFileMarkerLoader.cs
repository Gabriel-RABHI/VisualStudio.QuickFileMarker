using QuickFileMarker.Loader.Contracts;

namespace QuickFileMarker.Loader
{
    public class QuickFileMarkerLoader : IFileMarkerLoader
    {
        public string[] RootPathFilters { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string[] MarkerFlags { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public IEnumerable<IMarkerGroup> MarkerGroups => throw new NotImplementedException();

        public IEnumerable<IMarkerGroup> OtherGroups => throw new NotImplementedException();

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        void IFileMarkerLoader.AddListener(IFileMarkerLoaderListener listener)
        {
            throw new NotImplementedException();
        }
    }
}
