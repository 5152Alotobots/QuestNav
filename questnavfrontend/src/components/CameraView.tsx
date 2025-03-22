import React from 'react';
import { Anchor, Tag } from 'lucide-react';
import { AprilTag } from '../types/types.ts';

interface CameraViewProps {
    isStreaming: boolean;
    showGrid: boolean;
    zoomLevel: number;
    aprilTags: AprilTag[];
    selectedTag: number | null;
    onTagSelect: (tagId: number) => void;
}

const CameraView: React.FC<CameraViewProps> = ({
                                                   isStreaming,
                                                   showGrid,
                                                   zoomLevel,
                                                   aprilTags,
                                                   selectedTag,
                                                   onTagSelect
                                               }) => {
    const detectedTags = aprilTags.filter(tag => tag.detected);

    return (
        <div className="flex-1 relative bg-black">
            <div className="absolute inset-0 flex items-center justify-center">
                {isStreaming ? (
                    <img
                        src="/api/placeholder/800/600"
                        alt="Camera feed placeholder"
                        className="max-w-full max-h-full"
                        style={{ transform: `scale(${zoomLevel})` }}
                    />
                ) : (
                    <div className="text-white text-xl">Camera Feed Paused</div>
                )}
            </div>

            {/* Grid overlay */}
            {isStreaming && showGrid && (
                <div
                    className="absolute inset-0 border border-gray-500 opacity-30 pointer-events-none"
                    style={{
                        backgroundImage: 'linear-gradient(to right, rgba(255,255,255,0.1) 1px, transparent 1px), linear-gradient(to bottom, rgba(255,255,255,0.1) 1px, transparent 1px)',
                        backgroundSize: '50px 50px'
                    }}
                />
            )}

            {/* Detected AprilTags overlay */}
            {isStreaming && detectedTags.map(tag => (
                <div
                    key={tag.id}
                    className={`absolute border-2 ${
                        selectedTag === tag.id ? 'border-yellow-400' :
                            tag.anchored ? 'border-blue-400' : 'border-green-400'
                    } rounded-lg p-1 text-white text-xs bg-black bg-opacity-70 cursor-pointer`}
                    style={{
                        left: `${30 + tag.id * 15}%`,
                        top: `${20 + tag.id * 10}%`,
                        transform: 'translate(-50%, -50%)'
                    }}
                    onClick={() => onTagSelect(tag.id)}
                >
                    <div className="flex items-center">
                        <Tag size={12} className="mr-1" />
                        <span className="font-bold">#{tag.id}:</span> {tag.name}
                        {tag.anchored && <Anchor size={12} className="ml-1 text-blue-400" />}
                    </div>
                    <div className="text-gray-300 mt-1">
                        Conf: {(tag.confidence * 100).toFixed(0)}%
                    </div>
                </div>
            ))}
        </div>
    );
};

export default CameraView;