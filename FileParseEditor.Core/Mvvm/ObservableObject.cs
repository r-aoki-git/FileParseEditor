using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FileParseEditor.Core.Mvvm;

/// <summary>
/// プロパティの変更を画面に通知する、ViewModel の基底クラス（<see cref="INotifyPropertyChanged"/> の実装）。
/// </summary>
public abstract class ObservableObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// 値が変わった場合だけフィールドを書き換え、変更を通知する。
    /// </summary>
    /// <returns>値が変わった場合は true。</returns>
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
