export interface GenerateCircuitRequest {
    prompt: string;
    boardOverride?: string;
}

export interface GenerateCircuitResponse {
    success: boolean;
    pinMapping?: Record<string, string>;
    generatedCode?: string;
    componentsUsed?: string[];
    error?: string;
    needsBreadboard?: boolean;
    warnings?: string[];
}

export interface PinCoordinate {
    x: number;
    y: number;
    label: string;
    side: "left" | "right" | "top" | "bottom";
}

export interface ComponentVisual {
    instance: string;
    type: string;
    displayName: string;
    x: number;
    y: number;
    width: number;
    height: number;
    color: string;
    pins: PinCoordinate[];
}
