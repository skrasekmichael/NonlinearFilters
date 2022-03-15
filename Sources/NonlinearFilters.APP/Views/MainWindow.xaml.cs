using NonlinearFilters.APP.ViewModels;
using System.Windows;

namespace NonlinearFilters.APP.Views
{
	public partial class MainWindow : Window
	{
		public MainWindow(MainViewModel viewModel)
		{
			InitializeComponent();
			DataContext = viewModel;
		}
	}
}
