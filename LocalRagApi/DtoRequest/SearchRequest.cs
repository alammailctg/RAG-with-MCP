namespace LocalRagApi.DtoRequest
{
    public class SearchRequest
    {
        public string Question { get; set; } = string.Empty;
        public int Limit { get; set; } = 5;
    }
}
