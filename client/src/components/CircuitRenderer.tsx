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
    buildArduinoPinsFromSvg,
    setArduinoPins,
} from "@/lib/fritzing";
import { parseSvgConnectors, type SvgParseResult } from "@/lib/svgConnectorParser";
import { OrthogonalRouter, type Point } from "@/lib/wireRouter";

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

            // Collect all component types
            const instanceSet = new Set<string>();
            for (const key of Object.keys(pinMapping)) {
                instanceSet.add(key.split(".")[0]);
            }
            const componentTypes = Array.from(new Set(
                Array.from(instanceSet).map(inst => inst.replace(/_\d+$/, ""))
            ));
            const allTypes = ["arduino_uno", ...componentTypes];
            if (needsBreadboard) {
                allTypes.push("breadboard_full");
            }

            // Parse SVGs for connector data + preload images — in parallel
            const parsePromise = Promise.all(
                allTypes.map(async (type) => {
                    const result = await parseSvgConnectors(`/assets/components/${type}.svg`);
                    return [type, result] as [string, SvgParseResult | null];
                })
            ).then(entries => {
                const map = new Map<string, SvgParseResult>();
                for (const [type, result] of entries) {
                    if (result) map.set(type, result);
                }
                return map;
            });

            const imagePromise = preloadComponentImages(allTypes);

            Promise.all([parsePromise, imagePromise]).then(([svgParsedMap, imageMap]) => {
                // ─── Build component visuals with SVG connector data ──
                const components = buildComponentVisuals(pinMapping, svgParsedMap);

                // Force high-resolution internally (at least 3x scale) for crisp exports
                const dpr = Math.max(window.devicePixelRatio || 1, 3);
                canvas.width = CANVAS_W * dpr;
                canvas.height = CANVAS_H * dpr;
                ctx.scale(dpr, dpr);
                ctx.imageSmoothingEnabled = true;
                ctx.imageSmoothingQuality = "high";
                ctx.clearRect(0, 0, CANVAS_W, CANVAS_H);

                // ─── Background ─────────────────────────────
                drawBackground(ctx);

                // Initialize the professional Orthogonal A* Router
                const router = new OrthogonalRouter(CANVAS_W, CANVAS_H);

                // Block the Arduino as an unwalkable obstacle
                const obstacles = [
                    { x: BOARD_X, y: BOARD_Y, width: BOARD_WIDTH, height: BOARD_HEIGHT },
                    ...components.map(c => ({ x: c.x, y: c.y, width: c.width, height: c.height }))
                ];
                router.blockObstacles(obstacles, 15);

                // Unblock the exact pins we need to route to
                for (const [key, boardPinId] of Object.entries(pinMapping)) {
                    const boardPin = ARDUINO_UNO_PINS[boardPinId];
                    if (boardPin) router.unblockPin(boardPin.x, boardPin.y);

                    const [instance, pinName] = key.split(".");
                    const compVisual = components.find((c) => c.instance === instance);
                    if (!compVisual) continue;
                    const compPin = compVisual.pins.find((p) => p.label === pinName);
                    if (compPin) router.unblockPin(compPin.x, compPin.y);
                }

                if (needsBreadboard) {
                    const bbPoly = { x: BB_X, y: BB_Y, width: BB_W, height: BB_H };
                    router.blockObstacles([bbPoly], 5);
                }

                // ─── 1. ROUTE & DRAW WIRES (behind everything) ──
                drawManhattanWires(ctx, pinMapping, components, needsBreadboard, router);

                // ─── 2. DRAW BREADBOARD (if needed) ─────────
                if (needsBreadboard) {
                    const bbImg = imageMap.get("breadboard_full");
                    if (bbImg) {
                        drawBreadboardSVG(ctx, bbImg);
                    } else {
                        drawBreadboard(ctx);
                    }
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
            <div className="relative rounded-xl overflow-hidden border border-black/10 bg-white">
                <canvas
                    ref={canvasRef}
                    width={CANVAS_W}
                    height={CANVAS_H}
                    style={{
                        width: "100%",
                        height: "auto",
                        imageRendering: "auto",
                        WebkitFontSmoothing: "antialiased",
                        MozOsxFontSmoothing: "grayscale"
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
//  BACKGROUND (Fritzing Light Style)
// ═══════════════════════════════════════════════════════════════

function drawBackground(ctx: CanvasRenderingContext2D) {
    // Light neutral background
    ctx.fillStyle = "#F8F9FA";
    ctx.fillRect(0, 0, CANVAS_W, CANVAS_H);

    // Subtle grid pattern
    ctx.strokeStyle = "#E8EAED";
    ctx.lineWidth = 1;

    for (let x = 0; x < CANVAS_W; x += 40) {
        ctx.beginPath();
        ctx.moveTo(x, 0);
        ctx.lineTo(x, CANVAS_H);
        ctx.stroke();
    }

    for (let y = 0; y < CANVAS_H; y += 40) {
        ctx.beginPath();
        ctx.moveTo(0, y);
        ctx.lineTo(CANVAS_W, y);
        ctx.stroke();
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
        ctx.fillStyle = "rgba(0,0,0,0.8)";
        ctx.font = isPwm ? "bold 10px monospace" : "10px monospace";
        ctx.textAlign = "center";
        if (pin.side === "top") {
            ctx.fillText(pin.label, pin.x, pin.y - 16);
            if (isPwm) {
                ctx.fillStyle = "rgba(255,136,0,0.8)";
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

    // Component name label with background to prevent wire clipping
    const labelY = y + height + 16;
    ctx.font = "bold 11px 'Inter', 'Segoe UI', sans-serif";
    const textMetrics = ctx.measureText(comp.displayName);
    const labelW = textMetrics.width + 12;
    const labelH = 18;

    ctx.fillStyle = "rgba(255, 255, 255, 0.9)";
    ctx.shadowColor = "rgba(0,0,0,0.1)";
    ctx.shadowBlur = 4;
    roundRect(ctx, x + width / 2 - labelW / 2, labelY - 12, labelW, labelH, 4);
    ctx.fill();
    ctx.shadowColor = "transparent";

    ctx.fillStyle = "#222222";
    ctx.textAlign = "center";
    ctx.fillText(comp.displayName, x + width / 2, labelY);

    if (img) {
        // Draw the SVG standalone with no artificial background or borders
        // (Just like standard Fritzing)
        const svgAspect = img.naturalWidth / img.naturalHeight;
        const boxW = width;
        const boxH = height;
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
        ctx.fillStyle = "#ffffff";
        ctx.fillText(meta.icon, x + width / 2, y + height / 2 + 6);
    }

    // Pin connector dots
    for (const pin of comp.pins) {
        const pinColor = getWireColor(pin.label);

        // Pin connector (Fritzing style square boot / header)
        ctx.fillStyle = "#333333";
        ctx.fillRect(pin.x - 4, pin.y - 4, 8, 8);

        ctx.fillStyle = pinColor;
        ctx.fillRect(pin.x - 2, pin.y - 2, 4, 4);

        // Pin label
        ctx.fillStyle = "rgba(0,0,0,0.85)";
        ctx.font = "bold 9px monospace";
        ctx.textAlign = "right";
        ctx.fillText(pin.label, pin.x - 8, pin.y + 3);
    }
}

// ═══════════════════════════════════════════════════════════════
//  WIRE ROUTING - Orthogonal A* Pathfinding
// ═══════════════════════════════════════════════════════════════

const BB_X = BOARD_X;
const BB_Y = BOARD_Y + BOARD_HEIGHT + 50;
const BB_W = 560;
const BB_H = 80;
const RAIL_VCC_Y = BB_Y + 18;
const RAIL_GND_Y = BB_Y + BB_H - 18;

function drawManhattanWires(
    ctx: CanvasRenderingContext2D,
    pinMapping: Record<string, string>,
    components: ReturnType<typeof buildComponentVisuals>,
    needsBreadboard: boolean,
    router: OrthogonalRouter
) {
    let vccTapIdx = 0;
    let gndTapIdx = 0;

    for (const [key, boardPinId] of Object.entries(pinMapping)) {
        const [instance, pinName] = key.split(".");
        const boardPin = ARDUINO_UNO_PINS[boardPinId];
        if (!boardPin) continue;

        const compVisual = components.find((c) => c.instance === instance);
        if (!compVisual) continue;
        const compPin = compVisual.pins.find((p) => p.label === pinName);
        if (!compPin) continue;

        const wireColor = getWireColor(pinName);
        const isPower = boardPinId === "5V" || boardPinId === "3V3";
        const isGround = boardPinId === "GND";
        const thickness = (isPower || isGround) ? 3.5 : 2.5;

        let fromX: number, fromY: number;

        if (needsBreadboard && (isPower || isGround)) {
            fromY = isPower ? RAIL_VCC_Y : RAIL_GND_Y;
            fromX = BB_X + 80 + (isPower ? vccTapIdx++ : gndTapIdx++) * 50;
            const color = isPower ? "#FF4444" : "#4488FF";

            router.unblockPin(fromX, fromY);

            drawAStarJumper(ctx, fromX, fromY, compPin.x, compPin.y, color, thickness, router);
            drawEndpoint(ctx, fromX, fromY, color);
            drawEndpoint(ctx, compPin.x, compPin.y, color);
        } else {
            fromX = boardPin.x;
            fromY = boardPin.y;

            drawAStarJumper(ctx, fromX, fromY, compPin.x, compPin.y, wireColor, thickness, router);

            drawEndpoint(ctx, fromX, fromY, wireColor);
            drawEndpoint(ctx, compPin.x, compPin.y, wireColor);
        }
    }
}

function drawAStarJumper(
    ctx: CanvasRenderingContext2D,
    fromX: number, fromY: number,
    toX: number, toY: number,
    color: string, thickness: number,
    router: OrthogonalRouter
) {
    const path = router.routeWire(fromX, fromY, toX, toY);

    // Border (Dark silhouette underneath wire)
    ctx.strokeStyle = "#222222";
    ctx.lineWidth = thickness + 3;
    ctx.lineCap = "round";
    ctx.lineJoin = "round";
    drawPathWithCorners(ctx, path, 6);
    ctx.stroke();

    // Core Solid Wire Color
    ctx.strokeStyle = color;
    ctx.lineWidth = thickness;
    ctx.lineCap = "round";
    ctx.lineJoin = "round";
    drawPathWithCorners(ctx, path, 6);
    ctx.stroke();
}

/** Draw a polyline path with rounded corners at each bend */
function drawPathWithCorners(ctx: CanvasRenderingContext2D, path: Point[], radius: number) {
    if (path.length === 0) return;
    ctx.beginPath();
    ctx.moveTo(path[0].x, path[0].y);

    for (let i = 1; i < path.length; i++) {
        const curr = path[i];

        if (i < path.length - 1) {
            const next = path[i + 1];
            // Use arcTo for rounded corners so it looks professional
            ctx.arcTo(curr.x, curr.y, next.x, next.y, radius);
        } else {
            ctx.lineTo(curr.x, curr.y);
        }
    }
}

function drawEndpoint(ctx: CanvasRenderingContext2D, x: number, y: number, color: string) {
    // Outer glow for connected pins
    ctx.beginPath();
    ctx.arc(x, y, 6, 0, Math.PI * 2);
    ctx.fillStyle = color;
    ctx.globalAlpha = 0.3;
    ctx.fill();
    ctx.globalAlpha = 1.0;

    // Inner dot
    ctx.beginPath();
    ctx.arc(x, y, 3.5, 0, Math.PI * 2);
    ctx.fillStyle = color;
    ctx.fill();

    // Tiny white highlight for reflection
    ctx.beginPath();
    ctx.arc(x - 1, y - 1, 1.5, 0, Math.PI * 2);
    ctx.fillStyle = "#FFFFFF";
    ctx.fill();
}

// ═══════════════════════════════════════════════════════════════
//  BREADBOARD
// ═══════════════════════════════════════════════════════════════

function drawBreadboardSVG(ctx: CanvasRenderingContext2D, img: HTMLImageElement) {
    ctx.shadowColor = "rgba(0,0,0,0.15)";
    ctx.shadowBlur = 15;
    ctx.shadowOffsetY = 4;

    // Scale SVG into the procedural BB footprint
    ctx.drawImage(img, BB_X, BB_Y - 10, BB_W, BB_H + 20);

    ctx.shadowColor = "transparent";
    ctx.shadowBlur = 0;
}

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
        ctx.fillStyle = "#444"; // Darker realistic holes
        ctx.beginPath();
        ctx.arc(hx, RAIL_VCC_Y, 2.5, 0, Math.PI * 2);
        ctx.fill();
        ctx.beginPath();
        ctx.arc(hx, RAIL_GND_Y, 2.5, 0, Math.PI * 2);
        ctx.fill();
    }

    ctx.fillStyle = "#555";
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

    ctx.fillStyle = "rgba(0,0,0,0.6)";
    ctx.font = "bold 10px 'Inter', sans-serif";
    ctx.textAlign = "left";
    ctx.fillText(`IoT Circuit Builder · ${componentCount} components · A* strict parallel`, lx, ly);

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

        ctx.fillStyle = "rgba(0,0,0,0.8)";
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
