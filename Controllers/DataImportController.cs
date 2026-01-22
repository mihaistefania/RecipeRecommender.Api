using Microsoft.AspNetCore.Mvc;
using RecipeRecommender.Api.Services;

namespace RecipeRecommender.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DataImportController : ControllerBase
    {
        private readonly RecombeeService _recombeeService;

        public DataImportController()
        {
            _recombeeService = new RecombeeService();
        }

        [HttpPost("import")]
        public IActionResult Import()
        {
            string path = Path.Combine(AppContext.BaseDirectory, "DataSet", "food_recipes.csv");
            _recombeeService.CreateItemProperties();
            _recombeeService.ImportRecipesFromCsv(path);
            return Ok("✅ Recipes imported successfully into Recombee!");
        }
    }
}
