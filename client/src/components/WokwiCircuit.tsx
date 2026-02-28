"use client";

import React, { useEffect, useState, useCallback } from "react";
import {
    ReactFlow,
    useNodesState,
    useEdgesState,
    Background,
    Controls,
    MiniMap,
    Node,
    Edge
} from '@xyflow/react';
import '@xyflow/react/dist/style.css';
import WokwiNode from "./WokwiNode";
import GenericNode from "./GenericNode";
import { getLayoutedElements } from "../lib/layoutEngine";

// Wokwi elements are client-side only custom elements
if (typeof window !== "undefined") {
    require("@wokwi/elements");
}

interface WokwiCircuitProps {
    pinMapping: Record<string, string>;
    needsBreadboard?: boolean;
}

const nodeTypes = {
    wokwiNode: WokwiNode,
    genericNode: GenericNode
};

const WOKWI_REGISTRY: Record<string, string> = {
    arduino_uno: "wokwi-arduino-uno",
    led_red: "wokwi-led",
    push_button: "wokwi-pushbutton",
    resistor: "wokwi-resistor",
    potentiometer: "wokwi-potentiometer",
    sg90_servo: "wokwi-servo",
    hc_sr04_ultrasonic: "wokwi-hc-sr04",
    buzzer: "wokwi-buzzer",
    oled_128x64: "wokwi-ssd1306",
    ssd1306: "wokwi-ssd1306",
    dht11: "wokwi-dht11",
    ldr_sensor: "wokwi-photoresistor-sensor",
    ir_sensor: "wokwi-ir-receiver",
    breadboard: "wokwi-breadboard-half"
};

export default function WokwiCircuit({ pinMapping, needsBreadboard }: WokwiCircuitProps) {
    const [nodes, setNodes, onNodesChange] = useNodesState<Node>([]);
    const [edges, setEdges, onEdgesChange] = useEdgesState<Edge>([]);
    const [mounted, setMounted] = useState(false);

    useEffect(() => {
        setMounted(true);
    }, []);

    useEffect(() => {
        if (!mounted) return;

        const initialNodes: Node[] = [];
        const initialEdges: Edge[] = [];
        const unoId = "uno_0";

        // Spawn Arduino
        initialNodes.push({
            id: unoId,
            type: 'wokwiNode',
            position: { x: 0, y: 0 },
            data: { type: 'wokwi-arduino-uno', label: 'ARDUINO UNO R3' }
        });

        // Spawn Breadboard if needed (positioned statically or let layout engine handle)
        if (needsBreadboard) {
            initialNodes.push({
                id: 'bb1',
                type: 'wokwiNode',
                position: { x: -300, y: 300 },
                data: { type: 'wokwi-breadboard-half', label: 'Power Rail Breadboard' },
            });
        }

        // Spawn Components
        const instanceSet = new Set<string>();
        const instancePins: Record<string, string[]> = {};

        for (const [key, value] of Object.entries(pinMapping)) {
            const [inst, pinName] = key.split(".");
            instanceSet.add(inst);
            if (!instancePins[inst]) instancePins[inst] = [];
            instancePins[inst].push(pinName);

            // Also check the target (value) for components
            if (value.includes(".")) {
                const [targetInst, targetPin] = value.split(".");
                instanceSet.add(targetInst);
                if (!instancePins[targetInst]) instancePins[targetInst] = [];
                if (!instancePins[targetInst].includes(targetPin)) {
                    instancePins[targetInst].push(targetPin);
                }
            }
        }

        Array.from(instanceSet).forEach(inst => {
            if (inst === unoId || inst === 'bb1') return; // Skip base parts

            const type = inst.replace(/_\d+$/, "");
            const wokwiType = WOKWI_REGISTRY[type];

            if (wokwiType) {
                initialNodes.push({
                    id: inst,
                    type: 'wokwiNode',
                    position: { x: 0, y: 0 },
                    data: { type: wokwiType, label: inst.replace("_", " #").toUpperCase() }
                });
            } else {
                initialNodes.push({
                    id: inst,
                    type: 'genericNode',
                    position: { x: 0, y: 0 },
                    data: { type: type, label: inst.replace("_", " #").toUpperCase(), pins: Array.from(new Set(instancePins[inst])) }
                });
            }
        });

        // Spawn Wires
        let edgeId = 0;
        Object.entries(pinMapping).forEach(([compPin, boardPinRaw]) => {
            const [inst, pinName] = compPin.split(".");
            const type = inst.replace(/_\d+$/, "");
            const wokwiType = WOKWI_REGISTRY[type];

            let cPin = pinName;
            if (wokwiType) {
                if (type === "led_red") cPin = pinName === "ANODE" ? "A" : "C";
                if (type === "resistor") cPin = pinName === "PIN1" ? "1" : "2";
                if (type === "push_button") cPin = pinName === "SIGNAL" ? "1.l" : "2.l";
                if (type === "dc_motor") cPin = pinName === "Term1" ? "Term1" : "Term2";
                if (type === "diode") cPin = pinName === "ANODE" ? "A" : "C";
                if (type === "l298n_motor_driver") cPin = pinName;
                if (type === "battery_9v") cPin = pinName;
                if (type.startsWith("capacitor")) cPin = (pinName === "PIN1" || pinName === "ANODE") ? "1" : "2";
            }

            let targetInst = unoId;
            let targetPin = boardPinRaw;

            // Dynamic Target Parsing (Component to Component wiring)
            if (boardPinRaw.includes(".")) {
                const parts = boardPinRaw.split(".");
                targetInst = parts[0];
                targetPin = parts[1];

                const tType = targetInst.replace(/_\d+$/, "");
                const tWokwiType = WOKWI_REGISTRY[tType];
                if (tWokwiType) {
                    if (tType === "led_red") targetPin = targetPin === "ANODE" ? "A" : "C";
                    if (tType === "resistor") targetPin = targetPin === "PIN1" ? "1" : "2";
                    if (tType === "push_button") targetPin = targetPin === "SIGNAL" ? "1.l" : "2.l";
                    if (tType === "dc_motor") targetPin = targetPin === "Term1" ? "Term1" : "Term2";
                    if (tType === "diode") targetPin = targetPin === "ANODE" ? "A" : "C";
                    if (tType === "l298n_motor_driver") targetPin = targetPin;
                    if (tType === "battery_9v") targetPin = targetPin;
                    if (tType.startsWith("capacitor")) targetPin = (targetPin === "PIN1" || targetPin === "ANODE") ? "1" : "2";
                }
            } else {
                // Formatting for Arduino Uno standard targets
                targetPin = targetPin.startsWith("D") ? targetPin.substring(1) : targetPin;
                if (targetPin === "GND") targetPin = "GND.1";
                if (targetPin === "3V3") targetPin = "3.3V";
            }

            initialEdges.push({
                id: `e${edgeId++}`,
                source: inst,
                sourceHandle: cPin,
                target: targetInst,
                targetHandle: targetPin,
                type: 'smoothstep',
                animated: boardPinRaw.includes("5V") || boardPinRaw.includes("3V3") || boardPinRaw.includes("12V"),
                style: { stroke: getWireColor(boardPinRaw), strokeWidth: 3 },
            });
        });

        // Run Layout Engine
        getLayoutedElements(initialNodes, initialEdges).then(({ layoutedNodes, layoutedEdges }) => {
            setNodes(layoutedNodes);
            setEdges(layoutedEdges);
        });

    }, [mounted, pinMapping, needsBreadboard]);

    if (!mounted) {
        return (
            <div className="w-full h-[800px] bg-white rounded-xl flex items-center justify-center border border-dashed border-gray-300">
                <div className="flex flex-col items-center gap-4">
                    <div className="w-8 h-8 border-4 border-emerald-500 border-t-transparent rounded-full animate-spin"></div>
                    <p className="text-sm text-gray-400 font-medium">Booting ELKjs Physics Engine...</p>
                </div>
            </div>
        );
    }

    return (
        <div className="w-full h-[400px] sm:h-[600px] md:h-[800px] border border-gray-200 rounded-xl overflow-hidden shadow-inner">
            <ReactFlow
                nodes={nodes}
                edges={edges}
                onNodesChange={onNodesChange}
                onEdgesChange={onEdgesChange}
                nodeTypes={nodeTypes}
                fitView
                fitViewOptions={{ padding: 0.2 }}
                minZoom={0.1}
                maxZoom={2}
                colorMode="light"
            >
                <Background color="#e2e8f0" gap={20} size={1.5} />
                <Controls showInteractive={false} />
            </ReactFlow>
        </div>
    );
}

function getWireColor(pinName: string): string {
    const upper = pinName.toUpperCase();
    if (upper === "VCC" || upper === "5V" || upper === "3V3") return "#ff4d4d";
    if (upper === "GND") return "#2d3436";
    if (upper === "SIGNAL" || upper.includes("PWM") || upper === "IN1" || upper === "IN2" || upper === "ENA") return "#fdcb6e";
    if (upper === "IN3" || upper === "IN4" || upper === "ENB") return "#0984e3";
    if (upper === "SDA") return "#00b894";
    if (upper === "SCL") return "#0984e3";
    if (upper === "TRIG") return "#e17055";
    if (upper === "ECHO") return "#6c5ce7";
    return "#a29bfe"; // Default soft purple
}
