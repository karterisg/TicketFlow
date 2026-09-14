using System.Net;

namespace TicketFlow.API.Exceptions;


//it is used for handling bad requests in the API. When a bad request is encountered, this exception can be thrown with a specific message, which can then be caught and handled appropriately by the middleware or controller to return a meaningful response to the client.
public class BadRequestException : Exception
{
    public HttpStatusCode StatusCode => HttpStatusCode.BadRequest;
    public string Title => "Bad Request";

    public BadRequestException(string message) : base(message) { }
}