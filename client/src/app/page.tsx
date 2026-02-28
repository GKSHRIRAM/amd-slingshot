"use client";

import { useState, useRef } from "react";
import WokwiCircuit from "@/components/WokwiCircuit";
import { generateCircuit } from "@/lib/api";
import type { GenerateCircuitResponse } from "@/types/circuit";

export default function Home() {
  const [prompt, setPrompt] = useState("");
  const [loading, setLoading] = useState(false);
  const [result, setResult] = useState<GenerateCircuitResponse | null>(null);
  const [activeTab, setActiveTab] = useState<"circuit" | "code" | "mapping">(
    "circuit"
  );

  const handleExportWokwi = async () => {
    if (!result?.pinMapping || !result?.generatedCode) return;

    try {
      const JSZip = (await import("jszip")).default;
      const { saveAs } = (await import("file-saver")).default;

      const zip = new JSZip();
      zip.file("sketch.ino", result.generatedCode);

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
        battery_9v: "wokwi-battery-9v",
        dc_motor: "chip-generic_motor",
        l298n_motor_driver: "chip-l298n_driver",
        diode: "wokwi-diode",
        capacitor_ceramic: "wokwi-capacitor",
        capacitor_electrolytic: "wokwi-capacitor",
        breadboard: "wokwi-breadboard-half"
      };

      const instances = Array.from(new Set(Object.keys(result.pinMapping).map(k => k.split(".")[0])));
      // Also include target instances from component-to-component wiring that might not be source keys
      const allInstances = new Set(instances);
      Object.values(result.pinMapping).forEach(v => {
        if (v.includes(".")) allInstances.add(v.split(".")[0]);
      });

      const parts: any[] = [{ type: "wokwi-arduino-uno", id: "uno", top: 0, left: 0, attrs: {} }];

      Array.from(allInstances).forEach((inst, i) => {
        const type = inst.replace(/_\d+$/, "");
        const wokwiType = WOKWI_REGISTRY[type];
        if (wokwiType) {
          parts.push({
            type: wokwiType, id: inst, top: -250, left: 150 + (i * 120), attrs: {}
          });
        }
      });

      if (result.needsBreadboard) {
        parts.push({
          type: "wokwi-breadboard-half", id: "bb1", top: 200, left: 150, attrs: {}
        });
      }

      const connections: any[] = [];
      Object.entries(result.pinMapping).forEach(([compPin, boardPinRaw]) => {
        const [inst, pinName] = compPin.split(".");
        const type = inst.replace(/_\d+$/, "");
        if (!WOKWI_REGISTRY[type]) return;

        let targetInst = "uno";
        let targetPin = boardPinRaw;

        // Topological Mapping Check (Component to Component)
        if (boardPinRaw.includes(".")) {
          const partsArr = boardPinRaw.split(".");
          targetInst = partsArr[0];
          targetPin = partsArr[1];

          // Re-map the target pin for Wokwi internal names if necessary
          const tType = targetInst.replace(/_\d+$/, "");
          if (tType === "led_red") targetPin = targetPin === "ANODE" ? "A" : "C";
          if (tType === "resistor") targetPin = targetPin === "PIN1" ? "1" : "2";
          if (tType === "push_button") targetPin = targetPin === "SIGNAL" ? "1.l" : "2.l";
          if (tType === "dc_motor") targetPin = targetPin === "Term1" ? "Term1" : "Term2";
          if (tType === "diode") targetPin = targetPin === "ANODE" ? "A" : "C";
          if (tType === "l298n_motor_driver") targetPin = targetPin; // Native pins: 12V, GND, IN1, etc.
          if (tType === "battery_9v") targetPin = targetPin; // Native pins: VCC, GND
          if (tType.startsWith("capacitor")) targetPin = (targetPin === "PIN1" || targetPin === "ANODE") ? "1" : "2";
        } else {
          // Standard Arduino Mapping
          targetPin = boardPinRaw.startsWith("D") ? boardPinRaw.substring(1) : boardPinRaw;
          if (targetPin === "GND") targetPin = "GND.1";
          if (targetPin === "3V3") targetPin = "3.3V";
        }

        let wPin = pinName;
        if (type === "led_red") wPin = pinName === "ANODE" ? "A" : "C";
        if (type === "resistor") wPin = pinName === "PIN1" ? "1" : "2";
        if (type === "push_button") wPin = pinName === "SIGNAL" ? "1.l" : "2.l";
        if (type === "dc_motor") wPin = pinName === "Term1" ? "Term1" : "Term2";
        if (type === "diode") wPin = pinName === "ANODE" ? "A" : "C";
        if (type === "l298n_motor_driver") wPin = pinName; // Match SeedData pin names to Chip API
        if (type === "battery_9v") wPin = pinName; // Match SeedData pin names (VCC/GND)
        if (type.startsWith("capacitor")) wPin = (pinName === "PIN1" || pinName === "ANODE") ? "1" : "2";

        connections.push([
          `${targetInst}:${targetPin}`,
          `${inst}:${wPin}`,
          boardPinRaw.includes("5V") || boardPinRaw.includes("12V") || boardPinRaw.includes("VCC") ? "red" : boardPinRaw.includes("GND") ? "black" : "green",
          []
        ]);
      });

      // Wokwi Custom Chip API Injection
      if (allInstances.has("l298n_motor_driver_0")) {
        zip.file("l298n_driver.chip.json", JSON.stringify({
          name: "l298n_driver",
          pins: ["12V", "GND", "5V", "ENA", "IN1", "IN2", "IN3", "IN4", "ENB", "OUT1", "OUT2", "OUT3", "OUT4"]
        }, null, 2));
      }
      if (Array.from(allInstances).some(i => i.startsWith("dc_motor"))) {
        zip.file("generic_motor.chip.json", JSON.stringify({
          name: "generic_motor",
          pins: ["Term1", "Term2"]
        }, null, 2));
      }

      zip.file("diagram.json", JSON.stringify({ version: 1, author: "IoTBuilder", editor: "wokwi", parts, connections, dependencies: {} }, null, 2));

      const content = await zip.generateAsync({ type: "blob" });
      saveAs(content, "wokwi-simulation.zip");

    } catch (err) {
      console.error(err);
      alert("Failed to export Wokwi ZIP.");
    }
  };

  const handleGenerate = async () => {
    if (!prompt.trim()) return;
    setLoading(true);
    setResult(null);

    try {
      const response = await generateCircuit({ prompt });
      setResult(response);
      if (response.success) setActiveTab("circuit");
    } catch (err) {
      setResult({
        success: false,
        error: `Connection error: ${err instanceof Error ? err.message : "Unknown"}`,
      });
    } finally {
      setLoading(false);
    }
  };

  const examplePrompts = [
    "A line following robot with 2 IR sensors and 2 DC motors with L298N driver",
    "Robot car with ultrasonic sensor and servo for obstacle avoidance",
    "Simple LED blink project with 3 red LEDs",
  ];

  return (
    <div className="min-h-screen bg-gradient-to-br from-[#0a0a1a] via-[#0f0f2d] to-[#1a0a2e] text-white">
      {/* ─── Header ──────────────────────────────────────────── */}
      <header className="border-b border-white/5 backdrop-blur-xl bg-white/[0.02]">
        <div className="max-w-[1400px] mx-auto px-4 sm:px-6 py-4 flex items-center justify-between gap-4">
          <div className="flex items-center gap-2 sm:gap-3 min-w-0">
            <div className="w-8 h-8 sm:w-10 sm:h-10 rounded-xl bg-gradient-to-br from-emerald-400 to-cyan-500 flex items-center justify-center text-sm sm:text-lg font-bold shadow-lg shadow-emerald-500/20 flex-shrink-0">
              ⚡
            </div>
            <div className="min-w-0">
              <h1 className="text-sm sm:text-lg font-bold bg-gradient-to-r from-emerald-400 to-cyan-400 bg-clip-text text-transparent truncate">
                IoT Circuit Builder
              </h1>
              <p className="text-[10px] sm:text-[11px] text-white/30 tracking-wider truncate">
                AMD HACKATHON • AI-POWERED CIRCUIT
              </p>
            </div>
          </div>
          <div className="flex items-center gap-2 text-[10px] sm:text-xs text-white/40 flex-shrink-0">
            <span className="inline-block w-2 h-2 rounded-full bg-emerald-400 animate-pulse"></span>
            <span className="hidden sm:inline">Arduino Uno R3</span>
            <span className="sm:hidden">Arduino</span>
          </div>
        </div>
      </header>

      <main className="max-w-[1400px] mx-auto px-4 sm:px-6 py-6 sm:py-8 space-y-4 sm:space-y-6">
        {/* ─── Prompt Input ──────────────────────────────────── */}
        <section className="rounded-2xl border border-white/[0.06] bg-white/[0.02] backdrop-blur-xl p-4 sm:p-6">
          <label className="block text-sm font-medium text-white/60 mb-3">
            Describe your IoT project
          </label>
          <div className="flex flex-col sm:flex-row gap-3">
            <textarea
              value={prompt}
              onChange={(e) => setPrompt(e.target.value)}
              placeholder="e.g., A line following robot with 2 IR sensors and 2 DC motors..."
              className="flex-1 bg-white/[0.04] border border-white/[0.08] rounded-xl px-4 py-3 text-sm text-white placeholder-white/20 focus:outline-none focus:ring-2 focus:ring-emerald-500/40 focus:border-emerald-500/40 resize-none transition-all"
              rows={3}
              onKeyDown={(e) => {
                if (e.key === "Enter" && (e.metaKey || e.ctrlKey)) {
                  handleGenerate();
                }
              }}
            />
            <button
              onClick={handleGenerate}
              disabled={loading || !prompt.trim()}
              className="w-full sm:w-auto sm:self-end px-4 sm:px-6 py-3 rounded-xl font-semibold text-sm bg-gradient-to-r from-emerald-500 to-cyan-500 hover:from-emerald-400 hover:to-cyan-400 disabled:opacity-30 disabled:cursor-not-allowed transition-all shadow-lg shadow-emerald-500/20 hover:shadow-emerald-500/40"
            >
              {loading ? (
                <span className="flex items-center gap-2">
                  <svg className="w-4 h-4 animate-spin" viewBox="0 0 24 24">
                    <circle
                      className="opacity-25"
                      cx="12"
                      cy="12"
                      r="10"
                      stroke="currentColor"
                      strokeWidth="4"
                      fill="none"
                    />
                    <path
                      className="opacity-75"
                      fill="currentColor"
                      d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"
                    />
                  </svg>
                  Generating...
                </span>
              ) : (
                "Generate Circuit ⚡"
              )}
            </button>
          </div>

          {/* Example prompts */}
          <div className="flex gap-2 mt-3 flex-wrap">
            {examplePrompts.map((ex, i) => (
              <button
                key={i}
                onClick={() => setPrompt(ex)}
                className="px-2 sm:px-3 py-1.5 rounded-lg text-[10px] sm:text-[11px] bg-white/[0.04] border border-white/[0.06] text-white/40 hover:text-white/70 hover:bg-white/[0.08] transition-all truncate"
              >
                {ex.length > 40 ? ex.slice(0, 40) + "..." : ex}
              </button>
            ))}
          </div>
        </section>

        {/* ─── Error Display ─────────────────────────────────── */}
        {result && !result.success && (
          <section className="rounded-2xl border border-red-500/20 bg-red-500/[0.05] p-4 sm:p-6">
            <h3 className="text-red-400 font-semibold text-sm mb-2">
              Circuit Generation Failed
            </h3>
            <pre className="text-red-300/80 text-xs whitespace-pre-wrap font-mono leading-relaxed overflow-x-auto">
              {result.error}
            </pre>
          </section>
        )}

        {/* ─── Results ───────────────────────────────────────── */}
        {result?.success && (
          <>
            {/* Tabs - Responsive */}
            <div className="flex gap-1 bg-white/[0.03] rounded-xl p-1 overflow-x-auto border border-white/[0.06]">
              {(
                [
                  { id: "circuit", label: "🔌 Circuit" },
                  { id: "code", label: "💻 Code" },
                  { id: "mapping", label: "📋 Mapping" },
                ] as const
              ).map((tab) => (
                <button
                  key={tab.id}
                  onClick={() => setActiveTab(tab.id)}
                  className={`px-2 sm:px-4 py-2 rounded-lg text-xs sm:text-sm font-medium transition-all whitespace-nowrap ${
                    activeTab === tab.id
                      ? "bg-emerald-500/20 text-emerald-300 shadow-inner"
                      : "text-white/40 hover:text-white/60"
                  }`}
                >
                  {tab.label}
                </button>
              ))}
            </div>

            {/* Breadboard Required Banner */}
            {result.needsBreadboard && (
              <div className="rounded-xl border border-amber-500/30 bg-amber-500/[0.08] px-3 sm:px-5 py-3 flex flex-col sm:flex-row items-start sm:items-center gap-3">
                <span className="text-xl sm:text-2xl flex-shrink-0">🔲</span>
                <div>
                  <p className="text-amber-300 font-semibold text-sm">
                    Breadboard Required
                  </p>
                  <p className="text-amber-200/60 text-xs leading-relaxed">
                    Multiple components need 5V power. Route Arduino 5V →
                    breadboard + rail, then connect all VCC lines to the
                    breadboard.
                  </p>
                </div>
              </div>
            )}

            {/* Hardware Warnings */}
            {result.warnings && result.warnings.length > 0 && (
              <div className="space-y-2">
                {result.warnings.map((warning, i) => (
                  <div
                    key={i}
                    className="rounded-xl border border-amber-500/20 bg-amber-500/[0.05] px-5 py-3"
                  >
                    <pre className="text-amber-200/80 text-xs whitespace-pre-wrap font-mono leading-relaxed">
                      {warning}
                    </pre>
                  </div>
                ))}
              </div>
            )}

            {/* Tab Content */}
            <section className="rounded-2xl border border-white/[0.06] bg-white/[0.02] backdrop-blur-xl overflow-hidden">
              {/* Circuit Tab */}
              {activeTab === "circuit" && result.pinMapping && (
                <div className="p-3 sm:p-4">
                  <div className="flex flex-col sm:flex-row justify-end gap-2 sm:gap-3 mb-3 z-50 relative">
                    <button
                      onClick={handleExportWokwi}
                      className="flex items-center justify-center gap-2 px-3 sm:px-4 py-2 rounded-lg bg-blue-500/20 text-blue-400 border border-blue-500/30 hover:bg-blue-500/30 transition-colors text-xs sm:text-sm font-medium z-50 hover:shadow-lg shadow-blue-500/20"
                    >
                      📦 <span className="hidden sm:inline">Download ZIP</span><span className="sm:hidden">ZIP</span>
                    </button>
                  </div>
                  <WokwiCircuit pinMapping={result.pinMapping} needsBreadboard={result.needsBreadboard} />
                </div>
              )}

              {/* Code Tab */}
              {activeTab === "code" && result.generatedCode && (
                <div className="relative">
                  <div className="absolute top-2 right-2 sm:top-3 sm:right-3 z-10">
                    <button
                      onClick={() =>
                        navigator.clipboard.writeText(result.generatedCode!)
                      }
                      className="px-2 sm:px-3 py-1.5 rounded-lg text-[10px] sm:text-[11px] bg-white/[0.08] border border-white/[0.1] text-white/50 hover:text-white/80 hover:bg-white/[0.12] transition-all"
                    >
                      📋 Copy
                    </button>
                  </div>
                  <pre className="p-4 sm:p-6 text-xs sm:text-sm font-mono text-emerald-300/90 leading-relaxed overflow-x-auto max-h-[400px] sm:max-h-[600px] overflow-y-auto">
                    {result.generatedCode}
                  </pre>
                </div>
              )}

              {/* Mapping Tab */}
              {activeTab === "mapping" && result.pinMapping && (
                <div className="p-4 sm:p-6 bg-white rounded-xl">
                  <div className="grid grid-cols-1 sm:grid-cols-2 gap-2 sm:gap-3">
                    {Object.entries(result.pinMapping).map(
                      ([compPin, boardPin]) => (
                        <div
                          key={compPin}
                          className="flex items-center justify-between px-3 sm:px-4 py-2 sm:py-3 rounded-xl bg-gray-100 border border-gray-300 hover:bg-gray-50 transition-colors"
                        >
                          <span className="text-xs sm:text-sm font-mono text-gray-800 font-bold truncate">
                            {compPin}
                          </span>
                          <span className="text-xs text-gray-400 flex-shrink-0">→</span>
                          <span className="text-xs sm:text-sm font-mono text-green-600 font-bold truncate">
                            {boardPin}
                          </span>
                        </div>
                      )
                    )}
                  </div>
                </div>
              )}
            </section>

            {/* Components Used */}
            {result.componentsUsed && (
              <section className="flex gap-2 flex-wrap">
                {result.componentsUsed.map((comp, i) => (
                  <span
                    key={i}
                    className="px-3 py-1.5 rounded-lg text-[11px] bg-white/[0.04] border border-white/[0.06] text-white/50"
                  >
                    {comp}
                  </span>
                ))}
              </section>
            )}
          </>
        )}

        {/* ─── Empty State ───────────────────────────────────── */}
        {!result && !loading && (
          <section className="text-center py-20 text-white/20">
            <div className="text-6xl mb-4">🔧</div>
            <h2 className="text-xl font-semibold mb-2">
              Describe Your IoT Project
            </h2>
            <p className="text-sm max-w-md mx-auto">
              Enter a plain-language description of what you want to build.
              The AI will parse your intent, solve the electronics constraints,
              and generate working Arduino code.
            </p>
          </section>
        )}
      </main>

      {/* ─── Footer ──────────────────────────────────────────── */}
      <footer className="border-t border-white/5 mt-12 py-6 text-center text-xs text-white/20">
        IoT Circuit Builder • AMD Hackathon 2025 • Powered by Arduino Physics Engine
      </footer>
    </div>
  );
}
