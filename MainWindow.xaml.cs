using System.Windows;
using System.Windows.Controls;

namespace StarRuptureSaveEditor;

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

    private void SetAmount_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button &&
            button.Tag is string tagValue &&
            int.TryParse(tagValue, out int amount) &&
            DataContext is MainViewModel mainVm &&
            mainVm.InventoryEditor.SelectedItem != null)
        {
            mainVm.InventoryEditor.SelectedItem.Amount = amount;
        }
    }
}
