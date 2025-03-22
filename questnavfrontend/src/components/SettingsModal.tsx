import React, { useState } from 'react';
import { X } from 'lucide-react';

interface SettingsModalProps {
    isOpen: boolean;
    showGrid: boolean;
    onClose: () => void;
    onToggleGrid: () => void;
    onSaveSettings: () => void;
}

const SettingsModal: React.FC<SettingsModalProps> = ({
                                                         isOpen,
                                                         showGrid,
                                                         onClose,
                                                         onToggleGrid,
                                                         onSaveSettings
                                                     }) => {
    const [showCoordinates, setShowCoordinates] = useState(false);
    const [showDistances, setShowDistances] = useState(false);
    const [notifyNewTag, setNotifyNewTag] = useState(true);
    const [notifyLostTag, setNotifyLostTag] = useState(true);

    if (!isOpen) return null;

    return (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
            <div className="bg-white rounded-lg shadow-lg w-96 overflow-hidden">
                <div className="bg-blue-600 text-white p-3 flex items-center justify-between">
                    <h3 className="font-semibold">Dashboard Settings</h3>
                    <button onClick={onClose}>
                        <X size={20} />
                    </button>
                </div>

                <div className="p-4">
                    <div className="mb-4">
                        <label className="block mb-2 text-sm font-medium">Display Options</label>
                        <div className="space-y-2">
                            <div className="flex items-center">
                                <input
                                    type="checkbox"
                                    id="showGrid"
                                    checked={showGrid}
                                    onChange={onToggleGrid}
                                    className="mr-2"
                                />
                                <label htmlFor="showGrid">Show grid overlay</label>
                            </div>
                            <div className="flex items-center">
                                <input
                                    type="checkbox"
                                    id="showCoordinates"
                                    checked={showCoordinates}
                                    onChange={() => setShowCoordinates(!showCoordinates)}
                                    className="mr-2"
                                />
                                <label htmlFor="showCoordinates">Show coordinates</label>
                            </div>
                            <div className="flex items-center">
                                <input
                                    type="checkbox"
                                    id="showDistances"
                                    checked={showDistances}
                                    onChange={() => setShowDistances(!showDistances)}
                                    className="mr-2"
                                />
                                <label htmlFor="showDistances">Show distances between tags</label>
                            </div>
                        </div>
                    </div>

                    <div className="mb-4">
                        <label className="block mb-2 text-sm font-medium">Camera Settings</label>
                        <div className="space-y-2">
                            <div>
                                <label className="block text-xs mb-1">Resolution</label>
                                <select className="w-full border rounded p-2 text-sm">
                                    <option>1920x1080 (High)</option>
                                    <option>1280x720 (Medium)</option>
                                    <option>854x480 (Low)</option>
                                </select>
                            </div>
                            <div>
                                <label className="block text-xs mb-1">Frame Rate</label>
                                <select className="w-full border rounded p-2 text-sm">
                                    <option>60 FPS</option>
                                    <option>30 FPS</option>
                                    <option>15 FPS</option>
                                </select>
                            </div>
                        </div>
                    </div>

                    <div className="mb-4">
                        <label className="block mb-2 text-sm font-medium">Notification Preferences</label>
                        <div className="space-y-2">
                            <div className="flex items-center">
                                <input
                                    type="checkbox"
                                    id="notifyNewTag"
                                    checked={notifyNewTag}
                                    onChange={() => setNotifyNewTag(!notifyNewTag)}
                                    className="mr-2"
                                />
                                <label htmlFor="notifyNewTag">Notify when new tag is detected</label>
                            </div>
                            <div className="flex items-center">
                                <input
                                    type="checkbox"
                                    id="notifyLostTag"
                                    checked={notifyLostTag}
                                    onChange={() => setNotifyLostTag(!notifyLostTag)}
                                    className="mr-2"
                                />
                                <label htmlFor="notifyLostTag">Notify when tag tracking is lost</label>
                            </div>
                        </div>
                    </div>

                    <div className="mt-6 flex justify-end space-x-2">
                        <button
                            className="px-4 py-2 bg-gray-200 rounded hover:bg-gray-300"
                            onClick={onClose}
                        >
                            Cancel
                        </button>
                        <button
                            className="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700"
                            onClick={onSaveSettings}
                        >
                            Save Settings
                        </button>
                    </div>
                </div>
            </div>
        </div>
    );
};

export default SettingsModal;