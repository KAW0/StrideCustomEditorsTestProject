using Stride.Core.Presentation.ViewModels;

namespace StrideCustomEditorsTestProject.Editor;

public sealed class TestEditorToolCellViewModel : ViewModelBase
{
    private string value;
    private bool isSelected;

    public int Index { get; }

    public string Value
    {
        get => value;
        set => SetValue(ref this.value, value);
    }

    public bool IsSelected
    {
        get => isSelected;
        set => SetValue(ref isSelected, value);
    }

    public TestEditorToolCellViewModel(int index)
    {
        Index = index;
        value = ".";
    }
}
