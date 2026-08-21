namespace LocalRagApi.DtoRequest
{
    public class AddDocumentRequest
    {
        public string DocumentId { get; set; } = string.Empty;

        public string? Title { get; set; }

        public string Content { get; set; } = string.Empty;
    }
}
