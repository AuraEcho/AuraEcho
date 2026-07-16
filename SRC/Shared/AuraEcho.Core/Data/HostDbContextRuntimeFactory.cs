using Microsoft.EntityFrameworkCore;
using AuraEcho.Core.Tools;

namespace AuraEcho.Core.Data
{
    public static class HostDbContextRuntimeFactory
    {
        private static DbContextOptions<HostDbContext> _options;

        static HostDbContextRuntimeFactory()
        {
            _options =
                new DbContextOptionsBuilder<HostDbContext>()
                    .UseSqlite($"Data Source={ApplicationPaths.HostDataBase}")
                    .Options;
        }

        public static HostDbContext CreateDbContext()
        {
            return new HostDbContext(_options);
        }
    }
}
