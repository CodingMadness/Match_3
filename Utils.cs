using System.Numerics;

namespace Match_3
{
    public static class Utils
    {
        /// <summary>
        /// Rounds a random number to a value which has no remainder
        /// </summary>
        /// <param name="rnd"></param>
        /// <param name="r"></param>
        /// <param name="divisor"></param>
        /// <returns></returns>
        public static int Round(Random rnd, Range r, int divisor)
        {
            if (r.Start.Value > r.End.Value)
            {
                r = new Range(r.End, r.Start);
            }

            int value = rnd.Next(r.Start.Value, r.End.Value);
            value = value % divisor == 0  ? value : ((int)MathF.Round(value / divisor)) * divisor;
            return value;
        }

        private static FastNoiseLite noiseMaker = new(DateTime.UtcNow.GetHashCode());

        static Utils()
        {
            noiseMaker.SetFrequency(10f);
            noiseMaker.SetFractalType(FastNoiseLite.FractalType.PingPong);
            noiseMaker.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        }
        public static FastNoiseLite NoiseMaker => noiseMaker;

        private static float RoundTo2Digits(this float value)
        {
             value = value = (float)Math.Round(value * 100f) / 100f;
             value = MathF.Round(value, 2);
             return value < 0 ? -value : value;
        }
        
        public static float Trunc(this float value, int digits)
        {
            float mult = MathF.Pow(10.0f, digits);
            float result = MathF.Truncate( mult * value ) / mult;
            return  result < 0 ? -result : result;;
        }
        public static float GetNewNoise(Vector2 coord, float noiseToIgnore)
        {
            float baseNoise = noiseMaker.GetNoise(coord.X, coord.Y).Trunc(2);
            noiseToIgnore = noiseToIgnore.Trunc(2);
            
            while (baseNoise == noiseToIgnore)
            {
                baseNoise = noiseMaker.GetNoise(coord.X, coord.Y).Trunc(2);
            }

            return baseNoise;
        }
    }
}
