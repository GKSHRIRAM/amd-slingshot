import React from 'react';
import { Handle, Position } from '@xyflow/react';

interface GenericNodeProps {
    data: {
        type: string;
        label: string;
        pins: string[];
    };
}

export default function GenericNode({ data }: GenericNodeProps) {
    // Dynamically calculate height based on number of pins to fit them nicely
    const height = Math.max(80, 40 + Math.ceil(data.pins.length / 2) * 20);

    return (
        <div
            className="relative drop-shadow-2xl flex flex-col items-center justify-center p-4 bg-gray-800 border-2 border-gray-600 rounded-lg min-w-[140px]"
            style={{ minHeight: `${height}px` }}
        >
            {/* Dynamic Pins */}
            {data.pins.map((pinName, i) => {
                const isLeft = i % 2 === 0;
                const top = 20 + Math.floor(i / 2) * 20;

                return (
                    <div key={pinName}>
                        <Handle
                            type="source"
                            position={isLeft ? Position.Left : Position.Right}
                            id={pinName}
                            style={{
                                top: `${top}px`,
                                background: '#3b82f6',
                                width: '8px',
                                height: '8px',
                                borderRadius: '50%',
                                border: '2px solid #1f2937'
                            }}
                        />
                        <Handle
                            type="target"
                            position={isLeft ? Position.Left : Position.Right}
                            id={pinName}
                            style={{
                                top: `${top}px`,
                                background: 'transparent',
                                width: '8px',
                                height: '8px',
                                borderRadius: '50%',
                                border: 'none',
                                zIndex: -1
                            }}
                        />
                        <span
                            className="absolute text-[8px] font-mono text-gray-300 font-bold"
                            style={{
                                top: `${top - 5}px`,
                                [isLeft ? 'left' : 'right']: '12px'
                            }}
                        >
                            {pinName}
                        </span>
                    </div>
                );
            })}

            {/* The Visual Label */}
            <div className="text-emerald-400 font-mono text-xs font-bold text-center mb-1 mt-2">
                {data.label}
            </div>
            <div className="text-gray-500 font-mono text-[8px] uppercase tracking-wider">
                GENERIC MODULE
            </div>
        </div>
    );
}
