using Microsoft.Win32;
using NonlinearFilters.Extensions;
using NonlinearFilters.Filters;
using NonlinearFilters.Filters.Parameters;
using NonlinearFilters.Filters2D;
using NonlinearFilters.Filters3D;
using NonlinearFilters.Mathematics;
using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;
using System.Diagnostics;
using System.Drawing;
using System.Windows;
using System.Windows.Threading;
using System.IO;

namespace NonlinearFilters.APP
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private bool isVol = false;

		private Bitmap? InBmp, OutBmp;
		private VolumetricImage? InVol, OutVol;

		public MainWindow()
		{
			InitializeComponent();
		}

		private void BtnApplyFilter_Click(object sender, RoutedEventArgs e)
		{
			bool grayscale = CheckBoxIsGrayScale.IsChecked ?? false;

			if (isVol)
			{
				if (InVol is not null)
				{
					var filter = new FastBilateralFilter3(ref InVol, new BilateralParameters(15, 15) with
					{
						GrayScale = grayscale
					});

					filter.OnProgressChanged += new ProgressChanged((percentage, sender) =>
					{
						Dispatcher.Invoke(() => ProgressBar.Value = percentage);
					});

					var t = Task.Factory.StartNew(() =>
					{
						var watch = new Stopwatch();

						watch.Start();
						OutVol = filter.ApplyFilter(Environment.ProcessorCount - 1);
						watch.Stop();

						Dispatcher.Invoke(() =>
						{
							OutputImage.Source = OutVol.Render().ToBitmapImage();
							TxtTimeElapsed.Text = watch.Elapsed.ToString();
							btnOutput3D.IsEnabled = true;
						});
					});
				}
			}
			else
			{
				if (InBmp is not null)
				{
					var filter = new FastBilateralFilter(ref InBmp, new BilateralParameters(15, 25.5) with
					{
						GrayScale = grayscale
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
		}

		private void LoadVol(string path)
		{
			InVol = VolumetricImage.FromFile(path);
			InBmp = InVol.Render();
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
					isVol = true;
					LoadVol(openFileDialog.FileName);
				}
				else
				{
					isVol = false;
					InBmp = new Bitmap(openFileDialog.FileName);
					InputImage.Source = InBmp.ToBitmapImage();
				}
				btnInput3D.IsEnabled = isVol;
			}
		}

		private void BtnInput3D_Click(object sender, RoutedEventArgs e)
		{
			if (InVol is not null)
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
				game.SetVolume(InVol);
				game.Run();
			}
		}

		private void BtnOutput3D_Click(object sender, RoutedEventArgs e)
		{
			if (OutVol is not null)
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
				game.SetVolume(OutVol);
				game.Run();
			}
		}
	}
}
