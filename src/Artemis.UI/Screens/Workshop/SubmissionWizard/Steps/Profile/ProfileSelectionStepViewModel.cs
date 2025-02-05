using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Artemis.Core;
using Artemis.Core.Services;
using Artemis.UI.Extensions;
using Artemis.UI.Screens.Workshop.Profile;
using Material.Icons;
using ReactiveUI;
using SkiaSharp;

namespace Artemis.UI.Screens.Workshop.SubmissionWizard.Steps.Profile;

public class ProfileSelectionStepViewModel : SubmissionViewModel
{
    private readonly IProfileService _profileService;
    private ProfileConfiguration? _selectedProfile;

    /// <inheritdoc />
    public ProfileSelectionStepViewModel(IProfileService profileService, ProfilePreviewViewModel profilePreviewViewModel)
    {
        _profileService = profileService;

        // Use copies of the profiles, the originals are used by the core and could be disposed
        Profiles = new ObservableCollection<ProfileConfiguration>(_profileService.ProfileConfigurations.Select(_profileService.CloneProfileConfiguration));
        foreach (ProfileConfiguration profileConfiguration in Profiles)
            _profileService.LoadProfileConfigurationIcon(profileConfiguration);

        ProfilePreview = profilePreviewViewModel;

        GoBack = ReactiveCommand.Create(() => State.ChangeScreen<EntryTypeStepViewModel>());
        Continue = ReactiveCommand.Create(ExecuteContinue, this.WhenAnyValue(vm => vm.SelectedProfile).Select(p => p != null));

        this.WhenAnyValue(vm => vm.SelectedProfile).Subscribe(p => Update(p));
        this.WhenActivated((CompositeDisposable _) =>
        {
            ShowGoBack = State.EntryId == null;
            if (State.EntrySource is ProfileConfiguration profileConfiguration)
                SelectedProfile = Profiles.FirstOrDefault(p => p.ProfileId == profileConfiguration.ProfileId);
        });
    }

    public ObservableCollection<ProfileConfiguration> Profiles { get; }
    public ProfilePreviewViewModel ProfilePreview { get; }

    public ProfileConfiguration? SelectedProfile
    {
        get => _selectedProfile;
        set => RaiseAndSetIfChanged(ref _selectedProfile, value);
    }

    private void Update(ProfileConfiguration? profileConfiguration)
    {
        ProfilePreview.ProfileConfiguration = null;

        foreach (ProfileConfiguration configuration in Profiles)
        {
            if (configuration == profileConfiguration)
                _profileService.ActivateProfile(configuration);
            else
                _profileService.DeactivateProfile(configuration);
        }

        ProfilePreview.ProfileConfiguration = profileConfiguration;
    }

    private void ExecuteContinue()
    {
        if (SelectedProfile == null)
            return;

        State.EntrySource = SelectedProfile;
        State.Name = SelectedProfile.Name;
        State.Icon = SelectedProfile.Icon.GetIconStream();

        // Render the material icon of the profile
        if (State.Icon == null && SelectedProfile.Icon.IconName != null)
            State.Icon = Enum.Parse<MaterialIconKind>(SelectedProfile.Icon.IconName).EncodeToBitmap(128, 14, SKColors.White);

        State.ChangeScreen<ProfileAdaptionHintsStepViewModel>();
    }
}