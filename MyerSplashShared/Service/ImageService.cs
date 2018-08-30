using JP.API;
using MyerSplash.Data;
using MyerSplashShared.Data;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyerSplashShared.Service
{
    public class ImageService : ImageServiceBase
    {
        protected string RequestUrl { get; set; }

        public int Count { get; set; } = 30;

        public ImageService(string url, UnsplashImageFactory factory,
            CancellationTokenSourceFactory ctsFactory) : base(factory, ctsFactory)
        {
            RequestUrl = url;
        }

        public Task<CommonRespMsg> GetImageDetailAsync(string id)
        {
            return _cloudService.GetImageDetailAsync(id, GetCancellationToken());
        }

        public override async Task<IEnumerable<UnsplashImage>> GetImagesAsync()
        {
            var result = await _cloudService.GetImagesAsync(Page, Count, GetCancellationToken(), RequestUrl);
            if (result.IsRequestSuccessful)
            {
                var imageList = _ImageFactory.GetImages(result.JsonSrc);
                return imageList;
            }
            else
            {
                return null;
            }
        }
    }
}