using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace ClipyFlow.Models
{
    public class LanguageCategory : INotifyPropertyChanged
    {
        private string _languageName = string.Empty;
        public string LanguageName
        {
            get => _languageName;
            set
            {
                if (_languageName != value)
                {
                    _languageName = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LanguageName)));
                }
            }
        }

        private int _totalUsageCount;
        public int TotalUsageCount
        {
            get => _totalUsageCount;
            set
            {
                if (_totalUsageCount != value)
                {
                    _totalUsageCount = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TotalUsageCount)));
                }
            }
        }

        public ObservableCollection<LanguageSnippet> Snippets { get; set; } = new ObservableCollection<LanguageSnippet>();

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
