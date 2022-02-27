using Microsoft.Win32;
using NonlinearFilters.Extensions;
using NonlinearFilters.Filters2;
using NonlinearFilters.Filters2.Parameters;
using NonlinearFilters.Mathematics;
using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace NonlinearFilters.APP
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private bool volType = false;

		private Bitmap? InBmp, OutBmp;
		private VolumetricImage? Vol;

		public MainWindow()
		{
			InitializeComponent();
		}

		private void BtnApplyFilter_Click(object sender, RoutedEventArgs e)
		{
			if (InBmp != null)
			{
				bool grayscale = CheckBoxIsGrayScale.IsChecked ?? false;

				var filter = new FastBilateralFilter(ref InBmp, new BilateralParameters(15, 25.5) with
				{
					GrayScale = grayscale,
				});

				filter.OnProgressChanged += new ProgressChanged((percentage, sender) =>
				{
					Dispatcher.Invoke(() => ProgressBar.Value = percentage);
				});

				var t = Task.Factory.StartNew(() =>
				{
					var watch = new Stopwatch();

					watch.Start();
					OutBmp = filter.ApplyFilter(Environment.ProcessorCount - 1);
					watch.Stop();

					Dispatcher.Invoke(() =>
					{
						OutputImage.Source = OutBmp.ToBitmapImage();
						TxtTimeElapsed.Text = watch.Elapsed.ToString();
					});
				});
			}
		}

		private void LoadVol(string path)
		{
			Vol = VolumetricImage.FromFile(path);
			InBmp = Vol.Render();
			InputImage.Source = InBmp.ToBitmapImage();
		}

		private void BtnOpen_Click(object sender, RoutedEventArgs e)
		{
			var openFileDialog = new OpenFileDialog
			{
				Filter = ".png|*.png|.jpg|*.jpg|.vol|*.vol",
				Multiselect = false
			};

			if (openFileDialog.ShowDialog() == true)
			{
				if (Path.GetExtension(openFileDialog.FileName) == ".vol")
				{
					volType = true;
					LoadVol(openFileDialog.FileName);
				}
				else
				{
					volType = false;
					InBmp = new Bitmap(openFileDialog.FileName);
					InputImage.Source = InBmp.ToBitmapImage();
				}
				btn3D.IsEnabled = volType;
			}
		}

		private void Btn3D_Click(object sender, RoutedEventArgs e)
		{
			var gameWindowSettings = new GameWindowSettings()
			{

			};

			var nativeWindowSettings = new NativeWindowSettings()
			{
				Size = new Vector2i(800, 600),
				Title = "Volume Renderer"
			};

			using var game = new VolumeWindow(gameWindowSettings, nativeWindowSettings);
			game.SetVolume(Vol);
			game.Run();

		}
	}
}
