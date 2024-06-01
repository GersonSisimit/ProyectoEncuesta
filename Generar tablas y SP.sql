--1 Crear la base de datos
CREATE DATABASE Encuesta;--Tiene que llamarse encuesta , a esta se conecta el proyecto
--Usar la base de datos
Use Encuesta

-- Crear tablas
CREATE TABLE Usuario (
    UsuarioID INT PRIMARY KEY IDENTITY(1,1),
    Nombre NVARCHAR(50) NOT NULL,
    Contraseña NVARCHAR(64) NOT NULL -- Se deja en 64 ya que guardará el string en sha256
);

-- Registro del usuario por defecto
INSERT INTO Usuario VALUES('Admin', '95e470f41389d5613e49d20492172870e0cad63ba3973afe3cceda5a34a93e54');

-- Crear tabla Encuesta
CREATE TABLE Encuesta (
    IDEncuesta INT PRIMARY KEY IDENTITY(1,1),
    Nombre NVARCHAR(255) NOT NULL,
    Descripcion NVARCHAR(255)
);

-- Crear tabla CampoEncuesta con ON DELETE CASCADE para borrar los datos y no tener datos innecesarios
CREATE TABLE CampoEncuesta (
    IDCampoEncuesta INT PRIMARY KEY IDENTITY(1,1),
    IDEncuesta INT,
    NombreCampo NVARCHAR(255) NOT NULL,
    Titulo NVARCHAR(255) NOT NULL,
    Descripcion NVARCHAR(255),
    TipoInput NVARCHAR(50) NOT NULL,
    Requerido BIT NOT NULL,
    FOREIGN KEY (IDEncuesta) REFERENCES Encuesta(IDEncuesta) ON DELETE CASCADE
);

-- Crear tabla RespuestaEncuesta con ON DELETE CASCADE para borrar los datos y no tener datos innecesarios
CREATE TABLE RespuestaEncuesta (
    IDRespuestaEncuesta INT PRIMARY KEY IDENTITY(1,1),
    IDCampoEncuesta INT,
    Respuesta NVARCHAR(255) NOT NULL,
    FechaRespuesta DATETIME NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (IDCampoEncuesta) REFERENCES CampoEncuesta(IDCampoEncuesta) ON DELETE CASCADE
);

-- Crear procedimientos almacenados (SP)

-- SP para login
CREATE PROCEDURE sp_ValidarUsuario
    @Nombre NVARCHAR(255),
    @Contraseña NVARCHAR(65)
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM Usuario WHERE Nombre = @Nombre AND Contraseña = @Contraseña)
    BEGIN
        SELECT 1 AS Resultado; -- Usuario válido
    END
    ELSE
    BEGIN
        SELECT 0 AS Resultado; -- Usuario no válido
    END
END

-- SP para seleccionar una encuesta
CREATE PROCEDURE sp_SelectEncuesta
    @IDEncuesta INT
AS
BEGIN
    SELECT IDEncuesta, Nombre, Descripcion
    FROM Encuesta
    WHERE IDEncuesta = @IDEncuesta;
END

-- SP para insertar en Encuesta
CREATE PROCEDURE sp_InsertEncuesta
    @Nombre NVARCHAR(255),
    @Descripcion NVARCHAR(255)
AS
BEGIN
    INSERT INTO Encuesta (Nombre, Descripcion)
    VALUES (@Nombre, @Descripcion);
    SELECT SCOPE_IDENTITY() AS IDEncuesta;
END

-- SP para seleccionar campos por encuesta
CREATE PROCEDURE sp_SelectCamposPorEncuesta
    @IDEncuesta INT
AS
BEGIN
    SELECT IDCampoEncuesta, IDEncuesta, NombreCampo, Titulo, Descripcion, TipoInput, Requerido
    FROM CampoEncuesta
    WHERE IDEncuesta = @IDEncuesta;
END

-- SP para insertar en CampoEncuesta
CREATE PROCEDURE sp_InsertCampoEncuesta
    @IDEncuesta INT,
    @NombreCampo NVARCHAR(255),
    @Titulo NVARCHAR(255),
    @Descripcion NVARCHAR(255),
    @TipoInput NVARCHAR(50),
    @Requerido BIT
AS
BEGIN
    INSERT INTO CampoEncuesta (IDEncuesta, NombreCampo, Titulo, Descripcion, TipoInput, Requerido)
    VALUES (@IDEncuesta, @NombreCampo, @Titulo, @Descripcion, @TipoInput, @Requerido);
    SELECT SCOPE_IDENTITY() AS IDCampoEncuesta;
END

-- SP para guardar respuestas de encuesta
CREATE PROCEDURE sp_InsertRespuestaEncuesta
    @IDCampoEncuesta INT,
    @Respuesta NVARCHAR(255)
AS
BEGIN
    INSERT INTO RespuestaEncuesta (IDCampoEncuesta, Respuesta)
    VALUES (@IDCampoEncuesta, @Respuesta);
END

-- SP para obtener todas las encuestas
CREATE PROCEDURE GetAllEncuestas
AS
BEGIN
    SELECT IDEncuesta, Nombre, Descripcion FROM Encuesta;
END

-- SP para borrar encuesta
CREATE PROCEDURE DeleteEncuesta
    @IDEncuesta INT
AS
BEGIN
    DELETE FROM Encuesta WHERE IDEncuesta = @IDEncuesta;
END

-- SP para editar encuesta
CREATE PROCEDURE EditEncuesta
    @IDEncuesta INT,
    @Nombre NVARCHAR(255),
    @Descripcion NVARCHAR(255)
AS
BEGIN
    UPDATE Encuesta
    SET Nombre = @Nombre, Descripcion = @Descripcion
    WHERE IDEncuesta = @IDEncuesta;
END

-- SP para editar campo de encuesta
CREATE PROCEDURE EditCampoEncuesta
    @IDCampoEncuesta INT,
    @NombreCampo NVARCHAR(255),
    @Titulo NVARCHAR(255),
    @Descripcion NVARCHAR(255),
    @TipoInput NVARCHAR(50),
    @Requerido BIT
AS
BEGIN
    UPDATE CampoEncuesta
    SET NombreCampo = @NombreCampo,
        Titulo = @Titulo,
        Descripcion = ISNULL(@Descripcion, ''),
        TipoInput = @TipoInput,
        Requerido = @Requerido
    WHERE IDCampoEncuesta = @IDCampoEncuesta;
END

-- SP para borrar campo de encuesta
CREATE PROCEDURE DeleteCampoEncuesta
    @IDCampoEncuesta INT
AS
BEGIN
    DELETE FROM CampoEncuesta WHERE IDCampoEncuesta = @IDCampoEncuesta;
END

-- SP para obtener respuestas por campo
CREATE PROCEDURE GetRespuestasPorCampo
    @IDCampoEncuesta INT
AS
BEGIN
    SELECT IDRespuestaEncuesta, IDCampoEncuesta, Respuesta, FechaRespuesta
    FROM RespuestaEncuesta
    WHERE IDCampoEncuesta = @IDCampoEncuesta;
END
