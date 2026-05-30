# Microsoft Store submission notes

## Store description

BiliRadar is packaged as a self-contained app for Microsoft Store distribution and does not require users to install the .NET Desktop Runtime separately.

This app connects to Bilibili and requires the user's own Bilibili account for subscription, watch-later, and notification-related features.

## Notes for certification

BiliRadar can be launched without installing any external .NET runtime. The Store package is built from the `Release` configuration with self-contained .NET and Windows App SDK runtime settings enabled.

BiliRadar requires the user to sign in with a Bilibili account before its primary functionality can be tested. The app reads the signed-in user's Bilibili subscriptions, watch-later list, and related update information to provide monitoring and notifications.

We are unable to provide a shared Bilibili test account because Bilibili login requires phone number verification / SMS verification and may trigger additional account security checks during sign-in.

Suggested verification path:

1. Launch BiliRadar.
2. Open the sign-in flow.
3. Sign in to Bilibili with a valid Bilibili account in the in-app WebView login window.
4. Confirm the app loads account-related Bilibili data and can show update notification settings.

If a test account cannot be provided, disclose that Bilibili account access is required in the first two lines of the Store description.
