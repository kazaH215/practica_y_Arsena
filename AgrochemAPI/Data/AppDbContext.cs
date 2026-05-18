using Microsoft.EntityFrameworkCore;
using AgrochemAPI.Models;

namespace AgrochemAPI.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<ProductionBatch> ProductionBatches => Set<ProductionBatch>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<RawMaterial> RawMaterials => Set<RawMaterial>();
    public DbSet<Equipment> Equipment => Set<Equipment>();
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<RecipeComponent> RecipeComponents => Set<RecipeComponent>();
    public DbSet<TechCard> TechCards => Set<TechCard>();
    public DbSet<TechStep> TechSteps => Set<TechStep>();
    public DbSet<ProductionOrder> ProductionOrders => Set<ProductionOrder>();
    public DbSet<BatchStepExecution> BatchStepExecutions => Set<BatchStepExecution>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<LabTest> LabTests => Set<LabTest>();
    public DbSet<LabTestParameter> LabTestParameters => Set<LabTestParameter>();
    public DbSet<RawMaterialBatch> RawMaterialBatches => Set<RawMaterialBatch>();
    public DbSet<Deviation> Deviations => Set<Deviation>();           // ← ДОБАВИТЬ
    public DbSet<Notification> Notifications => Set<Notification>();  // ← ДОБАВИТЬ
    public DbSet<EventLog> EventLogs => Set<EventLog>();              // ← ДОБАВИТЬ (опционально)

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Отключаем каскадное удаление для BatchStepExecution → TechStep
        modelBuilder.Entity<BatchStepExecution>()
            .HasOne(b => b.TechStep)
            .WithMany()
            .HasForeignKey(b => b.StepId)
            .OnDelete(DeleteBehavior.NoAction);
    }

}