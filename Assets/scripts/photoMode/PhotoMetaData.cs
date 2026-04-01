
[System.Serializable]
public class PhotoMetadata
{
    public string photoID; // Matches the PNG filename (e.g., "photo_20260401_150530")
    public string timestamp;
    public float colorScore;
    public float saturationScore;
    public float contrastScore;
    public float symmetryScore;
    public float totalScore;
}
