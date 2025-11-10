import type { JSX } from "react";
import { useEffect, useState } from "react";
import { copyToClipboard } from "../App";
import copyIcon from "../assets/content_copy_26dp_FFFFFF_FILL0_wght400_GRAD0_opsz24.svg"

export interface ErrorType {
  stackTrace: string,
  time: string,
  message: string,
  type: string,
}

export function ErrorComponent(props: { stackTrace: string, time: string, message: string, type: string }): JSX.Element {
  function getColor(): string {
    switch (props.type) {
      case 'error':
        return "red";
      case 'info':
        return "";
      case 'warning':
        return "yellow";
      default:
        return "";
    }
  }

  var [copied, setCopied] = useState(false);

  useEffect(() => {
    // If 'isCopied' is true, set a timer to turn it false after 1 second
    if (copied) {
      const timer = setTimeout(() => {
        setCopied(false);
      }, 500);

      // This cleanup function clears the timer if the component unmounts
      return () => clearTimeout(timer);
    }
  }, [copied]);


  return <div
    onClick={() => {
      copyToClipboard(`QUESTNAV LOG: [${props.time}] [${props.type.toUpperCase()}] ${props.message} (${props.stackTrace})`)
      setCopied(true);
    }}
    className="log-message"
  >
    <div>
      <p><span className={`${getColor()} pill`}>{props.stackTrace}</span> - <span className='grey'>{props.time}</span></p>
      <p className='log-message-text'>{props.message}</p>
    </div>
    <div className={`log-message-copy `}>
      {<span className={`pill blue log-message-copy-hidden ${copied ? 'visible-copy-notif' : ''}`}>Copied!</span>}
      <img src={copyIcon} className="copy-icon" />
    </div>
  </div>
}
