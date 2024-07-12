using Microsoft.EntityFrameworkCore;
using Server.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Data
{
    public class AppDbContext : DbContext
    {      
        public DbSet<ChatMessage> ChatMessages { get; set; }
     
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql("Server=localhost; Database=ChatApp; Username=postgres; Password=12345");
        }
    }
}
