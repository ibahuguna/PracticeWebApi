using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Rewrite;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.UseRewriter(new RewriteOptions().AddRewrite("^tasks(/.*)?$", "todos$1", skipRemainingRules: true)); ;

app.Use(async (context, next) =>
    {
        var start = DateTime.UtcNow;
        await next(context);
        var duration = DateTime.UtcNow - start;
        Console.WriteLine($"[{DateTime.Now}] {context.Request.Method} {context.Request.Path} -> " +
            $"{context.Response.StatusCode} in {duration.TotalMilliseconds} ms");
    });

var todos = new List<Todo>();

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
})
.AddEndpointFilter(async (context, next) =>
{
    var taskArgument = context.GetArgument<Todo>(0);
    var errors = new Dictionary<string, string[]>();
    if (taskArgument.DueDate < DateTime.UtcNow)
        errors.Add(nameof(Todo.DueDate), ["Cannot have due date in the past"]);
    if (taskArgument.IsCompleted)
        errors.Add(nameof(Todo.IsCompleted), ["Cannot add completed todo."]);

    if (errors.Count > 0)
        return Results.ValidationProblem(errors);

    return await next(context);
});

app.MapPut("/todos/{id}", (int id, Todo updatedTodo) =>
{
    var existingTodo = todos.SingleOrDefault(t => t.Id == id);
    if (existingTodo is null)
        return Results.NotFound();
    var updated = existingTodo with
    {
        Name = updatedTodo.Name,
        DueDate = updatedTodo.DueDate,
        IsCompleted = updatedTodo.IsCompleted
    };
    var index = todos.IndexOf(existingTodo);
    todos[index] = updated;
    return Results.Ok(updated);
});

app.MapDelete("/todos/{id}", (int id) =>
{
    var removed = todos.RemoveAll(t => id == t.Id);
    return (removed == 0) ?
         Results.NotFound() :
         Results.NoContent();
});

app.Run();

public record Todo(int Id, string Name, DateTime DueDate, bool IsCompleted);

