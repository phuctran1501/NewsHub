import {
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
  type HubConnection,
} from '@microsoft/signalr'
import { useEffect, useRef, useState } from 'react'
import { env } from '../config/env'
import { getAccessToken } from '../services/tokenStore'

interface UseSignalROptions {
  enabled?: boolean
  hubPath?: string
}

export function useSignalR(options: UseSignalROptions = {}) {
  const { enabled = false, hubPath = '/hubs/notifications' } = options
  const connectionRef = useRef<HubConnection | null>(null)
  const [connectionState, setConnectionState] = useState<HubConnectionState>(
    HubConnectionState.Disconnected,
  )

  useEffect(() => {
    if (!enabled) {
      return
    }

    const connection = new HubConnectionBuilder()
      .withUrl(`${env.apiBaseUrl}${hubPath}`, {
        accessTokenFactory: () => getAccessToken() ?? '',
      })
      .withAutomaticReconnect()
      .configureLogging(env.isDevelopment ? LogLevel.Warning : LogLevel.Error)
      .build()

    connectionRef.current = connection

    connection.onreconnecting(() => setConnectionState(HubConnectionState.Reconnecting))
    connection.onreconnected(() => setConnectionState(HubConnectionState.Connected))
    connection.onclose(() => setConnectionState(HubConnectionState.Disconnected))

    connection
      .start()
      .then(() => setConnectionState(HubConnectionState.Connected))
      .catch(() => setConnectionState(HubConnectionState.Disconnected))

    return () => {
      connectionRef.current = null
      void connection.stop()
    }
  }, [enabled, hubPath])

  return {
    connection: connectionRef,
    connectionState,
    isConnected: connectionState === HubConnectionState.Connected,
  }
}
