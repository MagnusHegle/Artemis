﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Artemis.Core;
using Artemis.UI.Avalonia.Ninject.Factories;
using Artemis.UI.Avalonia.Shared;
using Artemis.UI.Avalonia.Shared.Services.Interfaces;
using ReactiveUI;

namespace Artemis.UI.Avalonia.Screens.Plugins.ViewModels
{
    public class PluginPrerequisitesInstallDialogViewModel : DialogViewModelBase<bool>
    {
        private PluginPrerequisiteViewModel? _activePrerequisite;
        private bool _canInstall;
        private bool _showFailed;
        private bool _showInstall = true;
        private bool _showIntro = true;
        private bool _showProgress;
        private CancellationTokenSource? _tokenSource;

        public PluginPrerequisitesInstallDialogViewModel(List<IPrerequisitesSubject> subjects, IPrerequisitesVmFactory prerequisitesVmFactory)
        {
            Prerequisites = new ObservableCollection<PluginPrerequisiteViewModel>();
            foreach (PluginPrerequisite prerequisite in subjects.SelectMany(prerequisitesSubject => prerequisitesSubject.Prerequisites))
                Prerequisites.Add(prerequisitesVmFactory.PluginPrerequisiteViewModel(prerequisite, false));

            CanInstall = false;
            Task.Run(() => CanInstall = Prerequisites.Any(p => !p.PluginPrerequisite.IsMet()));
        }

        public ObservableCollection<PluginPrerequisiteViewModel> Prerequisites { get; }

        public PluginPrerequisiteViewModel? ActivePrerequisite
        {
            get => _activePrerequisite;
            set => this.RaiseAndSetIfChanged(ref _activePrerequisite, value);
        }

        public bool ShowProgress
        {
            get => _showProgress;
            set => this.RaiseAndSetIfChanged(ref _showProgress, value);
        }

        public bool ShowIntro
        {
            get => _showIntro;
            set => this.RaiseAndSetIfChanged(ref _showIntro, value);
        }

        public bool ShowFailed
        {
            get => _showFailed;
            set => this.RaiseAndSetIfChanged(ref _showFailed, value);
        }

        public bool ShowInstall
        {
            get => _showInstall;
            set => this.RaiseAndSetIfChanged(ref _showInstall, value);
        }

        public bool CanInstall
        {
            get => _canInstall;
            set => this.RaiseAndSetIfChanged(ref _canInstall, value);
        }

        public async Task Install()
        {
            CanInstall = false;
            ShowFailed = false;
            ShowIntro = false;
            ShowProgress = true;

            _tokenSource = new CancellationTokenSource();

            try
            {
                foreach (PluginPrerequisiteViewModel pluginPrerequisiteViewModel in Prerequisites)
                {
                    pluginPrerequisiteViewModel.IsMet = pluginPrerequisiteViewModel.PluginPrerequisite.IsMet();
                    if (pluginPrerequisiteViewModel.IsMet)
                        continue;

                    ActivePrerequisite = pluginPrerequisiteViewModel;
                    await ActivePrerequisite.Install(_tokenSource.Token);

                    if (!ActivePrerequisite.IsMet)
                    {
                        CanInstall = true;
                        ShowFailed = true;
                        ShowProgress = false;
                        return;
                    }

                    // Wait after the task finished for the user to process what happened
                    if (pluginPrerequisiteViewModel != Prerequisites.Last())
                        await Task.Delay(1000);
                }

                ShowInstall = false;
            }
            catch (OperationCanceledException)
            {
                // ignored
            }
            finally
            {
                _tokenSource.Dispose();
                _tokenSource = null;
            }
        }

        public void Accept()
        {
            Close(true);
        }

        public static async Task<bool> Show(IWindowService windowService, List<IPrerequisitesSubject> subjects)
        {
            return await windowService.ShowDialogAsync<PluginPrerequisitesInstallDialogViewModel, bool>(("subjects", subjects));
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _tokenSource?.Cancel();
                _tokenSource?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}