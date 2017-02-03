namespace _3DHologramPrototype.Common
{
    public enum AnimationSpeed
    {
        VeryFast,
        Fast,
        Medium,
        Slow
    }

    public static class SpeedMap
    {
        public static int GetSpeed(AnimationSpeed s)
        {
            switch (s)
            {
                case AnimationSpeed.VeryFast:
                    return 5;
                case AnimationSpeed.Fast:
                    return 20;
                case AnimationSpeed.Medium:
                    return 50;
            }
            return 250;
        }
    }
}
