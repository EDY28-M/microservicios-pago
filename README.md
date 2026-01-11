# Payment Gateway Service - Microservicio de Pagos con Stripe

Microservicio independiente para procesar pagos de matrícula usando Stripe, integrado con el sistema académico principal.

## Características

- ✅ Integración completa con Stripe
- ✅ Webhooks para procesamiento automático de pagos
- ✅ Integración con backend principal para matrícula automática
- ✅ Autenticación JWT
- ✅ Base de datos independiente para registros de pago

## Requisitos Previos

- .NET 9.0 SDK
- SQL Server (misma base de datos del sistema principal)
- Cuenta de Stripe (test o producción)
- Backend principal ejecutándose

## Configuración

### 1. Variables de Entorno / appsettings.json

Editar `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "TU_CONNECTION_STRING"
  },
  "Stripe": {
    "SecretKey": "sk_test_...",
    "PublishableKey": "pk_test_...",
    "WebhookSecret": "whsec_..."
  },
  "BackendPrincipal": {
    "BaseUrl": "http://localhost:5251",
    "ApiKey": "" // Opcional
  },
  "JwtSettings": {
    "SecretKey": "TU_JWT_SECRET_KEY",
    "Issuer": "PaymentGatewayAPI",
    "Audience": "PaymentGatewayClients"
  }
}
```

### 2. Crear Tablas en Base de Datos

Ejecutar el script SQL:

```bash
sqlcmd -S TU_SERVIDOR -d TU_BASE_DE_DATOS -i create_payment_tables.sql
```

O ejecutar manualmente el contenido de `create_payment_tables.sql` en SQL Server Management Studio.

### 3. Configurar Webhook en Stripe Dashboard

1. Ir a Stripe Dashboard → Developers → Webhooks
2. Agregar endpoint: `https://tu-dominio.com/api/webhooks/stripe`
3. Seleccionar eventos:
   - `payment_intent.succeeded`
   - `payment_intent.payment_failed`
   - `payment_intent.canceled`
4. Copiar el "Signing secret" y agregarlo a `appsettings.json` como `Stripe:WebhookSecret`

## Ejecución

```bash
cd PaymentGatewayService
dotnet run
```

El servicio estará disponible en `http://localhost:5000` (o el puerto configurado).

## Endpoints

### POST `/api/payments/create-intent`
Crea un Payment Intent para procesar el pago.

**Headers:**
```
Authorization: Bearer {JWT_TOKEN}
```

**Body:**
```json
{
  "idPeriodo": 1,
  "cursos": [
    {
      "idCurso": 1,
      "precio": 100.00,
      "cantidad": 1
    }
  ]
}
```

**Response:**
```json
{
  "id": 1,
  "clientSecret": "pi_xxx_secret_xxx",
  "paymentIntentId": "pi_xxx",
  "amount": 100.00,
  "currency": "USD",
  "status": "requires_payment_method"
}
```

### POST `/api/webhooks/stripe`
Endpoint para recibir webhooks de Stripe (público, pero verifica firma).

### GET `/api/payments/status/{paymentIntentId}`
Obtiene el estado de un pago.

### GET `/api/payments/historial`
Obtiene el historial de pagos del estudiante autenticado.

## Integración con Backend Principal

El microservicio llama al endpoint del backend principal:

**POST** `http://backend-principal/api/estudiantes/matricular-pago`

**Body:**
```json
{
  "idEstudiante": 1,
  "idPeriodo": 1,
  "idsCursos": [1, 2, 3],
  "stripePaymentIntentId": "pi_xxx"
}
```

Este endpoint debe estar implementado en el `EstudiantesController` del backend principal.

## Frontend

Ver documentación en `FRONTEND_ADMIN_VERSION_FINAL/FRONTEND_ADMIN/README_PAYMENTS.md`

## Testing

### Usar Tarjetas de Prueba de Stripe

- **Éxito:** `4242 4242 4242 4242`
- **Rechazo:** `4000 0000 0000 0002`
- **3D Secure:** `4000 0025 0000 3155`

Fecha de expiración: cualquier fecha futura
CVC: cualquier 3 dígitos

## Troubleshooting

### Error: "Stripe SecretKey no está configurada"
- Verificar que `Stripe:SecretKey` esté en `appsettings.json`

### Error: "WebhookSecret no está configurada"
- Configurar el webhook secret de Stripe Dashboard

### Error: "BackendPrincipal:BaseUrl no está configurada"
- Verificar la URL del backend principal en `appsettings.json`

### El pago se completa pero no se matricula
- Verificar logs del microservicio
- Verificar que el webhook esté configurado correctamente
- Verificar que el endpoint `/api/estudiantes/matricular-pago` esté funcionando

## Estructura del Proyecto

```
PaymentGatewayService/
├── Controllers/
│   ├── PaymentsController.cs      # Endpoints de pagos
│   └── WebhooksController.cs      # Webhooks de Stripe
├── Services/
│   ├── StripeService.cs          # Integración con Stripe
│   ├── PaymentService.cs        # Lógica de negocio de pagos
│   └── BackendIntegrationService.cs # Integración con backend principal
├── Models/
│   ├── Payment.cs
│   └── PaymentItem.cs
├── DTOs/
│   ├── CreatePaymentIntentDto.cs
│   ├── PaymentResponseDto.cs
│   └── MatriculaPagoDto.cs
├── Infrastructure/
│   └── PaymentDbContext.cs
└── Program.cs
```

## Licencia

Este proyecto es parte del sistema de gestión académica.
