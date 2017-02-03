#if DEBUG
using _3DHologramPrototype.Common;
using System.Windows.Input;

namespace _3DHologramPrototype
{
    public partial class MainWindow
    {
        private void InitSandbox()
        {
            _gestureDetector = new GestureDetector(Grid);
            Grid.ManipulationDelta += uielement_ManipulationDelta;
        }

        GestureDetector _gestureDetector;

        private void uielement_ManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            {
                if (_gestureDetector.IsPanningAllowed)
                {
                    // Translate
                }
            }

            {
                if (_gestureDetector.IsScalingAllowed)
                {
                    // Scale
                }
            }

            {
                if (_gestureDetector.IsRotatingAllowed)
                {
                    // Rotate
                }
            }
        }
    }
}
#endif
