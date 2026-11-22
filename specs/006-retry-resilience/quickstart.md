# Quickstart: Retry short-lived failures

Run after the implementation, with the services started as usual (`RunAll.ps1`). Fill the Result column.

| # | Steps | Expected | Result |
|---|---|---|---|
| Q1 | Stop the API. Send an image in a chat (the upload goes through the API, so first send the image, then stop the API before the preview is ready; or stop the API for 10 seconds right after sending). Start the API again within about 30 seconds. | The ImageProcessing log shows warnings `Retry ImageResizeRequestConsumer: attempt N after ...`, then the preview appears in the chat. | |
| Q2 | Same as Q1 for a link in a message. | The LinkPreview log shows retry warnings for `LinkPreviewApiClient`; the preview appears. | |
| Q3 | Keep the API stopped for longer than the total time (about 2 minutes). | The consumer fails once after the last attempt, as before this feature; the log shows the number of attempts. | |
| Q4 | Stop the Authentication service for a few seconds, then call any authenticated endpoint (token check) while it is down and start it again within the retry time. | The request succeeds (debug log of `Grpc.Net.Client` shows the repeat). | |
| Q5 | With the Authentication service stopped, request a login code. | One quick failure; no repeat; after starting the service the next request works and the e-mail is sent. | |
| Q6 | Send a link whose page returns 404. | One attempt only, no retry warning. | |
| Q7 | Set `Resilience:Http:MaxRetryAttempts` to `0` in the LinkPreview `appsettings.json` and repeat Q2. | No repeat; the failure appears at once. | |
| Q8 | Set `Resilience:Http:Delay` to `00:00:00` and start the service. | The service stops at start with a message that names `Resilience:Http:Delay`. | |
| Q9 | Redeploy both Lambdas; stop the API briefly while a preview is being made. | The Lambda log shows the repeat warning; the preview arrives after the API is back. | |
| Q10 | Check the retry warnings in all logs. | They contain the client name, the attempt and the reason, and no address, token or message text. | |
