using LocalRag.Application.Features.Commands;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ProcurementAiApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DocumentsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public DocumentsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> Add([FromBody] AddDocumentCommand command,   CancellationToken cancellationToken)
        {
            var chunkCount = await _mediator.Send(command, cancellationToken);

            return Ok(new
            {
                Message = "Document indexed successfully.",
                ChunkCount = chunkCount
            });
        }
    }
}
