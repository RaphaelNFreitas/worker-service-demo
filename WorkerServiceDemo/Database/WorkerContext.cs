using System.Net.Mime;
using Microsoft.EntityFrameworkCore;
using WorkerServiceDemo.Core;

namespace WorkerServiceDemo.Database
{
    public class WorkerContext : DbContext
    {
        public WorkerContext(DbContextOptions<WorkerContext> options)
            : base(options)
        {

        }

        public DbSet<Imagem> Imagens { get; set; }
    }
}