namespace SmartCafeDesktop.Data;

public static class DatabaseConfig
{
    // Sesuaikan Server Name jika memakai SQL Server lokal (misal: localhost\SQLEXPRESS atau .\SQLEXPRESS)
    public static string ConnectionString { get; set; } =
        @"Server=VEGA\SQLEXPRESS;Database=SmartCafeDB;Trusted_Connection=True;TrustServerCertificate=True;";
}