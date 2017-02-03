using System;
using System.Windows;

namespace _3DHologramPrototype.Common
{
    public class GestureDetector
    {
        private readonly uint _pixelPerCm = 38;
        private bool _isGestureDetected = false;

        public bool IsPanningAllowed { get; private set; }
        public bool IsScalingAllowed { get; private set; }
        public bool IsRotatingAllowed { get; private set; }

        public GestureDetector(FrameworkElement uiElement)
        {
            IsPanningAllowed = false;
            IsScalingAllowed = false;
            IsRotatingAllowed = false;

            uiElement.ManipulationStarted += (sender, args) =>
            {
                IsPanningAllowed = true;
            };

            double scale = 0.0d;
            double rot = 0.0d;

            uiElement.ManipulationDelta += (sender, args) =>
            {
                const double MIN_SCALE_TRIGGER = 0.05;
                const int MIN_ROTATIONANGLE_TRIGGER_DEGREE = 10;
                const int MIN_FINGER_DISTANCE_FOR_ROTATION_CM = 2;

                var manipulatorBounds = Rect.Empty;
                foreach (var manipulator in args.Manipulators)
                {
                    manipulatorBounds.Union(manipulator.GetPosition(sender as IInputElement));
                }

                var distance = (manipulatorBounds.TopLeft - manipulatorBounds.BottomRight).Length;
                var distanceInCm = distance / _pixelPerCm;

                scale += 1 - (args.DeltaManipulation.Scale.Length / Math.Sqrt(2));

                rot += args.DeltaManipulation.Rotation;

                if (Math.Abs(scale) > MIN_SCALE_TRIGGER && Math.Abs(rot) < MIN_ROTATIONANGLE_TRIGGER_DEGREE)
                {
                    ApplyScaleMode();
                }

                if (Math.Abs(rot) >= MIN_ROTATIONANGLE_TRIGGER_DEGREE && distanceInCm > MIN_FINGER_DISTANCE_FOR_ROTATION_CM)
                {
                    ApplyRotationMode();
                }
            };

            uiElement.ManipulationCompleted += (sender, args) =>
            {
                scale = 0.0d;
                rot = 0.0d;
                IsPanningAllowed = false;
                IsScalingAllowed = false;
                IsRotatingAllowed = false;
                _isGestureDetected = false;
            };
        }

        private void ApplyScaleMode()
        {
            if (!_isGestureDetected)
            {
                _isGestureDetected = true;
                IsPanningAllowed = true;
                IsScalingAllowed = true;
                IsRotatingAllowed = false;
            }
        }

        private void ApplyRotationMode()
        {
            if (!_isGestureDetected)
            {
                _isGestureDetected = true;
                IsPanningAllowed = true;
                IsScalingAllowed = true;
                IsRotatingAllowed = true;
            }
        }
    }
}
