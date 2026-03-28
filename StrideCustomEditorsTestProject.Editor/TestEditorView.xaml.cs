// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.


using Stride.Core.Assets.Editor.Annotations;

using Stride.Editor.CustomEditors.Annotations;

using Stride.Editor.CustomEditors.Wpf.Views;
using StrideCustomEditorsTestProject.Editor;

namespace StrideCustomEditorsTestProject.Editor
{
    [CustomEditor<TestEditorViewModel>("test.tool-editor", "Test Editor")]
    public partial class TestEditorView : CustomEditorView
    {
        public TestEditorView()
        {
            InitializeComponent();
            
        }
    }
}
