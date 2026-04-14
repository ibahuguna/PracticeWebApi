using Microsoft.AspNetCore.Http.HttpResults;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

var todos = new List<Todo>();

app.MapGet("/todos", () => todos);

app.MapGet("/todos/{id}", Results<Ok<Todo>, NotFound> (int id) =>
{
    var targetTodo = todos.SingleOrDefault(t => id == t.Id);
    return targetTodo is null
    ? TypedResults.NotFound()
    : TypedResults.Ok(targetTodo);
});

app.MapPost("/todos", (Todo task) =>
{
    todos.Add(task);
    return TypedResults.Created($"/todos/{task.Id}", task);
});

app.MapDelete("/todos/{id}", (int id) =>
{
    var removed = todos.RemoveAll(t => id == t.Id);
    return (removed == 0)?
         Results.NotFound() :
         Results.NoContent();
});

app.Run();

public record Todo(int Id, string Name, DateTime DueDate, bool IsCompleted);

