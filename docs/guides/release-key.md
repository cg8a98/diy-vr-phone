# Release key and installing builds

Every merge to `main` builds a signed app and publishes it as a numbered release on GitHub. The signing key is what lets a new version install over the old one and keep your settings. It is made once, and it is permanent: if you lose it, you cannot update the installed app (you would have to uninstall first and lose its data).

You need nothing but Git for Windows. No Java, no Android Studio.

## Part 1: Make the key (once)

1. Open File Explorer and go to `C:\Claude Code Projects\DIY VR Smartphone Headset\private\`. Create a new folder inside it called `release-key`.
2. Open the Start menu, type **Git Bash** and open it. A black window appears.
3. Go to the new folder. Copy this line, paste it into Git Bash (right-click -> Paste) and press Enter:

   ```
   cd "/c/Claude Code Projects/DIY VR Smartphone Headset/private/release-key"
   ```
   - How you know it worked: the line ends with `release-key`, and no error appears.
4. Choose a password. Make it long: at least 16 characters, for example four unrelated words and a number. Do not reuse a password from anywhere else. Write it in your password manager (or on paper kept safely). You will type it three times below.
   - Important: the store password and the key password must be the same. The key file format used here (PKCS12) does not allow two different ones. In GitHub you will enter the same password twice.
   - Avoid quotes, backslashes and spaces in the password. They cause trouble later.
5. Create the key and certificate. Paste this and press Enter (nothing is asked; it just makes two files):

   ```
   MSYS_NO_PATHCONV=1 openssl req -x509 -newkey rsa:2048 -sha256 -days 10000 -nodes -keyout key.pem -out cert.pem -subj "/CN=Carlos DIY VR"
   ```
   - How you know it worked: `key.pem` and `cert.pem` now exist in the folder (type `ls` to see them).
6. Pack them into one keystore file. Paste this and press Enter. It asks for the password: type your password from step 4 (you will not see it as you type; that is normal), then type it again to confirm:

   ```
   openssl pkcs12 -export -inkey key.pem -in cert.pem -name diyvr -out release.p12
   ```
   - The name `diyvr` is the "alias". You will need it later.
   - How you know it worked: `ls` shows `release.p12`.
7. Check it opens. Paste this, type your password when asked:

   ```
   openssl pkcs12 -in release.p12 -info -noout
   ```
   - How you know it worked: it prints lines like `MAC: sha256` and `PKCS7 Encrypted data`, and no "invalid password" or "error".
8. Delete the two loose files, which are no longer needed:

   ```
   rm key.pem cert.pem
   ```
9. **Back it up now.** Copy `release.p12` to a second place (a USB stick or your cloud drive's private area). The password goes in your password manager. Never put the file or password in the repository, in an email, or in a chat with Claude.
   - How you know it worked: you can open the second location and see `release.p12`.

## Part 2: Give the key to GitHub (once)

10. In Git Bash, make the file into one line of text and copy it to the clipboard:

    ```
    base64 -w0 release.p12 | clip
    ```
    - How you know it worked: no output and no error. The clipboard now holds a very long line of letters and numbers.
11. In your browser open the repository on GitHub: **Settings** -> **Secrets and variables** -> **Actions** -> **New repository secret**.
12. Add the first secret. Name: `ANDROID_KEYSTORE_BASE64`. Secret: paste (Ctrl+V). Click **Add secret**.
13. Add three more, one at a time. Names must match exactly:
    - `ANDROID_KEYSTORE_PASS`: your password from step 4
    - `ANDROID_KEYALIAS_NAME`: `diyvr`
    - `ANDROID_KEYALIAS_PASS`: your password from step 4 again (the same one)
14. Clear the clipboard by copying any other text.

How you know it worked: under **Repository secrets** you see the four new names, plus the three Unity ones from before.

## Part 3: Check it works

15. Merge the pull request for this ticket (Claude will tell you when it is ready), or merge any later one.
16. On GitHub open the **Actions** tab and wait for the run on `main` to finish (about as long as a pull-request build; the "Publish release" job runs last).
17. Click **Releases** on the right side of the repository's main page.

How you know it worked: there is a release named like `DIY VR 0.1.12`, with an `.apk` file attached, and a short list of what changed. Each later merge gives a higher number.

If the build fails with "Secret ... is missing", one of the four names in Part 2 is missing or misspelled. Fix it in GitHub and use **Re-run failed jobs**. The build refuses to publish an unsigned app on purpose.

If the build fails at the signing step with a password error, the two passwords differ from the one in the file. Re-enter both secrets.

## How to install a build on your phone

1. On your phone, open the browser and go to the repository's **Releases** page (bookmark it).
2. Tap the newest release. Under **Assets**, tap the `.apk` file to download it.
3. If the phone asks, tap **Allow** for downloads, and when it warns that this type of file can be harmful, tap **Download anyway**. It is your own app.
4. Open the downloaded file (from the browser's download notice, or the **Files** / **My Files** app, Downloads folder).
5. The first time, Android says the browser is not allowed to install apps. Tap **Settings**, switch on **Allow from this source**, then go back.
6. Tap **Install**. If an older version is already installed, this installs over it and keeps your settings. This works because every release is signed with the same key.
7. Tap **Open**.

How you know it worked: the app opens, and on a second release the app's version number is higher without you having to set anything up again.

If the install says "App not installed" or "conflicts with an existing package": the old version was signed with a different key (for example a debug build from a pull request). Uninstall the old app once, then install the release. From then on, updates install over the top.
