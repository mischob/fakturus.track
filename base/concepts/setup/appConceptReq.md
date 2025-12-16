# App Track requirements

This app shall be a Blazor Web Assembly app like C:\Projects\Fakturus\fakturus.poi
Based on NET Core 10. 

The app is called shortly "Track" and for Customer "Time Tracker"
Basic feature is to track and monitor the daily work time.

## Setup Requirments

Copy the basics from C:\Projects\Fakturus\fakturus.poi, like
- logging
- tailwind setup
- mobile optimiced look and feel
- Logos and Icons
- PostgreSQL setup
- Deployment Setup and Concept on Hetzner,..
- Setup git repo

And other basic setup.

## Features

The App shall at the beginning support the following feature

### Start and Stop Work Time

The customer shall be able to set the Start and the End Time of work on the main page.
If you press "Start", the current Time shall be taken and stored
If you press "Stop", the current Time shall be taken and stored
It shall be possible to edit the Start or Stop time in a mobile friendly way to cover the case that this will be set out of current time.
Both options shall allways be available. So it shall be possible for the customer to "forget" to enter one or the other.
There should also be a "New" option to start a new pair of "Start", "Stop"
The values shall be stored locally first and shall be tried to give to backend in a background job. For that provide also a "Sync" option to send it out on request.


