using LocalRag.Application.Features.Commands;
using LocalRag.Application.Features.Queries;
using LocalRagApi.DtoRequest;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LocalRagApi.Controllers
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

        [HttpPost("add-doc")]
        public async Task<IActionResult> Add(AddDocumentRequest request, CancellationToken cancellationToken)
        {
            var command = new AddDocumentCommand(
                request.DocumentId,
                request.Title,
                request.Content);

            var chunkCount = await _mediator.Send(command, cancellationToken);

            return Ok(new
            {
                Message = "Document indexed successfully.",
                ChunkCount = chunkCount
            });
        }

        [HttpPost("ask")]
        public async Task<IActionResult> Ask([FromBody] AskRagQuery query,   CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                query,
                cancellationToken);

            return Ok(result);
        }
    }
}
