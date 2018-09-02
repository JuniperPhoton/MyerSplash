using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Ioc;
using JP.Utils.Data.Json;
using JP.Utils.UI;
using MyerSplash.Common;
using MyerSplash.Data;
using MyerSplash.ViewModel;
using MyerSplashShared.Data;
using MyerSplashShared.Service;
using MyerSplashShared.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Data.Json;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace MyerSplash.Model
{
    public class ImageItem : ModelBase
    {
        [IgnoreDataMember]
        public DownloadsViewModel DownloadsVM
        {
            get
            {
                return SimpleIoc.Default.GetInstance<DownloadsViewModel>();
            }
        }

        private UnsplashImage _image;
        public UnsplashImage Image
        {
            get
            {
                return _image;
            }
            set
            {
                if (_image != value)
                {
                    _image = value;
                    RaisePropertyChanged(() => Image);
                }
            }
        }

        private CachedBitmapSource _bitmapSource;
        [IgnoreDataMember]
        public CachedBitmapSource BitmapSource
        {
            get
            {
                return _bitmapSource;
            }
            set
            {
                if (_bitmapSource != value)
                {
                    _bitmapSource = value;
                    RaisePropertyChanged(() => BitmapSource);
                }
            }
        }

        [IgnoreDataMember]
        public Thickness NameThickness
        {
            get
            {
                if (Image.IsUnsplash)
                {
                    return new Thickness(0, 0, 0, 2);
                }
                else
                {
                    return new Thickness(0);
                }
            }
        }

        private SolidColorBrush _majorColor;
        [IgnoreDataMember]
        public SolidColorBrush MajorColor
        {
            get
            {
                return _majorColor;
            }
            set
            {
                if (_majorColor != value)
                {
                    _majorColor = value;
                    RaisePropertyChanged(() => MajorColor);
                    if (ColorConverter.IsLight(value.Color))
                    {
                        InfoForeColor = new SolidColorBrush(Colors.Black);
                        InfoForeColor = new SolidColorBrush(Colors.Black);
                        InfoForeColor = new SolidColorBrush(Colors.Black);
                        BtnForeColor = new SolidColorBrush(Colors.White);
                    }
                    else
                    {
                        InfoForeColor = new SolidColorBrush(Colors.White);
                        InfoForeColor = new SolidColorBrush(Colors.White);
                        InfoForeColor = new SolidColorBrush(Colors.White);
                        BtnForeColor = new SolidColorBrush(Colors.Black);
                    }
                }
            }
        }

        private SolidColorBrush _infoForeColor;
        [IgnoreDataMember]
        public SolidColorBrush InfoForeColor
        {
            get
            {
                return _infoForeColor;
            }
            set
            {
                if (_infoForeColor != value)
                {
                    _infoForeColor = value;
                    RaisePropertyChanged(() => InfoForeColor);
                }
            }
        }

        private SolidColorBrush _btnForeColor;
        [IgnoreDataMember]
        public SolidColorBrush BtnForeColor
        {
            get
            {
                return _btnForeColor;
            }
            set
            {
                if (_btnForeColor != value)
                {
                    _btnForeColor = value;
                    RaisePropertyChanged(() => BtnForeColor);
                }
            }
        }

        private SolidColorBrush _backColorBrush;
        [IgnoreDataMember]
        public SolidColorBrush BackColorBrush
        {
            get
            {
                return _backColorBrush;
            }
            set
            {
                if (_backColorBrush != value)
                {
                    _backColorBrush = value;
                    RaisePropertyChanged(() => BackColorBrush);
                }
            }
        }

        private RelayCommand _shareCommand;
        [IgnoreDataMember]
        public RelayCommand ShareCommand
        {
            get
            {
                if (_shareCommand != null) return _shareCommand;
                return _shareCommand = new RelayCommand(() =>
                {
                    ToggleShare();
                });
            }
        }

        private RelayCommand _navigateHomeCommand;
        [IgnoreDataMember]
        public RelayCommand NavigateHomeCommand
        {
            get
            {
                if (_navigateHomeCommand != null) return _navigateHomeCommand;
                return _navigateHomeCommand = new RelayCommand(async () =>
                {
                    if (!string.IsNullOrEmpty(Image.Owner.Links.HomePageUrl))
                    {
                        await Launcher.LaunchUriAsync(new Uri(Image.Owner.Links.HomePageUrl));
                    }
                });
            }
        }

        private RelayCommand _downloadCommand;
        [IgnoreDataMember]
        public RelayCommand DownloadCommand
        {
            get
            {
                if (_downloadCommand != null) return _downloadCommand;
                return _downloadCommand = new RelayCommand(() =>
                {
                    var downloaditem = new DownloadItem(this);
                    var task = downloaditem.DownloadFullImageAsync(JP.Utils.Network.CTSFactory.MakeCTS());
                    var task2 = DownloadsVM.AddDownloadingImageAsync(downloaditem);
                });
            }
        }

        public Visibility AuthorVisibility
        {
            get
            {
                if (Image.IsUnsplash)
                {
                    return Visibility.Visible;
                }
                else return Visibility.Collapsed;
            }
        }

        public Visibility DateTimeVisibility
        {
            get
            {
                return Image.IsInHighlightList ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public Visibility RecommendationVisibility
        {
            get
            {
                if (Image.IsUnsplash || Image.IsInHighlightList)
                {
                    return Visibility.Collapsed;
                }
                else return Visibility.Visible;
            }
        }

        public Visibility ExifThumbVisibility
        {
            get
            {
                if (Image.IsUnsplash)
                {
                    return Visibility.Visible;
                }
                else return Visibility.Collapsed;
            }
        }

        public string PhotoByText
        {
            get
            {
                if (Image.IsUnsplash)
                {
                    return "photo by";
                }
                else return "recommended by";
            }
        }

        [IgnoreDataMember]
        public StorageFile DownloadedFile { get; set; }

        public DownloadStatus DownloadStatus { get; set; } = DownloadStatus.Pending;

        public string ShareText => $"Share {Image.Owner.Name}'s amazing photo from MyerSplash app. {Image.Urls.Full}";

        public string OwnerString
        {
            get
            {
                return Image.Owner.Name;
            }
        }

        public string LocationString
        {
            get
            {
                if (Image.Location == null || Image.Location.City == null || Image.Location.Country == null)
                {
                    return "Unknown";
                }
                return $"{Image.Location.City}, {Image.Location.Country}";
            }
        }

        public string SizeString
        {
            get
            {
                return $"{Image.Width} x {Image.Height}";
            }
        }

        private ImageService _service = new ImageService(null, new UnsplashImageFactory(false),
            CancellationTokenSourceFactory.CreateDefault());

        public ImageItem()
        {
            BitmapSource = new CachedBitmapSource();
        }

        public ImageItem(UnsplashImage image)
        {
            Image = image;
            BitmapSource = new CachedBitmapSource();
        }

        public void Init()
        {
            BackColorBrush = new SolidColorBrush(Image.ColorValue.ToColor());
            MajorColor = new SolidColorBrush(Image.ColorValue.ToColor());
        }

        public string GetFileNameForDownloading()
        {
            var fileName = $"{Image.Owner.Name}  {Image.SimpleCreateTimeString}.jpg";
            var invalidChars = Path.GetInvalidFileNameChars();
            foreach (var c in invalidChars)
            {
                if (fileName.Contains(c))
                {
                    fileName = fileName.Replace(c.ToString(), "");
                }
            }
            return fileName;
        }

        public async Task SetDataRequestDataAsync(DataRequest request)
        {
            var requestData = request.Data;
            requestData.SetWebLink(new Uri(Image.Urls.Full));
            requestData.Properties.Title = $"Share a photo by {Image.Owner?.Name ?? "Unknown"}";
            requestData.Properties.ContentSourceWebLink = new Uri(Image.Urls.Full);
            requestData.Properties.ContentSourceApplicationLink = new Uri(Image.Urls.Full);

            requestData.SetText(ShareText);

            var file = await StorageFile.GetFileFromPathAsync(BitmapSource.LocalPath);
            if (file != null)
            {
                List<IStorageItem> imageItems = new List<IStorageItem>
                {
                    file
                };
                requestData.SetStorageItems(imageItems);

                var imageStreamRef = RandomAccessStreamReference.CreateFromFile(file);
                requestData.SetBitmap(imageStreamRef);
                requestData.Properties.Thumbnail = imageStreamRef;
            }
        }

        public async Task TryLoadBitmapAsync()
        {
            if (BitmapSource.Bitmap != null) return;
            var url = GetUrlFromSettings();

            if (string.IsNullOrEmpty(url)) return;

            var task = CheckAndGetDownloadedFileAsync();

            BitmapSource.ExpectedFileName = Image.ID + ".jpg";
            BitmapSource.RemoteUrl = url;
            await BitmapSource.LoadBitmapAsync();
        }

        public async Task CheckAndGetDownloadedFileAsync()
        {
            var name = GetFileNameForDownloading();
            var folder = await KnownFolders.PicturesLibrary.CreateFolderAsync("MyerSplash", CreationCollisionOption.OpenIfExists);
            if (folder != null)
            {
                if (await folder.TryGetItemAsync(name) is StorageFile file)
                {
                    var pro = await file.GetBasicPropertiesAsync();
                    if (pro.Size > 10)
                    {
                        this.DownloadStatus = DownloadStatus.Ok;
                        DownloadedFile = file;
                    }
                }
            }
        }

        public string GetUrlFromSettings()
        {
            var quality = App.AppSettings.LoadQuality;
            switch (quality)
            {
                case 0: return Image.Urls.Regular;
                case 1: return Image.Urls.Small;
                case 2: return Image.Urls.Thumb;
                default: return "";
            }
        }

        public string GetSaveImageUrlFromSettings()
        {
            var quality = App.AppSettings.SaveQuality;
            switch (quality)
            {
                case 0: return Image.Urls.Raw;
                case 1: return Image.Urls.Full;
                case 2: return Image.Urls.Regular;
                default: return "";
            }
        }

        public string GetDownloadLocationUrl()
        {
            return Image?.Links?.DownloadLocation;
        }

        public async Task GetExifInfoAsync()
        {
            var result = await _service.GetImageDetailAsync(Image.ID);
            if (result.IsRequestSuccessful)
            {
                JsonObject.TryParse(result.JsonSrc, out JsonObject json);
                if (json != null)
                {
                    var exifObject = JsonParser.GetJsonObjFromJsonObj(json, "exif");
                    if (exifObject != null)
                    {
                        Image.Exif = JsonConvert.DeserializeObject<ImageExif>(exifObject.ToString());
                        RaisePropertyChanged(() => SizeString);
                    }

                    var locationObj = JsonParser.GetJsonObjFromJsonObj(json, "location");
                    if (locationObj != null)
                    {
                        Image.Location = JsonConvert.DeserializeObject<ImageLocation>(locationObj.ToString());
                        RaisePropertyChanged(() => LocationString);
                    }
                }
            }
        }

        public void ToggleShare()
        {
            DataTransferManager.GetForCurrentView().DataRequested += DownloadItemTemplate_DataRequested;
            DataTransferManager.ShowShareUI();
        }

        private async void DownloadItemTemplate_DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            var deferral = args.Request.GetDeferral();
            sender.TargetApplicationChosen += (s, e) =>
            {
                deferral.Complete();
            };
            await SetDataRequestDataAsync(args.Request);
            deferral.Complete();
            DataTransferManager.GetForCurrentView().DataRequested -= DownloadItemTemplate_DataRequested;
        }
    }
}