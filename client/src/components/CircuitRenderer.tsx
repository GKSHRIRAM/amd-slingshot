"use client";

import { useRef, useEffect, forwardRef, useImperativeHandle } from "react";
import {
    ARDUINO_UNO_PINS,
    BOARD_X,
    BOARD_Y,
    BOARD_WIDTH,
    BOARD_HEIGHT,
    buildComponentVisuals,
    getComponentMeta,
    getWireColor,
} from "@/lib/fritzing";
import { RoutingGrid, findPath, applyWireOffset, type Point } from "@/lib/wireRouter";

interface CircuitRendererProps {
    pinMapping: Record<string, string>;
    needsBreadboard?: boolean;
}

// ═══════════════════════════════════════════════════════════════
// Professional Circuit Renderer
// Clean Fritzing SVGs + A* Manhattan wire routing
// ═══════════════════════════════════════════════════════════════

const CANVAS_W = 1400;
const CANVAS_H = 900;

const CircuitRenderer = forwardRef<HTMLCanvasElement, CircuitRendererProps>(
    function CircuitRenderer({ pinMapping, needsBreadboard = false }, ref) {
        const canvasRef = useRef<HTMLCanvasElement>(null);
        useImperativeHandle(ref, () => canvasRef.current as HTMLCanvasElement);

        useEffect(() => {
            const canvas = canvasRef.current;
            if (!canvas) return;
            const ctx = canvas.getContext("2d");
            if (!ctx) return;

            const components = buildComponentVisuals(pinMapping);
            const types = new Set(components.map((c) => c.type));
            types.add("arduino_uno");

            preloadComponentImages(Array.from(types)).then((imageMap) => {
                const dpr = window.devicePixelRatio || 2;
                canvas.width = CANVAS_W * dpr;
                canvas.height = CANVAS_H * dpr;
                ctx.scale(dpr, dpr);
                ctx.imageSmoothingEnabled = true;
                ctx.imageSmoothingQuality = "high";
                ctx.clearRect(0, 0, CANVAS_W, CANVAS_H);

                // ─── Background ─────────────────────────────
                drawBackground(ctx);

                // ─── Build routing grid & block obstacles ────
                const grid = new RoutingGrid(CANVAS_W, CANVAS_H);
                // Block Arduino board
                grid.blockRegion(BOARD_X, BOARD_Y, BOARD_WIDTH, BOARD_HEIGHT);
                // Block each component
                for (const comp of components) {
                    grid.blockRegion(comp.x, comp.y, comp.width, comp.height);
                }
                // Unblock pin positions so pathfinder can reach them
                for (const pin of Object.values(ARDUINO_UNO_PINS)) {
                    grid.unblockCell(pin.x, pin.y);
                }
                for (const comp of components) {
                    for (const pin of comp.pins) {
                        grid.unblockCell(pin.x, pin.y);
                    }
                }

                // ─── 1. ROUTE & DRAW WIRES (behind everything) ──
                drawRoutedWires(ctx, grid, pinMapping, components, needsBreadboard);

                // ─── 2. DRAW BREADBOARD (if needed) ─────────
                if (needsBreadboard) {
                    drawBreadboard(ctx);
                }

                // ─── 3. DRAW ARDUINO BOARD ──────────────────
                const arduinoImg = imageMap.get("arduino_uno");
                if (arduinoImg) {
                    drawArduinoBoardSVG(ctx, arduinoImg);
                } else {
                    drawArduinoBoardFallback(ctx);
                }
                drawPinHeaders(ctx);

                // ─── 4. DRAW COMPONENTS (clean SVGs) ────────
                for (const comp of components) {
                    const img = imageMap.get(comp.type);
                    drawComponentClean(ctx, comp, img || null);
                }

                // ─── 5. LEGEND ──────────────────────────────
                drawLegend(ctx, components.length);
            });
        }, [pinMapping, needsBreadboard]);

        return (
            <div className="relative rounded-xl overflow-hidden border border-white/10 bg-[#0a0a1a]">
                <canvas
                    ref={canvasRef}
                    width={CANVAS_W}
                    height={CANVAS_H}
                    style={{
                        width: "100%",
                        height: "auto",
                        imageRendering: "crisp-edges" as const,
                    }}
                    className="block"
                />
            </div>
        );
    }
);

export default CircuitRenderer;

// ═══════════════════════════════════════════════════════════════
//  SVG IMAGE PRELOADER
// ═══════════════════════════════════════════════════════════════

function preloadComponentImages(types: string[]): Promise<Map<string, HTMLImageElement>> {
    return new Promise((resolve) => {
        const imageMap = new Map<string, HTMLImageElement>();
        let loaded = 0;
        const total = types.length;
        if (total === 0) { resolve(imageMap); return; }

        for (const type of types) {
            const img = new Image();
            img.onload = () => { imageMap.set(type, img); loaded++; if (loaded === total) resolve(imageMap); };
            img.onerror = () => { loaded++; if (loaded === total) resolve(imageMap); };
            img.src = `/assets/components/${type}.svg`;
        }
    });
}

// ═══════════════════════════════════════════════════════════════
//  BACKGROUND
// ═══════════════════════════════════════════════════════════════

function drawBackground(ctx: CanvasRenderingContext2D) {
    const bg = ctx.createRadialGradient(CANVAS_W / 2, CANVAS_H / 2, 50, CANVAS_W / 2, CANVAS_H / 2, CANVAS_W);
    bg.addColorStop(0, "#151530");
    bg.addColorStop(1, "#0a0a1a");
    ctx.fillStyle = bg;
    ctx.fillRect(0, 0, CANVAS_W, CANVAS_H);

    // Subtle dot grid
    ctx.fillStyle = "rgba(255,255,255,0.02)";
    for (let x = 0; x < CANVAS_W; x += 24) {
        for (let y = 0; y < CANVAS_H; y += 24) {
            ctx.beginPath();
            ctx.arc(x, y, 1, 0, Math.PI * 2);
            ctx.fill();
        }
    }
}

// ═══════════════════════════════════════════════════════════════
//  ARDUINO BOARD — SVG or fallback
// ═══════════════════════════════════════════════════════════════

function drawArduinoBoardSVG(ctx: CanvasRenderingContext2D, img: HTMLImageElement) {
    ctx.shadowColor = "rgba(0,180,100,0.2)";
    ctx.shadowBlur = 25;
    ctx.shadowOffsetY = 5;
    ctx.drawImage(img, BOARD_X, BOARD_Y, BOARD_WIDTH, BOARD_HEIGHT);
    ctx.shadowColor = "transparent";
    ctx.shadowBlur = 0;
}

function drawArduinoBoardFallback(ctx: CanvasRenderingContext2D) {
    ctx.shadowColor = "rgba(0,180,100,0.2)";
    ctx.shadowBlur = 25;
    ctx.shadowOffsetY = 5;
    const pcb = ctx.createLinearGradient(BOARD_X, BOARD_Y, BOARD_X, BOARD_Y + BOARD_HEIGHT);
    pcb.addColorStop(0, "#00796B");
    pcb.addColorStop(1, "#004D40");
    ctx.fillStyle = pcb;
    roundRect(ctx, BOARD_X, BOARD_Y, BOARD_WIDTH, BOARD_HEIGHT, 10);
    ctx.fill();
    ctx.shadowColor = "transparent";
    ctx.shadowBlur = 0;

    ctx.fillStyle = "#fff";
    ctx.font = "bold 18px 'Inter', sans-serif";
    ctx.textAlign = "center";
    ctx.fillText("Arduino Uno R3", BOARD_X + BOARD_WIDTH / 2, BOARD_Y + BOARD_HEIGHT / 2);
}

// ═══════════════════════════════════════════════════════════════
//  PIN HEADERS — Overlay on board
// ═══════════════════════════════════════════════════════════════

const PWM_PINS = new Set(["D3", "D5", "D6", "D9", "D10", "D11"]);

function drawPinHeaders(ctx: CanvasRenderingContext2D) {
    for (const [pinId, pin] of Object.entries(ARDUINO_UNO_PINS)) {
        const isPwm = PWM_PINS.has(pinId);
        const isPower = pinId === "5V" || pinId === "3V3";
        const isGnd = pinId === "GND";

        let color = "#FFD700";
        if (isPwm) color = "#FF8800";
        if (isPower) color = "#FF4444";
        if (isGnd) color = "#888888";

        // Hole
        ctx.fillStyle = "#111";
        ctx.beginPath();
        ctx.arc(pin.x, pin.y, 7, 0, Math.PI * 2);
        ctx.fill();

        // Ring
        ctx.strokeStyle = color;
        ctx.lineWidth = 2.5;
        ctx.beginPath();
        ctx.arc(pin.x, pin.y, 7, 0, Math.PI * 2);
        ctx.stroke();

        // Dot
        ctx.fillStyle = color;
        ctx.beginPath();
        ctx.arc(pin.x, pin.y, 3, 0, Math.PI * 2);
        ctx.fill();

        // Label
        ctx.fillStyle = "rgba(255,255,255,0.7)";
        ctx.font = isPwm ? "bold 10px monospace" : "10px monospace";
        ctx.textAlign = "center";
        if (pin.side === "top") {
            ctx.fillText(pin.label, pin.x, pin.y - 16);
            if (isPwm) {
                ctx.fillStyle = "rgba(255,136,0,0.5)";
                ctx.font = "7px monospace";
                ctx.fillText("~", pin.x + 16, pin.y - 14);
            }
        } else {
            ctx.fillText(pin.label, pin.x, pin.y + 22);
        }
    }
}

// ═══════════════════════════════════════════════════════════════
//  COMPONENT RENDERING — Clean SVG, no chrome
// ═══════════════════════════════════════════════════════════════

function drawComponentClean(
    ctx: CanvasRenderingContext2D,
    comp: ReturnType<typeof buildComponentVisuals>[0],
    img: HTMLImageElement | null
) {
    const { x, y, width, height } = comp;
    const meta = getComponentMeta(comp.type);

    // Subtle shadow only
    ctx.shadowColor = `${meta.color}33`;
    ctx.shadowBlur = 12;
    ctx.shadowOffsetY = 3;

    // Light panel background (semi-transparent, no heavy gradient)
    ctx.fillStyle = "rgba(18,18,30,0.75)";
    roundRect(ctx, x - 2, y - 2, width + 4, height + 4, 8);
    ctx.fill();
    ctx.shadowColor = "transparent";
    ctx.shadowBlur = 0;

    // Thin subtle border
    ctx.strokeStyle = `${meta.color}44`;
    ctx.lineWidth = 1;
    roundRect(ctx, x - 2, y - 2, width + 4, height + 4, 8);
    ctx.stroke();

    if (img) {
        // Render Fritzing SVG at natural aspect ratio
        const svgAspect = img.naturalWidth / img.naturalHeight;
        const boxW = width - 6;
        const boxH = height - 22; // Room for label
        let drawW: number, drawH: number;

        if (boxW / boxH > svgAspect) {
            drawH = boxH;
            drawW = drawH * svgAspect;
        } else {
            drawW = boxW;
            drawH = drawW / svgAspect;
        }

        const drawX = x + (width - drawW) / 2;
        const drawY = y + 2;
        ctx.drawImage(img, drawX, drawY, drawW, drawH);
    } else {
        // Fallback: icon + colored rectangle
        const grad = ctx.createLinearGradient(x, y, x, y + height);
        grad.addColorStop(0, meta.gradient[0]);
        grad.addColorStop(1, meta.gradient[1]);
        ctx.fillStyle = grad;
        roundRect(ctx, x + 4, y + 4, width - 8, height - 26, 6);
        ctx.fill();

        ctx.font = "20px serif";
        ctx.textAlign = "center";
        ctx.fillText(meta.icon, x + width / 2, y + height / 2 - 6);
    }

    // Component name
    ctx.fillStyle = "#ffffff";
    ctx.font = "bold 10px 'Inter', 'Segoe UI', sans-serif";
    ctx.textAlign = "center";
    ctx.fillText(comp.displayName, x + width / 2, y + height - 2);

    // Pin connector dots
    for (const pin of comp.pins) {
        const pinColor = getWireColor(pin.label);

        ctx.fillStyle = "#111";
        ctx.beginPath();
        ctx.arc(pin.x, pin.y, 5, 0, Math.PI * 2);
        ctx.fill();

        ctx.strokeStyle = pinColor;
        ctx.lineWidth = 1.5;
        ctx.beginPath();
        ctx.arc(pin.x, pin.y, 5, 0, Math.PI * 2);
        ctx.stroke();

        ctx.fillStyle = pinColor;
        ctx.beginPath();
        ctx.arc(pin.x, pin.y, 2, 0, Math.PI * 2);
        ctx.fill();

        // Pin label
        ctx.fillStyle = "rgba(255,255,255,0.75)";
        ctx.font = "bold 8px monospace";
        ctx.textAlign = "right";
        ctx.fillText(pin.label, pin.x - 9, pin.y + 3);
    }
}

// ═══════════════════════════════════════════════════════════════
//  A* MANHATTAN WIRE ROUTING
// ═══════════════════════════════════════════════════════════════

const BB_X = BOARD_X;
const BB_Y = BOARD_Y + BOARD_HEIGHT + 50;
const BB_W = 560;
const BB_H = 80;
const RAIL_VCC_Y = BB_Y + 18;
const RAIL_GND_Y = BB_Y + BB_H - 18;

function drawRoutedWires(
    ctx: CanvasRenderingContext2D,
    grid: RoutingGrid,
    pinMapping: Record<string, string>,
    components: ReturnType<typeof buildComponentVisuals>,
    needsBreadboard: boolean
) {
    // Track wire groups going to same board pin for separation
    const pinGroups = new Map<string, number>();

    // If breadboard, draw power bus wires first
    if (needsBreadboard) {
        const vccPin = ARDUINO_UNO_PINS["5V"];
        const gndPin = ARDUINO_UNO_PINS["GND"];
        if (vccPin) drawManhattanWire(ctx, grid, vccPin.x, vccPin.y, BB_X + 40, RAIL_VCC_Y, "#FF4444", 3);
        if (gndPin) drawManhattanWire(ctx, grid, gndPin.x, gndPin.y, BB_X + 40, RAIL_GND_Y, "#4488FF", 3);
    }

    let vccTapIdx = 0;
    let gndTapIdx = 0;

    for (const [key, boardPinId] of Object.entries(pinMapping)) {
        const [instance, pinName] = key.split(".");
        const boardPin = ARDUINO_UNO_PINS[boardPinId];
        if (!boardPin) continue;

        const compVisual = components.find((c) => {
            const instType = instance.replace(/_\d+$/, "");
            return c.type === instType;
        });
        if (!compVisual) continue;
        const compPin = compVisual.pins.find((p) => p.label === pinName);
        if (!compPin) continue;

        const wireColor = getWireColor(pinName);
        const isPower = boardPinId === "5V" || boardPinId === "3V3";
        const isGround = boardPinId === "GND";

        // Get wire offset for separation
        const groupKey = boardPinId;
        const offset = pinGroups.get(groupKey) || 0;
        pinGroups.set(groupKey, offset + 1);

        // Wire thickness: power=3px, signal=2px
        const thickness = (isPower || isGround) ? 3 : 2;

        if (needsBreadboard && (isPower || isGround)) {
            // Route through breadboard rail
            const railY = isPower ? RAIL_VCC_Y : RAIL_GND_Y;
            const tapX = BB_X + 80 + (isPower ? vccTapIdx++ : gndTapIdx++) * 40;
            const color = isPower ? "#FF4444" : "#4488FF";
            drawManhattanWire(ctx, grid, tapX, railY, compPin.x, compPin.y, color, thickness);
            drawEndpoint(ctx, tapX, railY, color);
            drawEndpoint(ctx, compPin.x, compPin.y, color);
        } else {
            // Direct routed wire: Arduino pin → component pin
            drawManhattanWire(ctx, grid, boardPin.x, boardPin.y, compPin.x, compPin.y, wireColor, thickness, offset);
            drawEndpoint(ctx, boardPin.x, boardPin.y, wireColor);
            drawEndpoint(ctx, compPin.x, compPin.y, wireColor);
        }
    }
}

function drawManhattanWire(
    ctx: CanvasRenderingContext2D,
    grid: RoutingGrid,
    fromX: number, fromY: number,
    toX: number, toY: number,
    color: string, thickness: number,
    offsetIndex: number = 0
) {
    // Find path using A*
    let path = findPath(grid, fromX, fromY, toX, toY);

    // Apply separation offset for parallel wires
    if (offsetIndex > 0) {
        path = applyWireOffset(path, offsetIndex);
    }

    if (path.length < 2) return;

    // Subtle glow
    ctx.strokeStyle = color + "30";
    ctx.lineWidth = thickness + 4;
    ctx.lineCap = "round";
    ctx.lineJoin = "round";
    drawPathWithCorners(ctx, path, 5);
    ctx.stroke();

    // Core wire
    ctx.strokeStyle = color;
    ctx.lineWidth = thickness;
    ctx.lineCap = "round";
    ctx.lineJoin = "round";
    drawPathWithCorners(ctx, path, 5);
    ctx.stroke();
}

/** Draw a polyline path with rounded corners at each bend */
function drawPathWithCorners(ctx: CanvasRenderingContext2D, path: Point[], radius: number) {
    ctx.beginPath();
    ctx.moveTo(path[0].x, path[0].y);

    for (let i = 1; i < path.length; i++) {
        const curr = path[i];

        if (i < path.length - 1) {
            const next = path[i + 1];
            // Use arcTo for rounded corners
            ctx.arcTo(curr.x, curr.y, next.x, next.y, radius);
        } else {
            ctx.lineTo(curr.x, curr.y);
        }
    }
}

function drawEndpoint(ctx: CanvasRenderingContext2D, x: number, y: number, color: string) {
    ctx.fillStyle = color + "55";
    ctx.beginPath();
    ctx.arc(x, y, 5, 0, Math.PI * 2);
    ctx.fill();

    ctx.fillStyle = color;
    ctx.beginPath();
    ctx.arc(x, y, 3, 0, Math.PI * 2);
    ctx.fill();
}

// ═══════════════════════════════════════════════════════════════
//  BREADBOARD — Procedural (below Arduino)
// ═══════════════════════════════════════════════════════════════

function drawBreadboard(ctx: CanvasRenderingContext2D) {
    const bbGrad = ctx.createLinearGradient(BB_X, BB_Y, BB_X, BB_Y + BB_H);
    bbGrad.addColorStop(0, "#f5f0e8");
    bbGrad.addColorStop(0.5, "#eee8d8");
    bbGrad.addColorStop(1, "#e0d8c8");
    ctx.fillStyle = bbGrad;
    roundRect(ctx, BB_X, BB_Y, BB_W, BB_H, 5);
    ctx.fill();

    ctx.strokeStyle = "#ccc";
    ctx.lineWidth = 1;
    roundRect(ctx, BB_X, BB_Y, BB_W, BB_H, 5);
    ctx.stroke();

    // VCC rail
    ctx.fillStyle = "#FF444433";
    ctx.fillRect(BB_X + 12, RAIL_VCC_Y - 5, BB_W - 24, 10);
    ctx.strokeStyle = "#FF4444";
    ctx.lineWidth = 1;
    ctx.setLineDash([4, 4]);
    ctx.beginPath();
    ctx.moveTo(BB_X + 12, RAIL_VCC_Y);
    ctx.lineTo(BB_X + BB_W - 12, RAIL_VCC_Y);
    ctx.stroke();
    ctx.setLineDash([]);

    ctx.fillStyle = "#FF4444";
    ctx.font = "bold 11px monospace";
    ctx.textAlign = "left";
    ctx.fillText("+", BB_X + 3, RAIL_VCC_Y + 4);

    // GND rail
    ctx.fillStyle = "#4488FF33";
    ctx.fillRect(BB_X + 12, RAIL_GND_Y - 5, BB_W - 24, 10);
    ctx.strokeStyle = "#4488FF";
    ctx.lineWidth = 1;
    ctx.setLineDash([4, 4]);
    ctx.beginPath();
    ctx.moveTo(BB_X + 12, RAIL_GND_Y);
    ctx.lineTo(BB_X + BB_W - 12, RAIL_GND_Y);
    ctx.stroke();
    ctx.setLineDash([]);

    ctx.fillStyle = "#4488FF";
    ctx.font = "bold 11px monospace";
    ctx.textAlign = "left";
    ctx.fillText("−", BB_X + 3, RAIL_GND_Y + 4);

    // Pin holes
    for (let i = 0; i < 38; i++) {
        const hx = BB_X + 24 + i * 14;
        ctx.fillStyle = "#bbb";
        ctx.beginPath();
        ctx.arc(hx, RAIL_VCC_Y, 2.5, 0, Math.PI * 2);
        ctx.fill();
        ctx.beginPath();
        ctx.arc(hx, RAIL_GND_Y, 2.5, 0, Math.PI * 2);
        ctx.fill();
    }

    ctx.fillStyle = "#888";
    ctx.font = "bold 8px 'Inter', sans-serif";
    ctx.textAlign = "center";
    ctx.fillText("BREADBOARD · POWER RAILS", BB_X + BB_W / 2, BB_Y - 5);
}

// ═══════════════════════════════════════════════════════════════
//  LEGEND
// ═══════════════════════════════════════════════════════════════

function drawLegend(ctx: CanvasRenderingContext2D, componentCount: number) {
    const lx = 16;
    const ly = CANVAS_H - 48;

    ctx.fillStyle = "rgba(255,255,255,0.35)";
    ctx.font = "bold 10px 'Inter', sans-serif";
    ctx.textAlign = "left";
    ctx.fillText(`IoT Circuit Builder · ${componentCount} components · A* routed`, lx, ly);

    const legend = [
        { color: "#FF4444", label: "VCC" },
        { color: "#4488FF", label: "Signal" },
        { color: "#555555", label: "GND" },
        { color: "#FF8800", label: "PWM" },
        { color: "#00DD88", label: "SDA" },
        { color: "#7B68EE", label: "Echo" },
    ];

    legend.forEach((item, i) => {
        const ix = lx + i * 90;
        const iy = ly + 16;

        ctx.strokeStyle = item.color;
        ctx.lineWidth = 2.5;
        ctx.beginPath();
        ctx.moveTo(ix, iy);
        ctx.lineTo(ix + 24, iy);
        ctx.stroke();

        ctx.fillStyle = "rgba(255,255,255,0.45)";
        ctx.font = "9px 'Inter', sans-serif";
        ctx.fillText(item.label, ix + 28, iy + 3);
    });
}

// ═══════════════════════════════════════════════════════════════
//  UTILITY
// ═══════════════════════════════════════════════════════════════

function roundRect(ctx: CanvasRenderingContext2D, x: number, y: number, w: number, h: number, r: number) {
    ctx.beginPath();
    ctx.moveTo(x + r, y);
    ctx.lineTo(x + w - r, y);
    ctx.arcTo(x + w, y, x + w, y + r, r);
    ctx.lineTo(x + w, y + h - r);
    ctx.arcTo(x + w, y + h, x + w - r, y + h, r);
    ctx.lineTo(x + r, y + h);
    ctx.arcTo(x, y + h, x, y + h - r, r);
    ctx.lineTo(x, y + r);
    ctx.arcTo(x, y, x + r, y, r);
    ctx.closePath();
}
