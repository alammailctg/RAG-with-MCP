using System;
using System.Collections.Generic;
using System.Text;

namespace LocalRag.Domain.Model
{
    public class DocumentChunk
    {
        public long Id { get; set; }
        public string DocumentId { get; set; } = default!;
        public string? Title { get; set; }
        public string Content { get; set; } = default!;
        public float[] Embedding { get; set; } = default!;
        public DateTime CreatedAt { get; set; }
    }
}
