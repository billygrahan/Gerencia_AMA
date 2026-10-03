using Microsoft.EntityFrameworkCore;
using ApiTesourariaAMA.Models;
using ApiTesourariaAMA.Enums;

namespace ApiTesourariaAMA.context;
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Usuario> Usuarios { get; set; }
    public DbSet<TipoDoacao> TiposDoacao { get; set; }
    public DbSet<DebitoMembro> DebitosMembros { get; set; }
    public DbSet<Comprovante> Comprovantes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Define precisão para valores monetários
        modelBuilder.Entity<DebitoMembro>()
            .Property(d => d.Valor)
            .HasConversion<double>(); 

        // // Hash válida e testada para a senha: Admin@123
        // const string hashSenhaAdmin = "$2a$11$N7I1fPjZ.x/k4A2/6i4gO.8xW/V7lB7w3KjX7xZ/8m2K2d0f0G1e2";

        // modelBuilder.Entity<Usuario>().HasData(
        //     new Usuario
        //     {
        //         Id = 1,
        //         Nome = "Billy Grahan",
        //         Email = "tesourariaiasdgenibau@gmail.com",
        //         Senha = hashSenhaAdmin,
        //         Perfil = PerfilUsuario.Tesoureiro
        //     }
        // );
    }
}