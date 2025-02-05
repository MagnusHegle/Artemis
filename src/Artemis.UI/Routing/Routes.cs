using System.Collections.Generic;
using Artemis.UI.Screens.Home;
using Artemis.UI.Screens.ProfileEditor;
using Artemis.UI.Screens.Root;
using Artemis.UI.Screens.Settings;
using Artemis.UI.Screens.Settings.Updating;
using Artemis.UI.Screens.SurfaceEditor;
using Artemis.UI.Screens.Workshop;
using Artemis.UI.Screens.Workshop.Entries;
using Artemis.UI.Screens.Workshop.Entries.Tabs;
using Artemis.UI.Screens.Workshop.Home;
using Artemis.UI.Screens.Workshop.Layout;
using Artemis.UI.Screens.Workshop.Library;
using Artemis.UI.Screens.Workshop.Library.Tabs;
using Artemis.UI.Screens.Workshop.Profile;
using Artemis.UI.Shared.Routing;

namespace Artemis.UI.Routing;

public static class Routes
{
    public static List<IRouterRegistration> ArtemisRoutes = new()
    {
        new RouteRegistration<BlankViewModel>("blank"),
        new RouteRegistration<HomeViewModel>("home"),
        new RouteRegistration<WorkshopViewModel>("workshop")
        {
            Children = new List<IRouterRegistration>
            {
                new RouteRegistration<WorkshopOfflineViewModel>("offline/{message:string}"),
                new RouteRegistration<EntriesViewModel>("entries")
                {
                    Children = new List<IRouterRegistration>
                    {
                        new RouteRegistration<ProfileListViewModel>("profiles/{page:int}"),
                        new RouteRegistration<ProfileDetailsViewModel>("profiles/details/{entryId:long}"),
#if DEBUG
                        new RouteRegistration<LayoutListViewModel>("layouts/{page:int}"),
                        new RouteRegistration<LayoutDetailsViewModel>("layouts/details/{entryId:long}"),
#endif
                    }
                },
                new RouteRegistration<WorkshopLibraryViewModel>("library")
                {
                    Children = new List<IRouterRegistration>
                    {
                        new RouteRegistration<InstalledTabViewModel>("installed"),
                        new RouteRegistration<SubmissionsTabViewModel>("submissions"),
                        new RouteRegistration<SubmissionDetailViewModel>("submissions/{entryId:long}"),
                    }
                }
            }
        },
        new RouteRegistration<SurfaceEditorViewModel>("surface-editor"),
        new RouteRegistration<SettingsViewModel>("settings")
        {
            Children = new List<IRouterRegistration>
            {
                new RouteRegistration<GeneralTabViewModel>("general"),
                new RouteRegistration<PluginsTabViewModel>("plugins"),
                new RouteRegistration<DevicesTabViewModel>("devices"),
                new RouteRegistration<ReleasesTabViewModel>("releases")
                {
                    Children = new List<IRouterRegistration>
                    {
                        new RouteRegistration<ReleaseDetailsViewModel>("{releaseId:guid}")
                    }
                },
                new RouteRegistration<AboutTabViewModel>("about")
            }
        },
        new RouteRegistration<ProfileEditorViewModel>("profile-editor/{profileConfigurationId:guid}")
    };
}