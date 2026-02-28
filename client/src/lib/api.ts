import { GenerateCircuitRequest, GenerateCircuitResponse } from "@/types/circuit";

const API_BASE = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5050";

export async function generateCircuit(
    request: GenerateCircuitRequest
): Promise<GenerateCircuitResponse> {
    try {
        const response = await fetch(`${API_BASE}/api/circuit/generate`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(request),
        });

        const data: GenerateCircuitResponse = await response.json();
        return data;
    } catch (error) {
        return {
            success: false,
            error: `Failed to connect to API: ${error instanceof Error ? error.message : "Unknown error"}`,
        };
    }
}

export async function checkHealth(): Promise<boolean> {
    try {
        const response = await fetch(`${API_BASE}/api/circuit/health`);
        return response.ok;
    } catch {
        return false;
    }
}
