import { useState } from 'react'
import './App.css'
import { AprilTag, CalibrationStatus, LogMessage, ViewMode } from './types/types.ts'
import DashboardHeader from './components/DashboardHeader'
import MainView from './components/MainView'
import TagStatusPanel from './components/TagStatusPanel'
import LogPanel from './components/LogPanel'
import SettingsModal from './components/SettingsModal'

// Mock data
const mockAprilTags: AprilTag[] = [
    {
        id: 1,
        name: 'Robot Base',
        detected: true,
        anchored: true,
        confidence: 0.98,
        lastSeen: '2 sec ago',
        position: { x: 0.45, y: 0.12, z: 1.03 },
        rotation: { x: 0, y: 180, z: 0 }
    },
    {
        id: 2,
        name: 'Workspace Corner 1',
        detected: true,
        anchored: false,
        confidence: 0.87,
        lastSeen: '5 sec ago',
        position: { x: 1.23, y: 0.0, z: 2.15 },
        rotation: { x: 0, y: 90, z: 0 }
    },
    {
        id: 3,
        name: 'Workspace Corner 2',
        detected: false,
        anchored: false,
        confidence: 0,
        lastSeen: 'N/A',
        position: null,
        rotation: null
    },
    {
        id: 4,
        name: 'Tool Station',
        detected: true,
        anchored: true,
        confidence: 0.92,
        lastSeen: '1 sec ago',
        position: { x: -0.8, y: 0.75, z: 1.65 },
        rotation: { x: 0, y: 270, z: 0 }
    },
    {
        id: 5,
        name: 'Charging Station',
        detected: false,
        anchored: false,
        confidence: 0,
        lastSeen: '3 min ago',
        position: null,
        rotation: null
    },
]

const mockLogMessages: LogMessage[] = [
    { time: '14:32:45', message: 'Calibration started', type: 'info' },
    { time: '14:32:48', message: 'AprilTag #1 detected', type: 'success' },
    { time: '14:32:50', message: 'Spatial anchor placed at AprilTag #1', type: 'success' },
    { time: '14:33:12', message: 'AprilTag #4 detected', type: 'success' },
    { time: '14:33:15', message: 'Spatial anchor placed at AprilTag #4', type: 'success' },
    { time: '14:33:45', message: 'AprilTag #2 detected', type: 'success' },
]

function App() {
    // State hooks
    const [aprilTags, setAprilTags] = useState<AprilTag[]>(mockAprilTags)
    const [selectedTag, setSelectedTag] = useState<number | null>(null)
    const [calibrationStatus, setCalibrationStatus] = useState<CalibrationStatus>('in-progress')
    const [isStreaming, setIsStreaming] = useState<boolean>(true)
    const [zoomLevel, setZoomLevel] = useState<number>(1)
    const [showGrid, setShowGrid] = useState<boolean>(true)
    const [showSettings, setShowSettings] = useState<boolean>(false)
    const [viewMode, setViewMode] = useState<ViewMode>('camera')
    const [logMessages, setLogMessages] = useState<LogMessage[]>(mockLogMessages)

    // Helper function for generating timestamp
    const getCurrentTimestamp = (): string => {
        return new Date().toLocaleTimeString('en-US', {
            hour12: false,
            hour: '2-digit',
            minute: '2-digit',
            second: '2-digit'
        })
    }

    // Add a log message
    const addLogMessage = (message: string, type: LogMessage['type'] = 'info') => {
        const timeString = getCurrentTimestamp()
        setLogMessages(prev => [...prev, { time: timeString, message, type }])
    }

    // Handlers
    const handleTagSelect = (tagId: number) => {
        setSelectedTag(tagId === selectedTag ? null : tagId)
    }

    const handleClearSelectedTag = () => {
        setSelectedTag(null)
    }

    const handleToggleAnchor = (tagId: number) => {
        setAprilTags(prev => prev.map(tag =>
            tag.id === tagId ? { ...tag, anchored: !tag.anchored } : tag
        ))

        // Add a log message
        const tag = aprilTags.find(t => t.id === tagId)
        if (tag) {
            const newAnchorStatus = !tag.anchored
            addLogMessage(
                newAnchorStatus
                    ? `Spatial anchor placed at AprilTag #${tagId}`
                    : `Spatial anchor removed from AprilTag #${tagId}`,
                newAnchorStatus ? 'success' : 'info'
            )
        }
    }

    const handleZoomIn = () => {
        setZoomLevel(prev => Math.min(prev + 0.25, 2.5))
    }

    const handleZoomOut = () => {
        setZoomLevel(prev => Math.max(prev - 0.25, 0.5))
    }

    const handleToggleGrid = () => {
        setShowGrid(prev => !prev)
    }

    const handleResetView = () => {
        setZoomLevel(1)
    }

    const handleCalibrateCamera = () => {
        addLogMessage('Camera calibration initiated')
    }

    const handleToggleSettings = () => {
        setShowSettings(prev => !prev)
    }

    const handleSaveSettings = () => {
        setShowSettings(false)
        addLogMessage('Dashboard settings updated')
    }

    const handleCompleteCalibration = () => {
        setCalibrationStatus('completed')
        addLogMessage('Calibration completed successfully', 'success')
    }

    const handleResetCalibration = () => {
        setCalibrationStatus('in-progress')
        setAprilTags(prev => prev.map(tag => ({...tag, anchored: false})))
        addLogMessage('Calibration reset')
    }

    return (
        <div className="flex flex-col h-screen bg-gray-100">
            <DashboardHeader
                calibrationStatus={calibrationStatus}
                onToggleSettings={handleToggleSettings}
                onResetCalibration={handleResetCalibration}
            />

            <div className="flex flex-1 p-4 space-x-4 overflow-hidden">
                <MainView
                    viewMode={viewMode}
                    isStreaming={isStreaming}
                    showGrid={showGrid}
                    zoomLevel={zoomLevel}
                    aprilTags={aprilTags}
                    selectedTag={selectedTag}
                    onViewModeChange={setViewMode}
                    onToggleStreaming={() => setIsStreaming(prev => !prev)}
                    onZoomIn={handleZoomIn}
                    onZoomOut={handleZoomOut}
                    onToggleGrid={handleToggleGrid}
                    onResetView={handleResetView}
                    onCalibrateCamera={handleCalibrateCamera}
                    onTagSelect={handleTagSelect}
                />

                <div className="w-2/5 flex flex-col space-y-4">
                    <TagStatusPanel
                        aprilTags={aprilTags}
                        selectedTag={selectedTag}
                        onTagSelect={handleTagSelect}
                        onClearSelectedTag={handleClearSelectedTag}
                        onToggleAnchor={handleToggleAnchor}
                        onCompleteCalibration={handleCompleteCalibration}
                    />

                    <LogPanel
                        logMessages={logMessages}
                    />
                </div>
            </div>

            <SettingsModal
                isOpen={showSettings}
                showGrid={showGrid}
                onClose={handleToggleSettings}
                onToggleGrid={handleToggleGrid}
                onSaveSettings={handleSaveSettings}
            />
        </div>
    )
}

export default App