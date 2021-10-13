﻿using Microsoft.Win32;
using NonlinearFilters.Filters2;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
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

				FastBilateralFilter filter = new(ref InBmp, 16, 0.3);
				filter.OnProgressChanged += new ProgressChanged((percentage, sender) =>
				{
					Dispatcher.Invoke(() => ProgressBar.Value = percentage);
				});

				var t = Task.Factory.StartNew(() =>
				{
					Stopwatch watch = new();

					watch.Start();
					OutBmp = filter.ApplyFilter(Environment.ProcessorCount - 1, grayscale);
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
			OpenFileDialog openFileDialog = new();
			openFileDialog.Filter = ".png|*.png";
			openFileDialog.Multiselect = false;
			if (openFileDialog.ShowDialog() == true)
			{
				InBmp = new Bitmap(openFileDialog.FileName);
				InputImage.Source = ToBitmapImage(ref InBmp);
			}
		}

		public static BitmapImage ToBitmapImage(ref Bitmap bmp)
		{
			using MemoryStream memory = new();
			bmp.Save(memory, ImageFormat.Png);
			memory.Position = 0;

			BitmapImage bitmapImage = new();
			bitmapImage.BeginInit();
			bitmapImage.StreamSource = memory;
			bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
			bitmapImage.EndInit();
			bitmapImage.Freeze();

			return bitmapImage;
		}
	}
}
