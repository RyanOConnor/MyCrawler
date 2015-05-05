package com.android.ryan.cloudcrawlerclient;

import android.app.AlertDialog;
import android.content.Context;
import android.content.DialogInterface;
import android.content.Intent;
import android.content.pm.ApplicationInfo;
import android.content.res.AssetManager;
import android.os.AsyncTask;
import android.os.Build;
import android.os.Bundle;
import android.support.v7.app.ActionBarActivity;
import android.util.Log;
import android.util.Pair;
import android.view.KeyEvent;
import android.view.LayoutInflater;
import android.view.Menu;
import android.view.MenuItem;
import android.view.MotionEvent;
import android.view.View;
import android.webkit.JavascriptInterface;
import android.webkit.WebView;
import android.webkit.WebViewClient;
import android.widget.EditText;
import android.widget.Toast;

import org.json.JSONArray;
import org.json.JSONObject;

import java.io.BufferedReader;
import java.io.IOException;
import java.io.InputStream;
import java.io.InputStreamReader;
import java.util.HashSet;
import java.util.List;

public class TargetContentActivity extends ActionBarActivity {

    WebView webView;
    Context browserContext;
    MyJavaScriptInterface javaScriptInterface;
    volatile boolean pageIsFinished = true;     // Set this to false to disable touch events prior to page finishing

    boolean modifyingFeed;
    static FeedResults linkFeedResults;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_target_content);
        browserContext = this;

        webView = (WebView)findViewById(R.id.web_view);
        allowChromeDebugging();
        webView.getSettings().setJavaScriptEnabled(true);
        webView.getSettings().setDomStorageEnabled(true);
        javaScriptInterface = new MyJavaScriptInterface();
        webView.addJavascriptInterface(javaScriptInterface, "HTMLOUT");

        webView.setOnTouchListener(new View.OnTouchListener() {
            @Override
            public boolean onTouch(View v, MotionEvent event) {
                if(pageIsFinished)
                    return false;   // Allows touch events
                else
                    return true;    // Suppresses touch events
            }
        });
        webView.setWebViewClient(new WebViewClient() {
            @Override
            public void onPageFinished(WebView view, String url) {
                pageIsFinished = true;
            }
        });

        Bundle extras = getIntent().getExtras();
        String url = "";
        if(extras != null){
            modifyingFeed = extras.getBoolean("modifyingFeed");
            if(modifyingFeed){
                linkFeedResults = (FeedResults)extras.get("feedResults");
                url = linkFeedResults.url;
            } else {
               url = extras.getString("url");
                if (!url.startsWith("http://")) {
                    url = "http://" + url;
                }
            }
        }

        webView.loadUrl(url);
    }

    @Override
    public boolean onOptionsItemSelected(MenuItem item) {
        int id = item.getItemId();
        if (id == R.id.action_begin_select) {
            injectScript(webView, "browserScript.js");
            return true;
        }
        return super.onOptionsItemSelected(item);
    }

    private void injectScript(WebView view, String path){
        StringBuilder text = new StringBuilder();
        try{
            AssetManager manager = getAssets();
            InputStream input = manager.open(path);
            BufferedReader reader = new BufferedReader(new InputStreamReader(input));
            String line;
            while ((line = reader.readLine()) != null) {
                text.append(line);
            }
            view.loadUrl("javascript:" + text.toString());
        } catch (IOException ex) {
            ex.printStackTrace();
            Log.d(this.getClass().toString(), ex.getStackTrace().toString());
        }
    }

    @Override
    public boolean onKeyDown(int keycode, KeyEvent event){
        if((keycode == KeyEvent.KEYCODE_BACK) && (webView.canGoBack())){
            webView.goBack();
            return true;
        }
        return super.onKeyDown(keycode, event);
    }

    @Override
    public boolean onCreateOptionsMenu(Menu menu) {
        // Inflate the menu; this adds items to the action bar if it is present.
        getMenuInflater().inflate(R.menu.menu_browser, menu);
        return true;
    }

    public void allowChromeDebugging(){
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.KITKAT) {
            if (0 != (getApplicationInfo().flags &= ApplicationInfo.FLAG_DEBUGGABLE)){
                WebView.setWebContentsDebuggingEnabled(true);
            }
        }
    }

    public class MyJavaScriptInterface {

        @SuppressWarnings("unused")
        @JavascriptInterface
        public void showHTML(String path, String host, String json) throws Exception{
            JSONParser parser = new JSONParser();
            JSONArray jArray = new JSONArray(json);
            final List<Link> links = parser.parseJavascriptStringify(jArray);
            displayKeywordDialog(path, host, links);
        }

        public void displayKeywordDialog(final String path, final String host, final List<Link> results){
            AlertDialog.Builder dialogBuilder = new AlertDialog.Builder(browserContext);
            dialogBuilder.setCancelable(false);
            dialogBuilder.setTitle("Any keywords of interest for " + host + "?");
            dialogBuilder.setMessage("We'll rank your results according to your specified interests");
            dialogBuilder.setNegativeButton("Yes", new DialogInterface.OnClickListener() {
                @Override
                public void onClick(DialogInterface dialogInterface, int which) {
                    final HashSet<String> keywords = new HashSet<String>();
                    final AlertDialog.Builder keywordBuilder = new AlertDialog.Builder(browserContext);
                    LayoutInflater inflater = LayoutInflater.from(browserContext);
                    View view = inflater.inflate(R.layout.keyword_prompt, null);
                    final EditText userKeyword = (EditText) view.findViewById(R.id.user_keywords);

                    keywordBuilder.setView(view);
                    keywordBuilder.setCancelable(false);
                    keywordBuilder.setPositiveButton("Done", new DialogInterface.OnClickListener() {
                        @Override
                        public void onClick(DialogInterface dialog, int id) {
                            LinkFeed linkFeed = new LinkFeed(host, path, keywords, results);
                            if (modifyingFeed)
                                new AsyncModifyFeed().execute(linkFeed, linkFeedResults);
                            else
                                new AsyncCreateFeed().execute(linkFeed);
                            //finish();
                        }
                    });
                    keywordBuilder.setNegativeButton("OK", new DialogInterface.OnClickListener() {
                        @Override
                        public void onClick(DialogInterface dialog, int id) {
                        }
                    });
                    AlertDialog dialog = keywordBuilder.create();
                    dialog.show();
                    dialog.getButton(AlertDialog.BUTTON_NEGATIVE).setOnClickListener(new View.OnClickListener() {
                        @Override
                        public void onClick(final View v) {
                            final String keyword = userKeyword.getText().toString();
                            keywords.add(keyword);
                            userKeyword.setText("");
                            runOnUiThread(new Runnable() {
                                public void run() {
                                    Toast.makeText(v.getContext(), "Added \"" + keyword + "\"", Toast.LENGTH_SHORT).show();
                                }
                            });
                        }
                    });
                }
            });
            dialogBuilder.setPositiveButton("No", new DialogInterface.OnClickListener() {
                @Override
                public void onClick(DialogInterface dialog, int which) {
                    LinkFeed linkFeed = new LinkFeed(host, path, new HashSet<String>(), results);
                    if (modifyingFeed)
                        new AsyncModifyFeed().execute(linkFeed, linkFeedResults);
                    else
                        new AsyncCreateFeed().execute(linkFeed);
                    //finish();
                }
            });
            dialogBuilder.create();
            dialogBuilder.show();
        }

        public void displayLinkAlreadyExists(String url){
            new AlertDialog.Builder(browserContext)
                    .setTitle("Feed Already Exists")
                    .setMessage("It seems you already have a feed for " + url)
                    .setPositiveButton(android.R.string.ok, null)
                    .setCancelable(false)
                    .create()
                    .show();
        }
    }

    protected class AsyncCreateFeed extends AsyncTask<LinkFeed, Void, Pair<RestAPI.ServerResponse, LinkFeed>> {

        LinkFeed feed;

        @Override
        protected Pair<RestAPI.ServerResponse, LinkFeed> doInBackground(LinkFeed... params){
            feed = params[0];
            RestAPI api = new RestAPI();
            Pair<RestAPI.ServerResponse, LinkFeed> response = null;
            try{
                String userid = AccessState.instance().getUserID(getApplicationContext());
                JSONObject jsonObj = api.AddLinkFeed(userid, feed.url, feed.htmlTags, feed.keywords);
                JSONParser parser = new JSONParser();
                response = parser.parseAddFeedResponse(jsonObj);
            } catch (Exception ex) {
                ex.printStackTrace();
                Log.d(this.getClass().toString(), ex.getStackTrace().toString());
            }
            return response;
        }

        @Override
        protected void onPostExecute(Pair<RestAPI.ServerResponse, LinkFeed> response) {
            switch(response.first){
                case Success:
                    if(response.second.userPageRank == null || response.second.userPageRank.isEmpty()){
                        response.second.userPageRank = feed.userPageRank;
                    }
                    StorageManager.instance().saveFeedResults((FeedResults)response.second, browserContext);
                    startActivity(new Intent(browserContext, MainActivity.class));
                    finish();
                    break;
                case LinkAlreadyExists:
                    javaScriptInterface.displayLinkAlreadyExists(feed.url);
                    break;
                case ServerError:
                    Toast.makeText(browserContext, "Server error, try again", Toast.LENGTH_SHORT).show();
                    break;
            }
        }
    }

    protected class AsyncModifyFeed extends AsyncTask<LinkFeed, Void, Pair<RestAPI.ServerResponse, LinkFeed>> {

        LinkFeed linkFeed;
        FeedResults previousFeed;

        @Override
        protected Pair<RestAPI.ServerResponse, LinkFeed> doInBackground(LinkFeed... params){
            linkFeed = params[0];
            previousFeed = (FeedResults)params[1];
            RestAPI api = new RestAPI();
            Pair<RestAPI.ServerResponse, LinkFeed> response = null;
            try {
                String userid = AccessState.instance().getUserID(getApplicationContext());
                JSONObject obj = api.ModifyFeed(userid, previousFeed.getRecordId(), previousFeed.getResultsId(),
                                                linkFeed.htmlTags, linkFeed.keywords);
                JSONParser parser = new JSONParser();
                response = parser.parseAddFeedResponse(obj);
            } catch(Exception ex) {
                ex.printStackTrace();
                Log.d(this.getClass().toString(), ex.getStackTrace().toString());
            }
            return response;
        }

        @Override
        protected void onPostExecute(Pair<RestAPI.ServerResponse, LinkFeed> response){
            switch (response.first){
                case Success:
                    StorageManager.instance().saveFeedResults((FeedResults)response.second, browserContext);
                    startActivity(new Intent(browserContext, MainActivity.class));
                    finish();
                    break;
                case ServerError:
                    Toast.makeText(browserContext, "Server error, try again", Toast.LENGTH_SHORT).show();
                    break;
            }
        }
    }
}
