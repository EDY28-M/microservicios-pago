using Microsoft.EntityFrameworkCore;
using PaymentGatewayService.DTOs;
using PaymentGatewayService.Infrastructure;
using PaymentGatewayService.Models;
using PaymentGatewayService.Services;
using System.Text.Json;

namespace PaymentGatewayService.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly PaymentDbContext _context;
        private readonly IStripeService _stripeService;
        private readonly IBackendIntegrationService _backendIntegrationService;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(
            PaymentDbContext context,
            IStripeService stripeService,
            IBackendIntegrationService backendIntegrationService,
            ILogger<PaymentService> logger)
        {
            _context = context;
            _stripeService = stripeService;
            _backendIntegrationService = backendIntegrationService;
            _logger = logger;
        }

        public async Task<PaymentResponseDto> CreatePaymentIntentAsync(int idEstudiante, CreatePaymentIntentDto dto)
        {
            // Calcular total
            var total = dto.Cursos.Sum(c => c.Precio * c.Cantidad);

            if (total <= 0)
            {
                throw new ArgumentException("El monto total debe ser mayor a 0");
            }

            // Preparar metadata
            var metadata = new Dictionary<string, string>
            {
                { "idEstudiante", idEstudiante.ToString() },
                { "idPeriodo", dto.IdPeriodo.ToString() },
                { "cursos", JsonSerializer.Serialize(dto.Cursos.Select(c => c.IdCurso)) }
            };

            if (dto.Metadata != null)
            {
                foreach (var item in dto.Metadata)
                {
                    metadata[item.Key] = item.Value;
                }
            }

            // Crear PaymentIntent en Stripe
            var paymentIntent = await _stripeService.CreatePaymentIntentAsync(
                total,
                "usd",
                metadata
            );

            // Guardar en BD
            var payment = new Payment
            {
                IdEstudiante = idEstudiante,
                IdPeriodo = dto.IdPeriodo,
                StripePaymentIntentId = paymentIntent.Id,
                StripeCustomerId = paymentIntent.CustomerId,
                Amount = total,
                Currency = "USD",
                Status = paymentIntent.Status,
                PaymentMethod = paymentIntent.PaymentMethodTypes?.FirstOrDefault(),
                MetadataJson = JsonSerializer.Serialize(dto.Cursos),
                FechaCreacion = DateTime.UtcNow
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            // Agregar items
            foreach (var curso in dto.Cursos)
            {
                var item = new PaymentItem
                {
                    IdPayment = payment.Id,
                    IdCurso = curso.IdCurso,
                    Cantidad = curso.Cantidad,
                    PrecioUnitario = curso.Precio,
                    Subtotal = curso.Precio * curso.Cantidad
                };
                _context.PaymentItems.Add(item);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("PaymentIntent creado: {PaymentIntentId} para estudiante {IdEstudiante}", 
                paymentIntent.Id, idEstudiante);

            return new PaymentResponseDto
            {
                Id = payment.Id,
                ClientSecret = paymentIntent.ClientSecret,
                PaymentIntentId = paymentIntent.Id,
                Amount = total,
                Currency = "USD",
                Status = paymentIntent.Status,
                FechaCreacion = payment.FechaCreacion,
                Items = dto.Cursos.Select(c => new PaymentItemDto
                {
                    IdCurso = c.IdCurso,
                    Cantidad = c.Cantidad,
                    PrecioUnitario = c.Precio,
                    Subtotal = c.Precio * c.Cantidad
                }).ToList()
            };
        }

        public async Task<PaymentStatusDto?> GetPaymentStatusAsync(string paymentIntentId)
        {
            var payment = await _context.Payments
                .Include(p => p.PaymentItems)
                .FirstOrDefaultAsync(p => p.StripePaymentIntentId == paymentIntentId);

            if (payment == null)
                return null;

            return new PaymentStatusDto
            {
                Id = payment.Id,
                Status = payment.Status,
                Amount = payment.Amount,
                FechaPagoExitoso = payment.FechaPagoExitoso,
                Procesado = payment.Procesado,
                ErrorMessage = payment.ErrorMessage,
                Items = payment.PaymentItems.Select(i => new PaymentItemDto
                {
                    IdCurso = i.IdCurso,
                    Cantidad = i.Cantidad,
                    PrecioUnitario = i.PrecioUnitario,
                    Subtotal = i.Subtotal
                }).ToList()
            };
        }

        public async Task<List<PaymentStatusDto>> GetPaymentsByEstudianteAsync(int idEstudiante)
        {
            var payments = await _context.Payments
                .Include(p => p.PaymentItems)
                .Where(p => p.IdEstudiante == idEstudiante)
                .OrderByDescending(p => p.FechaCreacion)
                .ToListAsync();

            return payments.Select(p => new PaymentStatusDto
            {
                Id = p.Id,
                Status = p.Status,
                Amount = p.Amount,
                FechaPagoExitoso = p.FechaPagoExitoso,
                Procesado = p.Procesado,
                ErrorMessage = p.ErrorMessage,
                Items = p.PaymentItems.Select(i => new PaymentItemDto
                {
                    IdCurso = i.IdCurso,
                    Cantidad = i.Cantidad,
                    PrecioUnitario = i.PrecioUnitario,
                    Subtotal = i.Subtotal
                }).ToList()
            }).ToList();
        }

        public async Task<Payment?> GetPaymentByIntentIdAsync(string paymentIntentId)
        {
            return await _context.Payments
                .Include(p => p.PaymentItems)
                .FirstOrDefaultAsync(p => p.StripePaymentIntentId == paymentIntentId);
        }

        public async Task<bool> ProcessPaymentSuccessAsync(string paymentIntentId)
        {
            var payment = await GetPaymentByIntentIdAsync(paymentIntentId);

            if (payment == null)
            {
                _logger.LogWarning("Payment no encontrado: {PaymentIntentId}", paymentIntentId);
                return false;
            }

            if (payment.Procesado)
            {
                _logger.LogInformation("Payment ya procesado: {PaymentIntentId}", paymentIntentId);
                return true;
            }

            try
            {
                // Verificar si es un pago de matrícula (sin cursos) o pago de cursos
                var isMatriculaPayment = payment.MetadataJson != null && 
                                        payment.MetadataJson.Contains("matricula") &&
                                        (!payment.PaymentItems.Any() || payment.PaymentItems.All(i => i.IdCurso == 0));

                if (isMatriculaPayment)
                {
                    // Es solo el pago de matrícula, no necesita matricular cursos
                    payment.Procesado = true;
                    payment.FechaPagoExitoso = DateTime.UtcNow;
                    payment.FechaActualizacion = DateTime.UtcNow;
                    payment.Status = "succeeded";
                    payment.ErrorMessage = null;

                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Pago de matrícula procesado exitosamente: {PaymentIntentId}", paymentIntentId);
                    return true;
                }
                else
                {
                    // Es un pago de cursos, proceder con la matrícula
                    var cursos = payment.PaymentItems.Where(i => i.IdCurso > 0).Select(i => i.IdCurso).ToList();

                    if (!cursos.Any())
                    {
                        payment.ErrorMessage = "No hay cursos para matricular";
                        payment.FechaActualizacion = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                        return false;
                    }

                    // Llamar al backend principal para matricular
                    var matriculaDto = new MatriculaPagoDto
                    {
                        IdEstudiante = payment.IdEstudiante,
                        IdPeriodo = payment.IdPeriodo,
                        IdsCursos = cursos,
                        StripePaymentIntentId = paymentIntentId
                    };

                    var matriculaExitoso = await _backendIntegrationService.MatricularEstudianteAsync(matriculaDto);

                    if (matriculaExitoso)
                    {
                        payment.Procesado = true;
                        payment.FechaPagoExitoso = DateTime.UtcNow;
                        payment.FechaActualizacion = DateTime.UtcNow;
                        payment.Status = "succeeded";
                        payment.ErrorMessage = null;

                        await _context.SaveChangesAsync();

                        _logger.LogInformation("Pago procesado exitosamente: {PaymentIntentId}", paymentIntentId);
                        return true;
                    }
                    else
                    {
                        payment.ErrorMessage = "Error al matricular estudiante en el backend principal";
                        payment.FechaActualizacion = DateTime.UtcNow;
                        await _context.SaveChangesAsync();

                        _logger.LogError("Error al procesar matrícula para pago: {PaymentIntentId}", paymentIntentId);
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                payment.ErrorMessage = $"Excepción: {ex.Message}";
                payment.FechaActualizacion = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogError(ex, "Excepción al procesar pago: {PaymentIntentId}", paymentIntentId);
                return false;
            }
        }

        public async Task<PaymentResponseDto> CreateMatriculaPaymentIntentAsync(int idEstudiante, int idPeriodo)
        {
            // Monto fijo de matrícula: 5 PEN (mínimo de Stripe es ~2 PEN)
            const decimal matriculaAmount = 5.00m; 
            const string currency = "pen"; // Soles peruanos

            // Preparar metadata
            var metadata = new Dictionary<string, string>
            {
                { "idEstudiante", idEstudiante.ToString() },
                { "idPeriodo", idPeriodo.ToString() },
                { "tipo", "matricula" } // Marcar como pago de matrícula
            };

            // Crear PaymentIntent en Stripe
            var paymentIntent = await _stripeService.CreatePaymentIntentAsync(
                matriculaAmount,
                currency,
                metadata
            );

            // Guardar en BD
            var payment = new Payment
            {
                IdEstudiante = idEstudiante,
                IdPeriodo = idPeriodo,
                StripePaymentIntentId = paymentIntent.Id,
                StripeCustomerId = paymentIntent.CustomerId,
                Amount = matriculaAmount,
                Currency = currency.ToUpper(),
                Status = paymentIntent.Status,
                PaymentMethod = paymentIntent.PaymentMethodTypes?.FirstOrDefault(),
                MetadataJson = JsonSerializer.Serialize(new { tipo = "matricula" }),
                FechaCreacion = DateTime.UtcNow
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            _logger.LogInformation("PaymentIntent de matrícula creado: {PaymentIntentId} para estudiante {IdEstudiante}", 
                paymentIntent.Id, idEstudiante);

            return new PaymentResponseDto
            {
                Id = payment.Id,
                ClientSecret = paymentIntent.ClientSecret,
                PaymentIntentId = paymentIntent.Id,
                Amount = matriculaAmount,
                Currency = currency.ToUpper(),
                Status = paymentIntent.Status,
                FechaCreacion = payment.FechaCreacion,
                Items = new List<PaymentItemDto>
                {
                    new PaymentItemDto
                    {
                        IdCurso = 0, // No es un curso específico
                        NombreCurso = "Matrícula",
                        Cantidad = 1,
                        PrecioUnitario = matriculaAmount,
                        Subtotal = matriculaAmount
                    }
                }
            };
        }

        public async Task<bool> HasPaidMatriculaAsync(int idEstudiante, int idPeriodo)
        {
            _logger.LogInformation("[HASPAID] Verificando pago de matrícula: idEstudiante={IdEstudiante}, idPeriodo={IdPeriodo}", 
                idEstudiante, idPeriodo);

            // Buscar todos los pagos del estudiante en el periodo para debugging
            var allPayments = await _context.Payments
                .Where(p => p.IdEstudiante == idEstudiante && p.IdPeriodo == idPeriodo)
                .ToListAsync();

            _logger.LogInformation("[HASPAID] Total pagos encontrados para estudiante {IdEstudiante}, periodo {IdPeriodo}: {Count}", 
                idEstudiante, idPeriodo, allPayments.Count);

            foreach (var p in allPayments)
            {
                _logger.LogInformation("[HASPAID] Pago ID={PaymentId}, Status={Status}, Metadata={Metadata}, Amount={Amount}", 
                    p.Id, p.Status, p.MetadataJson, p.Amount);
            }

            // Solo verificamos que exista un pago exitoso de matrícula
            // No requerimos Procesado=true porque el webhook puede tardar
            var payment = await _context.Payments
                .Where(p => 
                    p.IdEstudiante == idEstudiante &&
                    p.IdPeriodo == idPeriodo &&
                    p.Status == "succeeded" &&
                    (p.MetadataJson != null && (p.MetadataJson.Contains("matricula") || p.MetadataJson.Contains("\"tipo\":\"matricula\""))))
                .FirstOrDefaultAsync();

            var result = payment != null;
            _logger.LogInformation("[HASPAID] Resultado: {Result} (payment ID: {PaymentId})", 
                result, payment?.Id);
            
            return result;
        }
    }
}
