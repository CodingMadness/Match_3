namespace Match_3.GameTypes;

public interface IScalable
{
    public Scale factor { get; set; }
    public GameTime ScaleTimer { get; }
    public Shape Body { get; }
    
}