using AForge.Video;
using AForge.Video.DirectShow;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
using AForge.Imaging.Filters;
using AForge.Imaging;
using AForge.Vision.Motion;
using AForge.Imaging.Textures;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Timers;

namespace LaserTargetTracker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private ObservableCollection<Shot> ShotList = new ObservableCollection<Shot>();
        private DateTime lastShotTime = DateTime.Now;
        CameraManager cameraManager = new CameraManager();
        private VideoCaptureDevice Camera;
        private bool tracking = false;
        ThresholdedDifference diffFilter = new ThresholdedDifference(95);
        IFilter erosionFilter = new Erosion();
        BlobCounter blobCounter = new BlobCounter();
        bool shotCaptured = false;

        Random random = new Random();
        System.Timers.Timer StartTimer = new System.Timers.Timer();

        private bool NewBackground = false;
        private Bitmap BackgroundFrame;
        private Bitmap CurrentFrame;
        double px = 0;
        double py = 0;

        public MainWindow()
        {
            InitializeComponent();
            foreach(FilterInfo cam in cameraManager.Cameras)
            {
                CameraCombo.Items.Add(cam.Name);
            }
            CameraCombo.SelectedIndex = 0;
            Camera = new VideoCaptureDevice(cameraManager.Cameras[CameraCombo.SelectedIndex].MonikerString);
            Camera.NewFrame += new NewFrameEventHandler(Camera_NewFrame);
            Camera.Start();
            ShotDataGrid.ItemsSource = ShotList;
            ShotList.CollectionChanged += UpdateStatusLabel;
            StartTimer.Elapsed += StartTrackingEvent;
        }

        private void UpdateStatusLabel(object sender, NotifyCollectionChangedEventArgs e)
        {
            StatusLabel.Content = "Count: " + ShotList.Count; 
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }

        private void CameraCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Camera = new VideoCaptureDevice(cameraManager.Cameras[CameraCombo.SelectedIndex].MonikerString);
            Camera.NewFrame += new NewFrameEventHandler(Camera_NewFrame);
            Camera.Start();
        }

        // Event called by webcam source
        private void Camera_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            CurrentFrame = (Bitmap)eventArgs.Frame.Clone();
            Bitmap img0;

            Dispatcher.Invoke(() =>//new ThreadStart(delegate
            {
                if (NewBackground && CurrentFrame.Width > 0)
                {
                    BackgroundFrame = CurrentFrame;
                    diffFilter.OverlayImage = BackgroundFrame;
                    NewBackground = false;
                    Console.Beep();
                    lastShotTime = DateTime.Now;
                }
                else if (BackgroundFrame != null && !NewBackground && tracking)
                {
                    img0 = diffFilter.Apply(CurrentFrame);
                    img0 = erosionFilter.Apply(img0);
                    blobCounter.ProcessImage(img0);
                    if (blobCounter.ObjectsCount == 1 && !shotCaptured)
                    {
                        // we got a shot
                        int tx = img0.Width;
                        int ty = img0.Height;
                        int x = blobCounter.GetObjectsRectangles()[0].Location.X;
                        int y = blobCounter.GetObjectsRectangles()[0].Location.Y;

                        string time = DateTime.Now.ToString("HH:mm:ss:ff");

                        double dt = (DateTime.Now - lastShotTime).TotalSeconds;
                        lastShotTime = DateTime.Now;
                        Shot shot = new Shot(ShotList.Count, x, y, time, dt);

                        var dot = new Ellipse();
                        dot.Fill = System.Windows.Media.Brushes.Red;
                        dot.Stroke = System.Windows.Media.Brushes.Red;
                        dot.Height = 5;
                        dot.Width = 5;
                        var coords = PixelToCoord(x, y, tx, ty);
                        dot.Margin = new Thickness(coords[0], coords[1], 0, 0);

                        // I think this is what is bogging the frame rate down
                        Application.Current.Dispatcher.Invoke((Action)delegate
                        {
                            ShotList.Add(shot);
                            DisplayCanvas.Children.Add(dot);
                        });
                        shotCaptured = true;
                    }
                    else if (blobCounter.ObjectsCount == 0)
                    {
                        shotCaptured = false;
                    }
                }
            });
            Dispatcher.Invoke(() =>
            {
                // Here is where the frame is assigned to the UI display image
                OutputDisplayImage.Source = ToSource(CurrentFrame);
            });
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Environment.Exit(0);
        }

        private void CaptureBackgroundButton_Click(object sender, RoutedEventArgs e)
        {
            if (tracking)
            {
                tracking = false;
                CaptureBackgroundButton.Background = System.Windows.Media.Brushes.Green;
                CaptureBackgroundButton.Content = "Start Tracking";
                CameraViewBorder.BorderBrush = System.Windows.Media.Brushes.Transparent;

            }
            else
            {
                CaptureBackgroundButton.Background = System.Windows.Media.Brushes.Orange;
                CaptureBackgroundButton.Content = "Stand-By";
                StartTimer.Interval = 5000 + random.Next(0, 4000);
                StartTimer.Start();
            }

        }

        private void StartTrackingEvent(object sender, ElapsedEventArgs e)
        {
            StartTimer.Stop();
            NewBackground = true;
            tracking = true;

            Dispatcher.Invoke(() => 
            {
                CaptureBackgroundButton.Background = System.Windows.Media.Brushes.Red;
                CaptureBackgroundButton.Content = "Stop Tracking";
                CameraViewBorder.BorderBrush = System.Windows.Media.Brushes.Red;
            });
        }

        private BitmapImage ToSource(Bitmap img)
        {
            MemoryStream ms = new MemoryStream();
            img.Save(ms, ImageFormat.Bmp);
            ms.Seek(0, SeekOrigin.Begin);
            BitmapImage bi = new BitmapImage();
            bi.BeginInit();
            bi.StreamSource = ms;
            bi.EndInit();
            bi.Freeze();
            return bi;
        }

        private double[] PixelToCoord(int x, int y, int tx, int ty)
        {
            py = OutputDisplayImage.Height;
            px = OutputDisplayImage.Width;
            double[] result = new double[2];
            result[0] = ((x * px) / tx);
            result[1] = ((y * py) / ty);
            return result;
        }

        private void ClearListButton_Click(object sender, RoutedEventArgs e)
        {
            ShotList.Clear();
            var dots = DisplayCanvas.Children.OfType<Ellipse>().ToList();
            foreach(var dot in dots)
            {
                DisplayCanvas.Children.Remove(dot);
            }
        }
    }
}
