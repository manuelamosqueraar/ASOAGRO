using Asoagro.Models;
using Microsoft.EntityFrameworkCore;

namespace Asoagro.Data;

public class AsoagroDbContext : DbContext
{
    public AsoagroDbContext(DbContextOptions<AsoagroDbContext> options)
        : base(options)
    {
    }

    public DbSet<Asociado> Asociados => Set<Asociado>();

    public DbSet<TipoDocumento> TiposDocumento => Set<TipoDocumento>();

    public DbSet<Documento> Documentos => Set<Documento>();

    public DbSet<Acta> Actas => Set<Acta>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Asociado>(entity =>
        {
            entity.ToTable("asociados", "public");

            entity.HasKey(a => a.Id);
            entity.Property(a => a.Id)
                .HasColumnName("id")
                .UseIdentityByDefaultColumn();

            entity.Property(a => a.Cedula)
                .HasColumnName("cedula")
                .HasMaxLength(20)
                .IsRequired();

            entity.HasIndex(a => a.Cedula)
                .IsUnique()
                .HasDatabaseName("uq_asociados_cedula");

            entity.Property(a => a.NombreCompleto)
                .HasColumnName("nombre_completo")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(a => a.FincaVereda)
                .HasColumnName("finca_vereda")
                .HasMaxLength(100)
                .HasDefaultValue(string.Empty);

            entity.Property(a => a.Telefono)
                .HasColumnName("telefono")
                .HasMaxLength(30)
                .HasDefaultValue(string.Empty);

            entity.Property(a => a.Correo)
                .HasColumnName("correo")
                .HasMaxLength(150)
                .HasDefaultValue(string.Empty);

            entity.Property(a => a.NombrePredio)
                .HasColumnName("nombre_predio")
                .HasMaxLength(100)
                .HasDefaultValue(string.Empty);

            entity.Property(a => a.VeredaSector)
                .HasColumnName("vereda_sector")
                .HasMaxLength(100)
                .HasDefaultValue(string.Empty);

            entity.Property(a => a.Municipio)
                .HasColumnName("municipio")
                .HasMaxLength(100)
                .HasDefaultValue(string.Empty);

            entity.Property(a => a.HectareasPredio)
                .HasColumnName("hectareas_predio")
                .HasDefaultValue(0d);

            entity.Property(a => a.CultivoPrincipal)
                .HasColumnName("cultivo_principal")
                .HasMaxLength(50)
                .HasDefaultValue(string.Empty);

            entity.Property(a => a.TieneBPA)
                .HasColumnName("tiene_bpa")
                .HasDefaultValue(false);

            entity.Property(a => a.EsVictima)
                .HasColumnName("es_victima")
                .HasMaxLength(10)
                .HasDefaultValue("No");

            entity.Property(a => a.EstadoMembresia)
                .HasColumnName("estado_membresia")
                .HasMaxLength(20)
                .HasDefaultValue("AlDia");

            entity.Property(a => a.FechaRegistro)
                .HasColumnName("fecha_registro");

            entity.Property(a => a.FechaActualizacion)
                .HasColumnName("fecha_actualizacion");

            entity.Property(a => a.CreadoPor)
                .HasColumnName("creado_por")
                .HasMaxLength(80)
                .HasDefaultValue("Sistema");

            entity.Property(a => a.ActualizadoPor)
                .HasColumnName("actualizado_por")
                .HasMaxLength(80)
                .HasDefaultValue("Sistema");
        });

        modelBuilder.Entity<TipoDocumento>(entity =>
        {
            entity.ToTable("tipos_documento", "public");

            entity.HasKey(t => t.Id);
            entity.Property(t => t.Id)
                .HasColumnName("id")
                .UseIdentityByDefaultColumn();

            entity.Property(t => t.Codigo)
                .HasColumnName("codigo")
                .HasMaxLength(50)
                .IsRequired();

            entity.HasIndex(t => t.Codigo)
                .IsUnique()
                .HasDatabaseName("uq_tipos_documento_codigo");

            entity.Property(t => t.Nombre)
                .HasColumnName("nombre")
                .HasMaxLength(120)
                .IsRequired();
        });

        modelBuilder.Entity<Documento>(entity =>
        {
            entity.ToTable("documentos", "public");

            entity.HasKey(d => d.Id);
            entity.Property(d => d.Id)
                .HasColumnName("id")
                .UseIdentityByDefaultColumn();

            entity.Property(d => d.AsociadoId)
                .HasColumnName("asociado_id");

            entity.Property(d => d.TipoDocumentoId)
                .HasColumnName("tipo_documento_id");

            entity.Property(d => d.DocumentoAsociado)
                .HasColumnName("documento_asociado")
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(d => d.TipoDocumento)
                .HasColumnName("tipo_documento")
                .HasMaxLength(120)
                .IsRequired();

            entity.Property(d => d.ArchivoNombre)
                .HasColumnName("archivo_nombre")
                .HasMaxLength(255)
                .HasDefaultValue(string.Empty);

            entity.Property(d => d.FechaEmision)
                .HasColumnName("fecha_emision");

            entity.Property(d => d.FechaVencimiento)
                .HasColumnName("fecha_vencimiento");

            entity.Property(d => d.Estado)
                .HasColumnName("estado")
                .HasMaxLength(30)
                .HasDefaultValue("Vigente");

            entity.HasOne(d => d.Asociado)
                .WithMany()
                .HasForeignKey(d => d.AsociadoId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_documentos_asociados");

            entity.HasOne(d => d.TipoDocumentoNav)
                .WithMany()
                .HasForeignKey(d => d.TipoDocumentoId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_documentos_tipos_documento");
        });

        modelBuilder.Entity<Acta>(entity =>
        {
            entity.ToTable("actas", "public");

            entity.HasKey(a => a.Id);
            entity.Property(a => a.Id)
                .HasColumnName("id")
                .UseIdentityByDefaultColumn();

            entity.Property(a => a.Tipo)
                .HasColumnName("tipo")
                .HasMaxLength(60)
                .IsRequired();

            entity.Property(a => a.Titulo)
                .HasColumnName("titulo")
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(a => a.Fecha)
                .HasColumnName("fecha");

            entity.Property(a => a.Descripcion)
                .HasColumnName("descripcion")
                .IsRequired();

            entity.Property(a => a.ArchivoNombre)
                .HasColumnName("archivo_nombre")
                .HasMaxLength(255)
                .IsRequired();
        });
    }
}

