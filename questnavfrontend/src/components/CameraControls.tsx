import React from 'react';
import { Camera, Grid, RotateCw, ZoomIn, ZoomOut } from 'lucide-react';

interface CameraControlsProps {
    zoomLevel: number;
    showGrid: boolean;
    detectedTagsCount: number;
    onZoomIn: () => void;
    onZoomOut: () => void;
    onToggleGrid: () => void;
    onResetView: () => void;
    onCalibrateCamera: () => void;
}

const CameraControls: React.FC<CameraControlsProps> = ({
                                                           zoomLevel,
                                                           showGrid,
                                                           detectedTagsCount,
                                                           onZoomIn,
                                                           onZoomOut,
                                                           onToggleGrid,
                                                           onResetView,
                                                           onCalibrateCamera
                                                       }) => {
    return (
        <div className="bg-gray-800 p-3 text-white">
            <div className="flex justify-between items-center">
                <div className="flex space-x-3">
                    <button
                        className="bg-blue-600 px-3 py-1 rounded hover:bg-blue-700 flex items-center"
                        onClick={onCalibrateCamera}
                    >
                        <Camera size={16} className="mr-1" />
                        Calibrate Camera
                    </button>
                    <div className="flex items-center space-x-1 bg-gray-700 rounded">
                        <button
                            className="px-2 py-1 hover:bg-gray-600 rounded-l"
                            onClick={onZoomOut}
                        >
                            <ZoomOut size={16} />
                        </button>
                        <div className="px-2">{Math.round(zoomLevel * 100)}%</div>
                        <button
                            className="px-2 py-1 hover:bg-gray-600 rounded-r"
                            onClick={onZoomIn}
                        >
                            <ZoomIn size={16} />
                        </button>
                    </div>
                    <button
                        className={`px-3 py-1 rounded hover:bg-gray-600 flex items-center ${showGrid ? 'bg-gray-600' : 'bg-gray-700'}`}
                        onClick={onToggleGrid}
                    >
                        <Grid size={16} className="mr-1" />
                        {showGrid ? 'Hide Grid' : 'Show Grid'}
                    </button>
                    <button
                        className="bg-gray-700 px-3 py-1 rounded hover:bg-gray-600 flex items-center"
                        onClick={onResetView}
                    >
                        <RotateCw size={16} className="mr-1" />
                        Reset View
                    </button>
                </div>
                <div className="text-sm">
                    {detectedTagsCount} tags detected
                </div>
            </div>
        </div>
    );
};

export default CameraControls;