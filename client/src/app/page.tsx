"use client";

import { useState } from "react";
import CircuitRenderer from "@/components/CircuitRenderer";
import { generateCircuit } from "@/lib/api";
import type { GenerateCircuitResponse } from "@/types/circuit";

export default function Home() {
  const [prompt, setPrompt] = useState("");
  const [loading, setLoading] = useState(false);
  const [result, setResult] = useState<GenerateCircuitResponse | null>(null);
  const [activeTab, setActiveTab] = useState<"circuit" | "code" | "mapping">(
    "circuit"
  );

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
        <div className="max-w-[1400px] mx-auto px-6 py-4 flex items-center justify-between">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-xl bg-gradient-to-br from-emerald-400 to-cyan-500 flex items-center justify-center text-lg font-bold shadow-lg shadow-emerald-500/20">
              ⚡
            </div>
            <div>
              <h1 className="text-lg font-bold bg-gradient-to-r from-emerald-400 to-cyan-400 bg-clip-text text-transparent">
                IoT Circuit Builder
              </h1>
              <p className="text-[11px] text-white/30 tracking-wider">
                AMD HACKATHON • AI-POWERED CIRCUIT DESIGN
              </p>
            </div>
          </div>
          <div className="flex items-center gap-2 text-xs text-white/40">
            <span className="inline-block w-2 h-2 rounded-full bg-emerald-400 animate-pulse"></span>
            Arduino Uno R3
          </div>
        </div>
      </header>

      <main className="max-w-[1400px] mx-auto px-6 py-8 space-y-6">
        {/* ─── Prompt Input ──────────────────────────────────── */}
        <section className="rounded-2xl border border-white/[0.06] bg-white/[0.02] backdrop-blur-xl p-6">
          <label className="block text-sm font-medium text-white/60 mb-3">
            Describe your IoT project
          </label>
          <div className="flex gap-3">
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
              className="self-end px-6 py-3 rounded-xl font-semibold text-sm bg-gradient-to-r from-emerald-500 to-cyan-500 hover:from-emerald-400 hover:to-cyan-400 disabled:opacity-30 disabled:cursor-not-allowed transition-all shadow-lg shadow-emerald-500/20 hover:shadow-emerald-500/40"
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
                className="px-3 py-1.5 rounded-lg text-[11px] bg-white/[0.04] border border-white/[0.06] text-white/40 hover:text-white/70 hover:bg-white/[0.08] transition-all"
              >
                {ex.length > 50 ? ex.slice(0, 50) + "..." : ex}
              </button>
            ))}
          </div>
        </section>

        {/* ─── Error Display ─────────────────────────────────── */}
        {result && !result.success && (
          <section className="rounded-2xl border border-red-500/20 bg-red-500/[0.05] p-6">
            <h3 className="text-red-400 font-semibold text-sm mb-2">
              Circuit Generation Failed
            </h3>
            <pre className="text-red-300/80 text-xs whitespace-pre-wrap font-mono leading-relaxed">
              {result.error}
            </pre>
          </section>
        )}

        {/* ─── Results ───────────────────────────────────────── */}
        {result?.success && (
          <>
            {/* Tabs */}
            <div className="flex gap-1 bg-white/[0.03] rounded-xl p-1 w-fit border border-white/[0.06]">
              {(
                [
                  { id: "circuit", label: "🔌 Circuit Diagram" },
                  { id: "code", label: "💻 Arduino Code" },
                  { id: "mapping", label: "📋 Pin Mapping" },
                ] as const
              ).map((tab) => (
                <button
                  key={tab.id}
                  onClick={() => setActiveTab(tab.id)}
                  className={`px-4 py-2 rounded-lg text-xs font-medium transition-all ${activeTab === tab.id
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
              <div className="rounded-xl border border-amber-500/30 bg-amber-500/[0.08] px-5 py-3 flex items-center gap-3">
                <span className="text-2xl">🔲</span>
                <div>
                  <p className="text-amber-300 font-semibold text-sm">
                    Breadboard Required
                  </p>
                  <p className="text-amber-200/60 text-xs">
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
                <div className="p-4">
                  <CircuitRenderer pinMapping={result.pinMapping} needsBreadboard={result.needsBreadboard} />
                </div>
              )}

              {/* Code Tab */}
              {activeTab === "code" && result.generatedCode && (
                <div className="relative">
                  <div className="absolute top-3 right-3 z-10">
                    <button
                      onClick={() =>
                        navigator.clipboard.writeText(result.generatedCode!)
                      }
                      className="px-3 py-1.5 rounded-lg text-[11px] bg-white/[0.08] border border-white/[0.1] text-white/50 hover:text-white/80 hover:bg-white/[0.12] transition-all"
                    >
                      📋 Copy
                    </button>
                  </div>
                  <pre className="p-6 text-sm font-mono text-emerald-300/90 leading-relaxed overflow-x-auto max-h-[600px] overflow-y-auto">
                    {result.generatedCode}
                  </pre>
                </div>
              )}

              {/* Mapping Tab */}
              {activeTab === "mapping" && result.pinMapping && (
                <div className="p-6">
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                    {Object.entries(result.pinMapping).map(
                      ([compPin, boardPin]) => (
                        <div
                          key={compPin}
                          className="flex items-center justify-between px-4 py-3 rounded-xl bg-white/[0.03] border border-white/[0.06]"
                        >
                          <span className="text-sm font-mono text-white/70">
                            {compPin}
                          </span>
                          <span className="text-xs text-white/30">→</span>
                          <span className="text-sm font-mono text-emerald-400 font-bold">
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
