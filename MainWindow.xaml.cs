using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using OpenCvSharp.CPlusPlus;
using OpenCvSharp.Extensions;

namespace MakeImageCensored
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {

        public DrawingAttributes inkDA = new DrawingAttributes
        {
            Color = Colors.SpringGreen,
            Height = 5,
            Width = 5,
            FitToCurve = true,
            IsHighlighter = true
        };
        public MainWindow()
        {
            InitializeComponent();
            inkCanvas.DefaultDrawingAttributes = inkDA;
        }

        private void Btn_Save(object sender, RoutedEventArgs e)
        {

            SaveFileDialog saveDialog = new SaveFileDialog
            {
                Filter = "PNG|*.png",
                AddExtension = true
            };

            if (saveDialog.ShowDialog() == true)
            {

                FileStream stream = new FileStream(saveDialog.FileName, FileMode.Create, FileAccess.Write);
                int width = (int)inkCanvas.ActualWidth;
                int height = (int)inkCanvas.ActualHeight;
                inkCanvas.Background = null;
                //render ink to bitmap
                RenderTargetBitmap renderBitmap =
                new RenderTargetBitmap(width, height, 96d, 96d, PixelFormats.Default);
                renderBitmap.Render(inkCanvas);
                //save the ink to a memory stream
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(renderBitmap));
                encoder.Save(stream);
                stream.Close();
            }
        }

        private void Btn_Process(object sender, RoutedEventArgs e)
        {

            var copyCanvas = new InkCanvas
            {
                Width = inkCanvas.Width,
                Height = inkCanvas.Height,
                Strokes = inkCanvas.Strokes.Clone()
            };

            using (var stream = new MemoryStream())
            {
                int width = (int)copyCanvas.Width;
                int height = (int)copyCanvas.Height;
                RenderTargetBitmap renderBitmap =
                new RenderTargetBitmap(width, height, 96d, 96d, PixelFormats.Default);
                renderBitmap.Render(copyCanvas);
                //save the ink to a memory stream
                PngBitmapEncoder encoder;
                encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(renderBitmap));
                encoder.Save(stream);
                Global.StrokeImage = System.Drawing.Image.FromStream(stream) as Bitmap;
                //Global.StrokeImage.Save("C:\\temp\\1.png");
            }

            //

            if (Global.SourceImage != null && Global.StrokeImage != null)
            {
                //Global.StrokeImage

                var Stroke = BitmapConverter.ToMat(Global.StrokeImage);
                var Source = BitmapConverter.ToMat(Global.SourceImage);
                var newMat = new Mat(Stroke.Rows, Stroke.Cols, MatType.CV_8UC3);
                Parallel.For(0, Stroke.Rows, (y) =>
                {
                    Parallel.For(0, Stroke.Cols, (x) =>
                    {
                        var pt = Stroke.At<Vec3b>(y, x);
                        var so = Source.At<Vec3b>(y, x);

                        if (pt.Item0 == 0 && pt.Item1 == 0 && pt.Item2 == 0)
                        {
                            newMat.Set<Vec3b>(y, x, new Vec3b(0, 0, 0));
                            
                        }
                        else
                        {
                            byte b = (byte)(Math.Pow(so[0] / 255.0d, Global.Gamma)*255);
                            byte g = (byte)(Math.Pow(so[1] / 255.0d, Global.Gamma)*255);
                            byte r = (byte)(Math.Pow(so[2] / 255.0d, Global.Gamma)*255);
                            newMat.Set<Vec3b>(y, x, new Vec3b(b,g,r));
                        }

                        

                    });
                    Console.WriteLine();
                });
                //Cv2.ImShow("hello", newMat);
                

               

                SaveFileDialog saveDialog = new SaveFileDialog
                {
                    Filter = "PNG|*.png",
                    AddExtension = true
                };

                if (saveDialog.ShowDialog() == true)
                {
                    Cv2.ImWrite(saveDialog.FileName, newMat);
                    Png png = new Png(saveDialog.FileName);

                    png.RemoveChunk(Png.ChunkType.RgbColorSpace);
                    png.InsertChunk("gAMA", 4, 389);
                    png.Save(saveDialog.FileName);
                }
                
            }



        }
        private void Btn_Clear(object sender, RoutedEventArgs e)
        {
            inkCanvas.Strokes.Clear();
        }

        private void Btn_Open(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openDialog = new OpenFileDialog();

            if (openDialog.ShowDialog() == true)
            {
                if (File.Exists(openDialog.FileName))
                {
                    Global.SourceImage = new Bitmap(openDialog.FileName);

                    inkCanvas.Width = CanvasBorder.Width = Global.SourceImage.Width;
                    inkCanvas.Height = CanvasBorder.Height = Global.SourceImage.Height;

                    MessageBox.Show(Global.SourceImage.Width + ":" + Global.SourceImage.Height);

                    ImageBrush imgBrush = new ImageBrush
                    {
                        ImageSource = BitmapSourceConverter.ToBitmapSource(Global.SourceImage),
                        Stretch = Stretch.Uniform,

                    };
                    inkCanvas.Background = imgBrush;
                }
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.OemCloseBrackets)
            {
                inkCanvas.DefaultDrawingAttributes.Height++;
                inkCanvas.DefaultDrawingAttributes.Width++;
            }
            else if (e.Key == Key.OemOpenBrackets)
            {
                inkCanvas.DefaultDrawingAttributes.Height--;
                inkCanvas.DefaultDrawingAttributes.Width--;
            }

        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                Global.Gamma = double.Parse((sender as TextBox).Text);
            }
            catch
            {
                Global.Gamma = 0.0095;
            }
        }
    }
}
