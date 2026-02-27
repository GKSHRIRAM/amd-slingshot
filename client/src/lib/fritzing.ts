import { ComponentVisual, PinCoordinate } from "@/types/circuit";

// ═══════════════════════════════════════════════════════════════
// Arduino Uno Pin Coordinates (Fritzing-based layout)
// These map physical pin positions to (x, y) canvas coordinates
// ═══════════════════════════════════════════════════════════════

const BOARD_X = 80;
const BOARD_Y = 100;
const BOARD_WIDTH = 600;
const BOARD_HEIGHT = 410;

const PIN_SPACING = 38;
const TOP_ROW_Y = BOARD_Y - 12;
const BOTTOM_ROW_Y = BOARD_Y + BOARD_HEIGHT + 12;

export const ARDUINO_UNO_PINS: Record<string, PinCoordinate> = {};

// Digital pins D0-D13 (top row, right to left)
for (let i = 0; i <= 13; i++) {
    ARDUINO_UNO_PINS[`D${i}`] = {
        x: BOARD_X + BOARD_WIDTH - 30 - i * PIN_SPACING,
        y: TOP_ROW_Y,
        label: `D${i}`,
        side: "top",
    };
}

// Analog pins A0-A5 (bottom row, right to left)
for (let i = 0; i <= 5; i++) {
    ARDUINO_UNO_PINS[`A${i}`] = {
        x: BOARD_X + BOARD_WIDTH - 30 - i * PIN_SPACING,
        y: BOTTOM_ROW_Y,
        label: `A${i}`,
        side: "bottom",
    };
}

// Power pins
ARDUINO_UNO_PINS["5V"] = {
    x: BOARD_X + 60,
    y: BOTTOM_ROW_Y,
    label: "5V",
    side: "bottom",
};
ARDUINO_UNO_PINS["3V3"] = {
    x: BOARD_X + 28,
    y: BOTTOM_ROW_Y,
    label: "3V3",
    side: "bottom",
};
ARDUINO_UNO_PINS["GND"] = {
    x: BOARD_X + 92,
    y: BOTTOM_ROW_Y,
    label: "GND",
    side: "bottom",
};

// ═══════════════════════════════════════════════════════════════
//  COMPONENT METADATA — All 14 supported components
// ═══════════════════════════════════════════════════════════════

interface ComponentMeta {
    color: string;
    gradient: [string, string];  // Top → Bottom gradient
    icon: string;
    width: number;
    height: number;
    displayName: string;
    category: "sensor" | "actuator" | "driver" | "input" | "indicator" | "display";
}

const COMPONENT_META: Record<string, ComponentMeta> = {
    ir_sensor: {
        color: "#FF6B35",
        gradient: ["#FF8855", "#CC4411"],
        icon: "📡",
        width: 150, height: 100,
        displayName: "IR Sensor",
        category: "sensor",
    },
    hc_sr04_ultrasonic: {
        color: "#4ECDC4",
        gradient: ["#5FE0D7", "#2BA89F"],
        icon: "📏",
        width: 170, height: 100,
        displayName: "HC-SR04",
        category: "sensor",
    },
    l298n_motor_driver: {
        color: "#E63946",
        gradient: ["#FF4D5A", "#B8202D"],
        icon: "⚙️",
        width: 190, height: 130,
        displayName: "L298N Driver",
        category: "driver",
    },
    dc_motor: {
        color: "#457B9D",
        gradient: ["#5A8FAF", "#2D5F7D"],
        icon: "🔄",
        width: 140, height: 110,
        displayName: "DC Motor",
        category: "actuator",
    },
    sg90_servo: {
        color: "#2A9D8F",
        gradient: ["#35B8A8", "#1E7A6F"],
        icon: "🦾",
        width: 150, height: 100,
        displayName: "SG90 Servo",
        category: "actuator",
    },
    led_red: {
        color: "#E76F51",
        gradient: ["#FF8866", "#CC4433"],
        icon: "💡",
        width: 100, height: 90,
        displayName: "Red LED",
        category: "indicator",
    },
    potentiometer: {
        color: "#8B5E3C",
        gradient: ["#A77450", "#6B4226"],
        icon: "🎛️",
        width: 140, height: 100,
        displayName: "Potentiometer",
        category: "input",
    },
    bme280: {
        color: "#6A5ACD",
        gradient: ["#7B6BE0", "#5445AA"],
        icon: "🌡️",
        width: 150, height: 100,
        displayName: "BME280",
        category: "sensor",
    },
    oled_128x64: {
        color: "#1C1C2E",
        gradient: ["#2A2A44", "#0E0E1A"],
        icon: "🖥️",
        width: 160, height: 110,
        displayName: "OLED Display",
        category: "display",
    },
    ldr_sensor: {
        color: "#DAA520",
        gradient: ["#F0C040", "#B8880A"],
        icon: "☀️",
        width: 120, height: 90,
        displayName: "LDR Sensor",
        category: "sensor",
    },
    dht11: {
        color: "#4A90D9",
        gradient: ["#5CA0E9", "#3070B9"],
        icon: "🌧️",
        width: 140, height: 100,
        displayName: "DHT11",
        category: "sensor",
    },
    buzzer: {
        color: "#2C2C2C",
        gradient: ["#444444", "#1A1A1A"],
        icon: "🔊",
        width: 110, height: 90,
        displayName: "Buzzer",
        category: "actuator",
    },
    push_button: {
        color: "#CD5C5C",
        gradient: ["#E06C6C", "#AA3C3C"],
        icon: "🔘",
        width: 100, height: 90,
        displayName: "Button",
        category: "input",
    },
    relay_module: {
        color: "#2F4F4F",
        gradient: ["#3D6565", "#1F3535"],
        icon: "⚡",
        width: 150, height: 100,
        displayName: "Relay",
        category: "actuator",
    },
};

// ═══════════════════════════════════════════════════════════════
//  COMPONENT VISUAL BUILDER
// ═══════════════════════════════════════════════════════════════

export { BOARD_X, BOARD_Y, BOARD_WIDTH, BOARD_HEIGHT };

export function getComponentMeta(type: string): ComponentMeta {
    return COMPONENT_META[type] || {
        color: "#6C757D",
        gradient: ["#7D8E9D", "#4C5D6D"],
        icon: "❓",
        width: 140, height: 100,
        displayName: type,
        category: "sensor" as const,
    };
}

export function buildComponentVisuals(
    pinMapping: Record<string, string>
): ComponentVisual[] {
    const components: ComponentVisual[] = [];
    const instanceSet = new Set<string>();

    // Collect unique component instances
    for (const key of Object.keys(pinMapping)) {
        const instance = key.split(".")[0];
        instanceSet.add(instance);
    }

    const instances = Array.from(instanceSet);
    const startX = 760;
    const startY = 80;
    const colSpacing = 240;
    const rowSpacing = 160;

    instances.forEach((instance, idx) => {
        const type = instance.replace(/_\d+$/, "");
        const meta = getComponentMeta(type);

        // Position components in a grid to the right of the board
        const col = Math.floor(idx / 4);
        const row = idx % 4;
        const x = startX + col * colSpacing;
        const y = startY + row * rowSpacing;

        // Collect pins for this instance
        const pins: PinCoordinate[] = [];
        for (const [key] of Object.entries(pinMapping)) {
            if (!key.startsWith(instance + ".")) continue;
            const pinName = key.split(".")[1];
            const pinIdx = pins.length;
            pins.push({
                x: x - 14,
                y: y + 24 + pinIdx * 22,
                label: pinName,
                side: "left",
            });
        }

        components.push({
            type,
            displayName: formatDisplayName(type, instance),
            x,
            y,
            width: meta.width,
            height: Math.max(meta.height, pins.length * 22 + 40),
            color: meta.color,
            pins,
        });
    });

    return components;
}

function formatDisplayName(type: string, instance: string): string {
    const meta = COMPONENT_META[type];
    const idx = instance.split("_").pop();
    const name = meta?.displayName || type;
    return `${name} #${idx}`;
}

// ═══════════════════════════════════════════════════════════════
//  WIRE COLORS — Vibrant, physics-accurate color coding
// ═══════════════════════════════════════════════════════════════

export function getWireColor(pinName: string): string {
    const upper = pinName.toUpperCase();
    if (upper === "VCC" || upper === "5V") return "#FF4444";         // Bright red — power
    if (upper === "GND" || upper === "CATHODE") return "#555555";    // Dark grey — ground
    if (upper === "SIGNAL" || upper === "ENA" || upper === "ENB")
        return "#FF8800";                                             // Vibrant orange — PWM/signal
    if (upper === "SDA") return "#00DD88";                           // Cyan-green — I2C data
    if (upper === "SCL") return "#00AAFF";                           // Bright blue — I2C clock
    if (upper === "TRIG") return "#FFD700";                          // Gold — trigger
    if (upper === "ECHO") return "#7B68EE";                          // Purple — echo return
    if (upper === "DATA") return "#FF69B4";                          // Pink — data line
    if (upper === "ANODE") return "#FF6600";                         // Orange — LED anode
    return "#44AAFF";                                                 // Default blue — digital
}

export function getWireGlow(pinName: string): string {
    const upper = pinName.toUpperCase();
    if (upper === "VCC" || upper === "5V") return "rgba(255,68,68,0.4)";
    if (upper === "GND" || upper === "CATHODE") return "rgba(85,85,85,0.3)";
    return "rgba(68,170,255,0.3)";
}
