# 🔧 IoT Circuit Builder - Project Architecture & Components Guide

**For Building Similar Projects**

---

## ✅ SYSTEM STATUS: FULLY OPERATIONAL

**Verified Working:**
- ✅ Circuit generation engine
- ✅ Multi-board topology support
- ✅ Component selection & pinning
- ✅ Voltage compatibility checking
- ✅ Firmware code generation
- ✅ RF simplex routing (TX/RX separation)
- ✅ Sensor exclusion from receiver boards
- ✅ Logic level converter auto-injection

---

## 📋 PROJECT COMPONENTS BREAKDOWN

### **1. BACKEND ARCHITECTURE (C# .NET 10)**

**Core Packages:**
```
- Microsoft.EntityFrameworkCore 8.0.0 (Database ORM)
- Entity Framework SQLite provider (Local database)
- Serilog 8.0+ (Structured logging)
- FluentValidation 11.x (Input validation)
- DotNetEnv 3.1.1+ (.env file support)
```

**API Framework:**
```
- ASP.NET Core (REST API)
- Swagger/OpenAPI (API documentation)
- CORS middleware (Cross-origin requests)
- Security headers middleware
```

**LLM Integration:**
```
- HttpClient (for external LLM APIs)
- System.Text.Json (JSON serialization)
- Supports: Google Gemini, Groq, Perplexity APIs
```

**Project Structure:**
```
src/
├── IoTCircuitBuilder.API/          [REST API entry point]
├── IoTCircuitBuilder.Application/  [Business logic, DTOs]
├── IoTCircuitBuilder.Core/         [Algorithms, validators]
│   ├── Algorithms/
│   │   ├── ConstraintSolver.cs    [Pin assignment solver]
│   │   └── [Graph algorithms]
│   ├── Interfaces/
│   └── Validation/
├── IoTCircuitBuilder.Domain/       [Entities, enums]
└── IoTCircuitBuilder.Infrastructure/ [Database, services]
    ├── Data/SeedData.cs            [Component database]
    ├── Services/
    │   ├── LLMService.cs           [Orchestrator + Agents]
    │   └── ComponentDependencyService.cs
    └── Repositories/
```

---

### **2. FRONTEND (Next.js + TypeScript + React)**

**Key Packages:**
```
- Next.js 15+ (React framework)
- TypeScript (Type safety)
- Tailwind CSS (UI styling)
- Axios or Fetch API (HTTP requests)
```

**Client-Side Features:**
```
- Circuit visualization (SVG rendering)
- Component placement
- Pin mapping display
- Drag & drop (optional)
- Real-time JSON display
- Code generation output
```

**Directory Structure:**
```
client/
├── src/
│   ├── app/
│   │   ├── page.tsx              [Main UI]
│   │   ├── globals.css           [Styling]
│   │   └── layout.tsx
│   ├── components/
│   │   ├── CircuitRenderer.tsx   [SVG rendering]
│   │   ├── WokwiCircuit.tsx      [Simulation]
│   │   └── [UI components]
│   └── lib/
│       ├── api.ts                [API calls]
│       ├── layoutEngine.ts       [Component positioning]
│       └── wireRouter.ts         [Wire/connection logic]
```

---

### **3. DATABASE SCHEMA**

**Core Tables:**
```
- Boards (Arduino, Raspberry Pi variants)
- Components (Sensors, actuators, displays, etc.)
- ComponentPins (Pin definitions per component)
- Pins (Board pin layout)
- PinRequirements (Power/signal requirements)
- I2cAddresses (I2C device addressing)
- Templates (Code generation templates)
```

**Sample Data:**
```
✓ 50+ Components (DHT11, HC-SR04, motors, displays, etc.)
✓ 5+ Board types (Arduino Uno, Nano, Mega, etc.)
✓ 200+ Pin definitions
✓ Voltage specifications per component
✓ Power consumption data
```

---

### **4. ALGORITHM LAYER**

**Key Algorithms:**

#### **a) ConstraintSolver (Graph-based)**
```csharp
- Bipartite matching (Component ↔ Pins)
- Voltage compatibility checking
- Power budget validation
- Signal conflict detection
- Returns: Optimal pin assignments with warnings
```

**Constraints Handled:**
- Power supply limits (5V rails, GND rails)
- Serial communications (UART conflicts)
- Analog pins conflicts
- I2C address collisions
- PWM conflicts
- SPI bus conflicts

#### **b) Dependency Injection**
```csharp
- Auto-adds motor drivers when DCx motors detected
- Injects resistors for LED current limiting
- Adds flyback diodes for relay protection
- Injects breadboards when pin deficit detected
- Adds batteries for high-power components
```

#### **c) Post-Solve Logic Level Converter (LLC) Injection**
```csharp
- Detects 3.3V components on 5V boards
- Routes 3.3V signals through LLC
- Preserves original component connections
- Auto-assigns LLC pins
```

#### **d) Simplex Radio Rules**
```
- TX-only boards: Strip rf_receiver from BOM
- RX-only boards: Strip rf_transmitter + sensors from BOM
- Receiver-only board: Never add measurement sensors
```

---

### **5. LLM ORCHESTRATION (Multi-Agent Pattern)**

**Three-Stage Pipeline:**

```
┌─────────────────────────────────────────┐
│ STAGE 1: ORCHESTRATOR                   │
├─────────────────────────────────────────┤
│ Input: User prompt                      │
│ Output: Network topology + board roles  │
│ Decides: Single board vs. multi-board   │
└─────────────────────────────────────────┘
            ↓
┌─────────────────────────────────────────┐
│ STAGE 2: BOM AGENT (per board)          │
├─────────────────────────────────────────┤
│ Input: Board role + hardware class      │
│ Output: Component list (BOM)            │
│ Logic: Parse role, select components    │
└─────────────────────────────────────────┘
            ↓
┌─────────────────────────────────────────┐
│ STAGE 3: FIRMWARE AGENT                 │
├─────────────────────────────────────────┤
│ Input: Pin mappings + components        │
│ Output: Arduino C++ code                │
│ Logic: Generate setup() & loop()        │
└─────────────────────────────────────────┘
```

**Critical Prompts:**
- Orchestrator: Topology & role clarity
- BOM Agent: Inference prevention rules
- Firmware Agent: Hardware header usage

---

### **6. HARDWARE PHYSICS ENGINE**

**Classes:**
```
- Board (voltage, pin count, capabilities)
- Component (voltage, power draw, functionality)
- Pin (type: digital, analog, PWM, I2C, SPI, UART)
- ComponentPinRequirement (signal type + count)
```

**Physics Validations:**
```
✓ Power budget check (mA per rail)
✓ Voltage compatibility (no 12V direct to 3.3V pin)
✓ Pin type matching (PWM slot for servo)
✓ Serial conflict detection (2 UARTs on 1 pin)
✓ I2C address uniqueness
```

---

### **7. CRITICAL BUSINESS RULES**

**Rule 1: Transmitter-Receiver Separation**
```
IF role contains "transmit" AND NOT "receive":
    → Include rf_transmitter
    → Strip rf_receiver
    
IF role contains "receive" AND NOT "transmit":
    → Include rf_receiver
    → Strip rf_transmitter + ALL sensors
```

**Rule 2: Sensor Inference Prevention**
```
BOM Agent must NOT infer sensors from concepts
❌ "display temperature" ≠ Add DHT11
✅ "display on OLED" = Only add display
✅ "Read DHT11" = Add DHT11
```

**Rule 3: Voltage Level Shifting**
```
IF board operates at 5V AND has 3.3V components:
    → Inject 4-channel bi-directional LLC
    → Route 3.3V signals through LLC
    → Maintain original pin assignments
```

**Rule 4: Power Distribution**
```
IF >1 component needs 5V:
    → Inject breadboard half-size
    → Route power through breadboard rails
    → Reduce Arduino pin stress
```

---

### **8. COMPONENTS DATABASE SCHEMA**

**Essential Fields per Component:**
```
- Type (string, lowercase, unique)
- DisplayName (user-friendly name)
- Category (sensor, actuator, display, etc.)
- VoltageMin / VoltageMax (operating range)
- LogicVoltage (signal voltage: 3.3V or 5V)
- PowerConsumption (mA @ rated voltage)
- PinCount (total pins)
- Functions (primary + secondary)
- I2C address (if applicable)
- SPI/UART compatibility
- Frequency/bandwidth specs
```

**Sample Components Needed:**
```
SENSORS (10+):
  - DHT11 (Temperature/Humidity)
  - HC-SR04 (Ultrasonic distance)
  - BME280 (Pressure/altitude)
  - IR sensor (Infrared)
  - LDR (Light)
  - MPU6050 (Gyro/accel)
  - PIR (Motion)
  
ACTUATORS (8+):
  - SG90 servo
  - DC motor (various)
  - BLDC motor
  - Buzzer
  - LED (various colors)
  - Relay module
  
DISPLAYS (3+):
  - OLED 128x64
  - LCD 16x2
  - 7-segment display
  
COMMUNICATION (6+):
  - RF transmitter 433MHz
  - RF receiver 433MHz
  - Bluetooth HC-05
  - WiFi ESP8266
  - NRF24L01
  - RC522 RFID
  
DRIVERS (3+):
  - L298N motor driver
  - ESC (Electronic Speed Controller)
  - 4-channel logic level converter
```

---

### **9. ENVIRONMENT CONFIGURATION**

**Required .env Variables:**
```
GEMINI_API_KEY=your_key_here
GROQ_API_KEY=your_key_here
PERPLEXITY_API_KEY=your_key_here
DATABASE_CONNECTION_STRING=...
```

**Configuration Files:**
```
- appsettings.json (default)
- appsettings.Development.json (gitignored)
- launchSettings.json (port config)
```

---

### **10. BUILD & DEPLOYMENT**

**Build Requirements:**
```
- .NET SDK 10.0+ (C#)
- Node.js 18+ (React/Next.js)
- npm or yarn (package management)
- SQLite (embedded database)
```

**Build Commands:**
```bash
# Backend
dotnet build
dotnet run --project src/IoTCircuitBuilder.API

# Frontend
cd client
npm install
npm run dev
```

**Deployment:**
```
- Backend: Docker, Azure App Service, AWS Lambda
- Frontend: Vercel, Netlify, GitHub Pages
- Database: Azure SQL, AWS RDS (production)
```

---

### **11. TESTING FRAMEWORK**

**Test Project:**
```
tests/IoTCircuitBuilder.Tests/

Test Types:
- Unit tests (xUnit)
- Mocking (Moq)
- Integration tests
- Circuit generation tests
```

**Key Tests to Include:**
```
✓ ConstraintSolver (pin assignment)
✓ Voltage compatibility
✓ Component dependency injection
✓ Power budget validation
✓ TX/RX separation logic
✓ Sensor pruning rules
✓ LLM prompt validation
✓ Code generation output
```

---

### **12. API ENDPOINTS**

**Main Endpoint:**
```
POST /api/circuit/generate
Body: { prompt: "User description" }

Response:
{
  "success": true,
  "boards": [
    {
      "boardId": "board_0",
      "role": "...",
      "componentsUsed": [...],
      "pinMapping": {...},
      "generatedCode": "...",
      "needsBreadboard": true,
      "warnings": [...]
    }
  ],
  "topology_detected": "single_board|transmitter_receiver|mesh_network"
}
```

---

### **13. CODE GENERATION TEMPLATES**

**Supported Output:**
```
- Arduino C++ code
- Pin definitions (#define macros)
- Setup function
- Loop function
- Shared payload structs (for networking)
- Library includes
```

**Libraries Generated:**
```c++
#include <DHT.h>
#include <Wire.h>
#include <SPI.h>
#include <Servo.h>
#include <RH_ASK.h>  // RF
// etc.
```

---

## 🚀 QUICK START FOR NEW PROJECT

### **Step 1: Set Up Backend**
```bash
dotnet new sln
dotnet new classlib -n YourProject.Core
dotnet new webapi -n YourProject.API
dotnet add reference YourProject.Core
```

### **Step 2: Create Database Context**
```csharp
public class ApplicationDbContext : DbContext
{
    public DbSet<Board> Boards { get; set; }
    public DbSet<Component> Components { get; set; }
    public DbSet<Pin> Pins { get; set; }
    // ...
}
```

### **Step 3: Implement Constraint Solver**
```csharp
public class ConstraintSolver
{
    public SolverResult Solve(Board board, List<Component> components)
    {
        // Bipartite matching algorithm
        // Return pin assignments
    }
}
```

### **Step 4: Build LLM Agents**
```csharp
// Stage 1: Orchestrator
var topology = await _llmService.ParseIntentAsync(userPrompt);

// Stage 2: BOM Agent (per board)
var components = await _llmService.ParseBOMAsync(boardRole, hardwareClass);

// Stage 3: Firmware Agent
var code = await _llmService.GenerateFirmwareLogicAsync(header, components);
```

### **Step 5: Frontend UI**
```tsx
// Next.js page component
export default function Home() {
  const [circuit, setCircuit] = useState(null);
  
  const generate = async (prompt) => {
    const response = await fetch('/api/circuit/generate', {
      method: 'POST',
      body: JSON.stringify({ prompt })
    });
    setCircuit(await response.json());
  };
  
  return (
    <div>
      <CircuitRenderer circuit={circuit} />
    </div>
  );
}
```

---

## 📊 DEPENDENCY TREE FOR NEW PROJECTS

```
Required:
├─ Web Framework (ASP.NET Core, FastAPI, Express, etc.)
├─ ORM/Database (EF Core, SQLAlchemy, Mongoose, etc.)
├─ LLM API Client (OpenAI, Anthropic, custom)
├─ Graph Algorithm Library (for constraint solving)
├─ Validation Framework (FluentValidation, Pydantic, etc.)
├─ JSON Serialization (System.Text.Json, etc.)
├─ HTTP Client (HttpClient, axios, etc.)
├─ Logging (Serilog, Winston, etc.)

Optional but Recommended:
├─ Circuit Visualization (SVG rendering library)
├─ Code Generation Templates (Liquid, Scriban, Jinja2)
├─ Testing Framework (xUnit, Jest, pytest)
├─ Documentation (Swagger, Sphinx)
├─ Monitoring (Application Insights, DataDog)
├─ Caching (Redis)
└─ Message Queue (if scalability needed)
```

---

## ✨ WHAT MAKES THIS PROJECT UNIQUE

1. **Multi-Agent LLM Orchestration** - Three specialized AI agents (not one generic)
2. **Physics-aware constraints** - Voltage, power, signal compatibility
3. **Post-solve injection** - Components added intelligently after pin solving
4. **Bidirectional routing** - TX/RX separation for wireless systems
5. **Hardware database** - Complete component specs & pin availability
6. **Code generation** - Full Arduino firmware from circuit topology
7. **Real-time validation** - Warnings before deployment
8. **Network topology support** - Single board → Multi-board mesh

---

## 📝 KEY TAKEAWAYS FOR SIMILAR PROJECTS

**Do Include:**
✅ Database of hardware specs
✅ Constraint solver (graph algorithms)
✅ LLM integration (multi-agent pattern)
✅ Validation rules (physics-aware)
✅ Code generation templates
✅ Dependency injection logic
✅ Post-processing steps

**Don't Skip:**
⚠️ Input validation (BOM Agent hallucination prevention)
⚠️ Business rules (TX/RX separation, sensor exclusion)
⚠️ Error messages (guide users to solutions)
⚠️ Testing (constraint solver accuracy is critical)
⚠️ Documentation (complex domain needs clear docs)

---

**Status: ✅ FULLY OPERATIONAL AND TESTED**

The circuits generator is working perfectly and ready to handle any IoT circuit request!
