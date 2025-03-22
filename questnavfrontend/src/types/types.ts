export interface Position {
    x: number;
    y: number;
    z: number;
}

export interface Rotation {
    x: number;
    y: number;
    z: number;
}

export interface AprilTag {
    id: number;
    name: string;
    detected: boolean;
    anchored: boolean;
    confidence: number;
    lastSeen: string;
    position: Position | null;
    rotation: Rotation | null;
}

export interface LogMessage {
    time: string;
    message: string;
    type: 'info' | 'success' | 'error' | 'warning';
}

export type CalibrationStatus = 'not-started' | 'in-progress' | 'completed';
export type ViewMode = 'camera' | 'topdown' | 'split';