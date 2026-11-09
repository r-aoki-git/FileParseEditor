using FileParseEditor.Core.Mvvm;

namespace FileParseEditor.Tests.Mvvm;

public class ObservableObjectTests
{
    [Fact]
    public void SetProperty_ChangedValue_UpdatesFieldAndNotifiesOnce()
    {
        var target = new Sample();
        var notified = new List<string?>();
        target.PropertyChanged += (_, e) => notified.Add(e.PropertyName);

        var changed = target.SetName("a");

        Assert.True(changed);
        Assert.Equal("a", target.Name);
        Assert.Equal(new[] { nameof(Sample.Name) }, notified);
    }

    [Fact]
    public void SetProperty_SameValue_DoesNotNotify()
    {
        var target = new Sample();
        target.SetName("a");
        var notified = 0;
        target.PropertyChanged += (_, _) => notified++;

        var changed = target.SetName("a");

        Assert.False(changed);
        Assert.Equal(0, notified);
    }

    [Fact]
    public void SetProperty_NullAndBack_IsTreatedAsChange()
    {
        var target = new Sample();
        target.SetName("a");
        var notified = 0;
        target.PropertyChanged += (_, _) => notified++;

        Assert.True(target.SetName(null));
        Assert.True(target.SetName("a"));
        Assert.Equal(2, notified);
    }

    [Fact]
    public void SetProperty_WithoutSubscriber_DoesNotThrow()
    {
        var target = new Sample();

        Assert.True(target.SetName("a"));
    }

    private sealed class Sample : ObservableObject
    {
        private string? _name;

        public string? Name => _name;

        public bool SetName(string? value) => SetProperty(ref _name, value, nameof(Name));
    }
}
