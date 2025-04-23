using Proyecto_FarmaScan.Service;

public class ErrorService : IErrorService
{
    private readonly ILogger<ErrorService> _logger;

    public ErrorService(ILogger<ErrorService> logger)
    {
        _logger = logger;
    }

    public void LogError(Exception ex, string? path)
    {
        _logger.LogError(ex, "Error en ruta: {Path}", path);
   
    }
}
