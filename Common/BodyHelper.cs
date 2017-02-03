using Microsoft.Kinect;

namespace _3DHologramPrototype.Common
{
    public static class BodyHelper
    {
        public static JointType GetActiveHand(Point3D leftHand, Point3D rightHand)
        {
            if (leftHand.Z > rightHand.Z)
                return JointType.HandLeft;
            return JointType.HandRight;
        }
    }
}
