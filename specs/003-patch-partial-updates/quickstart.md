# Quickstart: verify PATCH for partial message updates

Run the back end with `RunAll.ps1` and the Angular app with `npm start`; open `https://127.0.0.1:4200` and use DevTools (Network tab). Run Q4 with the image and link preview services in local-service mode and Q5 with the cloud functions, each selected in the services' `appsettings.json`.

| # | Steps | Expected | Covers | Result |
|---|---|---|---|---|
| Q1 | Open `/swagger` and expand the `Message` section | No `PUT` under `/api/Message`; `MarkAsRead`, `SetLinkPreview` and `SetImagePreview` show `PATCH` | AS1.1, SC-001 | |
| Q2 | In the browser console on the app origin, run `fetch('/api/Message/MarkAsRead?chatId=x&messageId=y', {method: 'PUT', credentials: 'include'})` (adjust the base URL to the API host) | Status `405` | AS1.2, FR-002 | |
| Q3 | Sign in on two accounts. Open a chat on the first, send a message from the second | The message arrives live and is marked as read; the `MarkAsRead` request in DevTools is `PATCH` and answers `200` | AS2.1, FR-003 | |
| Q4 | Local-service mode: send an image and a link from one account | Both previews arrive on the message; the service logs show no `405` | AS3.1, AS3.2 | |
| Q5 | Cloud-function mode, after redeploying both functions: send an image and a link | Both previews arrive; CloudWatch shows no `405` | AS3.1, AS3.2 | |
| Q6 | Optional, old front end build against the new API: call `markAsRead` from a tab loaded before the release | The call fails with `405`; reloading the tab fixes it | spec edge cases | |

Record the date and outcome in the Result column when you run each row.
