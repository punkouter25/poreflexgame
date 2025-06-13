using PoReflexGame.Api.Models; // Update this using directive
using PoReflexGame.Api.Services; // Update this using directive
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PoReflexGame.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HighScoresController : ControllerBase
    {
        private readonly AzureTableStorageService _azureTableStorageService;

        public HighScoresController(AzureTableStorageService azureTableStorageService)
        {
            _azureTableStorageService = azureTableStorageService;
        }

        [HttpPost]
        public async Task<IActionResult> PostHighScore([FromBody] HighScore highScore)
        {
            if (highScore == null || string.IsNullOrWhiteSpace(highScore.PlayerName))
            {
                return BadRequest("Player name and score are required.");
            }

            highScore.PartitionKey = "HighScores";
            highScore.RowKey = $"{highScore.Score:D10}-{Guid.NewGuid()}";
            highScore.Timestamp = DateTimeOffset.UtcNow;

            await _azureTableStorageService.AddEntityAsync("highscores", highScore); // Use the service
            return CreatedAtAction(nameof(GetHighScores), new { }, highScore);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<HighScore>>> GetHighScores([FromQuery] int top = 10)
        {
            var highScores = await _azureTableStorageService.GetEntitiesAsync<HighScore>(
                "highscores", 
                filter: "PartitionKey eq 'HighScores'", 
                topN: top, 
                orderBy: "Score desc"); // Use the service and order by score

            return Ok(highScores.OrderByDescending(hs => hs.Score).Take(top)); // Ensure sorting is correct
        }
    }
}
