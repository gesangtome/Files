using CommunityToolkit.Mvvm.DependencyInjection;
using Files.Shared;
using Files.Shared.Cloud;
using Files.App.DataModels.NavigationControlItems;
using Files.App.Helpers;
using CommunityToolkit.WinUI;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;

namespace Files.App.Filesystem.Cloud
{
    public class CloudDrivesManager
    {
        private readonly ILogger logger = Ioc.Default.GetService<ILogger>();
        private readonly ICloudDetector detector = Ioc.Default.GetService<ICloudDetector>();

        public EventHandler<NotifyCollectionChangedEventArgs> DataChanged;

        private readonly List<DriveItem> drives = new();
        public IReadOnlyList<DriveItem> Drives
        {
            get
            {
                lock (drives)
                {
                    return drives.ToList().AsReadOnly();
                }
            }
        }

        public async Task UpdateDrivesAsync()
        {
            var providers = await detector?.DetectCloudProvidersAsync();
            if (providers is null)
            {
                return;
            }

            foreach (var provider in providers)
            {
                logger?.Info($"Adding cloud provider \"{provider.Name}\" mapped to {provider.SyncFolder}");
                var cloudProviderItem = new DriveItem
                {
                    Text = provider.Name,
                    Path = provider.SyncFolder,
                    Type = DriveType.CloudDrive,
                };
                cloudProviderItem.MenuOptions = new ContextMenuOptions
                {
                    IsLocationItem = true,
                    ShowEjectDevice = cloudProviderItem.IsRemovable,
                    ShowShellItems = true,
                    ShowProperties = true,
                };
                var iconData = provider.IconData ?? await FileThumbnailHelper.LoadIconWithoutOverlayAsync(provider.SyncFolder, 24);
                if (iconData is not null)
                {
                    cloudProviderItem.IconData = iconData;
                    await App.Window.DispatcherQueue
                        .EnqueueAsync(async () => cloudProviderItem.Icon = await iconData.ToBitmapAsync());
                }

                lock (drives)
                {
                    if (drives.Any(x => x.Path == cloudProviderItem.Path))
                    {
                        continue;
                    }
                    drives.Add(cloudProviderItem);
                }

                DataChanged?.Invoke(SectionType.CloudDrives,
                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, cloudProviderItem));
            }
        }
    }
}