using System.Net;

namespace TicketFlow.API.Exceptions;

public class NotFoundException : Exception
{
    public HttpStatusCode StatusCode => HttpStatusCode.NotFound;
    public string Title => "Resource Not Found";

    public NotFoundException(string entityName, int id)
        : base($"{entityName} with id {id} was not found.") { }
}