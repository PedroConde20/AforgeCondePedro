using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using AForge.Imaging;
using Microsoft.Win32;
using AForge.Video;
using AForge.Video.DirectShow;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using Color = System.Drawing.Color;
using Point = System.Drawing.Point;
using AForge.Imaging.Filters;
using AForge;

namespace Aforge2._0
{
    /// <summary>
    /// Lógica de interacción para MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        System.Windows.Controls.Image image = new System.Windows.Controls.Image();
        public ObservableCollection<FilterInfo> VideoDevices { get; set; }
        public FilterInfo CurrentDevice
        {
            get { return _currentDevice; }
            set { _currentDevice = value; this.OnPropertyChanged("Disp Actual"); }

        }
        public MainWindow()
        {
            InitializeComponent();
            GetVideoDevices();
        }

        public int selector;
        public BitmapImage snapshoot;
        public Bitmap ImagenBitmap;
        public Bitmap auxiliar;


        int[] histograma;
        int totalPixeles = 0;
        int contador;
        BitmapImage image_original;
        BitmapImage image_gray;
        int umbral = 128;
        bool imgReady = false;
        double[] HA;
        double sMIN;

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string nombrePropiedad)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                var e = new PropertyChangedEventArgs(nombrePropiedad);
                handler(this, e);
            }
        }

        public static BitmapImage ToBitmapImage(System.Drawing.Image bitmap)
        {
            using (var memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Png);
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
        private Bitmap BitmapImage2Bitmap(BitmapImage imgOrg)
        {
            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(imgOrg));
                enc.Save(outStream);
                Bitmap bitmap = new Bitmap(outStream);

                return new Bitmap(bitmap);
            }
        }

        //Camara
        // private IVideoSource _VideoSource;
        private VideoCaptureDevice _VideoSource;
        private FilterInfo _currentDevice;
        List<int[,]> listadefiltros = new List<int[,]>();


        new int[,] Identidad = { { 1, 0, -1 }, { 0, 0, 0 }, { -1, 0, 1 } };
        new int[,] DeteccionBordes = { { 0, 1, 0 }, { 1, -4, 1 }, { 0, 1, 0 } };
        new int[,] DeteccionBordes2 = { { -1, -1, -1 }, { -1, 8, -1 }, { -1, -1, -1 } };
        new int[,] Enfocar = { { 0, -1, 0 }, { -1, 5, -1 }, { 0, -1, 0 } };
        new int[,] Desenfocar = { { 1, 1, 1 }, { 1, 1, 1 }, { 1, 1, 1 } };
        new int[,] Desenfoque_Gaussiano = { { 1, 2, 1 }, { 2, 4, 2 }, { 1, 2, 1 } };

        new int[,] Desenfoque_Gaussiano5 = { { 1, 4, 6, 4, 1 }, { 4, 16, 24, 16, 4 }, { 6, 24, 36, 24, 6 }, { 4, 16, 24, 16, 4 }, { 1, 4, 6, 4, 1 } };
        new int[,] MascaraDesenfoque = { { 1, 4, 6, 4, 1 }, { 4, 16, 24, 16, 4 }, { 6, 24, -476, 24, 6 }, { 4, 16, 24, 16, 4 }, { 1, 4, 6, 4, 1 } };

        new int[,] Filtro1 = { { 1, 0, -1 }, { 1, 0, -1 }, { 1, 0, -1 } };

        new int[,] Filtro2 = { { -1, 0, 1 }, { -2, 0, 2 }, { -1, 0, 1 } };
        private void btnIniciarCamara_Click(object sender, RoutedEventArgs e)
        {
            //Iniciar

            StartCamera();
            if (cb_resolution.SelectedItem != null)
            {
                _VideoSource.VideoResolution = _VideoSource.VideoCapabilities[cb_resolution.SelectedIndex];
            }
        }

        private void btnDetenerCamara_Click(object sender, RoutedEventArgs e)
        {
            //Detener
            StopCamera();
        }

        private void cb_resolution_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //VideoCaptureDevice _VideoSource = new VideoCaptureDevice(CurrentDevice.MonikerString);
            StopCamera();
            if (cb_resolution.SelectedItem != null)
            {
                StartCamera();
                _VideoSource.VideoResolution = _VideoSource.VideoCapabilities[cb_resolution.SelectedIndex];
            }
        }
        public void StopCamera()
        {
            if (_VideoSource != null && _VideoSource.IsRunning)
            {
                _VideoSource.SignalToStop();
                _VideoSource.NewFrame -= video_NewFrame;
                _VideoSource = null;

            }
        }
        private void video_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            try
            {
                BitmapImage bi;
                Bitmap bm;
                using (var bitmap = (Bitmap)eventArgs.Frame.Clone())
                {
                    bi = ToBitmapImage(bitmap);
                    bm = bitmap;
                    //snapShoot
                    switch (selector)
                    {
                        case 20:
                            snapshoot = bi;
                            selector = -1;
                            break;
                        case 2:
                            snapshoot = bi;
                            Grayscale filtergs = new Grayscale(0.2125, 0.7154, 0.0721);

                            Bitmap grayImageth = filtergs.Apply(bm);
                            Threshold filterT = new Threshold(100);

                            filterT.ApplyInPlace(grayImageth);

                            snapshoot = ToBitmapImage(grayImageth);
                            break;

                        case 0:
                            Grayscale filter = new Grayscale(0.2125, 0.7154, 0.0721);
                            Bitmap grayImage = filter.Apply(bm);
                            snapshoot = ToBitmapImage(grayImage);
                            break;
                        case 1:
                            Invert filterin = new Invert();
                            Bitmap grayImage2 = filterin.Apply(bm);
                            snapshoot = ToBitmapImage(grayImage2);

                            break;
                        case 3:
                            snapshoot = bi;
                            Grayscale filtergrayscale = new Grayscale(0.2125, 0.7154, 0.0721);
                            Bitmap grayImageGS = filtergrayscale.Apply(bm);
                            SobelEdgeDetector filterS = new SobelEdgeDetector();
                            filterS.ApplyInPlace(grayImageGS);
                            snapshoot = ToBitmapImage(grayImageGS);
                            break;

                        case 4:
                            snapshoot = bi;
                            Grayscale filtergrayscale1 = new Grayscale(0.2125, 0.7154, 0.0721);
                            Bitmap grayImageGS1 = filtergrayscale1.Apply(bm);
                            Sharpen filterSh = new Sharpen();
                            filterSh.ApplyInPlace(grayImageGS1);
                            snapshoot = ToBitmapImage(grayImageGS1);
                            break;
                        case 5:
                            snapshoot = bi;
                            Grayscale filtergrayscale2 = new Grayscale(0.2125, 0.7154, 0.0721);
                            Bitmap grayImageGS2 = filtergrayscale2.Apply(bm);
                            GaussianBlur filterGB = new GaussianBlur(4, 11);
                            filterGB.ApplyInPlace(grayImageGS2);
                            snapshoot = ToBitmapImage(grayImageGS2);
                            break;
                        case 6:
                            snapshoot = bi;
                            Grayscale filtergrayscale3 = new Grayscale(0.2125, 0.7154, 0.0721);
                            Bitmap grayImageGS3 = filtergrayscale3.Apply(bm);
                            GaussianSharpen filterGSha = new GaussianSharpen(4, 11);
                            filterGSha.ApplyInPlace(grayImageGS3);
                            snapshoot = ToBitmapImage(grayImageGS3);
                            break;
                        case 7: //desde aqui Kernels
                            snapshoot = bi;
                            Grayscale filtergrayscale4 = new Grayscale(0.2125, 0.7154, 0.0721);
                            Bitmap grayImageGS4 = filtergrayscale4.Apply(bm);
                            Convolution filterconvo = new Convolution(Identidad);
                            filterconvo.ApplyInPlace(grayImageGS4);
                            snapshoot = ToBitmapImage(grayImageGS4);
                            break;
                        case 8:
                            snapshoot = bi;
                            Grayscale filtergrayscale5 = new Grayscale(0.2125, 0.7154, 0.0721);
                            Bitmap grayImageGS5 = filtergrayscale5.Apply(bm);
                            Convolution filterconvo1 = new Convolution(DeteccionBordes);
                            filterconvo1.ApplyInPlace(grayImageGS5);
                            snapshoot = ToBitmapImage(grayImageGS5);
                            break;
                        case 9:
                            snapshoot = bi;
                            Grayscale filtergrayscale6 = new Grayscale(0.2125, 0.7154, 0.0721);
                            Bitmap grayImageGS6 = filtergrayscale6.Apply(bm);
                            Convolution filterconvo2 = new Convolution(DeteccionBordes2);
                            filterconvo2.ApplyInPlace(grayImageGS6);
                            snapshoot = ToBitmapImage(grayImageGS6);
                            break;
                        case 10:
                            snapshoot = bi;
                            Grayscale filtergrayscale7 = new Grayscale(0.2125, 0.7154, 0.0721);
                            Bitmap grayImageGS7 = filtergrayscale7.Apply(bm);
                            Convolution filterconvo3 = new Convolution(Enfocar);
                            filterconvo3.ApplyInPlace(grayImageGS7);
                            snapshoot = ToBitmapImage(grayImageGS7);
                            break;

                        case 11:
                            snapshoot = bi;
                            Grayscale filtergrayscale8 = new Grayscale(0.2125, 0.7154, 0.0721);
                            Bitmap grayImageGS8 = filtergrayscale8.Apply(bm);
                            Convolution filterconvo4 = new Convolution(Desenfocar);
                            filterconvo4.ApplyInPlace(grayImageGS8);
                            snapshoot = ToBitmapImage(grayImageGS8);
                            break;
                        case 12:
                            snapshoot = bi;
                            Grayscale filtergrayscale9 = new Grayscale(0.2125, 0.7154, 0.0721);
                            Bitmap grayImageGS9 = filtergrayscale9.Apply(bm);
                            Convolution filterconvo5 = new Convolution(Desenfoque_Gaussiano);
                            filterconvo5.ApplyInPlace(grayImageGS9);
                            snapshoot = ToBitmapImage(grayImageGS9);
                            break;
                        case 13:
                            snapshoot = bi;
                            Grayscale filtergrayscale10 = new Grayscale(0.2125, 0.7154, 0.0721);
                            Bitmap grayImageGS10 = filtergrayscale10.Apply(bm);
                            Convolution filterconvo6 = new Convolution(Desenfoque_Gaussiano5);
                            filterconvo6.ApplyInPlace(grayImageGS10);
                            snapshoot = ToBitmapImage(grayImageGS10);
                            break;
                        case 14:
                            snapshoot = bi;
                            Grayscale filtergrayscale11 = new Grayscale(0.2125, 0.7154, 0.0721);
                            Bitmap grayImageGS11 = filtergrayscale11.Apply(bm);
                            Convolution filterconvo7 = new Convolution(MascaraDesenfoque);
                            filterconvo7.ApplyInPlace(grayImageGS11);
                            snapshoot = ToBitmapImage(grayImageGS11);
                            break;
                        case 15:
                            snapshoot = bi;
                            Grayscale filtergrayscale12 = new Grayscale(0.2125, 0.7154, 0.0721);
                            Bitmap grayImageGS12 = filtergrayscale12.Apply(bm);
                            Convolution filterconvo8 = new Convolution(Filtro1);
                            filterconvo8.ApplyInPlace(grayImageGS12);
                            snapshoot = ToBitmapImage(grayImageGS12);
                            break;
                        case 16:
                            snapshoot = bi;
                            Grayscale filtergrayscale13 = new Grayscale(0.2125, 0.7154, 0.0721);
                            Bitmap grayImageGS13 = filtergrayscale13.Apply(bm);
                            Convolution filterconvo9 = new Convolution(Filtro2);
                            filterconvo9.ApplyInPlace(grayImageGS13);
                            snapshoot = ToBitmapImage(grayImageGS13);
                            break;

                        case 17:
                            snapshoot = bi;



                            ColorFiltering filtercolor = new ColorFiltering();
                            Bitmap auxi =  filtercolor.Apply(bm);

                            //bitmapAUX = Grayscale.CommonAlgorithms.BT709.Apply(bitmapAUX);
                            // set color ranges to keep
                            int red = (int)(sld_red.Value);
                            int redmax = (int)(sld_redmax.Value);
                            int green = (int)(sld_green.Value);
                            int greenmax = (int)(sld_greenmax.Value);
                            int blue = (int)(sld_blue.Value);
                            int bluemax = (int)(sld_bluemax.Value);


                            filtercolor.Red = new IntRange(red, redmax);
                            filtercolor.Green = new IntRange(green, greenmax);
                            filtercolor.Blue = new IntRange(blue, bluemax);
                            // apply the filter

                            filtercolor.ApplyInPlace(auxi);
                            break;

                    }
                }

                bi.Freeze();
                Dispatcher.BeginInvoke(new ThreadStart(delegate { img_pantalla.Source = bi; img_filters.Source = snapshoot; }));

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        private void StartCamera()
        {
            CurrentDevice = VideoDevices[cb_VideoDevices.SelectedIndex];
            if (CurrentDevice != null)
            {

                _VideoSource = new VideoCaptureDevice(CurrentDevice.MonikerString);
                _VideoSource.VideoResolution = _VideoSource.VideoCapabilities[0];
                int a = _VideoSource.VideoCapabilities.Length;

                _VideoSource.NewFrame += video_NewFrame; //se suscribe al evento NewFrame

                for (int i = 0; i < _VideoSource.VideoCapabilities.Length; i++)
                {
                    string resolution_size = _VideoSource.VideoCapabilities[i].FrameSize.ToString();
                    cb_resolution.Items.Add(resolution_size);
                }
                _VideoSource.Start();
            }
        }
        public void GetVideoDevices()
        {
            VideoDevices = new ObservableCollection<FilterInfo>();
            foreach (FilterInfo filterInfo in new FilterInfoCollection(FilterCategory.VideoInputDevice))
            {
                VideoDevices.Add(filterInfo);
            }
            if (VideoDevices.Any())
            {
                CurrentDevice = VideoDevices[0];
                for (int i = 0; i < VideoDevices.Count; i++)
                {
                    cb_VideoDevices.Items.Add(VideoDevices[i].Name.ToString());
                }
            }
            else
            {
                MessageBox.Show("Error che");
            }
        }

        private void cb_VideoDevices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void cb_scales_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selector = cb_scales.SelectedIndex;
        }
        private void btnBlob_Click(object sender, RoutedEventArgs e)
        {
            //PARA COLOR FILTERING
            // create filter
    

            Bitmap bitmapAUX = BitmapImage2Bitmap(image_original);
            // apply the filter
            ColorFiltering filter = new ColorFiltering();
            //bitmapAUX = Grayscale.CommonAlgorithms.BT709.Apply(bitmapAUX);
            // set color ranges to keep
            int red = (int)(sld_red.Value);
            int redmax = (int)(sld_redmax.Value);
            int green = (int)(sld_green.Value);
            int greenmax = (int)(sld_greenmax.Value);
            int blue = (int)(sld_blue.Value);
            int bluemax = (int)(sld_bluemax.Value);


            filter.Red = new IntRange(red, redmax);
            filter.Green = new IntRange(green, greenmax);
            filter.Blue = new IntRange(blue, bluemax);
            // apply the filter

            filter.ApplyInPlace(bitmapAUX);
            img_filters.Source = ToBitmapImage(bitmapAUX);

            // create instance of blob counter
            BlobCounter blobCounter = new BlobCounter();
            // process input image
            blobCounter.ProcessImage(bitmapAUX);
            // get information about detected objects
            Blob[] blobs = blobCounter.GetObjectsInformation();
            BitmapImage image = ToBitmapImage(bitmapAUX);
            snapshoot =ToBitmapImage(bitmapAUX);

        }

        private void sld_red_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

        }
        public BitmapImage Threshold(BitmapImage img_ori, int umbralado)
        {
            Bitmap bitmapAUX = BitmapImage2Bitmap(img_ori);//colores
            Bitmap bitmapRESULT = new Bitmap((int)(bitmapAUX.Width), (int)(bitmapAUX.Height));
            int colorGRAY, newCOLOR;
            System.Drawing.Color c;
            for (int f = 0; f < bitmapAUX.Height; f++)
            {
                for (int col = 0; col < bitmapAUX.Width; col++)
                {
                    c = bitmapAUX.GetPixel(col, f);
                    colorGRAY = c.R;
                    if (colorGRAY >= umbralado) { newCOLOR = 255; }
                    else
                    {
                        newCOLOR = 0;
                    }
                    bitmapRESULT.SetPixel(col, f, System.Drawing.Color.FromArgb(newCOLOR, newCOLOR, newCOLOR));

                }
            }
            return ToBitmapImage(bitmapRESULT);
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //abrir imagen
            //Cargar imagen
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() == true)
            {
                string fileName = ofd.FileName;
                image_original = new BitmapImage(new Uri(fileName));
                img_image.Source = image_original;
            }
        }

        private void btnConvertir_Click(object sender, RoutedEventArgs e)
        {
            //sin aforge
            Bitmap bitmapAUX = BitmapImage2Bitmap(image_original);//colores
            bitmapAUX = Grayscale.CommonAlgorithms.RMY.Apply(bitmapAUX);
            image_gray = ToBitmapImage(bitmapAUX);

            image_gray = Threshold(image_gray, 104);



            Erosion filter2 = new Erosion();
            Bitmap aux2 = AForge.Imaging.Image.Clone(BitmapImage2Bitmap(image_gray), System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            filter2.ApplyInPlace(aux2);


            Dilatation filter1 = new Dilatation();
            // apply the filter
            Bitmap aux = AForge.Imaging.Image.Clone(aux2, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            filter1.ApplyInPlace(aux);
            filter1.ApplyInPlace(aux);
            // create filter
            BlobsFiltering filter = new BlobsFiltering();
             // configure filter
             Bitmap aux3 = AForge.Imaging.Image.Clone(aux, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
             filter.CoupledSizeFiltering = true;
             filter.MaxHeight = 55;
             filter.MaxWidth = 55;
             // apply the filter
             filter.ApplyInPlace(aux3);


            ConnectedComponentsLabeling filter5 = new ConnectedComponentsLabeling();
            // apply the filter

            Bitmap newImage = filter5.Apply(aux3);
            imagen_gray.Source = ToBitmapImage(newImage);
            imgReady = true;
            int objectCount = filter5.ObjectCount;
            lblcontar.Content = objectCount;

        }

        private void sldthreshold_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            umbral = (int)sldthreshold.Value;
            if (imgReady)
            {
                BitmapImage aux_original1 = Threshold(image_gray, umbral);

                imagen_gray.Source = aux_original1;
            }
        }

        private void btnDilatacion_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Dilatation filter1 = new Dilatation();
                // apply the filter
                Bitmap aux = AForge.Imaging.Image.Clone(BitmapImage2Bitmap(image_gray), System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                filter1.ApplyInPlace(aux);
                imagen_gray.Source = ToBitmapImage(aux);
            }
            catch (Exception)
            {

                throw;
            }

        }

        private void btnErocion_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Erosion filter2 = new Erosion();
                Bitmap aux2 = AForge.Imaging.Image.Clone(BitmapImage2Bitmap(image_gray), System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                filter2.ApplyInPlace(aux2);

                imagen_gray.Source = ToBitmapImage(aux2);
            }
            catch (Exception)
            {

                throw;
            }

        }

        private void btnLabeling_Click(object sender, RoutedEventArgs e)
        {
            ConnectedComponentsLabeling filter3 = new ConnectedComponentsLabeling();
            // apply the filter

            Bitmap newImage = filter3.Apply(BitmapImage2Bitmap(image_gray));
            imagen_gray.Source = ToBitmapImage(newImage);
            imgReady = true;
            int objectCount = filter3.ObjectCount;
            lblcontar.Content = objectCount;
        }
    }
}
