/*
  Verificación local de las defensas de integridad y tenencia.

  Uso:
    sqlcmd -S "SATUS22\MSSQL1SVR" -d ChefMetrics -E -C -b -i scripts\verificar-integridad.sql

  Cada prueba abre su propia transacción y hace ROLLBACK: no deja datos de
  prueba en la base. Si alguna defensa no bloquea el insert inválido, el
  script termina con error para que CI o el comité lo vean.
*/
SET NOCOUNT ON;
SET XACT_ABORT OFF;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;

DECLARE @errores int = 0;
DECLARE @mensaje nvarchar(2048);

/* 1. Trigger: producto y receta de negocios distintos. */
BEGIN TRY
    BEGIN TRANSACTION;

    INSERT INTO dbo.ProductoMenuReceta (ProductoMenuId, RecetaId, Cantidad)
    SELECT TOP (1) p.Id, r.Id, 1
    FROM dbo.ProductoMenu p
    CROSS JOIN dbo.Receta r
    WHERE p.NegocioId = 3 AND r.NegocioId = 5
      AND NOT EXISTS (
          SELECT 1 FROM dbo.ProductoMenuReceta pr
          WHERE pr.ProductoMenuId = p.Id AND pr.RecetaId = r.Id);

    IF @@ROWCOUNT = 0
        THROW 51001, N'No hay datos para probar el trigger de ProductoMenuReceta.', 1;

    ROLLBACK TRANSACTION;
    SET @errores += 1;
    PRINT N'FALLO: se permitió asociar una receta de otro negocio.';
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    IF ERROR_NUMBER() = 51001
    BEGIN
        SET @errores += 1;
        PRINT N'FALLO: ' + ERROR_MESSAGE();
    END
    ELSE
    BEGIN
        SET @mensaje = ERROR_MESSAGE();
        PRINT N'OK: trigger ProductoMenuReceta -> ' + @mensaje;
    END
END CATCH;

/* 2. Trigger: producto e insumo de negocios distintos. */
BEGIN TRY
    BEGIN TRANSACTION;

    INSERT INTO dbo.ProductoMenuInsumo (ProductoMenuId, InsumoId, Cantidad, UnidadMedida)
    SELECT TOP (1) p.Id, i.Id, 1, N'unidad'
    FROM dbo.ProductoMenu p
    CROSS JOIN dbo.Insumo i
    WHERE p.NegocioId = 3 AND i.NegocioId = 5
      AND NOT EXISTS (
          SELECT 1 FROM dbo.ProductoMenuInsumo pi
          WHERE pi.ProductoMenuId = p.Id AND pi.InsumoId = i.Id);

    IF @@ROWCOUNT = 0
        THROW 51002, N'No hay datos para probar el trigger de ProductoMenuInsumo.', 1;

    ROLLBACK TRANSACTION;
    SET @errores += 1;
    PRINT N'FALLO: se permitió asociar un insumo de otro negocio al menú.';
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    IF ERROR_NUMBER() = 51002
    BEGIN
        SET @errores += 1;
        PRINT N'FALLO: ' + ERROR_MESSAGE();
    END
    ELSE
    BEGIN
        SET @mensaje = ERROR_MESSAGE();
        PRINT N'OK: trigger ProductoMenuInsumo -> ' + @mensaje;
    END
END CATCH;

/* 3. Trigger: receta e insumo de negocios distintos. */
BEGIN TRY
    BEGIN TRANSACTION;

    INSERT INTO dbo.RecetaInsumo (RecetaId, InsumoId, Cantidad, UnidadMedida, PorcentajeMerma)
    SELECT TOP (1) r.Id, i.Id, 1, 'g', 0
    FROM dbo.Receta r
    CROSS JOIN dbo.Insumo i
    WHERE r.NegocioId = 3 AND i.NegocioId = 5
      AND NOT EXISTS (
          SELECT 1 FROM dbo.RecetaInsumo ri
          WHERE ri.RecetaId = r.Id AND ri.InsumoId = i.Id);

    IF @@ROWCOUNT = 0
        THROW 51003, N'No hay datos para probar el trigger de RecetaInsumo.', 1;

    ROLLBACK TRANSACTION;
    SET @errores += 1;
    PRINT N'FALLO: se permitió asociar un insumo de otro negocio a una receta.';
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    IF ERROR_NUMBER() = 51003
    BEGIN
        SET @errores += 1;
        PRINT N'FALLO: ' + ERROR_MESSAGE();
    END
    ELSE
    BEGIN
        SET @mensaje = ERROR_MESSAGE();
        PRINT N'OK: trigger RecetaInsumo -> ' + @mensaje;
    END
END CATCH;

/* 4. Trigger: insumo y proveedor de negocios distintos. */
BEGIN TRY
    BEGIN TRANSACTION;

    INSERT INTO dbo.InsumoProveedor
        (InsumoId, ProveedorId, Costo, EsPrincipal, FechaActualizacion, Cantidad, UnidadMedida)
    SELECT TOP (1) i.Id, p.Id, 1, 0, SYSDATETIME(), 1, N'unidad'
    FROM dbo.Insumo i
    CROSS JOIN dbo.Proveedor p
    WHERE i.NegocioId = 3 AND p.NegocioId = 5
      AND NOT EXISTS (
          SELECT 1 FROM dbo.InsumoProveedor ip
          WHERE ip.InsumoId = i.Id AND ip.ProveedorId = p.Id);

    IF @@ROWCOUNT = 0
        THROW 51004, N'No hay datos para probar el trigger de InsumoProveedor.', 1;

    ROLLBACK TRANSACTION;
    SET @errores += 1;
    PRINT N'FALLO: se permitió asociar un proveedor de otro negocio.';
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    IF ERROR_NUMBER() = 51004
    BEGIN
        SET @errores += 1;
        PRINT N'FALLO: ' + ERROR_MESSAGE();
    END
    ELSE
    BEGIN
        SET @mensaje = ERROR_MESSAGE();
        PRINT N'OK: trigger InsumoProveedor -> ' + @mensaje;
    END
END CATCH;

/* 5. Check: nombre de producto demasiado corto. */
BEGIN TRY
    BEGIN TRANSACTION;

    INSERT INTO dbo.ProductoMenu (NegocioId, Nombre, PrecioVenta, Activo, FechaCreacion)
    VALUES (3, N'AB', 10, 1, SYSDATETIME());

    ROLLBACK TRANSACTION;
    SET @errores += 1;
    PRINT N'FALLO: se permitió un nombre de producto de 2 caracteres.';
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    SET @mensaje = ERROR_MESSAGE();
    PRINT N'OK: check CK_ProductoMenu_Nombre -> ' + @mensaje;
END CATCH;

/* 6. Check: precio cero. */
BEGIN TRY
    BEGIN TRANSACTION;

    INSERT INTO dbo.ProductoMenu (NegocioId, Nombre, PrecioVenta, Activo, FechaCreacion)
    VALUES (3, N'Producto prueba precio', 0, 1, SYSDATETIME());

    ROLLBACK TRANSACTION;
    SET @errores += 1;
    PRINT N'FALLO: se permitió un precio de venta en cero.';
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    SET @mensaje = ERROR_MESSAGE();
    PRINT N'OK: check CK_ProductoMenu_Precio -> ' + @mensaje;
END CATCH;

/* 7. FK: historial apuntando a una asociación inexistente. */
BEGIN TRY
    BEGIN TRANSACTION;

    INSERT INTO dbo.HistorialPrecioInsumo
        (InsumoProveedorId, CostoAnterior, CostoNuevo, FechaCambio)
    VALUES (999999, 1, 1, SYSDATETIME());

    ROLLBACK TRANSACTION;
    SET @errores += 1;
    PRINT N'FALLO: se permitió un historial con InsumoProveedorId inexistente.';
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    SET @mensaje = ERROR_MESSAGE();
    PRINT N'OK: FK HistorialPrecioInsumo -> ' + @mensaje;
END CATCH;

/* 8. FK con ON DELETE SET NULL: al borrar la asociación se conserva el historial. */
BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @asociacionConHistorial int =
    (
        SELECT TOP (1) h.InsumoProveedorId
        FROM dbo.HistorialPrecioInsumo h
        WHERE h.InsumoProveedorId IS NOT NULL
        ORDER BY h.Id
    );

    IF @asociacionConHistorial IS NULL
        THROW 51008, N'No hay historial asociado para probar ON DELETE SET NULL.', 1;

    DELETE FROM dbo.InsumoProveedor WHERE Id = @asociacionConHistorial;

    IF EXISTS (SELECT 1 FROM dbo.HistorialPrecioInsumo WHERE InsumoProveedorId = @asociacionConHistorial)
    BEGIN
        ROLLBACK TRANSACTION;
        SET @errores += 1;
        PRINT N'FALLO: el historial conservó una referencia a una asociación eliminada.';
    END
    ELSE
    BEGIN
        ROLLBACK TRANSACTION;
        PRINT N'OK: FK con ON DELETE SET NULL conserva el historial sin la asociación.';
    END
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    IF ERROR_NUMBER() = 51008
    BEGIN
        SET @errores += 1;
        PRINT N'FALLO: ' + ERROR_MESSAGE();
    END
    ELSE
    BEGIN
        SET @errores += 1;
        SET @mensaje = ERROR_MESSAGE();
        PRINT N'FALLO: no se pudo probar ON DELETE SET NULL -> ' + @mensaje;
    END
END CATCH;

IF @errores > 0
BEGIN
    THROW 51000, N'Fallaron una o más verificaciones de integridad.', 1;
END;

PRINT N'OK: todas las verificaciones de integridad pasaron.';
