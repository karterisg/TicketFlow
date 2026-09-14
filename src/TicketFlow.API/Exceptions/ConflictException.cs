using System.Net;

namespace TicketFlow.API.Exceptions;

public class ConflictException : Exception
{
    public HttpStatusCode StatusCode => HttpStatusCode.Conflict;
    public string Title => "Conflict";

    public ConflictException(string message) : base(message) { }
}