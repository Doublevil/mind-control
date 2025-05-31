using System.Windows;
using System.Windows.Controls;
using MindControl.Samples.SlimeRancherDemo;

namespace MindControl.Samples.SrDemoWpfApp;

public class GameStateTemplateSelector : DataTemplateSelector
{
    public DataTemplate? NotRunningTemplate { get; set; }
    public DataTemplate? InMenuTemplate { get; set; }
    public DataTemplate? InGameTemplate { get; set; }

    public override DataTemplate? SelectTemplate(object? item, DependencyObject container)
    {
        if (item is GameRunState runState)
        {
            return runState switch
            {
                GameRunState.NotRunning => NotRunningTemplate,
                GameRunState.InMenu => InMenuTemplate,
                GameRunState.InGame => InGameTemplate,
                _ => base.SelectTemplate(item, container)
            };
        }
        return base.SelectTemplate(item, container);
    }
}