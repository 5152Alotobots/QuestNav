import React from 'react';
import { AprilTag } from '../types/types.ts';
import { Anchor, Tag } from 'lucide-react';

interface SplitViewProps {
    isStreaming: boolean;
    showGrid: boolean;
    zoomLevel: number;
    aprilTags: AprilTag[];
    selectedTag: number | null;
    onTagSelect: (tagId: number) => void;
}

const SplitView: React.FC<SplitViewProps> = ({
                                                 isStreaming,
                                                 showGrid,
                                                 zoomLevel,
                                                 aprilTags,
                                                 selectedTag,
                                                 onTagSelect
                                             }) => {
    const tagsWithPosition = aprilTags.filter(tag => tag.position);
    const anchoredTags = aprilTags.filter(tag => tag.anchored && tag.position);
    const detectedTags = aprilTags.filter(tag => tag.detected);

    return (
        <div className="flex h-full">
            {/* Camera view side */}
            <div className="w-1/2 border-r border-gray-800 relative">
                <div className="absolute inset-0 flex items-center justify-center">
                    {isStreaming ? (
                        <img
                            src="/api/placeholder/800/600"
                            alt="Camera feed"
                            className="w-full h-full object-contain"
                            style={{ transform: `scale(${zoomLevel})` }}
                        />
                    ) : (
                        <div className="text-white text-xl">Camera Feed Paused</div>
                    )}
                </div>

                {/* Camera overlays */}
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

                <div className="absolute top-2 left-2 text-white text-xs bg-black bg-opacity-50 px-2 py-1 rounded">
                    Camera View
                </div>
            </div>

            {/* Top-down view side */}
            <div className="w-1/2 relative">
                <div className="absolute inset-0 bg-gray-900 flex items-center justify-center">
                    <div className="w-4/5 h-4/5 border border-gray-500 relative">
                        {/* Grid lines */}
                        <div
                            className="absolute inset-0"
                            style={{
                                backgroundImage: 'linear-gradient(to right, rgba(255,255,255,0.2) 1px, transparent 1px), linear-gradient(to bottom, rgba(255,255,255,0.2) 1px, transparent 1px)',
                                backgroundSize: '50px 50px'
                            }}
                        />

                        {/* Origin indicator */}
                        <div className="absolute top-1/2 left-1/2 transform -translate-x-1/2 -translate-y-1/2">
                            <div className="w-6 h-6 border-2 border-yellow-400 rounded-full flex items-center justify-center">
                                <div className="w-2 h-2 bg-yellow-400 rounded-full"></div>
                            </div>
                            <div className="absolute top-7 left-1/2 transform -translate-x-1/2 text-yellow-400 text-xs whitespace-nowrap">
                                Origin (0,0,0)
                            </div>
                        </div>

                        {/* Quest headset position */}
                        <div className="absolute w-8 h-8 top-1/2 left-1/3 transform -translate-x-1/2 -translate-y-1/2">
                            <div className="w-full h-full border-2 border-purple-500 bg-purple-500 bg-opacity-30 rounded flex items-center justify-center">
                                <div className="w-4 h-4 border-2 border-purple-200 rounded-full flex items-center justify-center">
                                    <div className="w-1 h-1 bg-purple-200 rounded-full"></div>
                                </div>
                            </div>
                            <div className="absolute h-5 w-1 bg-purple-500 left-1/2 transform -translate-x-1/2 rounded-full"></div>
                            <div className="absolute top-9 left-1/2 transform -translate-x-1/2 text-xs whitespace-nowrap text-purple-300">
                                Quest 3
                            </div>
                        </div>

                        {/* AprilTag indicators */}
                        {tagsWithPosition.map(tag => (
                            <div
                                key={tag.id}
                                className={`absolute w-6 h-6 transform -translate-x-1/2 -translate-y-1/2 cursor-pointer ${
                                    selectedTag === tag.id ? 'z-10' : ''
                                }`}
                                style={{
                                    left: `${50 + (tag.position?.x || 0) * 15}%`,
                                    top: `${50 - (tag.position?.z || 0) * 15}%`,
                                }}
                                onClick={() => onTagSelect(tag.id)}
                            >
                                <div className={`w-full h-full rounded-lg border-2 flex items-center justify-center text-xs font-bold ${
                                    tag.anchored
                                        ? 'border-blue-500 bg-blue-500 bg-opacity-30 text-blue-200'
                                        : 'border-green-500 bg-green-500 bg-opacity-20 text-green-200'
                                }`}>
                                    {tag.id}
                                    <div
                                        className={`absolute w-8 h-1 ${tag.anchored ? 'bg-blue-500' : 'bg-green-500'}`}
                                        style={{
                                            transformOrigin: 'center',
                                            transform: `rotate(${tag.rotation ? tag.rotation.y : 0}deg)`,
                                        }}
                                    ></div>
                                </div>
                                <div className="absolute top-7 left-1/2 transform -translate-x-1/2 text-xs whitespace-nowrap text-white">
                                    {tag.name}
                                </div>
                            </div>
                        ))}

                        {/* Drawing connection lines between tags */}
                        <svg className="absolute inset-0 pointer-events-none" width="100%" height="100%">
                            {anchoredTags.map((tag, i, arr) => {
                                if (i < arr.length - 1) {
                                    const nextTag = arr[i + 1];
                                    const x1 = `${50 + (tag.position?.x || 0) * 15}%`;
                                    const y1 = `${50 - (tag.position?.z || 0) * 15}%`;
                                    const x2 = `${50 + (nextTag.position?.x || 0) * 15}%`;
                                    const y2 = `${50 - (nextTag.position?.z || 0) * 15}%`;
                                    return (
                                        <line
                                            key={`line-${tag.id}-${nextTag.id}`}
                                            x1={x1}
                                            y1={y1}
                                            x2={x2}
                                            y2={y2}
                                            stroke="#3b82f6"
                                            strokeWidth="1"
                                            strokeDasharray="5,5"
                                        />
                                    );
                                }
                                return null;
                            })}
                        </svg>
                    </div>
                </div>
                <div className="absolute top-2 left-2 text-white text-xs bg-black bg-opacity-50 px-2 py-1 rounded">
                    Top-Down Map
                </div>
            </div>
        </div>
    );
};

export default SplitView;