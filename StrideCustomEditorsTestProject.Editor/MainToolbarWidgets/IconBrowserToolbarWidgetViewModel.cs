using System.Threading.Tasks;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.ViewModels;
using Stride.Editor.CustomEditors.Models;
using Stride.Editor.CustomEditors.Services;
using Stride.Editor.CustomEditors.ViewModel;

namespace StrideCustomEditorsTestProject.Editor.MainToolbarWidgets;

public sealed class IconBrowserToolbarWidgetViewModel : MainToolbarWidgetViewModel
{
    public ICommandBase OpenCommand { get; }

    public IconBrowserToolbarWidgetViewModel(
        IViewModelServiceProvider serviceProvider,
        MainToolbarWidgetContext context)
        : base(serviceProvider, context)
    {
        OpenCommand = new AnonymousTaskCommand(serviceProvider, OpenIconBrowserAsync);
    }

    private async Task OpenIconBrowserAsync()
    {
        await ServiceProvider.Get<ICustomEditorsService>().OpenAsync("test.icon-browser");
    }
}
