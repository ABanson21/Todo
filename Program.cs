namespace TodoBackend;

public static class   Program
{
    public static int Main(String[] args)
    {
        try
        {
            var host = CreateHostBuilder(args).Build();
            host.Run();
            return 0;
        }
        catch (Exception ex)
        {
            return 1;
        }
    }

    private static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            });
    }
}