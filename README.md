# IoT Circuit Builder

An AI-powered intelligent circuit design application that generates IoT circuits from natural language descriptions. Built with ASP.NET Core backend and Next.js frontend, featuring real-time circuit visualization, pin mapping, and automated Arduino code generation.

**AMD Hackathon Project** | AI-Powered IoT Electronics Assembly

---

## 🎯 Features

### 🤖 AI-Powered Circuit Generation
- **NLP-to-Circuit**: Convert natural language descriptions into complete IoT circuit designs
- **Automated Component Selection**: Intelligent selection of appropriate components based on project requirements
- **Smart Pin Mapping**: Automatic constraint solving for optimal microcontroller pin assignments
- **Dependency Management**: Handles complex component relationships and electrical constraints

### 🎨 Interactive Circuit Visualization
- **Real-time 3D Wokwi Simulation**: Live circuit preview with electrical behavior simulation
- **Drag-and-Drop Layout**: Interactive component positioning with automatic routing
- **Visual Pin Mapping**: Clear visualization of connections between components and microcontroller
- **Wire Routing**: Intelligent wire path calculation to minimize overlaps

### 💻 Code Generation
- **Arduino Sketch Compilation**: Generates complete, compilable Arduino C++ code
- **Wokwi.com Export**: Download circuits as ZIP files compatible with Wokwi electronics simulator
- **Breadboard Support**: Automatic breadboard power rail generation when needed
- **Hardware Library**: Support for 15+ common IoT components (LEDs, sensors, motors, etc.)

### 📊 Advanced Architecture
- **Constraint Solver**: CSP (Constraint Satisfaction Problem) solver for pin allocation
- **Layered Architecture**: Clean separation between Domain, Application, Core, and Infrastructure
- **Database Persistence**: SQLite for component library and board specifications
- **Responsive UI**: Modern, dark-themed interface with real-time feedback
- **Network Accessible**: Access the application from any device on your network (phones, tablets, laptops)

---

## 🏗️ Architecture

### Backend Structure (ASP.NET Core)

```
src/
├── IoTCircuitBuilder.API/
│   ├── Controllers/
│   │   └── CircuitController.cs          # REST endpoints
│   ├── Properties/
│   │   └── launchSettings.json           # Debug configuration
│   └── Program.cs                        # Service registration & middleware
│
├── IoTCircuitBuilder.Domain/
│   ├── Entities/                         # Core business models
│   ├── Enums/                            # Component & pin types
│   └── ValueObjects/                     # Immutable value types
│
├── IoTCircuitBuilder.Core/
│   ├── Algorithms/
│   │   └── ConstraintSolver.cs           # CSP solving logic
│   ├── Interfaces/                       # Core contract definitions
│   └── Validation/
│       └── PinMappingValidator.cs        # Electrical constraint validation
│
├── IoTCircuitBuilder.Application/
│   ├── DTOs/                             # Request/Response models
│   ├── Interfaces/                       # Service contracts
│   └── Services/
│       └── CircuitGenerationService.cs   # Main orchestration
│
└── IoTCircuitBuilder.Infrastructure/
    ├── Data/
    │   └── ApplicationDbContext.cs       # EF Core DbContext
    ├── Repositories/                     # Data access patterns
    ├── Services/                         # External integrations (LLM, Code Gen)
    └── Templates/                        # Arduino code templates
```

### Frontend Structure (Next.js)

```
client/
├── src/
│   ├── app/
│   │   ├── page.tsx                      # Main application page
│   │   ├── layout.tsx                    # Root layout
│   │   └── globals.css                   # Global styles
│   ├── components/
│   │   ├── WokwiCircuit.tsx              # 3D circuit visualization
│   │   ├── WokwiNode.tsx                 # Wokwi element wrapper
│   │   └── GenericNode.tsx               # Custom component renderer
│   ├── lib/
│   │   ├── api.ts                        # Backend API client
│   │   ├── layoutEngine.ts               # ELK.js graph layout
│   │   ├── wireRouter.ts                 # Wire path calculation
│   │   └── fritzing.ts                   # Fritzing integration
│   └── types/
│       ├── circuit.ts                    # Circuit data structures
│       └── wokwi-elements.d.ts           # Wokwi type definitions
├── next.config.ts                        # Next.js configuration
├── postcss.config.mjs                    # Tailwind CSS setup
├── tailwind.config.ts                    # Tailwind themes
└── tsconfig.json                         # TypeScript settings
```

---

## 📋 Technology Stack

### Backend
- **Framework**: ASP.NET Core 8.0
- **Database**: SQLite with Entity Framework Core
- **Logging**: Serilog
- **API Documentation**: Swagger/OpenAPI
- **Language**: C#
- **LLM Providers**: 
  - Perplexity Sonar (Primary - Real-time component intelligence)
  - Google Gemini (Fallback 1 - Multi-modal)
  - Groq Llama 3.3 (Fallback 2 - Open-source)

### Frontend
- **Framework**: Next.js 16 (React 19)
- **Styling**: Tailwind CSS 4 with PostCSS
- **Circuit Visualization**: 
  - Wokwi Elements (3D electronics simulation)
  - ReactFlow (graph/node visualization)
  - ELK.js (automatic layout engine)
- **Code Editor**: Monaco Editor
- **Export**: JSZip, File-Saver
- **Language**: TypeScript

### DevOps
- **Runtime**: .NET 8.0 & Node.js 18+
- **Package Managers**: NuGet, npm
- **Version Control**: Git

---

## 🚀 Supported Components

### Microcontrollers
- Arduino Uno R3

### Actuators
- Red LED (various brightness levels)
- DC Motor (with speed control)
- SG90 Servo Motor
- Buzzer (piezo)
- OLED Display (128x64)

### Sensors
- Push Button (digital input)
- HC-SR04 Ultrasonic Distance Sensor
- DHT11 Temperature/Humidity Sensor
- Photoresistor (LDR)
- IR Receiver Module

### Power & Passive
- 9V Battery
- Breadboard (power distribution)
- Resistors (fixed & variable)
- Capacitors (ceramic & electrolytic)
- Diodes
- L298N Motor Driver

---

## 📡 API Endpoints

### `POST /api/circuit/generate`
Generate an IoT circuit from natural language description.

**Request:**
```json
{
  "prompt": "Create a line following robot with 2 IR sensors and 2 DC motors with L298N driver"
}
```

**Response:**
```json
{
  "success": true,
  "generatedCode": "// Arduino sketch code...",
  "pinMapping": {
    "ir_sensor_1.SIGNAL": "D2",
    "ir_sensor_2.SIGNAL": "D3",
    "dc_motor_1.Term1": "D5",
    "l298n_driver.IN1": "D6"
  },
  "componentsUsed": ["Arduino Uno", "IR Sensor", "DC Motor", "L298N Driver"],
  "needsBreadboard": true
}
```

### `GET /api/circuit/health`
Health check endpoint.

**Response:**
```json
{
  "status": "healthy",
  "timestamp": "2026-02-28T21:45:00Z"
}
```

---

## 🎮 User Interface Walkthrough

### Main Page
1. **Header**: Branding and navigation
2. **Prompt Section**: Natural language input area with example suggestions
3. **Generation Button**: Trigger AI-powered circuit generation
4. **Result Tabs**:
   - **Circuit**: Interactive 3D visualization using Wokwi simulator
   - **Code**: Generated Arduino C++ sketch (syntax highlighted with Monaco)
   - **Pin Mapping**: Detailed component-to-pin connection table

### Export Options
- **Download ZIP for Wokwi.com**: Bundles circuit and code for direct import to Wokwi
- **Light Theme Pin Mapping**: Clean, readable display of all electrical connections

---

## 🔧 Configuration Files

### Backend
- **appsettings.json**: Database connection, logging levels
- **launchSettings.json**: Debug profiles, port configuration (default: 5050)
- **IoTCircuitBuilder.API.csproj**: NuGet dependencies and build settings

### Frontend
- **next.config.ts**: Turbopack root configuration, build optimization
- **package.json**: npm dependencies and scripts
- **tsconfig.json**: TypeScript strict mode settings
- **tailwind.config.ts**: Custom theme extensions

---

## 🧪 Testing

Unit tests are included for:
- Constraint solver algorithms
- Pin mapping validation
- Component dependency resolution

Run tests with:
```bash
dotnet test
```

---

## 🎨 UI/UX Highlights

- **Dark Theme Base**: Purple-to-navy gradient background
- **Light Circuit Display**: Clean white background for pin mapping
- **Real-time Loading**: Animated spinner during circuit generation
- **Responsive Layout**: Works on desktop and tablet devices
- **Accessibility**: Proper contrast ratios and semantic HTML

---

## 📦 Deployment

### Prerequisites
- .NET 8.0 SDK
- Node.js 18+
- SQLite (bundled with .NET)

### Production Build
```bash
# Backend
dotnet publish -c Release -o ./publish/api

# Frontend
cd client && npm run build
```

### Docker Support
The project can be containerized for cloud deployment (configuration available upon request).

---

## 🤝 Contributing

This is an AMD Hackathon project. Contributions should follow:
1. Clean Code principles
2. SOLID architecture patterns
3. Comprehensive Git commit messages
4. Code comments for complex algorithms

---

## 📝 License

Project developed for AMD Hackathon 2026.

---

## 🆘 Troubleshooting

### Port Already in Use
If port 5050 is occupied:
```powershell
taskkill /F /IM dotnet.exe
# Then restart the API
```

### Next.js Workspace Warning
Ensure `next.config.ts` has `turbopack.root` configured to eliminate multiple lockfile warnings.

### Database Issues
To reset the SQLite database:
```bash
rm -r bin/ obj/
dotnet build
```

---

## 📞 Support

For issues or questions regarding this project, refer to the [SETUP.md](SETUP.md) guide for detailed installation and configuration instructions.

---

**Built with ❤️ for the AMD Hackathon 2026**
