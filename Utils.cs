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
            int value = rnd.Next(r.Start.Value, r.End.Value);
            value = value % divisor == 0  ? value : ((int)MathF.Round(value / divisor)) * divisor;
            return value;
        }
    }
}
