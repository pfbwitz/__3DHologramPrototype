using _3DHologramPrototype.Common;
using HelixToolkit.Wpf;
using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace _3DHologramPrototype
{
    public partial class MainWindow : Window
    {
        #region properties

        private bool _isAnimating;

        private double AutoAnimationSpeed = 1;

        private System.Windows.Forms.Timer _timer = null;

        private double _gridSize;
      
        private string _filename;

        private bool _mouseDown;

        private bool _running = true;

        private double _lastMouseX;

        private int _mouseWheelUp;

        private int _mouseWheelDown;

        private double _varX;
        public double VarX
        {
            get { return _varX; }
            set
            {
                _varX = value;
                if (_varX < 0)
                    _varX = 359;
                else if (_varZ > 360)
                    _varX = 1;
            }
        }

        private double _varY;
        public double VarY
        {
            get { return _varY; }
            set
            {
                _varY = value;
                if (_varY < 0)
                    _varY = 359;
                else if (_varZ > 360)
                    _varY = 1;
            }
        }

        private double _varZ;
        public double VarZ
        {
            get { return _varZ; }
            set
            {
                _varZ = value;
                if (_varZ < 0)
                    _varZ = 359;
                else if (_varZ > 360)
                    _varZ = 1;
            }
        }

        public double ZoomFactor = 1;

        #endregion

        public MainWindow()
        {
            InitializeComponent();

            #region choose file

            var dialog = new OpenFileDialog {
                InitialDirectory = AppDomain.CurrentDomain.BaseDirectory + "Models\\",
                Filter = "StereoLithography models (*.stl)|*.stl|Wavefront models (*.obj)|*.obj|Lightwave models (*.lwo)|*.lwo|" +
                    "3D Studio models (*.3ds)|*.3ds|All files (*.*)|*.*"
            };
          
            if (dialog.ShowDialog().Value)
                _filename = dialog.FileName;
            else
                Close();

            #endregion

            #region layout

            KeyDown += MainWindow_KeyDown;

            SetGridSize(SystemParameters.PrimaryScreenHeight);

            SkeletonImage.Visibility = App.IsKinectMode ? Visibility.Visible : Visibility.Collapsed;
#if !DEBUG
            SkeletonImage.Visibility = Visibility.Collapsed;
#endif

            #endregion

            #region load model

            if (string.IsNullOrEmpty(_filename))
            {
                Close();
                return;
            }

            var file = _filename;
            var content = new ModelImporter().Load(file);
            ((GeometryModel3D)content.Children[0]).Material = new DiffuseMaterial(new SolidColorBrush(Colors.White));
            Top.Content = content;
            Bottom.Content = content;
            Left.Content = content;
            Right.Content = content;

            viewPort3dRight.Loaded += (s, a) => Common.CameraController.SetCameraView(viewPort3dRight, Common.CameraController.eCameraViews.Right, 0);
            viewPort3dLeft.Loaded += (s, a) => Common.CameraController.SetCameraView(viewPort3dLeft, Common.CameraController.eCameraViews.Right, 0);
            viewPort3dBottom.Loaded += (s, a) => Common.CameraController.SetCameraView(viewPort3dBottom, Common.CameraController.eCameraViews.Right, 0);
            viewPort3dTop.Loaded += (s, a) => Common.CameraController.SetCameraView(viewPort3dTop, Common.CameraController.eCameraViews.Right, 0);

            #endregion

            #region event wiring

            WireClicks(this);
            WireClicks(viewPort3dRight);

            PreviewMouseWheel += MainWindow_PreviewMouseWheel;

            #endregion

            #region kinect

            if (App.IsKinectMode)
                SetupKinect();

            #endregion

            #region mouse

            if (!App.IsKinectMode)
            {
                _lastMouseX = Mouse.GetPosition(this).X;
                Task.Run(async () =>
                {
                    var trigger = 10;
                    while (_running)
                    {
                        if (!_running)
                            break;
                        await Task.Delay(1);
                        await Dispatcher.BeginInvoke(new Action(() =>
                        {
                            var direction = MovementDirection.Right;
                            var position = Mouse.GetPosition(this).X;
                            var delta = position - _lastMouseX;
                            if (delta < 0)
                            {
                                delta *= -1;
                                direction = MovementDirection.Left;
                            }
                            if (delta % trigger < 3 || !_mouseDown)
                            {
                                if (_isAnimating)
                                    VarZ = AutoAnimationSpeed;
                                return;
                            }

                            if (position >= _lastMouseX - 10 && position <= _lastMouseX + 10)
                            {
                                VarZ = _isAnimating ? AutoAnimationSpeed : 0;
                                Reset(delta);
                            }
                            else if (direction == MovementDirection.Left)
                                MoveLeft(delta);
                            else
                                MoveRight(delta);
                            _lastMouseX = position;
                        }));
                    }
                });
            }

            #endregion

#if DEBUG
            InitSandbox();
#endif
            //Animate(AnimationSpeed.Fast, MovementDirection.Right);
        }

        #region Transformation

        private void SetGridSize(double size)
        {
            _gridSize = size;
            Grid.Width = size;
            Grid.Height = size;

            foreach (var row in Grid.RowDefinitions)
                row.Height = new GridLength(size / Grid.RowDefinitions.Count);

            foreach (var col in Grid.ColumnDefinitions)
                col.Width = new GridLength(size / Grid.ColumnDefinitions.Count);

            SkeletonBox.Width = SkeletonBox.Height = size / Grid.RowDefinitions.Count;
        }

        public void InvokeKinectMovement(MovementDirection direction, double delta)
        {
            if (_mouseDown)
            {
                switch (direction)
                {
                    case MovementDirection.Left:
                        MoveLeft(delta);
                        break;
                    case MovementDirection.Right:
                        MoveRight(delta);
                        break;
                }
            }
        }

        private void Reset(double delta)
        {
            RotateZ(Top);
            RotateZ(Bottom);
            RotateZ(Left);
            RotateZ(Right);
        }

        private void MoveLeft(double delta)
        {
            VarZ--;
            //VarZ = -10;
            Reset(delta);
        }

        private void MoveRight(double delta)
        {
            VarZ++;
            //VarZ = 10;
            Reset(delta);
        }

        private void RotateX(ModelVisual3D model)
        {
            var axis = new Vector3D(1, 0, 0);
            Rotate(VarX, model, axis);
        }

        private void RotateY(ModelVisual3D model)
        {
            var axis = new Vector3D(0, 1, 0);
            Rotate(VarY, model, axis);
        }

        private void RotateZ(ModelVisual3D model)
        {
            var axis = new Vector3D(0, 0, 1);

            Rotate(VarZ, model, axis);
        }

        private void Rotate(double angle, ModelVisual3D model, Vector3D vector)
        {
            var matrix = model.Transform.Value;
            matrix.Rotate(new Quaternion(vector, angle));
            model.Transform = new MatrixTransform3D(matrix);
        }

        private void Zoom(ModelVisual3D model, Vector3D scale)
        {
            var matrix = model.Transform.Value;
            matrix.Scale(scale);
            model.Transform = new MatrixTransform3D(matrix);
        }

        private void Animate(AnimationSpeed speed, MovementDirection direction)
        {
            _isAnimating = true;
            var interval = SpeedMap.GetSpeed(speed);
            if (direction == MovementDirection.Right)
                AutoAnimationSpeed = -AutoAnimationSpeed;
            _timer = new System.Windows.Forms.Timer { Interval = interval };
            VarZ = AutoAnimationSpeed;
            _timer.Tick += (s, a) => Reset(0);
            _timer.Start();
        }

        #endregion

        #region setup

        private void WireClicks(UIElement control)
        {
            control.MouseDown += (s, a) => _mouseDown = true;
            control.MouseUp += (s, a) => {
                _mouseDown = false;
                VarZ = 0;
            };
        }

        #endregion

        #region handlers

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            var upperBounds = SystemParameters.PrimaryScreenHeight;
            var lowerBounds = SystemParameters.PrimaryScreenHeight / 4;

            if (e.Key == Key.A)
                SetGridSize(_gridSize - 10 >= lowerBounds ? _gridSize - 10 : lowerBounds);
            else if (e.Key == Key.D)
                SetGridSize(_gridSize + 10 <= upperBounds ? _gridSize + 10 : upperBounds);
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            _running = false;

            if(_timer != null)
            {
                _timer.Stop();
                _timer.Dispose();
            }

            if (_bodyFrameReader != null)
            {
                _bodyFrameReader.Dispose();
                _bodyFrameReader = null;
            }

            if (_kinectSensor != null)
            {
                _kinectSensor.Close();
                _kinectSensor = null;
            }
        }

        private void MainWindow_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            //if (e.Delta > 0)
            //{
            //    _mouseWheelUp++;
            //    _mouseWheelDown = 0;
            //}
            //if (e.Delta < 0)
            //{
            //    _mouseWheelDown++;
            //    _mouseWheelUp = 0;
            //}

            //if (e.Delta > 0)
            //    ZoomFactor -= _mouseWheelUp >= 1 ? 0.05 : 0;
            //else
            //    ZoomFactor += _mouseWheelDown >= 1 ? 0.05 : 0;

            //if (ZoomFactor < 1 || ZoomFactor > 3)
            //    ZoomFactor = 1;

            //if(_mouseWheelUp > 10 || _mouseWheelDown > 10)
            //{

            if (e.Delta < 0)
                ZoomFactor += 0.01;
            else
                ZoomFactor -= 0.01;

            Zoom(Top, new Vector3D(ZoomFactor, ZoomFactor, ZoomFactor));
            Zoom(Bottom, new Vector3D(ZoomFactor, ZoomFactor, ZoomFactor));
            Zoom(Left, new Vector3D(ZoomFactor, ZoomFactor, ZoomFactor));
            Zoom(Right, new Vector3D(ZoomFactor, ZoomFactor, ZoomFactor));
            //}
        }

        #endregion
    }
}
