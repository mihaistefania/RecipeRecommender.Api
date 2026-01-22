namespace RecipeRecommender.Api.Models
{
    public class RecipeModel
    {
        public string Recipe_Title { get; set; }
        public string Url { get; set; }
        public string Record_Health { get; set; }
        public int Vote_Count { get; set; }
        public double Rating { get; set; }
        public string Description { get; set; }
        public string Cuisine { get; set; }
        public string Course { get; set; }
        public string Diet { get; set; }
        public string Prep_Time { get; set; }
        public string Cook_Time { get; set; }
        public string Ingredients { get; set; } // comma- or | separated
        public string Instructions { get; set; }
        public string Author { get; set; }
        public string Tags { get; set; }
        public string Category { get; set; }
    }
}