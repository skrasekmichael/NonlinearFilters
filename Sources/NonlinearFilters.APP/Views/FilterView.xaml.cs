using System.Windows.Controls;
using System.Windows.Input;

namespace NonlinearFilters.APP.Views
{
	public partial class FilterParametersView : UserControl
	{
		public FilterParametersView()
		{
			InitializeComponent();
		}

		private void ProcessCountValidation(object sender, TextCompositionEventArgs e)
		{
			var textbox = (TextBox)sender;
			var text = textbox.Text.AsSpan();
			var input = textbox.SelectionLength switch
			{
				> 0 => $"{text[..textbox.SelectionStart]}{e.Text}{text[(textbox.SelectionStart + textbox.SelectionLength)..]}",
				_ => $"{text}{e.Text}"
			};

			if (uint.TryParse(input, out var processCount))
				e.Handled = processCount < 1 || processCount > Environment.ProcessorCount;
			else
				e.Handled = true;
		}
	}
}
