using Asoagro.Models;
using Microsoft.EntityFrameworkCore;

namespace Asoagro.Data;

public class AsoagroDbContext : DbContext
{
    public AsoagroDbContext(DbContextOptions<AsoagroDbContext> options)
        : base(options)
    {
    }

    public DbSet<AsociacionEntity> Asociaciones => Set<AsociacionEntity>();
    public DbSet<Asociado> Asociados => Set<Asociado>();
    public DbSet<CultivoEntity> Cultivos => Set<CultivoEntity>();
    public DbSet<GrupoVulnerableEntity> GruposVulnerables => Set<GrupoVulnerableEntity>();
    public DbSet<TipoCuotaEntity> TiposCuota => Set<TipoCuotaEntity>();
    public DbSet<AsociadoCultivoEntity> AsociadoCultivos => Set<AsociadoCultivoEntity>();
    public DbSet<AsociadoGrupoVulnerableEntity> AsociadoGruposVulnerables => Set<AsociadoGrupoVulnerableEntity>();
    public DbSet<CuotaAsociadoEntity> CuotasAsociados => Set<CuotaAsociadoEntity>();
    public DbSet<TipoDocumento> TiposDocumento => Set<TipoDocumento>();
    public DbSet<Documento> Documentos => Set<Documento>();
    public DbSet<HistorialDocumentoEntity> HistorialDocumentos => Set<HistorialDocumentoEntity>();
    public DbSet<Acta> Actas => Set<Acta>();
    public DbSet<SeguimientoAsociado> SeguimientosAsociados => Set<SeguimientoAsociado>();
    public DbSet<UsuarioEntity> Usuarios => Set<UsuarioEntity>();
    public DbSet<PermisoModuloEntity> PermisosModulos => Set<PermisoModuloEntity>();
    public DbSet<AuditoriaEntity> Auditorias => Set<AuditoriaEntity>();
    public DbSet<CostoGastoEntity> CostosGastos => Set<CostoGastoEntity>();
    public DbSet<ProductoEntity> Productos => Set<ProductoEntity>();
    public DbSet<VentaEntity> Ventas => Set<VentaEntity>();
    public DbSet<VentaDetalleEntity> VentasDetalles => Set<VentaDetalleEntity>();
    public DbSet<VistaAlertaVencimiento> VistasAlertasVencimiento => Set<VistaAlertaVencimiento>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 1. Asociaciones
        modelBuilder.Entity<AsociacionEntity>(entity =>
        {
            entity.ToTable("asociaciones", "public");

            entity.HasKey(a => a.Id);
            entity.Property(a => a.Id)
                .HasColumnName("id")
                .UseIdentityByDefaultColumn();

            entity.Property(a => a.Nombre)
                .HasColumnName("nombre")
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(a => a.Nit)
                .HasColumnName("nit")
                .HasMaxLength(30);

            entity.Property(a => a.Municipio)
                .HasColumnName("municipio")
                .HasMaxLength(100);

            entity.Property(a => a.RepresentanteLegal)
                .HasColumnName("representante_legal")
                .HasMaxLength(150);

            entity.Property(a => a.Correo)
                .HasColumnName("correo")
                .HasMaxLength(150);

            entity.Property(a => a.Telefono)
                .HasColumnName("telefono")
                .HasMaxLength(50);

            entity.Property(a => a.Activo)
                .HasColumnName("activo")
                .HasDefaultValue(true);

            entity.Property(a => a.CreadoEn)
                .HasColumnName("creado_en")
                .HasColumnType("timestamp with time zone");
        });

        // 2. Asociados
        modelBuilder.Entity<Asociado>(entity =>
        {
            entity.ToTable("asociados", "public");

            entity.HasKey(a => a.Id);
            entity.Property(a => a.Id)
                .HasColumnName("id")
                .UseIdentityByDefaultColumn();

            entity.Property(a => a.AsociacionId)
                .HasColumnName("asociacion_id")
                .IsRequired();

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

            entity.Property(a => a.Telefono)
                .HasColumnName("telefono")
                .HasMaxLength(30);

            entity.Property(a => a.Correo)
                .HasColumnName("correo")
                .HasMaxLength(150);

            entity.Property(a => a.NombrePredio)
                .HasColumnName("nombre_predio")
                .HasMaxLength(100);

            entity.Property(a => a.VeredaSector)
                .HasColumnName("vereda_sector")
                .HasMaxLength(100);

            entity.Property(a => a.Municipio)
                .HasColumnName("municipio")
                .HasMaxLength(100);

            entity.Property(a => a.HectareasPredio)
                .HasColumnName("hectareas_predio")
                .HasColumnType("numeric");

            entity.Property(a => a.EsVictima)
                .HasColumnName("es_victima")
                .HasColumnType("boolean")
                .HasDefaultValue(false);

            entity.Property(a => a.EstadoMembresia)
                .HasColumnName("estado_membresia")
                .HasMaxLength(20)
                .HasDefaultValue("Activo");

            entity.Property(a => a.FechaRegistro)
                .HasColumnName("fecha_registro")
                .HasColumnType("timestamp with time zone");

            entity.Property(a => a.FechaActualizacion)
                .HasColumnName("fecha_actualizacion")
                .HasColumnType("timestamp with time zone");

            entity.Property(a => a.CreadoPor)
                .HasColumnName("creado_por")
                .HasMaxLength(80)
                .HasDefaultValue("Sistema");

            entity.Property(a => a.ActualizadoPor)
                .HasColumnName("actualizado_por")
                .HasMaxLength(80);

            entity.Property(a => a.FincaVereda)
                .HasColumnName("finca_vereda")
                .HasMaxLength(100);

            entity.Property(a => a.CultivoPrincipal)
                .HasColumnName("cultivo_principal")
                .HasMaxLength(50);

            entity.Property(a => a.TieneBPA)
                .HasColumnName("tiene_bpa")
                .HasColumnType("boolean")
                .HasDefaultValue(false);

            entity.Ignore(a => a.TipoDocumentoIdentidad);

            entity.HasOne(a => a.Asociacion)
                .WithMany(asoc => asoc.Asociados)
                .HasForeignKey(a => a.AsociacionId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_asociados_asociacion");
        });

        // 3. Cultivos
        modelBuilder.Entity<CultivoEntity>(entity =>
        {
            entity.ToTable("cultivos", "public");

            entity.HasKey(c => c.Id);
            entity.Property(c => c.Id)
                .HasColumnName("id")
                .UseIdentityByDefaultColumn();

            entity.Property(c => c.Nombre)
                .HasColumnName("nombre")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(c => c.Activo)
                .HasColumnName("activo")
                .HasDefaultValue(true);
        });

        // 4. Grupos Vulnerables
        modelBuilder.Entity<GrupoVulnerableEntity>(entity =>
        {
            entity.ToTable("grupos_vulnerables", "public");

            entity.HasKey(g => g.Id);
            entity.Property(g => g.Id)
                .HasColumnName("id")
                .UseIdentityByDefaultColumn();

            entity.Property(g => g.Nombre)
                .HasColumnName("nombre")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(g => g.Activo)
                .HasColumnName("activo")
                .HasDefaultValue(true);
        });

        // 5. Tipos Cuota
        modelBuilder.Entity<TipoCuotaEntity>(entity =>
        {
            entity.ToTable("tipos_cuota", "public");

            entity.HasKey(t => t.Id);
            entity.Property(t => t.Id)
                .HasColumnName("id")
                .UseIdentityByDefaultColumn();

            entity.Property(t => t.Codigo)
                .HasColumnName("codigo")
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(t => t.Nombre)
                .HasColumnName("nombre")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(t => t.PermitePeriodicidad)
                .HasColumnName("permite_periodicidad")
                .HasDefaultValue(true);

            entity.Property(t => t.Activo)
                .HasColumnName("activo")
                .HasDefaultValue(true);
        });

        // 6. Asociado Cultivos
        modelBuilder.Entity<AsociadoCultivoEntity>(entity =>
        {
            entity.ToTable("asociado_cultivos", "public");

            entity.HasKey(ac => ac.Id);
            entity.Property(ac => ac.Id)
                .HasColumnName("id")
                .UseIdentityByDefaultColumn();

            entity.Property(ac => ac.AsociadoId)
                .HasColumnName("asociado_id")
                .IsRequired();

            entity.Property(ac => ac.CultivoId)
                .HasColumnName("cultivo_id")
                .IsRequired();

            entity.Property(ac => ac.Hectareas)
                .HasColumnName("hectareas")
                .HasColumnType("numeric");

            entity.Property(ac => ac.TieneBpa)
                .HasColumnName("tiene_bpa")
                .HasDefaultValue(false);

            entity.Property(ac => ac.CertificadoBpaNombre)
                .HasColumnName("certificado_bpa_nombre")
                .HasMaxLength(255);

            entity.Property(ac => ac.CertificadoBpaRuta)
                .HasColumnName("certificado_bpa_ruta")
                .HasColumnType("text");

            entity.Property(ac => ac.BpaFechaVencimiento)
                .HasColumnName("bpa_fecha_vencimiento")
                .HasColumnType("date");

            entity.Property(ac => ac.CreadoEn)
                .HasColumnName("creado_en")
                .HasColumnType("timestamp with time zone");

            entity.HasOne(ac => ac.Asociado)
                .WithMany(a => a.AsociadoCultivos)
                .HasForeignKey(ac => ac.AsociadoId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ac => ac.Cultivo)
                .WithMany()
                .HasForeignKey(ac => ac.CultivoId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 7. Asociado Grupos Vulnerables
        modelBuilder.Entity<AsociadoGrupoVulnerableEntity>(entity =>
        {
            entity.ToTable("asociado_grupos_vulnerables", "public");

            entity.HasKey(ag => ag.Id);
            entity.Property(ag => ag.Id)
                .HasColumnName("id")
                .UseIdentityByDefaultColumn();

            entity.Property(ag => ag.AsociadoId)
                .HasColumnName("asociado_id")
                .IsRequired();

            entity.Property(ag => ag.GrupoId)
                .HasColumnName("grupo_id")
                .IsRequired();

            entity.Property(ag => ag.ArchivoNombre)
                .HasColumnName("archivo_nombre")
                .HasMaxLength(255);

            entity.Property(ag => ag.ArchivoRuta)
                .HasColumnName("archivo_ruta")
                .HasColumnType("text");

            entity.Property(ag => ag.Observaciones)
                .HasColumnName("observaciones")
                .HasColumnType("text");

            entity.Property(ag => ag.CreadoEn)
                .HasColumnName("creado_en")
                .HasColumnType("timestamp with time zone");

            entity.HasOne(ag => ag.Asociado)
                .WithMany(a => a.AsociadoGruposVulnerables)
                .HasForeignKey(ag => ag.AsociadoId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ag => ag.GrupoVulnerable)
                .WithMany()
                .HasForeignKey(ag => ag.GrupoId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 8. Cuotas Asociado
        modelBuilder.Entity<CuotaAsociadoEntity>(entity =>
        {
            entity.ToTable("cuotas_asociado", "public");

            entity.HasKey(c => c.Id);
            entity.Property(c => c.Id)
                .HasColumnName("id")
                .UseIdentityByDefaultColumn();

            entity.Property(c => c.AsociadoId)
                .HasColumnName("asociado_id")
                .IsRequired();

            entity.Property(c => c.TipoCuotaId)
                .HasColumnName("tipo_cuota_id")
                .IsRequired();

            entity.Property(c => c.Periodicidad)
                .HasColumnName("periodicidad")
                .HasMaxLength(50);

            entity.Property(c => c.PeriodoDesde)
                .HasColumnName("periodo_desde")
                .HasColumnType("date");

            entity.Property(c => c.PeriodoHasta)
                .HasColumnName("periodo_hasta")
                .HasColumnType("date");

            entity.Property(c => c.Valor)
                .HasColumnName("valor")
                .HasColumnType("numeric")
                .IsRequired();

            entity.Property(c => c.ValorPagado)
                .HasColumnName("valor_pagado")
                .HasColumnType("numeric")
                .IsRequired();

            entity.Property(c => c.FechaVencimiento)
                .HasColumnName("fecha_vencimiento")
                .HasColumnType("date");

            entity.Property(c => c.FechaPago)
                .HasColumnName("fecha_pago")
                .HasColumnType("date");

            entity.Property(c => c.Estado)
                .HasColumnName("estado")
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(c => c.Motivo)
                .HasColumnName("motivo")
                .HasColumnType("text");

            entity.Property(c => c.RegistradoPor)
                .HasColumnName("registrado_por");

            entity.Property(c => c.CreadoEn)
                .HasColumnName("creado_en")
                .HasColumnType("timestamp with time zone");

            entity.HasOne(c => c.Asociado)
                .WithMany(a => a.CuotasAsociados)
                .HasForeignKey(c => c.AsociadoId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(c => c.TipoCuota)
                .WithMany()
                .HasForeignKey(c => c.TipoCuotaId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 9. Tipos Documento
        modelBuilder.Entity<TipoDocumento>(entity =>
        {
            entity.ToTable("tipos_documento", "public");

            entity.HasKey(t => t.Id);
            entity.Property(t => t.Id)
                .HasColumnName("id")
                .UseIdentityByDefaultColumn();

            entity.Property(t => t.Nombre)
                .HasColumnName("nombre")
                .HasMaxLength(120)
                .IsRequired();

            entity.Property(t => t.Categoria)
                .HasColumnName("categoria");

            entity.Property(t => t.RequiereVencimiento)
                .HasColumnName("requiere_vencimiento");

            entity.Property(t => t.AplicaAAsociado)
                .HasColumnName("aplica_a_asociado");

            entity.Property(t => t.Descripcion)
                .HasColumnName("descripcion")
                .HasColumnType("text");

            entity.Property(t => t.Activo)
                .HasColumnName("activo")
                .HasDefaultValue(true);
        });

        // 10. Documentos
        modelBuilder.Entity<Documento>(entity =>
        {
            entity.ToTable("documentos", "public");

            entity.HasKey(d => d.Id);
            entity.Property(d => d.Id)
                .HasColumnName("id")
                .UseIdentityByDefaultColumn();

            entity.Property(d => d.AsociacionId)
                .HasColumnName("asociacion_id")
                .IsRequired();

            entity.Property(d => d.AsociadoId)
                .HasColumnName("asociado_id");

            entity.Property(d => d.TipoDocumentoId)
                .HasColumnName("tipo_documento_id")
                .IsRequired();

            entity.Property(d => d.NumeroDocumento)
                .HasColumnName("numero_documento")
                .HasMaxLength(50);

            entity.Property(d => d.EntidadEmisora)
                .HasColumnName("entidad_emisora")
                .HasMaxLength(150);

            entity.Property(d => d.FechaExpedicion)
                .HasColumnName("fecha_expedicion")
                .HasColumnType("date");

            entity.Property(d => d.FechaVencimiento)
                .HasColumnName("fecha_vencimiento")
                .HasColumnType("date");

            entity.Property(d => d.Estado)
                .HasColumnName("estado")
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(d => d.ArchivoNombre)
                .HasColumnName("archivo_nombre")
                .HasMaxLength(255);

            entity.Property(d => d.ArchivoRuta)
                .HasColumnName("archivo_ruta")
                .HasColumnType("text");

            entity.Property(d => d.Observaciones)
                .HasColumnName("observaciones")
                .HasColumnType("text");

            entity.Property(d => d.SubidoPor)
                .HasColumnName("subido_por");

            entity.Property(d => d.CreadoEn)
                .HasColumnName("creado_en")
                .HasColumnType("timestamp with time zone");

            entity.Property(d => d.ActualizadoEn)
                .HasColumnName("actualizado_en")
                .HasColumnType("timestamp with time zone");

            entity.HasOne(d => d.Asociacion)
                .WithMany(a => a.Documentos)
                .HasForeignKey(d => d.AsociacionId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Asociado)
                .WithMany()
                .HasForeignKey(d => d.AsociadoId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);

            entity.HasOne(d => d.TipoDocumentoNav)
                .WithMany()
                .HasForeignKey(d => d.TipoDocumentoId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 11. Historial Documentos
        modelBuilder.Entity<HistorialDocumentoEntity>(entity =>
        {
            entity.ToTable("historial_documentos", "public");

            entity.HasKey(h => h.Id);
            entity.Property(h => h.Id)
                .HasColumnName("id")
                .UseIdentityByDefaultColumn();

            entity.Property(h => h.DocumentoId)
                .HasColumnName("documento_id")
                .IsRequired();

            entity.Property(h => h.Accion)
                .HasColumnName("accion")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(h => h.EstadoAnterior)
                .HasColumnName("estado_anterior");

            entity.Property(h => h.EstadoNuevo)
                .HasColumnName("estado_nuevo");

            entity.Property(h => h.VencAnterior)
                .HasColumnName("venc_anterior")
                .HasColumnType("date");

            entity.Property(h => h.VencNuevo)
                .HasColumnName("venc_nuevo")
                .HasColumnType("date");

            entity.Property(h => h.UsuarioId)
                .HasColumnName("usuario_id");

            entity.Property(h => h.FechaRegistro)
                .HasColumnName("fecha_registro")
                .HasColumnType("timestamp with time zone");

            entity.HasOne(h => h.Documento)
                .WithMany()
                .HasForeignKey(h => h.DocumentoId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // 12. Actas
        modelBuilder.Entity<Acta>(entity =>
        {
            entity.ToTable("actas", "public");

            entity.HasKey(a => a.Id);
            entity.Property(a => a.Id)
                .HasColumnName("id")
                .UseIdentityByDefaultColumn();

            entity.Property(a => a.AsociacionId)
                .HasColumnName("asociacion_id")
                .IsRequired();

            entity.Property(a => a.NumeroActa)
                .HasColumnName("numero_acta")
                .HasMaxLength(50);

            entity.Property(a => a.Tipo)
                .HasColumnName("tipo")
                .HasMaxLength(60)
                .IsRequired();

            entity.Property(a => a.Titulo)
                .HasColumnName("titulo")
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(a => a.FechaActa)
                .HasColumnName("fecha_acta")
                .HasColumnType("date");

            entity.Property(a => a.Resumen)
                .HasColumnName("resumen")
                .HasColumnType("text");

            entity.Property(a => a.ArchivoNombre)
                .HasColumnName("archivo_nombre")
                .HasMaxLength(255);

            entity.Property(a => a.ArchivoRuta)
                .HasColumnName("archivo_ruta")
                .HasColumnType("text");

            entity.Property(a => a.SubidoPor)
                .HasColumnName("subido_por");

            entity.Property(a => a.CreadoEn)
                .HasColumnName("creado_en")
                .HasColumnType("timestamp with time zone");

            entity.HasOne(a => a.Asociacion)
                .WithMany(asoc => asoc.Actas)
                .HasForeignKey(a => a.AsociacionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 13. Seguimientos Asociados
        modelBuilder.Entity<SeguimientoAsociado>(entity =>
        {
            entity.ToTable("seguimientos_asociados", "public");

            entity.HasKey(seguimiento => seguimiento.Id);
            entity.Property(seguimiento => seguimiento.Id)
                .HasColumnName("id")
                .UseIdentityByDefaultColumn();

            entity.Property(seguimiento => seguimiento.AsociadoId)
                .HasColumnName("asociado_id");

            entity.Property(seguimiento => seguimiento.TipoGestion)
                .HasColumnName("tipo_gestion")
                .HasMaxLength(40)
                .IsRequired();

            entity.Property(seguimiento => seguimiento.FechaGestion)
                .HasColumnName("fecha_gestion")
                .HasColumnType("timestamp with time zone");

            entity.Property(seguimiento => seguimiento.Descripcion)
                .HasColumnName("descripcion")
                .HasMaxLength(1000)
                .IsRequired();

            entity.Property(seguimiento => seguimiento.ProximaGestion)
                .HasColumnName("proxima_gestion")
                .HasColumnType("date");

            entity.Ignore(seguimiento => seguimiento.HoraReunion);

            entity.Property(seguimiento => seguimiento.GestionadoPor)
                .HasColumnName("gestionado_por")
                .HasMaxLength(254)
                .IsRequired();

            entity.HasOne(seguimiento => seguimiento.Asociado)
                .WithMany()
                .HasForeignKey(seguimiento => seguimiento.AsociadoId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_seguimientos_asociados_asociado");
        });

        // 14. Usuarios
        modelBuilder.Entity<UsuarioEntity>(entity =>
        {
            entity.ToTable("usuarios", "public");

            entity.HasKey(u => u.Id);
            entity.Property(u => u.Id)
                .HasColumnName("id")
                .UseIdentityByDefaultColumn();

            entity.Property(u => u.NombreCompleto)
                .HasColumnName("nombre_completo")
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(u => u.DocumentoId)
                .HasColumnName("documento_id")
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(u => u.Correo)
                .HasColumnName("correo")
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(u => u.Telefono)
                .HasColumnName("telefono")
                .HasMaxLength(50);

            entity.Property(u => u.ContrasenaHash)
                .HasColumnName("contrasena_hash")
                .HasColumnType("text");

            entity.Property(u => u.Rol)
                .HasColumnName("rol")
                .IsRequired();

            entity.Property(u => u.AsociacionId)
                .HasColumnName("asociacion_id");

            entity.Property(u => u.Activo)
                .HasColumnName("activo")
                .HasDefaultValue(true);

            entity.Property(u => u.UltimoAcceso)
                .HasColumnName("ultimo_acceso")
                .HasColumnType("timestamp with time zone");

            entity.Property(u => u.CreadoEn)
                .HasColumnName("creado_en")
                .HasColumnType("timestamp with time zone");

            entity.HasOne(u => u.Asociacion)
                .WithMany()
                .HasForeignKey(u => u.AsociacionId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // 15. Permisos Módulo
        modelBuilder.Entity<PermisoModuloEntity>(entity =>
        {
            entity.ToTable("permisos_modulo", "public");

            entity.HasKey(p => p.Id);
            entity.Property(p => p.Id)
                .HasColumnName("id")
                .UseIdentityByDefaultColumn();

            entity.Property(p => p.Rol)
                .HasColumnName("rol")
                .IsRequired();

            entity.Property(p => p.Modulo)
                .HasColumnName("modulo")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(p => p.PuedeVer)
                .HasColumnName("puede_ver")
                .HasDefaultValue(false);

            entity.Property(p => p.PuedeCrear)
                .HasColumnName("puede_crear")
                .HasDefaultValue(false);

            entity.Property(p => p.PuedeEditar)
                .HasColumnName("puede_editar")
                .HasDefaultValue(false);

            entity.Property(p => p.PuedeEliminar)
                .HasColumnName("puede_eliminar")
                .HasDefaultValue(false);
        });

        // 16. Auditoría
        modelBuilder.Entity<AuditoriaEntity>(entity =>
        {
            entity.ToTable("auditoria", "public");

            entity.HasKey(a => a.Id);
            entity.Property(a => a.Id)
                .HasColumnName("id")
                .UseIdentityByDefaultColumn();

            entity.Property(a => a.UsuarioId)
                .HasColumnName("usuario_id");

            entity.Property(a => a.Accion)
                .HasColumnName("accion")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(a => a.Tabla)
                .HasColumnName("tabla")
                .HasMaxLength(100);

            entity.Property(a => a.RegistroId)
                .HasColumnName("registro_id");

            entity.Property(a => a.Detalle)
                .HasColumnName("detalle")
                .HasColumnType("text");

            entity.Property(a => a.Ip)
                .HasColumnName("ip")
                .HasMaxLength(50);

            entity.Property(a => a.Fecha)
                .HasColumnName("fecha")
                .HasColumnType("timestamp with time zone");
        });

        // 17. Costos y Gastos
        modelBuilder.Entity<CostoGastoEntity>(entity =>
        {
            entity.ToTable("costos_gastos", "public");

            entity.HasKey(cg => cg.Id);
            entity.Property(cg => cg.Id)
                .HasColumnName("id")
                .UseIdentityByDefaultColumn();

            entity.Property(cg => cg.AsociacionId)
                .HasColumnName("asociacion_id")
                .IsRequired();

            entity.Property(cg => cg.Tipo)
                .HasColumnName("tipo")
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(cg => cg.Categoria)
                .HasColumnName("categoria")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(cg => cg.Descripcion)
                .HasColumnName("descripcion")
                .HasColumnType("text")
                .IsRequired();

            entity.Property(cg => cg.Fecha)
                .HasColumnName("fecha")
                .HasColumnType("date")
                .IsRequired();

            entity.Property(cg => cg.Valor)
                .HasColumnName("valor")
                .HasColumnType("numeric")
                .IsRequired();

            entity.Property(cg => cg.Proveedor)
                .HasColumnName("proveedor")
                .HasMaxLength(150);

            entity.Property(cg => cg.NumeroSoporte)
                .HasColumnName("numero_soporte")
                .HasMaxLength(100);

            entity.Property(cg => cg.ArchivoNombre)
                .HasColumnName("archivo_nombre")
                .HasMaxLength(255);

            entity.Property(cg => cg.ArchivoRuta)
                .HasColumnName("archivo_ruta")
                .HasColumnType("text");

            entity.Property(cg => cg.RegistradoPor)
                .HasColumnName("registrado_por");

            entity.Property(cg => cg.CreadoEn)
                .HasColumnName("creado_en")
                .HasColumnType("timestamp with time zone");

            entity.HasOne(cg => cg.Asociacion)
                .WithMany()
                .HasForeignKey(cg => cg.AsociacionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 18. Productos
        modelBuilder.Entity<ProductoEntity>(entity =>
        {
            entity.ToTable("productos", "public");

            entity.HasKey(p => p.Id);
            entity.Property(p => p.Id)
                .HasColumnName("id")
                .UseIdentityByDefaultColumn();

            entity.Property(p => p.Nombre)
                .HasColumnName("nombre")
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(p => p.UnidadMedida)
                .HasColumnName("unidad_medida")
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(p => p.Categoria)
                .HasColumnName("categoria")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(p => p.Activo)
                .HasColumnName("activo")
                .HasDefaultValue(true);
        });

        // 19. Ventas
        modelBuilder.Entity<VentaEntity>(entity =>
        {
            entity.ToTable("ventas", "public");

            entity.HasKey(v => v.Id);
            entity.Property(v => v.Id)
                .HasColumnName("id")
                .UseIdentityByDefaultColumn();

            entity.Property(v => v.AsociacionId)
                .HasColumnName("asociacion_id")
                .IsRequired();

            entity.Property(v => v.AsociadoId)
                .HasColumnName("asociado_id");

            entity.Property(v => v.FechaVenta)
                .HasColumnName("fecha_venta")
                .HasColumnType("date")
                .IsRequired();

            entity.Property(v => v.Comprador)
                .HasColumnName("comprador")
                .HasMaxLength(150);

            entity.Property(v => v.ValorTotal)
                .HasColumnName("valor_total")
                .HasColumnType("numeric")
                .IsRequired();

            entity.Property(v => v.EstadoDespacho)
                .HasColumnName("estado_despacho")
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(v => v.FechaDespacho)
                .HasColumnName("fecha_despacho")
                .HasColumnType("date");

            entity.Property(v => v.EstadoPago)
                .HasColumnName("estado_pago")
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(v => v.ValorPagado)
                .HasColumnName("valor_pagado")
                .HasColumnType("numeric")
                .IsRequired();

            entity.Property(v => v.FechaPago)
                .HasColumnName("fecha_pago")
                .HasColumnType("date");

            entity.Property(v => v.Observaciones)
                .HasColumnName("observaciones")
                .HasColumnType("text");

            entity.Property(v => v.RegistradoPor)
                .HasColumnName("registrado_por");

            entity.Property(v => v.CreadoEn)
                .HasColumnName("creado_en")
                .HasColumnType("timestamp with time zone");

            entity.HasOne(v => v.Asociacion)
                .WithMany()
                .HasForeignKey(v => v.AsociacionId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(v => v.Asociado)
                .WithMany()
                .HasForeignKey(v => v.AsociadoId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // 20. Ventas Detalle
        modelBuilder.Entity<VentaDetalleEntity>(entity =>
        {
            entity.ToTable("ventas_detalle", "public");

            entity.HasKey(vd => vd.Id);
            entity.Property(vd => vd.Id)
                .HasColumnName("id")
                .UseIdentityByDefaultColumn();

            entity.Property(vd => vd.VentaId)
                .HasColumnName("venta_id")
                .IsRequired();

            entity.Property(vd => vd.ProductoId)
                .HasColumnName("producto_id")
                .IsRequired();

            entity.Property(vd => vd.Cantidad)
                .HasColumnName("cantidad")
                .HasColumnType("numeric")
                .IsRequired();

            entity.Property(vd => vd.PrecioUnitario)
                .HasColumnName("precio_unitario")
                .HasColumnType("numeric")
                .IsRequired();

            entity.Property(vd => vd.ValorTotal)
                .HasColumnName("valor_total")
                .HasColumnType("numeric");

            entity.HasOne(vd => vd.Venta)
                .WithMany(v => v.Detalles)
                .HasForeignKey(vd => vd.VentaId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(vd => vd.Producto)
                .WithMany()
                .HasForeignKey(vd => vd.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 21. Vista Alertas Vencimiento (Keyless Entity View)
        modelBuilder.Entity<VistaAlertaVencimiento>(entity =>
        {
            entity.ToView("vista_alertas_vencimiento", "public");
            entity.HasNoKey();

            entity.Property(v => v.DocumentoId).HasColumnName("documento_id");
            entity.Property(v => v.AsociacionId).HasColumnName("asociacion_id");
            entity.Property(v => v.Asociacion).HasColumnName("asociacion");
            entity.Property(v => v.AsociadoId).HasColumnName("asociado_id");
            entity.Property(v => v.TipoDocumento).HasColumnName("tipo_documento");
            entity.Property(v => v.FechaVencimiento).HasColumnName("fecha_vencimiento").HasColumnType("date");
            entity.Property(v => v.DiasRestantes).HasColumnName("dias_restantes");
            entity.Property(v => v.Semaforo).HasColumnName("semaforo");
        });
    }
}
