using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using llcom.Avalonia.ViewModels;

namespace llcom.Avalonia.Views;

public partial class LuaScriptView : UserControl
{
    public LuaScriptView()
    {
        InitializeComponent();
    }

    private void RunOneLineTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is LuaScriptViewModel vm)
        {
            vm.RunCommandCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void TextEditor_TextChanged(object? sender, EventArgs e)
    {
        if (DataContext is LuaScriptViewModel vm)
        {
            vm.MarkDocumentChanged();
        }
    }

    private void TextEditor_LostFocus(object? sender, RoutedEventArgs e)
    {
        if (DataContext is LuaScriptViewModel vm)
        {
            vm.OnEditorLostFocus();
        }
    }
}
