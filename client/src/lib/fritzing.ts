import { ComponentVisual, PinCoordinate } from "@/types/circuit";

// ═══════════════════════════════════════════════════════════════
// Arduino Uno Pin Coordinates (Fritzing-based layout)
// These map physical pin positions to (x, y) canvas coordinates
// ═══════════════════════════════════════════════════════════════

const BOARD_X = 80;
const BOARD_Y = 100;
const BOARD_WIDTH = 340;
const BOARD_HEIGHT = 220;

const PIN_SPACING = 22;
const TOP_ROW_Y = BOARD_Y - 8;
const BOTTOM_ROW_Y = BOARD_Y + BOARD_HEIGHT + 8;

export const ARDUINO_UNO_PINS: Record<string, PinCoordinate> = {};

// Digital pins D0-D13 (top row, right to left)
for (let i = 0; i <= 13; i++) {
    ARDUINO_UNO_PINS[`D${i}`] = {
        x: BOARD_X + BOARD_WIDTH - 20 - i * PIN_SPACING,
        y: TOP_ROW_Y,
        label: `D${i}`,
        side: "top",
    };
}

// Analog pins A0-A5 (bottom row, right to left)
for (let i = 0; i <= 5; i++) {
    ARDUINO_UNO_PINS[`A${i}`] = {
        x: BOARD_X + BOARD_WIDTH - 20 - i * PIN_SPACING,
        y: BOTTOM_ROW_Y,
        label: `A${i}`,
        side: "bottom",
    };
}

// Power pins
ARDUINO_UNO_PINS["5V"] = {
    x: BOARD_X + 40,
    y: BOTTOM_ROW_Y,
    label: "5V",
    side: "bottom",
};
ARDUINO_UNO_PINS["3V3"] = {
    x: BOARD_X + 18,
    y: BOTTOM_ROW_Y,
    label: "3V3",
    side: "bottom",
};
ARDUINO_UNO_PINS["GND"] = {
    x: BOARD_X + 62,
    y: BOTTOM_ROW_Y,
    label: "GND",
    side: "bottom",
};

// ─── Component Visual Defaults ──────────────────────────────────

const COMPONENT_COLORS: Record<string, string> = {
    ir_sensor: "#FF6B35",
    hc_sr04_ultrasonic: "#4ECDC4",
    l298n_motor_driver: "#E63946",
    dc_motor: "#457B9D",
    sg90_servo: "#2A9D8F",
    led_red: "#E76F51",
    default: "#6C757D",
};

const COMPONENT_SIZES: Record<string, { w: number; h: number }> = {
    ir_sensor: { w: 70, h: 45 },
    hc_sr04_ultrasonic: { w: 90, h: 45 },
    l298n_motor_driver: { w: 100, h: 60 },
    dc_motor: { w: 60, h: 50 },
    sg90_servo: { w: 70, h: 50 },
    led_red: { w: 40, h: 40 },
};

export function buildComponentVisuals(
    pinMapping: Record<string, string>
): ComponentVisual[] {
    const components: ComponentVisual[] = [];
    const instanceSet = new Set<string>();

    // Collect unique component instances
    for (const key of Object.keys(pinMapping)) {
        const instance = key.split(".")[0]; // "ir_sensor_0"
        instanceSet.add(instance);
    }

    const instances = Array.from(instanceSet);
    const startX = 520;
    const startY = 80;
    const spacing = 100;

    instances.forEach((instance, idx) => {
        const type = instance.replace(/_\d+$/, ""); // "ir_sensor"
        const size = COMPONENT_SIZES[type] || { w: 70, h: 45 };
        const color = COMPONENT_COLORS[type] || COMPONENT_COLORS.default;

        // Position components in a column to the right of the board
        const col = Math.floor(idx / 4);
        const row = idx % 4;
        const x = startX + col * 160;
        const y = startY + row * spacing;

        // Collect pins for this instance
        const pins: PinCoordinate[] = [];
        for (const [key, boardPin] of Object.entries(pinMapping)) {
            if (!key.startsWith(instance + ".")) continue;
            const pinName = key.split(".")[1];
            const pinIdx = pins.length;
            pins.push({
                x: x - 10,
                y: y + 12 + pinIdx * 14,
                label: pinName,
                side: "left",
            });
        }

        components.push({
            type,
            displayName: formatDisplayName(type, instance),
            x,
            y,
            width: size.w,
            height: Math.max(size.h, pins.length * 14 + 24),
            color,
            pins,
        });
    });

    return components;
}

function formatDisplayName(type: string, instance: string): string {
    const names: Record<string, string> = {
        ir_sensor: "IR Sensor",
        hc_sr04_ultrasonic: "HC-SR04",
        l298n_motor_driver: "L298N Driver",
        dc_motor: "DC Motor",
        sg90_servo: "SG90 Servo",
        led_red: "Red LED",
    };
    const idx = instance.split("_").pop();
    return `${names[type] || type} #${idx}`;
}

export function getWireColor(pinName: string): string {
    if (pinName === "VCC" || pinName === "5V") return "#E63946";
    if (pinName === "GND" || pinName === "CATHODE") return "#1D3557";
    if (pinName === "SIGNAL" || pinName === "ENA" || pinName === "ENB")
        return "#F4A261";
    return "#2A9D8F";
}
