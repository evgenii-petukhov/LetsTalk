# Deployment notes: PATCH for partial message updates

Applies to the release of `specs/003-patch-partial-updates`. No setting, secret or database change.

## What changes
`MarkAsRead`, `SetLinkPreview` and `SetImagePreview` under `/api/Message` answer to `PATCH` instead of `PUT`. A `PUT` to them now answers `405`. This is a hard switch (see the spec): the old verb is not kept.

## Who calls what
| Operation | Caller | Deployed with |
|---|---|---|
| `MarkAsRead` | Angular app (`frontend/`) | the front end |
| `SetImagePreview` | `LetsTalk.Server.ImageProcessing.Lambda` (cloud-function mode) or `LetsTalk.Server.ImageProcessing.Service` (local-service mode) | the Lambda, or the service |
| `SetLinkPreview` | `LetsTalk.Server.LinkPreview.Lambda` (cloud-function mode) or `LetsTalk.Server.LinkPreview` (local-service mode) | the Lambda, or the service |

All of them use the generated `ApiClient`, so each must be rebuilt and redeployed with the new client. Which mode runs is chosen in each service's `appsettings.json`; in production the cloud functions do the work.

## Release order
1. Build everything from the merged branch (the clients are already regenerated).
2. Deploy the **two Lambdas** (image preview, link preview) and the **API** in the same window. If they cannot be simultaneous, deploy the Lambdas first: their new `PATCH` calls fail with `405` against the old API just as the old `PUT` calls fail against the new one, so the gap is the same either way and should be kept as short as possible.
3. Deploy the **front end** right after the API. A browser tab with the old build keeps calling `PUT MarkAsRead` until it is reloaded; the call fails and the message stays unread until the next read action.
4. In local-service mode, restart `ImageProcessing.Service` and `LinkPreview` with the new build too.

## What users see during the gap
- Messages are delivered normally. Only previews fail: an image keeps no resized preview and a link shows no card. The result is not retried, so a message sent during the gap keeps no preview.
- Unread markers may be late on old browser tabs (see step 3).

## How to spot a missed component
- The caller logs an `ApiException` with `The HTTP status code of the response was not expected (405)` from `SetImagePreview` or `SetLinkPreview` (CloudWatch for the Lambdas, the service log in local mode).
- The API's own log shows no request reaching the handler for those calls.
- Fix: deploy the missing component, nothing else.

## After the release
Send one image and one link from the app and see both previews arrive (quickstart Q4 or Q5).

## Rollback
Redeploy the previous builds of the API, the Lambdas and the front end together. The data is unchanged, so nothing else needs to be undone.
