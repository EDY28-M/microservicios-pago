# 🚀 Despliegue en Render - PaymentGatewayService

## Pasos para desplegar

### 1. Subir a GitHub
```bash
cd PaymentGatewayService
git init
git add .
git commit -m "Preparar para Render"
git remote add origin https://github.com/TU_USUARIO/payment-gateway-service.git
git push -u origin main
```

### 2. Crear servicio en Render

1. Ve a [https://render.com](https://render.com)
2. Click en **New → Web Service**
3. Conecta tu repositorio de GitHub
4. Selecciona **Docker** como runtime
5. Configura el servicio:
   - **Name**: `payment-gateway-service`
   - **Region**: Oregon (o tu preferencia)
   - **Plan**: Free (o Starter para producción)

### 3. Configurar variables de entorno en Render

En el dashboard de Render, ve a **Environment** y agrega:

| Variable | Descripción |
|----------|-------------|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ConnectionStrings__DefaultConnection` | Tu connection string de SQL Server |
| `Stripe__SecretKey` | `sk_test_...` o `sk_live_...` |
| `Stripe__PublishableKey` | `pk_test_...` o `pk_live_...` |
| `Stripe__WebhookSecret` | `whsec_...` |
| `JwtSettings__SecretKey` | Tu clave JWT (debe coincidir con el backend principal) |
| `BackendPrincipal__BaseUrl` | URL de tu API principal (ej: `https://gestion-academica-api.onrender.com`) |

> ⚠️ **Importante**: Usa doble guión bajo `__` para las variables anidadas en .NET

### 4. Configurar Webhook de Stripe

1. Ve a [Stripe Dashboard → Webhooks](https://dashboard.stripe.com/webhooks)
2. Click en **Add endpoint**
3. URL: `https://TU-SERVICIO.onrender.com/api/payments/webhook`
4. Eventos a escuchar:
   - `payment_intent.succeeded`
   - `payment_intent.payment_failed`

### 5. Verificar despliegue

```bash
# Health check
curl https://TU-SERVICIO.onrender.com/health

# Debería responder:
# {"status":"healthy","timestamp":"2026-01-11T..."}
```

## Archivos creados

- `Dockerfile` - Build multi-stage para .NET 9.0
- `render.yaml` - Blueprint de Render (opcional, para IaC)
- `.dockerignore` - Excluye archivos innecesarios
- `appsettings.Production.json` - Config de producción sin secretos

## Notas

- El servicio usa el puerto asignado por Render via `$PORT`
- Los free tier services se "duermen" después de 15 min de inactividad
- El primer request después de dormir toma ~30 segundos
