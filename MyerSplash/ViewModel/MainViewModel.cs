using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using JP.Utils.Framework;
using Microsoft.QueryStringDotNET;
using MyerSplash.Common;
using MyerSplash.Data;
using MyerSplash.Model;
using MyerSplash.ViewModel.DataViewModel;
using MyerSplashShared.API;
using MyerSplashShared.Service;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Windows.Storage;
using Windows.System;
using Windows.UI.Xaml;
using System.Collections.ObjectModel;
using System.Diagnostics;
using MyerSplashShared.Utils;

namespace MyerSplash.ViewModel
{
    public class MainViewModel : ViewModelBase, INavigable
    {
        private const int NEW_INDEX = 0;
        private const int FEATURED_INDEX = 1;
        private const int RANDOM_INDEX = 2;
        private const int HIGHLIGHTS_INDEX = 3;

        public const string NEW_NAME = "NEW";
        public const string FEATURED_NAME = "FEATURED";
        public const string RANDOM_NAME = "RANDOM";
        public const string HIGHLIGHTS_NAME = "HIGHLIGHTS";

        public Dictionary<int, string> INDEX_TO_NAME = new Dictionary<int, string>()
        {
            { NEW_INDEX,NEW_NAME },
            { FEATURED_INDEX,FEATURED_NAME },
            { RANDOM_INDEX,RANDOM_NAME },
            { HIGHLIGHTS_INDEX,HIGHLIGHTS_NAME }
        };

        public event EventHandler<int> AboutToUpdateSelectedIndex;
        public event EventHandler DataUpdated;

        private string DefaultTitleName
        {
            get
            {
                var key = AppSettings.Instance.DefaultCategory;
                if (INDEX_TO_NAME.ContainsKey(key))
                {
                    return INDEX_TO_NAME[AppSettings.Instance.DefaultCategory];
                }
                else
                {
                    return "MyerSplash";
                }
            }
        }

        private Dictionary<int, ImageDataViewModel> _vms = new Dictionary<int, ImageDataViewModel>();

        private ImageDataViewModel _dataVM;
        public ImageDataViewModel DataVM
        {
            get
            {
                return _dataVM;
            }
            set
            {
                if (_dataVM != value)
                {
                    _dataVM = value;
                    RaisePropertyChanged(() => DataVM);
                }
            }
        }

        public bool IsInView { get; set; }

        private ObservableCollection<string> _tabs;
        public ObservableCollection<string> Tabs
        {
            get
            {
                return _tabs;
            }
            set
            {
                if (_tabs != value)
                {
                    _tabs = value;
                    RaisePropertyChanged(() => Tabs);
                }
            }
        }

        #region Search

        private bool _showSearchBar;
        public bool ShowSearchBar
        {
            get
            {
                return _showSearchBar;
            }
            set
            {
                if (_showSearchBar != value)
                {
                    _showSearchBar = value;
                    RaisePropertyChanged(() => ShowSearchBar);
                }
            }
        }

        private string _searchKeyword;
        public string SearchKeyword
        {
            get
            {
                return _searchKeyword;
            }
            set
            {
                if (_searchKeyword != value)
                {
                    _searchKeyword = value;
                    RaisePropertyChanged(() => SearchKeyword);
                }
            }
        }

        private RelayCommand _searchCommand;
        public RelayCommand SearchCommand
        {
            get
            {
                if (_searchCommand != null) return _searchCommand;
                return _searchCommand = new RelayCommand(() =>
                  {
                      ShowSearchBar = true;
                      NavigationService.AddOperation(() =>
                          {
                              if (ShowSearchBar)
                              {
                                  ShowSearchBar = false;
                                  return true;
                              }
                              else return false;
                          });
                  });
            }
        }

        private RelayCommand _hideSearchCommand;
        public RelayCommand HideSearchCommand
        {
            get
            {
                if (_hideSearchCommand != null) return _hideSearchCommand;
                return _hideSearchCommand = new RelayCommand(() =>
                  {
                      ShowSearchBar = false;
                  });
            }
        }

        private RelayCommand _beginSearchCommand;
        public RelayCommand BeginSearchCommand
        {
            get
            {
                if (_beginSearchCommand != null) return _beginSearchCommand;
                return _beginSearchCommand = new RelayCommand(async () =>
                  {
                      if (ShowSearchBar)
                      {
                          ShowSearchBar = false;
                          await SearchByKeywordAsync();
                          SearchKeyword = "";
                      }
                  });
            }
        }

        #endregion Search

        private RelayCommand _refreshCommand;
        public RelayCommand RefreshCommand
        {
            get
            {
                if (_refreshCommand != null) return _refreshCommand;
                return _refreshCommand = new RelayCommand(async () =>
                  {
                      await RefreshListAsync();
                  });
            }
        }

        private RelayCommand _retryCommand;
        public RelayCommand RetryCommand
        {
            get
            {
                if (_retryCommand != null) return _retryCommand;
                return _retryCommand = new RelayCommand(async () =>
                  {
                      FooterLoadingVisibility = Visibility.Visible;
                      FooterReloadVisibility = Visibility.Collapsed;
                      await DataVM.RetryAsync();
                  });
            }
        }

        private bool _isRefreshing;
        public bool IsRefreshing
        {
            get
            {
                return _isRefreshing;
            }
            set
            {
                if (_isRefreshing != value)
                {
                    _isRefreshing = value;
                    RaisePropertyChanged(() => IsRefreshing);
                }
            }
        }

        private Visibility _footerLoadingVisibility;
        public Visibility FooterLoadingVisibility
        {
            get
            {
                return _footerLoadingVisibility;
            }
            set
            {
                if (_footerLoadingVisibility != value)
                {
                    _footerLoadingVisibility = value;
                    RaisePropertyChanged(() => FooterLoadingVisibility);
                }
            }
        }

        private Visibility _endVisiblity;
        public Visibility EndVisibility
        {
            get
            {
                return _endVisiblity;
            }
            set
            {
                if (_endVisiblity != value)
                {
                    _endVisiblity = value;
                    RaisePropertyChanged(() => EndVisibility);
                }
            }
        }

        private Visibility _noItemHintVisibility;
        public Visibility NoItemHintVisibility
        {
            get
            {
                return _noItemHintVisibility;
            }
            set
            {
                if (_noItemHintVisibility != value)
                {
                    _noItemHintVisibility = value;
                    RaisePropertyChanged(() => NoItemHintVisibility);
                }
            }
        }

        private Visibility _noNetworkHintVisibility;
        public Visibility NoNetworkHintVisibility
        {
            get
            {
                return _noNetworkHintVisibility;
            }
            set
            {
                if (_noNetworkHintVisibility != value)
                {
                    _noNetworkHintVisibility = value;
                    RaisePropertyChanged(() => NoNetworkHintVisibility);
                }
            }
        }

        private Visibility _footerReloadVisibility;
        public Visibility FooterReloadVisibility
        {
            get
            {
                return _footerReloadVisibility;
            }
            set
            {
                if (_footerReloadVisibility != value)
                {
                    _footerReloadVisibility = value;
                    RaisePropertyChanged(() => FooterReloadVisibility);
                }
            }
        }

        private RelayCommand _presentSettingsCommand;
        public RelayCommand PresentSettingsCommand
        {
            get
            {
                if (_presentSettingsCommand != null) return _presentSettingsCommand;
                return _presentSettingsCommand = new RelayCommand(() =>
                  {
                      SettingsPagePresented = true;
                      NavigationService.AddOperation(() =>
                          {
                              if (SettingsPagePresented)
                              {
                                  SettingsPagePresented = false;
                                  return true;
                              }
                              return false;
                          });
                  });
            }
        }

        private bool _aboutPagePresented;
        public bool AboutPagePresented
        {
            get
            {
                return _aboutPagePresented;
            }
            set
            {
                if (_aboutPagePresented != value)
                {
                    _aboutPagePresented = value;
                    RaisePropertyChanged(() => AboutPagePresented);
                }
            }
        }

        private bool _downloadsPagePresented;
        public bool DownloadsPagePresented
        {
            get
            {
                return _downloadsPagePresented;
            }
            set
            {
                if (_downloadsPagePresented != value)
                {
                    _downloadsPagePresented = value;
                    RaisePropertyChanged(() => DownloadsPagePresented);
                }
            }
        }

        private bool _settingsPagePresented;
        public bool SettingsPagePresented
        {
            get
            {
                return _settingsPagePresented;
            }
            set
            {
                if (_settingsPagePresented != value)
                {
                    _settingsPagePresented = value;
                    RaisePropertyChanged(() => SettingsPagePresented);
                }
            }
        }

        private RelayCommand _presentDownloadsCommand;
        public RelayCommand PresentDownloadsCommand
        {
            get
            {
                if (_presentDownloadsCommand != null) return _presentDownloadsCommand;
                return _presentDownloadsCommand = new RelayCommand(() =>
                  {
                      DownloadsPagePresented = !DownloadsPagePresented;

                      if (DownloadsPagePresented)
                      {
                          NavigationService.AddOperation(() =>
                          {
                              if (DownloadsPagePresented)
                              {
                                  DownloadsPagePresented = false;
                                  return true;
                              }
                              return false;
                          });
                      }
                  });
            }
        }

        private RelayCommand _presentAboutCommand;
        public RelayCommand PresentAboutCommand
        {
            get
            {
                if (_presentAboutCommand != null) return _presentAboutCommand;
                return _presentAboutCommand = new RelayCommand(() =>
                  {
                      AboutPagePresented = true;
                      NavigationService.AddOperation(() =>
                          {
                              if (AboutPagePresented)
                              {
                                  AboutPagePresented = false;
                                  return true;
                              }
                              return false;
                          });
                  });
            }
        }

        private int _selectedIndex;
        public int SelectedIndex
        {
            get
            {
                return _selectedIndex;
            }
            set
            {
                if (_selectedIndex != value)
                {
                    var lastValue = value;

                    _selectedIndex = value;

                    AboutToUpdateSelectedIndex?.Invoke(this, lastValue);

                    RaisePropertyChanged(() => SelectedIndex);

                    if (value >= 0)
                    {
                        if (lastValue != -1)
                        {
                            DataVM = CreateOrCacheDataVm(value);
                            if (DataVM != null && DataVM.DataList.Count == 0)
                            {
                                var task = RefreshListAsync();
                            }
                            DataUpdated?.Invoke(this, null);
                        }
                    }
                }
            }
        }

        private UnsplashImageFactory _normalFactory;
        public UnsplashImageFactory NormalFactory
        {
            get
            {
                return _normalFactory ?? (_normalFactory = new UnsplashImageFactory(false));
            }
        }

        private UnsplashImageFactory _featuredFactory;
        public UnsplashImageFactory FeaturedFactory
        {
            get
            {
                return _featuredFactory ?? (_featuredFactory = new UnsplashImageFactory(true));
            }
        }

        public bool IsFirstActived { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public MainViewModel()
        {
            FooterLoadingVisibility = Visibility.Collapsed;
            NoItemHintVisibility = Visibility.Collapsed;
            NoNetworkHintVisibility = Visibility.Collapsed;
            FooterReloadVisibility = Visibility.Collapsed;
            EndVisibility = Visibility.Collapsed;
            IsRefreshing = true;
            DownloadsPagePresented = false;

            SelectedIndex = -1;
            Tabs = new ObservableCollection<string>();

            DataVM = new ImageDataViewModel(this,
                new ImageService(Request.GetNewImages, NormalFactory));
        }

        private ImageDataViewModel CreateOrCacheDataVm(int index)
        {
            ImageDataViewModel vm = null;
            if (_vms.ContainsKey(index))
            {
                vm = _vms[index];
            }

            if (vm == null)
            {
                switch (index)
                {
                    case NEW_INDEX:
                        vm = new ImageDataViewModel(this, new ImageService(Request.GetNewImages, NormalFactory));
                        break;
                    case FEATURED_INDEX:
                        vm = new ImageDataViewModel(this, new ImageService(Request.GetFeaturedImages, FeaturedFactory));
                        break;
                    case RANDOM_INDEX:
                        vm = new RandomImagesDataViewModel(this, new RandomImageService(NormalFactory));
                        break;
                    case HIGHLIGHTS_INDEX:
                        vm = new ImageDataViewModel(this, new HighlightImageService(NormalFactory));
                        break;
                }

                if (vm != null)
                {
                    _vms[index] = vm;
                }
            }

            return vm;
        }

        private async Task SearchByKeywordAsync()
        {
            var searchService = new SearchImageService(NormalFactory, SearchKeyword);

            if (Tabs.Count != INDEX_TO_NAME.Count && Tabs.Count > 0)
            {
                Tabs.RemoveAt(Tabs.Count - 1);
            }
            Tabs.Add(SearchKeyword.ToUpper());

            SelectedIndex = Tabs.Count - 1;
            DataVM = new SearchResultViewModel(this, searchService);

            _vms[SelectedIndex] = DataVM;

            await RefreshListAsync();
        }

        private async Task RefreshListAsync()
        {
            IsRefreshing = true;
            await DataVM.RefreshAsync();

            if (SelectedIndex == NEW_INDEX && AppSettings.Instance.EnableTodayRecommendation)
            {
                InsertTodayHighlight();
            }

            IsRefreshing = false;

            await UpdateLiveTileAsync();
        }

        public void RemoveTodayHighlight()
        {
            var vm = _vms[NEW_INDEX];
            if (vm != null)
            {
                var first = vm.DataList.FirstOrDefault();
                if (first != null && !first.Image.IsUnsplash)
                {
                    vm.DataList.Remove(first);
                }
            }
        }

        public void InsertTodayHighlight()
        {
            var vm = _vms[NEW_INDEX];
            if (vm != null)
            {
                var date = DateTime.Now.ToString("yyyyMMdd");
                var first = vm.DataList.FirstOrDefault();
                if (first != null && first.Image.ID != date)
                {
                    RemoveTodayHighlight();

                    var imageItem = new ImageItem(UnsplashImageFactory.CreateTodayHighlightImage());
                    imageItem.Init();

                    vm.DataList.Insert(0, imageItem);
                }
            }
        }

        public void Activate(object param)
        {
            var task = HandleLaunchArg(param as string);
        }

        private async Task UpdateLiveTileAsync()
        {
            if (App.AppSettings.EnableTile)
            {
                Debug.WriteLine("About to update tile.");
                await LiveTileUpdater.UpdateImagesTileAsync();
            }
        }

        private async Task HandleLaunchArg(string arg)
        {
            if (arg == Value.SEARCH)
            {
                ShowSearchBar = true;
            }
            else if (arg == Value.DOWNLOADS)
            {
                DownloadsPagePresented = true;
            }
            else
            {
                var queryStr = QueryString.Parse(arg);
                var action = queryStr[Key.ACTION_KEY];
                if (!queryStr.Contains(Key.FILE_PATH_KEY))
                {
                    return;
                }
                var filePath = queryStr[Key.FILE_PATH_KEY];
                if (filePath != null)
                {
                    switch (action)
                    {
                        case Value.SET_AS:
                            await WallpaperSettingHelper.SetAsBackgroundAsync(await StorageFile.GetFileFromPathAsync(filePath));
                            break;
                        case Value.VIEW:
                            await Launcher.LaunchFileAsync(await StorageFile.GetFileFromPathAsync(filePath));
                            break;
                    }
                }
            }
        }

        public void Deactivate(object param)
        {
        }

        public void OnLoaded()
        {
            SelectedIndex = NEW_INDEX;
            INDEX_TO_NAME.Select(s => s.Value).ToList().ForEach(s =>
            {
                Tabs.Add(s);
            });
        }
    }
}