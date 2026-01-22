using Recombee.ApiClient;
using Recombee.ApiClient.ApiRequests;
using Recombee.ApiClient.Bindings;
using System.Globalization;
using CsvHelper;
using Recombee.ApiClient.Util;
using CsvHelper.Configuration;
using System.Text.RegularExpressions;
using RecipeRecommender.Api.Models;

namespace RecipeRecommender.Api.Services
{
    public class RecombeeService
    {
        private readonly RecombeeClient _client;

        public RecombeeService()
        {
           _client = new RecombeeClient("stefania-mihai-upb-prod", "2C1jMLiZqu7yi27FIDCrS4Xde1GOFsyxXECDSH1OkvqxEg7vO05qZM1OyU4hgAq6", region: Region.EuWest);
        }

        // 1️⃣ Create item properties
        public void CreateItemProperties()
        {
            var properties = new List<(string name, string type)>
            {
                ("recipe_title", "string"),
                ("url", "string"),
                ("record_health", "string"),
                ("vote_count", "int"),
                ("rating", "double"),
                ("description", "string"),
                ("cuisine", "string"),
                ("course", "string"),
                ("diet", "string"),
                ("prep_time", "string"),
                ("cook_time", "string"),
                ("ingredients", "set"),
                ("instructions", "string"),
                ("author", "string"),
                ("tags", "string"),
                ("category", "string")
            };

            foreach (var (name, type) in properties)
            {
                try
                {
                    _client.Send(new AddItemProperty(name, type));
                    Console.WriteLine($"✅ Property '{name}' added.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Property '{name}' exists. Skipping. ({ex.Message})");
                }
            }
        }

        // 2️⃣ Import recipes from CSV
        public void ImportRecipesFromCsv(string csvFilePath)
        {
            var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                PrepareHeaderForMatch = args => args.Header?.Trim().ToLower().Replace(" ", "_"),
                MissingFieldFound = null,
                HeaderValidated = null,
                BadDataFound = null,
            };

            using var reader = new StreamReader(csvFilePath);
            using var csv = new CsvReader(reader, csvConfig);
            var records = csv.GetRecords<dynamic>();

            foreach (var record in records)
            {
                string recipeTitle = record.recipe_title ?? "";

                // 🧩 sanitize itemId
                string itemId = Regex.Replace(
                    recipeTitle.ToLowerInvariant().Replace(" ", "_"),
                    @"[^a-z0-9_\-:@\.]", ""
                );

                var values = new Dictionary<string, object>
                {
                    { "recipe_title", record.recipe_title },
                    { "url", record.url },
                    { "record_health", record.record_health },
                    { "vote_count", TryParseInt(record.vote_count) },
                    { "rating", TryParseDouble(record.rating) },
                    { "description", record.description },
                    { "cuisine", record.cuisine },
                    { "course", record.course },
                    { "diet", record.diet },
                    { "prep_time", record.prep_time },
                    { "cook_time", record.cook_time },
                    { "ingredients", record.ingredients?.ToString().Split('|', StringSplitOptions.RemoveEmptyEntries) },
                    { "instructions", record.instructions },
                    { "author", record.author },
                    { "tags", record.tags },
                    { "category", record.category }
                };

                try
                {
                    _client.Send(new AddItem(itemId));
                    _client.Send(new SetItemValues(itemId, values, cascadeCreate: true));
                    Console.WriteLine($"✅ Added: {recipeTitle} → {itemId}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error adding {recipeTitle}: {ex.Message}");
                }
            }
        }

        // ✅ Content-Based Recommendation
        public List<Dictionary<string, object>> GetContentBasedRecommendations(string recipeId, int count = 5)
        {
            try
            {
                // Recombee va recomanda rețete similare cu cea dată
                var response = _client.Send(new RecommendItemsToItem(
                    recipeId,   // itemId = rețeta de referință
                    null,       // userId = null, deci pur content-based
                    count,
                    returnProperties: true
                ));

                var results = new List<Dictionary<string, object>>();

                foreach (var rec in response.Recomms)
                {
                    var item = new Dictionary<string, object>
                    {
                        { "id", rec.Id },
                        { "values", rec.Values }
                    };
                    results.Add(item);
                }

                return results;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error generating content-based recommendations: {ex.Message}");
                return new List<Dictionary<string, object>>();
            }
        }

        // ✅ Collaborative Filtering Recommendation
        public List<Dictionary<string, object>> GetCollaborativeRecommendations(string userId, int count = 5)
        {
            try
            {
                var request = new RecommendItemsToUser(
                    userId,
                    count,
                    returnProperties: true,  // include recipe details
                    diversity: 0.4,          // promote long-tail content
                    rotationRate: 0.2,       // vary results slightly each time
                    rotationTime: 3600       // remember rotation for 1 hour
                );

                var response = _client.Send(request);

                var results = new List<Dictionary<string, object>>();

                foreach (var rec in response.Recomms)
                {
                    var item = new Dictionary<string, object>
            {
                { "id", rec.Id },
                { "values", rec.Values }
            };
                    results.Add(item);
                }

                return results;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error generating collaborative recommendations: {ex.Message}");
                return new List<Dictionary<string, object>>();
            }
        }

        public string AddRecipe(RecipeModel recipe)
        {
            try
            {
                // Generate safe item ID (like your CSV import)
                string itemId = Regex.Replace(
                    recipe.Recipe_Title.ToLowerInvariant().Replace(" ", "_"),
                    @"[^a-z0-9_\-:@\.]", ""
                );

                // Add recipe item to Recombee
                _client.Send(new AddItem(itemId));

                // Prepare item values (matching dataset structure)
                var values = new Dictionary<string, object>
        {
            { "recipe_title", recipe.Recipe_Title },
            { "url", recipe.Url },
            { "record_health", recipe.Record_Health },
            { "vote_count", recipe.Vote_Count },
            { "rating", recipe.Rating },
            { "description", recipe.Description },
            { "cuisine", recipe.Cuisine },
            { "course", recipe.Course },
            { "diet", recipe.Diet },
            { "prep_time", recipe.Prep_Time },
            { "cook_time", recipe.Cook_Time },
            { "ingredients", recipe.Ingredients?.Split('|', StringSplitOptions.RemoveEmptyEntries) },
            { "instructions", recipe.Instructions },
            { "author", recipe.Author },
            { "tags", recipe.Tags },
            { "category", recipe.Category }
        };

                _client.Send(new SetItemValues(itemId, values, cascadeCreate: true));
                Console.WriteLine($"✅ Added new recipe: {recipe.Recipe_Title} → {itemId}");
                return itemId;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error adding recipe: {ex.Message}");
                throw;
            }
        }

        public List<RecipeModel> GetTopRatedFromDataset(int count = 5)
        {
            string path = Path.Combine(AppContext.BaseDirectory, "DataSet", "food_recipes.csv");

            var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                PrepareHeaderForMatch = args => args.Header?.Trim().ToLower().Replace(" ", "_"),
                MissingFieldFound = null
            };

            using var reader = new StreamReader(path);
            using var csv = new CsvReader(reader, csvConfig);
            var allRecords = csv.GetRecords<RecipeModel>().ToList();

            // ✅ Filter to keep only English titles (no foreign characters)
            var englishOnly = allRecords.Where(r =>
                !string.IsNullOrWhiteSpace(r.Recipe_Title) &&
                Regex.IsMatch(r.Recipe_Title, @"^[a-zA-Z0-9\s\p{P}]+$")
            );

            var records = englishOnly
                .OrderByDescending(r => r.Rating)
                .ThenByDescending(r => r.Vote_Count)
                .Take(count)
                .ToList();

            Console.WriteLine($"✅ Returning {records.Count} top-rated English recipes from dataset.");
            return records;
        }

        /*public List<RecipeModel> GetTopRatedFromDataset(int count = 5)
        {
            string path = Path.Combine(AppContext.BaseDirectory, "DataSet", "food_recipes.csv");

            var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                PrepareHeaderForMatch = args => args.Header?.Trim().ToLower().Replace(" ", "_"),
                MissingFieldFound = null
            };

            using var reader = new StreamReader(path);
            using var csv = new CsvReader(reader, csvConfig);
            var records = csv.GetRecords<RecipeModel>()
                             .OrderByDescending(r => r.Rating)
                             .ThenByDescending(r => r.Vote_Count)
                             .Take(count)
                             .ToList();

            Console.WriteLine($"✅ Returning {records.Count} top-rated recipes from dataset.");
            return records;
        }*/

        private int TryParseInt(object value) =>
            int.TryParse(value?.ToString(), out var result) ? result : 0;

        private double TryParseDouble(object value) =>
            double.TryParse(value?.ToString(), out var result) ? result : 0.0;
    }
}
