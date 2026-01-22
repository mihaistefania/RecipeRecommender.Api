using Microsoft.AspNetCore.Mvc;
using RecipeRecommender.Api.Models;
using RecipeRecommender.Api.Services;
using Recombee.ApiClient;
using Recombee.ApiClient.ApiRequests;
using Recombee.ApiClient.Bindings;
using Recombee.ApiClient.Util;

namespace RecipeRecommender.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RecommenderController : ControllerBase
    {
        private readonly RecombeeService _service;
        private readonly RecombeeClient _client;

        public RecommenderController()
        {
            _service = new RecombeeService();
            _client = new RecombeeClient("stefania-mihai-upb-prod", "2C1jMLiZqu7yi27FIDCrS4Xde1GOFsyxXECDSH1OkvqxEg7vO05qZM1OyU4hgAq6", region: Region.EuWest);

        }

        // ✅ Content-Based Recommendation
        [HttpGet("content-based/{recipeId}")]
        public IActionResult GetContentBasedRecommendations(string recipeId)
        {
            var result = _service.GetContentBasedRecommendations(recipeId, 5);
            return Ok(result);
        }

        // ✅ Collaborative Recommendation
        [HttpGet("collaborative/{userId}")]
        public IActionResult GetCollaborativeRecommendations(string userId)
        {
            var result = _service.GetCollaborativeRecommendations(userId, 5);
            return Ok(result);
        }

        [HttpPost("add")]
        public IActionResult AddRecipe([FromBody] RecipeModel recipe)
        {
            if (recipe == null || string.IsNullOrWhiteSpace(recipe.Recipe_Title))
                return BadRequest("Recipe title is required.");

            var id = _service.AddRecipe(recipe);
            return Ok($"✅ Recipe '{recipe.Recipe_Title}' added successfully with ID: {id}");
        }

        [HttpPost("schema/add-ingredients-text")]
        public IActionResult AddIngredientsTextProperty()
        {
            _client.Send(new AddItemProperty(
                "ingredients_text",
                "string"
            ));

            return Ok("ingredients_text property created");
        }

        [HttpPost("migrate/ingredients/all")]
        public IActionResult MigrateIngredientsForAllItems()
        {
            int batchSize = 500;   // safe batch size
            int offset = 0;

            int totalChecked = 0;
            int migrated = 0;
            int skipped = 0;

            while (true)
            {
                // 1. Fetch a batch of items
                var items = _client.Send(
                    new ListItems(
                        count: batchSize,
                        offset: offset
                    )
                );

                // Stop when no more items
                if (items == null || items.Count() == 0)
                    break;

                foreach (var item in items)
                {
                    totalChecked++;
                    var recipeId = item.ItemId;

                    // 2. Get item values (SETs included automatically)
                    var itemValues = _client.Send(
                        new GetItemValues(recipeId)
                    );

                    // 3. Validate ingredients
                    if (!itemValues.Values.ContainsKey("ingredients"))
                    {
                        skipped++;
                        continue;
                    }

                    var ingredientsSet =
    itemValues.Values["ingredients"] as IEnumerable<object>;

                    if (ingredientsSet == null || !ingredientsSet.Any())
                    {
                        skipped++;
                        continue;
                    }

                    var ingredientsText = string.Join(
    ", ",
    ingredientsSet.Select(i => i.ToString())
);

                    _client.Send(new SetItemValues(
                        recipeId,
                        new Dictionary<string, object>
                        {
        { "ingredients_text", ingredientsText }
                        }
                    ));

                    migrated++;
                }

                // 6. Move to next page
                offset += batchSize;
            }

            return Ok(new
            {
                totalChecked,
                migrated,
                skipped
            });
        }
        [HttpPost("migrate/ingredients/{recipeId}")]
        public IActionResult MigrateIngredientsForItem(string recipeId)
        {
            var itemValues = _client.Send(
                new GetItemValues(recipeId)
            );

            if (!itemValues.Values.ContainsKey("ingredients"))
                return NotFound("Ingredients not found");

            var ingredientsSet = itemValues.Values["ingredients"] as List<object>;

            if (ingredientsSet == null || ingredientsSet.Count == 0)
                return Ok("No ingredients to migrate");

            var ingredientsText = string.Join(
                ", ",
                ingredientsSet.Select(i => i.ToString())
            );

            _client.Send(new SetItemValues(
                recipeId,
                new Dictionary<string, object>
                {
            { "ingredients_text", ingredientsText }
                }
            ));

            return Ok(new
            {
                recipeId,
                ingredients_text = ingredientsText
            });
        }

        [HttpGet("popular")]
        public IActionResult GetMostPopular([FromQuery] int count = 5)
        {
            
                Console.WriteLine("⚠️ No data from Recombee, using dataset fallback...");
                var fallback = _service.GetTopRatedFromDataset(count);
                return Ok(fallback);
        }

        [HttpGet("bookmarked/{userId}")]
        public IActionResult GetBookmarkedRecipes(string userId)
        {
            try
            {
                // Get all bookmarked items for this user
                var bookmarks = _client.Send(new ListUserBookmarks(userId));

                var bookmarkedRecipes = new List<object>();

                foreach (var bookmark in bookmarks)
                {
                    // Fetch the full recipe info for each bookmarked item
                    var itemValues = _client.Send(new GetItemValues(bookmark.ItemId));

                    bookmarkedRecipes.Add(new
                    {
                        Id = bookmark.ItemId,
                        Values = itemValues.Values
                    });
                }

                return Ok(bookmarkedRecipes);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error fetching bookmarked recipes: {ex.Message}");
                return StatusCode(500, "Error retrieving bookmarked recipes.");
            }
        }
    }

}
