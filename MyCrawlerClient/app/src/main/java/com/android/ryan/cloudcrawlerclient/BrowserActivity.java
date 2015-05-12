package com.android.ryan.cloudcrawlerclient;

import android.support.v7.app.ActionBarActivity;
import android.os.Bundle;
import android.view.KeyEvent;
import android.view.Menu;
import android.view.MenuItem;
import android.webkit.WebView;
import android.webkit.WebViewClient;


public class BrowserActivity extends ActionBarActivity {

    WebView browserView;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_browser);

        String url = getIntent().getStringExtra("url");
        browserView = (WebView)findViewById(R.id.browser_view);
        browserView.setWebViewClient(new WebViewClient());
        browserView.loadUrl(url);
    }

    @Override
    public boolean onKeyDown(int keycode, KeyEvent event){
        if((keycode == KeyEvent.KEYCODE_BACK) && (browserView.canGoBack())){
            browserView.goBack();
            return true;
        }
        return super.onKeyDown(keycode, event);
    }

    @Override
    public boolean onOptionsItemSelected(MenuItem item) {
        return super.onOptionsItemSelected(item);
    }
}
