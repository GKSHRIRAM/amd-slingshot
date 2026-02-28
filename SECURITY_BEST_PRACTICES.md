# Security Best Practices

## Overview
This document outlines security best practices for the IoT Circuit Builder project to maintain a secure, production-ready codebase.

---

## 1. Credentials & Secrets Management

### ✅ DO:
- **Store API keys in `.env` file** (never in source code)
  ```bash
  # .env (local only)
  GEMINI_API_KEY=sk-123abc...
  PERPLEXITY_API_KEY=sk-456def...
  ```

- **Use `appsettings.json` with placeholder values**
  ```json
  {
    "LLM": {
      "GeminiApiKey": "${GEMINI_API_KEY}",
      "PerplexityApiKey": "${PERPLEXITY_API_KEY}"
    }
  }
  ```

- **Use `IConfiguration` to load from environment variables**
  ```csharp
  var geminiKey = configuration["LLM:GeminiApiKey"];
  var perplexityKey = Environment.GetEnvironmentVariable("PERPLEXITY_API_KEY");
  ```

- **Add `.env` to `.gitignore` permanently**
  ```
  .env
  .env.local
  .env.*.local
  ```

- **Use `.env.example` to show required variables**
  ```
  Copy .env.example to .env, then fill in your keys
  ```

- **Rotate API keys regularly** (every 90 days for production)

### ❌ DON'T:
- ❌ Commit API keys to Git (even in private repos — use git history scanning)
- ❌ Log API keys or tokens (password, bearer tokens, etc.)
- ❌ Pass secrets in URL query parameters
- ❌ Store secrets in code comments
- ❌ Use same API key for dev/staging/production

---

## 2. Input Validation & Sanitization

### ✅ DO:
```csharp
// Enforce length limits
const int MAX_PROMPT_LENGTH = 5000;
const int MIN_PROMPT_LENGTH = 5;

if (request.Prompt.Length > MAX_PROMPT_LENGTH)
    return BadRequest($"Prompt cannot exceed {MAX_PROMPT_LENGTH} characters");

// Sanitize user input to prevent injection
var sanitized = System.Net.WebUtility.HtmlEncode(request.Prompt.Trim());

// Validate data types
if (!int.TryParse(componentCount, out _))
    return BadRequest("Invalid component count");

// Whitelist validation for enums
if (!Enum.IsDefined(typeof(ComponentType), request.Type))
    return BadRequest("Invalid component type");
```

### ❌ DON'T:
- ❌ Trust user input — always validate
- ❌ Use string concatenation for SQL (always use parameterized queries)
- ❌ Accept arbitrary file uploads without validation
- ❌ Skip validation on "internal" APIs

---

## 3. Logging & Monitoring

### ✅ DO:
```csharp
// Log non-sensitive information
_logger.LogInformation("Circuit generation requested (prompt length: {Length})", prompt.Length);
_logger.LogWarning("API call failed with status {StatusCode}", response.StatusCode);

// Log security events
_logger.LogWarning("Invalid API key attempted from {IpAddress}", Request.HttpContext.Connection.RemoteIpAddress);

// Use structured logging
_logger.LogError("Database connection failed. Host: {Host}, Error: {Error}", dbHost, ex.Message);
```

### ❌ DON'T:
- ❌ Log API keys, tokens, or passwords
- ❌ Log user PII (emails, phone numbers, API keys)
- ❌ Log full request/response bodies with sensitive data
- ❌ Use `Console.WriteLine()` for sensitive information
- ❌ Leave debug logs in production

---

## 4. API Security

### ✅ DO:

**Use HTTP Headers for sensitive data:**
```csharp
request.Headers.Add("Authorization", $"Bearer {_apiKey}");
request.Headers.Add("x-goog-api-key", _geminiApiKey);
```

**Restrict CORS to known origins:**
```csharp
services.AddCors(options => {
    options.AddPolicy("AllowFrontend", policy => {
        policy.WithOrigins("http://localhost:3000", "http://192.168.0.104:3000")
              .AllowAnyHeader()
              .WithMethods("GET", "POST", "OPTIONS")
              .AllowCredentials();
    });
});
```

**Add security headers:**
```csharp
context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
context.Response.Headers.Add("X-Frame-Options", "DENY");
context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
```

**Validate API keys at runtime:**
```csharp
[Authorize(AuthenticationSchemes = "ApiKey")]
[HttpPost("generate")]
public async Task<IActionResult> Generate([FromBody] CircuitRequest request) { }
```

### ❌ DON'T:
- ❌ Pass API keys in URL query parameters
- ❌ Use `AllowAnyOrigin()` for CORS (too permissive)
- ❌ Use `AllowAnyMethod()` without specific methods
- ❌ Expose stack traces in error responses (use generic error messages)
- ❌ Skip HTTPS in production

---

## 5. Database Security

### ✅ DO:
```csharp
// Use parameterized queries (Entity Framework handles this)
var circuit = await _context.Circuits
    .FromSql($"SELECT * FROM circuits WHERE id = {circuitId}")
    .FirstOrDefaultAsync();

// Avoid raw SQL concatenation
// GOOD:
var query = $"SELECT * FROM circuits WHERE id = @id";
// BAD (SQL injection risk):
var query = $"SELECT * FROM circuits WHERE id = {id}";
```

### ❌ DON'T:
- ❌ Build SQL queries with string concatenation
- ❌ Store passwords in plaintext (use bcrypt/PBKDF2)
- ❌ Expose database errors in API responses

---

## 6. Frontend Security (Next.js/React)

### ✅ DO:
```tsx
// Use Content Security Policy
<meta 
  httpEquiv="Content-Security-Policy"
  content="default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'"
/>

// Sanitize user input before rendering
import DOMPurify from 'dompurify';
const safe = DOMPurify.sanitize(userInput);

// Never trust user data in URLs
const href = `/circuit/${encodeURIComponent(circuitId)}`;

// Use environment variables for API endpoints
const API_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5050';
```

### ❌ DON'T:
- ❌ Use `dangerouslySetInnerHTML` with user input
- ❌ Store sensitive data in localStorage (use only sessionStorage or secure http-only cookies)
- ❌ Expose API keys in client-side code
- ❌ Trust error messages from APIs

---

## 7. Dependency Management

### ✅ DO:
```bash
# Check for vulnerabilities in NuGet packages
dotnet list package --vulnerable

# Check for vulnerabilities in npm packages
npm audit

# Update dependencies regularly
dotnet add package PackageName --version latest
npm update

# Review changelog before major version upgrades
```

### ❌ DON'T:
- ❌ Use packages with known security vulnerabilities
- ❌ Ignore deprecation warnings
- ❌ Use outdated .NET versions (support ended features)

---

## 8. Deployment Security

### Prerequisites Before Production:
- [ ] All API keys stored in environment variables, NOT in code
- [ ] `.env` file excluded from Git (check `.gitignore`)
- [ ] HTTPS enabled (SSL/TLS certificate installed)
- [ ] CORS restricted to specific frontend origins
- [ ] Rate limiting configured (100 req/min per IP)
- [ ] API key authentication enabled
- [ ] Swagger/API documentation disabled in production
- [ ] Logs encrypted and monitored
- [ ] Database backups encrypted and tested
- [ ] Secrets rotation schedule established

### Production Deployment Checklist:
```bash
# 1. Build in Release mode
dotnet build -c Release

# 2. Run security tests
dotnet test --filter "Security"

# 3. Scan dependencies
dotnet list package --vulnerable

# 4. Enable HTTPS
dotnet publish -c Release -o ./publish

# 5. Set environment variables (NOT in code!)
export GEMINI_API_KEY=sk-...
export PERPLEXITY_API_KEY=sk-...

# 6. Run behind reverse proxy (nginx/Apache)
# 6. Enable WAF (Web Application Firewall)
# 6. Monitor logs with SIEM
```

---

## 9. Incident Response

### If API Key is Compromised:
1. **Immediately revoke the key** in the AI provider's dashboard
2. **Generate a new key**
3. **Update environment variables** on all servers
4. **Check logs** for unauthorized access
5. **Rotate all related credentials**
6. **Post-mortem**: Document how the key leaked and update processes

### If Database is Breached:
1. **Take database offline** (if safe)
2. **Notify users** as required by GDPR/local regulations
3. **Restore from encrypted backup**
4. **Audit access logs** to determine scope
5. **Implement additional monitoring**

---

## 10. Security Audit Schedule

| Task | Frequency | Owner |
|------|-----------|-------|
| Dependency vulnerability scan | Weekly | CI/CD |
| API security review | Monthly | Security Team |
| Database backup verification | Weekly | DevOps |
| Access logs review | Daily | Security Team |
| API key rotation | 90 days | Operations |
| Full security audit | Quarterly | External Auditor |
| User security training | Annually | HR/Security |

---

## 11. Security Resources

- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [OWASP API Security Top 10](https://owasp.org/www-project-api-security/)
- [Microsoft: Secure coding practices](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/overview)
- [ASP.NET Core Security](https://learn.microsoft.com/en-us/aspnet/core/security/)
- [Next.js Security Best Practices](https://nextjs.org/docs/pages/building-your-application/configuring/environment-variables)
- [npm audit](https://docs.npmjs.com/cli/audit)

---

## Questions?

For security questions, **DO NOT** ask in public channels. Email security@company.com or create a private GitHub Security Advisory.

**Never commit secrets.** When in doubt, ask first.

Last Updated: 2024
