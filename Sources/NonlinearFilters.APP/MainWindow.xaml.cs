﻿using Microsoft.Win32;
using NonlinearFilters.Filters2;
using NonlinearFilters.Filters2.Parameters;
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
		private Bitmap? InBmp, OutBmp;

		public MainWindow()
		{
			InitializeComponent();
		}

		private void BtnApplyFilter_Click(object sender, RoutedEventArgs e)
		{
			if (InBmp != null)
			{
				bool grayscale = CheckBoxIsGrayScale.IsChecked ?? false;

				var filter = new BilateralFilter(ref InBmp, new BilateralParameters(15, 25.5) with
				{
					GrayScale = grayscale,
				});

				/*var filter = new NonLocalMeansFilter(ref InBmp, new NonLocalMeansParameters(1, 10, 8, ImplementationType.Pixelwise) with
				{
					GrayScale = grayscale
				});*/

				filter.OnProgressChanged += new ProgressChanged((percentage, sender) =>
				{
					Dispatcher.Invoke(() => ProgressBar.Value = percentage);
				});

				var t = Task.Factory.StartNew(() =>
				{
					Stopwatch watch = new();

					watch.Start();
					OutBmp = filter.ApplyFilter(Environment.ProcessorCount - 1);
					watch.Stop();

					Dispatcher.Invoke(() =>
					{
						OutputImage.Source = ToBitmapImage(ref OutBmp);
						TxtTimeElapsed.Text = watch.Elapsed.ToString();
					});
				});

				t.ContinueWith(task =>
				{
					Debug.WriteLine(t.Exception?.Message);
				});
			}
		}

		private void BtnOpen_Click(object sender, RoutedEventArgs e)
		{
			var openFileDialog = new OpenFileDialog
			{
				Filter = ".png|*.png",
				Multiselect = false
			};

			if (openFileDialog.ShowDialog() == true)
			{
				InBmp = new Bitmap(openFileDialog.FileName);
				InputImage.Source = ToBitmapImage(ref InBmp);
			}
		}

		public static BitmapImage ToBitmapImage(ref Bitmap bmp)
		{
			using var memory = new MemoryStream();
			bmp.Save(memory, ImageFormat.Png);
			memory.Position = 0;

			var bitmapImage = new BitmapImage();
			bitmapImage.BeginInit();
			bitmapImage.StreamSource = memory;
			bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
			bitmapImage.EndInit();
			bitmapImage.Freeze();

			return bitmapImage;
		}
	}
}
