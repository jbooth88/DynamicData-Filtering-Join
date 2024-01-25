namespace TestApp;
public partial class MainWindow : IDisposable
{
    public MainWindow()
    {
        InitializeComponent();
        SetBinding(DataContextProperty, new Binding()
        {
            Source = new ViewModel(),
            Mode = BindingMode.OneTime
        });

        Observable
            .FromEventPattern<RoutedEventArgs>(ClearButton, nameof(ClearButton.Click))
            .ObserveOn(Dispatcher, DispatcherPriority.Normal)
            .Subscribe(_ => LabelNumberPickBox.SelectedItem = null)
            .DisposeWith(cleanup);
    }
    private readonly CompositeDisposable cleanup = new();
    public void Dispose() => cleanup.Dispose();
}