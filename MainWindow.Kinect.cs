using _3DHologramPrototype.Common;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace _3DHologramPrototype
{
    public partial class MainWindow
    {
        #region properties

        private const int MovementTrigger = 10;

        public int LastLeftX { get; private set; }

        public int LastRightX { get; private set; }

        public int LastLeftY { get; private set; }

        public int LastRightY { get; private set; }

        private const double HandSize = 30;

        private const double JointThickness = 3;

        private const double ClipBoundsThickness = 10;

        private const float InferredZPositionClamp = 0.1f;

        private readonly Brush _handClosedBrush = new SolidColorBrush(Color.FromArgb(128, 255, 0, 0));

        private readonly Brush _handOpenBrush = new SolidColorBrush(Color.FromArgb(128, 0, 255, 0));

        private readonly Brush _handLassoBrush = new SolidColorBrush(Color.FromArgb(128, 0, 0, 255));

        private readonly Brush _trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));
        
        private readonly Brush _inferredJointBrush = Brushes.Yellow;
      
        private readonly Pen _inferredBonePen = new Pen(Brushes.Gray, 1);

        private DrawingGroup _drawingGroup;

        private DrawingImage _imageSource;

        private KinectSensor _kinectSensor = null;

        private CoordinateMapper _coordinateMapper = null;

        private BodyFrameReader _bodyFrameReader = null;

        private Body[] _bodies = null;

        private List<Tuple<JointType, JointType>> _bones;

        private int _displayWidth;

        private int _displayHeight;

        private List<Pen> _bodyColors;

        public ImageSource ImageSource
        {
            get { return _imageSource; }
        }

        #endregion

        #region handlers

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (_bodyFrameReader != null)
               _bodyFrameReader.FrameArrived += Reader_FrameArrived;
        }

        private void Reader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            bool dataReceived = false;

            using (var bodyFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {
                    if (_bodies == null)
                        _bodies = new Body[bodyFrame.BodyCount];

                    bodyFrame.GetAndRefreshBodyData(_bodies);
                    dataReceived = true;
                }
            }

            if (dataReceived)
            {
                using (var dc = _drawingGroup.Open())
                {
                    dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, _displayWidth, _displayHeight));

                    int penIndex = 0;
                    foreach (var body in _bodies)
                    {
                        var drawPen = _bodyColors[penIndex++];

                        if (body.IsTracked)
                        {
                            DrawClippedEdges(body, dc);

                            var joints = body.Joints;

                           var jointPoints = new Dictionary<JointType, Point3D>();

                            foreach (var jointType in joints.Keys)
                            {
                                // sometimes the depth(Z) of an inferred joint may show as negative
                                // clamp down to 0.1f to prevent coordinatemapper from returning (-Infinity, -Infinity)
                                CameraSpacePoint position = joints[jointType].Position;
                                if (position.Z < 0)
                                    position.Z = InferredZPositionClamp;
                                
                                var depthSpacePoint = _coordinateMapper.MapCameraPointToDepthSpace(position);
                                jointPoints[jointType] = new Point3D(depthSpacePoint.X, depthSpacePoint.Y, position.Z);
                            }

                            DrawBody(joints, jointPoints, dc, drawPen);

                            var activeHand = BodyHelper.GetActiveHand(jointPoints[JointType.HandLeft],
                                jointPoints[JointType.HandRight]);

                            if(activeHand == JointType.HandLeft)
                                DrawLeftHand(activeHand, body.HandLeftState, jointPoints[JointType.HandLeft], dc, JointType.HandLeft);
                            else if (activeHand == JointType.HandRight)
                                DrawRightHand(activeHand, body.HandRightState, jointPoints[JointType.HandRight], dc, JointType.HandRight);
                        }
                    }

                    _drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, _displayWidth, _displayHeight));
                }
            }
        }

        #endregion

        #region methods

        public void SetupKinect()
        {
            #region body declaration

            _bones = new List<Tuple<JointType, JointType>>();

            // Torso
            _bones.Add(new Tuple<JointType, JointType>(JointType.Head, JointType.Neck));
            _bones.Add(new Tuple<JointType, JointType>(JointType.Neck, JointType.SpineShoulder));
            _bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.SpineMid));
            _bones.Add(new Tuple<JointType, JointType>(JointType.SpineMid, JointType.SpineBase));
            _bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderRight));
            _bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderLeft));
            _bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipRight));
            _bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipLeft));

            // Right Arm
            _bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderRight, JointType.ElbowRight));
            _bones.Add(new Tuple<JointType, JointType>(JointType.ElbowRight, JointType.WristRight));
            _bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.HandRight));
            _bones.Add(new Tuple<JointType, JointType>(JointType.HandRight, JointType.HandTipRight));
            _bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.ThumbRight));

            // Left Arm
            _bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderLeft, JointType.ElbowLeft));
            _bones.Add(new Tuple<JointType, JointType>(JointType.ElbowLeft, JointType.WristLeft));
            _bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.HandLeft));
            _bones.Add(new Tuple<JointType, JointType>(JointType.HandLeft, JointType.HandTipLeft));
            _bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.ThumbLeft));

            // Right Leg
            _bones.Add(new Tuple<JointType, JointType>(JointType.HipRight, JointType.KneeRight));
            _bones.Add(new Tuple<JointType, JointType>(JointType.KneeRight, JointType.AnkleRight));
            _bones.Add(new Tuple<JointType, JointType>(JointType.AnkleRight, JointType.FootRight));

            // Left Leg
            _bones.Add(new Tuple<JointType, JointType>(JointType.HipLeft, JointType.KneeLeft));
            _bones.Add(new Tuple<JointType, JointType>(JointType.KneeLeft, JointType.AnkleLeft));
            _bones.Add(new Tuple<JointType, JointType>(JointType.AnkleLeft, JointType.FootLeft));

            // populate body colors, one for each BodyIndex
            _bodyColors = new List<Pen>();

            _bodyColors.Add(new Pen(Brushes.Red, 6));
            _bodyColors.Add(new Pen(Brushes.Orange, 6));
            _bodyColors.Add(new Pen(Brushes.Green, 6));
            _bodyColors.Add(new Pen(Brushes.Blue, 6));
            _bodyColors.Add(new Pen(Brushes.Indigo, 6));
            _bodyColors.Add(new Pen(Brushes.Violet, 6));

            #endregion

            #region sensor setup

            _kinectSensor = KinectSensor.GetDefault();
            _coordinateMapper = _kinectSensor.CoordinateMapper;

            var frameDescription = _kinectSensor.DepthFrameSource.FrameDescription;

            _displayWidth = frameDescription.Width;
            _displayHeight = frameDescription.Height;

            _bodyFrameReader = _kinectSensor.BodyFrameSource.OpenReader();

            _kinectSensor.IsAvailableChanged += (s, a) => Console.WriteLine(a.IsAvailable ? "Kinect available" : "Kinect not available");

            _kinectSensor.Open();

            #endregion

            #region bitmap

            _drawingGroup = new DrawingGroup();

            _imageSource = new DrawingImage(_drawingGroup);

            #endregion

            #region wiring

            DataContext = this;

            Loaded += MainWindow_Loaded;

            #endregion
        }

        private void DrawBody(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point3D> jointPoints, DrawingContext drawingContext, Pen drawingPen)
        {
            foreach (var bone in _bones)
                DrawBone(joints, jointPoints, bone.Item1, bone.Item2, drawingContext, drawingPen);

            foreach (var jointType in joints.Keys)
            {
                Brush drawBrush = null;

                var trackingState = joints[jointType].TrackingState;

                if (trackingState == TrackingState.Tracked)
                    drawBrush = _trackedJointBrush;
                else if (trackingState == TrackingState.Inferred)
                    drawBrush = _inferredJointBrush;

                if (drawBrush != null)
                    drawingContext.DrawEllipse(drawBrush, null, jointPoints[jointType].Point2D, JointThickness, JointThickness);
                
            }
        }

        private void DrawBone(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point3D> jointPoints, JointType jointType0, 
            JointType jointType1, DrawingContext drawingContext, Pen drawingPen)
        {
            var joint0 = joints[jointType0];
            var joint1 = joints[jointType1];

            if (joint0.TrackingState == TrackingState.NotTracked ||
                joint1.TrackingState == TrackingState.NotTracked)
                return;

            Pen drawPen = _inferredBonePen;
            if ((joint0.TrackingState == TrackingState.Tracked) && (joint1.TrackingState == TrackingState.Tracked))
                drawPen = drawingPen;

            drawingContext.DrawLine(drawPen, jointPoints[jointType0].Point2D, jointPoints[jointType1].Point2D);
        }

        private MovementDirection GetDirection(MovementDirection defaultDirection, MovementDirection altDirection, double newPos, double oldPos, int trigger, out double delta)
        {
            var direction = defaultDirection;

            delta = newPos - oldPos;
            if (delta < 0)
            {
                direction = MovementDirection.Left;
                delta *= -1;
            }
            return direction;
        }

        private void DrawRightHand(JointType activeHand, HandState handState, Point3D handPosition, DrawingContext drawingContext, JointType jointType)
        {
            DrawHand(handState, handPosition, drawingContext);

            if (activeHand != jointType)
                return;

            _mouseDown = handState == HandState.Closed;

            //horizontal
            double delta;
            var direction = GetDirection(MovementDirection.Right, MovementDirection.Left, handPosition.X, LastRightX, out delta);

            if (!_mouseDown || delta < MovementTrigger)
                VarZ = 0;

            if (delta >= MovementTrigger)
            {
                LastRightX = Convert.ToInt32(handPosition.X);
                InvokeKinectMovement(direction, delta);
            }

            //vertical
            direction = GetDirection(MovementDirection.Up, MovementDirection.Down, handPosition.Y, LastRightY, out delta);

            if (!_mouseDown || delta < MovementTrigger)
                VarX = 0;

            if (delta >= MovementTrigger)
            {
                LastRightY = Convert.ToInt32(handPosition.Y);
                InvokeKinectMovement(direction, delta);
            }
        }

        private void DrawLeftHand(JointType activeHand, HandState handState, Point3D handPosition, DrawingContext drawingContext, JointType jointType)
        {
            DrawHand(handState, handPosition, drawingContext);
            if (activeHand != jointType)
                return;

            _mouseDown = handState == HandState.Closed;

            //horizontal
            double delta;
            var direction = GetDirection(MovementDirection.Right, MovementDirection.Left, handPosition.X, LastLeftX, out delta);
         
            if (!_mouseDown || delta < MovementTrigger)
                VarZ = 0;
            
            if (delta >= MovementTrigger)
            {
                LastRightX = Convert.ToInt32(handPosition.X);
                InvokeKinectMovement(direction, delta);
            }

            //vertical
            direction = GetDirection(MovementDirection.Up, MovementDirection.Down, handPosition.Y, LastLeftY, out delta);

            if (!_mouseDown || delta < MovementTrigger)
                VarX = 0;

            if (delta >= MovementTrigger)
            {
                LastRightY = Convert.ToInt32(handPosition.Y);
                InvokeKinectMovement(direction, delta);
            }
        }

        private void DrawHand(HandState handState, Point3D handPosition, DrawingContext drawingContext)
        {
            switch (handState)
            {
                case HandState.Closed:
                    drawingContext.DrawEllipse(_handClosedBrush, null, handPosition.Point2D, HandSize, HandSize);
                    break;
                case HandState.Open:
                    drawingContext.DrawEllipse(_handOpenBrush, null, handPosition.Point2D, HandSize, HandSize);
                    break;
                case HandState.Lasso:
                    drawingContext.DrawEllipse(_handLassoBrush, null, handPosition.Point2D, HandSize, HandSize);
                    break;
            }
        }

        private void DrawClippedEdges(Body body, DrawingContext drawingContext)
        {
            var clippedEdges = body.ClippedEdges;

            if (clippedEdges.HasFlag(FrameEdges.Bottom))
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, _displayHeight - ClipBoundsThickness, _displayWidth, ClipBoundsThickness));
            

            if (clippedEdges.HasFlag(FrameEdges.Top))
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, _displayWidth, ClipBoundsThickness));
            

            if (clippedEdges.HasFlag(FrameEdges.Left))
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, ClipBoundsThickness, _displayHeight));
            

            if (clippedEdges.HasFlag(FrameEdges.Right))
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(_displayWidth - ClipBoundsThickness, 0, ClipBoundsThickness, _displayHeight));
        }

        #endregion
    }
}
