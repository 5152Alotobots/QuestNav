import React from 'react';
import { AprilTag, ViewMode } from '../types/types.ts';
import ViewModeTabs from './ViewModeTabs';
import CameraView from './CameraView';
import TopDownView from './TopDownView';
import SplitView from './SplitView';
import CameraControls from './CameraControls';

interface MainViewProps {
    viewMode: ViewMode;
    isStreaming: boolean;
    showGrid: boolean;
    zoomLevel: number;
    aprilTags: AprilTag[];
    selectedTag: number | null;
    onViewModeChange: (mode: ViewMode) => void;
    onToggleStreaming: () => void;
    onZoomIn: () => void;
    onZoomOut: () => void;
    onToggleGrid: () => void;
    onResetView: () => void;
    onCalibrateCamera: () => void;
    onTagSelect: (tagId: number) => void;
}

const MainView: React.FC<MainViewProps> = ({
                                               viewMode,
                                               isStreaming,
                                               showGrid,
                                               zoomLevel,
                                               aprilTags,
                                               selectedTag,
                                               onViewModeChange,
                                               onToggleStreaming,
                                               onZoomIn,
                                               onZoomOut,
                                               onToggleGrid,
                                               onResetView,
                                               onCalibrateCamera,
                                               onTagSelect
                                           }) => {
    const detectedTagsCount = aprilTags.filter(tag => tag.detected).length;

    return (
        <div className="flex flex-col w-3/5 bg-white rounded-lg shadow overflow-hidden">
            <ViewModeTabs
                viewMode={viewMode}
                isStreaming={isStreaming}
                onViewModeChange={onViewModeChange}
                onToggleStreaming={onToggleStreaming}
            />

            <div className="flex-1 relative bg-black">
                {viewMode === 'camera' && (
                    <CameraView
                        isStreaming={isStreaming}
                        showGrid={showGrid}
                        zoomLevel={zoomLevel}
                        aprilTags={aprilTags}
                        selectedTag={selectedTag}
                        onTagSelect={onTagSelect}
                    />
                )}

                {viewMode === 'topdown' && (
                    <TopDownView
                        aprilTags={aprilTags}
                        selectedTag={selectedTag}
                        onTagSelect={onTagSelect}
                    />
                )}

                {viewMode === 'split' && (
                    <SplitView
                        isStreaming={isStreaming}
                        showGrid={showGrid}
                        zoomLevel={zoomLevel}
                        aprilTags={aprilTags}
                        selectedTag={selectedTag}
                        onTagSelect={onTagSelect}
                    />
                )}
            </div>

            <CameraControls
                zoomLevel={zoomLevel}
                showGrid={showGrid}
                detectedTagsCount={detectedTagsCount}
                onZoomIn={onZoomIn}
                onZoomOut={onZoomOut}
                onToggleGrid={onToggleGrid}
                onResetView={onResetView}
                onCalibrateCamera={onCalibrateCamera}
            />
        </div>
    );
};

export default MainView;