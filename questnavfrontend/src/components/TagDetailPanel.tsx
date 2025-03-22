import React from 'react';
import { Anchor, Check, X } from 'lucide-react';
import { AprilTag } from '../types/types.ts';

interface TagDetailPanelProps {
    selectedTag: number | null;
    aprilTags: AprilTag[];
    onClearSelectedTag: () => void;
    onToggleAnchor: (tagId: number) => void;
}

const TagDetailPanel: React.FC<TagDetailPanelProps> = ({
                                                           selectedTag,
                                                           aprilTags,
                                                           onClearSelectedTag,
                                                           onToggleAnchor
                                                       }) => {
    if (!selectedTag) return null;

    const tag = aprilTags.find(t => t.id === selectedTag);
    if (!tag) return null;

    return (
        <div className="mb-4 border-l-4 border-blue-500 bg-blue-50 p-4 rounded-r-lg">
            <div className="flex justify-between items-start">
                <div>
                    <h3 className="font-bold text-lg flex items-center">
                        {tag.name}
                        <span className="ml-2 text-sm bg-gray-200 px-2 py-0.5 rounded">
              ID: {tag.id}
            </span>
                    </h3>
                    <div className="text-gray-500 text-sm mb-3">
                        Last seen: {tag.lastSeen}
                    </div>
                </div>
                <button
                    className="text-gray-500 hover:text-gray-700"
                    onClick={onClearSelectedTag}
                >
                    <X size={18} />
                </button>
            </div>

            <div className="grid grid-cols-2 gap-3 mb-3">
                <div className="bg-white p-2 rounded border border-gray-200">
                    <div className="text-gray-500 text-xs">Detection Status</div>
                    <div className={`flex items-center ${
                        tag.detected ? 'text-green-600' : 'text-gray-400'
                    }`}>
                        {tag.detected ? (
                            <>
                                <Check size={16} className="mr-1" />
                                Detected
                            </>
                        ) : (
                            <>
                                <X size={16} className="mr-1" />
                                Not Detected
                            </>
                        )}
                    </div>
                </div>

                <div className="bg-white p-2 rounded border border-gray-200">
                    <div className="text-gray-500 text-xs">Confidence</div>
                    <div>{tag.detected ?
                        `${(tag.confidence * 100).toFixed(1)}%` : 'N/A'}</div>
                </div>

                <div className="bg-white p-2 rounded border border-gray-200">
                    <div className="text-gray-500 text-xs">Anchor Status</div>
                    <div className={`flex items-center ${
                        tag.anchored ? 'text-blue-600' : 'text-gray-600'
                    }`}>
                        {tag.anchored ? (
                            <>
                                <Anchor size={16} className="mr-1" />
                                Anchored
                            </>
                        ) : (
                            'No Anchor'
                        )}
                    </div>
                </div>

                <div className="bg-white p-2 rounded border border-gray-200">
                    <div className="text-gray-500 text-xs">Position (X, Y, Z)</div>
                    <div>
                        {tag.position ?
                            `${tag.position.x.toFixed(2)}, 
               ${tag.position.y.toFixed(2)}, 
               ${tag.position.z.toFixed(2)}` :
                            'Unknown'}
                    </div>
                </div>
            </div>

            {tag.detected && (
                <div className="flex space-x-2">
                    <button
                        className={`px-3 py-2 rounded ${
                            tag.anchored
                                ? 'bg-red-100 text-red-700 hover:bg-red-200'
                                : 'bg-blue-100 text-blue-700 hover:bg-blue-200'
                        }`}
                        onClick={() => onToggleAnchor(tag.id)}
                    >
                        {tag.anchored ? 'Remove Spatial Anchor' : 'Place Spatial Anchor'}
                    </button>
                    <button className="px-3 py-2 bg-gray-100 text-gray-700 hover:bg-gray-200 rounded">
                        Center View
                    </button>
                </div>
            )}
        </div>
    );
};

export default TagDetailPanel;