using System.Collections.Generic;
using MyMvcApp.Models;
using Microsoft.EntityFrameworkCore;

namespace MyMvcApp.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

        public DbSet<Appointment> Appointment { get; set; }
        public DbSet<Department> Department { get; set; } 
        public DbSet<Doctor> Doctor { get; set; }
        public DbSet<Hospital> Hospital { get; set; }
        public DbSet<Invoice> Invoice { get; set; }
        public DbSet<Patient> Patient { get; set; }
        public DbSet<Payment> Payment { get; set; }
        public DbSet<MyMvcApp.Models.User> User { get; set; } = default!;
    }
}
