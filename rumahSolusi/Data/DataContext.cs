using Microsoft.EntityFrameworkCore;

namespace rumahSolusi.Data
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {

        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            optionsBuilder
                .UseSqlServer("Data Source=RIJALELFIKRI\\SQLEXPRESS;Initial Catalog=RumahSolusi;Integrated Security=True;" + "MultipleActiveResultSets = True;");
        }
        public DbSet<UserModel> MstUsers { get; set; }

    }
}