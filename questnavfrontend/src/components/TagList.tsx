import React from 'react';
import { Anchor, Check, X } from 'lucide-react';
import { AprilTag } from '../types/types.ts';

interface TagListProps {
    aprilTags: AprilTag[];
    selectedTag: number | null;
    onTagSelect: (tagId: number) => void;
    onToggleAnchor: (tagId: number) => void;
}

const TagList: React.FC<TagListProps> = ({
                                             aprilTags,
                                             selectedTag,
                                             onTagSelect,
                                             onToggleAnchor
                                         }) => {
    return (
        <div className="space-y-2">
            {aprilTags.map(tag => (
                <div
                    key={tag.id}
                    className={`border rounded-lg p-3 cursor-pointer ${
                        selectedTag === tag.id ? 'border-blue-500 bg-blue-50' : 'border-gray-200 hover:bg-gray-50'
                    }`}
                    onClick={() => onTagSelect(tag.id)}
                >
                    <div className="flex justify-between items-center">
                        <div className="font-medium flex items-center">
              <span className="mr-2 inline-block w-5 h-5 rounded-full bg-gray-200 text-center text-xs font-bold">
                {tag.id}
              </span>
                            {tag.name}
                            {tag.anchored && (
                                <Anchor size={14} className="ml-2 text-blue-500" />
                            )}
                        </div>
                        <div className={`flex items-center text-xs ${
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

                    <div className="mt-2 grid grid-cols-2 gap-2 text-xs text-gray-500">
                        <div>Last seen: {tag.lastSeen}</div>
                        <div>Confidence: {tag.detected ? `${(tag.confidence * 100).toFixed(0)}%` : 'N/A'}</div>
                    </div>

                    {tag.detected && (
                        <div className="mt-2 flex justify-end">
                            <button
                                className={`px-2 py-1 text-xs rounded ${
                                    tag.anchored
                                        ? 'bg-gray-100 text-gray-700 hover:bg-gray-200'
                                        : 'bg-blue-100 text-blue-700 hover:bg-blue-200'
                                }`}
                                onClick={(e) => {
                                    e.stopPropagation();
                                    if (!tag.anchored) onToggleAnchor(tag.id);
                                }}
                            >
                                {tag.anchored ? 'Anchored' : 'Place Anchor'}
                            </button>
                        </div>
                    )}
                </div>
            ))}
        </div>
    );
};

export default TagList;