import React from 'react';
import { LogMessage } from '../types/types.ts';

interface LogPanelProps {
    logMessages: LogMessage[];
}

const LogPanel: React.FC<LogPanelProps> = ({ logMessages }) => {
    return (
        <div className="bg-white rounded-lg shadow h-64 overflow-hidden flex flex-col">
            <div className="bg-gray-800 p-3 text-white">
                <h2 className="font-semibold">Calibration Log</h2>
            </div>

            <div className="flex-1 overflow-y-auto p-3 bg-gray-900 text-gray-200 font-mono text-xs">
                {logMessages.map((log, index) => (
                    <div key={index} className="mb-1 flex">
                        <span className="text-gray-500 mr-2">[{log.time}]</span>
                        <span className={
                            log.type === 'success' ? 'text-green-400' :
                                log.type === 'error' ? 'text-red-400' :
                                    log.type === 'warning' ? 'text-yellow-400' : 'text-blue-300'
                        }>
              {log.message}
            </span>
                    </div>
                ))}
            </div>
        </div>
    );
};

export default LogPanel;