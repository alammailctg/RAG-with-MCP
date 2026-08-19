using System;
using System.Collections.Generic;
using System.Text;

namespace LocalRag.Application.DTOs
{
    public class RagAnswerDto
    {
        public string Question { get; set; } = default!;
        public string Answer { get; set; } = default!;
        public IReadOnlyList<DocumentChunkDto> Sources { get; set; } = [];
    }
}
