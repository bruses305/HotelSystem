using HotelSystem.ViewModels;

namespace HotelSystem.Services;

public interface INavigationService
{
    void NavigateTo<TViewModel>() where TViewModel : BaseViewModel;
    void NavigateTo(BaseViewModel viewModel);
    BaseViewModel? CurrentViewModel { get; }
    event EventHandler<BaseViewModel>? NavigationChanged;
}
