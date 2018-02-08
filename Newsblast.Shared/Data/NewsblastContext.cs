using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Newsblast.Shared.Data.Models;

namespace Newsblast.Shared.Data
{
    public class NewsblastContext : DbContext
    {
        public DbSet<Source> Sources { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<Embed> Embeds { get; set; }

        public NewsblastContext(DbContextOptions<NewsblastContext> options)
            : base(options)
        {

        }
    }

    public class NewsblastContextFactory : IDesignTimeDbContextFactory<NewsblastContext>
    {
        public NewsblastContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<NewsblastContext>()
                .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Integrated Security=true");

            return new NewsblastContext(optionsBuilder.Options);
        }
    }
}
