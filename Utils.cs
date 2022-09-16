namespace Match_3
{
    public static class Utils
    {
        public static int RoundValueToNearestOf3(Random rnd, Range r)
        {
            const float toRoundTo = 3.0f;
            int value = rnd.Next(r.Start.Value, r.End.Value);
            value = (int)(value % toRoundTo == 0 ? value : ((int)MathF.Round(value / toRoundTo)) * toRoundTo);
            return value;
        }
    }
}
