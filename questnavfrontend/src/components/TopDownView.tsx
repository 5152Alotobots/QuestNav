import React from 'react';
import { AprilTag } from '../types/types.ts';

interface TopDownViewProps {
    aprilTags: AprilTag[];
    selectedTag: number | null;
    onTagSelect: (tagId: number) => void;
}

const TopDownView: React.FC<TopDownViewProps> = ({
                                                     aprilTags,
                                                     selectedTag,
                                                     onTagSelect
                                                 }) => {
    const tagsWithPosition = aprilTags.filter(tag => tag.position);
    const anchoredTags = aprilTags.filter(tag => tag.anchored && tag.position);

    return (
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
    );
};

export default TopDownView;