using Artemis.UI.Shared.Controls.GradientPicker;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;

namespace Artemis.VisualScripting.Nodes.Color.Screens;

public partial class RampSKColorNodeCustomView : ReactiveUserControl<RampSKColorNodeCustomViewModel>
{
    public RampSKColorNodeCustomView()
    {
        InitializeComponent();
    }


    private void GradientPickerButton_OnFlyoutOpened(GradientPickerButton sender, EventArgs args)
    {
    }

    private void GradientPickerButton_OnFlyoutClosed(GradientPickerButton sender, EventArgs args)
    {
        ViewModel?.StoreGradient();
    }
}