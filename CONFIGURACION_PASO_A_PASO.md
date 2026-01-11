# Guía Paso a Paso - Configuración Completa

## 📋 PASO 1: Obtener Credenciales de Stripe

### 1.1 Crear cuenta en Stripe (si no tienes)

1. Ve a https://stripe.com
2. Crea una cuenta (es gratis)
3. Activa el modo de prueba (Test Mode)

### 1.2 Obtener las API Keys

1. Ve a https://dashboard.stripe.com/test/apikeys
2. Encontrarás dos claves:
   - **Publishable key** (empieza con `pk_test_...`) → Para el frontend
   - **Secret key** (empieza con `sk_test_...`) → Para el backend
3. Haz clic en "Reveal test key" para ver la Secret key

### 1.3 Copiar las claves

- **Publishable key**: `pk_test_...` (la verás directamente)
- **Secret key**: `sk_test_...` (haz clic en "Reveal" para verla)

---

## 📋 PASO 2: Configurar appsettings.json del Microservicio

Edita el archivo:
`BACKEND_DEVELOMENT\PaymentGatewayService\appsettings.json`

### 2.1 Configurar Stripe

```json
"Stripe": {
  "SecretKey": "sk_test_TU_SECRET_KEY_AQUI",
  "PublishableKey": "pk_test_TU_PUBLISHABLE_KEY_AQUI",
  "WebhookSecret": ""  // Lo configuramos después en el paso 4
}
```

### 2.2 Configurar JWT (IMPORTANTE)

El `SecretKey` debe ser el **MISMO** que el del backend principal.

1. Abre `API_REST_CURSOSACADEMICOS\appsettings.json`
2. Si tiene un `JwtSettings:SecretKey`, cópialo
3. Si está vacío, genera uno nuevo (ver abajo)

**Generar un JWT Secret Key nuevo:**

Puedes usar este comando en PowerShell:
```powershell
[Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes([System.Guid]::NewGuid().ToString() + [System.Guid]::NewGuid().ToString()))
```

O usar este sitio: https://generate-secret.vercel.app/64

**Luego, copia el mismo valor en AMBOS archivos:**

- `PaymentGatewayService\appsettings.json` → `JwtSettings:SecretKey`
- `API_REST_CURSOSACADEMICOS\appsettings.json` → `JwtSettings:SecretKey`

### 2.3 Configurar Backend Principal URL

```json
"BackendPrincipal": {
  "BaseUrl": "http://localhost:5251",  // O la URL donde corre tu backend principal
  "ApiKey": ""  // Opcional, dejar vacío si no usas API key
}
```

**Ejemplo completo de appsettings.json:**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=SQL5113.site4now.net;Initial Catalog=db_ac27fb_sistemagestiontram;User Id=db_ac27fb_sistemagestiontram_admin;Password=JUNIOR28.edy;TrustServerCertificate=True;MultipleActiveResultSets=True;"
  },
  "Stripe": {
    "SecretKey": "sk_test_51ABC123...",
    "PublishableKey": "pk_test_51ABC123...",
    "WebhookSecret": ""
  },
  "BackendPrincipal": {
    "BaseUrl": "http://localhost:5251",
    "ApiKey": ""
  },
  "JwtSettings": {
    "SecretKey": "TU_JWT_SECRET_KEY_AQUI_MISMO_QUE_BACKEND_PRINCIPAL",
    "Issuer": "PaymentGatewayAPI",
    "Audience": "PaymentGatewayClients"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

---

## 📋 PASO 3: Configurar Variables de Entorno del Frontend

### 3.1 Editar archivo .env

Edita el archivo:
`FRONTEND_ADMIN_VERSION_FINAL\FRONTEND_ADMIN\.env`

Agrega estas líneas:

```env
# Stripe Publishable Key (la misma que copiaste en el paso 1)
VITE_STRIPE_PUBLISHABLE_KEY=pk_test_TU_PUBLISHABLE_KEY_AQUI

# URL del microservicio de pagos
VITE_PAYMENT_API_URL=http://localhost:5000/api

# URL del backend principal
VITE_BACKEND_API_URL=http://localhost:5251/api
```

**Ejemplo completo:**

```env
VITE_STRIPE_PUBLISHABLE_KEY=pk_test_51ABC123...
VITE_PAYMENT_API_URL=http://localhost:5000/api
VITE_BACKEND_API_URL=http://localhost:5251/api
```

### 3.2 Reiniciar el servidor de desarrollo

Si el frontend ya está corriendo, **deténlo y vuelve a iniciarlo** para que cargue las nuevas variables:

```bash
# Detener (Ctrl+C)
# Luego iniciar de nuevo
npm run dev
```

---

## 📋 PASO 4: Configurar Webhook en Stripe Dashboard

### Opción A: Para Desarrollo Local (Recomendado para pruebas)

#### 4.1 Instalar Stripe CLI

1. Descarga desde: https://stripe.com/docs/stripe-cli
2. O con Chocolatey: `choco install stripe`
3. O con Scoop: `scoop install stripe`

#### 4.2 Login en Stripe CLI

```bash
stripe login
```

Te pedirá autenticarte en el navegador.

#### 4.3 Iniciar el forwarding de webhooks

En una terminal separada (mientras el microservicio está corriendo):

```bash
stripe listen --forward-to localhost:5000/api/webhooks/stripe
```

**IMPORTANTE:** Este comando mostrará un `whsec_...` (webhook secret). **Cópialo**.

#### 4.4 Agregar el Webhook Secret al appsettings.json

```json
"Stripe": {
  "SecretKey": "sk_test_...",
  "PublishableKey": "pk_test_...",
  "WebhookSecret": "whsec_EL_SECRET_QUE_TE_DIO_STRIPE_CLI"
}
```

**Mantén la terminal de `stripe listen` corriendo** mientras pruebas.

### Opción B: Para Producción (con URL pública)

#### 4.1 Usar ngrok para exponer tu localhost

```bash
# Instalar ngrok: https://ngrok.com/download
ngrok http 5000
```

Esto te dará una URL pública como: `https://abc123.ngrok.io`

#### 4.2 Configurar webhook en Stripe Dashboard

1. Ve a https://dashboard.stripe.com/test/webhooks
2. Click en "Add endpoint"
3. Endpoint URL: `https://abc123.ngrok.io/api/webhooks/stripe`
4. Seleccionar eventos:
   - ✅ `payment_intent.succeeded`
   - ✅ `payment_intent.payment_failed`
   - ✅ `payment_intent.canceled`
5. Click en "Add endpoint"
6. Copia el "Signing secret" (empieza con `whsec_...`)
7. Agrégalo a `appsettings.json` como `Stripe:WebhookSecret`

---

## 📋 PASO 5: Probar el Flujo Completo

### 5.1 Iniciar los servicios en orden

**Terminal 1 - Backend Principal:**
```bash
cd BACKEND_DEVELOMENT\API_REST_CURSOSACADEMICOS
dotnet run
```
Debería estar en: `http://localhost:5251`

**Terminal 2 - Microservicio de Pagos:**
```bash
cd BACKEND_DEVELOMENT\PaymentGatewayService
dotnet run
```
Debería estar en: `http://localhost:5000` (o el puerto que configuraste)

**Terminal 3 - Stripe CLI (si usas desarrollo local):**
```bash
stripe listen --forward-to localhost:5000/api/webhooks/stripe
```

**Terminal 4 - Frontend:**
```bash
cd FRONTEND_ADMIN_VERSION_FINAL\FRONTEND_ADMIN
npm run dev
```

### 5.2 Verificar que todo esté funcionando

1. **Backend Principal:**
   - Abre: http://localhost:5251/swagger
   - Deberías ver los endpoints

2. **Microservicio de Pagos:**
   - Abre: http://localhost:5000/health
   - Debería retornar: `{"status":"healthy",...}`

3. **Frontend:**
   - Abre: http://localhost:5173 (o el puerto que use Vite)
   - Deberías ver la aplicación

### 5.3 Probar el flujo de pago

1. **Iniciar sesión como estudiante:**
   - Ve a `/estudiante/login`
   - Inicia sesión con credenciales de estudiante

2. **Ir a matrícula:**
   - Ve a `/estudiante/matricula`
   - Selecciona cursos (si tienes la funcionalidad de selección)

3. **Ir a pago:**
   - Click en "Pagar y Matricular" o navega a `/estudiante/pago-matricula`
   - Deberías ver el formulario de pago

4. **Probar con tarjeta de prueba:**
   - **Tarjeta de éxito:** `4242 4242 4242 4242`
   - **Fecha:** Cualquier fecha futura (ej: 12/25)
   - **CVC:** Cualquier 3 dígitos (ej: 123)
   - **ZIP:** Cualquier código postal (ej: 12345)

5. **Completar el pago:**
   - Ingresa los datos de la tarjeta
   - Click en "Pagar y Matricular"
   - Deberías ver "Pago Exitoso"
   - Espera a que se procese la matrícula
   - Debería redirigir a `/estudiante/mis-cursos`

### 5.4 Verificar en la base de datos

```sql
-- Ver pagos creados
SELECT * FROM Payment ORDER BY fecha_creacion DESC;

-- Ver items de pago
SELECT * FROM PaymentItem;

-- Ver matrículas creadas
SELECT * FROM Matricula WHERE isAutorizado = 1 ORDER BY fecha_matricula DESC;
```

### 5.5 Verificar logs

Revisa las consolas de:
- Backend Principal (debería mostrar la llamada a `matricular-pago`)
- Microservicio de Pagos (debería mostrar el webhook recibido)
- Stripe CLI (si usas desarrollo local, debería mostrar eventos)

---

## 🔧 Troubleshooting

### Error: "Stripe SecretKey no está configurada"
- Verifica que `Stripe:SecretKey` esté en `appsettings.json`
- Asegúrate de que no tenga espacios extra

### Error: "JWT SecretKey no está configurada"
- Verifica que `JwtSettings:SecretKey` esté en ambos `appsettings.json`
- Deben ser **exactamente iguales** en ambos archivos

### Error: "Failed to create payment intent"
- Verifica que el microservicio esté corriendo
- Verifica que `VITE_PAYMENT_API_URL` sea correcta
- Verifica que el token JWT sea válido (inicia sesión primero)

### El pago se completa pero no se matricula
- Verifica que el webhook esté configurado
- Verifica logs del microservicio
- Verifica que el endpoint `/api/estudiantes/matricular-pago` exista en el backend principal
- Verifica que el estudiante y período existan en la BD

### El webhook no funciona
- Si usas desarrollo local, asegúrate de que `stripe listen` esté corriendo
- Verifica que el `WebhookSecret` sea correcto
- Verifica que la URL del webhook sea accesible

---

## ✅ Checklist Final

- [ ] Credenciales de Stripe configuradas en `appsettings.json`
- [ ] JWT SecretKey configurado (mismo en ambos backends)
- [ ] Variables de entorno del frontend configuradas
- [ ] Webhook configurado (Stripe CLI o Dashboard)
- [ ] Backend principal corriendo
- [ ] Microservicio de pagos corriendo
- [ ] Frontend corriendo
- [ ] Probar flujo completo con tarjeta de prueba

¡Listo! 🎉
