using System.Threading.Tasks;
using Avalonia.Controls;
using Irihi.Avalonia.Shared.Contracts;
using MiniToolBoxCross.ViewModels.Dialogs;
using Ursa.Controls;

namespace MiniToolBoxCross.Models.Repositories;

public interface IDialogService
{
    Task ShowCustomModalAsync<TView, TViewModel>(
        TViewModel viewModel,
        OverlayDialogOptions? options = null
    )
        where TView : Control, new()
        where TViewModel : DialogBase, IDialogContext;

    Task<TResult?> ShowCustomModalAsync<TView, TViewModel, TResult>(
        TViewModel viewModel,
        OverlayDialogOptions? options = null
    )
        where TView : Control, new()
        where TViewModel : DialogBase, IDialogContext;
}
