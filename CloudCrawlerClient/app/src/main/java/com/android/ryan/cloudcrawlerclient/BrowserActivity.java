package com.android.ryan.cloudcrawlerclient;

import android.app.AlertDialog;
import android.content.Context;
import android.net.Uri;
import android.support.v7.app.ActionBarActivity;
import android.os.Bundle;
import android.util.Base64;
import android.util.Log;
import android.view.Menu;
import android.view.MenuItem;
import android.webkit.JavascriptInterface;
import android.webkit.JsResult;
import android.webkit.WebChromeClient;
import android.webkit.WebView;
import android.webkit.WebViewClient;

import org.w3c.dom.NodeList;

import java.io.File;
import java.io.FileInputStream;
import java.io.FileNotFoundException;
import java.io.IOException;
import java.io.InputStream;
import java.net.URL;
import java.util.logging.Handler;


public class BrowserActivity extends ActionBarActivity {

    WebView webView;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);

        setContentView(R.layout.activity_browser);

        webView = (WebView)findViewById(R.id.webView);
        webView.getSettings().setJavaScriptEnabled(true);
        webView.getSettings().setDomStorageEnabled(true);
        webView.addJavascriptInterface(new MyJavaScriptInterface(), "HTMLOUT");
        webView.setWebViewClient(new WebViewClient() {
            @Override
            public void onPageFinished(WebView view, String url) {
                injectScript(webView, "C:/Users/Ryan/AndroidStudioProjects/CloudCrawlerClient/app/src/BrowserScript.txt");
                /*webView.loadUrl("javascript:"
                                       + "var x = document.getElementsByTagName('a');"
                                       + "for(var i = 0; i < x.length; i++){"
                                       + "  x[i].addEventListener(\"click\", highlight, false);"
                                       + "  x[i].addEventListener(\"click\", locateNode, false);"
                                       + "}"
                                       + "function highlight() {"
                                       + "  this.style.backgroundColor = \"yellow\";"
                                       + "  this.style.color = \"black\";"
                                       + "  this.href = \"javascript:void(0);\";"
                                       + "  if(this.className.indexOf(\" \") != -1){"
                                       + "      var classes = this.className.split(' ');"
                                       + "      var query = classes[0];"
                                       + "      for(var j = 1; j < classes.length; j++) {"
                                       + "          query = query + \" \" + classes[j];"
                                       + "      }"
                                       + "      var nodes = document.getElementsByClassName(query);"
                                       + "      for(var k = 0; k < nodes.length; k++){"
                                       + "          nodes[k].style.backgroundColor = \"yellow\";"
                                       + "          nodes[k].style.color = \"black\";"
                                       + "          nodes[k].href = \"javascript:void(0);\";"
                                       + "      }"
                                       + "  } else {"
                                       + "      var nodes = document.getElementsByClassName(this.className);"
                                       + "      for(var k = 0; k < nodes.length; k++) {"
                                       + "          nodes[k].style.backgroundColor = \"yellow\";"
                                       + "          nodes[k].style.color = \"black\";"
                                       + "          nodes[k].href = \"javascript:void(0);\";"
                                       + "      }"
                                       + "  }"
                                       + "}"
                                       + "function locateNode() {"
                                       + "  if(!this.className) {"
                                       + "      var retValue = constructNodePath(this);"
                                       + "      window.HTMLOUT.showHTML(retValue);"
                                       + "  } else {"
                                       + "      window.HTMLOUT.showHTML(this.className);"
                                       + "  }"
                                       + "}"
                                       + "function constructNodePath(node) {"
                                       + "  var nodePath = node.tagName + node.className;"
                                       + "  while(!node.className) {"
                                       + "      node = node.parentNode;"
                                       + "      if(node.className){"
                                       + "          nodePath = node.tagName + '.' + node.className + ' ' + nodePath;"
                                       + "      } else {"
                                       + "          nodePath = node.tagName + ' ' + nodePath;"
                                       + "      }"
                                       + "  }"
                                       + "}"
                                       );*/
            }
        });

        Bundle extras = getIntent().getExtras();
        if(extras != null){
            String url = extras.getString("url");
            if(!url.startsWith("http://")){
                url = "http://" + url;
            }
            webView.loadUrl(url);
        }
    }

    private void injectScript(WebView view, String path){
        String content = "";
        try{
            InputStream input = getAssets().open(path);
            byte[] buffer = new byte[input.available()];
            while (input.read(buffer) != -1) {
                content += new String(buffer);
            }
            view.loadUrl("javascript:" + content);
        }catch (IOException ex) {
            ex.printStackTrace();
        }
    }

    final Context myApp = this;

    @Override
    public boolean onCreateOptionsMenu(Menu menu) {
        // Inflate the menu; this adds items to the action bar if it is present.
        getMenuInflater().inflate(R.menu.menu_browser, menu);
        return true;
    }

    @Override
    public boolean onOptionsItemSelected(MenuItem item) {
        // Handle action bar item clicks here. The action bar will
        // automatically handle clicks on the Home/Up button, so long
        // as you specify a parent activity in AndroidManifest.xml.
        int id = item.getItemId();

        //noinspection SimplifiableIfStatement
        if (id == R.id.action_settings) {
            return true;
        }

        return super.onOptionsItemSelected(item);
    }

    public class MyJavaScriptInterface {

        @SuppressWarnings("unused")
        @JavascriptInterface
        public void showHTML(String html){
            String test = html;
            new AlertDialog.Builder(myApp)
                    .setTitle("HTML")
                    .setMessage(html)
                    .setPositiveButton(android.R.string.ok, null)
                    .setCancelable(false)
                    .create()
                    .show();
        }

        public void getHtmlTags(final String html){

        }
    }

    final class MyWebChromeClient extends WebChromeClient {
        @Override
        public boolean onJsAlert(WebView view, String url, String message, JsResult result) {
            Log.d("LogTag", message);
            result.confirm();
            return true;
        }
    }
}
