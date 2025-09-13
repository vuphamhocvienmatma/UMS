using Microsoft.AspNetCore.Mvc;
using Nest;
using UMS.Models.Documents;

namespace UMS.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly IElasticClient _elasticClient;
        private const string IndexName = "users";

        public DashboardController(IElasticClient elasticClient)
        {
            _elasticClient = elasticClient;
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchUsers(string query, int page = 1, int pageSize = 10)
        {
            var response = await _elasticClient.SearchAsync<UserDocument>(s => s
                .Index(IndexName)
                .From((page - 1) * pageSize)
                .Size(pageSize)
                .Query(q => q
                    .MultiMatch(mm => mm
                        .Query(query)
                        .Fields(f => f
                            .Field(p => p.FullName, 3)
                            .Field(p => p.Username)
                            .Field(p => p.Email)
                        )
                        .Fuzziness(Fuzziness.Auto)
                    )
                )
            );

            if (!response.IsValid)
            {
                return StatusCode(500, "Error querying Elasticsearch");
            }

            return Ok(response.Documents);
        }

        [HttpGet("users-by-role")]
        public async Task<IActionResult> GetUsersByRole(string role)
        {
            var response = await _elasticClient.SearchAsync<UserDocument>(s => s
                .Index(IndexName)
                .Query(q => q
                    .Term(t => t
                        .Field(f => f.Roles)
                        .Value(role)
                    )
                )
            );
            return Ok(response.Documents);
        }
    }
}
