using Microsoft.EntityFrameworkCore;
using Onlink.Models;

namespace Onlink.Data
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Employee> Employee { get; set; }
        public DbSet<Job> Jobs { get; set; }
        public DbSet<Employer> Employer { get; set; }
        public DbSet<Post> Post { get; set; }
        public DbSet<Resume> Resume { get; set; }
        public DbSet<Certificate> Certificate { get; set; }
        public DbSet<EmployeeJob> EmployeeJob { get; set; }
        public DbSet<PostLike> PostLikes { get; set; }
        public DbSet<JobApplication> JobApplication { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Disable all cascading deletes
            foreach (var relationship in modelBuilder.Model.GetEntityTypes()
                .SelectMany(e => e.GetForeignKeys()))
            {
                relationship.DeleteBehavior = DeleteBehavior.NoAction;
            }

            // User <-> Employer (1:1)
            modelBuilder.Entity<Employer>()
                .HasOne(e => e.User)
                .WithOne(u => u.Employer)
                .HasForeignKey<Employer>(e => e.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            // User <-> Employee (1:1)
            modelBuilder.Entity<Employee>()
                .HasOne(e => e.User)
                .WithOne(u => u.Employee)
                .HasForeignKey<Employee>(e => e.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            // Resume <-> Employee
            modelBuilder.Entity<Resume>()
                .HasOne(r => r.Employee)
                .WithMany(e => e.Resumes)
                .HasForeignKey(r => r.EmployeeId)
                .OnDelete(DeleteBehavior.NoAction);

            // Resume <-> Employer
            modelBuilder.Entity<Resume>()
                .HasOne(r => r.Employer)
                .WithMany(e => e.Resume)
                .HasForeignKey(r => r.EmployerId)
                .OnDelete(DeleteBehavior.NoAction);

            // Post <-> Employee (optional)
            modelBuilder.Entity<Post>()
                .HasOne(p => p.Employee)
                .WithMany(e => e.Posts)
                .HasForeignKey(p => p.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Post <-> Employer (optional)
            modelBuilder.Entity<Post>()
                .HasOne(p => p.Employer)
                .WithMany(e => e.Posts)
                .HasForeignKey(p => p.EmployerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Self-referencing relationship for Post replies
            modelBuilder.Entity<Post>()
                .HasMany(p => p.RelatedPosts)
                .WithOne(p => p.ParentPost)
                .HasForeignKey(p => p.ParentPostId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<PostLike>()
                .HasIndex(pl => new { pl.PostId, pl.UserId })
                .IsUnique(); // prevents duplicate likes per user per post


            base.OnModelCreating(modelBuilder);
        }
    }
}