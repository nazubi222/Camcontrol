using System;
using System.Security.AccessControl;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfApp1;

namespace WebcamCaptureApp
{
    public partial class MainWindow : Window
    {

        private WebcamStreaming _webcamStreaming1;
        private WebcamStreaming _webcamStreaming2;
        private List<CameraDevice> cameraList;
        Point? lastCenterPositionOnTarget;
        Point? lastMousePositionOnTarget;
        Point? lastDragPoint;
        double zoomFactor = 1.2; // Adjust as needed

        public MainWindow()
        {
            InitializeComponent();
            cmbCameraDevices.ItemsSource = CameraDeviceEnumerator.GetAllConnectedCameras();
            cmbCameraDevices.SelectedIndex = 0;
            cameraList = CameraDeviceEnumerator.GetAllConnectedCameras();
        }

        private async void btnStart_Click(object sender, RoutedEventArgs e)
        {
            //chkFlip.IsEnabled = true;
            btnStop.IsEnabled = false;
            btnStart.IsEnabled = false;

            var cam1 = cameraList[2].OpenCvId;
            if (_webcamStreaming1 == null || _webcamStreaming1.CameraDeviceId != cam1)
            {
                _webcamStreaming1?.Dispose();
                _webcamStreaming1 = new WebcamStreaming(
                    imageControlForRendering: webcamPreview,
                    frameWidth: 300,
                    frameHeight: 300,
                    cameraDeviceId: cam1);
            }

            var cam2 = cameraList[0].OpenCvId;
            if (_webcamStreaming2 == null || _webcamStreaming2.CameraDeviceId != cam2)
            {
                _webcamStreaming2?.Dispose();
                _webcamStreaming2 = new WebcamStreaming(
                    imageControlForRendering: webcamPreview2,
                    frameWidth: 300,
                    frameHeight: 300,
                    cameraDeviceId: cam2);
            }


            try
            {
                await _webcamStreaming1.Start();
                await _webcamStreaming2.Start();
                btnStop.IsEnabled = true;
                btnStart.IsEnabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                btnStop.IsEnabled = false;
                btnStart.IsEnabled = true;
            }
        }

        private async void btnStop_Click(object sender, RoutedEventArgs e)
        {
            //chkFlip.IsEnabled = false;

            try
            {
                await _webcamStreaming1.Stop();
                await _webcamStreaming2.Stop();
                btnStop.IsEnabled = false;
                btnStart.IsEnabled = true;

                // To save the screenshot
                // var screenshot = _webcamStreaming.LastPngFrame;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _webcamStreaming1?.Dispose();
            _webcamStreaming2?.Dispose();
        }

        private void webcamPreview2_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            
            double maxZoom = 8;
            double newHeight = webcamPreview2.Height;
            double newWidth = webcamPreview2.Width;

            if (newHeight < maxZoom * 400)
            {
                newHeight *= zoomFactor;
                newWidth *= zoomFactor;
            }


            Point posNow = e.GetPosition(webcamPreview2);
            //MessageBox.Show(posNow.ToString());
            // Calculate normalized cursor position
            double normalizedCursorX = posNow.X / webcamPreview2.ActualWidth;
            double normalizedCursorY = posNow.Y / webcamPreview2.ActualHeight;

            // Calculate the difference between the cursor position and the center of the control
            double offsetX = posNow.X - webcamPreview2.ActualWidth / 2;
            double offsetY = posNow.Y - webcamPreview2.ActualHeight / 2;

            // Adjust the scroll position based on the zoom factor and differences
            double newHorizontalOffset = (normalizedCursorX * newWidth - posNow.X) * zoomFactor + offsetX;
            double newVerticalOffset = (normalizedCursorY * newHeight - posNow.Y) * zoomFactor + offsetY;

            webcamPreview2.Height = newHeight;
            webcamPreview2.Width = newWidth;

            // Adjust the scroll position to keep the zoom centered around the cursor position
            scrollViewer.ScrollToHorizontalOffset(newHorizontalOffset);
            scrollViewer.ScrollToVerticalOffset(newVerticalOffset);
            UpdateMaxOffsets(scrollViewer, newWidth/2, newHeight/5);

        }

        private void webcamPreview2_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            double minZoom = 1;
            double newHeight = webcamPreview2.Height;
            double newWidth = webcamPreview2.Width;

            if (newHeight > minZoom * 400)
            {
                newHeight /= zoomFactor;
                newWidth /= zoomFactor;
            }

            Point posNow = e.GetPosition(webcamPreview);

            double dX = posNow.X / webcamPreview2.ActualWidth;
            double dY = posNow.Y / webcamPreview2.ActualHeight;

            double newHorizontalOffset = (scrollViewer.HorizontalOffset + dX) * newWidth - dX * posNow.X;
            double newVerticalOffset = (scrollViewer.VerticalOffset + dY) * newHeight - dY * posNow.Y;

            webcamPreview2.Height = newHeight;
            webcamPreview2.Width = newWidth;

            // Adjust the scroll position to keep the zoom centered around the cursor position
            scrollViewer.ScrollToHorizontalOffset(newHorizontalOffset);
            scrollViewer.ScrollToVerticalOffset(newVerticalOffset);
        }

        private void webcamPreview2_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle)
            {
                var mousePos = e.GetPosition(scrollViewer);
                if (mousePos.X <= scrollViewer.ViewportWidth && mousePos.Y <
                    scrollViewer.ViewportHeight) //make sure we still can use the scrollbars
                {
                    scrollViewer.Cursor = Cursors.SizeAll;
                    lastDragPoint = mousePos;
                    Mouse.Capture(scrollViewer);
                }
            }
        }

        private void g(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle)
            {
                scrollViewer.Cursor = Cursors.Arrow;
                Mouse.Capture(null);
                lastDragPoint = null;
            }
        }

        private void scrollViewer_MouseMove(object sender, MouseEventArgs e)
        {
            if (lastDragPoint.HasValue)
            {
                Point posNow = e.GetPosition(scrollViewer);

                double dX = posNow.X - lastDragPoint.Value.X;
                double dY = posNow.Y - lastDragPoint.Value.Y;

                lastDragPoint = posNow;

                scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset - dX);
                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - dY);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            webcamPreview2.Height = 300;
            webcamPreview2.Width = 400;
        }

        private void UpdateMaxOffsets(ScrollViewer scrollViewer, double newMaxOffsetX, double newMaxOffsetY)
        {
            // Update the Tag property with the new maximum offsets
            scrollViewer.Tag = $"MaxOffsetX={newMaxOffsetX}, MaxOffsetY={newMaxOffsetY}";
        }

     
    }

}
    

