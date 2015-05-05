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


    /*@Override
    public boolean onCreateOptionsMenu(Menu menu) {
        // Inflate the menu; this adds items to the action bar if it is present.
        getMenuInflater().inflate(R.menu.menu_browser, menu);
        return true;
    }*/

    @Override
    public boolean onOptionsItemSelected(MenuItem item) {
        // Handle action bar item clicks here. The action bar will
        // automatically handle clicks on the Home/Up button, so long
        // as you specify a parent activity in AndroidManifest.xml.
        int id = item.getItemId();

        return super.onOptionsItemSelected(item);
    }
}
