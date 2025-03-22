import React from 'react';
import { Tag } from 'lucide-react';
import { AprilTag } from '../types/types.ts';
import TagList from './TagList';
import TagDetailPanel from './TagDetailPanel';
import CalibrationProgress from './CalibrationProgress';

interface TagStatusPanelProps {
    aprilTags: AprilTag[];
    selectedTag: number | null;
    onTagSelect: (tagId: number) => void;
    onClearSelectedTag: () => void;
    onToggleAnchor: (tagId: number) => void;
    onCompleteCalibration: () => void;
}

const TagStatusPanel: React.FC<TagStatusPanelProps> = ({
                                                           aprilTags,
                                                           selectedTag,
                                                           onTagSelect,
                                                           onClearSelectedTag,
                                                           onToggleAnchor,
                                                           onCompleteCalibration
                                                       }) => {
    return (
        <div className="bg-white rounded-lg shadow flex-1 overflow-hidden flex flex-col">
            <div className="bg-gray-800 p-3 text-white flex justify-between items-center">
                <div className="flex items-center">
                    <Tag size={20} className="mr-2" />
                    <h2 className="font-semibold">AprilTag Status</h2>
                </div>
                <div className="text-sm">
                    {aprilTags.filter(tag => tag.anchored).length}/{aprilTags.length} anchored
                </div>
            </div>

            <div className="flex-1 overflow-y-auto p-4">
                {selectedTag && (
                    <TagDetailPanel
                        selectedTag={selectedTag}
                        aprilTags={aprilTags}
                        onClearSelectedTag={onClearSelectedTag}
                        onToggleAnchor={onToggleAnchor}
                    />
                )}

                <TagList
                    aprilTags={aprilTags}
                    selectedTag={selectedTag}
                    onTagSelect={onTagSelect}
                    onToggleAnchor={onToggleAnchor}
                />
            </div>

            <CalibrationProgress
                aprilTags={aprilTags}
                onCompleteCalibration={onCompleteCalibration}
            />
        </div>
    );
};

export default TagStatusPanel;