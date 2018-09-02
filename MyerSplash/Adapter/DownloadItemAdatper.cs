using MyerSplash.Model;
using System;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Xaml.Controls;

namespace MyerSplash.Adapter
{
    public class DownloadItemAdatper : AnimatedAdapter
    {
        public async override void OnChoosingItemContainer(ListViewBase sender, ChoosingItemContainerEventArgs args)
        {
            if ((args.Item is DownloadItem item))
            {
                await item.ImageItem.TryLoadBitmapAsync();
            }
        }
    }
}
