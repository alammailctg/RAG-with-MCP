using System;
using System.Collections.Generic;
using System.Text;

namespace LocalRag.Application.DTOs
{
    public class DocumentChunkDto
    {
        public long Id { get; set; }
        public string DocumentId { get; set; } = default!;
        public string? Title { get; set; }
        public string Content { get; set; } = default!;
        public double Distance { get; set; }
    }
}
