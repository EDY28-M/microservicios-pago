-- Script para crear las tablas de Payment y PaymentItem
-- Ejecutar en la base de datos del sistema académico

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Payment')
BEGIN
    CREATE TABLE Payment (
        id INT PRIMARY KEY IDENTITY(1,1),
        idEstudiante INT NOT NULL,
        idPeriodo INT NOT NULL,
        stripe_payment_intent_id NVARCHAR(255) NOT NULL UNIQUE,
        stripe_customer_id NVARCHAR(255) NULL,
        amount DECIMAL(10,2) NOT NULL,
        currency VARCHAR(3) NOT NULL DEFAULT 'USD',
        status VARCHAR(50) NOT NULL, -- 'pending', 'succeeded', 'failed', 'canceled'
        payment_method VARCHAR(50) NULL,
        metadata_json NVARCHAR(MAX) NULL, -- JSON con detalles de cursos
        fecha_creacion DATETIME NOT NULL DEFAULT GETDATE(),
        fecha_actualizacion DATETIME NULL,
        fecha_pago_exitoso DATETIME NULL,
        error_message NVARCHAR(1000) NULL,
        procesado BIT NOT NULL DEFAULT 0,
        FOREIGN KEY (idEstudiante) REFERENCES Estudiante(id),
        FOREIGN KEY (idPeriodo) REFERENCES Periodo(id)
    );

    CREATE INDEX IX_Payment_idEstudiante ON Payment(idEstudiante);
    CREATE INDEX IX_Payment_idPeriodo ON Payment(idPeriodo);
    CREATE INDEX IX_Payment_stripe_payment_intent_id ON Payment(stripe_payment_intent_id);
    CREATE INDEX IX_Payment_status ON Payment(status);
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PaymentItem')
BEGIN
    CREATE TABLE PaymentItem (
        id INT PRIMARY KEY IDENTITY(1,1),
        idPayment INT NOT NULL,
        idCurso INT NOT NULL,
        cantidad INT NOT NULL DEFAULT 1,
        precio_unitario DECIMAL(10,2) NOT NULL,
        subtotal DECIMAL(10,2) NOT NULL,
        FOREIGN KEY (idPayment) REFERENCES Payment(id) ON DELETE CASCADE,
        FOREIGN KEY (idCurso) REFERENCES Curso(id)
    );

    CREATE INDEX IX_PaymentItem_idPayment ON PaymentItem(idPayment);
    CREATE INDEX IX_PaymentItem_idCurso ON PaymentItem(idCurso);
END
GO

PRINT 'Tablas Payment y PaymentItem creadas exitosamente';
