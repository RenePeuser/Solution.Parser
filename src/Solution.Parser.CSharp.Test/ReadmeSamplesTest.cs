using System;
using System.Collections.Immutable;
using System.Linq;
using AspNetCore.Simple.MsTest.Sdk;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Solution.Parser.CSharp.Test.ParseHelper;

namespace Solution.Parser.CSharp.Test
{
    /// <summary>
    /// The rules of the cookbook in the README, word for word. Developers and agents copy them, so each
    /// one runs here against code that breaks it once and code that keeps it, and a README sample that
    /// stops working fails the build.
    /// </summary>
    [TestClass]
    public class ReadmeSamplesTest
    {
        private const string ReadmeFix = "Keep the README cookbook and this test in step";

        private static ImmutableList<CSharpSyntaxTree> Trees(string code)
        {
            return ImmutableList.Create(ParseCode(code));
        }

        private static void Finds(string[] expected, System.Collections.Generic.IEnumerable<string> findings, string rule)
        {
            Assert.That.AreEqual<string>(expected,
                                         findings.Select(f => f[(f.IndexOf("): ", System.StringComparison.Ordinal) + 3)..]),
                                         because: $"the README rule '{rule}' must find exactly the broken code",
                                         fix: ReadmeFix);
        }

        [TestMethod]
        public void Inject_The_Clock()
        {
            var trees = Trees("""
                public class Billing(TimeProvider clock)
                {
                    public DateTime Due => DateTime.Now.AddDays(30);
                    public DateTimeOffset Today() => clock.GetUtcNow();
                }
                """);

            var findings = from member in trees.MembersWithBody
                           from access in member.MemberAccesses
                           where access.Target == "DateTime" && access.Name is "Now" or "UtcNow" or "Today"
                           select $"{access.Location}: inject TimeProvider instead of {access.SourceCode}";

            Finds(["inject TimeProvider instead of DateTime.Now"], findings, "inject the clock");
        }

        [TestMethod]
        public void No_Async_Void()
        {
            var trees = Trees("""
                public class View
                {
                    public async void Refresh() => await Task.Delay(1);
                    private async void OnClick(object sender, EventArgs e) => await Task.Delay(1);
                    public async Task SaveAsync() => await Task.Delay(1);
                }
                """);

            var findings = from method in trees.Methods
                           where method.IsAsync && method.ReturnType.IsVoid
                           where !method.Parameters.LastOrDefault()?.TypeReference.Name.EndsWith("EventArgs", StringComparison.Ordinal) ?? true
                           select $"{method.Location}: {method.Name} is async void, return a Task";

            Finds(["Refresh is async void, return a Task"], findings, "no async void");
        }

        [TestMethod]
        public void Awaitable_Methods_End_In_Async()
        {
            var trees = Trees("""
                public class Store
                {
                    public Task<Order> Load(int id) => Task.FromResult(new Order());
                    public ValueTask SaveAsync() => ValueTask.CompletedTask;
                    public Order Find(int id) => new();
                }
                """);

            var findings = from method in trees.Methods
                           where method.ReturnType.IsTask && !method.Name.EndsWith("Async", StringComparison.Ordinal)
                           select $"{method.Location}: {method.Name} returns {method.ReturnType.Name}, call it {method.Name}Async";

            Finds(["Load returns Task, call it LoadAsync"], findings, "awaitable methods end in Async");
        }

        [TestMethod]
        public void Log_With_Templates()
        {
            var trees = Trees("""
                public class Checkout(ILogger<Checkout> logger)
                {
                    public void Pay(Order order)
                    {
                        logger.LogInformation($"Paying {order.Id}");
                        logger.LogInformation("Paying {OrderId}", order.Id);
                    }
                }
                """);

            var findings = from call in trees.Invocations
                           where call.Name.StartsWith("Log", StringComparison.Ordinal) && call.Arguments.Any(a => a.Kind == ExpressionKind.InterpolatedString)
                           select $"{call.Location}: {call.Name} with an interpolated string loses the structure, pass a template";

            Finds(["LogInformation with an interpolated string loses the structure, pass a template"], findings, "log with templates");
        }

        [TestMethod]
        public void No_New_HttpClient()
        {
            var trees = Trees("""
                public class Gateway(IHttpClientFactory factory)
                {
                    public HttpClient Create() => new HttpClient();
                    public HttpClient Rent() => factory.CreateClient("api");
                }
                """);

            var findings = from member in trees.MembersWithBody
                           from creation in member.ObjectCreations
                           where creation.TypeName == "HttpClient"
                           select $"{creation.Location}: use IHttpClientFactory instead of new HttpClient()";

            Finds(["use IHttpClientFactory instead of new HttpClient()"], findings, "no new HttpClient");
        }

        [TestMethod]
        public void Records_Hold_No_Mutable_Collections()
        {
            var trees = Trees("""
                public record Order(string Id, List<Line> Lines);
                public record Invoice(string Id, ImmutableList<Line> Lines)
                {
                    public Dictionary<string, decimal> Taxes { get; init; } = [];
                }
                public record Line(string Sku, int Count);
                """);

            var findings = from record in trees.Records
                           from property in record.Properties
                           where property.TypeReference.IsMutableCollection
                           select $"{property.Location}: {record.Name}.{property.Name} is a {property.TypeReference.Name}, use an immutable collection";

            Finds(["Order.Lines is a List, use an immutable collection",
                   "Invoice.Taxes is a Dictionary, use an immutable collection"],
                  findings,
                  "records hold no mutable collections");
        }

        [TestMethod]
        public void Getters_Do_Not_Throw()
        {
            var trees = Trees("""
                public class Account
                {
                    private decimal? _balance;
                    public decimal Balance => _balance ?? throw new InvalidOperationException("not loaded");
                    public decimal Limit { get { if (_balance is null) throw new InvalidOperationException(); return 100; } }
                    public bool IsLoaded => _balance is not null;
                }
                """);

            var findings = from property in trees.Properties
                           let getter = property.Accessors.Get
                           where getter is not null
                           where getter.Throws.Any() || getter.AllExpressions.Any(e => e.Kind == ExpressionKind.Throw)
                           select $"{property.Location}: the getter of {property.Name} throws, make it a method";

            Finds(["the getter of Balance throws, make it a method", "the getter of Limit throws, make it a method"],
                  findings,
                  "getters do not throw");
        }

        [TestMethod]
        public void Null_Checks_Read_As_Null_Checks()
        {
            var trees = Trees("""
                public class Guard
                {
                    public bool A(object x) => x != null;
                    public bool B(object x) => x is { };
                    public bool C(object x) => x is not null;
                }
                """);

            var findings = from member in trees.MembersWithBody
                           from expression in member.AllExpressions
                           where (expression.Operator is "==" or "!=" && expression.Right?.Kind == ExpressionKind.NullLiteral)
                              || expression.Pattern is { Kind: PatternKind.Recursive, IsNotNull: true }
                           select $"{expression.Location}: write {expression.SourceCode} as is null or is not null";

            Finds(["write x != null as is null or is not null", "write x is { } as is null or is not null"],
                  findings,
                  "null checks read as null checks");
        }

        [TestMethod]
        public void Switch_Expressions_Are_Exhaustive()
        {
            var trees = Trees("""
                public class Pricing
                {
                    public decimal Rate(Tier tier) => tier switch { Tier.Gold => 0.2m, Tier.Silver => 0.1m };
                    public decimal Fee(Tier tier) => tier switch { Tier.Gold => 0m, _ => 5m };
                }
                """);

            var findings = from member in trees.MembersWithBody
                           from switchExpression in member.SwitchExpressions
                           where !switchExpression.HasDiscardArm
                           select $"{switchExpression.Location}: {member.Name} switches without a _ arm";

            Finds(["Rate switches without a _ arm"], findings, "switch expressions are exhaustive");
        }

        [TestMethod]
        public void Parameters_Are_Not_Reassigned()
        {
            var trees = Trees("""
                public class Text
                {
                    public string Clean(string value) { value = value.Trim(); return value; }
                    public string Tidy(string value) { var trimmed = value.Trim(); return trimmed; }
                }
                """);

            var findings = from method in trees.Methods
                           from assignment in method.AllExpressions
                           where assignment.Kind == ExpressionKind.Assignment
                              && method.Parameters.Any(p => p.Name == assignment.Left?.Name)
                           select $"{assignment.Location}: do not assign to the parameter {assignment.Left!.Name}";

            Finds(["do not assign to the parameter value"], findings, "parameters are not reassigned");
        }

        [TestMethod]
        public void Classic_Extensions_Move_Into_Extension_Blocks()
        {
            var trees = Trees("""
                public static class StringExtensions
                {
                    public static string Shout(this string value) => value.ToUpper();

                    extension(string value)
                    {
                        public string Whisper() => value.ToLower();
                    }
                }
                """);

            var findings = from type in trees.AllTypes()
                           where type.HasExtensionMembers
                           from method in type.Methods
                           where method.IsExtension && !method.IsInExtensionBlock
                           select $"{method.Location}: move {method.Name} into an extension block";

            Finds(["move Shout into an extension block"], findings, "classic extensions move into extension blocks");
        }

        [TestMethod]
        public void Endpoints_Require_Authorization()
        {
            var trees = Trees("""
                public static class Endpoints
                {
                    public static void Map(WebApplication app)
                    {
                        app.MapGet("/orders", () => "all").WithName("orders");
                        app.MapGet("/admin", () => "secret").RequireAuthorization();
                    }
                }
                """);

            var findings = from method in trees.Methods
                           from chain in method.InvocationChains
                           where chain.Contains("MapGet") && !chain.After("MapGet").Any(c => c.Name == "RequireAuthorization")
                           select $"{chain.Location}: {chain.First.Arguments[0].StringValue} is not protected";

            Finds(["/orders is not protected"], findings, "endpoints require authorization");
        }

        [TestMethod]
        public void Quick_Start_Finds_Mutable_Properties()
        {
            var trees = Trees("""
                namespace My.Sample;
                public class Person { public string Name { get; set; } public int Age { get; init; } }
                """);

            var findings = from property in trees.Properties
                           where !property.IsReadOnly
                           select $"{property.Location}: {property.FullyQualifiedName} is mutable";

            Finds(["My.Sample.Person.Name is mutable"], findings, "quick start");
        }

        [TestMethod]
        public void Else_If_Chains_Stay_Short()
        {
            var trees = Trees("""
                public class Shipping
                {
                    public decimal Cost(string zone)
                    {
                        if (zone == "DE") return 5;
                        else if (zone == "AT") return 7;
                        else if (zone == "CH") return 12;
                        else return 20;
                    }

                    public decimal Fee(bool express) { if (express) return 10; else return 0; }
                }
                """);

            var findings = from member in trees.MembersWithBody
                           from chain in member.IfStatements
                           where !chain.IsElseIf && chain.ElseCount > 2
                           select $"{chain.Location}: {member.Name} has an if with {chain.ElseCount} else, use early returns, a switch or a lookup";

            Finds(["Cost has an if with 3 else, use early returns, a switch or a lookup"], findings, "else if chains stay short");
        }

        [TestMethod]
        public void No_Else_After_Return()
        {
            var trees = Trees("""
                public class Orders
                {
                    public string Status(Order? order)
                    {
                        if (order is null)
                        {
                            return "missing";
                        }
                        else
                        {
                            return order.State;
                        }
                    }

                    public string Label(Order? order)
                    {
                        if (order is null) return "missing";
                        return order.Name;
                    }
                }
                """);

            var findings = from member in trees.MembersWithBody
                           from statement in member.IfStatements
                           where statement.HasElse && statement.Then.EndsWithJump
                           select $"{statement.Location}: the if in {member.Name} returns, drop the else and go on after it";

            Finds(["the if in Status returns, drop the else and go on after it"], findings, "no else after return");
        }

        [TestMethod]
        public void No_Loop_Inside_A_Loop()
        {
            var trees = Trees("""
                public class Report
                {
                    public int Matches(Order[] orders, Customer[] customers)
                    {
                        var count = 0;
                        foreach (var order in orders)
                        {
                            for (var i = 0; i < customers.Length; i++)
                            {
                                if (customers[i].Id == order.CustomerId) count++;
                            }
                        }
                        return count;
                    }

                    public int Total(Order[] orders)
                    {
                        var total = 0;
                        foreach (var order in orders) total += order.Sum;
                        return total;
                    }
                }
                """);

            var findings = from method in trees.Methods
                           from loop in method.Loops
                           from inner in loop.Loops
                           select $"{inner.Location}: {method.Name} runs a {inner.Kind} inside a {loop.Kind}, use a lookup";

            Finds(["Matches runs a For inside a ForEach, use a lookup"], findings, "no loop inside a loop");
        }

        [TestMethod]
        public void No_Save_Inside_A_Loop()
        {
            var trees = Trees("""
                public class Import(ShopContext db)
                {
                    public async Task RunAsync(Order[] orders)
                    {
                        foreach (var order in orders)
                        {
                            db.Orders.Add(order);
                            await db.SaveChangesAsync();
                        }
                    }

                    public async Task RunOnceAsync(Order[] orders)
                    {
                        foreach (var order in orders) db.Orders.Add(order);
                        await db.SaveChangesAsync();
                    }
                }
                """);

            var findings = from member in trees.MembersWithBody
                           from loop in member.Loops
                           from call in loop.Invocations
                           where call.Name is "SaveChanges" or "SaveChangesAsync"
                           select $"{call.Location}: {member.Name} calls {call.Name} once per item, save after the loop";

            Finds(["RunAsync calls SaveChangesAsync once per item, save after the loop"], findings, "no save inside a loop");
        }

        [TestMethod]
        public void No_Await_Inside_A_ForEach()
        {
            var trees = Trees("""
                public class Mailer(IMailClient client)
                {
                    public async Task SendAllAsync(Mail[] mails)
                    {
                        foreach (var mail in mails)
                        {
                            await client.SendAsync(mail);
                        }
                    }

                    public Task SendTogetherAsync(Mail[] mails) => Task.WhenAll(mails.Select(client.SendAsync));
                }
                """);

            var findings = from member in trees.MembersWithBody
                           from loop in member.ForEachLoops
                           from awaited in loop.Awaits
                           select $"{awaited.Location}: {member.Name} awaits one at a time, start the tasks and await Task.WhenAll";

            Finds(["SendAllAsync awaits one at a time, start the tasks and await Task.WhenAll"], findings, "no await inside a foreach");
        }

        [TestMethod]
        public void Throw_Keeps_The_Stack_Trace()
        {
            var trees = Trees("""
                public class Store
                {
                    public void Save()
                    {
                        try { Write(); }
                        catch (IOException ex) { throw ex; }
                        catch (TimeoutException ex) { throw new StoreException("timeout", ex); }
                        catch (Exception) { throw; }
                    }
                }
                """);

            var findings = from member in trees.MembersWithBody
                           from tryStatement in member.TryStatements
                           from @catch in tryStatement.Catches
                           from throwStatement in @catch.Throws
                           where @catch.VariableName is not null && throwStatement.Value?.Name == @catch.VariableName
                           select $"{throwStatement.Location}: {throwStatement.SourceCode} loses the stack trace, write throw;";

            Finds(["throw ex; loses the stack trace, write throw;"], findings, "throw keeps the stack trace");
        }
    }
}
