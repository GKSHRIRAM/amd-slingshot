"use client";

import { useRef, useEffect, useState } from "react";
import {
    ARDUINO_UNO_PINS,
    buildComponentVisuals,
    getWireColor,
} from "@/lib/fritzing";

interface CircuitRendererProps {
    pinMapping: Record<string, string>;
}

// ═══════════════════════════════════════════════════════════════
// Canvas-based Circuit Renderer (replaces Konva for SSR compat)
// Draws: Arduino board, components, wiring with Bezier curves
// ═══════════════════════════════════════════════════════════════

const BOARD_X = 80;
const BOARD_Y = 100;
const BOARD_WIDTH = 340;
const BOARD_HEIGHT = 220;

export default function CircuitRenderer({ pinMapping }: CircuitRendererProps) {
    const canvasRef = useRef<HTMLCanvasElement>(null);
    const [hoveredWire, setHoveredWire] = useState<string | null>(null);

    useEffect(() => {
        const canvas = canvasRef.current;
        if (!canvas) return;

        const ctx = canvas.getContext("2d");
        if (!ctx) return;

        const dpr = window.devicePixelRatio || 1;
        canvas.width = 900 * dpr;
        canvas.height = 500 * dpr;
        ctx.scale(dpr, dpr);

        // Clear
        ctx.clearRect(0, 0, 900, 500);

        // ─── Background ──────────────────────────────────────
        const bgGrad = ctx.createLinearGradient(0, 0, 900, 500);
        bgGrad.addColorStop(0, "#0f0f23");
        bgGrad.addColorStop(1, "#1a1a3e");
        ctx.fillStyle = bgGrad;
        ctx.fillRect(0, 0, 900, 500);

        // Grid
        ctx.strokeStyle = "rgba(255,255,255,0.03)";
        ctx.lineWidth = 1;
        for (let x = 0; x < 900; x += 20) {
            ctx.beginPath();
            ctx.moveTo(x, 0);
            ctx.lineTo(x, 500);
            ctx.stroke();
        }
        for (let y = 0; y < 500; y += 20) {
            ctx.beginPath();
            ctx.moveTo(0, y);
            ctx.lineTo(900, y);
            ctx.stroke();
        }

        // ─── Arduino Board ───────────────────────────────────
        // Shadow
        ctx.shadowColor = "rgba(0, 120, 215, 0.3)";
        ctx.shadowBlur = 20;
        ctx.shadowOffsetX = 0;
        ctx.shadowOffsetY = 4;

        // Board body
        const boardGrad = ctx.createLinearGradient(
            BOARD_X,
            BOARD_Y,
            BOARD_X,
            BOARD_Y + BOARD_HEIGHT
        );
        boardGrad.addColorStop(0, "#0d6e3a");
        boardGrad.addColorStop(1, "#094d29");
        ctx.fillStyle = boardGrad;
        roundRect(ctx, BOARD_X, BOARD_Y, BOARD_WIDTH, BOARD_HEIGHT, 8);
        ctx.fill();

        ctx.shadowColor = "transparent";
        ctx.shadowBlur = 0;

        // Board border
        ctx.strokeStyle = "#1db954";
        ctx.lineWidth = 2;
        roundRect(ctx, BOARD_X, BOARD_Y, BOARD_WIDTH, BOARD_HEIGHT, 8);
        ctx.stroke();

        // Board label
        ctx.fillStyle = "#ffffff";
        ctx.font = "bold 14px 'Inter', sans-serif";
        ctx.textAlign = "center";
        ctx.fillText("ARDUINO UNO R3", BOARD_X + BOARD_WIDTH / 2, BOARD_Y + 30);

        // USB port
        ctx.fillStyle = "#c0c0c0";
        roundRect(ctx, BOARD_X + 5, BOARD_Y + 80, 30, 50, 3);
        ctx.fill();
        ctx.fillStyle = "#666";
        ctx.font = "8px monospace";
        ctx.fillText("USB", BOARD_X + 20, BOARD_Y + 110);

        // Chip
        ctx.fillStyle = "#1a1a2e";
        roundRect(ctx, BOARD_X + 130, BOARD_Y + 70, 80, 80, 4);
        ctx.fill();
        ctx.strokeStyle = "#333";
        ctx.lineWidth = 1;
        roundRect(ctx, BOARD_X + 130, BOARD_Y + 70, 80, 80, 4);
        ctx.stroke();
        ctx.fillStyle = "#666";
        ctx.font = "9px monospace";
        ctx.textAlign = "center";
        ctx.fillText("ATmega328P", BOARD_X + 170, BOARD_Y + 115);

        // ─── Board Pin Headers ───────────────────────────────
        drawPinHeaders(ctx);

        // ─── Components ──────────────────────────────────────
        const components = buildComponentVisuals(pinMapping);
        for (const comp of components) {
            // Shadow
            ctx.shadowColor = `${comp.color}33`;
            ctx.shadowBlur = 12;

            // Component body
            ctx.fillStyle = comp.color;
            roundRect(ctx, comp.x, comp.y, comp.width, comp.height, 6);
            ctx.fill();

            ctx.shadowColor = "transparent";
            ctx.shadowBlur = 0;

            // Border
            ctx.strokeStyle = "rgba(255,255,255,0.2)";
            ctx.lineWidth = 1;
            roundRect(ctx, comp.x, comp.y, comp.width, comp.height, 6);
            ctx.stroke();

            // Label
            ctx.fillStyle = "#fff";
            ctx.font = "bold 10px 'Inter', sans-serif";
            ctx.textAlign = "center";
            ctx.fillText(
                comp.displayName,
                comp.x + comp.width / 2,
                comp.y + comp.height - 6
            );

            // Pin dots on component
            for (const pin of comp.pins) {
                ctx.fillStyle = "#fff";
                ctx.beginPath();
                ctx.arc(pin.x, pin.y, 3, 0, Math.PI * 2);
                ctx.fill();

                ctx.fillStyle = "rgba(255,255,255,0.7)";
                ctx.font = "8px monospace";
                ctx.textAlign = "right";
                ctx.fillText(pin.label, pin.x - 6, pin.y + 3);
            }
        }

        // ─── Wires (Bezier Curves) ───────────────────────────
        let compIdx = 0;
        const instanceMap = new Map<string, typeof components[0]>();
        for (const comp of components) {
            // Map instance name to visual
            const instanceName = Object.keys(pinMapping)
                .map((k) => k.split(".")[0])
                .find((k) => k.replace(/_\d+$/, "") === comp.type && !instanceMap.has(k));
            if (instanceName) instanceMap.set(instanceName, comp);
        }

        for (const [key, boardPinId] of Object.entries(pinMapping)) {
            const [instance, pinName] = key.split(".");
            const boardPin = ARDUINO_UNO_PINS[boardPinId];
            if (!boardPin) continue;

            // Find the component visual and its pin
            const compVisual = components.find((c) => {
                const instType = instance.replace(/_\d+$/, "");
                return c.type === instType;
            });
            if (!compVisual) continue;

            const compPin = compVisual.pins.find((p) => p.label === pinName);
            if (!compPin) continue;

            // Draw Bezier wire
            const wireColor = getWireColor(pinName);
            ctx.strokeStyle = wireColor;
            ctx.lineWidth = 2;
            ctx.globalAlpha = 0.8;
            ctx.setLineDash([]);

            const fromX = boardPin.x;
            const fromY = boardPin.y;
            const toX = compPin.x;
            const toY = compPin.y;

            // Control points for smooth curve
            const midX = (fromX + toX) / 2;
            const cp1x = midX;
            const cp1y = fromY;
            const cp2x = midX;
            const cp2y = toY;

            ctx.beginPath();
            ctx.moveTo(fromX, fromY);
            ctx.bezierCurveTo(cp1x, cp1y, cp2x, cp2y, toX, toY);
            ctx.stroke();

            ctx.globalAlpha = 1.0;

            // Small dot at connection points
            ctx.fillStyle = wireColor;
            ctx.beginPath();
            ctx.arc(fromX, fromY, 3, 0, Math.PI * 2);
            ctx.fill();
            ctx.beginPath();
            ctx.arc(toX, toY, 3, 0, Math.PI * 2);
            ctx.fill();

            compIdx++;
        }

        // ─── Legend ────────────────────────────────────────────
        const legendX = 20;
        const legendY = 420;
        ctx.fillStyle = "rgba(255,255,255,0.06)";
        roundRect(ctx, legendX, legendY, 200, 70, 6);
        ctx.fill();

        ctx.fillStyle = "#aaa";
        ctx.font = "bold 10px 'Inter', sans-serif";
        ctx.textAlign = "left";
        ctx.fillText("WIRE LEGEND", legendX + 10, legendY + 16);

        const legends = [
            { color: "#E63946", label: "Power (VCC/5V)" },
            { color: "#1D3557", label: "Ground (GND)" },
            { color: "#F4A261", label: "Signal (PWM/Enable)" },
            { color: "#2A9D8F", label: "Data (Digital/Analog)" },
        ];

        legends.forEach((l, i) => {
            const ly = legendY + 30 + i * 12;
            ctx.fillStyle = l.color;
            ctx.fillRect(legendX + 10, ly - 4, 12, 4);
            ctx.fillStyle = "#888";
            ctx.font = "9px 'Inter', sans-serif";
            ctx.fillText(l.label, legendX + 28, ly);
        });
    }, [pinMapping, hoveredWire]);

    return (
        <div className="relative rounded-xl overflow-hidden border border-white/10 bg-[#0f0f23]">
            <canvas
                ref={canvasRef}
                width={900}
                height={500}
                style={{ width: "100%", height: "auto" }}
                className="block"
            />
        </div>
    );
}

function roundRect(
    ctx: CanvasRenderingContext2D,
    x: number,
    y: number,
    w: number,
    h: number,
    r: number
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

function drawPinHeaders(ctx: CanvasRenderingContext2D) {
    // Draw all Arduino Uno pins as small squares with labels
    for (const [pinId, pin] of Object.entries(ARDUINO_UNO_PINS)) {
        // Pin square
        ctx.fillStyle = "#ffd700";
        ctx.fillRect(pin.x - 4, pin.y - 4, 8, 8);

        // Pin border
        ctx.strokeStyle = "#b8860b";
        ctx.lineWidth = 1;
        ctx.strokeRect(pin.x - 4, pin.y - 4, 8, 8);

        // Label
        ctx.fillStyle = "rgba(255,255,255,0.5)";
        ctx.font = "7px monospace";
        ctx.textAlign = "center";

        if (pin.side === "top") {
            ctx.fillText(pin.label, pin.x, pin.y - 10);
        } else {
            ctx.fillText(pin.label, pin.x, pin.y + 18);
        }
    }
}
