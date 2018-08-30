using JP.Utils.Debug;
using MyerSplash.Data;
using MyerSplash.Model;
using MyerSplashCustomControl;
using MyerSplashShared.API;
using MyerSplashShared.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace MyerSplash.ViewModel.DataViewModel
{
    public class ImageDataViewModel : DataViewModelBase<ImageItem>
    {
        protected MainViewModel _mainViewModel;
        protected ImageServiceBase _imageService;

        public ImageDataViewModel(MainViewModel viewModel, ImageServiceBase service)
        {
            _mainViewModel = viewModel;
            _imageService = service;
        }

        public void Cancel()
        {
            _imageService?.Cancel();
        }

        protected override void ClickItem(ImageItem item)
        {
        }

        protected IEnumerable<ImageItem> CreateImageItems(IEnumerable<UnsplashImage> images)
        {
            var list = new List<ImageItem>();
            foreach (var i in images)
            {
                list.Add(new ImageItem(i));
            }
            return list;
        }

        protected void UpdateHintVisibility(IEnumerable<ImageItem> list)
        {
            _mainViewModel.FooterLoadingVisibility = Visibility.Collapsed;
            _mainViewModel.FooterReloadVisibility = Visibility.Collapsed;
            _mainViewModel.NoNetworkHintVisibility = Visibility.Collapsed;
            _mainViewModel.EndVisibility = Visibility.Collapsed;

            // No items at all
            if (DataList.Count == 0)
            {
                if (list.Count() == 0)
                {
                    _mainViewModel.NoItemHintVisibility = Visibility.Visible;
                }
            }
            else
            {
                _mainViewModel.NoItemHintVisibility = Visibility.Collapsed;

                if (list.Count() == 0)
                {
                    _mainViewModel.EndVisibility = Visibility.Visible;
                }
            }
        }

        protected async override Task<IEnumerable<ImageItem>> GetList(int pageIndex)
        {
            try
            {
                if (pageIndex >= 2)
                {
                    var task = RunOnUiThread(() =>
                      {
                          _mainViewModel.FooterLoadingVisibility = Visibility.Visible;
                          _mainViewModel.EndVisibility = Visibility.Collapsed;
                          _mainViewModel.NoItemHintVisibility = Visibility.Collapsed;
                      });
                }

                var result = await RequestAsync(pageIndex);

                await RunOnUiThread(() =>
                {
                    UpdateHintVisibility(result);
                });

                return result;
            }
            catch (Exception e2)
            {
                var task = Logger.LogAsync(e2);
                HandleFailed(e2);
                return new List<ImageItem>();
            }
        }

        private void HandleFailed(Exception e)
        {
            var task = RunOnUiThread(() =>
             {
                 _mainViewModel.FooterLoadingVisibility = Visibility.Collapsed;
                 _mainViewModel.FooterReloadVisibility = Visibility.Collapsed;

                 _mainViewModel.IsRefreshing = false;

                 if (_mainViewModel.DataVM.DataList?.Count == 0)
                 {
                     _mainViewModel.NoNetworkHintVisibility = Visibility.Visible;
                 }
                 else
                 {
                     _mainViewModel.NoNetworkHintVisibility = Visibility.Collapsed;
                     _mainViewModel.FooterReloadVisibility = Visibility.Visible;
                 }

                 ToastService.SendToast(e.Message);
             });
        }

        protected override void LoadMoreItemCompleted(IEnumerable<ImageItem> list, int pagingIndex)
        {
            foreach (var item in list)
            {
                item.Init();
            }
        }

        protected async virtual Task<IEnumerable<ImageItem>> RequestAsync(int pageIndex)
        {
            try
            {
                _imageService.Page = pageIndex;
                var result = await _imageService.GetImagesAsync();
                if (result != null)
                {
                    return CreateImageItems(result);
                }
                else throw new APIException("Request failed");
            }
            catch (TaskCanceledException)
            {
                throw new APIException("Request timeout");
            }
        }
    }
}