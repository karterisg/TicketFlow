using System.Net;

namespace TicketFlow.API.Exceptions;

public class ForbiddenException : Exception
{
    public HttpStatusCode StatusCode => HttpStatusCode.Forbidden;
    public string Title => "Forbidden";

    public ForbiddenException(string message) : base(message) { }
}
