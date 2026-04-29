using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PayrollSystem.ViewModels
{
    /// <summary>
    /// Base ViewModel with INotifyPropertyChanged support
    /// </summary>
    public abstract class BaseViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        public virtual bool HasUnsavedChanges => false;

        public event System.EventHandler<ToastEventArgs>? ToastRequested;

        protected void ShowToast(string message, string icon = "✅")
        {
            ToastRequested?.Invoke(this, new ToastEventArgs(message, icon));
        }
    }

    public class ToastEventArgs : System.EventArgs
    {
        public string Message { get; }
        public string Icon { get; }

        public ToastEventArgs(string message, string icon)
        {
            Message = message;
            Icon = icon;
        }
    }
}
