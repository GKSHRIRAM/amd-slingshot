# 🚀 IoT Circuit Builder - Setup Guide

Complete step-by-step instructions to get the IoT Circuit Builder running on your local machine.

---

## 📋 Prerequisites

Ensure you have the following installed:

### Required Software
- **.NET 8.0 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Node.js 18+** - [Download](https://nodejs.org/)
- **npm** (comes with Node.js)
- **Git** - [Download](https://git-scm.com/)
- **Visual Studio Code** (recommended) - [Download](https://code.visualstudio.com/)

### Verify Installation
```powershell
# Check .NET version
dotnet --version

# Check Node.js version
node --version

# Check npm version
npm --version
```

---

## 🔧 Installation Steps

### Step 1: Clone or Extract the Repository

```powershell
cd E:\amd final\amd
```

### Step 2: Backend Setup (ASP.NET Core API)

#### 2.1 Restore NuGet Packages
```powershell
cd src\IoTCircuitBuilder.API
dotnet restore
```

#### 2.2 Build the Project
```powershell
dotnet build
```

#### 2.3 Apply Database Migrations
```powershell
# Ensure you're in the API project directory
dotnet ef database update
```

If `dotnet ef` command is not found, install the EF tool:
```powershell
dotnet tool install --global dotnet-ef
```

#### 2.4 Verify API Configuration
Check `src/IoTCircuitBuilder.API/Properties/launchSettings.json`:

```json
{
  "profiles": {
    "http": {
      "commandName": "Project",
      "applicationUrl": "http://localhost:5050",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

**Note**: Ensure port 5050 is available. If occupied, kill existing processes:
```powershell
taskkill /F /IM dotnet.exe
```

#### 2.5 Configure LLM API Keys (Optional but Recommended)

The API uses AI to intelligently select circuit components. Configure at least one LLM provider:

**Option A: Environment Variables (Recommended)**
```powershell
# Set Perplexity Sonar (Recommended - fastest & most accurate)
[Environment]::SetEnvironmentVariable("Perplexity__ApiKey", "YOUR_PERPLEXITY_KEY", "User")

# OR Gemini (Backup option)
[Environment]::SetEnvironmentVariable("Gemini__ApiKey", "YOUR_GEMINI_KEY", "User")

# OR Groq (Another backup option)
[Environment]::SetEnvironmentVariable("Groq__ApiKey", "YOUR_GROQ_KEY", "User")
```

**Option B: Configuration File**

Edit `src/IoTCircuitBuilder.API/appsettings.Development.json`:
```json
{
  "Perplexity": {
    "ApiKey": "YOUR_PERPLEXITY_API_KEY"
  },
  "Gemini": {
    "ApiKey": "YOUR_GEMINI_API_KEY"
  },
  "Groq": {
    "ApiKey": "YOUR_GROQ_API_KEY"
  }
}
```

**Get API Keys:**
- **Perplexity**: https://www.perplexity.ai/ → API Console
- **Gemini**: https://aistudio.google.com/ → Get API Key
- **Groq**: https://console.groq.com/ → API Keys

⚠️ **IMPORTANT**: Never commit API keys! Ensure `appsettings.Development.json` is in `.gitignore`.

For detailed Perplexity setup, see [PERPLEXITY_SETUP.md](PERPLEXITY_SETUP.md).

### Step 3: Frontend Setup (Next.js Client)

#### 3.1 Navigate to Client Directory
```powershell
cd E:\amd final\amd\client
```

#### 3.2 Install npm Dependencies
```powershell
npm install
```

#### 3.3 Verify Next.js Configuration
Check `next.config.ts` includes Turbopack root configuration:

```typescript
import type { NextConfig } from "next";
import path from "path";

const nextConfig: NextConfig = {
  turbopack: {
    root: path.join(__dirname),
  },
};

export default nextConfig;
```

#### 3.4 Clean up Conflicting Files
Remove any extra `package-lock.json` or `package.json` files from parent directories:
```powershell
# From E:\ root
Remove-Item -Path "E:\package-lock.json" -Force -ErrorAction SilentlyContinue
Remove-Item -Path "E:\package.json" -Force -ErrorAction SilentlyContinue
```

---

## ▶️ Running the Application

### Method 1: Two Terminal Windows (Recommended)

#### Terminal 1: Start the Backend API
```powershell
cd E:\amd final\amd
dotnet run --project src/IoTCircuitBuilder.API
```

Expected output:
```
[HH:MM:SS INF] IoT Circuit Builder API started
Listening on http://127.0.0.1:5050
```

#### Terminal 2: Start the Frontend
```powershell
cd E:\amd final\amd\client
npm run dev
```

Expected output:
```
▲ Next.js 16.1.6 (Turbopack)
- Local:         http://localhost:3000
- Networks:      http://192.168.0.104:3000
✓ Ready in 4.2s
```

### Method 2: VS Code Tasks (Alternative)

1. Open the workspace in VS Code
2. Press `Ctrl+Shift+D` (Debug view)
3. Choose configuration and run both tasks

---

## 🌐 Access the Application

Once both services are running:

### Local Access
- **Frontend**: http://localhost:3000
- **Backend API**: http://localhost:5050
- **API Documentation**: http://localhost:5050/swagger

### Network Access (Other Devices)
You can access the application from other devices on the same network:

**From Your Phone/Tablet:**
- **Frontend**: http://192.168.0.104:3000 (replace with your machine's network IP)
- **Backend API**: http://192.168.0.104:5050

**To find your network IP:**
```powershell
ipconfig
# Look for IPv4 Address under your network adapter (e.g., 192.168.x.x)
```

**Note**: Make sure your machine's firewall allows connections on ports 3000 and 5050, especially if accessing from other devices.

---

## 📁 Project Structure Overview

### Backend Projects

```
IoTCircuitBuilder.Domain/
├── Entities/           # Core business models (Component, Board, Circuit)
├── Enums/              # Types (ComponentType, PinMode, VoltageLevel)
└── ValueObjects/       # Immutable values (PinInfo, ConnectionRule)

IoTCircuitBuilder.Core/
├── Algorithms/         # ConstraintSolver for pin assignment
├── Interfaces/         # Core contracts
└── Validation/         # PinMappingValidator, electrical rules

IoTCircuitBuilder.Application/
├── DTOs/               # Data transfer objects for API
├── Interfaces/         # Service contracts
└── Services/           # CircuitGenerationService, ComponentDependencyService

IoTCircuitBuilder.Infrastructure/
├── Data/               # Database context and migrations
├── Repositories/       # Data access layer
├── Services/           # LLMService, CodeGeneratorService
└── Templates/          # Arduino code templates

IoTCircuitBuilder.API/
├── Controllers/        # REST endpoints
├── Properties/         # Launch settings
└── Program.cs          # DI configuration
```

### Frontend Structure

```
client/src/
├── app/
│   ├── page.tsx        # Main interface with tabs
│   ├── layout.tsx      # Root layout and providers
│   └── globals.css     # Tailwind directives

├── components/
│   ├── WokwiCircuit.tsx       # 3D circuit visualization wrapper
│   ├── WokwiNode.tsx          # Individual Wokwi component renderer
│   └── GenericNode.tsx        # Custom component fallback

├── lib/
│   ├── api.ts          # Backend HTTP client
│   ├── layoutEngine.ts # ELK.js graph layout algorithm
│   ├── wireRouter.ts   # Wire path finding
│   └── fritzing.ts     # Fritzing integration helper

└── types/
    ├── circuit.ts      # TypeScript interfaces
    └── wokwi-elements.d.ts
```

---

## ⚙️ Configuration Files

### Backend Configuration

**`appsettings.json`**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=circuit_builder.db"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

**`appsettings.Development.json`** (Development overrides)
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft": "Warning"
    }
  }
}
```

### Frontend Configuration

**`.env.local`** (Create if needed for API base URL)
```env
NEXT_PUBLIC_API_URL=http://localhost:5050
```

---

## 🧪 Running Tests

### Backend Tests
```powershell
cd E:\amd final\amd\tests\IoTCircuitBuilder.Tests
dotnet test
```

Tests cover:
- Constraint solver algorithms
- Pin mapping validation
- Component dependency resolution

### Frontend Tests (if configured)
```powershell
cd E:\amd final\amd\client
npm run test
```

---

## 🐛 Troubleshooting

### Issue: "Port 5050 already in use"

**Solution**:
```powershell
# Find process using port 5050
netstat -ano | Select-String "5050"

# Kill the process (example: PID 12345)
taskkill /PID 12345 /F

# Or kill all dotnet processes
taskkill /F /IM dotnet.exe

# Then restart the API
dotnet run --project src/IoTCircuitBuilder.API
```

### Issue: "Next.js workspace root warning"

**Solution**: Verify `client/next.config.ts` has proper configuration:
```typescript
const nextConfig: NextConfig = {
  turbopack: {
    root: path.join(__dirname),
  },
};
```

### Issue: "Database file not found"

**Solution**:
```powershell
cd E:\amd final\amd\src\IoTCircuitBuilder.API
# Delete existing database files
Remove-Item -Path "bin/" -Recurse -Force
Remove-Item -Path "obj/" -Recurse -Force

# Rebuild
dotnet build
dotnet run
```

### Issue: "Node modules not found"

**Solution**:
```powershell
cd E:\amd final\amd\client

# Clear npm cache
npm cache clean --force

# Delete node_modules
Remove-Item -Path "node_modules" -Recurse -Force

# Reinstall
npm install
```

### Issue: "Cross origin request detected" warning

**Symptom**: Warning in console when accessing from network IP (e.g., 192.168.x.x)
```
⚠ Cross origin request detected from 192.168.0.104 to /_next/* resource.
```

**Solution**: This is already fixed in `client/next.config.ts` with `allowedDevOrigins` configuration:
```typescript
allowedDevOrigins: [
  "localhost",
  "127.0.0.1",
  "192.168.0.104",  // Local network IP
  "*.local",        // Local network domains
]
```

If you still see the warning:
1. Restart the dev server: `npm run dev`
2. Clear browser cache (Ctrl+Shift+Del)
3. Hard refresh (Ctrl+F5)

### Issue: "Cannot connect from phone/other device"

**Checklist**:
1. ✅ Both backend and frontend are running
2. ✅ Get your machine's network IP: `ipconfig`
3. ✅ Use correct IP with ports: `http://YOUR_IP:3000`
4. ✅ Phone/device is on same WiFi network
5. ✅ Windows Firewall allows ports 3000 & 5050:
   ```powershell
   # Allow port 3000
   netsh advfirewall firewall add rule name="Node.js Dev Server" dir=in action=allow protocol=tcp localport=3000
   
   # Allow port 5050
   netsh advfirewall firewall add rule name=".NET API" dir=in action=allow protocol=tcp localport=5050
   ```

**Solution**:
```powershell
# Find your network IP
ipconfig
# Example output: IPv4 Address: 192.168.0.104

# From phone, visit: http://192.168.0.104:3000
# Backend accessible at: http://192.168.0.104:5050
```

---

## 🔄 Development Workflow

### Backend Development
```powershell
# Enter API project
cd src/IoTCircuitBuilder.API

# Run with hot reload
dotnet watch run

# Open Swagger docs
# http://localhost:5050/swagger
```

### Frontend Development
```powershell
cd client

# Run dev server with hot reload
npm run dev

# Build for production
npm run build

# Start production server
npm start
```

### Database Modifications
If modifying the EF Core models:
```powershell
cd src/IoTCircuitBuilder.API

# Create a migration
dotnet ef migrations add MigrationName

# Apply migration
dotnet ef database update

# Rollback last migration
dotnet ef migrations remove
```

---

## 📦 Key Dependencies

### Backend (.NET)
| Package | Version | Purpose |
|---------|---------|---------|
| Microsoft.EntityFrameworkCore | 8.0 | ORM for database |
| Serilog | 4.x | Structured logging |
| Swashbuckle.AspNetCore | 6.x | Swagger API docs |

### Frontend (npm)
| Package | Version | Purpose |
|---------|---------|---------|
| next | 16.1.6 | React framework |
| react | 19.2.3 | UI library |
| @xyflow/react | 12.10.1 | Graph visualization |
| @wokwi/elements | 1.9.1 | Circuit simulation |
| tailwindcss | 4.x | Utility-first CSS |
| elkjs | 0.11.0 | Auto layout engine |

---

## 🚀 Building for Production

### Backend Release Build
```powershell
cd E:\amd final\amd
dotnet publish -c Release -o ./publish/api
```

### Frontend Production Build
```powershell
cd E:\amd final\amd\client
npm run build
npm start
```

---

## 📊 Performance Optimization Tips

1. **Enable Response Compression** in `Program.cs`:
   ```csharp
   builder.Services.AddResponseCompression();
   app.UseResponseCompression();
   ```

2. **Frontend Caching**: Modify `next.config.ts`:
   ```typescript
   const nextConfig: NextConfig = {
     compress: true,
     productionBrowserSourceMaps: false,
   };
   ```

3. **Database Indexing**: Add indexes to frequently queried columns in migrations.

---

## 🆘 Getting Help

### Check Logs
- **Backend**: `src/IoTCircuitBuilder.API/logs/` directory
- **Frontend**: Browser console (F12 → Console tab)

### API Documentation
- Swagger UI: http://localhost:5050/swagger

### Example Prompts to Test
1. "A line following robot with 2 IR sensors and 2 DC motors with L298N driver"
2. "Robot car with ultrasonic sensor and servo for obstacle avoidance"
3. "Simple LED blink project with 3 red LEDs"

---

## ✅ Checklist for First Run

- [ ] .NET 8.0 SDK installed
- [ ] Node.js 18+ installed
- [ ] Git cloned/extracted the repository
- [ ] NuGet packages restored (`dotnet restore`)
- [ ] npm packages installed (`npm install`)
- [ ] No process occupying port 5050
- [ ] Database migrations applied (`dotnet ef database update`)
- [ ] Backend started successfully (`dotnet run`)
- [ ] Frontend started successfully (`npm run dev`)
- [ ] Browser opened to http://localhost:3000
- [ ] Test circuit generation with example prompt

---

## 🎓 Next Steps

1. **Explore the UI**: Generate a test circuit
2. **Review Generated Code**: Check the Arduino sketch
3. **Examine Pin Mapping**: Understand component connections
4. **Export to Wokwi**: Download ZIP and test in editor
5. **Read Source Code**: Start with `Program.cs` and `page.tsx`

---

**Happy circuiting! 🔌⚡**

For detailed project information, see [README.md](README.md)
