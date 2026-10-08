using Microsoft.EntityFrameworkCore;

namespace Muyu.Shared.Infrastructure;

/// <summary>
/// Permite que cada módulo registre su modelo de EF Core sin exponer sus entidades
/// al Shared Kernel ni crear dependencias circulares.
/// </summary>
public interface IModuleEntityConfiguration
{
    void Configure(ModelBuilder modelBuilder);
}
