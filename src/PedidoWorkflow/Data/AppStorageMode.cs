namespace PedidoWorkflow.Data;

public sealed class AppStorageMode
{
    public bool UseInMemory { get; set; }
    public string? InitializationError { get; set; }
}
