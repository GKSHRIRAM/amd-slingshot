// ═══════════════════════════════════════════════════════════════
//  SVG CONNECTOR PARSER
//  Extracts pixel-perfect pin coordinates from Fritzing SVGs
//  using embedded connectorNpin elements + DOMParser
// ═══════════════════════════════════════════════════════════════

export interface SvgConnectorCoord {
    connectorId: string;     // e.g. "connector0"
    x: number;               // SVG-native X (before scaling)
    y: number;               // SVG-native Y
}

export interface SvgParseResult {
    viewBoxW: number;
    viewBoxH: number;
    connectors: SvgConnectorCoord[];
}

// Cache parsed SVGs to avoid re-fetching
const svgCache = new Map<string, SvgParseResult>();

/**
 * Fetch and parse an SVG file to extract all connectorNpin coordinates.
 * Coordinates are in the SVG's native viewBox units.
 */
export async function parseSvgConnectors(svgUrl: string): Promise<SvgParseResult | null> {
    if (svgCache.has(svgUrl)) {
        return svgCache.get(svgUrl)!;
    }

    try {
        const response = await fetch(svgUrl);
        if (!response.ok) return null;
        const svgText = await response.text();

        const parser = new DOMParser();
        const doc = parser.parseFromString(svgText, "image/svg+xml");
        const svgEl = doc.querySelector("svg");
        if (!svgEl) return null;

        // Extract viewBox
        const vb = svgEl.getAttribute("viewBox");
        let viewBoxW = 100, viewBoxH = 100;
        if (vb) {
            const parts = vb.split(/[\s,]+/).map(Number);
            if (parts.length >= 4) {
                viewBoxW = parts[2];
                viewBoxH = parts[3];
            }
        }

        // Check for a breadboard group with transform (e.g., servo.svg)
        const bbGroup = doc.querySelector('g[id="breadboard"]');
        let groupTx = 0, groupTy = 0;
        if (bbGroup) {
            const transform = bbGroup.getAttribute("transform");
            if (transform) {
                const match = transform.match(/translate\(\s*([-\d.]+)\s*[,\s]\s*([-\d.]+)\s*\)/);
                if (match) {
                    groupTx = parseFloat(match[1]);
                    groupTy = parseFloat(match[2]);
                }
            }
        }

        // Find all connectorNpin elements
        const connectors: SvgConnectorCoord[] = [];
        const allElements = doc.querySelectorAll("[id]");
        for (const el of allElements) {
            const id = el.getAttribute("id") || "";
            const pinMatch = id.match(/^connector(\d+)pin$/);
            if (!pinMatch) continue;

            const connectorId = `connector${pinMatch[1]}`;
            let cx = 0, cy = 0;

            const tag = el.tagName.toLowerCase();
            if (tag === "circle") {
                cx = parseFloat(el.getAttribute("cx") || "0");
                cy = parseFloat(el.getAttribute("cy") || "0");
            } else if (tag === "rect") {
                const rx = parseFloat(el.getAttribute("x") || "0");
                const ry = parseFloat(el.getAttribute("y") || "0");
                const rw = parseFloat(el.getAttribute("width") || "0");
                const rh = parseFloat(el.getAttribute("height") || "0");
                cx = rx + rw / 2;
                cy = ry + rh / 2;
            } else if (tag === "line") {
                cx = parseFloat(el.getAttribute("x1") || "0");
                cy = parseFloat(el.getAttribute("y1") || "0");
            }

            // Apply group transform if the element is inside the breadboard group
            if (bbGroup && bbGroup.contains(el)) {
                cx += groupTx;
                cy += groupTy;
            }

            connectors.push({ connectorId, x: cx, y: cy });
        }

        const result: SvgParseResult = { viewBoxW, viewBoxH, connectors };
        svgCache.set(svgUrl, result);
        return result;
    } catch {
        return null;
    }
}

/**
 * Transform SVG-native connector coordinates to canvas pixel coordinates.
 * Given: component drawn at (drawX, drawY) with size (drawW, drawH)
 * and the SVG has viewBox dimensions (vbW, vbH)
 */
export function svgToCanvas(
    connX: number, connY: number,
    vbW: number, vbH: number,
    drawX: number, drawY: number,
    drawW: number, drawH: number,
): { x: number; y: number } {
    return {
        x: drawX + (connX / vbW) * drawW,
        y: drawY + (connY / vbH) * drawH,
    };
}

// ═══════════════════════════════════════════════════════════════
//  CONNECTOR → PIN NAME MAPPINGS
//  Maintained per-component (SVGs only have connector IDs, not
//  human-readable pin names)
// ═══════════════════════════════════════════════════════════════

/**
 * Arduino Uno Rev3 connector mapping.
 * Derived from the official Fritzing part definition:
 *   connector0-5     → A0-A5 (bottom analog header)
 *   connector51-66   → D13-D0 (top digital header, right to left)
 *   connector67-68   → AREF, GND (top, leftmost)
 *   connector84-91   → Power header: RESET, 3V3, 5V, GND, GND, VIN, ?, ?
 */
export const ARDUINO_CONNECTOR_MAP: Record<string, string> = {
    // Bottom row — Analog pins (left to right in SVG)
    connector0: "A0",
    connector1: "A1",
    connector2: "A2",
    connector3: "A3",
    connector4: "A4",
    connector5: "A5",

    // Top row — Digital pins (right to left: D0 is rightmost)
    // connector51 = IOREF, connector52 = RESET
    connector51: "IOREF",
    connector52: "RESET_T",
    connector53: "D13",
    connector54: "D12",
    connector55: "D11",
    connector56: "D10",
    connector57: "D9",
    connector58: "D8",
    connector59: "D7",
    connector60: "D6",
    connector61: "D5",
    connector62: "D4",
    connector63: "D3",
    connector64: "D2",
    connector65: "D1",
    connector66: "D0",
    connector67: "AREF",
    connector68: "GND_T",

    // Bottom power header
    connector84: "RESET_B",
    connector85: "3V3",
    connector86: "5V",
    connector87: "GND",
    connector88: "GND2",
    connector89: "VIN",
    connector90: "NC1",
    connector91: "NC2",

    // ICSP header
    connector39: "ICSP_MISO",
    connector40: "ICSP_5V",
    connector41: "ICSP_SCK",
    connector42: "ICSP_MOSI",
    connector43: "ICSP_RST",
    connector44: "ICSP_GND",
};

/**
 * Component connector mappings — maps Fritzing connector IDs
 * to the pin names our backend uses.
 */
export const COMPONENT_CONNECTOR_MAPS: Record<string, Record<string, string>> = {
    led_red: {
        connector0: "ANODE",
        connector1: "CATHODE",
    },
    sg90_servo: {
        connector0: "GND",
        connector1: "VCC",
        connector2: "SIGNAL",
    },
    hc_sr04_ultrasonic: {
        connector0: "VCC",
        connector1: "TRIG",
        connector2: "ECHO",
        connector3: "GND",
    },
    ir_sensor: {
        connector0: "VCC",
        connector1: "GND",
        connector2: "OUT",
    },
    l298n_motor_driver: {
        connector0: "ENA",
        connector1: "IN1",
        connector2: "IN2",
        connector3: "IN3",
        connector4: "IN4",
        connector5: "ENB",
        connector6: "MOTOR_A+",
        connector7: "MOTOR_A-",
        connector8: "VCC",
        connector9: "GND",
        connector10: "5V",
        connector11: "MOTOR_B+",
        connector12: "MOTOR_B-",
    },
    dc_motor: {
        connector0: "MOTOR+",
        connector1: "MOTOR-",
    },
    potentiometer: {
        connector0: "VCC",
        connector1: "SIGNAL",
        connector2: "GND",
    },
    bme280: {
        connector0: "VCC",
        connector1: "GND",
        connector2: "SCL",
        connector3: "SDA",
    },
    oled_128x64: {
        connector0: "GND",
        connector1: "VCC",
        connector2: "SCL",
        connector3: "SDA",
    },
    ldr_sensor: {
        connector0: "VCC",
        connector1: "SIGNAL",
    },
    dht11: {
        connector0: "VCC",
        connector1: "DATA",
        connector2: "GND",
    },
    buzzer: {
        connector0: "VCC",
        connector1: "GND",
    },
    push_button: {
        connector0: "VCC",
        connector1: "GND",
    },
    relay_module: {
        connector0: "VCC",
        connector1: "GND",
        connector2: "SIGNAL",
    },
};

// ═══════════════════════════════════════════════════════════════
//  HIGH-LEVEL API
// ═══════════════════════════════════════════════════════════════

/**
 * Parse an SVG and return a map of pin names → SVG-native coordinates.
 * Uses the connector map for the given component type.
 */
export async function getComponentPinCoords(
    svgUrl: string,
    componentType: string
): Promise<Map<string, { x: number; y: number }> | null> {
    const parsed = await parseSvgConnectors(svgUrl);
    if (!parsed) return null;

    const connMap = COMPONENT_CONNECTOR_MAPS[componentType];
    if (!connMap) return null;

    const pinCoords = new Map<string, { x: number; y: number }>();
    for (const conn of parsed.connectors) {
        const pinName = connMap[conn.connectorId];
        if (pinName) {
            pinCoords.set(pinName, { x: conn.x, y: conn.y });
        }
    }

    return pinCoords;
}

/**
 * Parse Arduino Uno SVG and return pin name → SVG-native coordinates.
 */
export async function getArduinoPinCoords(
    svgUrl: string
): Promise<Map<string, { x: number; y: number }> | null> {
    const parsed = await parseSvgConnectors(svgUrl);
    if (!parsed) return null;

    const pinCoords = new Map<string, { x: number; y: number }>();
    for (const conn of parsed.connectors) {
        const pinName = ARDUINO_CONNECTOR_MAP[conn.connectorId];
        if (pinName) {
            pinCoords.set(pinName, { x: conn.x, y: conn.y });
        }
    }

    return pinCoords;
}
