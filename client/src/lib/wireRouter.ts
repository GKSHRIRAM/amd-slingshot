// ═══════════════════════════════════════════════════════════════
//  A* Manhattan Wire Router
//  Professional orthogonal routing with obstacle avoidance
// ═══════════════════════════════════════════════════════════════

export interface Point {
    x: number;
    y: number;
}

interface Cell {
    row: number;
    col: number;
}

interface AStarNode {
    cell: Cell;
    g: number;       // cost from start
    h: number;       // heuristic to goal
    f: number;       // g + h
    parent: AStarNode | null;
    direction: number; // 0=up, 1=right, 2=down, 3=left, -1=start
}

// ═══════════════════════════════════════════════════════════════
//  ROUTING GRID — Discretizes canvas into cells
// ═══════════════════════════════════════════════════════════════

const CELL_SIZE = 6; // 6px per cell — balance between precision and speed
const TURN_PENALTY = 2; // Penalty for changing direction (encourages straight runs)

export class RoutingGrid {
    private cells: boolean[][];
    readonly cols: number;
    readonly rows: number;

    constructor(canvasWidth: number, canvasHeight: number) {
        this.cols = Math.ceil(canvasWidth / CELL_SIZE);
        this.rows = Math.ceil(canvasHeight / CELL_SIZE);
        // All cells start as OPEN (true)
        this.cells = Array.from({ length: this.rows }, () =>
            Array(this.cols).fill(true)
        );
    }

    /** Mark a rectangular region as BLOCKED */
    blockRegion(x: number, y: number, width: number, height: number, padding = 8) {
        const startCol = Math.max(0, Math.floor((x - padding) / CELL_SIZE));
        const endCol = Math.min(this.cols - 1, Math.ceil((x + width + padding) / CELL_SIZE));
        const startRow = Math.max(0, Math.floor((y - padding) / CELL_SIZE));
        const endRow = Math.min(this.rows - 1, Math.ceil((y + height + padding) / CELL_SIZE));

        for (let r = startRow; r <= endRow; r++) {
            for (let c = startCol; c <= endCol; c++) {
                this.cells[r][c] = false;
            }
        }
    }

    /** Unblock a specific cell (for pin endpoints) */
    unblockCell(x: number, y: number) {
        const cell = this.toCell(x, y);
        if (this.isInBounds(cell.row, cell.col)) {
            this.cells[cell.row][cell.col] = true;
        }
        // Also unblock neighbors so the pathfinder can reach it
        for (const [dr, dc] of [[0, 1], [0, -1], [1, 0], [-1, 0]]) {
            const nr = cell.row + dr;
            const nc = cell.col + dc;
            if (this.isInBounds(nr, nc)) {
                this.cells[nr][nc] = true;
            }
        }
    }

    isOpen(row: number, col: number): boolean {
        return this.isInBounds(row, col) && this.cells[row][col];
    }

    isInBounds(row: number, col: number): boolean {
        return row >= 0 && row < this.rows && col >= 0 && col < this.cols;
    }

    toCell(x: number, y: number): Cell {
        return {
            row: Math.round(y / CELL_SIZE),
            col: Math.round(x / CELL_SIZE),
        };
    }

    toPixel(cell: Cell): Point {
        return {
            x: cell.col * CELL_SIZE,
            y: cell.row * CELL_SIZE,
        };
    }
}

// ═══════════════════════════════════════════════════════════════
//  MIN-HEAP PRIORITY QUEUE
// ═══════════════════════════════════════════════════════════════

class MinHeap {
    private heap: AStarNode[] = [];

    push(node: AStarNode) {
        this.heap.push(node);
        this.bubbleUp(this.heap.length - 1);
    }

    pop(): AStarNode | undefined {
        if (this.heap.length === 0) return undefined;
        const top = this.heap[0];
        const last = this.heap.pop()!;
        if (this.heap.length > 0) {
            this.heap[0] = last;
            this.sinkDown(0);
        }
        return top;
    }

    get size() { return this.heap.length; }

    private bubbleUp(i: number) {
        while (i > 0) {
            const parent = (i - 1) >> 1;
            if (this.heap[parent].f <= this.heap[i].f) break;
            [this.heap[parent], this.heap[i]] = [this.heap[i], this.heap[parent]];
            i = parent;
        }
    }

    private sinkDown(i: number) {
        const n = this.heap.length;
        while (true) {
            let smallest = i;
            const left = 2 * i + 1;
            const right = 2 * i + 2;
            if (left < n && this.heap[left].f < this.heap[smallest].f) smallest = left;
            if (right < n && this.heap[right].f < this.heap[smallest].f) smallest = right;
            if (smallest === i) break;
            [this.heap[smallest], this.heap[i]] = [this.heap[i], this.heap[smallest]];
            i = smallest;
        }
    }
}

// ═══════════════════════════════════════════════════════════════
//  A* PATHFINDER — Orthogonal only (Manhattan routing)
// ═══════════════════════════════════════════════════════════════

// 4 directions: up, right, down, left
const DR = [-1, 0, 1, 0];
const DC = [0, 1, 0, -1];

const MAX_ITERATIONS = 15000; // Safety limit

export function findPath(
    grid: RoutingGrid,
    startX: number, startY: number,
    endX: number, endY: number
): Point[] {
    const start = grid.toCell(startX, startY);
    const goal = grid.toCell(endX, endY);

    // If start === goal, return direct
    if (start.row === goal.row && start.col === goal.col) {
        return [{ x: startX, y: startY }, { x: endX, y: endY }];
    }

    const openSet = new MinHeap();
    const closedSet = new Set<string>();
    const gScores = new Map<string, number>();

    const startNode: AStarNode = {
        cell: start,
        g: 0,
        h: manhattan(start, goal),
        f: manhattan(start, goal),
        parent: null,
        direction: -1,
    };

    const key = (c: Cell) => `${c.row},${c.col}`;
    openSet.push(startNode);
    gScores.set(key(start), 0);

    let iterations = 0;

    while (openSet.size > 0 && iterations < MAX_ITERATIONS) {
        iterations++;
        const current = openSet.pop()!;
        const ck = key(current.cell);

        if (current.cell.row === goal.row && current.cell.col === goal.col) {
            // Reconstruct path
            const path = reconstructPath(current, grid);
            // Snap endpoints to exact pixel coordinates
            path[0] = { x: startX, y: startY };
            path[path.length - 1] = { x: endX, y: endY };
            return simplifyPath(path);
        }

        if (closedSet.has(ck)) continue;
        closedSet.add(ck);

        // Expand 4 orthogonal neighbors
        for (let dir = 0; dir < 4; dir++) {
            const nr = current.cell.row + DR[dir];
            const nc = current.cell.col + DC[dir];

            if (!grid.isOpen(nr, nc)) continue;
            const nk = `${nr},${nc}`;
            if (closedSet.has(nk)) continue;

            // Cost: 1 per step + turn penalty if direction changes
            const turnCost = (current.direction >= 0 && current.direction !== dir) ? TURN_PENALTY : 0;
            const newG = current.g + 1 + turnCost;

            const existingG = gScores.get(nk);
            if (existingG !== undefined && newG >= existingG) continue;

            gScores.set(nk, newG);

            const h = manhattan({ row: nr, col: nc }, goal);
            openSet.push({
                cell: { row: nr, col: nc },
                g: newG,
                h,
                f: newG + h,
                parent: current,
                direction: dir,
            });
        }
    }

    // Fallback: no valid path found — return straight line
    return [{ x: startX, y: startY }, { x: endX, y: endY }];
}

function manhattan(a: Cell, b: Cell): number {
    return Math.abs(a.row - b.row) + Math.abs(a.col - b.col);
}

function reconstructPath(node: AStarNode, grid: RoutingGrid): Point[] {
    const cells: Cell[] = [];
    let current: AStarNode | null = node;
    while (current) {
        cells.unshift(current.cell);
        current = current.parent;
    }
    return cells.map((c) => grid.toPixel(c));
}

// ═══════════════════════════════════════════════════════════════
//  PATH SIMPLIFICATION — Remove redundant collinear waypoints
// ═══════════════════════════════════════════════════════════════

function simplifyPath(path: Point[]): Point[] {
    if (path.length <= 2) return path;

    const simplified: Point[] = [path[0]];

    for (let i = 1; i < path.length - 1; i++) {
        const prev = simplified[simplified.length - 1];
        const curr = path[i];
        const next = path[i + 1];

        // Keep point only if direction changes (not collinear)
        const dx1 = curr.x - prev.x;
        const dy1 = curr.y - prev.y;
        const dx2 = next.x - curr.x;
        const dy2 = next.y - curr.y;

        // Direction changed — keep this waypoint
        const sameDir = (dx1 === 0 && dx2 === 0) || (dy1 === 0 && dy2 === 0);
        if (!sameDir) {
            simplified.push(curr);
        }
    }

    simplified.push(path[path.length - 1]);
    return simplified;
}

// ═══════════════════════════════════════════════════════════════
//  WIRE SEPARATION — Offset parallel wires
// ═══════════════════════════════════════════════════════════════

const WIRE_SEPARATION = 8; // px between parallel wires

export function applyWireOffset(path: Point[], offsetIndex: number): Point[] {
    if (offsetIndex === 0 || path.length < 2) return path;

    const offset = offsetIndex * WIRE_SEPARATION;

    return path.map((point, i) => {
        if (i === 0 || i === path.length - 1) return point; // Don't offset endpoints

        const prev = path[i - 1];
        const curr = point;

        // Determine segment direction and apply perpendicular offset
        const dx = curr.x - prev.x;
        const dy = curr.y - prev.y;

        if (Math.abs(dx) > Math.abs(dy)) {
            // Horizontal segment — offset vertically
            return { x: curr.x, y: curr.y + offset };
        } else {
            // Vertical segment — offset horizontally
            return { x: curr.x + offset, y: curr.y };
        }
    });
}
