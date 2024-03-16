namespace WebApiClient.interfaces;

public class WatermarkSettings
{
    public Config Big { get; set; }
    public Config Small { get; set; }
    /// <summary>
    /// Use big configuration if size is above cutoff, otherwise use small configuration
    /// </summary>
    public int Cutoff { get; set; }
}