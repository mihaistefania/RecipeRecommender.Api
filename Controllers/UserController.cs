using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using RecipeRecommender.Api.Models;
using RecipeRecommender.Api.Services;
using Recombee.ApiClient.ApiRequests;
using Recombee.ApiClient.Util;
using Recombee.ApiClient;

namespace RecipeRecommender.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly UserService _userService;

        public UserController()
        {
            _userService = new UserService();
        }

        [HttpPost("create-properties")]
        public IActionResult CreateUserProperties()
        {
            _userService.CreateUserProperties();
            return Ok("✅ User properties created in Recombee.");
        }

        [HttpPost("add")]
        public IActionResult AddUser([FromBody] UserModel user)
        {
            if (user == null)
                return BadRequest("Invalid user data.");

            var newUserId = _userService.AddUser(user);

            return Ok(new
            {
                message = $"✅ User {user.FirstName} {user.LastName} added successfully!",
                userId = newUserId
            });
        }


        // POST api/user/rating
        [HttpPost("rating")]
        public IActionResult AddRating([FromQuery] string userId, [FromQuery] string recipeId, [FromQuery] double rating)
        {
            _userService.AddRating(userId, recipeId, rating);
            return Ok($"⭐ {userId} rated {recipeId} = {rating}");
        }

        // POST api/user/bookmark
        [HttpPost("bookmark")]
        public IActionResult AddBookmark([FromQuery] string userId, [FromQuery] string recipeId)
        {
            _userService.AddBookmark(userId, recipeId);
            return Ok($"❤️ {userId} bookmarked {recipeId}");
        }

        // POST api/user/view
        [HttpPost("view")]
        public IActionResult AddView([FromQuery] string userId, [FromQuery] string recipeId)
        {
            _userService.AddView(userId, recipeId);
            return Ok($"👁️ {userId} viewed {recipeId}");
        }

        [HttpPost("simulate-interactions")]
        public IActionResult SimulateInteractions()
        {
            var userService = new UserService();

            // Example: 10 users you already have in Recombee
            var userIds = new List<string>
            {
                "03cc4948-0bff-4715-88de-faca1406249e", "112fe484-de0b-4103-861f-f66f1bab7048",
                "1178f01f-e756-473b-96b7-4a05a30304cf", "13d1654b-9505-4660-8ec6-6c2ba7808a47",
                "1d280022-1f15-443b-9604-1bfabfa1372e", "31399e59-f552-4602-adff-92c68a225595",
                "493862aa-ef70-44bf-a67a-32e900839af6", "5d3687ab-a0af-481a-a759-debdd93e712a",
                "630dae7e-3c60-4fb5-a7b7-552cd4f3a6b0", "85f5b97c-3a75-44aa-9373-7a6d8baa3b92"
            };

                    // Get your recipe IDs — adjust to match how you generate them in import
                    var recipeIds = new List<string>
            {
                "avocado_and_almond_salad_recipe_with_feta_cheese",
                "sweet_potato_avocado_brownie_bites_recipe",
                "pita_topped_with_oven_roasted_cauliflower_basil_avocado_sauce_recipe",
                "15_minute_cauliflower_fried_rice_recipe",
                "15_minutes_mexican_fried_rice_recipe",
                "8_ingredient_sugar_free_granola_parfait_recipe",
                "almond_and_cashew_nut_mushroom_curry_recipe",
                "almond_butter__fig_muesli_bar_recipe",
                "almond_meal_muffins_recipe_gluten_free_and_grain_free",
                "quick_bread_pizza_recipe"
            };

            // Generate ~10 interactions per user
            userService.GenerateSampleInteractions(userIds, recipeIds, interactionsPerUser: 10);

            return Ok("✅ Sample interactions generated successfully!");
        }

        // ✅ POST /api/User/add-random
        [HttpPost("add-random")]
        public IActionResult AddRandomUsers([FromQuery] int count = 20)
        {
            _userService.AddRandomUsers(count);
            return Ok($"✅ {count} random users added successfully to Recombee!");
        }

        // ✅ DELETE /api/User/delete-all
        [HttpDelete("delete-all")]
        public IActionResult DeleteAllUsers()
        {
            _userService.DeleteAllUsers();
            return Ok("✅ All users deleted from Recombee!");
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] RecipeRecommender.Api.Models.LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
                return BadRequest("Email is required.");

            try
            {
                // Get all users from Recombee
                var client = new RecombeeClient("stefania-mihai-upb-prod", "2C1jMLiZqu7yi27FIDCrS4Xde1GOFsyxXECDSH1OkvqxEg7vO05qZM1OyU4hgAq6", region: Region.EuWest);
                var users = client.Send(new ListUsers());

                // Find a user with the given email
                foreach (var u in users)
                {
                    var userValues = client.Send(new GetUserValues(u.UserId));
                    if (userValues.Values.ContainsKey("Email") &&
                        userValues.Values["Email"].ToString().Equals(request.Email, StringComparison.OrdinalIgnoreCase))
                    {
                        return Ok(new
                        {
                            message = "✅ Login successful!",
                            userId = u.UserId,
                            firstName = userValues.Values.GetValueOrDefault("FirstName"),
                            lastName = userValues.Values.GetValueOrDefault("LastName"),
                            dietPreference = userValues.Values.GetValueOrDefault("Diet_Preference"),
                            preferredCuisine = userValues.Values.GetValueOrDefault("Preferred_Cuisine")
                        });
                    }
                }

                return NotFound("❌ No user found with this email.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"⚠️ Error during login: {ex.Message}");
            }
        }
    }
}
