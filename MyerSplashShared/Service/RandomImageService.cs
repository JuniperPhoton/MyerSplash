using MyerSplash.Data;
using MyerSplashShared.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyerSplashShared.Service
{
    public class RandomImageService : ImageServiceBase
    {
        public int Count { get; set; } = 20;

        public RandomImageService(UnsplashImageFactory factory,
            CancellationTokenSourceFactory ctsFactory) : base(factory, ctsFactory)
        {
        }

        public RandomImageService(int count, UnsplashImageFactory factory,
            CancellationTokenSourceFactory ctsFactory) : base(factory, ctsFactory)
        {
            Count = count;
        }

        public override async Task<IEnumerable<UnsplashImage>> GetImagesAsync()
        {
            var result = await _cloudService.GetRandomImagesAsync(Count, GetCancellationToken());
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