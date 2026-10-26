using Microsoft.EntityFrameworkCore;
namespace Twiggle.Api;
// No domain tables yet: health checks validate connectivity without creating a premature schema.
public sealed class TwiggleDbContext(DbContextOptions<TwiggleDbContext> options) : DbContext(options);
