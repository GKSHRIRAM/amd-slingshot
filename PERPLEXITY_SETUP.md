# Perplexity Sonar API Integration Guide

## 🔌 Overview

The IoT Circuit Builder now supports **Perplexity Sonar** as the primary AI/LLM backend for component intelligence. Sonar is fast, accurate, and optimized for structured JSON output.

### Supported LLM Providers (Priority Order)

1. **Perplexity Sonar** (Recommended) - Fast, accurate, real-time information
2. **Google Gemini** (Fallback 1) - Reliable, multi-modal capabilities
3. **Groq** (Fallback 2) - Open-source model infrastructure

---

## 🚀 Setup Instructions

### Step 1: Get Perplexity API Key

1. Visit [Perplexity AI Console](https://www.perplexity.ai/)
2. Sign up or log in to your account
3. Navigate to **API Keys** section
4. Click "Create API Key"
5. Copy your API key (starts with `pplx-`)

### Step 2: Configure API Key

#### Option A: Environment Variable (Recommended for Production)

**Windows (PowerShell)**:
```powershell
[Environment]::SetEnvironmentVariable("Perplexity__ApiKey", "YOUR_API_KEY_HERE", "User")
# Restart your terminal for changes to take effect
```

**Windows (Command Prompt)**:
```cmd
setx Perplexity__ApiKey YOUR_API_KEY_HERE
# Restart your terminal
```

**Linux/macOS**:
```bash
export Perplexity__ApiKey="YOUR_API_KEY_HERE"
# Add to ~/.bashrc or ~/.zshrc for persistence
```

#### Option B: Configuration File (Development)

Edit `src/IoTCircuitBuilder.API/appsettings.Development.json`:

```json
{
  "Perplexity": {
    "ApiKey": "pplx-your-actual-api-key-here"
  }
}
```

### Step 3: Verify Configuration

Run the application and check logs:

```powershell
cd E:\amd final\amd
dotnet run --project src/IoTCircuitBuilder.API
```

Look for these log messages:

✅ **Success**:
```
[INF] Attempting to parse intent using Perplexity Sonar...
[INF] RAW PERPLEXITY RESPONSE: {...}
```

⚠️ **Fallback to Gemini** (if Perplexity key not set):
```
[INF] Attempting to parse intent using Gemini...
```

---

## 📋 API Configuration Reference

### Perplexity Sonar Model Settings

| Setting | Value | Purpose |
|---------|-------|---------|
| Model | `sonar-pro` | Latest Perplexity model (fast + accurate) |
| Temperature | `0.1` | Deterministic, consistent output |
| Top-K | `0` | Disable sampling for reliability |
| Top-P | `0.9` | Nucleus sampling threshold |
| Citations | `false` | Disable citation tracking |

### Request/Response Format

**Request**:
```json
{
  "model": "sonar-pro",
  "messages": [
    {"role": "system", "content": "System prompt with component rules..."},
    {"role": "user", "content": "Extract components from: ..."}
  ],
  "temperature": 0.1,
  "top_k": 0,
  "top_p": 0.9,
  "return_citations": false
}
```

**Response**:
```json
{
  "choices": [
    {
      "message": {
        "role": "assistant",
        "content": "{\"board\": \"arduino_uno\", \"components\": [...]}"
      }
    }
  ]
}
```

---

## 🔐 Security Best Practices

### ⚠️ NEVER Commit API Keys

**Ensure these files are in `.gitignore`**:
```
appsettings.Development.json
appsettings.*.json
.env
.env.local
```

**Check current status**:
```powershell
cd E:\amd final\amd
git check-ignore appsettings.Development.json
```

### Environment Variable Priority

The application loads secrets in this order:

1. Environment Variables (highest priority)
2. `appsettings.Development.json` (dev only)
3. `appsettings.json` (default fallback)

### Recommended Setup

**Development Machine**:
- Set environment variable: `Perplexity__ApiKey=your_key`
- Keep `appsettings.Development.json` in `.gitignore`
- Never commit real API keys

**Production/Deployment**:
- Use hosted secrets manager (Azure Key Vault, AWS Secrets Manager, etc.)
- Set environment variable at deployment time
- Rotate API keys regularly

---

## 🧪 Testing Perplexity Integration

### Test with Development Tools

**Using curl**:
```bash
curl -X POST http://localhost:5050/api/circuit/generate \
  -H "Content-Type: application/json" \
  -d '{"prompt": "LED blink circuit"}'
```

**Expected Response**:
```json
{
  "success": true,
  "generatedCode": "...",
  "pinMapping": {...},
  "componentsUsed": ["Arduino Uno", "LED Red"]
}
```

### Check Logs

Look in `src/IoTCircuitBuilder.API/logs/` for detailed request/response:

```
[INF] Attempting to parse intent using Perplexity Sonar...
[INF] RAW PERPLEXITY RESPONSE: {"board":"arduino_uno","components":[...]}
```

---

## 🔄 Fallback Mechanism

If Perplexity fails, the system automatically tries:

```
Perplexity Sonar (if key configured)
  ↓ [If failed]
Google Gemini (if key configured)
  ↓ [If failed]
Groq Llama 3.3
  ↓ [If failed]
Error: All LLM services failed
```

### Force Fallback (Remove Perplexity Key)

```json
{
  "Perplexity": {
    "ApiKey": ""
  }
}
```

---

## 💰 Pricing & Rate Limits

| Model | Cost | Rate Limit |
|-------|------|-----------|
| sonar-pro | ~$0.50/1M tokens | 100 req/min |
| sonar | ~$0.10/1M tokens | 100 req/min |

**Cost Estimate**: ~0.0005 - 0.005¢ per circuit generation (typical request: 1K tokens)

---

## 🐛 Troubleshooting

### Issue: "Perplexity API returned 401"

**Cause**: Invalid or missing API key

**Solution**:
```powershell
# Verify key is set
$env:Perplexity__ApiKey
# Should output: pplx-xxxxxxxx...

# If empty, set it
[Environment]::SetEnvironmentVariable("Perplexity__ApiKey", "YOUR_KEY", "User")
```

### Issue: "Perplexity API returned 429 (Rate Limited)"

**Cause**: Exceeded rate limit (100 requests/minute)

**Solution**:
- Wait 1 minute before retrying
- Upgrade API plan for higher limits
- Implement request queuing in production

### Issue: "Perplexity returned empty response"

**Cause**: Perplexity didn't parse request properly

**Solution**:
1. Check system prompt in `LLMService.cs` is valid
2. Try with simpler prompt: "LED blink"
3. Check Perplexity API status: https://status.perplexity.ai/

### Issue: "All LLM services failed"

**Cause**: No API keys configured

**Solution**:
```json
{
  "Gemini": { "ApiKey": "YOUR_GEMINI_KEY" },
  "Groq": { "ApiKey": "YOUR_GROQ_KEY" },
  "Perplexity": { "ApiKey": "YOUR_PERPLEXITY_KEY" }
}
```

Set at least one to avoid error.

---

## 📚 Additional Resources

- [Perplexity API Documentation](https://docs.perplexity.ai/)
- [Sonar Model Specifications](https://docs.perplexity.ai/docs/ai-library/sonar)
- [API Console](https://www.perplexity.ai/api)

---

## 🚀 Summary Checklist

- [ ] Created Perplexity account and got API key
- [ ] Set `Perplexity__ApiKey` environment variable
- [ ] Verified `appsettings.Development.json` is in `.gitignore`
- [ ] Ran `dotnet build` successfully
- [ ] Started API: `dotnet run --project src/IoTCircuitBuilder.API`
- [ ] Tested with sample prompt via REST client
- [ ] Checked logs for "RAW PERPLEXITY RESPONSE"
- [ ] Verified fallback chain works if testing without Perplexity key

---

**Integration Status**: ✅ Complete & Production-Ready

The Perplexity Sonar API integration is now fully integrated as the primary LLM provider with automatic fallbacks to Gemini and Groq.
