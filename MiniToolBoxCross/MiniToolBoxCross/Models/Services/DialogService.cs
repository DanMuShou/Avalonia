using System.Threading.Tasks;
using Avalonia.Controls;
using Irihi.Avalonia.Shared.Contracts;
using MiniToolBoxCross.Models.Repositories;
using MiniToolBoxCross.ViewModels.Dialogs;
using Ursa.Controls;

namespace MiniToolBoxCross.Models.Services;

public class DialogService(INotificationService notificationService) : IDialogService
{
    private readonly OverlayDialogOptions _defaultOptions = new()
    {
        FullScreen = false,
        IsCloseButtonVisible = false,
        CanDragMove = true,
        CanResize = true,
    };

    public async Task ShowCustomModalAsync<TView, TViewModel>(
        TViewModel viewModel,
        OverlayDialogOptions? options = null
    )
        where TView : Control, new()
        where TViewModel : DialogBase, IDialogContext
    {
        viewModel.NotificationService = notificationService;
        await OverlayDialog.ShowCustomModal<TView, TViewModel, object>(
            viewModel,
            options: options ?? _defaultOptions
        );
    }

    public async Task<TResult?> ShowCustomModalAsync<TView, TViewModel, TResult>(
        TViewModel viewModel,
        OverlayDialogOptions? options = null
    )
        where TView : Control, new()
        where TViewModel : DialogBase, IDialogContext
    {
        viewModel.NotificationService = notificationService;
        return await OverlayDialog.ShowCustomModal<TView, TViewModel, TResult>(
            viewModel,
            options: options ?? _defaultOptions
        );
    }
}
