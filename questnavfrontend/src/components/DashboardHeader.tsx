import React from 'react';
import { Check, Download, RefreshCw, Settings } from 'lucide-react';
import { CalibrationStatus } from '../types/types.ts';

interface DashboardHeaderProps {
    calibrationStatus: CalibrationStatus;
    onToggleSettings: () => void;
    onResetCalibration: () => void;
}

const DashboardHeader: React.FC<DashboardHeaderProps> = ({
                                                             calibrationStatus,
                                                             onToggleSettings,
                                                             onResetCalibration
                                                         }) => {
    return (
        <header className="bg-blue-600 text-white p-3">
            <div className="flex justify-between items-center">
                <div className="flex items-center">
                    <h1 className="text-2xl font-bold mr-4">Quest 3 Robot Calibration Dashboard</h1>
                    <span className={`px-3 py-1 rounded-full flex items-center ${
                        calibrationStatus === 'completed' ? 'bg-green-500' :
                            calibrationStatus === 'in-progress' ? 'bg-yellow-500' : 'bg-gray-500'
                    }`}>
            <span className="mr-2">{
                calibrationStatus === 'completed' ? 'Calibration Complete' :
                    calibrationStatus === 'in-progress' ? 'Calibration in Progress' : 'Not Started'
            }</span>
                        {calibrationStatus === 'completed' && <Check size={16} />}
          </span>
                </div>

                <div className="flex items-center space-x-3">
                    <button
                        className="px-3 py-1 bg-blue-700 hover:bg-blue-800 rounded flex items-center"
                        onClick={onToggleSettings}
                    >
                        <Settings size={16} className="mr-2" />
                        Settings
                    </button>
                    <button className="px-3 py-1 bg-blue-700 hover:bg-blue-800 rounded flex items-center">
                        <Download size={16} className="mr-2" />
                        Export Data
                    </button>
                    <button
                        className="px-3 py-1 bg-blue-700 hover:bg-blue-800 rounded flex items-center"
                        onClick={onResetCalibration}
                    >
                        <RefreshCw size={16} className="mr-2" />
                        Reset Calibration
                    </button>
                </div>
            </div>
        </header>
    );
};

export default DashboardHeader;