using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Interop;
using Stride.Core.Presentation.ViewModels;
using Stride.Editor.CustomEditors.ViewModel;
using Stride.Editor.CustomEditors.Models;


namespace StrideCustomEditorsTestProject.Editor;

public sealed class IconBrowserEditorViewModel : CustomEditorViewModel
{
    private readonly List<IconEntry> allIcons = [];
    private string filterText = string.Empty;
    private IconEntry selectedIcon;

    public ObservableCollection<IconEntry> Icons { get; }

    public string FilterText
    {
        get => filterText;
        set
        {
            if (SetValue(ref filterText, value))
                ApplyFilter();
        }
    }

    public IconEntry SelectedIcon
    {
        get => selectedIcon;
        set
        {
            if (!SetValue(ref selectedIcon, value))
                return;

            CopyKeyCommand.IsEnabled = value is not null;
            OnPropertyChanged(nameof(SelectedKey));
            OnPropertyChanged(nameof(StatusText));
        }
    }

    public string SelectedKey => SelectedIcon?.DisplayKey ?? string.Empty;

    public string StatusText => $"{Icons.Count} icon(s)" + (SelectedIcon is null ? string.Empty : $" | Selected: {SelectedIcon.DisplayKey}");

    public ICommandBase CopyKeyCommand { get; }

    public IconBrowserEditorViewModel(
        IViewModelServiceProvider serviceProvider,
        CustomEditorContext context)
        : base(serviceProvider, context)
    {
        Icons = [];
        CopyKeyCommand = new AnonymousCommand(serviceProvider, CopySelectedKey, () => SelectedIcon is not null);

        LoadIcons();
        ApplyFilter();
    }

    private void LoadIcons()
    {
        var dictionary =
            (ResourceDictionary)Application.LoadComponent(new Uri("/Stride.Assets.Presentation;component/View/ImageDictionary.xaml", UriKind.Relative));
        foreach (DictionaryEntry entry in dictionary)
        {
            if (entry.Value is not ImageSource image)
                continue;

            allIcons.Add(new IconEntry(FormatKey(entry.Key), image));
        }

        allIcons.Sort((left, right) => StringComparer.OrdinalIgnoreCase.Compare(left.DisplayKey, right.DisplayKey));
    }

    private void ApplyFilter()
    {
        var query = filterText.Trim();
        var filtered = string.IsNullOrWhiteSpace(query)
            ? allIcons
            : allIcons.Where(x => x.DisplayKey.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();

        Icons.Clear();
        foreach (var icon in filtered)
            Icons.Add(icon);

        if (SelectedIcon is null || !Icons.Contains(SelectedIcon))
            SelectedIcon = Icons.FirstOrDefault();

        OnPropertyChanged(nameof(StatusText));
    }

    private void CopySelectedKey()
    {
        if (SelectedIcon is null)
            return;

        SafeClipboard.SetText(SelectedIcon.DisplayKey);
    }

    private static string FormatKey(object key)
    {
        return key switch
        {
            null => "<null>",
            string text => text,
            Type type => type.FullName ?? type.Name,
            _ => key.ToString() ?? key.GetType().Name,
        };
    }

    public sealed class IconEntry
    {
        public string DisplayKey { get; }

        public ImageSource Image { get; }

        public IconEntry(string displayKey, ImageSource image)
        {
            DisplayKey = displayKey;
            Image = image;
        }
    }
}
