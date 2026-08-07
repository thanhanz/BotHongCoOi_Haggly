using Microsoft.EntityFrameworkCore;

namespace Haggly.Infrastructure.Persistence;

public sealed class HagglyDbContext(DbContextOptions<HagglyDbContext> options) : DbContext(options);
