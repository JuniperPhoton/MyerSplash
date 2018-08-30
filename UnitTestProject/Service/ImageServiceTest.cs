using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using MyerSplash.Data;
using MyerSplashShared.API;
using MyerSplashShared.Data;
using MyerSplashShared.Service;
using System.Linq;
using System.Threading.Tasks;

namespace UnitTestProject.Service
{
    [TestClass]
    public class ImageServiceTest
    {
        private ImageServiceBase _newImageService = new ImageService(Request.GetNewImages,
            new UnsplashImageFactory(false), CancellationTokenSourceFactory.CreateDefault());

        private ImageServiceBase _featuredImageService = new ImageService(Request.GetFeaturedImages,
            new UnsplashImageFactory(true), CancellationTokenSourceFactory.CreateDefault());

        private ImageServiceBase _randomImageService = new RandomImageService(
            new UnsplashImageFactory(false), CancellationTokenSourceFactory.CreateDefault());

        [TestMethod]
        public async Task TestGetNewImages()
        {
            var result = await _newImageService.GetImagesAsync();
            Assert.IsTrue(result?.Count() > 0);
        }

        [TestMethod]
        public async Task TestGetFeatureImages()
        {
            var result = await _featuredImageService.GetImagesAsync();
            Assert.IsTrue(result?.Count() > 0);
        }

        [TestMethod]
        public async Task TestGetRandomImages()
        {
            var result = await _randomImageService.GetImagesAsync();
            Assert.IsTrue(result?.Count() > 0);
        }

        [TestMethod]
        public async Task TestSearchHasResultImages()
        {
            var service = new SearchImageService(new UnsplashImageFactory(false),
                CancellationTokenSourceFactory.CreateDefault(), "sea");
            var result = await service.GetImagesAsync();
            Assert.IsTrue(result?.Count() > 0);
        }

        [TestMethod]
        public async Task TestSearchHasNoResultImages()
        {
            var service = new SearchImageService(new UnsplashImageFactory(false),
                CancellationTokenSourceFactory.CreateDefault(), "dfafefsdfasfsdf");
            var result = await service.GetImagesAsync();
            Assert.IsTrue(result?.Count() == 0);
        }
    }
}