"use client";

import { useRef, useEffect, useState } from "react";
import {
    ARDUINO_UNO_PINS,
    BOARD_X,
    BOARD_Y,
    BOARD_WIDTH,
    BOARD_HEIGHT,
    buildComponentVisuals,
    getComponentMeta,
    getWireColor,
    getWireGlow,
} from "@/lib/fritzing";

interface CircuitRendererProps {
    pinMapping: Record<string, string>;
    needsBreadboard?: boolean;
}

// ═══════════════════════════════════════════════════════════════
// High-Quality Canvas Circuit Renderer
// Draws: Detailed Arduino board, gradient components with icons,
//        glowing Bezier wires, and proper pin routing
// ═══════════════════════════════════════════════════════════════

const CANVAS_W = 960;
const CANVAS_H = 640;

export default function CircuitRenderer({ pinMapping, needsBreadboard = false }: CircuitRendererProps) {
    const canvasRef = useRef<HTMLCanvasElement>(null);

    useEffect(() => {
        const canvas = canvasRef.current;
        if (!canvas) return;

        const ctx = canvas.getContext("2d");
        if (!ctx) return;

        const dpr = window.devicePixelRatio || 2;
        canvas.width = CANVAS_W * dpr;
        canvas.height = CANVAS_H * dpr;
        ctx.scale(dpr, dpr);

        // Enable smooth rendering
        ctx.imageSmoothingEnabled = true;
        ctx.imageSmoothingQuality = "high";

        // Clear
        ctx.clearRect(0, 0, CANVAS_W, CANVAS_H);

        // ─── Background ──────────────────────────────────────
        const bgGrad = ctx.createRadialGradient(
            CANVAS_W / 2, CANVAS_H / 2, 50,
            CANVAS_W / 2, CANVAS_H / 2, CANVAS_W
        );
        bgGrad.addColorStop(0, "#151530");
        bgGrad.addColorStop(1, "#0a0a1a");
        ctx.fillStyle = bgGrad;
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

        // ─── 1. DRAW WIRES FIRST (behind everything) ────────
        const components = buildComponentVisuals(pinMapping);
        drawWires(ctx, pinMapping, components, needsBreadboard);

        // ─── 2. DRAW BREADBOARD (if needed) ──────────────────
        if (needsBreadboard) {
            drawBreadboard(ctx);
        }

        // ─── 3. DRAW ARDUINO BOARD ───────────────────────────
        drawArduinoBoard(ctx);

        // ─── 3. DRAW COMPONENTS ──────────────────────────────
        for (const comp of components) {
            drawComponent(ctx, comp);
        }

        // ─── 4. DRAW LEGEND ──────────────────────────────────
        drawLegend(ctx);
    }, [pinMapping]);

    return (
        <div className="relative rounded-xl overflow-hidden border border-white/10 bg-[#0a0a1a]">
            <canvas
                ref={canvasRef}
                width={CANVAS_W}
                height={CANVAS_H}
                style={{ width: "100%", height: "auto" }}
                className="block"
            />
        </div>
    );
}

// ═══════════════════════════════════════════════════════════════
//  ARDUINO BOARD RENDERING
// ═══════════════════════════════════════════════════════════════

function drawArduinoBoard(ctx: CanvasRenderingContext2D) {
    // Board shadow
    ctx.shadowColor = "rgba(0, 180, 100, 0.25)";
    ctx.shadowBlur = 30;
    ctx.shadowOffsetX = 0;
    ctx.shadowOffsetY = 6;

    // PCB body gradient
    const pcbGrad = ctx.createLinearGradient(
        BOARD_X, BOARD_Y, BOARD_X, BOARD_Y + BOARD_HEIGHT
    );
    pcbGrad.addColorStop(0, "#0e7a42");
    pcbGrad.addColorStop(0.5, "#0a6636");
    pcbGrad.addColorStop(1, "#085028");
    ctx.fillStyle = pcbGrad;
    roundRect(ctx, BOARD_X, BOARD_Y, BOARD_WIDTH, BOARD_HEIGHT, 10);
    ctx.fill();

    ctx.shadowColor = "transparent";
    ctx.shadowBlur = 0;

    // PCB edge highlight
    ctx.strokeStyle = "#1db954";
    ctx.lineWidth = 2;
    roundRect(ctx, BOARD_X, BOARD_Y, BOARD_WIDTH, BOARD_HEIGHT, 10);
    ctx.stroke();

    // Inner solder mask texture (subtle lines)
    ctx.strokeStyle = "rgba(255,255,255,0.04)";
    ctx.lineWidth = 0.5;
    for (let i = 0; i < 15; i++) {
        const y = BOARD_Y + 20 + i * 16;
        ctx.beginPath();
        ctx.moveTo(BOARD_X + 10, y);
        ctx.lineTo(BOARD_X + BOARD_WIDTH - 10, y);
        ctx.stroke();
    }

    // ─── USB Port ────────────────────────────────────────
    const usbX = BOARD_X + 8;
    const usbY = BOARD_Y + 90;
    const usbGrad = ctx.createLinearGradient(usbX, usbY, usbX + 35, usbY);
    usbGrad.addColorStop(0, "#d0d0d0");
    usbGrad.addColorStop(0.5, "#e8e8e8");
    usbGrad.addColorStop(1, "#b0b0b0");
    ctx.fillStyle = usbGrad;
    roundRect(ctx, usbX, usbY, 35, 55, 3);
    ctx.fill();
    ctx.strokeStyle = "#888";
    ctx.lineWidth = 1;
    roundRect(ctx, usbX, usbY, 35, 55, 3);
    ctx.stroke();

    // USB inner port
    ctx.fillStyle = "#333";
    roundRect(ctx, usbX + 6, usbY + 8, 23, 39, 2);
    ctx.fill();

    ctx.fillStyle = "#999";
    ctx.font = "bold 7px monospace";
    ctx.textAlign = "center";
    ctx.fillText("USB", usbX + 17, usbY + 32);

    // ─── ATmega328P Chip ─────────────────────────────────
    const chipX = BOARD_X + 140;
    const chipY = BOARD_Y + 75;
    const chipW = 90;
    const chipH = 100;

    // Chip shadow
    ctx.fillStyle = "rgba(0,0,0,0.3)";
    roundRect(ctx, chipX + 3, chipY + 3, chipW, chipH, 4);
    ctx.fill();

    // Chip body
    const chipGrad = ctx.createLinearGradient(chipX, chipY, chipX + chipW, chipY + chipH);
    chipGrad.addColorStop(0, "#1a1a2e");
    chipGrad.addColorStop(1, "#111122");
    ctx.fillStyle = chipGrad;
    roundRect(ctx, chipX, chipY, chipW, chipH, 4);
    ctx.fill();

    // Chip border
    ctx.strokeStyle = "#444";
    ctx.lineWidth = 1;
    roundRect(ctx, chipX, chipY, chipW, chipH, 4);
    ctx.stroke();

    // Chip notch
    ctx.fillStyle = "#333";
    ctx.beginPath();
    ctx.arc(chipX + chipW / 2, chipY + 2, 5, 0, Math.PI);
    ctx.fill();

    // Chip legs (left and right)
    ctx.fillStyle = "#c0c0c0";
    for (let i = 0; i < 14; i++) {
        const ly = chipY + 8 + i * 6.5;
        // Left legs
        ctx.fillRect(chipX - 6, ly, 8, 2);
        // Right legs
        ctx.fillRect(chipX + chipW - 2, ly, 8, 2);
    }

    // Chip label
    ctx.fillStyle = "#888";
    ctx.font = "bold 8px 'Courier New', monospace";
    ctx.textAlign = "center";
    ctx.fillText("ATmega328P", chipX + chipW / 2, chipY + chipH / 2 - 4);
    ctx.fillStyle = "#666";
    ctx.font = "7px monospace";
    ctx.fillText("16MHz", chipX + chipW / 2, chipY + chipH / 2 + 8);

    // ─── Board Title ─────────────────────────────────────
    ctx.fillStyle = "#ffffff";
    ctx.font = "bold 15px 'Inter', 'Segoe UI', sans-serif";
    ctx.textAlign = "center";
    ctx.fillText("ARDUINO UNO R3", BOARD_X + BOARD_WIDTH / 2, BOARD_Y + 22);

    // Subtitle
    ctx.fillStyle = "rgba(255,255,255,0.3)";
    ctx.font = "8px monospace";
    ctx.fillText("ATmega328P • 5V • 16MHz", BOARD_X + BOARD_WIDTH / 2, BOARD_Y + 36);

    // ─── Power LED ───────────────────────────────────────
    ctx.fillStyle = "#00ff44";
    ctx.shadowColor = "#00ff44";
    ctx.shadowBlur = 8;
    ctx.beginPath();
    ctx.arc(BOARD_X + 55, BOARD_Y + BOARD_HEIGHT - 20, 3, 0, Math.PI * 2);
    ctx.fill();
    ctx.shadowBlur = 0;
    ctx.shadowColor = "transparent";

    ctx.fillStyle = "#666";
    ctx.font = "6px monospace";
    ctx.textAlign = "left";
    ctx.fillText("ON", BOARD_X + 61, BOARD_Y + BOARD_HEIGHT - 17);

    // ─── Reset Button ────────────────────────────────────
    ctx.fillStyle = "#cc3333";
    ctx.beginPath();
    ctx.arc(BOARD_X + BOARD_WIDTH - 30, BOARD_Y + 50, 6, 0, Math.PI * 2);
    ctx.fill();
    ctx.strokeStyle = "#992222";
    ctx.lineWidth = 1;
    ctx.beginPath();
    ctx.arc(BOARD_X + BOARD_WIDTH - 30, BOARD_Y + 50, 6, 0, Math.PI * 2);
    ctx.stroke();
    ctx.fillStyle = "#666";
    ctx.font = "5px monospace";
    ctx.textAlign = "center";
    ctx.fillText("RST", BOARD_X + BOARD_WIDTH - 30, BOARD_Y + 62);

    // ─── Pin Headers ─────────────────────────────────────
    drawPinHeaders(ctx);
}

function drawPinHeaders(ctx: CanvasRenderingContext2D) {
    const PWM_PINS = new Set(["D3", "D5", "D6", "D9", "D10", "D11"]);

    for (const [pinId, pin] of Object.entries(ARDUINO_UNO_PINS)) {
        // Pin hole background
        ctx.fillStyle = "#111";
        ctx.beginPath();
        ctx.arc(pin.x, pin.y, 5, 0, Math.PI * 2);
        ctx.fill();

        // Pin metallic ring
        const isPwm = PWM_PINS.has(pinId);
        const isPower = pinId === "5V" || pinId === "3V3";
        const isGnd = pinId === "GND";

        let ringColor = "#FFD700";
        if (isPwm) ringColor = "#FF8800";
        if (isPower) ringColor = "#FF4444";
        if (isGnd) ringColor = "#888888";

        ctx.strokeStyle = ringColor;
        ctx.lineWidth = 2;
        ctx.beginPath();
        ctx.arc(pin.x, pin.y, 5, 0, Math.PI * 2);
        ctx.stroke();

        // Center dot
        ctx.fillStyle = ringColor;
        ctx.beginPath();
        ctx.arc(pin.x, pin.y, 2, 0, Math.PI * 2);
        ctx.fill();

        // Label
        ctx.fillStyle = "rgba(255,255,255,0.55)";
        ctx.font = isPwm ? "bold 7px monospace" : "7px monospace";
        ctx.textAlign = "center";

        if (pin.side === "top") {
            ctx.fillText(pin.label, pin.x, pin.y - 12);
            if (isPwm) {
                ctx.fillStyle = "rgba(255,136,0,0.4)";
                ctx.font = "5px monospace";
                ctx.fillText("~", pin.x + 12, pin.y - 10);
            }
        } else {
            ctx.fillText(pin.label, pin.x, pin.y + 18);
        }
    }
}

// ═══════════════════════════════════════════════════════════════
//  COMPONENT RENDERING — Gradient bodies with icons
// ═══════════════════════════════════════════════════════════════

function drawComponent(
    ctx: CanvasRenderingContext2D,
    comp: ReturnType<typeof buildComponentVisuals>[0]
) {
    const meta = getComponentMeta(comp.type);
    const { x, y, width, height } = comp;

    // Drop shadow
    ctx.shadowColor = `${meta.color}44`;
    ctx.shadowBlur = 16;
    ctx.shadowOffsetX = 0;
    ctx.shadowOffsetY = 4;

    // Component body — gradient fill
    const bodyGrad = ctx.createLinearGradient(x, y, x, y + height);
    bodyGrad.addColorStop(0, meta.gradient[0]);
    bodyGrad.addColorStop(1, meta.gradient[1]);
    ctx.fillStyle = bodyGrad;
    roundRect(ctx, x, y, width, height, 8);
    ctx.fill();

    ctx.shadowColor = "transparent";
    ctx.shadowBlur = 0;

    // Highlight border (top-left lit)
    ctx.strokeStyle = `${meta.gradient[0]}88`;
    ctx.lineWidth = 1;
    roundRect(ctx, x, y, width, height, 8);
    ctx.stroke();

    // Outer border
    ctx.strokeStyle = "rgba(255,255,255,0.15)";
    ctx.lineWidth = 1;
    roundRect(ctx, x + 1, y + 1, width - 2, height - 2, 7);
    ctx.stroke();

    // Icon (top center)
    ctx.font = "16px serif";
    ctx.textAlign = "center";
    ctx.fillText(meta.icon, x + width / 2, y + 20);

    // Component name
    ctx.fillStyle = "#fff";
    ctx.font = "bold 9px 'Inter', 'Segoe UI', sans-serif";
    ctx.textAlign = "center";
    ctx.fillText(comp.displayName, x + width / 2, y + height - 8);

    // Category badge
    ctx.fillStyle = "rgba(0,0,0,0.3)";
    const catText = meta.category.toUpperCase();
    const catWidth = ctx.measureText(catText).width + 8;
    roundRect(ctx, x + width / 2 - catWidth / 2, y + height - 22, catWidth, 10, 3);
    ctx.fill();
    ctx.fillStyle = "rgba(255,255,255,0.5)";
    ctx.font = "5px monospace";
    ctx.fillText(catText, x + width / 2, y + height - 14);

    // Pin connector dots
    for (const pin of comp.pins) {
        // Pin hole
        ctx.fillStyle = "#222";
        ctx.beginPath();
        ctx.arc(pin.x, pin.y, 4, 0, Math.PI * 2);
        ctx.fill();

        // Pin ring
        const pinColor = getWireColor(pin.label);
        ctx.strokeStyle = pinColor;
        ctx.lineWidth = 1.5;
        ctx.beginPath();
        ctx.arc(pin.x, pin.y, 4, 0, Math.PI * 2);
        ctx.stroke();

        // Pin center
        ctx.fillStyle = pinColor;
        ctx.beginPath();
        ctx.arc(pin.x, pin.y, 1.5, 0, Math.PI * 2);
        ctx.fill();

        // Pin label
        ctx.fillStyle = "rgba(255,255,255,0.7)";
        ctx.font = "bold 7px monospace";
        ctx.textAlign = "right";
        ctx.fillText(pin.label, pin.x - 8, pin.y + 3);
    }
}

// ═══════════════════════════════════════════════════════════════
//  BREADBOARD RENDERING
// ═══════════════════════════════════════════════════════════════

const BB_X = BOARD_X + BOARD_WIDTH + 20;
const BB_Y = BOARD_Y + BOARD_HEIGHT + 40;
const BB_W = 400;
const BB_H = 60;

// Power rail positions
const RAIL_VCC_Y = BB_Y + 14;
const RAIL_GND_Y = BB_Y + BB_H - 14;

function drawBreadboard(ctx: CanvasRenderingContext2D) {
    // Breadboard body
    ctx.shadowColor = "rgba(200,200,200,0.1)";
    ctx.shadowBlur = 12;
    ctx.shadowOffsetY = 3;

    const bbGrad = ctx.createLinearGradient(BB_X, BB_Y, BB_X, BB_Y + BB_H);
    bbGrad.addColorStop(0, "#f5f0e8");
    bbGrad.addColorStop(0.5, "#eee8d8");
    bbGrad.addColorStop(1, "#e0d8c8");
    ctx.fillStyle = bbGrad;
    roundRect(ctx, BB_X, BB_Y, BB_W, BB_H, 5);
    ctx.fill();

    ctx.shadowColor = "transparent";
    ctx.shadowBlur = 0;

    ctx.strokeStyle = "#ccc";
    ctx.lineWidth = 1;
    roundRect(ctx, BB_X, BB_Y, BB_W, BB_H, 5);
    ctx.stroke();

    // Red power rail stripe
    ctx.fillStyle = "#FF444433";
    ctx.fillRect(BB_X + 10, RAIL_VCC_Y - 5, BB_W - 20, 10);
    ctx.strokeStyle = "#FF4444";
    ctx.lineWidth = 1;
    ctx.setLineDash([4, 4]);
    ctx.beginPath();
    ctx.moveTo(BB_X + 10, RAIL_VCC_Y);
    ctx.lineTo(BB_X + BB_W - 10, RAIL_VCC_Y);
    ctx.stroke();
    ctx.setLineDash([]);

    // "+" label
    ctx.fillStyle = "#FF4444";
    ctx.font = "bold 10px monospace";
    ctx.textAlign = "left";
    ctx.fillText("+", BB_X + 3, RAIL_VCC_Y + 4);

    // Blue ground rail stripe
    ctx.fillStyle = "#4488FF33";
    ctx.fillRect(BB_X + 10, RAIL_GND_Y - 5, BB_W - 20, 10);
    ctx.strokeStyle = "#4488FF";
    ctx.lineWidth = 1;
    ctx.setLineDash([4, 4]);
    ctx.beginPath();
    ctx.moveTo(BB_X + 10, RAIL_GND_Y);
    ctx.lineTo(BB_X + BB_W - 10, RAIL_GND_Y);
    ctx.stroke();
    ctx.setLineDash([]);

    // "-" label
    ctx.fillStyle = "#4488FF";
    ctx.font = "bold 10px monospace";
    ctx.textAlign = "left";
    ctx.fillText("−", BB_X + 3, RAIL_GND_Y + 4);

    // Pin holes on both rails
    for (let i = 0; i < 30; i++) {
        const hx = BB_X + 20 + i * 12;
        // VCC rail holes
        ctx.fillStyle = "#bbb";
        ctx.beginPath();
        ctx.arc(hx, RAIL_VCC_Y, 2, 0, Math.PI * 2);
        ctx.fill();
        // GND rail holes
        ctx.beginPath();
        ctx.arc(hx, RAIL_GND_Y, 2, 0, Math.PI * 2);
        ctx.fill();
    }

    // Label
    ctx.fillStyle = "#888";
    ctx.font = "bold 7px 'Inter', sans-serif";
    ctx.textAlign = "center";
    ctx.fillText("BREADBOARD — POWER RAILS", BB_X + BB_W / 2, BB_Y - 4);
}

// ═══════════════════════════════════════════════════════════════
//  WIRE RENDERING — Glowing Bezier curves with breadboard routing
// ═══════════════════════════════════════════════════════════════

function drawWires(
    ctx: CanvasRenderingContext2D,
    pinMapping: Record<string, string>,
    components: ReturnType<typeof buildComponentVisuals>,
    needsBreadboard: boolean
) {
    // If breadboard is needed, draw a single wire from Arduino 5V/GND to breadboard rails first
    if (needsBreadboard) {
        const vccPin = ARDUINO_UNO_PINS["5V"];
        const gndPin = ARDUINO_UNO_PINS["GND"];
        if (vccPin) {
            drawBezierWire(ctx, vccPin.x, vccPin.y, BB_X + 30, RAIL_VCC_Y, "#FF4444", "rgba(255,68,68,0.3)");
        }
        if (gndPin) {
            drawBezierWire(ctx, gndPin.x, gndPin.y, BB_X + 30, RAIL_GND_Y, "#4488FF", "rgba(68,136,255,0.3)");
        }
    }

    let vccTapIndex = 0;
    let gndTapIndex = 0;

    for (const [key, boardPinId] of Object.entries(pinMapping)) {
        const [instance, pinName] = key.split(".");
        const boardPin = ARDUINO_UNO_PINS[boardPinId];
        if (!boardPin) continue;

        // Find the component visual
        const compVisual = components.find((c) => {
            const instType = instance.replace(/_\d+$/, "");
            return c.type === instType;
        });
        if (!compVisual) continue;

        const compPin = compVisual.pins.find((p) => p.label === pinName);
        if (!compPin) continue;

        const wireColor = getWireColor(pinName);
        const glowColor = getWireGlow(pinName);

        const isPower = boardPinId === "5V" || boardPinId === "3V3";
        const isGround = boardPinId === "GND";

        if (needsBreadboard && (isPower || isGround)) {
            // Route through breadboard: rail → component
            const railY = isPower ? RAIL_VCC_Y : RAIL_GND_Y;
            const tapX = BB_X + 60 + (isPower ? vccTapIndex++ : gndTapIndex++) * 40;
            const color = isPower ? "#FF4444" : "#4488FF";
            const glow = isPower ? "rgba(255,68,68,0.3)" : "rgba(68,136,255,0.3)";

            drawBezierWire(ctx, tapX, railY, compPin.x, compPin.y, color, glow);
            drawSolderJoint(ctx, tapX, railY, color);
            drawSolderJoint(ctx, compPin.x, compPin.y, color);
        } else {
            // Direct wire: Arduino pin → component pin
            drawBezierWire(ctx, boardPin.x, boardPin.y, compPin.x, compPin.y, wireColor, glowColor);
            drawSolderJoint(ctx, boardPin.x, boardPin.y, wireColor);
            drawSolderJoint(ctx, compPin.x, compPin.y, wireColor);
        }
    }
}

function drawBezierWire(
    ctx: CanvasRenderingContext2D,
    fromX: number, fromY: number, toX: number, toY: number,
    wireColor: string, glowColor: string
) {
    const dx = Math.abs(toX - fromX);
    const cp1x = fromX + dx * 0.4;
    const cp1y = fromY;
    const cp2x = toX - dx * 0.4;
    const cp2y = toY;

    // Outer glow
    ctx.strokeStyle = glowColor;
    ctx.lineWidth = 6;
    ctx.lineCap = "round";
    ctx.beginPath();
    ctx.moveTo(fromX, fromY);
    ctx.bezierCurveTo(cp1x, cp1y, cp2x, cp2y, toX, toY);
    ctx.stroke();

    // Core wire
    ctx.strokeStyle = wireColor;
    ctx.lineWidth = 2.5;
    ctx.lineCap = "round";
    ctx.beginPath();
    ctx.moveTo(fromX, fromY);
    ctx.bezierCurveTo(cp1x, cp1y, cp2x, cp2y, toX, toY);
    ctx.stroke();

    // Inner highlight
    ctx.strokeStyle = `${wireColor}66`;
    ctx.lineWidth = 1;
    ctx.beginPath();
    ctx.moveTo(fromX, fromY);
    ctx.bezierCurveTo(cp1x, cp1y, cp2x, cp2y, toX, toY);
    ctx.stroke();
}

function drawSolderJoint(ctx: CanvasRenderingContext2D, x: number, y: number, color: string) {
    // Glow
    ctx.fillStyle = color + "44";
    ctx.beginPath();
    ctx.arc(x, y, 5, 0, Math.PI * 2);
    ctx.fill();

    // Ring
    ctx.fillStyle = color;
    ctx.beginPath();
    ctx.arc(x, y, 3, 0, Math.PI * 2);
    ctx.fill();

    // Highlight
    ctx.fillStyle = "#ffffff88";
    ctx.beginPath();
    ctx.arc(x - 1, y - 1, 1, 0, Math.PI * 2);
    ctx.fill();
}

// ═══════════════════════════════════════════════════════════════
//  LEGEND
// ═══════════════════════════════════════════════════════════════

function drawLegend(ctx: CanvasRenderingContext2D) {
    const legendX = 16;
    const legendY = CANVAS_H - 95;

    // Legend background
    ctx.fillStyle = "rgba(255,255,255,0.04)";
    roundRect(ctx, legendX, legendY, 180, 85, 8);
    ctx.fill();
    ctx.strokeStyle = "rgba(255,255,255,0.06)";
    ctx.lineWidth = 1;
    roundRect(ctx, legendX, legendY, 180, 85, 8);
    ctx.stroke();

    ctx.fillStyle = "#999";
    ctx.font = "bold 9px 'Inter', sans-serif";
    ctx.textAlign = "left";
    ctx.fillText("WIRE LEGEND", legendX + 12, legendY + 16);

    const legends = [
        { color: "#FF4444", label: "Power (VCC / 5V)" },
        { color: "#555555", label: "Ground (GND)" },
        { color: "#FF8800", label: "Signal (PWM / Enable)" },
        { color: "#00DD88", label: "I2C Data (SDA)" },
        { color: "#44AAFF", label: "Digital / Analog" },
    ];

    legends.forEach((l, i) => {
        const ly = legendY + 30 + i * 12;
        // Wire sample
        ctx.strokeStyle = l.color;
        ctx.lineWidth = 3;
        ctx.lineCap = "round";
        ctx.beginPath();
        ctx.moveTo(legendX + 12, ly - 2);
        ctx.lineTo(legendX + 28, ly - 2);
        ctx.stroke();

        // Label
        ctx.fillStyle = "#888";
        ctx.font = "8px 'Inter', sans-serif";
        ctx.fillText(l.label, legendX + 34, ly);
    });
}

// ═══════════════════════════════════════════════════════════════
//  UTILITY
// ═══════════════════════════════════════════════════════════════

function roundRect(
    ctx: CanvasRenderingContext2D,
    x: number, y: number, w: number, h: number, r: number
) {
    ctx.beginPath();
    ctx.moveTo(x + r, y);
    ctx.lineTo(x + w - r, y);
    ctx.quadraticCurveTo(x + w, y, x + w, y + r);
    ctx.lineTo(x + w, y + h - r);
    ctx.quadraticCurveTo(x + w, y + h, x + w - r, y + h);
    ctx.lineTo(x + r, y + h);
    ctx.quadraticCurveTo(x, y + h, x, y + h - r);
    ctx.lineTo(x, y + r);
    ctx.quadraticCurveTo(x, y, x + r, y);
    ctx.closePath();
}
