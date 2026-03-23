using HotelSystem.ViewModels;

namespace HotelSystem.Services;

public class NavigationService : INavigationService
{
    private readonly Dictionary<Type, BaseViewModel> _viewModels = new();
    private BaseViewModel? _currentViewModel;

    public BaseViewModel? CurrentViewModel => _currentViewModel;
    public event EventHandler<BaseViewModel>? NavigationChanged;

    public void NavigateTo<TViewModel>() where TViewModel : BaseViewModel
    {
        var type = typeof(TViewModel);
        if (!_viewModels.TryGetValue(type, out var viewModel))
        {
            viewModel = Activator.CreateInstance<TViewModel>();
            _viewModels[type] = viewModel;
        }
        _currentViewModel = viewModel;
        viewModel.InitializeAsync();
        NavigationChanged?.Invoke(this, viewModel);
    }

    public void NavigateTo(BaseViewModel viewModel)
    {
        var type = viewModel.GetType();
        _viewModels[type] = viewModel;
        _currentViewModel = viewModel;
        viewModel.InitializeAsync();
        NavigationChanged?.Invoke(this, viewModel);
    }
}

