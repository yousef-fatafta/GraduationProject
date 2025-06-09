namespace Onlink.Models
{
    public class CVComparisonResult
    {
        public string FileName { get; set; } = string.Empty;
        public double SimilarityScore { get; set; }
        public string MatchedJobTitle { get; set; } = string.Empty;
    }
}
