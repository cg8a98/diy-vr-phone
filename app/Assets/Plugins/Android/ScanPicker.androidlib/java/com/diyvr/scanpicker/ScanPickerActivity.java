package com.diyvr.scanpicker;

import android.app.Activity;
import android.content.ActivityNotFoundException;
import android.content.ContentResolver;
import android.content.Intent;
import android.database.Cursor;
import android.net.Uri;
import android.os.Bundle;
import android.provider.OpenableColumns;

import java.io.File;
import java.io.FileOutputStream;
import java.io.InputStream;
import java.io.OutputStream;

/**
 * A see-through screen that opens Android's file picker, copies the chosen file into the app's private storage
 * and closes again. The Unity side (ScanPicker.cs) starts it and then polls getState().
 * The file is only copied on the phone; nothing is sent anywhere.
 */
public class ScanPickerActivity extends Activity {
    // Must match the constants in ScanPicker.cs.
    public static final int IDLE = 0;
    public static final int PICKING = 1;
    public static final int DONE = 2;
    public static final int CANCELLED = 3;
    public static final int FAILED = 4;

    private static final int REQUEST_PICK = 4711;

    private static volatile int state = IDLE;
    private static volatile String destPath = "";
    private static volatile String displayName = "";
    private static volatile String error = "";

    private boolean copying;

    /** Called from Unity: show the picker and copy the chosen file to destPath. */
    public static void start(Activity activity, String dest) {
        destPath = dest == null ? "" : dest;
        displayName = "";
        error = "";
        state = PICKING;
        try {
            activity.startActivity(new Intent(activity, ScanPickerActivity.class));
        } catch (RuntimeException e) {
            fail("could not open the file picker: " + e.getMessage());
        }
    }

    public static int getState() {
        return state;
    }

    public static String getDisplayName() {
        return displayName;
    }

    public static String getError() {
        return error;
    }

    public static void reset() {
        state = IDLE;
        displayName = "";
        error = "";
    }

    private static void fail(String message) {
        error = message == null ? "unknown error" : message;
        state = FAILED;
    }

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        if (savedInstanceState != null) {
            return; // re-created while the picker is open; the result still arrives in onActivityResult
        }
        Intent intent = new Intent(Intent.ACTION_OPEN_DOCUMENT);
        intent.addCategory(Intent.CATEGORY_OPENABLE);
        intent.setType("*/*"); // Android has no file type for .glb on every phone; the app checks the file itself
        intent.putExtra(Intent.EXTRA_TITLE, getString(R.string.scan_picker_title));
        try {
            startActivityForResult(intent, REQUEST_PICK);
        } catch (ActivityNotFoundException e) {
            fail("this phone has no file picker");
            finish();
        }
    }

    @Override
    protected void onActivityResult(int requestCode, int resultCode, Intent data) {
        super.onActivityResult(requestCode, resultCode, data);
        if (requestCode != REQUEST_PICK) {
            return;
        }
        final Uri uri = data != null ? data.getData() : null;
        if (resultCode != RESULT_OK || uri == null) {
            state = CANCELLED;
            finish();
            return;
        }
        final String dest = destPath;
        if (dest.isEmpty()) {
            fail("the app was restarted while choosing; please try again");
            finish();
            return;
        }
        copying = true;
        final ContentResolver resolver = getApplicationContext().getContentResolver();
        new Thread(new Runnable() {
            @Override
            public void run() {
                try {
                    String name = queryName(resolver, uri);
                    copy(resolver, uri, new File(dest));
                    displayName = name;
                    state = DONE;
                } catch (Exception e) {
                    fail("could not copy the file: " + e.getMessage());
                }
                runOnUiThread(new Runnable() {
                    @Override
                    public void run() {
                        finish();
                    }
                });
            }
        }, "ScanPickerCopy").start();
    }

    @Override
    protected void onDestroy() {
        super.onDestroy();
        if (isFinishing() && !copying && state == PICKING) {
            state = CANCELLED;
        }
    }

    private static String queryName(ContentResolver resolver, Uri uri) {
        String name = null;
        try (Cursor cursor = resolver.query(uri, new String[] {OpenableColumns.DISPLAY_NAME}, null, null, null)) {
            if (cursor != null && cursor.moveToFirst()) {
                int column = cursor.getColumnIndex(OpenableColumns.DISPLAY_NAME);
                if (column >= 0) {
                    name = cursor.getString(column);
                }
            }
        } catch (RuntimeException e) {
            name = null; // the name is only for display
        }
        if (name == null || name.isEmpty()) {
            name = uri.getLastPathSegment();
        }
        return name == null ? "scan.glb" : name;
    }

    private static void copy(ContentResolver resolver, Uri uri, File dest) throws Exception {
        File parent = dest.getParentFile();
        if (parent != null && !parent.exists() && !parent.mkdirs()) {
            throw new Exception("cannot create " + parent.getName());
        }
        try (InputStream in = resolver.openInputStream(uri)) {
            if (in == null) {
                throw new Exception("cannot open the chosen file");
            }
            try (OutputStream out = new FileOutputStream(dest)) {
                byte[] buffer = new byte[1 << 16];
                int n;
                while ((n = in.read(buffer)) > 0) {
                    out.write(buffer, 0, n);
                }
                out.flush();
            }
        }
    }
}
