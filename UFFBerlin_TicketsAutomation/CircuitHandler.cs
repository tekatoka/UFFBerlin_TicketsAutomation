using Microsoft.AspNetCore.Components.Server.Circuits;
using System.Threading.Tasks;

public class CustomCircuitHandler : CircuitHandler
{
    public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Circuit {circuit.Id} connected.");
        return Task.CompletedTask;
    }

    public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Circuit {circuit.Id} disconnected.");
        return Task.CompletedTask;
    }
}
