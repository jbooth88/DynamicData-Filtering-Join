using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace TestApp;

public sealed partial class ViewModel : ObservableObject, IDisposable
{
    public ViewModel()
    {
        //Using RxApp.MainThreadScheduler crashes for some reason. Maybe I missed a nuget package?
        var uiScheduler = new System.Reactive.Concurrency.DispatcherScheduler(App.Current.Dispatcher, DispatcherPriority.Background);

        entityCache = new SourceCache<Entity, int>(x => x.Id).DisposeWith(cleanup);
        labelCache = new SourceCache<Label, int>(x => x.Id).DisposeWith(cleanup);

        var labelChangeSet = labelCache
            .Connect()
            .ChangeKey(x => x.LabelNumber);

        var entityChangeSet = entityCache
            .Connect()
            .AutoRefresh(x => x.LabelNumber);

        //Just for a drop down filter, not relevant to the demonstration
        labelChangeSet
            .DistinctValues(x => x.LabelNumber)
            .SortBy(x => x)
            .ObserveOn(uiScheduler)
            .Bind(out labelNumbers)
            .Subscribe()
            .DisposeWith(cleanup);

        var showStrangeBehaviour = true; //Change to false to see other test.
        if (showStrangeBehaviour)
        {
            labelChangeSet
                .Filter(this.WhenAnyValue(x => x.SelectedLabelNumber).Select(f => new Func<Label, bool>(l => string.IsNullOrWhiteSpace(f) || l.LabelNumber == f)))
                .InnerJoin(entityChangeSet, e => e.LabelNumber, (a, b) => new EntityLabelCombination(b, a))
                .SortBy(x => x.Entity.Id)
                .ObserveOn(uiScheduler)
                .Bind(out items)
                .Subscribe()
                .DisposeWith(cleanup);
        }
        else //The following approach seems not have the same issue as above
        {
            labelChangeSet
                .InnerJoin(entityChangeSet, e => e.LabelNumber, (a, b) => new EntityLabelCombination(b, a))
                .Filter(this.WhenAnyValue(x => x.SelectedLabelNumber).Select(f => new Func<EntityLabelCombination, bool>(l => string.IsNullOrWhiteSpace(f) || l.Entity.LabelNumber == f)))
                .SortBy(x => x.Entity.Id)
                .ObserveOn(uiScheduler)
                .Bind(out items)
                .Subscribe()
                .DisposeWith(cleanup);
        }
    }
    private readonly CompositeDisposable cleanup = new();
    public void Dispose() => cleanup.Dispose();

    private SourceCache<Entity, int> entityCache;
    private SourceCache<Label, int> labelCache;

    public ReadOnlyObservableCollection<EntityLabelCombination> Items => items;
    private readonly ReadOnlyObservableCollection<EntityLabelCombination> items;

    public ReadOnlyObservableCollection<string> LabelNumbers => labelNumbers;
    private readonly ReadOnlyObservableCollection<string> labelNumbers;

    [ObservableProperty] private string? selectedLabelNumber;


    [RelayCommand]
    private void RunTest()
    {
        //Simulation of issue
        //  A filter was used to illustrate that the issue arises once filtering is applied.
        //  There should be two EntityLabelCombination objects with Entity.Id = 3 displayed on screen.

        GetData();
        entityCache.Lookup(3).Value.LabelNumber = "0006";
        SelectedLabelNumber = "0002"; //Shows that an orphaned item exists with a label # that does not match the filter.
    }


    [RelayCommand]
    private void GetData()
    {
        //Create some sample data

        var entities = new Entity[]
        {
            new(1, "0001"),
            new(2, "0002"),
            new(3, "0003"),
            new(4, "0004"),
            new(5, "0005"),
            new(6, "0006"),
            new(7, "0007"),
            new(8, "0008"),
            new(9, "0009"),
            new(10, "0009"),
            new(11, "0001"),
            new(12, "0002"),
        };

        var labels = new Label[]
        {
            new(1, "0001"),
            new(2, "0002"),
            new(3, "0003"),
            new(4, "0004"),
            new(5, "0005"),
            new(6, "0006"),
            new(7, "0007"),
            new(8, "0008"),
            new(9, "0009"),
        };

        entityCache.EditDiff(entities, (a, b) => a.Equals(b));
        labelCache.EditDiff(labels, (a, b) => a.Equals(b));
    }
}

/// <summary>Every entity has a label, that could change at runtime.</summary>
public sealed partial class Entity : ObservableObject, IEquatable<Entity>
{
    public Entity(int id, string labelNumber)
    {
        Id = id;
        LabelNumber = labelNumber;
    }

    [ObservableProperty] private int id;
    [ObservableProperty] private string labelNumber;

    public bool Equals(Entity? other) => other is not null
        && other.Id == Id
        && string.Equals(other.LabelNumber, LabelNumber, StringComparison.Ordinal)
        ;
}

/// <summary>Labels can be shared between multiple entities.</summary>
public sealed partial class Label : ObservableObject, IEquatable<Label>
{
    public Label(int id, string labelNumber)
    {
        Id = id;
        LabelNumber = labelNumber;
    }

    [ObservableProperty] private int id;
    [ObservableProperty] private string labelNumber;

    public bool Equals(Label? other) => other is not null
        && other.Id == Id
        && string.Equals(other.LabelNumber, LabelNumber, StringComparison.Ordinal)
        ;
}

/// <summary>Entity->Label proxy for display in the UI.</summary>
public sealed partial class EntityLabelCombination
{
    public EntityLabelCombination(Entity entity, Label label)
    {
        Entity = entity;
        Label = label;
    }
    public Entity Entity { get; }
    public Label Label { get; }
}
