﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Artemis.Core;
using Artemis.Core.Services;
using Artemis.UI.Shared;
using Artemis.UI.Shared.Services;
using Artemis.UI.Shared.Services.Builders;
using Avalonia.Controls;
using ReactiveUI;
using RGB.NET.Layout;
using SkiaSharp;

namespace Artemis.UI.Screens.Device;

public class DeviceLayoutTabViewModel : ActivatableViewModelBase
{
    private readonly IWindowService _windowService;
    private readonly INotificationService _notificationService;
    private readonly IDeviceService _deviceService;

    public DeviceLayoutTabViewModel(IWindowService windowService, INotificationService notificationService, IDeviceService deviceService, ArtemisDevice device)
    {
        _windowService = windowService;
        _notificationService = notificationService;
        _deviceService = deviceService;

        Device = device;
        DisplayName = "Layout";
        DefaultLayoutPath = Device.DeviceProvider.LoadLayout(Device).FilePath;

        this.WhenActivated(d =>
        {
            Device.PropertyChanged += DeviceOnPropertyChanged;
            Disposable.Create(() => Device.PropertyChanged -= DeviceOnPropertyChanged).DisposeWith(d);
        });
    }

    public ArtemisDevice Device { get; }

    public string DefaultLayoutPath { get; }

    public string? ImagePath => Device.Layout?.Image?.LocalPath;

    public string? CustomLayoutPath => Device.CustomLayoutPath;

    public bool HasCustomLayout => Device.CustomLayoutPath != null;

    public void ClearCustomLayout()
    {
        Device.CustomLayoutPath = null;
        _notificationService.CreateNotification()
            .WithMessage("Cleared imported layout.")
            .WithSeverity(NotificationSeverity.Informational);
    }

    public async Task BrowseCustomLayout()
    {
        string[]? files = await _windowService.CreateOpenFileDialog()
            .WithTitle("Select device layout file")
            .HavingFilter(f => f.WithName("Layout files").WithExtension("xml"))
            .ShowAsync();

        if (files?.Length > 0)
        {
            Device.CustomLayoutPath = files[0];
            _notificationService.CreateNotification()
                .WithTitle("Imported layout")
                .WithMessage($"File loaded from {files[0]}")
                .WithSeverity(NotificationSeverity.Informational);
        }
    }

    public async Task ExportLayout()
    {
        string fileName = Device.DeviceProvider.GetDeviceLayoutName(Device);
        string layoutDir = Constants.LayoutsFolder;
        string filePath = Path.Combine(
            layoutDir,
            Device.RgbDevice.DeviceInfo.Manufacturer,
            Device.DeviceType.ToString(),
            fileName
        );
        if (!Directory.Exists(filePath))
            Directory.CreateDirectory(filePath);

        string? result = await _windowService.CreateSaveFileDialog()
            .HavingFilter(f => f.WithExtension("xml").WithName("Artemis layout"))
            .WithDirectory(filePath)
            .WithInitialFileName(fileName)
            .ShowAsync();

        if (result == null)
            return;

        ArtemisLayout? layout = Device.Layout;
        if (layout?.IsValid == true)
        {
            string path = layout.FilePath;
            File.Copy(path, result, true);
        }
        else
        {
            List<LedLayout> ledLayouts = Device.Leds.Select(x => new LedLayout()
            {
                Id = x.RgbLed.Id.ToString(),
                DescriptiveX = x.Rectangle.Left.ToString(),
                DescriptiveY = x.Rectangle.Top.ToString(),
                DescriptiveWidth = $"{x.Rectangle.Width}mm",
                DescriptiveHeight = $"{x.Rectangle.Height}mm",
            }).ToList();

            DeviceLayout emptyLayout = new()
            {
                Author = "Artemis",
                Type = Device.DeviceType,
                Vendor = Device.RgbDevice.DeviceInfo.Manufacturer,
                Model = Device.RgbDevice.DeviceInfo.Model,
                Width = Device.Rectangle.Width,
                Height = Device.Rectangle.Height,
                InternalLeds = ledLayouts,
            };

            XmlSerializer serializer = new(typeof(DeviceLayout));
            await using StreamWriter writer = new(result);
            serializer.Serialize(writer, emptyLayout);
        }

        _notificationService.CreateNotification()
            .WithMessage("Layout exported")
            .WithTimeout(TimeSpan.FromSeconds(5))
            .WithSeverity(NotificationSeverity.Success)
            .Show();
    }

    public void ShowCopiedNotification()
    {
        _notificationService.CreateNotification()
            .WithTitle("Copied!")
            .WithSeverity(NotificationSeverity.Informational)
            .Show();
    }
    
    private void DeviceOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(Device.CustomLayoutPath) or nameof(Device.DisableDefaultLayout))
        {
            Task.Run(() =>
            {
                _deviceService.ApplyDeviceLayout(Device, Device.GetBestDeviceLayout());
                _deviceService.SaveDevice(Device);
            });
            
            this.RaisePropertyChanged(nameof(CustomLayoutPath));
            this.RaisePropertyChanged(nameof(HasCustomLayout));
        }
    }
}