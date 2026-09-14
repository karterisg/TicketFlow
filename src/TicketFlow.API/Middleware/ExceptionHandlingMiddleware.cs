using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TicketFlow.API.Exceptions;

namespace TicketFlow.API.Middleware;

public class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context) //this runs for every request and catches any unhandled exceptions that occur during the request processing
    {
        var correlationId = Guid.NewGuid().ToString();

        context.Items["CorrelationId"] = correlationId;


        try
        {
            await next(context); //let the request continue normally to the next middleware in the pipeline
        }
        catch (Exception ex)//here we caught all of the exceptions and log it and return a proper response to the client
        {
            logger.LogError(ex, "unhandled exception  CorrelationId:{CorrelationId}", correlationId);//logs where the erros happens
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)//converts the exception into a proper HTTP response with a status code and a JSON body containing details about the error
    {
        var (statusCode, title) = exception switch
        {
            NotFoundException e => (e.StatusCode, e.Title),
            BadRequestException e => (e.StatusCode, e.Title),
            ConflictException e => (e.StatusCode, e.Title),
            ForbiddenException e => (e.StatusCode, e.Title),
            InvalidOperationException => (HttpStatusCode.BadRequest, "Invalid Operation"),
            KeyNotFoundException => (HttpStatusCode.NotFound, "Resource Not Found"),
            DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } } =>
                (HttpStatusCode.Conflict, "Duplicate Entry"),
            DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation } } =>
                (HttpStatusCode.Conflict, "Referenced record does not exist or is still in use"),
            _ => (HttpStatusCode.InternalServerError, "Internal Server Error")
        };




        var problemDetails = new
        {
            type = $"/errors/{(int)statusCode}",
            title,
            status = (int)statusCode,
            detail = exception.Message,
            instance = context.Request.Path.Value
        };

        context.Response.ContentType = "application/problem+json";//when an error occurs the response will be in the format of json
        context.Response.StatusCode = (int)statusCode;

        await context.Response.WriteAsync( //sending back to client
            JsonSerializer.Serialize(problemDetails));
    }
}