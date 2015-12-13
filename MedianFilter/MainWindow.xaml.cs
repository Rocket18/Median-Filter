using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MedianFilter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private String filePath { get; set; }

        private Bitmap originalBitmap { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            Median.ItemsSource = Enumerable.Range(3, 10).ToList();

            isgray.IsChecked = false;
        }

        private void OpenDialog(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "Select an image file.";
            ofd.Filter = "Jpeg Images(*.jpg)|*.jpg|Png Images(*.png)|*.png";
            ofd.Filter += "|Bitmap Images(*.bmp)|*.bmp";

            if (ofd.ShowDialog() == true)
            {
                filePath = ofd.FileName;
                using (var streamReader = new StreamReader(filePath))
                {
                    originalBitmap = (Bitmap)Bitmap.FromStream(streamReader.BaseStream);

                }


                OriginalImage.Source = new BitmapImage(new Uri(filePath));

                ApplyFilter();
            }
        }

        private void ApplyFilter()
        {
            if (Median.SelectedIndex == -1) return;

            var result = MedianFilter(originalBitmap, Median.SelectedIndex, isgray.IsChecked.Value);

            OutputImage.Source = CreateBitmapSourceFromGdiBitmap(result);

        }
        public static Bitmap MedianFilter(Bitmap sourceBitmap,int matrixSize,bool grayscale = true,int bias = 0)
        {
            BitmapData sourceData = sourceBitmap.LockBits(
                       new System.Drawing.Rectangle(0, 0,sourceBitmap.Width, sourceBitmap.Height),
                       ImageLockMode.ReadOnly,
                       System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            byte[] pixelBuffer = new byte[sourceData.Stride * sourceData.Height];

            byte[] resultBuffer = new byte[sourceData.Stride * sourceData.Height];

            Marshal.Copy(sourceData.Scan0, pixelBuffer, 0, pixelBuffer.Length);

            sourceBitmap.UnlockBits(sourceData);

            if (grayscale)
            {
                float rgb = 0;

                for (int k = 0; k < pixelBuffer.Length; k += 4)
                {
                    rgb = pixelBuffer[k] * 0.11f;
                    rgb += pixelBuffer[k + 1] * 0.59f;
                    rgb += pixelBuffer[k + 2] * 0.3f;


                    pixelBuffer[k] = (byte)rgb;
                    pixelBuffer[k + 1] = pixelBuffer[k];
                    pixelBuffer[k + 2] = pixelBuffer[k];
                    pixelBuffer[k + 3] = 255;
                }
            }

            int filterOffset = (matrixSize - 1) / 2;
            int calcOffset = 0;

            int byteOffset = 0;

            var neighbourPixels = new List<int>();
            byte[] middlePixel;

            for (int offsetY = filterOffset; offsetY < sourceBitmap.Height - filterOffset; offsetY++)
            {
                for (int offsetX = filterOffset; offsetX <sourceBitmap.Width - filterOffset; offsetX++)
                {
                    byteOffset = offsetY *sourceData.Stride + offsetX * 4;

                    neighbourPixels.Clear();

                    for (int filterY = -filterOffset; filterY <= filterOffset; filterY++)
                    {
                        for (int filterX = -filterOffset;filterX <= filterOffset; filterX++)
                        {

                            calcOffset = byteOffset + (filterX * 4) +(filterY * sourceData.Stride);

                            neighbourPixels.Add(BitConverter.ToInt32(pixelBuffer, calcOffset));
                        }
                    }

                    neighbourPixels.Sort();

                    middlePixel = BitConverter.GetBytes(neighbourPixels[filterOffset]);

                    resultBuffer[byteOffset] = middlePixel[0];
                    resultBuffer[byteOffset + 1] = middlePixel[1];
                    resultBuffer[byteOffset + 2] = middlePixel[2];
                    resultBuffer[byteOffset + 3] = middlePixel[3];
                }
            }

            var resultBitmap = new Bitmap(sourceBitmap.Width, sourceBitmap.Height);

            var resultData =
                       resultBitmap.LockBits(new System.Drawing.Rectangle(0, 0,
                       resultBitmap.Width, resultBitmap.Height),
                       ImageLockMode.WriteOnly,
                       System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            Marshal.Copy(resultBuffer, 0, resultData.Scan0,
                                       resultBuffer.Length);

            resultBitmap.UnlockBits(resultData);

            return resultBitmap;
        }

        public static BitmapSource CreateBitmapSourceFromGdiBitmap(Bitmap bitmap)
        {
            if (bitmap == null)
                throw new ArgumentNullException("bitmap");

            var rect = new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height);

            var bitmapData = bitmap.LockBits(
                rect,
                ImageLockMode.ReadWrite,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            try
            {
                var size = (rect.Width * rect.Height) * 4;

                return BitmapSource.Create(
                    bitmap.Width,
                    bitmap.Height,
                    bitmap.HorizontalResolution,
                    bitmap.VerticalResolution,
                    PixelFormats.Bgra32,
                    null,
                    bitmapData.Scan0,
                    size,
                    bitmapData.Stride);
            }
            finally
            {
                bitmap.UnlockBits(bitmapData);
            }
        }
   

        private void Median_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (originalBitmap == null) return;

            ApplyFilter();
        }

       

    }
}
