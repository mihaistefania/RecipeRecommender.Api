namespace RecipeRecommender.Api.Models
{
    public class UserModel
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public int Age { get; set; }
        public string DietType { get; set; }       // ex: Vegan, Vegetarian, etc.
        public string PreferredCuisine { get; set; } // ex: Italian, Mexican
        public int AvailableTime { get; set; }     // minute disponibile pentru gătit
    }
}