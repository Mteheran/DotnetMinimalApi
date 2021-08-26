using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(); 
builder.Services.AddDbContext<TodoDbContext>(options => options.UseInMemoryDatabase("TodoItems"));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.MapGet("/", (Func<string>)(() => "Hello World!"));
app.MapGet("/date", (() => DateTime.Now.ToLongDateString()));

app.MapGet("/todoitems", async (http) =>
{
    var dbContext = http.RequestServices.GetService<TodoDbContext>();
    var todoItems = await dbContext.TodoItems.ToListAsync();

    await http.Response.WriteAsJsonAsync(todoItems);
});



app.MapPost("/todoitems", async (http) =>
{
    var todoItem = await http.Request.ReadFromJsonAsync<TodoItem>();
    var dbContext = http.RequestServices.GetService<TodoDbContext>();
    dbContext.TodoItems.Add(todoItem);
    await dbContext.SaveChangesAsync();
    http.Response.StatusCode = 204;
});

app.MapPut("/todoitems/{id}", async (http) =>
{
    if (!http.Request.RouteValues.TryGetValue("id", out var id))
    {
        http.Response.StatusCode = 400;
        return;
    }

    var dbContext = http.RequestServices.GetService<TodoDbContext>();
    var todoItem = await dbContext.TodoItems.FindAsync(int.Parse(id.ToString()));
    if (todoItem == null)
    {
        http.Response.StatusCode = 404;
        return;
    }

    var inputTodoItem = await http.Request.ReadFromJsonAsync<TodoItem>();
    todoItem.IsCompleted = inputTodoItem.IsCompleted;
    await dbContext.SaveChangesAsync();
    http.Response.StatusCode = 204;
});

app.MapDelete("/todoitems/{id}", async (http) =>
{
    if (!http.Request.RouteValues.TryGetValue("id", out var id))
    {
        http.Response.StatusCode = 400;
        return;
    }

    var dbContext = http.RequestServices.GetService<TodoDbContext>();
    var todoItem = await dbContext.TodoItems.FindAsync(int.Parse(id.ToString()));
    if (todoItem == null)
    {
        http.Response.StatusCode = 404;
        return;
    }

    dbContext.TodoItems.Remove(todoItem);
    await dbContext.SaveChangesAsync();

    http.Response.StatusCode = 204;
});

 app.MapGet("/todoitems/{id}", async ([FromServices] TodoDbContext dbContext, int id) =>
    {
    
        var todoItem = await dbContext.TodoItems.FindAsync(id);
        if (todoItem == null)
        {        
            return Results.NotFound();
        }

        return Results.Ok(todoItem);
    });

//app.UseEndpoints( endpoints => 
//{
//    endpoints.MapGet("/todoitems/{id}", async ([FromServices] TodoDbContext dbContext, int id) =>
//    {
    
//        var todoItem = await dbContext.TodoItems.FindAsync(id);
//        if (todoItem == null)
//        {        
//            return Results.NotFound();
//        }

//        return Results.Ok();
//    });

//});

app.UseSwagger();
app.UseSwaggerUI();

await app.RunAsync();

public class TodoDbContext : DbContext
{
    public TodoDbContext(DbContextOptions options) : base(options)
    {
    }

    protected TodoDbContext()
    {
    }
    public DbSet<TodoItem> TodoItems { get; set; }
}
public class TodoItem
{
    public int Id { get; set; }
    public string Title { get; set; }
    public bool IsCompleted { get; set; }
}
