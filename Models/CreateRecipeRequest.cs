namespace RecipeRecommender.Api.Models
{
    public class CreateRecipeRequest
    {
        public string RecipeId { get; set; }
        public string RecipeTitle { get; set; }
        public string Author { get; set; }
        public string Category { get; set; }
        public string Cuisine { get; set; }
        public string Course { get; set; }
        public string Diet { get; set; }
        public string Description { get; set; }
        public string CookTime { get; set; }
        public string PrepTime { get; set; }

        public double? Rating { get; set; }
        public int? VoteCount { get; set; }

        public List<string> Ingredients { get; set; }
        public List<string> Tags { get; set; }

        public string Url { get; set; }
    }
}
