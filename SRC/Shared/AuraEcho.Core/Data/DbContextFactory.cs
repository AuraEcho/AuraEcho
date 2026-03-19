using Microsoft.EntityFrameworkCore;
using AuraEcho.Core.Tools;

namespace AuraEcho.Core.Data
{
    public static class DbContextFactory
    {
        private static DbContextOptions<AuraEchoDbContext> _options;

        static DbContextFactory()
        {
            _options =
                new DbContextOptionsBuilder<AuraEchoDbContext>()
                    .UseSqlite($"Data Source={ApplicationPaths.HostDataBase}")
                    .Options;
        }

        public static AuraEchoDbContext CreateDbContext()
        {
            return new AuraEchoDbContext(_options);
        }
    }
}
