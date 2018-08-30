using MyerSplash.Data;
using MyerSplashShared.Data;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyerSplashShared.Service
{
    public abstract class ImageServiceBase : IService
    {
        protected CloudService _cloudService = new CloudService();
        protected UnsplashImageFactory _ImageFactory;
        private CancellationTokenSourceFactory _ctsFactory;
        private CancellationTokenSource _cts;

        public int Page { get; set; } = 1;

        public ImageServiceBase(UnsplashImageFactory factory, CancellationTokenSourceFactory ctsFactory)
        {
            _ImageFactory = factory;
            _ctsFactory = ctsFactory;
        }

        protected CancellationToken GetCancellationToken()
        {
            Cancel();
            _cts = _ctsFactory.Create();
            return _cts.Token;
        }

        public abstract Task<IEnumerable<UnsplashImage>> GetImagesAsync();

        public void Cancel()
        {
            _cts?.Cancel();
        }
    }
}