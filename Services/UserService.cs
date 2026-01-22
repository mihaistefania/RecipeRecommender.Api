using RecipeRecommender.Api.Models;
using Recombee.ApiClient;
using Recombee.ApiClient.ApiRequests;
using Recombee.ApiClient.Util;
using System.Text.RegularExpressions;

namespace RecipeRecommender.Api.Services
{
    public class UserService
    {
        private readonly RecombeeClient _client;

        public UserService()
        {
            _client = new RecombeeClient("stefania-mihai-upb-prod", "2C1jMLiZqu7yi27FIDCrS4Xde1GOFsyxXECDSH1OkvqxEg7vO05qZM1OyU4hgAq6", region: Region.EuWest);
        }

        public void CreateUserProperties()
        {
            try
            {
                _client.Send(new AddUserProperty("FirstName", "string"));
                _client.Send(new AddUserProperty("LastName", "string"));
                _client.Send(new AddUserProperty("Email", "string"));
                _client.Send(new AddUserProperty("Age", "int"));
                _client.Send(new AddUserProperty("Diet_Preference", "string"));
                _client.Send(new AddUserProperty("Preferred_Cuisine", "string"));
                _client.Send(new AddUserProperty("Available_Time", "int"));

                Console.WriteLine("✅ User properties created successfully in Recombee!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error creating user properties: {ex.Message}");
            }
        }

        public string AddUser(UserModel user)
        {
            try
            {
                // 🔹 Generăm automat un userId unic
                string userId = $"{Guid.NewGuid().ToString()}";

                // 1️⃣ Adaugă utilizatorul în Recombee
                _client.Send(new AddUser(userId));

                // 2️⃣ Setează proprietățile utilizatorului
                var values = new Dictionary<string, object>
                {
                    { "FirstName", user.FirstName },
                    { "LastName", user.LastName },
                    { "Email", user.Email },
                    { "Age", user.Age },
                    { "Diet_Preference", user.DietType },
                    { "Preferred_Cuisine", user.PreferredCuisine },
                    { "Available_Time", user.AvailableTime }
                };

                _client.Send(new SetUserValues(userId, values, cascadeCreate: true));

                Console.WriteLine($"✅ Added user {userId} ({user.FirstName} {user.LastName})");
                return userId;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error adding user {user.FirstName}: {ex.Message}");
                throw;
            }
        }

        // 4️⃣ Record that a user rated a recipe
        public void AddRating(string userId, string recipeId, double rating)
        {
            try
            {
                _client.Send(new AddRating(userId, recipeId, rating, cascadeCreate: true));
                Console.WriteLine($"⭐ {userId} rated {recipeId} → {rating}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to add rating: {ex.Message}");
            }
        }

        // 5️⃣ Record a “like” or “favorite”
        public void AddBookmark(string userId, string recipeId)
        {
            try
            {
                _client.Send(new AddBookmark(userId, recipeId, cascadeCreate: true));
                Console.WriteLine($"❤️ {userId} bookmarked {recipeId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to add bookmark: {ex.Message}");
            }
        }

        // 6️⃣ Record that a user viewed a recipe (for implicit learning)
        public void AddView(string userId, string recipeId)
        {
            try
            {
                _client.Send(new AddDetailView(userId, recipeId, cascadeCreate: true));
                Console.WriteLine($"👁️ {userId} viewed {recipeId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to add view: {ex.Message}");
            }
        }

        public void GenerateSampleInteractions(List<string> userIds, List<string> recipeIds, int interactionsPerUser = 10)
        {
            var rand = new Random();
            Console.WriteLine("🔁 Generating simulated interactions...");

            foreach (var userId in userIds)
            {
                for (int i = 0; i < interactionsPerUser; i++)
                {
                    string recipeId = recipeIds[rand.Next(recipeIds.Count)];

                    // Random rating (1–5)
                    double rating = Math.Round(rand.NextDouble() * 4 + 1, 1);
                    double normalized = (rating - 3.0) / 2.0; // Normalize to [-1, 1]

                    _client.Send(new AddRating(userId, recipeId, normalized, cascadeCreate: true));
                    Console.WriteLine($"⭐ {userId} rated {recipeId} = {rating} (normalized: {normalized:F2})");

                    // 50% chance to bookmark
                    if (rand.NextDouble() < 0.5)
                    {
                        _client.Send(new AddBookmark(userId, recipeId, cascadeCreate: true));
                        Console.WriteLine($"❤️ {userId} bookmarked {recipeId}");
                    }

                    // 80% chance to add a view
                    if (rand.NextDouble() < 0.8)
                    {
                        _client.Send(new AddDetailView(userId, recipeId, cascadeCreate: true));
                        Console.WriteLine($"👁️ {userId} viewed {recipeId}");
                    }
                }
            }

            Console.WriteLine("✅ Finished generating sample interactions!");
        }

        public void AddRandomUsers(int count = 20)
        {
            var rand = new Random();

            // Example data pools
            string[] firstNames = { "Andrei", "Maria", "Elena", "Toma", "Stefan", "Andrada", "Bianca", "Vlad", "Radu", "Ioana", "George", "Daria", "Lucian", "Mihai", "Ana", "Cristina", "Alex", "Iulia", "Teodor", "Roxana" };
            string[] lastNames = { "Popescu", "Ionescu", "Dumitrescu", "Marinescu", "Stan", "Enache", "Matei", "Iliescu", "Voicu", "Radu", "Tudor", "Dragan", "Oprea", "Stoica", "Cojocaru", "Andrei", "Petrescu", "Neagu", "Sima", "Costache" };
            string[] diets = { "Vegetarian", "Vegan", "Non-Vegetarian", "Keto", "Low-Carb", "Pescatarian" };
            string[] cuisines = { "Italian", "Mexican", "Indian", "French", "Romanian", "Greek", "Japanese", "Thai", "Chinese", "Spanish" };

            Console.WriteLine($"👥 Adding {count} random users to Recombee...");

            for (int i = 1; i <= count; i++)
            {
                var firstName = firstNames[rand.Next(firstNames.Length)];
                var lastName = lastNames[rand.Next(lastNames.Length)];
                var diet = diets[rand.Next(diets.Length)];
                var cuisine = cuisines[rand.Next(cuisines.Length)];
                var age = rand.Next(18, 60);
                var availableTime = rand.Next(10, 90); // minutes available for cooking
                var email = $"{firstName.ToLower()}.{lastName.ToLower()}{rand.Next(100, 999)}@example.com";

                // Auto-generate unique userId
                string userId = Regex.Replace($"{Guid.NewGuid().ToString()}", @"[^a-z0-9_\-]", "");

                // Add user
                _client.Send(new AddUser(userId));

                // Set user properties
                var values = new Dictionary<string, object>
        {
            { "FirstName", firstName },
            { "LastName", lastName },
            { "Email", email },
            { "Age", age },
            { "Diet_Preference", diet },
            { "Preferred_Cuisine", cuisine },
            { "Available_Time", availableTime }
        };

                _client.Send(new SetUserValues(userId, values, cascadeCreate: true));

                Console.WriteLine($"✅ Added user {userId} → {firstName} {lastName}, {diet}, prefers {cuisine}");
            }

            Console.WriteLine("🎉 Finished adding random users to Recombee!");
        }

        public void DeleteAllUsers()
        {
            try
            {
                Console.WriteLine("🚨 Fetching all users from Recombee...");

                // Get all user IDs directly from Recombee
                var users = _client.Send(new ListUsers()).ToList();

                Console.WriteLine($"Found {users.Count} users.");

                foreach (var userId in users)
                {
                    try
                    {
                        _client.Send(new DeleteUser(userId.UserId));
                        Console.WriteLine($"🗑️ Deleted user: {userId}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠️ Failed to delete user {userId}: {ex.Message}");
                    }
                }

                Console.WriteLine("✅ All users deleted successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error while deleting users: {ex.Message}");
                throw;
            }
        }
    }
}
