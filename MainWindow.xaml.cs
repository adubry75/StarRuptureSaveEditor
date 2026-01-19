using System.Windows;

namespace ForeverSkiesSaveEditor;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
    
    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (DataContext is MainViewModel vm && vm.HasUnsavedChanges)
        {
            var result = MessageBox.Show(
                "You have unsaved changes. Save before closing?",
                "Unsaved Changes",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Warning);
            
            if (result == MessageBoxResult.Yes)
            {
                vm.SaveFileCommand.Execute(null);
            }
            else if (result == MessageBoxResult.Cancel)
            {
                e.Cancel = true;
                return;
            }
        }
        
        base.OnClosing(e);
    }
}
