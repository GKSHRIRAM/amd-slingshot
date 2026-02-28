import React, { useEffect, useRef, useState } from 'react';
import { Handle, Position, useUpdateNodeInternals, useNodeId } from '@xyflow/react';

interface WokwiNodeProps {
    data: {
        type: string;
        label: string;
    };
}

export default function WokwiNode({ data }: WokwiNodeProps) {
    const ref = useRef<HTMLDivElement>(null);
    const [pins, setPins] = useState<{ id: string; x: number; y: number }[]>([]);
    const updateNodeInternals = useUpdateNodeInternals();
    const nodeId = useNodeId();

    useEffect(() => {
        let attempts = 0;
        const interval = setInterval(() => {
            if (ref.current) {
                const comp = ref.current.firstElementChild as any;
                if (comp && comp.pinInfo && comp.pinInfo.length > 0) {
                    // Normalize the wokwi element pins into standard React Flow handles
                    setPins(
                        comp.pinInfo.map((p: any) => ({
                            id: p.name,
                            x: p.x,
                            y: p.y,
                        }))
                    );
                    clearInterval(interval);

                    // Critical: Tell React Flow the handles have physically mounted so wires can connect
                    if (nodeId) {
                        setTimeout(() => updateNodeInternals(nodeId), 50);
                    }
                }
            }
            attempts++;
            if (attempts > 50) clearInterval(interval); // Timeout after 1000ms
        }, 20);

        return () => clearInterval(interval);
    }, [data.type, nodeId, updateNodeInternals]);

    const Tag = data.type as any;

    return (
        <div ref={ref} className="relative drop-shadow-2xl bg-transparent">
            {/* 1. Hardware Visage */}
            <Tag className="block pointer-events-none" />

            {/* 2. Mathematical React Flow Pins */}
            {pins.map((p) => {
                // If the pin is physically located below the centerline of the component, 
                // force the wire to exit towards the bottom to prevent it crossing the component hull.
                const optimalPosition = p.y > 40 ? Position.Bottom : Position.Top;

                return (
                    <React.Fragment key={p.id}>
                        <Handle
                            type="source"
                            position={optimalPosition}
                            id={p.id}
                            style={{
                                left: p.x,
                                top: p.y,
                                background: 'transparent',
                                width: 2,
                                height: 2,
                                border: 'none',
                                transform: 'translate(-50%, -50%)',
                                position: 'absolute',
                                zIndex: 10,
                            }}
                        />
                        <Handle
                            type="target"
                            position={optimalPosition}
                            id={p.id}
                            style={{
                                left: p.x,
                                top: p.y,
                                background: 'transparent',
                                width: 2,
                                height: 2,
                                border: 'none',
                                transform: 'translate(-50%, -50%)',
                                position: 'absolute',
                                zIndex: 10,
                            }}
                        />
                    </React.Fragment>
                );
            })}

            {/* 3. Helper Label */}
            <div className="absolute -bottom-6 w-full text-center text-[10px] text-gray-500 font-mono bg-black/40 rounded-full px-2 py-0.5">
                {data.label}
            </div>
        </div>
    );
}
