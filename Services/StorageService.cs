using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ClipyFlow.Models;

namespace ClipyFlow.Services
{
    public class AppData
    {
        public List<ClipboardItem> History { get; set; } = new();
        public List<SnippetItem> Snippets { get; set; } = new();
        public List<LanguageCategory> LibraryCategories { get; set; } = new();
        public bool IsFirstRun { get; set; } = true;
        
        public SettingsData Settings { get; set; } = new();
    }

    public class SettingsData : System.ComponentModel.INotifyPropertyChanged
    {
        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }

        private bool _startWithWindows = false;
        public bool StartWithWindows { get => _startWithWindows; set { _startWithWindows = value; OnPropertyChanged(); } }
        
        private string _tenorApiKey = "LIVDSRZULELA";
        public string TenorApiKey { get => _tenorApiKey; set { _tenorApiKey = value; OnPropertyChanged(); } }
        
        private bool _autoPasteEnabled = true;
        public bool AutoPasteEnabled { get => _autoPasteEnabled; set { _autoPasteEnabled = value; OnPropertyChanged(); } }
        
        private string _theme = "System";
        public string Theme { get => _theme; set { _theme = value; OnPropertyChanged(); } }
        
        private string _globalHotkey = "Alt+V";
        public string GlobalHotkey { get => _globalHotkey; set { _globalHotkey = value; OnPropertyChanged(); } }
        
        private bool _isWindowPinned = false;
        public bool IsWindowPinned { get => _isWindowPinned; set { _isWindowPinned = value; OnPropertyChanged(); } }
        
        private string _customBackgroundPath = string.Empty;
        public string CustomBackgroundPath { get => _customBackgroundPath; set { _customBackgroundPath = value; OnPropertyChanged(); } }
        
        private string _customBackgroundColor = string.Empty;
        public string CustomBackgroundColor { get => _customBackgroundColor; set { _customBackgroundColor = value; OnPropertyChanged(); } }
        
        private double _customBackgroundOpacity = 0.5;
        public double CustomBackgroundOpacity { get => _customBackgroundOpacity; set { _customBackgroundOpacity = value; OnPropertyChanged(); } }
    }

    public class StorageService
    {
        private readonly string _folderPath;
        private readonly string _filePath;
        private static readonly System.Threading.SemaphoreSlim _fileLock = new System.Threading.SemaphoreSlim(1, 1);

        public StorageService()
        {
            _folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ClipyFlow");
            _filePath = Path.Combine(_folderPath, "data.json");
            
            if (!Directory.Exists(_folderPath))
            {
                Directory.CreateDirectory(_folderPath);
            }
        }

        private AppData GetDefaultData()
        {
            return new AppData
            {
                IsFirstRun = true,
                Snippets = new List<SnippetItem>
                {
                    new SnippetItem { Title = "Git Update", Content = "git add . && git commit -m \"update\" && git push" },
                    new SnippetItem { Title = "Shrug", Content = "¯\\_(ツ)_/¯" }
                },
                LibraryCategories = GetDefaultLibraryCategories()
            };
        }

        private List<LanguageCategory> GetDefaultLibraryCategories()
        {
            return new List<LanguageCategory>
            {
                new LanguageCategory
                {
                    LanguageName = "C++",
                    TotalUsageCount = 0,
                    Snippets = new ObservableCollection<LanguageSnippet>
                    {
                        new LanguageSnippet { Language = "C++", Title = "Console CP", Content = "SetConsoleCP(1251);\nSetConsoleOutputCP(1251);" },
                        new LanguageSnippet { Language = "C++", Title = "Include iostream", Content = "#include <iostream>\nusing namespace std;" },
                        new LanguageSnippet { Language = "C++", Title = "Main Boilerplate", Content = "int main() {\n    \n    return 0;\n}" },
                        new LanguageSnippet { Language = "C++", Title = "Vector Loop", Content = "for (int i = 0; i < vec.size(); i++) {\n    \n}" },
                        new LanguageSnippet { Language = "C++", Title = "Read Line", Content = "string s;\ngetline(cin, s);" }
                    }
                },
                new LanguageCategory
                {
                    LanguageName = "C#",
                    TotalUsageCount = 0,
                    Snippets = new ObservableCollection<LanguageSnippet>
                    {
                        new LanguageSnippet { Language = "C#", Title = "Console.WriteLine", Content = "Console.WriteLine(\"\");" },
                        new LanguageSnippet { Language = "C#", Title = "LINQ Select", Content = ".Select(x => x)" },
                        new LanguageSnippet { Language = "C#", Title = "LINQ Where", Content = ".Where(x => x)" },
                        new LanguageSnippet { Language = "C#", Title = "Try Catch", Content = "try\n{\n}\ncatch (Exception ex)\n{\n}" },
                        new LanguageSnippet { Language = "C#", Title = "Task Delay", Content = "await Task.Delay(1000);" }
                    }
                },
                new LanguageCategory
                {
                    LanguageName = "Python",
                    TotalUsageCount = 0,
                    Snippets = new ObservableCollection<LanguageSnippet>
                    {
                        new LanguageSnippet { Language = "Python", Title = "Main Block", Content = "if __name__ == '__main__':\n    pass" },
                        new LanguageSnippet { Language = "Python", Title = "Print", Content = "print()" },
                        new LanguageSnippet { Language = "Python", Title = "List Comprehension", Content = "[x for x in list if x]" },
                        new LanguageSnippet { Language = "Python", Title = "Try Except", Content = "try:\n    pass\nexcept Exception as e:\n    pass" },
                        new LanguageSnippet { Language = "Python", Title = "Open File", Content = "with open('file.txt', 'r') as f:\n    content = f.read()" }
                    }
                },
                new LanguageCategory
                {
                    LanguageName = "JavaScript",
                    TotalUsageCount = 0,
                    Snippets = new ObservableCollection<LanguageSnippet>
                    {
                        new LanguageSnippet { Language = "JavaScript", Title = "Console.log", Content = "console.log();" },
                        new LanguageSnippet { Language = "JavaScript", Title = "Arrow Function", Content = "const func = () => {\n\n};" },
                        new LanguageSnippet { Language = "JavaScript", Title = "Set Timeout", Content = "setTimeout(() => {\n    \n}, 1000);" },
                        new LanguageSnippet { Language = "JavaScript", Title = "Fetch API", Content = "fetch('url')\n  .then(res => res.json())\n  .then(data => console.log(data));" },
                        new LanguageSnippet { Language = "JavaScript", Title = "Async/Await", Content = "async function fetchData() {\n    const res = await fetch('url');\n    const data = await res.json();\n}" }
                    }
                },
                new LanguageCategory
                {
                    LanguageName = "HTML",
                    TotalUsageCount = 0,
                    Snippets = new ObservableCollection<LanguageSnippet>
                    {
                        new LanguageSnippet { Language = "HTML", Title = "HTML5 Boilerplate", Content = "<!DOCTYPE html>\n<html lang=\"en\">\n<head>\n    <meta charset=\"UTF-8\">\n    <title>Document</title>\n</head>\n<body>\n    \n</body>\n</html>" },
                        new LanguageSnippet { Language = "HTML", Title = "Link CSS", Content = "<link rel=\"stylesheet\" href=\"style.css\">" },
                        new LanguageSnippet { Language = "HTML", Title = "Script Tag", Content = "<script src=\"script.js\"></script>" }
                    }
                },
                new LanguageCategory
                {
                    LanguageName = "CSS",
                    TotalUsageCount = 0,
                    Snippets = new ObservableCollection<LanguageSnippet>
                    {
                        new LanguageSnippet { Language = "CSS", Title = "Flexbox Center", Content = "display: flex;\njustify-content: center;\nalign-items: center;" },
                        new LanguageSnippet { Language = "CSS", Title = "Reset", Content = "* {\n    margin: 0;\n    padding: 0;\n    box-sizing: border-box;\n}" },
                        new LanguageSnippet { Language = "CSS", Title = "Media Query", Content = "@media (max-width: 768px) {\n    \n}" }
                    }
                },
                new LanguageCategory
                {
                    LanguageName = "PHP",
                    TotalUsageCount = 0,
                    Snippets = new ObservableCollection<LanguageSnippet>
                    {
                        new LanguageSnippet { Language = "PHP", Title = "PHP Tags", Content = "<?php\n\n?>" },
                        new LanguageSnippet { Language = "PHP", Title = "Echo", Content = "echo \"\";" },
                        new LanguageSnippet { Language = "PHP", Title = "Foreach", Content = "foreach ($array as $key => $value) {\n    \n}" },
                        new LanguageSnippet { Language = "PHP", Title = "MySQLi Connect", Content = "$conn = new mysqli($servername, $username, $password, $dbname);\nif ($conn->connect_error) {\n    die(\"Connection failed: \" . $conn->connect_error);\n}" }
                    }
                }
            };
        }

        public AppData LoadData()
        {
            if (!File.Exists(_filePath))
            {
                return GetDefaultData();
            }

            try
            {
                var json = File.ReadAllText(_filePath);
                var data = JsonSerializer.Deserialize<AppData>(json);
                
                if (data == null) 
                {
                    return GetDefaultData();
                }

                if (data.LibraryCategories == null || data.LibraryCategories.Count == 0)
                {
                    data.LibraryCategories = GetDefaultLibraryCategories();
                }

                data.IsFirstRun = false;
                return data;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load data: {ex.Message}");
                // Fallback to default if JSON is totally broken
                return GetDefaultData();
            }
        }

        public async Task SaveDataAsync(AppData data)
        {
            await _fileLock.WaitAsync();
            try
            {
                data.IsFirstRun = false;
                string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                string tempPath = _filePath + ".tmp";
                File.WriteAllText(tempPath, json);
                File.Move(tempPath, _filePath, true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save data: {ex.Message}");
            }
            finally
            {
                _fileLock.Release();
            }
        }
    }
}
