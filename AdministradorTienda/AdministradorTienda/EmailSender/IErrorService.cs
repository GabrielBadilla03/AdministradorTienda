namespace Proyecto_FarmaScan.Service
{
    public interface IErrorService
    {
        void LogError(Exception ex, string? path);
    }
}
