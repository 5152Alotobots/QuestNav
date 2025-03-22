import React from 'react';
import { AlertCircle } from 'lucide-react';
import { AprilTag } from '../types/types.ts';

interface CalibrationProgressProps {
    aprilTags: AprilTag[];
    onCompleteCalibration: () => void;
}

const CalibrationProgress: React.FC<CalibrationProgressProps> = ({
                                                                     aprilTags,
                                                                     onCompleteCalibration
                                                                 }) => {
    const anchoredTagsCount = aprilTags.filter(tag => tag.anchored).length;
    const requiredAnchors = 3;
    const isComplete = anchoredTagsCount >= requiredAnchors;
    const progressPercentage = Math.min(100, (anchoredTagsCount / requiredAnchors) * 100);

    return (
        <div className="bg-gray-100 p-4 border-t">
            <div className="flex items-center justify-between mb-4">
                <div>
                    <div className="text-sm mb-1">Calibration Progress</div>
                    <div className="w-48 h-2 bg-gray-300 rounded-full overflow-hidden">
                        <div
                            className="h-full bg-blue-500 rounded-full"
                            style={{ width: `${progressPercentage}%` }}
                        ></div>
                    </div>
                </div>
                <button
                    className={`px-4 py-2 rounded text-white ${
                        isComplete ? 'bg-green-600 hover:bg-green-700' : 'bg-gray-400 cursor-not-allowed'
                    }`}
                    disabled={!isComplete}
                    onClick={onCompleteCalibration}
                >
                    Complete Calibration
                </button>
            </div>

            {!isComplete && (
                <div className="bg-yellow-100 text-yellow-800 p-3 rounded-lg flex items-start">
                    <AlertCircle size={18} className="mr-2 flex-shrink-0 mt-0.5" />
                    <div className="text-sm">
                        At least {requiredAnchors} spatial anchors must be placed to complete calibration.
                        Currently {anchoredTagsCount} of {requiredAnchors} required anchors are placed.
                    </div>
                </div>
            )}
        </div>
    );
};

export default CalibrationProgress;