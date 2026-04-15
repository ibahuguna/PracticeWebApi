using Microsoft.AspNetCore.Http.HttpResults;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

var todos = new List<Todo>();

app.Use(async (context, next) =>
    {
        var start = DateTime.UtcNow;
        await next();
        var duration = DateTime.UtcNow - start;
        Console.WriteLine($"[{DateTime.Now}] {context.Request.Method} {context.Request.Path} -> " +
            $"{context.Response.StatusCode} in {duration.TotalMilliseconds} ms");
    });

app.MapGet("/todos", () => todos);

app.MapGet("/todos/{id}", Results<Ok<Todo>, NotFound> (int id) =>
{
    var targetTodo = todos.SingleOrDefault(t => id == t.Id);
    return targetTodo is null
    ? TypedResults.NotFound()
    : TypedResults.Ok(targetTodo);
});

int nextId = 1;

app.MapPost("/todos", (Todo task) =>
{
    Todo newTask = task with { Id = nextId++ };
    todos.Add(newTask);
    return TypedResults.Created($"/todos/{newTask.Id}", newTask);
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

