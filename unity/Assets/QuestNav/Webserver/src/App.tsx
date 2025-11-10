import './App.css'
import logo from './assets/QuestNavLogo-Dark.svg'
import linkArrowIcon from "./assets/arrow_outward_28dp_FFFFFF_FILL0_wght400_GRAD0_opsz24.svg"
import pauseIcon from "./assets/pause_28dp_FFFFFF_FILL0_wght400_GRAD0_opsz24.svg"
import batteryIcon from "./assets/battery_android_full_28dp_FFFFFF_FILL0_wght400_GRAD0_opsz24.svg"
import downIcon from "./assets/arrow_cool_down_26dp_FFFFFF_FILL0_wght400_GRAD0_opsz24.svg"
import playIcon from "./assets/play_arrow_28dp_FFFFFF_FILL0_wght400_GRAD0_opsz24.svg"
import type { Ref } from 'react'
import { useEffect, useState, useRef } from 'react'
import { ErrorComponent } from './components/ErrorComponent'
import type { ErrorType } from './components/ErrorComponent'



export const copyToClipboard = (text: string) => {
  navigator.clipboard.writeText(text)
    .then(() => {
      console.log("Text copied to clipboard!");
    })
    .catch(err => {
      console.error("Failed to copy text: ", err);
    });
};


function App() {
  var battery = 50;
  var x = 67.667;
  var y = 50.667;
  var z = 10.421;
  const [logs, setLogs] = useState<ErrorType[] | null>(null);
  const scrollContainerRef: Ref<HTMLDivElement | null> = useRef(null);
  const userScrolledUpRef = useRef(false);
  const [isPaused, setIsPaused] = useState(false)
  const [theme, setTheme] = useState(localStorage.getItem('prefers-darkness') ?? 'light');

  useEffect(() => {
    const scrollElement = scrollContainerRef.current;
    if (scrollElement && !userScrolledUpRef.current && !isPaused) {
      scrollElement.scrollTop = scrollElement.scrollHeight;
    }
  }, [logs]);

  const handleScroll = () => {
    const scrollElement = scrollContainerRef.current;
    if (!scrollElement) return;

    const isAtBottom =
      scrollElement.scrollHeight - scrollElement.scrollTop - scrollElement.clientHeight < 20;

    if (isAtBottom) {
      userScrolledUpRef.current = false;
      setIsPaused(false)
    } else {
      userScrolledUpRef.current = true;
      setIsPaused(true)
    }
  };

  const jumpToBottom = () => {
    const scrollElement = scrollContainerRef.current;
    if (scrollElement) {
      scrollElement.scrollTo({
        top: scrollElement.scrollHeight,
        behavior: 'smooth'
      });

      userScrolledUpRef.current = false;
      setIsPaused(false);
    }
  };

  const handleDownloadLogs = () => {
    if (!logs || logs.length === 0) {
      console.warn("No logs to download.");
      return;
    }

    const fileContent = logs.map(log => {
      return `[${log.time}] [${log.type.toUpperCase()}] ${log.message} (${log.stackTrace})`;
    }).join('\n');

    const blob = new Blob([fileContent], { type: 'text/plain;charset=utf-8' });
    const link = document.createElement('a');
    const url = URL.createObjectURL(blob);
    const timestamp = new Date().toISOString().replace(/:/g, '-').slice(0, 19).replace('T', '_');

    link.download = `questnav_logs_${timestamp}.txt`;
    link.href = url;

    document.body.appendChild(link);
    link.click();

    document.body.removeChild(link);
    URL.revokeObjectURL(url);
  };



  useEffect(() => {
    const root = document.documentElement;
    if (theme === 'dark') {
      localStorage.setItem('prefers-darkness', 'dark');
      root.setAttribute('data-theme', 'dark');
    } else {
      localStorage.setItem('prefers-darkness', 'light');
      root.removeAttribute('data-theme');
    }
  }, [theme]);


  //TODO: REMOVE SECTION LATER ONLY FOR DEV
  const addLog = (isWarning = false) => {
    const newLog = {
      stackTrace: `/var/log/system.${isWarning ? 'info' : 'err'}.log`,
      time: new Date().toLocaleTimeString(),
      message: `A ${isWarning ? 'info' : 'error'} occurred at ${Date.now()} `,
      type: isWarning ? 'info' : 'error',
    };

    setLogs(prevLogs => (prevLogs != null) ? [...prevLogs, newLog] : [newLog]);
  };

  // Add a new log every few seconds to demonstrate
  useEffect(() => {
    const errorInterval = setInterval(() => addLog(((Math.random() * 20) > 10) ? true : false), 3000);
    return () => {
      clearInterval(errorInterval);
    };
  }, []);

  //TODO: REMOVE SECTION [[[END]]] ONLY FOR DEV

  return (
    <div className="app-container">
      <header className="nav-header">
        <img src={logo} alt="questNav" className="logo" onClick={() => setTheme(theme == 'light' ? 'dark' : 'light')} />
        <a href="https://questnav.gg" target='_blank' className="link">To Docs <img src={linkArrowIcon} alt="arrow" className="icon" />
        </a>
      </header>
      <div className="status-header">
        <h2>Network Tables: <span className='green'>Connected</span></h2>
        <div className="stats-group">
          <div>10:28:67 Updated</div>
          <div>26767 Frames</div>
          <div>67 FPS</div>
        </div>
      </div>
      <aside className="info-sidebar">
        <div className="info-header">
          <h3>Quest Status: <span className='green'>Tracking</span></h3>
        </div>
        <div className="info-content">
          <div>
            <div>
              <p><span>Lost Tracking Events:</span> <span className='red'>{1}</span></p>
              <p><span>Quest IP:</span> {"198.6.7.1"}</p>
              <p><span>Team:</span> {5152}</p>
            </div>
            <div>
              <p><span className='red'>X:</span> {x} <span className='green'>Y:</span> {y} <span className='blue'>Z:</span> {z}</p>
            </div>
            <div>
              <p><span>Yaw:</span> {133}°</p>
            </div>
          </div>
        </div>
        <div className="info-battery">
          <div className="progress-container info-battery">
            <img src={batteryIcon} />
            <p>50%</p>
            <progress className="custom-progress" value={battery} max={100} />
          </div>
        </div>
      </aside>
      <main className="logs-main">
        <div className="logs-header">
          <h3>Logs</h3>
          <div className="log-controls">
            <button className="btn" onClick={() => {
              setIsPaused(!isPaused)
              if (isPaused == true) {
                jumpToBottom()
              }
            }}><img src={isPaused ? playIcon : pauseIcon} className='icon' /></button>
            <button className="btn" onClick={() => handleDownloadLogs()}>Download</button>
          </div>
        </div>
        <div
          ref={scrollContainerRef}
          className="logs-content scroll"
          onScroll={handleScroll}
        >
          {(logs == null || logs?.length == 0) &&
            <p className='pill'>No problems yet...</p>
          }
          {logs && logs.length > 0 && logs.map((log, index) => (
            <ErrorComponent
              key={index}
              stackTrace={log.stackTrace}
              time={log.time}
              message={log.message}
              type={log.type}
            />
          ))}
        </div>
        {(isPaused) && (
          <button
            onClick={jumpToBottom}
            className="down-btn"
          >
            <img src={downIcon} />
          </button>
        )}
      </main>
    </div>
  );
}

export default App;
