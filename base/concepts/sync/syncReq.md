# Sync concept

At the moment the sync concept implemented in Fakturus.Track.Backend\Endpoints\WorkSessions and the respective UI (Fakturus.Track.Frontend\Services) is not optimal.
We tried changes several times, but that does not worked. 
So now we change the implementation to following algorithm:

1) If a work session is created locally, that should become a uuid. 
2) This uuid shall be given to the backend when a forced sync or a background sync is happening. The backend shall use this uuid to store it inside the database! The uuid will be the main id to check differences!
3) If the ui project has a new work session which is not yet synced to backend, it should try to sync it in background every 30s. But only if good network connection is available! 
4) So with this background sync it shall also be checked if new entries are available on backend which are not yet locally available. If that is the case, then the ui shall update the ui with the new entries
5) Each new entry which is not yet synced to backend shall have the "Pending Sync" badge as it is now
6) if a entry in local ui was synced, but the backend does not have it anymore (maybe deleted by another ui client), the ui shall remove it from the UI
7) if there is no new entry in ui which need to be synced, the background sync shall be stopped. Then only the manual sync button trigger the update
8) The manual sync and the background sync does the same job