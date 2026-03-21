namespace ClockworkUmbraco.Middlewares
{
    public class CSPMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IWebHostEnvironment _environment;

        public CSPMiddleware(RequestDelegate next, IWebHostEnvironment environment)
        {
            _next = next;
            _environment = environment;
        }

        public async Task Invoke(HttpContext context)
        {
            // Development modunda Browser Link için localhost bağlantılarına izin ver
            var isDevelopment = _environment.IsDevelopment();
            var localhostConnections = isDevelopment ? " http://localhost:* ws://localhost:* wss://localhost:*" : "";

            // Content Security Policy - Basit örnek konfigürasyon
            var cspHeader = string.Join(" ", new[]
            {
                "default-src 'self';", // Varsayılan olarak sadece kendi domain'den kaynaklara izin ver
                "script-src 'self' 'unsafe-inline' 'unsafe-eval';", // Script'ler için örnek - inline script'lere izin ver
                "style-src 'self' 'unsafe-inline';", // CSS'ler için örnek - inline style'lara izin ver
                "img-src 'self' data:;", // Resimler için örnek - data URL'lerine izin ver
                "connect-src 'self'" + localhostConnections + ";", // AJAX bağlantıları için örnek
                "frame-src 'self' https://marketplace.umbraco.com/;",
            });

            context.Response.Headers.Append("Content-Security-Policy", cspHeader);

            // HTTP Strict Transport Security - HTTPS kullanımını zorunlu kılar
            context.Response.Headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains; preload");

            // X-Frame-Options - Clickjacking saldırılarına karşı koruma
            context.Response.Headers.Append("X-Frame-Options", "SAMEORIGIN");

            // X-Content-Type-Options - MIME sniffing saldırılarını engeller
            context.Response.Headers.Append("X-Content-Type-Options", "nosniff");

            // Referrer Policy - Referrer bilgisinin kontrollü paylaşımı
            context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

            // Permissions Policy - Tarayıcı özelliklerinin kontrollü kullanımı
            context.Response.Headers.Append("Permissions-Policy",
                "camera=(), microphone=(), geolocation=(self), payment=(), usb=(), " +
                "accelerometer=(), gyroscope=(), magnetometer=(), fullscreen=(self)");

            // X-Permitted-Cross-Domain-Policies - Adobe Flash ve PDF cross-domain saldırılarını engeller
            context.Response.Headers.Append("X-Permitted-Cross-Domain-Policies", "none");

            // X-XSS-Protection - Eski tarayıcılarda XSS koruması (modern tarayıcılarda CSP kullanılır)
            context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");

            await _next(context);
        }
    }
}

