# Guía de Instalación - Payment Gateway Service

## Pasos de Instalación

### 1. Instalar Dependencias del Backend

```bash
cd PaymentGatewayService
dotnet restore
```

### 2. Configurar Base de Datos

Ejecutar el script SQL para crear las tablas:

```sql
-- Ejecutar create_payment_tables.sql en SQL Server Management Studio
-- O usar sqlcmd:
sqlcmd -S TU_SERVIDOR -d TU_BASE_DE_DATOS -i create_payment_tables.sql
```

### 3. Configurar appsettings.json

Editar `appsettings.json` con tus credenciales:

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
    "ApiKey": ""
  },
  "JwtSettings": {
    "SecretKey": "TU_JWT_SECRET_KEY_MISMO_QUE_BACKEND_PRINCIPAL",
    "Issuer": "PaymentGatewayAPI",
    "Audience": "PaymentGatewayClients"
  }
}
```

**Importante:** El `JwtSettings:SecretKey` debe ser el mismo que el del backend principal para que los tokens JWT sean válidos.

### 4. Configurar Webhook en Stripe

1. Ir a [Stripe Dashboard](https://dashboard.stripe.com/test/webhooks)
2. Click en "Add endpoint"
3. URL: `https://tu-dominio.com/api/webhooks/stripe` (o `http://localhost:5000/api/webhooks/stripe` para desarrollo con Stripe CLI)
4. Seleccionar eventos:
   - `payment_intent.succeeded`
   - `payment_intent.payment_failed`
   - `payment_intent.canceled`
5. Copiar el "Signing secret" y agregarlo a `appsettings.json` como `Stripe:WebhookSecret`

#### Para Desarrollo Local con Stripe CLI

```bash
# Instalar Stripe CLI
# https://stripe.com/docs/stripe-cli

# Login
stripe login

# Forward webhooks
stripe listen --forward-to localhost:5000/api/webhooks/stripe

# Copiar el webhook secret que aparece
```

### 5. Ejecutar el Microservicio

```bash
dotnet run
```

El servicio estará disponible en `http://localhost:5000` (o el puerto configurado en `launchSettings.json`).

### 6. Instalar Dependencias del Frontend

```bash
cd ../../FRONTEND_ADMIN_VERSION_FINAL/FRONTEND_ADMIN
npm install @stripe/stripe-js @stripe/react-stripe-js
```

### 7. Configurar Variables de Entorno del Frontend

Crear o editar `.env` o `.env.local`:

```env
VITE_STRIPE_PUBLISHABLE_KEY=pk_test_...
VITE_PAYMENT_API_URL=http://localhost:5000/api
VITE_BACKEND_API_URL=http://localhost:5251/api
```

### 8. Verificar Integración

1. Iniciar el backend principal
2. Iniciar el microservicio de pagos
3. Iniciar el frontend
4. Probar el flujo completo de pago

## Verificación

### Verificar que el Microservicio Está Funcionando

```bash
curl http://localhost:5000/health
```

Debería retornar:
```json
{
  "status": "healthy",
  "timestamp": "2024-01-01T00:00:00Z"
}
```

### Verificar que las Tablas se Crearon

```sql
SELECT * FROM Payment;
SELECT * FROM PaymentItem;
```

## Troubleshooting

### Error: "JWT SecretKey no está configurada"
- Verificar que `JwtSettings:SecretKey` esté en `appsettings.json`
- Debe ser el mismo que el del backend principal

### Error: "Stripe SecretKey no está configurada"
- Verificar que `Stripe:SecretKey` esté en `appsettings.json`
- Obtener de Stripe Dashboard → Developers → API keys

### Error: "BackendPrincipal:BaseUrl no está configurada"
- Verificar que la URL del backend principal sea correcta
- Verificar que el backend principal esté ejecutándose

### El webhook no funciona
- Verificar que el webhook secret esté correcto
- Verificar que la URL del webhook sea accesible públicamente (usar ngrok para desarrollo local)
- Verificar logs del microservicio

### El pago se completa pero no se matricula
- Verificar logs del microservicio
- Verificar que el endpoint `/api/estudiantes/matricular-pago` esté funcionando en el backend principal
- Verificar que el estudiante y período existan

## Próximos Pasos

1. Configurar HTTPS en producción
2. Configurar variables de entorno en el servidor
3. Configurar webhook en producción con URL pública
4. Implementar reembolsos si es necesario
5. Agregar logging y monitoreo
