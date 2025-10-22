namespace Tranzor.Models.GraphQL;

public class GraphQLRequest
{
    public string Query { get; set; }
    public object Variables { get; set; }
    public string OperationName { get; set; }
}