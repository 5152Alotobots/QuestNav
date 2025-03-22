import React from 'react';
import { Camera, Map, Maximize2 } from 'lucide-react';
import { ViewMode } from '../types/types.ts';

interface ViewModeTabsProps {
    viewMode: ViewMode;
    isStreaming: boolean;
    onViewModeChange: (mode: ViewMode) => void;
    onToggleStreaming: () => void;
}

const ViewModeTabs: React.FC<ViewModeTabsProps> = ({
                                                       viewMode,
                                                       isStreaming,
                                                       onViewModeChange,
                                                       onToggleStreaming
                                                   }) => {
    return (
        <div className="bg-gray-800 flex border-b border-gray-700">
            <button
                className={`px-4 py-2 text-white flex items-center ${viewMode === 'camera' ? 'bg-blue-600' : 'hover:bg-gray-700'}`}
                onClick={() => onViewModeChange('camera')}
            >
                <Camera size={16} className="mr-2" />
                Camera View
            </button>
            <button
                className={`px-4 py-2 text-white flex items-center ${viewMode === 'topdown' ? 'bg-blue-600' : 'hover:bg-gray-700'}`}
                onClick={() => onViewModeChange('topdown')}
            >
                <Map size={16} className="mr-2" />
                Top-Down Map
            </button>
            <button
                className={`px-4 py-2 text-white flex items-center ${viewMode === 'split' ? 'bg-blue-600' : 'hover:bg-gray-700'}`}
                onClick={() => onViewModeChange('split')}
            >
                <Maximize2 size={16} className="mr-2" />
                Split View
            </button>

            <div className="ml-auto flex items-center px-3">
                <div
                    className={`px-3 py-1 rounded-full text-sm flex items-center cursor-pointer ${isStreaming ? 'bg-green-500' : 'bg-red-500'}`}
                    onClick={onToggleStreaming}
                >
                    {isStreaming ? 'Live Stream' : 'Paused'}
                </div>
            </div>
        </div>
    );
};

export default ViewModeTabs;