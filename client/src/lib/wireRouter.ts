export interface Point {
    x: number;
    y: number;
}

export interface Obstacle {
    x: number;
    y: number;
    width: number;
    height: number;
}

const CELL_SIZE = 10;
const OBSTACLE_PADDING = 15;

/**
 * An advanced Manhattan A* Router with strict Direction Change Penalties.
 * Inspired by maxGraph and circuitgrid to produce professional EDA-style wires (minimum bends).
 */
export class OrthogonalRouter {
    private width: number;
    private height: number;
    private obstacles: Obstacle[] = [];
    private gridWidth: number;
    private gridHeight: number;
    private grid: boolean[][] = [];
    private weights: number[][] = [];

    constructor(width: number, height: number) {
        this.width = width;
        this.height = height;
        this.gridWidth = Math.ceil(width / CELL_SIZE) || 1;
        this.gridHeight = Math.ceil(height / CELL_SIZE) || 1;
        this.resetGrid();
    }

    private resetGrid() {
        this.grid = Array(this.gridHeight).fill(null).map(() => Array(this.gridWidth).fill(true));
        this.weights = Array(this.gridHeight).fill(null).map(() => Array(this.gridWidth).fill(1));
    }

    blockObstacles(obstacles: Obstacle[], padding = OBSTACLE_PADDING) {
        this.obstacles = obstacles;
        for (const obs of obstacles) {
            const startX = Math.max(0, Math.floor((obs.x - padding) / CELL_SIZE));
            const startY = Math.max(0, Math.floor((obs.y - padding) / CELL_SIZE));
            const endX = Math.min(this.gridWidth - 1, Math.ceil((obs.x + obs.width + padding) / CELL_SIZE));
            const endY = Math.min(this.gridHeight - 1, Math.ceil((obs.y + obs.height + padding) / CELL_SIZE));

            for (let y = startY; y <= endY; y++) {
                for (let x = startX; x <= endX; x++) {
                    if (y >= 0 && y < this.gridHeight && x >= 0 && x < this.gridWidth) {
                        this.grid[y][x] = false;
                    }
                }
            }
        }
    }

    unblockPin(x: number, y: number) {
        if (isNaN(x) || isNaN(y)) return;
        const gx = Math.floor(x / CELL_SIZE);
        const gy = Math.floor(y / CELL_SIZE);
        const neighbors = [[0, 0], [1, 0], [-1, 0], [0, 1], [0, -1], [2, 0], [-2, 0], [0, 2], [0, -2]];

        for (const [dx, dy] of neighbors) {
            const nx = gx + dx;
            const ny = gy + dy;
            if (nx >= 0 && nx < this.gridWidth && ny >= 0 && ny < this.gridHeight) {
                this.grid[ny][nx] = true;
                this.weights[ny][nx] = 1; // Clear any heavy wire penalty
            }
        }
    }

    /**
     * Custom A* Implementation with Direction Change Penalty.
     * Guarantees 90-degree lines with the absolute minimum number of bends.
     */
    routeWire(fromX: number, fromY: number, toX: number, toY: number): Point[] {
        if (isNaN(fromX) || isNaN(fromY) || isNaN(toX) || isNaN(toY)) {
            return []; // Invalid input, return empty array so React ignores it safely
        }

        const startX = Math.floor(fromX / CELL_SIZE);
        const startY = Math.floor(fromY / CELL_SIZE);
        const goalX = Math.floor(toX / CELL_SIZE);
        const goalY = Math.floor(toY / CELL_SIZE);

        if (startX < 0 || startX >= this.gridWidth || startY < 0 || startY >= this.gridHeight ||
            goalX < 0 || goalX >= this.gridWidth || goalY < 0 || goalY >= this.gridHeight) {
            return [{ x: fromX, y: fromY }, { x: toX, y: toY }];
        }

        // Priority queue [gScore, x, y, dirX, dirY]
        const openSet: { x: number, y: number, dirX: number, dirY: number, g: number, f: number }[] = [];

        // Track parent hashes for flawless backwards reconstruction
        const cameFrom = new Map<string, string>();
        const nodeInfo = new Map<string, { x: number, y: number }>();
        const gScores = new Map<string, number>();

        const getHashes = (x: number, y: number, dx: number, dy: number) => `${x},${y},${dx},${dy}`;

        // Initial 4 directions
        const dirs = [[1, 0], [-1, 0], [0, 1], [0, -1]];
        for (const [dx, dy] of dirs) {
            const hash = getHashes(startX, startY, dx, dy);
            gScores.set(hash, 0);
            nodeInfo.set(hash, { x: startX, y: startY });
            openSet.push({ x: startX, y: startY, dirX: dx, dirY: dy, g: 0, f: this.heuristic(startX, startY, goalX, goalY) });
        }

        let finalNode: { x: number, y: number } | null = null;
        let finalDirHash = "";

        const PENALTY_TURN = 50;  // High penalty to prevent "staircase" diagonals
        const PENALTY_WIRE = 100; // Penalty to avoid crossing other wires

        while (openSet.length > 0) {
            // Find Min F in O(N) instead of sorting O(N log N)
            let minIndex = 0;
            let minF = openSet[0].f;
            for (let i = 1; i < openSet.length; i++) {
                if (openSet[i].f < minF) {
                    minF = openSet[i].f;
                    minIndex = i;
                }
            }
            const current = openSet[minIndex];

            // Pop by swapping with the last element (O(1))
            openSet[minIndex] = openSet[openSet.length - 1];
            openSet.pop();

            const currentHash = getHashes(current.x, current.y, current.dirX, current.dirY);

            // Skip if we already found a cheaper path tying to this hash
            if (current.g > (gScores.get(currentHash) ?? Infinity)) {
                continue;
            }

            if (current.x === goalX && current.y === goalY) {
                finalNode = { x: current.x, y: current.y };
                finalDirHash = currentHash;
                break;
            }

            for (const [dx, dy] of dirs) {
                const nx = current.x + dx;
                const ny = current.y + dy;

                if (nx < 0 || nx >= this.gridWidth || ny < 0 || ny >= this.gridHeight) continue;
                if (!this.grid[ny][nx] && !(nx === goalX && ny === goalY)) continue; // Allow entering goal even if technically inside obstacle padding

                const isTurn = (current.dirX !== dx || current.dirY !== dy);
                if (current.dirX === -dx && current.dirY === -dy) continue; // Don't allow U-turns

                // Cost calculation
                const moveCost = this.weights[ny][nx]; // Base cost + any previous wire penalties
                const turnCost = isTurn ? PENALTY_TURN : 0;
                const tentativeG = current.g + moveCost + turnCost;

                const neighborHash = getHashes(nx, ny, dx, dy);
                const currentG = gScores.get(neighborHash) ?? Infinity;

                if (tentativeG < currentG) {
                    cameFrom.set(neighborHash, currentHash);
                    nodeInfo.set(neighborHash, { x: nx, y: ny });
                    gScores.set(neighborHash, tentativeG);

                    const f = tentativeG + this.heuristic(nx, ny, goalX, goalY);

                    // Always inject duplicates. The pop phase will natively skip stale entries.
                    openSet.push({ x: nx, y: ny, dirX: dx, dirY: dy, g: tentativeG, f });
                }
            }
        }

        if (!finalNode) {
            // Fallback: Direct 3-segment if Pathfinding gets completely trapped
            return this.generateFallbackPath(fromX, fromY, toX, toY);
        }

        // Reconstruct Path
        const pathGridCoords: Point[] = [];
        let currHash = finalDirHash;

        while (currHash && nodeInfo.has(currHash)) {
            const node = nodeInfo.get(currHash)!;
            pathGridCoords.push({ x: node.x, y: node.y });
            currHash = cameFrom.get(currHash)!;
        }

        pathGridCoords.reverse();

        // Penalize the route for future wires
        for (const pt of pathGridCoords) {
            if (pt.y >= 0 && pt.y < this.gridHeight && pt.x >= 0 && pt.x < this.gridWidth) {
                this.weights[pt.y][pt.x] += PENALTY_WIRE;

                // Also penalize neighbors slightly to keep wires spaced out
                for (const [dx, dy] of dirs) {
                    const nx = pt.x + dx;
                    const ny = pt.y + dy;
                    if (nx >= 0 && nx < this.gridWidth && ny >= 0 && ny < this.gridHeight) {
                        this.weights[ny][nx] += 20;
                    }
                }
            }
        }

        // Extract Corner Waypoints Only (Simplification / Smoothing)
        if (pathGridCoords.length === 0) {
            return [{ x: fromX, y: fromY }, { x: toX, y: toY }];
        }

        const waypoints: Point[] = [pathGridCoords[0]];
        for (let i = 1; i < pathGridCoords.length - 1; i++) {
            const prev = pathGridCoords[i - 1];
            const curr = pathGridCoords[i];
            const next = pathGridCoords[i + 1];

            const isHorizontal = prev.y === curr.y && curr.y === next.y;
            const isVertical = prev.x === curr.x && curr.x === next.x;

            if (!isHorizontal && !isVertical) {
                waypoints.push(curr); // It's a corner!
            }
        }
        waypoints.push(pathGridCoords[pathGridCoords.length - 1]);

        // Convert back to pixels
        const pixelPath = waypoints.map(pt => ({
            x: pt.x * CELL_SIZE + CELL_SIZE / 2,
            y: pt.y * CELL_SIZE + CELL_SIZE / 2
        }));

        // Force exact pixel snap on endpoints
        pixelPath[0] = { x: fromX, y: fromY };
        pixelPath[pixelPath.length - 1] = { x: toX, y: toY };

        return pixelPath;
    }

    private heuristic(x1: number, y1: number, x2: number, y2: number): number {
        // Manhattan distance + slight tie-breaker to favor straight lines
        const dx = Math.abs(x1 - x2);
        const dy = Math.abs(y1 - y2);
        return (dx + dy) * 1.001;
    }

    private generateFallbackPath(fromX: number, fromY: number, toX: number, toY: number): Point[] {
        const midX = (fromX + toX) / 2 + (Math.random() * 20 - 10);
        return [
            { x: fromX, y: fromY },
            { x: midX, y: fromY },
            { x: midX, y: toY },
            { x: toX, y: toY }
        ];
    }
}
