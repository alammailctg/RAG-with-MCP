using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LocalRagApi.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using LocalRag.Infrastructure.MCPServers;

    namespace ProcurementAiApi.Controllers
    {
        [ApiController]
        [Route("api/database")]
        public class DatabaseController : ControllerBase
        {
            private readonly DatabaseTools _databaseTools;
            public DatabaseController(
                DatabaseTools databaseTools)
            {
                _databaseTools = databaseTools;
            }

            [HttpPost("query")]
            public async Task<IActionResult> ExecuteQuery([FromBody] DatabaseQueryRequest request)
            {
                if (string.IsNullOrWhiteSpace(request.Sql))
                {
                    return BadRequest(
                        "SQL query cannot be empty");
                }

                var result = await _databaseTools.ExecuteQuery(request.Sql);

                return Ok(new
                {
                    Data = result
                });
            }
        }



        public class DatabaseQueryRequest
        {
            public string Sql { get; set; } = string.Empty;
        }
    }
}
