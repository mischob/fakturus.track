# Acces local Calendar

I want to have a new feature on our tracking page (Fakturus.Track.Frontend\Pages\TimeTracker.razor) that give access to our shared family calendar : webcal://p171-caldav.icloud.com/published/2/OTAyMTg3MzI5MDIxODczMhIZzx2Z_XAzm2xx72mDokl_-Mbw0ZAA9E8iAUUXq1ZI8Y02TuVfscIivU2qRdKQDFLRiXOeG7oiAvRvKPDqOiA

Here are the details:

- User shall be able to open the shared calendar and select an entries to convert to work sessions
- This entry shall be taken as work session with the Start/End Time (if not already be there)
- It will be marked as not synced yet and will be traded like an manually created work session and synced as normal

Technical Details:

- The public shared calendar address shall be for the moment inside the appsettings together with an userId that is also used for our database. Only if that fits the backend shall provide the data
- Add the respective endpoints as needed. Prepare the solution to handle a public calendar feed for each user. But only prepare it