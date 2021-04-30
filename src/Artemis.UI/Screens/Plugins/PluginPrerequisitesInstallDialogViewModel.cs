﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Artemis.Core;
using Artemis.UI.Exceptions;
using Artemis.UI.Ninject.Factories;
using Artemis.UI.Shared.Services;
using MaterialDesignThemes.Wpf;
using Stylet;

namespace Artemis.UI.Screens.Plugins
{
    public class PluginPrerequisitesInstallDialogViewModel : DialogViewModelBase
    {
        private readonly IDialogService _dialogService;
        private PluginPrerequisiteViewModel _activePrerequisite;
        private bool _canInstall;
        private bool _isFinished;
        private CancellationTokenSource _tokenSource;

        public PluginPrerequisitesInstallDialogViewModel(IPrerequisitesSubject subject, IPrerequisitesVmFactory prerequisitesVmFactory, IDialogService dialogService)
        {
            _dialogService = dialogService;
            // Constructor overloading doesn't work very well with Kernel.Get<T> :(
            if (subject is PluginInfo plugin)
            {
                PluginInfo = plugin;
                Prerequisites = new BindableCollection<PluginPrerequisiteViewModel>(plugin.Prerequisites.Select(p => prerequisitesVmFactory.PluginPrerequisiteViewModel(p, false)));
            }
            else if (subject is PluginFeatureInfo feature)
            {
                FeatureInfo = feature;
                Prerequisites = new BindableCollection<PluginPrerequisiteViewModel>(feature.Prerequisites.Select(p => prerequisitesVmFactory.PluginPrerequisiteViewModel(p, false)));
            }
            else
                throw new ArtemisUIException($"Expected plugin or feature to be passed to {nameof(PluginPrerequisitesInstallDialogViewModel)}");

            foreach (PluginPrerequisiteViewModel pluginPrerequisiteViewModel in Prerequisites)
                pluginPrerequisiteViewModel.ConductWith(this);
        }


        public PluginInfo PluginInfo { get; }
        public PluginFeatureInfo FeatureInfo { get; }
        public BindableCollection<PluginPrerequisiteViewModel> Prerequisites { get; }

        public PluginPrerequisiteViewModel ActivePrerequisite
        {
            get => _activePrerequisite;
            set => SetAndNotify(ref _activePrerequisite, value);
        }

        public bool CanInstall
        {
            get => _canInstall;
            set => SetAndNotify(ref _canInstall, value);
        }

        public bool IsFinished
        {
            get => _isFinished;
            set => SetAndNotify(ref _isFinished, value);
        }

        public bool IsSubjectPlugin => PluginInfo != null;
        public bool IsSubjectFeature => FeatureInfo != null;

        #region Overrides of DialogViewModelBase

        /// <inheritdoc />
        public override void OnDialogClosed(object sender, DialogClosingEventArgs e)
        {
            _tokenSource?.Cancel();
            base.OnDialogClosed(sender, e);
        }

        #endregion

        public async void Install()
        {
            CanInstall = false;
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

                    // Wait after the task finished for the user to process what happened
                    if (pluginPrerequisiteViewModel != Prerequisites.Last())
                        await Task.Delay(1000);
                }

                if (Prerequisites.All(p => p.IsMet))
                {
                    IsFinished = true;
                    return;
                }

                // This shouldn't be happening and the experience isn't very nice for the user (too lazy to make a nice UI for such an edge case)
                // but at least give some feedback
                Session?.Close(false);
                await _dialogService.ShowConfirmDialog(
                    "Plugin prerequisites",
                    "All prerequisites are installed but some still aren't met. \r\nPlease try again or contact the plugin creator.",
                    "Confirm",
                    ""
                );
                await _dialogService.ShowDialog<PluginPrerequisitesInstallDialogViewModel>(new Dictionary<string, object> {{"subject", PluginInfo}});
            }
            catch (OperationCanceledException)
            {
                // ignored
            }
            finally
            {
                CanInstall = true;
                _tokenSource.Dispose();
                _tokenSource = null;
            }
        }

        public void Accept()
        {
            Session?.Close(true);
        }

        #region Overrides of Screen

        /// <inheritdoc />
        protected override void OnInitialActivate()
        {
            CanInstall = false;
            if (PluginInfo != null)
                Task.Run(() => CanInstall = !PluginInfo.ArePrerequisitesMet());
            else
                Task.Run(() => CanInstall = !FeatureInfo.ArePrerequisitesMet());

            base.OnInitialActivate();
        }

        #endregion
    }
}