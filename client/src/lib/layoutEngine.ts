import ELK from 'elkjs/lib/elk.bundled.js';
import { Node, Edge } from '@xyflow/react';

const elk = new ELK();

export const getLayoutedElements = async (nodes: Node[], edges: Edge[]) => {
    const graph = {
        id: 'root',
        layoutOptions: {
            'elk.algorithm': 'layered',
            'elk.direction': 'RIGHT', // Flows from Arduino (left) to Sensors (right)
            'elk.edgeRouting': 'ORTHOGONAL', // THIS IS THE MAGIC: Forces 90-degree Manhattan lines
            'elk.spacing.nodeNode': '60', // Keeps components 60px apart
            'elk.layered.spacing.nodeNodeBetweenLayers': '100', // Distance between layers
            'elk.padding': '[top=50,left=50,bottom=50,right=50]'
        },
        // We pass the dimensions of the hardware so it routes around them cleanly
        children: nodes.map((node) => ({
            id: node.id,
            width: node.data?.type === 'wokwi-arduino-uno' ? 250 : 150,
            height: node.data?.type === 'wokwi-arduino-uno' ? 200 : 100,
        })),
        edges: edges.map((edge) => ({
            id: edge.id,
            sources: [edge.source],
            targets: [edge.target],
        })),
    };

    try {
        const layoutedGraph = await elk.layout(graph);

        // Map the new ELK-calculated coordinates back to React Flow format
        const layoutedNodes = nodes.map((node) => {
            const elkNode = layoutedGraph.children?.find((n) => n.id === node.id);
            return {
                ...node,
                position: (elkNode && elkNode.x !== undefined && elkNode.y !== undefined)
                    ? { x: elkNode.x, y: elkNode.y }
                    : node.position,
            };
        });

        return { layoutedNodes, layoutedEdges: edges };
    } catch (error) {
        console.error("ELK Routing Engine Crashed:", error);
        return { layoutedNodes: nodes, layoutedEdges: edges };
    }
};
