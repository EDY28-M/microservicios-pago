# 🚀 Quick Start - Configuración Rápida

## ⚡ PASO 1: Obtener Credenciales de Stripe (5 minutos)

1. Ve a: https://dashboard.stripe.com/test/apikeys
2. Copia:
   - **Publishable key**: `pk_test_...` (visible)
   - **Secret key**: `sk_test_...` (click en "Reveal test key")

---

## ⚡ PASO 2: Configurar appsettings.json del Microservicio

**Archivo:** `BACKEND_DEVELOMENT\PaymentGatewayService\appsettings.json`

Reemplaza los valores vacíos:

```json
{
  "Stripe": {
    "SecretKey": "sk_test_COPIA_AQUI_TU_SECRET_KEY",
    "PublishableKey": "pk_test_COPIA_AQUI_TU_PUBLISHABLE_KEY",
    "WebhookSecret": ""  // Lo llenamos después
  },
  "JwtSettings": {
    "SecretKey": "GENERA_UN_SECRET_KEY_AQUI",  // ⚠️ IMPORTANTE: Mismo que backend principal
    "Issuer": "PaymentGatewayAPI",
    "Audience": "PaymentGatewayClients"
  }
}
```

### 🔑 Generar JWT Secret Key

**Opción 1 - PowerShell:**
```powershell
[Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes([System.Guid]::NewGuid().ToString() + [System.Guid]::NewGuid().ToString()))
```

**Opción 2 - Online:**
https://generate-secret.vercel.app/64

**⚠️ IMPORTANTE:** Copia el mismo valor en:
- `PaymentGatewayService\appsettings.json` → `JwtSettings:SecretKey`
- `API_REST_CURSOSACADEMICOS\appsettings.json` → `JwtSettings:SecretKey`

---

## ⚡ PASO 3: Configurar Variables del Frontend

**Archivo:** `FRONTEND_ADMIN_VERSION_FINAL\FRONTEND_ADMIN\.env`

Agrega estas líneas (o crea el archivo si no existe):

```env
VITE_STRIPE_PUBLISHABLE_KEY=pk_test_COPIA_AQUI_TU_PUBLISHABLE_KEY
VITE_PAYMENT_API_URL=http://localhost:5000/api
VITE_BACKEND_API_URL=http://localhost:5251/api
```

**Luego reinicia el servidor de desarrollo del frontend.**

---

## ⚡ PASO 4: Configurar Webhook (Desarrollo Local)

### Instalar Stripe CLI

**Windows (Chocolatey):**
```powershell
choco install stripe
```

**O descarga desde:** https://stripe.com/docs/stripe-cli

### Login y Forward Webhooks

```bash
# 1. Login
stripe login

# 2. En una terminal separada (mientras el microservicio corre)
stripe listen --forward-to localhost:5000/api/webhooks/stripe
```

**Este comando mostrará un `whsec_...` - CÓPIALO**

### Agregar al appsettings.json

```json
"Stripe": {
  "SecretKey": "sk_test_...",
  "PublishableKey": "pk_test_...",
  "WebhookSecret": "whsec_EL_SECRET_QUE_TE_DIO_STRIPE_CLI"
}
```

**Mantén `stripe listen` corriendo mientras pruebas.**

---

## ⚡ PASO 5: Probar

### Iniciar servicios (en orden):

1. **Backend Principal:**
   ```bash
   cd BACKEND_DEVELOMENT\API_REST_CURSOSACADEMICOS
   dotnet run
   ```

2. **Microservicio de Pagos:**
   ```bash
   cd BACKEND_DEVELOMENT\PaymentGatewayService
   dotnet run
   ```

3. **Stripe CLI:**
   ```bash
   stripe listen --forward-to localhost:5000/api/webhooks/stripe
   ```

4. **Frontend:**
   ```bash
   cd FRONTEND_ADMIN_VERSION_FINAL\FRONTEND_ADMIN
   npm run dev
   ```

### Probar el flujo:

1. Inicia sesión como estudiante: `/estudiante/login`
2. Ve a matrícula: `/estudiante/matricula`
3. Navega a pago: `/estudiante/pago-matricula`
4. Usa tarjeta de prueba: `4242 4242 4242 4242`
5. Fecha: `12/25`, CVC: `123`

---

## ✅ Verificación Rápida

- [ ] http://localhost:5000/health → Debe retornar `{"status":"healthy"}`
- [ ] http://localhost:5251/swagger → Debe mostrar Swagger
- [ ] Frontend carga sin errores en consola
- [ ] Puedes iniciar sesión como estudiante
- [ ] Puedes ver la página de pago

---

## 🆘 Problemas Comunes

**"Stripe SecretKey no está configurada"**
→ Verifica `appsettings.json` del microservicio

**"JWT SecretKey no está configurada"**
→ Debe estar en AMBOS `appsettings.json` (mismo valor)

**"Failed to create payment intent"**
→ Verifica que el microservicio esté corriendo en puerto 5000

**El pago se completa pero no matricula**
→ Verifica que `stripe listen` esté corriendo
→ Verifica logs del microservicio
