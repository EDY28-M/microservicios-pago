# Implementación de Pago de Matrícula

## ✅ Cambios Realizados

### Backend - Microservicio de Pagos

1. **Nuevo endpoint para pagar matrícula:**
   - `POST /api/payments/pagar-matricula` - Crea Payment Intent para pagar matrícula (1 PEN)

2. **Nuevo endpoint para verificar pago:**
   - `GET /api/payments/verificar-matricula-pagada/{idPeriodo}` - Verifica si el estudiante pagó la matrícula

3. **Métodos agregados a PaymentService:**
   - `CreateMatriculaPaymentIntentAsync()` - Crea Payment Intent de 1 PEN para matrícula
   - `HasPaidMatriculaAsync()` - Verifica si el estudiante pagó la matrícula

4. **Webhook actualizado:**
   - Ahora diferencia entre pago de matrícula y pago de cursos
   - Los pagos de matrícula NO intentan matricular cursos automáticamente
   - Solo marcan el pago como procesado

### Backend Principal

1. **Validación en endpoint de matrícula:**
   - `POST /api/estudiantes/matricular` ahora valida que el estudiante haya pagado la matrícula
   - Retorna error si no ha pagado: `"Debes pagar la matrícula antes de poder matricular cursos"`

2. **Método de verificación:**
   - `VerificarPagoMatriculaAsync()` - Llama al microservicio para verificar el pago

3. **HttpClient configurado:**
   - Agregado `AddHttpClient()` en Program.cs para comunicación con microservicio

### Frontend

1. **Nueva página de pago de matrícula:**
   - `PagoMatriculaInicialPage.tsx` - Página dedicada para pagar la matrícula (1 PEN)
   - Ruta: `/estudiante/pago-matricula-inicial`

2. **AumentoCursosPage actualizado:**
   - Verifica si el estudiante ha pagado la matrícula antes de permitir matricular
   - Muestra alerta si no ha pagado con botón para ir a pagar
   - Deshabilita el botón de matricular si no ha pagado

3. **Nuevo método en estudiantesApi:**
   - `verificarMatriculaPagada()` - Verifica el estado del pago de matrícula

## 🔄 Flujo Completo

1. **Estudiante intenta matricular:**
   - Va a `/estudiante/aumento-cursos`
   - Si no ha pagado, ve alerta y botón "Pagar Matrícula"

2. **Pago de matrícula:**
   - Click en "Pagar Matrícula" → `/estudiante/pago-matricula-inicial`
   - Crea Payment Intent de 1 PEN
   - Usuario paga con Stripe
   - Webhook procesa el pago (solo marca como pagado, no matricula cursos)

3. **Después del pago:**
   - Redirige a `/estudiante/aumento-cursos`
   - Ahora puede seleccionar y matricular cursos
   - El backend valida que haya pagado antes de permitir matrícula

4. **Matrícula de cursos:**
   - Estudiante selecciona cursos
   - Click en "Matricular"
   - Backend valida pago de matrícula → Permite matricular

## 📝 Notas Importantes

- **Moneda:** PEN (Soles Peruanos)
- **Monto:** 1.00 PEN fijo para matrícula
- **Validación:** Tanto frontend como backend validan el pago
- **Webhook:** Los pagos de matrícula NO matricular cursos automáticamente
- **ID Estudiante:** El microservicio obtiene el ID del estudiante desde el backend principal usando el perfil del usuario autenticado

## 🧪 Testing

1. Iniciar sesión como estudiante
2. Intentar matricular sin pagar → Debe mostrar error
3. Ir a pagar matrícula → Completar pago con tarjeta de prueba
4. Volver a aumento de cursos → Debe permitir matricular
5. Matricular cursos → Debe funcionar correctamente
