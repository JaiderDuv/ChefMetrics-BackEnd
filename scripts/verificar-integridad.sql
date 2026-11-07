/*
  Verificación local de las defensas de integridad de la base.

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

/*
  Las pruebas de los triggers de tenencia se retiraron junto con los triggers
  (migración QuitarTriggersIncompatibles). EF Core no puede insertar con
  OUTPUT INSERTED en tablas con triggers, así que esa validación se hace en
  los servicios.
*/

/* 1. Check: nombre de producto demasiado corto. */
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

/* 2. Check: precio cero. */
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

/* 3. FK: historial apuntando a una asociación inexistente. */
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

/* 4. FK con ON DELETE SET NULL: al borrar la asociación se conserva el historial. */
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
        THROW 51003, N'No hay historial asociado para probar ON DELETE SET NULL.', 1;

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
    IF ERROR_NUMBER() = 51003
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
