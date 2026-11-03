# Adding the Unity secrets to GitHub

GitHub builds your app in the cloud. It needs your free Unity licence to do that. You type three values into GitHub once. Claude never sees them.

You already did these steps. Keep this page in case you ever need to redo them (new PC, expired licence).

## Steps

1. Open **Unity Hub** on your PC.
2. Click your profile icon (or the gear) and open **Preferences** -> **Licenses**.
3. Click **Add** and choose the **free personal licence** option. Accept the terms.
   - How you know it worked: a "Unity Personal" licence is listed.
4. Open File Explorer. Click the address bar, type `C:\ProgramData\Unity` and press Enter.
5. Find the file `Unity_lic.ulf`. Open it with Notepad. Select all (Ctrl+A) and copy (Ctrl+C). This whole text is the first secret.
6. If you sign in to Unity with a Google login, you have no Unity password yet. Go to https://id.unity.com, click **Forgot your password**, and set one. This password is the second secret.
7. In your browser open the repository on GitHub: **Settings** -> **Secrets and variables** -> **Actions** -> **New repository secret**.
8. Add three secrets, one at a time. Names must match exactly:
   - `UNITY_LICENSE`: the full text you copied from `Unity_lic.ulf`
   - `UNITY_EMAIL`: the email of your Unity account
   - `UNITY_PASSWORD`: the Unity password from step 6
9. Click **Add secret** after each one.

## How you know it worked

On the same page, under **Repository secrets**, you see three names listed: `UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD`. (GitHub never shows the values again; that is normal.)

The real proof is a pull request: the "EditMode tests" and "Android APK" checks start, and turn green.

## If a build says the licence is invalid

Repeat steps 1-5 and replace the `UNITY_LICENSE` secret with the new text.
