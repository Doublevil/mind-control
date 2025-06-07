using System.Windows;
using System.Windows.Controls;

namespace MindControl.Samples.SrDemoWpfApp;

public partial class GameStateView : UserControl
{
    public GameStateView()
    {
        InitializeComponent();
    }

    private void OnToggleInfiniteStaminaClicked(object sender, RoutedEventArgs e)
    {
        if (DataContext is GameStateViewModel vm)
            vm.ToggleInfiniteStamina();
    }
}