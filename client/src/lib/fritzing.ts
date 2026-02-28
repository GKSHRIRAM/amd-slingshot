import { ComponentVisual, PinCoordinate } from "@/types/circuit";
import {
    type SvgParseResult,
    svgToCanvas,
    ARDUINO_CONNECTOR_MAP,
    COMPONENT_CONNECTOR_MAPS,
} from "@/lib/svgConnectorParser";

// ═══════════════════════════════════════════════════════════════
// Board Layout Constants
// ═══════════════════════════════════════════════════════════════

const BOARD_X = 80;
const BOARD_Y = 100;
const BOARD_WIDTH = 600;
const BOARD_HEIGHT = 410;

// ═══════════════════════════════════════════════════════════════
// Arduino Pin Extraction from SVG
// Falls back to hardcoded positions if SVG parsing is unavailable
// ═══════════════════════════════════════════════════════════════

/** Build Arduino pin map from parsed SVG connector data */
export function buildArduinoPinsFromSvg(
    parsed: SvgParseResult
): Record<string, PinCoordinate> {
    const pins: Record<string, PinCoordinate> = {};

    for (const conn of parsed.connectors) {
        const pinName = ARDUINO_CONNECTOR_MAP[conn.connectorId];
        if (!pinName) continue;

        // Transform SVG coords → canvas coords
        const pos = svgToCanvas(
            conn.x, conn.y,
            parsed.viewBoxW, parsed.viewBoxH,
            BOARD_X, BOARD_Y,
            BOARD_WIDTH, BOARD_HEIGHT
        );

        // Determine side based on Y position in SVG
        // Top row pins have cy≈7.2, bottom row cy≈144 in the Uno SVG
        const normalizedY = conn.y / parsed.viewBoxH;
        const side: "top" | "bottom" = normalizedY < 0.5 ? "top" : "bottom";

        pins[pinName] = { x: pos.x, y: pos.y, label: pinName, side };
    }

    return pins;
}

/** Fallback: hardcoded Arduino pin positions (used if SVG parsing fails) */
export function getHardcodedArduinoPins(): Record<string, PinCoordinate> {
    const pins: Record<string, PinCoordinate> = {};
    const PIN_SPACING = 38;
    const TOP_ROW_Y = BOARD_Y - 12;
    const BOTTOM_ROW_Y = BOARD_Y + BOARD_HEIGHT + 12;

    for (let i = 0; i <= 13; i++) {
        pins[`D${i}`] = {
            x: BOARD_X + BOARD_WIDTH - 30 - i * PIN_SPACING,
            y: TOP_ROW_Y,
            label: `D${i}`,
            side: "top",
        };
    }
    for (let i = 0; i <= 5; i++) {
        pins[`A${i}`] = {
            x: BOARD_X + BOARD_WIDTH - 30 - i * PIN_SPACING,
            y: BOTTOM_ROW_Y,
            label: `A${i}`,
            side: "bottom",
        };
    }
    pins["5V"] = { x: BOARD_X + 60, y: BOTTOM_ROW_Y, label: "5V", side: "bottom" };
    pins["3V3"] = { x: BOARD_X + 28, y: BOTTOM_ROW_Y, label: "3V3", side: "bottom" };
    pins["GND"] = { x: BOARD_X + 92, y: BOTTOM_ROW_Y, label: "GND", side: "bottom" };

    return pins;
}

// Default export for backward compatibility (overwritten at render time)
export let ARDUINO_UNO_PINS: Record<string, PinCoordinate> = getHardcodedArduinoPins();

/** Called by the renderer once SVG parsing succeeds */
export function setArduinoPins(pins: Record<string, PinCoordinate>) {
    ARDUINO_UNO_PINS = pins;
}

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
    category: "sensor" | "actuator" | "driver" | "input" | "indicator" | "display" | "power" | "passive";
}

const COMPONENT_META: Record<string, ComponentMeta> = {
    ir_sensor: {
        color: "#FF6B35",
        gradient: ["#FF8855", "#CC4411"],
        icon: "📡",
        width: 225, height: 150,
        displayName: "IR Sensor",
        category: "sensor",
    },
    hc_sr04_ultrasonic: {
        color: "#4ECDC4",
        gradient: ["#5FE0D7", "#2BA89F"],
        icon: "📏",
        width: 255, height: 150,
        displayName: "HC-SR04",
        category: "sensor",
    },
    l298n_motor_driver: {
        color: "#E63946",
        gradient: ["#FF4D5A", "#B8202D"],
        icon: "⚙️",
        width: 285, height: 195,
        displayName: "L298N Driver",
        category: "driver",
    },
    dc_motor: {
        color: "#457B9D",
        gradient: ["#5A8FAF", "#2D5F7D"],
        icon: "🔄",
        width: 210, height: 165,
        displayName: "DC Motor",
        category: "actuator",
    },
    sg90_servo: {
        color: "#2A9D8F",
        gradient: ["#35B8A8", "#1E7A6F"],
        icon: "🦾",
        width: 225, height: 150,
        displayName: "SG90 Servo",
        category: "actuator",
    },
    led_red: {
        color: "#E76F51",
        gradient: ["#FF8866", "#CC4433"],
        icon: "💡",
        width: 150, height: 135,
        displayName: "Red LED",
        category: "indicator",
    },
    potentiometer: {
        color: "#8B5E3C",
        gradient: ["#A77450", "#6B4226"],
        icon: "🎛️",
        width: 210, height: 150,
        displayName: "Potentiometer",
        category: "input",
    },
    bme280: {
        color: "#6A5ACD",
        gradient: ["#7B6BE0", "#5445AA"],
        icon: "🌡️",
        width: 225, height: 150,
        displayName: "BME280",
        category: "sensor",
    },
    oled_128x64: {
        color: "#1C1C2E",
        gradient: ["#2A2A44", "#0E0E1A"],
        icon: "🖥️",
        width: 240, height: 165,
        displayName: "OLED Display",
        category: "display",
    },
    ldr_sensor: {
        color: "#DAA520",
        gradient: ["#F0C040", "#B8880A"],
        icon: "☀️",
        width: 180, height: 135,
        displayName: "LDR Sensor",
        category: "sensor",
    },
    dht11: {
        color: "#4A90D9",
        gradient: ["#5CA0E9", "#3070B9"],
        icon: "🌧️",
        width: 210, height: 150,
        displayName: "DHT11",
        category: "sensor",
    },
    buzzer: {
        color: "#2C2C2C",
        gradient: ["#444444", "#1A1A1A"],
        icon: "🔊",
        width: 165, height: 135,
        displayName: "Buzzer",
        category: "actuator",
    },
    push_button: {
        color: "#CD5C5C",
        gradient: ["#E06C6C", "#AA3C3C"],
        icon: "🔘",
        width: 150, height: 135,
        displayName: "Button",
        category: "input",
    },
    relay_module: {
        color: "#2F4F4F",
        gradient: ["#3D6565", "#1F3535"],
        icon: "⚡",
        width: 225, height: 150,
        displayName: "Relay",
        category: "actuator",
    },
    battery_9v: {
        color: "#333333",
        gradient: ["#555555", "#111111"],
        icon: "🔋",
        width: 195, height: 270,
        displayName: "9V Battery",
        category: "power",
    },
    resistor: {
        color: "#D2B48C",
        gradient: ["#E6C2A0", "#BEA382"],
        icon: "〰️",
        width: 135, height: 45,
        displayName: "Resistor",
        category: "passive",
    },
    diode: {
        color: "#222222",
        gradient: ["#444444", "#111111"],
        icon: "⏭️",
        width: 135, height: 45,
        displayName: "Diode",
        category: "passive",
    },
    capacitor_ceramic: {
        color: "#D2691E",
        gradient: ["#E67E22", "#B85C19"],
        icon: "🟡",
        width: 90, height: 90,
        displayName: "Ceramic Cap",
        category: "passive",
    },
    capacitor_electrolytic: {
        color: "#1A1A1A",
        gradient: ["#333333", "#0D0D0D"],
        icon: "🛢️",
        width: 105, height: 135,
        displayName: "Electrolytic Cap",
        category: "passive",
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
        width: 210, height: 150,
        displayName: type,
        category: "sensor" as const,
    };
}

export function buildComponentVisuals(
    pinMapping: Record<string, string>,
    svgParsedMap?: Map<string, SvgParseResult>
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
    const colSpacing = 360;
    const rowSpacing = 240;

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
        const svgParsed = svgParsedMap?.get(type);
        const connMap = COMPONENT_CONNECTOR_MAPS[type];

        // Build a reverse map: pinName → connectorId for this component type
        const pinNameToConnId = new Map<string, string>();
        if (connMap) {
            for (const [connId, pinName] of Object.entries(connMap)) {
                pinNameToConnId.set(pinName, connId);
            }
        }

        for (const [key] of Object.entries(pinMapping)) {
            if (!key.startsWith(instance + ".")) continue;
            const pinName = key.split(".")[1];

            // Try SVG-extracted coordinates first
            if (svgParsed && connMap) {
                const connId = pinNameToConnId.get(pinName);
                if (connId) {
                    const conn = svgParsed.connectors.find(c => c.connectorId === connId);
                    if (conn) {
                        // Calculate where in the component box the SVG draws this pin
                        // Component SVG is rendered in the box area (x, y, width, height-22)
                        const svgAspect = svgParsed.viewBoxW / svgParsed.viewBoxH;
                        const boxW = meta.width - 6;
                        const boxH = Math.max(meta.height, pins.length * 22 + 40) - 22;
                        let drawW: number, drawH: number;

                        if (boxW / boxH > svgAspect) {
                            drawH = boxH;
                            drawW = drawH * svgAspect;
                        } else {
                            drawW = boxW;
                            drawH = drawW / svgAspect;
                        }

                        const drawX = x + (meta.width - drawW) / 2;
                        const drawY = y + 2;

                        const pos = svgToCanvas(
                            conn.x, conn.y,
                            svgParsed.viewBoxW, svgParsed.viewBoxH,
                            drawX, drawY,
                            drawW, drawH
                        );

                        // Determine which side the pin is on
                        const normalizedX = conn.x / svgParsed.viewBoxW;
                        const side: "left" | "right" = normalizedX < 0.5 ? "left" : "right";

                        pins.push({
                            x: pos.x,
                            y: pos.y,
                            label: pinName,
                            side,
                        });
                        continue;
                    }
                }
            }

            // Fallback: stack pins vertically on the left
            const pinIdx = pins.length;
            pins.push({
                x: x - 14,
                y: y + 24 + pinIdx * 22,
                label: pinName,
                side: "left",
            });
        }

        components.push({
            instance,
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
