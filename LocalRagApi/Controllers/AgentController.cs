using LocalRag.Application.Interfaces;
using LocalRagApi.DtoRequest;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LocalRagApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AgentController : ControllerBase
    {
        private readonly IMcpAgentService _agent;
        public AgentController(IMcpAgentService agent)
        {
            _agent = agent;
        }

        [HttpPost("ask-duel-question")]
        public async Task<IActionResult> Ask([FromBody] AgentRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Question))
                return BadRequest("Question cannot be empty.");

            try
            {
                var answer = await _agent.AskAsync(request.Question, cancellationToken);

                return Ok(new
                {
                    Question = request.Question,
                    Answer = answer
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Message = ex.Message,
                    InnerException = ex.InnerException?.Message,
                    StackTrace = ex.StackTrace
                });
            }
        }
    }
}
