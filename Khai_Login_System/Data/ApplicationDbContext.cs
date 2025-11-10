using Khai_Login_System.Models;
using Microsoft.EntityFrameworkCore;

namespace Khai_Login_System.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<UserAccount> Users => Set<UserAccount>();
}
