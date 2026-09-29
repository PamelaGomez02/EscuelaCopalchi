USE AulaVirtualCopalchi;
GO

ALTER TABLE ESTUDIANTE
ADD observacion_baja NVARCHAR(1000) NULL;
GO

ALTER TABLE ESTUDIANTE
ADD fecha_baja NVARCHAR(1000) NULL;
GO

ALTER TABLE ESTUDIANTE
ADD parentesco NVARCHAR(1000) NULL;
GO

ALTER TABLE ESTUDIANTE
ADD correo_encargado NVARCHAR(1000) NULL;
GO


CREATE TABLE TIPO_ADECUACION
(
id_tipo INT IDENTITY(1,1) PRIMARY KEY,
 
nombre_adecuacion NVARCHAR(100) NOT NULL,
 
descripcion NVARCHAR(500) NULL
);
GO

ALTER TABLE ADECUACION_ACADEMICA
DROP COLUMN tipo;
GO

ALTER TABLE TIPO_ADECUACION
ADD objetivo NVARCHAR(500) NULL;
GO

ALTER TABLE ADECUACION_ACADEMICA
ADD tipo_adecuacion INT NULL;
GO

ALTER TABLE ADECUACION_ACADEMICA
ADD CONSTRAINT FK_ADECUACION_ACADEMICA_TIPO
FOREIGN KEY (tipo_adecuacion)
REFERENCES TIPO_ADECUACION(id_tipo);
GO


INSERT INTO TIPO_ADECUACION
(
    nombre_adecuacion,
    descripcion,
    objetivo
)
VALUES
(
    'Adecuaciones de Acceso',
    'Modificaciones en el entorno físico, recursos materiales o sistemas de comunicación.',
    'Facilitar que el estudiante llegue a la información y participe en las clases.'
),
(
    'Adecuaciones No Significativas',
    'Cambios en la metodología, las actividades o la forma de evaluar, sin alterar los objetivos ni los contenidos básicos del año que se cursa.',
    'Adaptar la enseñanza al ritmo y estilo de aprendizaje del alumno.'
),
(
    'Adecuaciones Significativas',
    'Modificaciones sustanciales que alteran los objetivos, contenidos básicos o criterios de evaluación oficiales.',
    'Responder a necesidades educativas muy específicas que impiden alcanzar el currículo estándar.'
);
GO





CREATE TABLE ALERGIA
(
id_alergia INT IDENTITY(1,1) NOT NULL,
 
id_estudiante INT NOT NULL,
 
alergia NVARCHAR(100) NOT NULL,
 
descripcion NVARCHAR(1000) NULL,
 
fecha_registro DATETIME2 NOT NULL
CONSTRAINT DF_ALERGIA_FECHA_REGISTRO
DEFAULT SYSDATETIME(),
 
CONSTRAINT PK_ALERGIA
PRIMARY KEY (id_alergia),
 
CONSTRAINT FK_ALERGIA_ESTUDIANTE
FOREIGN KEY (id_estudiante)
REFERENCES ESTUDIANTE(id_estudiante)
);
GO

INSERT INTO ALERGIA
(
id_estudiante,
alergia,
descripcion
)
VALUES
(
1,
'Penicilina',
'Reacción alérgica reportada por el encargado.'
),
(
1,
'Maní',
'Evitar el consumo de alimentos que contengan maní o trazas de maní.'
),
(
1,
'Mariscos',
'Presenta reacción alérgica al consumir mariscos y productos derivados.'
);
GO

CREATE TABLE OBSERVACION_MEDICA
(
    id_observacion INT IDENTITY(1,1) NOT NULL,

    id_estudiante INT NOT NULL,

    titulo NVARCHAR(150) NOT NULL,

    observacion NVARCHAR(2000) NOT NULL,

    fecha_registro DATETIME2 NOT NULL
        CONSTRAINT DF_OBSERVACION_FECHA
        DEFAULT SYSDATETIME(),

    CONSTRAINT PK_OBSERVACION_MEDICA
        PRIMARY KEY (id_observacion),

    CONSTRAINT FK_OBSERVACION_ESTUDIANTE
        FOREIGN KEY (id_estudiante)
        REFERENCES ESTUDIANTE(id_estudiante)
);
GO

Select * from ESTUDIANTE 